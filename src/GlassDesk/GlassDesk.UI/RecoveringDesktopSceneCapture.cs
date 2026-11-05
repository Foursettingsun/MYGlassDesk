#define TRACE
using System;
using System.Diagnostics;
using System.Threading;
using GlassDesk.Platform;
using GlassDesk.UI.Controls;

internal sealed class RecoveringDesktopSceneCapture : IDesktopSceneCapture, IDisposable
{
	private IDesktopSceneCapture? _source;

	private readonly ScreenRect _region;

	private readonly Action<CaptureTrace>? _trace;

	private bool _fallback;

	private long _initialDeadline;

	private readonly Func<IDesktopSceneCapture> _createFallback;

	private readonly Func<IDesktopSceneCapture> _createPrimary;

	private readonly Func<long> _clock;

	private long _lastBarrier;

	private long _lastReadAt;

	private bool _disposed;

	private long _reprobeAt;

	public bool AllowsStaticContent => _source?.AllowsStaticContent ?? false;

	public bool SupportsSampling => _source?.SupportsSampling ?? false;

	public long SceneRevision => _source?.SceneRevision ?? 0;

	internal nint CarrierHandle => _source?.CarrierHandle ?? 0;

	nint IDesktopSceneCapture.CarrierHandle => CarrierHandle;

	internal RecoveringDesktopSceneCapture(ScreenRect region, Action<CaptureTrace>? trace = null, string? executable = null)
		: this(region, DesktopSceneCaptureFactory.SelectProvider(WindowsCapabilities.BuildNumber), trace, executable)
	{
	}

	internal RecoveringDesktopSceneCapture(ScreenRect region, DesktopSceneProvider provider, Action<CaptureTrace>? trace = null, string? executable = null)
		: this(region, () => new IsolatedDesktopSceneCapture(region, provider == DesktopSceneProvider.IsolatedDocumentedScene, trace, executable), (provider == DesktopSceneProvider.IsolatedDocumentedScene) ? ((Func<IDesktopSceneCapture>)(() => new IsolatedDesktopSceneCapture(region, GlassBackdropSource.Wallpaper, trace, executable))) : ((Func<IDesktopSceneCapture>)(() => new RecoveringDesktopSceneCapture(region, DesktopSceneProvider.IsolatedDocumentedScene, trace, executable))), Stopwatch.GetTimestamp, trace)
	{
	}

	internal RecoveringDesktopSceneCapture(ScreenRect region, Func<IDesktopSceneCapture> createNative, Func<IDesktopSceneCapture> createFallback, Func<long> clock)
		: this(region, createNative, createFallback, clock, null)
	{
	}

	internal RecoveringDesktopSceneCapture(ScreenRect region, Func<IDesktopSceneCapture> createNative, Func<IDesktopSceneCapture> createFallback, Func<long> clock, Action<CaptureTrace>? trace)
	{
		_region = region;
		_createPrimary = createNative;
		_createFallback = createFallback;
		_clock = clock;
		_trace = trace;
		try
		{
			_source = createNative();
			_initialDeadline = _clock() + Stopwatch.Frequency * 3;
		}
		catch (Exception error)
		{
			Recover(error);
		}
	}

	private void Recover(Exception error)
	{
		IDesktopSceneCapture source = _source;
		_source = null;
		try
		{
			source?.Dispose();
		}
		catch
		{
			_disposed = true;
			throw;
		}
		_fallback = true;
		_reprobeAt = _clock() + Stopwatch.Frequency * 30;
		Trace.WriteLine("Native capture helper failed; documented compatibility background: " + error.Message);
		Action<CaptureTrace>? trace = _trace;
		if (trace != null)
		{
			long timestamp = Stopwatch.GetTimestamp();
			long sourceTicks = 0L;
			long endTicks = 0L;
			long barrierTicks = 0L;
			long generation = 0L;
			long windowHandle = 0L;
			int hResult = error.HResult;
			string detail = error.GetType().Name + ": " + error.Message;
			trace(new CaptureTrace("BackdropProvider", "DocumentedFallback", timestamp, sourceTicks, endTicks, barrierTicks, generation, 0, windowHandle, 0u, null, hResult, 0, null, null, null, null, null, 0, 0, 0, -1.0, -1.0, -1.0, 0, detail));
		}
		_source = _createFallback();
		_initialDeadline = _clock() + Stopwatch.Frequency * 3;
	}

