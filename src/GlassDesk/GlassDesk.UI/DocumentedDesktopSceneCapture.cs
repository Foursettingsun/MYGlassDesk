using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GlassDesk.Platform;
using GlassDesk.UI.CaptureCompatibility;
using GlassDesk.UI.Controls;

internal sealed class DocumentedDesktopSceneCapture : IDesktopSceneCapture, IDisposable
{
	private readonly record struct SceneWindow(nint Handle, ScreenRect Bounds);

	private struct Rect(int left, int top, int right, int bottom)
	{
		public int Left = left;

		public int Top = top;

		public int Right = right;

		public int Bottom = bottom;
	}

	private struct Size
	{
		public int Width;

		public int Height;
	}

	private struct ThumbnailProperties
	{
		public uint Flags;

		public Rect Destination;

		public Rect Source;

		public byte Opacity;

		public int Visible;

		public int ClientOnly;
	}

	private delegate bool EnumWindowProc(nint hwnd, nint parameter);

	private readonly ScreenRect _region;

	private readonly Action<CaptureTrace>? _trace;

	private readonly Func<int[]>? _excludedProcesses;

	private readonly bool _diagnosticOnScreen;

	private readonly Func<nint> _desktopWindow;

	private nint _carrier;

	private nint _owner;

	private HwndSource? _surface;

	private NativeWallpaperSurface? _nativeWallpaper;

	private readonly DrawingVisual _background = new DrawingVisual();

	private readonly WallpaperBackdrop _wallpaper = new WallpaperBackdrop();

	private readonly bool _requireVerifiedWallpaper;

	private readonly bool _desktopOnly;

	private IWindowCapture? _capture;

	private DesktopBackgroundComposer? _backgroundComposer;

	private byte[]? _backgroundPixels;

	private long _nextBackgroundTraceTicks;

	private readonly List<nint> _thumbnails = new List<nint>();

	private readonly HashSet<nint> _excludedOverlays = new HashSet<nint>();

	private SceneWindow[]? _members;

	private nint _lastDesktopWindow;

	private long _barrier;

	private nint _liveWallpaper;

	private nint _liveHost;

	private long _nextWallpaperScanTicks;

	private LiveWallpaperSurface? _liveSurface;

	private WriteableBitmap? _liveBitmap;

	private byte[]? _livePixels;

	private int _liveWidth;

	private int _liveHeight;

	internal nint CarrierHandle => _carrier;

	nint IDesktopSceneCapture.CarrierHandle => CarrierHandle;

	public long SceneRevision => _barrier;

	internal DocumentedDesktopSceneCapture(ScreenRect region, bool diagnosticOnScreen = false, Action<CaptureTrace>? trace = null)
		: this(region, diagnosticOnScreen, trace, null)
	{
	}

	internal DocumentedDesktopSceneCapture(ScreenRect region, bool diagnosticOnScreen = false, Action<CaptureTrace>? trace = null, Func<int[]>? excludedProcesses = null)
		: this(region, diagnosticOnScreen, trace, excludedProcesses, GetShellWindow)
	{
	}

	internal DocumentedDesktopSceneCapture(ScreenRect region, bool diagnosticOnScreen, Action<CaptureTrace>? trace, Func<int[]>? excludedProcesses, bool desktopOnly)
		: this(region, diagnosticOnScreen, trace, excludedProcesses, GetShellWindow, WindowsCapabilities.BuildNumber, desktopOnly)
	{
	}

	internal DocumentedDesktopSceneCapture(ScreenRect region, bool diagnosticOnScreen, Action<CaptureTrace>? trace, Func<int[]>? excludedProcesses, Func<nint> desktopWindow)
		: this(region, diagnosticOnScreen, trace, excludedProcesses, desktopWindow, WindowsCapabilities.BuildNumber)
	{
	}

	internal DocumentedDesktopSceneCapture(ScreenRect region, bool diagnosticOnScreen, Action<CaptureTrace>? trace, Func<int[]>? excludedProcesses, Func<nint> desktopWindow, int windowsBuild)
		: this(region, diagnosticOnScreen, trace, excludedProcesses, desktopWindow, windowsBuild, desktopOnly: false)
	{
	}

