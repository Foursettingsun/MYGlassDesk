using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using WinRT;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;

internal sealed class PersistentWindowCapture : IWindowCapture, IDisposable
{
	private nint _device;

	private nint _context;

	private nint _dxgi;

	private nint _inspectable;

	private IDirect3DDevice? _projected;

	private Direct3D11CaptureFramePool? _pool;

	private GraphicsCaptureSession? _session;

	private GraphicsCaptureItem? _item;

	private readonly SizeInt32 _captureSize;

	private long _previousSourceTicks;

	private long _lastSourceTicks;

	private long _awaitingSceneSince;

	private long _demandBarrier;

	private readonly Action<CaptureTrace>? _trace;

	private readonly WgcWindowCapture.PixelReadback _readback = new WgcWindowCapture.PixelReadback();

	private readonly object _notificationGate = new object();

	private readonly AutoResetEvent _arrival = new AutoResetEvent(initialState: false);

	private bool _disposed;

	private bool _notificationsRegistered;

	internal PersistentWindowCapture(nint hwnd, Action<CaptureTrace>? trace = null, nint sharedDevice = 0, nint sharedContext = 0)
	{
		_trace = trace;
		if (sharedDevice == 0 != (sharedContext == 0))
		{
			throw new ArgumentException("A shared capture device requires its immediate context.");
		}
		string value = "D3D11CreateDevice";
		try
		{
			if (sharedDevice == 0)
			{
				Marshal.ThrowExceptionForHR(D3D11CreateDevice(0, 1u, 0, 32u, 0, 0u, 7u, out _device, out var _, out _context));
			}
			else
			{
				Marshal.AddRef(sharedDevice);
				_device = sharedDevice;
				Marshal.AddRef(sharedContext);
				_context = sharedContext;
			}
			_trace?.Invoke(new CaptureTrace("GraphicsDevice", (sharedDevice == 0) ? "Created" : "Shared", Stopwatch.GetTimestamp(), 0L, 0L, 0L, 0L, 0, 0L));
			value = "IDXGIDevice.QueryInterface";
			Guid iid = new Guid("54ec77fa-1377-44e6-8c32-88fd5f44c84c");
			Marshal.ThrowExceptionForHR(Marshal.QueryInterface(_device, in iid, out _dxgi));
			value = "CreateDirect3D11DeviceFromDXGIDevice";
			Marshal.ThrowExceptionForHR(CreateDirect3D11DeviceFromDXGIDevice(_dxgi, out _inspectable));
			value = "IDirect3DDevice projection";
			_projected = MarshalInterface<IDirect3DDevice>.FromAbi(_inspectable);
			value = "GraphicsCaptureItem";
			GraphicsCaptureItem graphicsCaptureItem = (_item = WgcWindowCapture.CreateItem(hwnd));
			value = "GraphicsCaptureItem.Size";
			_captureSize = graphicsCaptureItem.Size;
			value = "Direct3D11CaptureFramePool.CreateFreeThreaded";
			_pool = Direct3D11CaptureFramePool.CreateFreeThreaded(_projected, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, graphicsCaptureItem.Size);
			value = "Direct3D11CaptureFramePool.FrameArrived";
			_pool.FrameArrived += OnFrameArrived;
			_notificationsRegistered = true;
			value = "Direct3D11CaptureFramePool.CreateCaptureSession";
			_session = _pool.CreateCaptureSession(graphicsCaptureItem);
			value = "GraphicsCaptureSession.IsCursorCaptureEnabled";
			_session.IsCursorCaptureEnabled = false;
			value = "GraphicsCaptureSession.StartCapture";
			_session.StartCapture();
		}
		catch (Exception ex)
		{
			Dispose();
			throw new InvalidOperationException($"Window capture initialization failed at {value} (0x{ex.HResult:X8}): {ex.Message}", ex);
		}
	}

