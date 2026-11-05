using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using GlassDesk.Platform;

internal sealed class SharedDesktopSceneCapture : IDesktopSceneCapture, IDisposable
{
	private struct Rect(int left, int top, int right, int bottom)
	{
		public int Left = left;

		public int Top = top;

		public int Right = right;

		public int Bottom = bottom;
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

	private readonly record struct SceneWindow(nint Handle, uint ProcessId, int Left, int Top, int Right, int Bottom, bool Visible, bool Maximized);

	private struct Size
	{
		public int Width;

		public int Height;
	}

	private struct CompositionAttribute
	{
		public int Attribute;

		public nint Data;

		public nuint Size;
	}

	private delegate bool EnumWindowProc(nint hwnd, nint parameter);

	private nint library;

	private nint device;

	private nint context;

	private nint dxgi;

	private nint composition;

	private nint target;

	private nint visual;

	private nint thumbnail;

	private nint carrier;

	private nint carrierOwner;

	private nint _rootVisual;

	private nint _desktopVisual;

	private nint _desktopThumbnail;

	private nint _shellWindow;

	private PersistentWindowCapture? _capture;

	private readonly ScreenRect _region;

	private nint[]? _lastExcluded;

	private SceneWindow[]? _lastMembers;

	private long _exclusionBarrier;

	private long _sceneRevision;

	private long _motionBarrier;

	private long _exclusionGeneration;

	private readonly Action<CaptureTrace>? _trace;

	private readonly int _excludedParentProcess;

	private readonly Func<int[]>? _excludedProcesses;

	private readonly bool _desktopOnly;

	private readonly Func<nint> _desktopWindow;

	internal nint CarrierHandle => carrier;

	nint IDesktopSceneCapture.CarrierHandle => CarrierHandle;

	public long SceneRevision => _sceneRevision;

	internal bool DiagnosticFreezeExclusions { get; set; }

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

	internal void DiagnosticRecommitExclusions(bool commit = true)
	{
		ApplyExclusions(_lastExcluded, commit);
	}

	internal unsafe void DiagnosticCommitOnly()
	{
		Check(((delegate* unmanaged[Stdcall]<nint, int>)(*(IntPtr*)((nint)(*(IntPtr*)composition) + (nint)3 * (nint)sizeof(nint))))(composition));
		_trace?.Invoke(new CaptureTrace("Composition", "CommitSubmitted", Stopwatch.GetTimestamp(), 0L, 0L, 0L, 0L, 0, 0L));
	}

	internal void DiagnosticReplaceVisual()
	{
		(nint[], SceneWindow[]) tuple = WindowSnapshot();
		ReplaceVisual(tuple.Item1, tuple.Item2);
	}

	private unsafe void ReplaceVisual(nint[] excluded, SceneWindow[] members)
	{
		nint pointer = 0;
		nint num = 0;
		delegate* unmanaged[Stdcall]<nint, nint, nint*, nint*, int> procAddress = (delegate* unmanaged[Stdcall]<nint, nint, nint*, nint*, int>)GetProcAddress(library, 163);
		try
		{
			Check(procAddress(carrier, composition, &pointer, &num));
			_trace?.Invoke(new CaptureTrace("VisualSwap", (num == thumbnail) ? "SameThumbnail" : "NewThumbnail", Stopwatch.GetTimestamp(), 0L, 0L, 0L, 0L, 0, 0L));
			_trace?.Invoke(new CaptureTrace("VisualIdentity", (pointer == visual) ? "Same" : "New", Stopwatch.GetTimestamp(), 0L, 0L, 0L, 0L, 0, 0L));
			SetNativeExclusions(num, excluded);
			DiagnosticCommitOnly();
			AddChild(pointer, above: true, visual);
			Check(((delegate* unmanaged[Stdcall]<nint, nint, int>)(*(IntPtr*)((nint)(*(IntPtr*)_rootVisual) + (nint)17 * (nint)sizeof(nint))))(_rootVisual, visual));
			DiagnosticCommitOnly();
			Check(DwmFlush());
			_trace?.Invoke(new CaptureTrace("Composition", "DwmFlushReturned", Stopwatch.GetTimestamp(), 0L, 0L, 0L, 0L, 0, 0L));
			DwmUnregisterThumbnail(thumbnail);
			Release(ref visual);
			thumbnail = num;
			num = 0;
			visual = pointer;
			pointer = 0;
			RecordExclusions(excluded);
			_capture?.RefreshSessionAtSceneBoundary();
			_lastMembers = members;
		}
		catch
		{
			((delegate* unmanaged[Stdcall]<nint, int>)(*(IntPtr*)((nint)(*(IntPtr*)_rootVisual) + (nint)18 * (nint)sizeof(nint))))(_rootVisual);
			if (_desktopVisual != 0)
			{
				AddChild(_desktopVisual, above: false, 0);
			}
			AddChild(visual, above: true, _desktopVisual);
			DiagnosticCommitOnly();
			throw;
		}
		finally
		{
			if (num != 0)
			{
				DwmUnregisterThumbnail(num);
			}
			Release(ref pointer);
		}
	}