	internal DocumentedDesktopSceneCapture(ScreenRect region, bool diagnosticOnScreen, Action<CaptureTrace>? trace, Func<int[]>? excludedProcesses, Func<nint> desktopWindow, int windowsBuild, bool desktopOnly)
	{
		if (region.Width <= 0 || region.Height <= 0 || (long)region.Width * (long)region.Height > 32000000)
		{
			throw new ArgumentOutOfRangeException("region");
		}
		_region = region;
		_trace = trace;
		_excludedProcesses = excludedProcesses;
		_diagnosticOnScreen = diagnosticOnScreen;
		_desktopWindow = desktopWindow;
		_requireVerifiedWallpaper = windowsBuild < 22000;
		_desktopOnly = desktopOnly;
		string value = "SceneWindow";
		try
		{
			_owner = CreateWindowExW(0u, "STATIC", "GlassDesk scene owner", 2147483648u, 0, 0, 1, 1, 0, 0, 0, 0);
			if (_owner == 0)
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
			uint num = (diagnosticOnScreen ? 8u : 524320u);
			if (!diagnosticOnScreen && _requireVerifiedWallpaper)
			{
				num |= 0x8000080;
			}
			uint style = (_requireVerifiedWallpaper ? 2147483662u : 2147483648u);
			_carrier = CreateWindowExW(num, "STATIC", "GlassDesk compatible desktop scene", style, region.X, region.Y, region.Width, region.Height, 0, 0, 0, 0);
			if (_carrier == 0)
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
			if (_requireVerifiedWallpaper)
			{
				_nativeWallpaper = new NativeWallpaperSurface(_carrier, region.Width, region.Height);
			}
			else
			{
				_surface = new HwndSource(new HwndSourceParameters("GlassDesk compatible scene background", region.Width, region.Height)
				{
					WindowStyle = 1342177280,
					ParentWindow = _carrier,
					PositionX = 0,
					PositionY = 0
				});
				_surface.RootVisual = _background;
			}
			if (!diagnosticOnScreen && !SetLayeredWindowAttributes(_carrier, 0u, 0, 2u))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
			int value2 = 1;
			DwmSetWindowAttribute(_carrier, 11, ref value2, 4);
			DwmSetWindowAttribute(_carrier, 12, ref value2, 4);
			if (!SetWindowPos(_carrier, (!diagnosticOnScreen) ? 1 : (-1), region.X, region.Y, region.Width, region.Height, 80u))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
			value = "Wallpaper";
			UpdateWallpaper();
			value = "DwmThumbnails";
			UpdateExclusions();
			value = "WindowCapture";
			if (_capture == null)
			{
				_capture = StartWindowCapture();
			}
			_trace?.Invoke(new CaptureTrace("BackdropProvider", "DocumentedThumbnails", Stopwatch.GetTimestamp(), 0L, 0L, 0L, 0L, 0, 0L));
		}
		catch (Exception ex)
		{
			Dispose();
			throw new InvalidOperationException($"Documented scene initialization failed at {value} (0x{ex.HResult:X8}): {ex.Message}", ex);
		}
	}