	public CapturePixels? TryRead(long notBeforeQpcTicks = 0L, TimeSpan waitForFrame = default(TimeSpan), CancellationToken cancellation = default(CancellationToken), bool reusePixels = false)
	{
		if (cancellation.IsCancellationRequested)
		{
			return null;
		}
		if (notBeforeQpcTicks > _demandBarrier)
		{
			_demandBarrier = notBeforeQpcTicks;
			if (_awaitingSceneSince == 0L)
			{
				_awaitingSceneSince = Stopwatch.GetTimestamp();
			}
		}
		_arrival.Reset();
		CapturePixels capturePixels = ReadFreshScene(notBeforeQpcTicks, reusePixels);
		if ((object)capturePixels != null || waitForFrame <= TimeSpan.Zero)
		{
			return capturePixels;
		}
		Stopwatch stopwatch = Stopwatch.StartNew();
		WaitHandle[] waitHandles = ((!cancellation.CanBeCanceled) ? new WaitHandle[1] { _arrival } : new WaitHandle[2] { cancellation.WaitHandle, _arrival });
		while (!cancellation.IsCancellationRequested && stopwatch.Elapsed < waitForFrame)
		{
			_trace?.Invoke(new CaptureTrace("NativeWait", "Waiting", Stopwatch.GetTimestamp(), 0L, 0L, notBeforeQpcTicks, 0L, 0, 0L));
			TimeSpan timeSpan = waitForFrame - stopwatch.Elapsed;
			if (timeSpan <= TimeSpan.Zero || WaitHandle.WaitAny(waitHandles, (int)Math.Min(2147483647.0, Math.Ceiling(timeSpan.TotalMilliseconds))) == 258 || cancellation.IsCancellationRequested)
			{
				break;
			}
			capturePixels = ReadFreshScene(notBeforeQpcTicks, reusePixels);
			if ((object)capturePixels != null)
			{
				return capturePixels;
			}
		}
		_trace?.Invoke(new CaptureTrace("NativeWait", cancellation.IsCancellationRequested ? "Cancelled" : "Deadline", Stopwatch.GetTimestamp(), 0L, 0L, notBeforeQpcTicks, 0L, 0, 0L));
		return null;
	}

	private CapturePixels? ReadFreshScene(long notBeforeQpcTicks, bool reusePixels)
	{
		CapturePixels capturePixels = ReadOnce(notBeforeQpcTicks, reusePixels);
		if ((object)capturePixels != null)
		{
			_awaitingSceneSince = 0L;
			return capturePixels;
		}
		if (_awaitingSceneSince != 0L && Stopwatch.GetTimestamp() - _awaitingSceneSince >= Stopwatch.Frequency * 32 / 1000)
		{
			RestartCaptureSession();
			return ReadOnce(notBeforeQpcTicks, reusePixels);
		}
		return null;
	}

	private CapturePixels? ReadOnce(long notBeforeQpcTicks, bool reusePixels)
	{
		Direct3D11CaptureFrame direct3D11CaptureFrame = null;
		int num = 0;
		try
		{
			for (int i = 0; i < 8; i++)
			{
				Direct3D11CaptureFrame direct3D11CaptureFrame2 = _pool.TryGetNextFrame();
				if ((object)direct3D11CaptureFrame2 == null)
				{
					break;
				}
				num++;
				if ((object)direct3D11CaptureFrame == null || direct3D11CaptureFrame2.SystemRelativeTime > direct3D11CaptureFrame.SystemRelativeTime)
				{
					direct3D11CaptureFrame?.Dispose();
					direct3D11CaptureFrame = direct3D11CaptureFrame2;
				}
				else
				{
					direct3D11CaptureFrame2.Dispose();
				}
			}
			if ((object)direct3D11CaptureFrame == null)
			{
				_trace?.Invoke(new CaptureTrace("NativeQueue", "EmptyPool", Stopwatch.GetTimestamp(), 0L, 0L, notBeforeQpcTicks, 0L, 0, 0L));
				return null;
			}
			long num2 = (long)Math.Round(direct3D11CaptureFrame.SystemRelativeTime.TotalSeconds * (double)Stopwatch.Frequency);
			if (notBeforeQpcTicks != 0L && direct3D11CaptureFrame.SystemRelativeTime.TotalSeconds < (double)notBeforeQpcTicks / (double)Stopwatch.Frequency)
			{
				_trace?.Invoke(new CaptureTrace("NativeQueue", "BeforeExclusion", Stopwatch.GetTimestamp(), num2, 0L, notBeforeQpcTicks, 0L, num, 0L));
				return null;
			}
			_trace?.Invoke(new CaptureTrace("NativeQueue", "Delivered", Stopwatch.GetTimestamp(), num2, 0L, notBeforeQpcTicks, 0L, num, 0L));
			_trace?.Invoke(new CaptureTrace("Readback", "Starting", Stopwatch.GetTimestamp(), num2, 0L, 0L, 0L, 0, 0L));
			long timestamp = Stopwatch.GetTimestamp();
			CapturePixels result = _readback.ReadPixels(_device, _context, direct3D11CaptureFrame, _trace, reusePixels);
			if (num2 > _lastSourceTicks)
			{
				_previousSourceTicks = _lastSourceTicks;
				_lastSourceTicks = num2;
			}
			_trace?.Invoke(new CaptureTrace("Acquisition", "ReadbackCompleted", Stopwatch.GetTimestamp(), num2, timestamp, 0L, 0L, 0, 0L));
			return result;
		}
		finally
		{
			direct3D11CaptureFrame?.Dispose();
		}
	}

