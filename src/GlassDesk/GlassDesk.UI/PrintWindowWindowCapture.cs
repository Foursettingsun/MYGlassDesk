using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using GlassDesk.UI.CaptureCompatibility;

internal sealed class PrintWindowWindowCapture : IWindowCapture, IDisposable
{
	private struct BitmapInfoHeader
	{
		public uint Size;

		public int Width;

		public int Height;

		public ushort Planes;

		public ushort BitCount;

		public uint Compression;

		public uint SizeImage;

		public int XPelsPerMeter;

		public int YPelsPerMeter;

		public uint ClrUsed;

		public uint ClrImportant;
	}

	private struct BitmapInfo
	{
		public BitmapInfoHeader Header;

		public uint Colors;
	}

	private const uint RenderFullContent = 2u;

	private const uint BiRgb = 0u;

	private const int DibRgbColors = 0;

	private static readonly TimeSpan MinimumFrameInterval = TimeSpan.FromMilliseconds(33.333333333333336);

	private static readonly TimeSpan LiveFrameInterval = TimeSpan.FromMilliseconds(8L);

	private static readonly TimeSpan MinimumFlushInterval = TimeSpan.FromMilliseconds(100L);

	private static readonly TimeSpan IdleFrameInterval = TimeSpan.FromMilliseconds(100L);

	private const int IdleFramesBeforeBackOff = 3;

	private readonly nint _window;

	private readonly int _width;

	private readonly int _height;

	private readonly Action<CaptureTrace>? _trace;

	private readonly object _gate = new object();

	private nint _memoryContext;

	private nint _bitmap;

	private nint _previousObject;

	private nint _bits;

	private byte[]? _reusablePixels;

	private long _lastCaptureTicks;

	private long _lastFlushTicks;

	private long _lastSignature;

	private int _idleFrames;

	private bool _disposed;

	internal PrintWindowWindowCapture(nint window, int width, int height, Action<CaptureTrace>? trace = null)
	{
		if (window == 0)
		{
			throw new ArgumentException("A scene carrier window is required.", "window");
		}
		if (width <= 0 || height <= 0 || (long)width * (long)height > 32000000)
		{
			throw new ArgumentOutOfRangeException("width", "The capture extent is invalid.");
		}
		_window = window;
		_width = width;
		_height = height;
		_trace = trace;
		try
		{
			CreateBackBuffer();
		}
		catch
		{
			Dispose();
			throw;
		}
	}

	public CapturePixels? TryRead(long notBeforeQpcTicks = 0L, TimeSpan waitForFrame = default(TimeSpan), CancellationToken cancellation = default(CancellationToken), bool reusePixels = false)
	{
		if (cancellation.IsCancellationRequested)
		{
			return null;
		}
		if (waitForFrame > TimeSpan.Zero)
		{
			WaitForNextSlot(waitForFrame);
		}
		lock (_gate)
		{
			if (_disposed || _memoryContext == 0 || _bitmap == 0)
			{
				return null;
			}
			Stopwatch.GetTimestamp();
			try
			{
				if (LiveBackdropMode.Enabled || Stopwatch.GetElapsedTime(_lastFlushTicks) >= MinimumFlushInterval)
				{
					Marshal.ThrowExceptionForHR(DwmFlush());
					_lastFlushTicks = Stopwatch.GetTimestamp();
				}
				if (!PrintWindow(_window, _memoryContext, 2u))
				{
					throw new Win32Exception(Marshal.GetLastWin32Error(), "PrintWindow(PW_RENDERFULLCONTENT) failed.");
				}
			}
			catch (Exception ex)
			{
				Action<CaptureTrace>? trace = _trace;
				if (trace != null)
				{
					long timestamp = Stopwatch.GetTimestamp();
					long sourceTicks = 0L;
					long endTicks = 0L;
					long barrierTicks = 0L;
					long generation = 0L;
					long windowHandle = 0L;
					string message = ex.Message;
					trace(new CaptureTrace("PrintWindow", "Failed", timestamp, sourceTicks, endTicks, barrierTicks, generation, 0, windowHandle, 0u, null, 0, 0, null, null, null, null, null, 0, 0, 0, -1.0, -1.0, -1.0, 0, message));
				}
				return null;
			}
			int num = checked(_width * _height * 4);
			byte[] array = ((reusePixels && _reusablePixels != null && _reusablePixels.Length == num) ? _reusablePixels : new byte[num]);
			if (reusePixels)
			{
				_reusablePixels = array;
			}
			Marshal.Copy(_bits, array, 0, num);
			MakeOpaque(array);
			TrackChange(array);
			_lastCaptureTicks = Stopwatch.GetTimestamp();
			_trace?.Invoke(new CaptureTrace("PrintWindow", "Delivered", _lastCaptureTicks, 0L, 0L, 0L, 0L, 0, 0L));
			return new CapturePixels(_width, _height, array)
			{
				SourceQpcTicks = _lastCaptureTicks
			};
		}
	}