	internal static CapturePixels? Capture(ScreenRect region, TimeSpan timeout, Action<CaptureTrace>? trace = null)
	{
		using SharedDesktopSceneCapture sharedDesktopSceneCapture = new SharedDesktopSceneCapture(region, diagnosticOnScreen: false, trace);
		Stopwatch stopwatch = Stopwatch.StartNew();
		while (stopwatch.Elapsed < timeout)
		{
			CapturePixels capturePixels = sharedDesktopSceneCapture.TryRead(default, default, 0L);
			if ((object)capturePixels != null)
			{
				return capturePixels;
			}
			Program.Pump(TimeSpan.FromMilliseconds(10L));
		}
		return null;
	}

	internal SharedDesktopSceneCapture(ScreenRect region, bool diagnosticOnScreen = false, Action<CaptureTrace>? trace = null, int isolatedParentProcess = 0, Func<int[]>? excludedProcesses = null)
		: this(region, diagnosticOnScreen, trace, isolatedParentProcess, excludedProcesses, desktopOnly: false)
	{
	}

	internal unsafe SharedDesktopSceneCapture(ScreenRect region, bool diagnosticOnScreen, Action<CaptureTrace>? trace, int isolatedParentProcess, Func<int[]>? excludedProcesses, bool desktopOnly, Func<nint>? desktopWindow = null)
	{
		_trace = trace;
		_excludedParentProcess = isolatedParentProcess;
		_excludedProcesses = excludedProcesses;
		_desktopOnly = desktopOnly;
		_desktopWindow = desktopWindow ?? new Func<nint>(GetShellWindow);
		if (Environment.OSVersion.Version.Build != 26100 && isolatedParentProcess <= 0)
		{
			throw new NotSupportedException("Private ABI probe is restricted to the inspected local OS build.");
		}
		if (WindowsCapabilities.BuildNumber < 22000)
		{
			throw new PlatformNotSupportedException("Windows 11 is required.");
		}
		if (region.Width <= 0 || region.Height <= 0 || (long)region.Width * (long)region.Height > 32000000)
		{
			throw new ArgumentOutOfRangeException("region");
		}
		_region = region;
		try
		{
			library = NativeLibrary.Load("dwmapi.dll");
			nint procAddress = GetProcAddress(library, 163);
			nint procAddress2 = GetProcAddress(library, 164);
			if (procAddress == 0 || procAddress2 == 0)
			{
				throw new NotSupportedException("Private shared visual ordinals unavailable.");
			}
			delegate* unmanaged[Stdcall]<nint, nint, nint*, nint*, int> obj = (delegate* unmanaged[Stdcall]<nint, nint, nint*, nint*, int>)procAddress;
			Check(D3D11CreateDevice(0, 1u, 0, 32u, 0, 0u, 7u, out device, out var _, out context));
			Guid iid = new Guid("54ec77fa-1377-44e6-8c32-88fd5f44c84c");
			Check(Marshal.QueryInterface(device, in iid, out dxgi));
			Guid iid2 = new Guid("5F4633FE-1E08-4CB8-8C75-CE24333F5602");
			Check(DCompositionCreateDevice3(dxgi, in iid2, out composition));
			carrierOwner = CreateWindowExW(0u, "STATIC", "GlassDesk capture owner", 2147483648u, 0, 0, 1, 1, 0, 0, 0, 0);
			if (carrierOwner == 0)
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
			carrier = CreateWindowExW(2097160u, "STATIC", "GlassDesk controlled shared desktop carrier", 2147483648u, diagnosticOnScreen ? region.X : (GetSystemMetrics(76) - region.Width - 64), diagnosticOnScreen ? region.Y : GetSystemMetrics(77), region.Width, region.Height, carrierOwner, 0, 0, 0);
			if (carrier == 0)
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
			int num = 1;
			CompositionAttribute data = new CompositionAttribute
			{
				Attribute = 13,
				Data = (nint)(&num),
				Size = 4u
			};
			if (!SetWindowCompositionAttribute(carrier, ref data))
			{
				throw new InvalidOperationException("Carrier live-preview exclusion failed.");
			}
			nint num2 = 0;
			nint num3 = 0;
			Check(obj(carrier, composition, &num2, &num3));
			visual = num2;
			thumbnail = num3;
			UpdateExclusions();
			CreateDesktopPlane();
			nint rootVisual = 0;
			Check(((delegate* unmanaged[Stdcall]<nint, nint*, int>)(*(IntPtr*)((nint)(*(IntPtr*)composition) + (nint)6 * (nint)sizeof(nint))))(composition, &rootVisual));
			_rootVisual = rootVisual;
			if (_desktopVisual != 0)
			{
				AddChild(_desktopVisual, above: false, 0);
			}
			if (visual != 0)
			{
				AddChild(visual, above: true, _desktopVisual);
			}
			nint num4 = 0;
			Check(((delegate* unmanaged[Stdcall]<nint, nint, int, nint*, int>)(*(IntPtr*)((nint)(*(IntPtr*)composition) + (nint)24 * (nint)sizeof(nint))))(composition, carrier, 0, &num4));
			target = num4;
			Check(((delegate* unmanaged[Stdcall]<nint, nint, int>)(*(IntPtr*)((nint)(*(IntPtr*)target) + (nint)3 * (nint)sizeof(nint))))(target, _rootVisual));
			Check(((delegate* unmanaged[Stdcall]<nint, int>)(*(IntPtr*)((nint)(*(IntPtr*)composition) + (nint)3 * (nint)sizeof(nint))))(composition));
			ShowWindow(carrier, 4);
			Check(DwmFlush());
			_capture = new PersistentWindowCapture(carrier, trace, device, context);
			ShowWindow(carrier, 0);
			if (SetWindowLongPtrW(carrier, -20, new IntPtr(136314888)) == 0)
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
			ShowWindow(carrier, 4);
		}
		catch
		{
			Dispose();
			throw;
		}
	}