	public void RefreshSessionAtSceneBoundary()
	{
		long timestamp = Stopwatch.GetTimestamp();
		if (_previousSourceTicks > 0 && _lastSourceTicks > _previousSourceTicks && timestamp - _lastSourceTicks <= Stopwatch.Frequency * 120 / 1000)
		{
			_pool.Recreate(_projected, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, _captureSize);
			_awaitingSceneSince = timestamp;
			_trace?.Invoke(new CaptureTrace("NativePool", "ActiveSceneBoundaryRecreated", timestamp, 0L, 0L, 0L, 0L, 0, 0L));
		}
		else
		{
			RestartCaptureSession();
		}
	}

	private void RestartCaptureSession()
	{
		_previousSourceTicks = 0L;
		_lastSourceTicks = 0L;
		_awaitingSceneSince = 0L;
		_pool.FrameArrived -= OnFrameArrived;
		_notificationsRegistered = false;
		_session?.Dispose();
		_session = null;
		_pool.Dispose();
		_pool = Direct3D11CaptureFramePool.CreateFreeThreaded(_projected, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, _captureSize);
		_pool.FrameArrived += OnFrameArrived;
		_notificationsRegistered = true;
		_session = _pool.CreateCaptureSession(_item);
		_session.IsCursorCaptureEnabled = false;
		_session.StartCapture();
		_trace?.Invoke(new CaptureTrace("CaptureSession", "SceneBoundaryRestarted", Stopwatch.GetTimestamp(), 0L, 0L, 0L, 0L, 0, 0L));
	}

	public void RetireQueuedFramesAtSceneBoundary(long barrierTicks)
	{
		for (int i = 0; i < 8; i++)
		{
			using Direct3D11CaptureFrame direct3D11CaptureFrame = _pool.TryGetNextFrame();
			if ((object)direct3D11CaptureFrame == null)
			{
				break;
			}
			long sourceTicks = (long)Math.Round(direct3D11CaptureFrame.SystemRelativeTime.TotalSeconds * (double)Stopwatch.Frequency);
			_trace?.Invoke(new CaptureTrace("NativeQueue", "RetiredAtSceneBoundary", Stopwatch.GetTimestamp(), sourceTicks, 0L, barrierTicks, 0L, 0, 0L));
		}
	}

	public void ReleaseReadbackBuffers()
	{
		_readback.Dispose();
	}

	public void Dispose()
	{
		lock (_notificationGate)
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
		}
		if ((object)_pool != null && _notificationsRegistered)
		{
			_pool.FrameArrived -= OnFrameArrived;
			_notificationsRegistered = false;
		}
		_session?.Dispose();
		_session = null;
		_pool?.Dispose();
		_pool = null;
		_item = null;
		_projected?.Dispose();
		_projected = null;
		_readback.Dispose();
		Release(ref _inspectable);
		Release(ref _dxgi);
		Release(ref _context);
		Release(ref _device);
		lock (_notificationGate)
		{
			_arrival.Dispose();
		}
	}

	private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
	{
		lock (_notificationGate)
		{
			if (_disposed)
			{
				return;
			}
			_arrival.Set();
		}
		_trace?.Invoke(new CaptureTrace("FrameArrival", "Arrived", Stopwatch.GetTimestamp(), 0L, 0L, 0L, 0L, 0, 0L));
	}

	private static void Release(ref nint p)
	{
		if (p != 0)
		{
			Marshal.Release(p);
		}
		p = 0;
	}

	[DllImport("d3d11.dll")]
	private static extern int D3D11CreateDevice(nint adapter, uint type, nint software, uint flags, nint levels, uint count, uint sdk, out nint device, out uint level, out nint context);

	[DllImport("d3d11.dll")]
	private static extern int CreateDirect3D11DeviceFromDXGIDevice(nint device, out nint inspectable);
}