	internal bool UpdateExclusions()
	{
		UpdateWallpaper();
		SceneWindow[] array = Snapshot(out var desktopWindow);
		if (_members != null && _lastDesktopWindow == desktopWindow && ((ReadOnlySpan<SceneWindow>)_members.AsSpan()).SequenceEqual((ReadOnlySpan<SceneWindow>)array))
		{
			return false;
		}
		List<nint> list = new List<nint>();
		List<ScreenRect> list2 = new List<ScreenRect>();
		try
		{
			SceneWindow[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				SceneWindow sceneWindow = array2[i];
				if (DwmRegisterThumbnail(_carrier, sceneWindow.Handle, out var thumbnail) < 0)
				{
					continue;
				}
				list.Add(thumbnail);
				if (DwmQueryThumbnailSourceSize(thumbnail, out var size) < 0 || size.Width <= 0 || size.Height <= 0)
				{
					continue;
				}
				ScreenRect bounds = sceneWindow.Bounds;
				int num = Math.Max(_region.X, bounds.X);
				int num2 = Math.Max(_region.Y, bounds.Y);
				int num3 = Math.Min(_region.X + _region.Width, bounds.X + bounds.Width);
				int num4 = Math.Min(_region.Y + _region.Height, bounds.Y + bounds.Height);
				if (num3 > num && num4 > num2)
				{
					ThumbnailProperties properties = new ThumbnailProperties
					{
						Flags = 31u,
						Destination = new Rect(num - _region.X, num2 - _region.Y, num3 - _region.X, num4 - _region.Y),
						Source = new Rect(Scale(num - bounds.X, size.Width, bounds.Width), Scale(num2 - bounds.Y, size.Height, bounds.Height), Scale(num3 - bounds.X, size.Width, bounds.Width), Scale(num4 - bounds.Y, size.Height, bounds.Height)),
						Opacity = byte.MaxValue,
						Visible = 1,
						ClientOnly = 0
					};
					if (DwmUpdateThumbnailProperties(thumbnail, ref properties) < 0)
					{
						_trace?.Invoke(new CaptureTrace("Thumbnail", "ForeignWindowUnavailable", Stopwatch.GetTimestamp(), 0L, 0L, 0L, 0L, 0, 0L));
					}
					else
					{
						list2.Add(new ScreenRect(num - _region.X, num2 - _region.Y, num3 - num, num4 - num2));
					}
				}
			}
			foreach (nint thumbnail2 in _thumbnails)
			{
				DwmUnregisterThumbnail(thumbnail2);
			}
			_thumbnails.Clear();
			_thumbnails.AddRange(list);
			list.Clear();
			_members = array;
			if (_requireVerifiedWallpaper)
			{
				_backgroundComposer = new DesktopBackgroundComposer(_region.Width, _region.Height, list2);
			}
			_lastDesktopWindow = desktopWindow;
			_barrier = Stopwatch.GetTimestamp();
			Marshal.ThrowExceptionForHR(DwmFlush());
			_capture?.RefreshSessionAtSceneBoundary();
			_trace?.Invoke(new CaptureTrace("Scene", "DocumentedSceneChanged", _barrier, 0L, 0L, _barrier, 0L, 0, 0L));
			return true;
		}
		finally
		{
			foreach (nint item in list)
			{
				DwmUnregisterThumbnail(item);
			}
		}
	}

	private IWindowCapture StartWindowCapture()
	{
		if (_requireVerifiedWallpaper)
		{
			PrintWindowWindowCapture result = new PrintWindowWindowCapture(_carrier, _region.Width, _region.Height, _trace);
			Action<CaptureTrace>? trace = _trace;
			if (trace != null)
			{
				trace(new CaptureTrace("WindowCapture", "PrintWindow", Stopwatch.GetTimestamp(), 0L, 0L, 0L, 0L, 0, 0L));
				return result;
			}
			return result;
		}
		SetOwner(0);
		long num = ((IntPtr)GetWindowLongPtrW(_carrier, -20)).ToInt64();
		SetWindowLongPtrW(_carrier, -20, (nint)(num & -134217761));
		if (!_diagnosticOnScreen && !SetLayeredWindowAttributes(_carrier, 0u, 1, 2u))
		{
			throw new Win32Exception(Marshal.GetLastWin32Error());
		}
		Marshal.ThrowExceptionForHR(DwmFlush());
		Program.Pump(TimeSpan.FromMilliseconds(20L));
		Marshal.ThrowExceptionForHR(DwmFlush());
		PersistentWindowCapture persistentWindowCapture = null;
		try
		{
			persistentWindowCapture = new PersistentWindowCapture(_carrier, _trace);
			if (!_diagnosticOnScreen && !SetLayeredWindowAttributes(_carrier, 0u, 0, 2u))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
			SetOwner(_owner);
			SetWindowLongPtrW(_carrier, -20, (nint)(num | 0x8000000));
			if (!_diagnosticOnScreen && !SetWindowPos(_carrier, 1, _region.X, _region.Y, _region.Width, _region.Height, 16u))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
			return persistentWindowCapture;
		}
		catch
		{
			persistentWindowCapture?.Dispose();
			throw;
		}
	}

	private void SetOwner(nint owner)
	{
		if (GetWindow(_carrier, 4u) != owner)
		{
			SetWindowLongPtrW(_carrier, -8, owner);
			if (GetWindow(_carrier, 4u) != owner)
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
		}
	}

	private static int Scale(int coordinate, int pixels, int extent)
	{
		return (int)((long)coordinate * (long)pixels / extent);
	}