	private void WaitForNextSlot(TimeSpan budget)
	{
		TimeSpan timeSpan;
		if (LiveBackdropMode.Enabled)
		{
			timeSpan = LiveFrameInterval;
		}
		else
		{
			timeSpan = ((_idleFrames >= 3) ? IdleFrameInterval : MinimumFrameInterval);
		}
		TimeSpan elapsedTime = Stopwatch.GetElapsedTime(_lastCaptureTicks);
		TimeSpan timeSpan2 = timeSpan - elapsedTime;
		if (!(timeSpan2 <= TimeSpan.Zero))
		{
			if (timeSpan2 > budget)
			{
				timeSpan2 = budget;
			}
			if (timeSpan2 > TimeSpan.Zero)
			{
				Thread.Sleep(timeSpan2);
			}
		}
	}

	private void TrackChange(byte[] pixels)
	{
		long num = Signature(pixels);
		if (num == _lastSignature)
		{
			if (_idleFrames < int.MaxValue)
			{
				_idleFrames++;
			}
		}
		else
		{
			_lastSignature = num;
			_idleFrames = 0;
		}
	}

	private static long Signature(byte[] pixels)
	{
		int num = Math.Max(64, (pixels.Length / 512) & -4);
		long num2 = 1469598103934665603L;
		for (int i = 0; i < pixels.Length; i += num)
		{
			num2 = (num2 ^ pixels[i]) * 1099511628211L;
		}
		return num2;
	}

	private static void MakeOpaque(byte[] pixels)
	{
		for (int i = 3; i < pixels.Length; i += 4)
		{
			pixels[i] = byte.MaxValue;
		}
	}

	private void CreateBackBuffer()
	{
		nint dC = GetDC(0);
		if (dC == 0)
		{
			throw new Win32Exception(Marshal.GetLastWin32Error(), "A screen device context is required.");
		}
		try
		{
			_memoryContext = CreateCompatibleDC(dC);
			if (_memoryContext == 0)
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
			BitmapInfoHeader header = new BitmapInfoHeader
			{
				Size = (uint)Marshal.SizeOf<BitmapInfoHeader>(),
				Width = _width,
				Height = -_height,
				Planes = 1,
				BitCount = 32,
				Compression = 0u
			};
			BitmapInfo info = new BitmapInfo
			{
				Header = header
			};
			_bitmap = CreateDIBSection(_memoryContext, ref info, 0u, out _bits, 0, 0u);
			if (_bitmap == 0 || _bits == 0)
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
			_previousObject = SelectObject(_memoryContext, _bitmap);
			if (_previousObject == 0)
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
		}
		finally
		{
			ReleaseDC(0, dC);
		}
	}

	public void RefreshSessionAtSceneBoundary()
	{
	}

	public void RetireQueuedFramesAtSceneBoundary(long barrierTicks)
	{
	}

	public void ReleaseReadbackBuffers()
	{
		lock (_gate)
		{
			_reusablePixels = null;
		}
	}

	public void Dispose()
	{
		lock (_gate)
		{
			if (!_disposed)
			{
				_disposed = true;
				_reusablePixels = null;
				if (_memoryContext != 0 && _previousObject != 0)
				{
					SelectObject(_memoryContext, _previousObject);
				}
				_previousObject = 0;
				if (_bitmap != 0)
				{
					DeleteObject(_bitmap);
				}
				_bitmap = 0;
				_bits = 0;
				if (_memoryContext != 0)
				{
					DeleteDC(_memoryContext);
				}
				_memoryContext = 0;
			}
		}
	}

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool PrintWindow(nint window, nint deviceContext, uint flags);

	[DllImport("user32.dll")]
	private static extern nint GetDC(nint window);

	[DllImport("user32.dll")]
	private static extern int ReleaseDC(nint window, nint deviceContext);

	[DllImport("gdi32.dll", SetLastError = true)]
	private static extern nint CreateCompatibleDC(nint deviceContext);

	[DllImport("gdi32.dll", SetLastError = true)]
	private static extern nint CreateDIBSection(nint deviceContext, ref BitmapInfo info, uint usage, out nint bits, nint section, uint offset);

	[DllImport("gdi32.dll", SetLastError = true)]
	private static extern nint SelectObject(nint deviceContext, nint objectHandle);

	[DllImport("gdi32.dll", SetLastError = true)]
	private static extern bool DeleteObject(nint objectHandle);

	[DllImport("gdi32.dll", SetLastError = true)]
	private static extern bool DeleteDC(nint deviceContext);

	[DllImport("dwmapi.dll")]
	private static extern int DwmFlush();
}