	private unsafe void AddChild(nint child, bool above, nint reference)
	{
		Check(((delegate* unmanaged[Stdcall]<nint, nint, int, nint, int>)(*(IntPtr*)((nint)(*(IntPtr*)_rootVisual) + (nint)16 * (nint)sizeof(nint))))(_rootVisual, child, above ? 1 : 0, reference));
	}

	private unsafe void CreateDesktopPlane()
	{
		nint num = _desktopWindow();
		if (num == 0)
		{
			throw new InvalidOperationException("Desktop shell is temporarily unavailable.");
		}
		ScreenRect screenRect = WgcWindowCapture.ReadScreenBounds(num);
		ThumbnailProperties thumbnailProperties = new ThumbnailProperties
		{
			Flags = 67108895u,
			Destination = new Rect(0, 0, _region.Width, _region.Height),
			Source = new Rect(_region.X - screenRect.X, _region.Y - screenRect.Y, _region.X - screenRect.X + _region.Width, _region.Y - screenRect.Y + _region.Height),
			Opacity = byte.MaxValue,
			Visible = 1,
			ClientOnly = 0
		};
		delegate* unmanaged[Stdcall]<nint, nint, uint, ThumbnailProperties*, nint, nint*, nint*, int> procAddress = (delegate* unmanaged[Stdcall]<nint, nint, uint, ThumbnailProperties*, nint, nint*, nint*, int>)GetProcAddress(library, 147);
		if (procAddress == (delegate* unmanaged[Stdcall]<nint, nint, uint, ThumbnailProperties*, nint, nint*, nint*, int>)null)
		{
			throw new NotSupportedException("Shared desktop-plane visual unavailable.");
		}
		nint desktopVisual = 0;
		nint desktopThumbnail = 0;
		Check(procAddress(carrier, num, 2u, &thumbnailProperties, composition, &desktopVisual, &desktopThumbnail));
		_desktopVisual = desktopVisual;
		_desktopThumbnail = desktopThumbnail;
		_shellWindow = num;
	}