	internal bool UpdateExclusions()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (_fallback && _clock() >= _reprobeAt)
		{
			IDesktopSceneCapture source = _source;
			_source = null;
			try
			{
				source?.Dispose();
			}
			catch
			{
				_disposed = true;
				throw;
			}
			_fallback = false;
			try
			{
				_source = _createPrimary();
				_initialDeadline = _clock() + Stopwatch.Frequency * 3;
				_trace?.Invoke(new CaptureTrace("BackdropProvider", "EnhancedReprobe", Stopwatch.GetTimestamp(), 0L, 0L, 0L, 0L, 0, 0L));
			}
			catch (Exception error)
			{
				Recover(error);
			}
			return true;
		}
		try
		{
			bool flag = _source.UpdateExclusions();
			if (flag && _initialDeadline == 0L)
			{
				_initialDeadline = _clock() + Stopwatch.Frequency * 3;
			}
			return flag;
		}
		catch (Exception error2) when (!_fallback)
		{
			Recover(error2);
			return true;
		}
	}

	bool IDesktopSceneCapture.UpdateExclusions()
	{
		return UpdateExclusions();
	}

	internal CapturePixels? TryRead(TimeSpan waitForFrame = default(TimeSpan), CancellationToken cancellation = default(CancellationToken), long sourceNotBeforeTicks = 0L, bool reusePixels = false)
	{
		return ReadFrame((IDesktopSceneCapture source) => source.TryRead(waitForFrame, cancellation, sourceNotBeforeTicks, reusePixels), cancellation, sourceNotBeforeTicks);
	}

	internal CapturePixels? TryReadSampled(PixelBounds crop, int width, int height, byte[] target, TimeSpan waitForFrame = default(TimeSpan), CancellationToken cancellation = default(CancellationToken), long sourceNotBeforeTicks = 0L)
	{
		return ReadFrame((IDesktopSceneCapture source) => source.TryReadSampled(crop, width, height, target, waitForFrame, cancellation, sourceNotBeforeTicks), cancellation, sourceNotBeforeTicks);
	}

	private CapturePixels? ReadFrame(Func<IDesktopSceneCapture, CapturePixels?> read, CancellationToken cancellation, long sourceNotBeforeTicks)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		cancellation.ThrowIfCancellationRequested();
		try
		{
			long num = _clock();
			bool flag = _lastReadAt != 0L && num - _lastReadAt >= Stopwatch.Frequency * 3;
			_lastReadAt = num;
			if (sourceNotBeforeTicks > _lastBarrier)
			{
				_lastBarrier = sourceNotBeforeTicks;
				if ((_initialDeadline == 0) | flag)
				{
					_initialDeadline = num + Stopwatch.Frequency * 3;
				}
			}
			CapturePixels capturePixels = read(_source);
			if ((object)capturePixels != null)
			{
				_initialDeadline = ((_fallback || AllowsStaticContent) ? 0 : (_clock() + Stopwatch.Frequency * 3));
			}
			else if (!cancellation.IsCancellationRequested && _initialDeadline != 0L && _clock() > _initialDeadline)
			{
				throw new TimeoutException((_fallback ? "Documented capture" : "Native capture helper") + " stopped delivering frames.");
			}
			return capturePixels;
		}
		catch (Exception error) when (!_fallback && !cancellation.IsCancellationRequested)
		{
			Recover(error);
			return ReadFrame(read, cancellation, sourceNotBeforeTicks);
		}
	}

	CapturePixels? IDesktopSceneCapture.TryRead(TimeSpan wait, CancellationToken cancellation, long notBefore, bool reuse)
	{
		return TryRead(wait, cancellation, notBefore, reuse);
	}

	CapturePixels? IDesktopSceneCapture.TryReadSampled(PixelBounds crop, int width, int height, byte[] target, TimeSpan wait, CancellationToken cancellation, long notBefore)
	{
		return TryReadSampled(crop, width, height, target, wait, cancellation, notBefore);
	}

	internal void ReleaseReadbackBuffers()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		try
		{
			_source.ReleaseReadbackBuffers();
		}
		catch (Exception error) when (!_fallback)
		{
			Recover(error);
		}
	}

	void IDesktopSceneCapture.ReleaseReadbackBuffers()
	{
		ReleaseReadbackBuffers();
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			IDesktopSceneCapture? source = _source;
			_source = null;
			source?.Dispose();
		}
	}
}