	private void UpdateWallpaper()
	{
		if ((_surface == null && _nativeWallpaper == null) || !_wallpaper.Update(new System.Windows.Rect(_region.X, _region.Y, _region.Width, _region.Height), _region.Width, _region.Height, 0.0))
		{
			return;
		}
		byte[] array = _wallpaper.Pixels;
		if (array == null)
		{
			if (_requireVerifiedWallpaper && !LiveBackdropMode.Enabled)
			{
				throw new InvalidOperationException("No verified wallpaper background is available; keep translucent glass instead of synthesizing a black live scene.");
			}
			uint sysColor = GetSysColor(1);
			array = new byte[checked(_region.Width * _region.Height * 4)];
			for (int i = 0; i < array.Length; i += 4)
			{
				array[i] = (byte)(sysColor >> 16);
				array[i + 1] = (byte)(sysColor >> 8);
				array[i + 2] = (byte)sysColor;
				array[i + 3] = byte.MaxValue;
			}
		}
		if (_nativeWallpaper != null)
		{
			_backgroundPixels = array;
			_nativeWallpaper.Update(array);
			Action<CaptureTrace>? trace = _trace;
			if (trace != null)
			{
				long timestamp = Stopwatch.GetTimestamp();
				long sourceTicks = 0L;
				long endTicks = 0L;
				long barrierTicks = 0L;
				long generation = 0L;
				long windowHandle = 0L;
				int width = _region.Width;
				int height = _region.Height;
				trace(new CaptureTrace("SceneBackground", "NativeWallpaper", timestamp, sourceTicks, endTicks, barrierTicks, generation, 0, windowHandle, 0u, null, 0, 0, null, null, null, null, null, 0, width, height));
			}
			return;
		}
		BitmapSource imageSource = BitmapSource.Create(_region.Width, _region.Height, 96.0, 96.0, PixelFormats.Bgra32, null, array, _region.Width * 4);
		Matrix transformFromDevice = _surface.CompositionTarget.TransformFromDevice;
		using DrawingContext drawingContext = _background.RenderOpen();
		drawingContext.DrawImage(imageSource, new System.Windows.Rect(0.0, 0.0, (double)_region.Width * transformFromDevice.M11, (double)_region.Height * transformFromDevice.M22));
	}