	internal CapturePixels? TryRead(TimeSpan waitForFrame = default(TimeSpan), CancellationToken cancellation = default(CancellationToken), long sourceNotBeforeTicks = 0L, bool reusePixels = false)
	{
		UpdateExclusions();
		long exclusionBarrier = _exclusionBarrier;
		CapturePixels capturePixels = _capture.TryRead(Math.Max(Math.Max(exclusionBarrier, _motionBarrier), sourceNotBeforeTicks), waitForFrame, cancellation, reusePixels);
		bool flag = UpdateExclusions() || exclusionBarrier != _exclusionBarrier;
		_trace?.Invoke(new CaptureTrace("Scene", flag ? "SceneWindowsChanged" : (((object)capturePixels == null) ? "Unavailable" : "Delivered"), Stopwatch.GetTimestamp(), capturePixels?.SourceQpcTicks ?? 0, 0L, _exclusionBarrier, _exclusionGeneration, 0, 0L));
		if (!flag)
		{
			return capturePixels;
		}
		return null;
	}

	internal bool UpdateExclusions()
	{
		if (_desktopThumbnail != 0 && _desktopWindow() != _shellWindow)
		{
			throw new InvalidOperationException("Desktop shell changed; the capture scene must be recreated.");
		}
		if (DiagnosticFreezeExclusions)
		{
			return false;
		}
		long timestamp = Stopwatch.GetTimestamp();
		(nint[], SceneWindow[]) tuple = WindowSnapshot();
		var (array, _) = tuple;
		_trace?.Invoke(new CaptureTrace("SceneSnapshot", "Completed", Stopwatch.GetTimestamp(), 0L, timestamp, 0L, 0L, 0, 0L));
		if (_lastExcluded != null && ((ReadOnlySpan<nint>)_lastExcluded.AsSpan()).SequenceEqual<nint>((ReadOnlySpan<nint>)array) && _lastMembers != null && ((ReadOnlySpan<SceneWindow>)_lastMembers.AsSpan()).SequenceEqual((ReadOnlySpan<SceneWindow>)tuple.Item2))
		{
			return false;
		}
		if (_lastExcluded != null && ((ReadOnlySpan<nint>)_lastExcluded.AsSpan()).SequenceEqual<nint>((ReadOnlySpan<nint>)array) && _lastMembers != null && CanFollowLiveWindows(_lastMembers, tuple.Item2))
		{
			_lastMembers = tuple.Item2;
			_motionBarrier = Stopwatch.GetTimestamp();
			_capture?.RetireQueuedFramesAtSceneBoundary(_motionBarrier);
			_trace?.Invoke(new CaptureTrace("SceneMotion", "Bounds", _motionBarrier, 0L, 0L, _motionBarrier, 0L, 0, 0L));
			return false;
		}
		if (_trace != null && _lastExcluded != null)
		{
			string reason = "OwnHandles";
			SceneWindow sceneWindow = default;
			if (((ReadOnlySpan<nint>)_lastExcluded.AsSpan()).SequenceEqual<nint>((ReadOnlySpan<nint>)array) && _lastMembers != null)
			{
				reason = "ExternalSet";
				int i;
				for (i = 0; i < Math.Min(_lastMembers.Length, tuple.Item2.Length) && _lastMembers[i] == tuple.Item2[i]; i++)
				{
				}
				if (i < tuple.Item2.Length)
				{
					sceneWindow = tuple.Item2[i];
				}
				if (i < _lastMembers.Length && i < tuple.Item2.Length && _lastMembers[i].Handle == sceneWindow.Handle)
				{
					reason = ((_lastMembers[i].Visible == sceneWindow.Visible) ? "ExternalBounds" : "ExternalVisibility");
				}
			}
			StringBuilder stringBuilder = new StringBuilder(128);
			if (sceneWindow.Handle != 0)
			{
				GetClassName(sceneWindow.Handle, stringBuilder, stringBuilder.Capacity);
			}
			_trace(new CaptureTrace("SceneChange", reason, Stopwatch.GetTimestamp(), 0L, 0L, 0L, 0L, 0, ((IntPtr)sceneWindow.Handle).ToInt64(), sceneWindow.ProcessId, stringBuilder.ToString()));
		}
		bool num = _lastMembers == null || !CanFollowLiveWindows(_lastMembers, tuple.Item2);
		if (target != 0)
		{
			ReplaceVisual(array, tuple.Item2);
		}
		else
		{
			ApplyExclusions(array);
			_lastMembers = tuple.Item2;
		}
		if (num)
		{
			_sceneRevision = _exclusionBarrier;
		}
		return num;
	}