	private SceneWindow[] Snapshot(out nint desktopWindow)
	{
		List<SceneWindow> windows = new List<SceneWindow>();
		int[] excluded = _excludedProcesses?.Invoke() ?? Array.Empty<int>();
		nint shell = _desktopWindow();
		desktopWindow = shell;
		if (shell == 0 || !GetWindowRect(shell, out var rect))
		{
			throw new InvalidOperationException("Desktop shell is unavailable for compatible capture.");
		}
		nint host = 0;
		if ((_requireVerifiedWallpaper || (_desktopOnly && IsDesktopHost(shell))) && (_liveWallpaper == 0 || !IsWindow(_liveWallpaper) || Stopwatch.GetTimestamp() >= _nextWallpaperScanTicks))
		{
			nint num = FindLiveWallpaper(out host);
			if (num != _liveWallpaper || host != _liveHost)
			{
				_liveWallpaper = num;
				_liveHost = host;
				_liveSurface?.Dispose();
				_liveSurface = null;
				_liveBitmap = null;
				_livePixels = null;
				_trace?.Invoke(new CaptureTrace("Wallpaper", (num == 0) ? "StaticOnly" : "LiveWindow", Stopwatch.GetTimestamp(), 0L, 0L, 0L, 0L, 0, 0L));
			}
			_nextWallpaperScanTicks = Stopwatch.GetTimestamp() + Stopwatch.Frequency;
		}
		if (_desktopOnly)
		{
			if (_requireVerifiedWallpaper || _liveWallpaper != 0)
			{
				return Array.Empty<SceneWindow>();
			}
			return new SceneWindow[1]
			{
				new SceneWindow(shell, new ScreenRect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top))
			};
		}
		if (!EnumWindows((nint hwnd, nint _) =>
		{
			GetWindowThreadProcessId(hwnd, out var pid);
			if (pid == 0 || pid == Environment.ProcessId || excluded.Contains((int)pid) || !IsWindowVisible(hwnd) || IsIconic(hwnd))
			{
				return true;
			}
			if (DwmGetWindowAttribute(hwnd, 14, out int value, 4) >= 0 && value != 0)
			{
				return true;
			}
			if (_requireVerifiedWallpaper && (hwnd == shell || IsDesktopHost(hwnd)))
			{
				return true;
			}
			if (DwmGetWindowAttribute(hwnd, 9, out Rect value2, Marshal.SizeOf<Rect>()) < 0 && !GetWindowRect(hwnd, out value2))
			{
				return true;
			}
			if (value2.Right <= value2.Left || value2.Bottom <= value2.Top || value2.Left >= _region.X + _region.Width || value2.Top >= _region.Y + _region.Height || value2.Right <= _region.X || value2.Bottom <= _region.Y)
			{
				return true;
			}
			if (_requireVerifiedWallpaper)
			{
				string text = CompatibleWindowVisibility.ExclusionReason(hwnd);
				if (text != null)
				{
					if (_excludedOverlays.Count == 64)
					{
						_excludedOverlays.Clear();
					}
					if (_excludedOverlays.Add(hwnd))
					{
						StringBuilder stringBuilder = new StringBuilder(128);
						GetClassNameW(hwnd, stringBuilder, stringBuilder.Capacity);
						Action<CaptureTrace>? trace = _trace;
						if (trace != null)
						{
							long timestamp = Stopwatch.GetTimestamp();
							long sourceTicks = 0L;
							long endTicks = 0L;
							long barrierTicks = 0L;
							long generation = 0L;
							long windowHandle = ((IntPtr)hwnd).ToInt64();
							uint windowProcess = pid;
							string windowClass = stringBuilder.ToString();
							int width = value2.Right - value2.Left;
							int height = value2.Bottom - value2.Top;
							trace(new CaptureTrace("SceneWindowExcluded", text, timestamp, sourceTicks, endTicks, barrierTicks, generation, 0, windowHandle, windowProcess, windowClass, 0, 0, null, null, null, null, null, 0, width, height));
						}
					}
					return true;
				}
			}
			windows.Add(new SceneWindow(hwnd, new ScreenRect(value2.Left, value2.Top, value2.Right - value2.Left, value2.Bottom - value2.Top)));
			return true;
		}, 0))
		{
			throw new Win32Exception(Marshal.GetLastWin32Error());
		}
		windows.Reverse();
		return windows.ToArray();
	}

	private nint FindLiveWallpaper(out nint host)
	{
		nint best = 0;
		nint bestHost = 0;
		long bestArea = 0L;
		long minimumArea = (long)_region.Width * (long)_region.Height / 4;
		int[] excluded = _excludedProcesses?.Invoke() ?? Array.Empty<int>();
		EnumWindows((nint hwnd, nint _) =>
		{
			if (!IsDesktopHost(hwnd))
			{
				return true;
			}
			if (!IsWindowVisible(hwnd) || IsIconic(hwnd))
			{
				return true;
			}
			GetWindowThreadProcessId(hwnd, out var hostPid);
			if (hostPid == 0)
			{
				return true;
			}
			EnumWindowProc callback = (nint child, nint num4) =>
			{
				GetWindowThreadProcessId(child, out var pid);
				if (pid == 0 || pid == hostPid || pid == Environment.ProcessId || excluded.Contains((int)pid) || !IsWindowVisible(child))
				{
					return true;
				}
				if (DwmGetWindowAttribute(child, 9, out Rect value, Marshal.SizeOf<Rect>()) < 0 && !GetWindowRect(child, out value))
				{
					return true;
				}
				int num = Math.Min(value.Right, _region.X + _region.Width) - Math.Max(value.Left, _region.X);
				int num2 = Math.Min(value.Bottom, _region.Y + _region.Height) - Math.Max(value.Top, _region.Y);
				if (num <= 0 || num2 <= 0)
				{
					return true;
				}
				long num3 = (long)num * (long)num2;
				if (num3 < minimumArea || num3 <= bestArea)
				{
					return true;
				}
				bestArea = num3;
				best = child;
				bestHost = hwnd;
				return true;
			};
			EnumChildWindows(hwnd, callback, 0);
			return true;
		}, 0);
		host = bestHost;
		return best;
	}

	private void DrawLiveWallpaper()
	{
		if (_requireVerifiedWallpaper)
		{
			byte[] pixels = _wallpaper.Pixels;
			if (pixels != null)
			{
				_backgroundPixels = pixels;
			}
		}
		if (_liveWallpaper == 0 || (_surface == null && _nativeWallpaper == null) || _capture == null || !GetWindowRect(_liveWallpaper, out var rect))
		{
			return;
		}
		int num = rect.Right - rect.Left;
		int num2 = rect.Bottom - rect.Top;
		if (num <= 0 || num2 <= 0)
		{
			return;
		}
		if (_liveSurface == null || _liveWidth != num || _liveHeight != num2)
		{
			_liveSurface?.Dispose();
			_liveSurface = new LiveWallpaperSurface(num, num2, _region.Width, _region.Height);
			_liveWidth = num;
			_liveHeight = num2;
			_livePixels = new byte[_region.Width * _region.Height * 4];
			if (_surface != null)
			{
				_liveBitmap = new WriteableBitmap(_region.Width, _region.Height, 96.0, 96.0, PixelFormats.Bgra32, null);
			}
		}
		if (_liveSurface == null || _livePixels == null)
		{
			return;
		}
		byte[] pixels2 = _wallpaper.Pixels;
		if (pixels2 != null && pixels2.Length == _livePixels.Length)
		{
			Buffer.BlockCopy(pixels2, 0, _livePixels, 0, pixels2.Length);
		}
		if (!_liveSurface.TryDraw(_liveWallpaper, _region.X, _region.Y, _region.Width, _region.Height, rect.Left, rect.Top, rect.Right, rect.Bottom, _livePixels))
		{
			return;
		}
		if (_nativeWallpaper != null)
		{
			_backgroundPixels = _livePixels;
			_nativeWallpaper.Update(_livePixels);
		}
		else if (_surface != null && _liveBitmap != null)
		{
			_liveBitmap.WritePixels(new Int32Rect(0, 0, _region.Width, _region.Height), _livePixels, _region.Width * 4, 0);
			Matrix transformFromDevice = _surface.CompositionTarget.TransformFromDevice;
			using DrawingContext drawingContext = _background.RenderOpen();
			drawingContext.DrawImage(_liveBitmap, new System.Windows.Rect(0.0, 0.0, (double)_region.Width * transformFromDevice.M11, (double)_region.Height * transformFromDevice.M22));
		}
	}

	private static bool IsDesktopHost(nint hwnd)
	{
		StringBuilder stringBuilder = new StringBuilder(128);
		GetClassNameW(hwnd, stringBuilder, stringBuilder.Capacity);
		string text = stringBuilder.ToString();
		if (text == "Progman" || text == "WorkerW")
		{
			return true;
		}
		return false;
	}

	internal CapturePixels? TryRead(TimeSpan waitForFrame = default(TimeSpan), CancellationToken cancellation = default(CancellationToken), long sourceNotBeforeTicks = 0L, bool reusePixels = false)
	{
		UpdateExclusions();
		DrawLiveWallpaper();
		CapturePixels capturePixels = _capture.TryRead(Math.Max(_barrier, sourceNotBeforeTicks), waitForFrame, cancellation, reusePixels);
		if ((object)capturePixels != null && _backgroundComposer != null)
		{
			byte[] backgroundPixels = _backgroundPixels;
			if (backgroundPixels != null)
			{
				bool flag = TraceBackground(capturePixels, backgroundPixels);
				_backgroundComposer.Apply(capturePixels.Pixels, backgroundPixels);
				if (flag)
				{
					TracePixelStatistics(capturePixels, "AfterComposition");
				}
			}
		}
		if (!UpdateExclusions())
		{
			return capturePixels;
		}
		return null;
	}

	private bool TraceBackground(CapturePixels frame, byte[] background)
	{
		if (_trace == null || Stopwatch.GetTimestamp() < _nextBackgroundTraceTicks)
		{
			return false;
		}
		_nextBackgroundTraceTicks = Stopwatch.GetTimestamp() + Stopwatch.Frequency;
		DocumentedDesktopSceneCapture documentedDesktopSceneCapture = this;
		CapturePixels frame2 = new CapturePixels(frame.Width, frame.Height, background);
		string reason;
		if (_liveWallpaper != 0)
		{
			reason = "LivePixels";
		}
		else
		{
			reason = ((_wallpaper.Pixels == null) ? "SystemColorFallback" : _wallpaper.SourceKind);
		}
		documentedDesktopSceneCapture.TracePixelStatistics(frame2, reason);
		TracePixelStatistics(frame, "CarrierBeforeComposition");
		return true;
	}

	private void TracePixelStatistics(CapturePixels frame, string reason)
	{
		NativeCaptureDiagnostics nativeCaptureDiagnostics = new NativeCaptureDiagnostics();
		nativeCaptureDiagnostics.ObservePixels(frame, 0L);
		CaptureTrace[] array = nativeCaptureDiagnostics.Drain();
		foreach (CaptureTrace captureTrace in array)
		{
			_trace?.Invoke(captureTrace with
			{
				Stage = "DesktopBackground",
				Reason = reason
			});
		}
	}

	internal void ReleaseReadbackBuffers()
	{
		_capture?.ReleaseReadbackBuffers();
	}

	bool IDesktopSceneCapture.UpdateExclusions()
	{
		return UpdateExclusions();
	}

	CapturePixels? IDesktopSceneCapture.TryRead(TimeSpan wait, CancellationToken cancellation, long notBefore, bool reuse)
	{
		return TryRead(wait, cancellation, notBefore, reuse);
	}

	void IDesktopSceneCapture.ReleaseReadbackBuffers()
	{
		ReleaseReadbackBuffers();
	}

	public void Dispose()
	{
		_capture?.Dispose();
		_capture = null;
		_liveSurface?.Dispose();
		_liveSurface = null;
		_liveBitmap = null;
		_livePixels = null;
		foreach (nint thumbnail in _thumbnails)
		{
			DwmUnregisterThumbnail(thumbnail);
		}
		_thumbnails.Clear();
		_surface?.Dispose();
		_surface = null;
		_nativeWallpaper?.Dispose();
		_nativeWallpaper = null;
		if (_carrier != 0)
		{
			DestroyWindow(_carrier);
			_carrier = 0;
		}
		if (_owner != 0)
		{
			DestroyWindow(_owner);
			_owner = 0;
		}
	}

	[DllImport("user32.dll", SetLastError = true)]
	private static extern nint CreateWindowExW(uint extendedStyle, [MarshalAs(UnmanagedType.LPWStr)] string className, [MarshalAs(UnmanagedType.LPWStr)] string name, uint style, int x, int y, int width, int height, nint parent, nint menu, nint instance, nint param);

	[DllImport("user32.dll")]
	private static extern bool DestroyWindow(nint hwnd);

	[DllImport("user32.dll")]
	private static extern int GetSystemMetrics(int index);

	[DllImport("user32.dll")]
	private static extern uint GetSysColor(int index);

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(nint hwnd, int command);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool SetWindowPos(nint hwnd, nint after, int x, int y, int width, int height, uint flags);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern nint SetWindowLongPtrW(nint hwnd, int index, nint value);

	[DllImport("user32.dll")]
	private static extern nint GetWindowLongPtrW(nint hwnd, int index);

	[DllImport("user32.dll")]
	private static extern nint GetWindow(nint hwnd, uint command);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool SetLayeredWindowAttributes(nint hwnd, uint color, byte alpha, uint flags);

	[DllImport("user32.dll")]
	private static extern bool EnumWindows(EnumWindowProc callback, nint parameter);

	[DllImport("user32.dll")]
	private static extern bool EnumChildWindows(nint hwnd, EnumWindowProc callback, nint parameter);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int GetClassNameW(nint hwnd, StringBuilder name, int count);

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(nint hwnd, out Rect rect);

	[DllImport("user32.dll")]
	private static extern bool IsWindow(nint hwnd);

	[DllImport("user32.dll")]
	private static extern bool IsWindowVisible(nint hwnd);

	[DllImport("user32.dll")]
	private static extern bool IsIconic(nint hwnd);

	[DllImport("user32.dll")]
	private static extern nint GetShellWindow();

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(nint hwnd, out uint pid);

	[DllImport("dwmapi.dll")]
	private static extern int DwmRegisterThumbnail(nint destination, nint source, out nint thumbnail);

	[DllImport("dwmapi.dll")]
	private static extern int DwmUnregisterThumbnail(nint thumbnail);

	[DllImport("dwmapi.dll")]
	private static extern int DwmUpdateThumbnailProperties(nint thumbnail, ref ThumbnailProperties properties);

	[DllImport("dwmapi.dll")]
	private static extern int DwmQueryThumbnailSourceSize(nint thumbnail, out Size size);

	[DllImport("dwmapi.dll")]
	private static extern int DwmFlush();

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int size);

	[DllImport("dwmapi.dll")]
	private static extern int DwmGetWindowAttribute(nint hwnd, int attribute, out int value, int size);

	[DllImport("dwmapi.dll")]
	private static extern int DwmGetWindowAttribute(nint hwnd, int attribute, out Rect value, int size);
}