	internal void ReleaseReadbackBuffers()
	{
		_capture?.ReleaseReadbackBuffers();
	}

	private static bool CanFollowLiveWindows(SceneWindow[] previous, SceneWindow[] current)
	{
		if (previous.Length != current.Length)
		{
			return false;
		}
		int i = 0;
		for (int j = 0; j < current.Length; j++)
		{
			SceneWindow sceneWindow;
			for (sceneWindow = current[j]; i < previous.Length && (previous[i].Handle != sceneWindow.Handle || previous[i].ProcessId != sceneWindow.ProcessId || previous[i].Visible != sceneWindow.Visible); i++)
			{
			}
			if (i == previous.Length)
			{
				return false;
			}
			if (previous[i].Maximized != sceneWindow.Maximized)
			{
				return false;
			}
			i++;
		}
		return true;
	}

	private (nint[] Excluded, SceneWindow[] Members) WindowSnapshot()
	{
		List<nint> exclusions = new List<nint>();
		List<SceneWindow> members = new List<SceneWindow>();
		int[] family = _excludedProcesses?.Invoke() ?? Array.Empty<int>();
		nint desktop = (_desktopOnly ? _desktopWindow() : 0);
		bool includeDesktopHosts = _desktopOnly && desktop == GetShellWindow();
		if (!EnumWindows((nint hwnd, nint _) =>
		{
			GetWindowThreadProcessId(hwnd, out var processId);
			int value;
			Rect rect;
			if (processId == Environment.ProcessId || processId == _excludedParentProcess || family.Contains((int)processId) || (_desktopOnly && hwnd != desktop && (!includeDesktopHosts || !IsDesktopHost(hwnd))))
			{
				exclusions.Add(hwnd);
			}
			else if (processId != 0 && IsWindowVisible(hwnd) && !IsIconic(hwnd) && (DwmGetWindowAttribute(hwnd, 14, out value, 4) < 0 || value == 0) && GetWindowRect(hwnd, out rect) && rect.Right > rect.Left && rect.Bottom > rect.Top && rect.Left < _region.X + _region.Width && rect.Top < _region.Y + _region.Height && rect.Right > _region.X && rect.Bottom > _region.Y)
			{
				members.Add(new SceneWindow(hwnd, processId, rect.Left, rect.Top, rect.Right, rect.Bottom, Visible: true, IsZoomed(hwnd)));
			}
			return true;
		}, 0))
		{
			throw new InvalidOperationException("Own-process HWND enumeration failed.");
		}
		return (Excluded: exclusions.Order().ToArray(), Members: members.ToArray());
	}

	private static bool IsDesktopHost(nint window)
	{
		StringBuilder stringBuilder = new StringBuilder(128);
		GetClassName(window, stringBuilder, stringBuilder.Capacity);
		string text = stringBuilder.ToString();
		if (text == "Progman" || text == "WorkerW")
		{
			return true;
		}
		return false;
	}

	private void ApplyExclusions(nint[] excluded, bool commit = true)
	{
		SetNativeExclusions(thumbnail, excluded);
		if (commit)
		{
			DiagnosticCommitOnly();
		}
		RecordExclusions(excluded);
	}

	private unsafe void SetNativeExclusions(nint thumbnailHandle, nint[] excluded)
	{
		delegate* unmanaged[Stdcall]<nint, nint*, uint, nint*, uint, Rect*, Size*, uint, int> procAddress = (delegate* unmanaged[Stdcall]<nint, nint*, uint, nint*, uint, Rect*, Size*, uint, int>)GetProcAddress(library, 164);
		Rect rect = new Rect
		{
			Left = _region.X,
			Top = _region.Y,
			Right = _region.X + _region.Width,
			Bottom = _region.Y + _region.Height
		};
		Size size = new Size
		{
			Width = _region.Width,
			Height = _region.Height
		};
		fixed (nint* ptr = excluded)
		{
			Check(procAddress(thumbnailHandle, null, 0u, ptr, (uint)excluded.Length, &rect, &size, 1u));
		}
	}

	private void RecordExclusions(nint[] excluded)
	{
		_motionBarrier = 0L;
		_exclusionBarrier = Stopwatch.GetTimestamp();
		_exclusionGeneration++;
		_lastExcluded = excluded;
		_trace?.Invoke(new CaptureTrace("Exclusions", "Changed", _exclusionBarrier, 0L, 0L, _exclusionBarrier, _exclusionGeneration, 0, 0L));
		_capture?.RetireQueuedFramesAtSceneBoundary(_exclusionBarrier);
	}

	public unsafe void Dispose()
	{
		_capture?.Dispose();
		_capture = null;
		if (target != 0)
		{
			((delegate* unmanaged[Stdcall]<nint, nint, int>)(*(IntPtr*)((nint)(*(IntPtr*)target) + (nint)3 * (nint)sizeof(nint))))(target, 0);
		}
		if (thumbnail != 0)
		{
			DwmUnregisterThumbnail(thumbnail);
			thumbnail = 0;
		}
		if (_desktopThumbnail != 0)
		{
			DwmUnregisterThumbnail(_desktopThumbnail);
			_desktopThumbnail = 0;
		}
		Release(ref visual);
		Release(ref _desktopVisual);
		Release(ref _rootVisual);
		Release(ref target);
		Release(ref composition);
		if (carrier != 0)
		{
			DestroyWindow(carrier);
			carrier = 0;
		}
		if (carrierOwner != 0)
		{
			DestroyWindow(carrierOwner);
			carrierOwner = 0;
		}
		Release(ref dxgi);
		Release(ref context);
		Release(ref device);
		if (library != 0)
		{
			NativeLibrary.Free(library);
			library = 0;
		}
	}

	private static void Check(int hr)
	{
		Marshal.ThrowExceptionForHR(hr);
	}

	private static void Release(ref nint pointer)
	{
		if (pointer != 0)
		{
			Marshal.Release(pointer);
		}
		pointer = 0;
	}

	[DllImport("kernel32.dll", ExactSpelling = true)]
	private static extern nint GetProcAddress(nint module, nint ordinal);

	[DllImport("d3d11.dll")]
	private static extern int D3D11CreateDevice(nint adapter, uint type, nint software, uint flags, nint levels, uint count, uint sdk, out nint device, out uint level, out nint context);

	[DllImport("dcomp.dll")]
	private static extern int DCompositionCreateDevice3(nint device, in Guid iid, out nint composition);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern nint CreateWindowExW(uint extendedStyle, [MarshalAs(UnmanagedType.LPWStr)] string className, [MarshalAs(UnmanagedType.LPWStr)] string name, uint style, int x, int y, int width, int height, nint parent, nint menu, nint instance, nint param);

	[DllImport("user32.dll")]
	private static extern bool DestroyWindow(nint hwnd);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern nint SetWindowLongPtrW(nint hwnd, int index, nint value);

	[DllImport("user32.dll")]
	private static extern nint GetShellWindow();

	[DllImport("user32.dll")]
	private static extern bool SetWindowCompositionAttribute(nint hwnd, ref CompositionAttribute data);

	[DllImport("user32.dll")]
	private static extern int GetSystemMetrics(int index);

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(nint hwnd, int command);

	[DllImport("user32.dll")]
	private static extern bool EnumWindows(EnumWindowProc callback, nint parameter);

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(nint hwnd, out Rect rect);

	[DllImport("user32.dll")]
	private static extern bool IsWindowVisible(nint hwnd);

	[DllImport("user32.dll")]
	private static extern bool IsIconic(nint hwnd);

	[DllImport("user32.dll")]
	private static extern bool IsZoomed(nint hwnd);

	[DllImport("dwmapi.dll")]
	private static extern int DwmGetWindowAttribute(nint hwnd, int attribute, out int value, int size);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int GetClassName(nint hwnd, StringBuilder text, int capacity);

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(nint hwnd, out uint processId);

	[DllImport("dwmapi.dll")]
	private static extern int DwmFlush();

	[DllImport("dwmapi.dll")]
	private static extern int DwmUnregisterThumbnail(nint thumbnail);
}
