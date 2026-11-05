#define TRACE
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using GlassDesk.Platform.Displays;
using GlassDesk.UI;

internal sealed class SharedBackdropFeed : IDisposable
{
	private double _captureCadenceMilliseconds = 16.0;

	private int _interactionActive = 1;

	private readonly TimeSpan _idleBufferRetention = TimeSpan.FromSeconds(10L);

	private readonly object _gate = new object();

	private LatestBackdropState? _state;

	private Thread? _thread;

	private CancellationTokenSource? _stop;

	private bool _stopping;

	private bool _performanceSleeping;

	private long _captureAcquisitions;

	private long _sceneUpdates;

	private long _resizes;

	private long _publishedFrames;

	private long _skippedForReaders;

	private long _ownersRetired;

	private int _activeCaptureOwner;

	private string? _failure;

	private readonly BackdropProviderPolicy _policy = new BackdropProviderPolicy();

	private long _signalGeneration = BackdropRecovery.Generation;

	private readonly Action<LatestBackdropState, CancellationToken> _run;

	private readonly Func<ScreenRect, bool, Action<CaptureTrace>?, IDesktopSceneCapture> _create = DesktopSceneCaptureFactory.Create;

	internal string? Failure => Volatile.Read(in _failure);

	internal Action<CapturePixels, byte[]>? CaptureObserved { get; init; }

	internal Action<nint>? CaptureStarting { get; init; }

	internal bool DiagnosticCarrierOnScreen { get; init; }

	internal Action<CaptureTrace>? TraceObserved { get; set; } = BackdropRecovery.Record;

	internal void SetInteractionActive(bool active)
	{
		Volatile.Write(ref _interactionActive, active ? 1 : 0);
	}

	internal void SetAnimationActive(bool active)
	{
		Volatile.Write(ref _captureCadenceMilliseconds, active ? 8.333333333333334 : 16.0);
	}

	internal object InspectPerformance()
	{
		lock (_gate)
		{
			return new
			{
				captureIntervalMilliseconds = Volatile.Read(in _captureCadenceMilliseconds),
				interactionActive = (Volatile.Read(in _interactionActive) != 0),
				captureAcquisitions = Interlocked.Read(in _captureAcquisitions),
				sceneUpdates = Interlocked.Read(in _sceneUpdates),
				resizes = Interlocked.Read(in _resizes),
				publishedFrames = Interlocked.Read(in _publishedFrames),
				skippedForReaders = Interlocked.Read(in _skippedForReaders),
				ownersRetired = Interlocked.Read(in _ownersRetired),
				performanceSleeping = _performanceSleeping,
				suspended = ((object)_state?.CurrentRequest == null),
				workerAlive = (_thread?.IsAlive ?? false),
				activeCaptureOwner = Volatile.Read(in _activeCaptureOwner),
				captureWorkerPids = CaptureProcessFamily.WorkerProcesses,
				pendingRetirement = CaptureProcessFamily.PendingRetirementCount,
				failure = Failure
			};
		}
	}

	internal object InspectTiming()
	{
		lock (_gate)
		{
			return new
			{
				failure = Failure,
				stopping = _stopping,
				workerAlive = _thread?.IsAlive,
				qpcFrequency = Stopwatch.Frequency,
				state = _state?.InspectTiming(Stopwatch.GetTimestamp())
			};
		}
	}

	internal SharedBackdropFeed()
	{
		_run = Run;
	}

	internal SharedBackdropFeed(Action<LatestBackdropState, CancellationToken> run)
	{
		_run = run;
	}

	internal SharedBackdropFeed(Func<ScreenRect, bool, Action<CaptureTrace>?, IDesktopSceneCapture> create, TimeSpan? idleBufferRetention = null)
	{
		_run = Run;
		_create = create ?? throw new ArgumentNullException("create");
		if (idleBufferRetention.HasValue)
		{
			TimeSpan valueOrDefault = idleBufferRetention.GetValueOrDefault();
			if (valueOrDefault <= TimeSpan.Zero)
			{
				throw new ArgumentOutOfRangeException("idleBufferRetention");
			}
			_idleBufferRetention = valueOrDefault;
		}
	}

	internal bool TryRead(PixelRect region, int width, int height, out BackdropFrame? frame)
	{
		frame = null;
		lock (_gate)
		{
			if (_performanceSleeping)
			{
				return false;
			}
			if (_signalGeneration != BackdropRecovery.Generation)
			{
				_signalGeneration = BackdropRecovery.Generation;
				_state?.InvalidateScene();
				_policy.RecordSuccess();
			}
			if (!_stopping && _state != null)
			{
				Thread? thread = _thread;
				if (thread != null && !thread.IsAlive)
				{
					if (!_policy.RetryAllowed(Stopwatch.GetTimestamp()))
					{
						return false;
					}
					_thread = null;
					_state = null;
					_stop = null;
				}
			}
			if (_stopping)
			{
				Thread? thread2 = _thread;
				if (thread2 != null && thread2.IsAlive)
				{
					TraceObserved?.Invoke(new CaptureTrace("Consumer", "WorkerStopping", Stopwatch.GetTimestamp(), 0L, 0L, 0L, 0L, 0, 0L));
					return false;
				}
				_thread = null;
				_state = null;
				_stop = null;
				_stopping = false;
			}
			if (_state == null)
			{
				Volatile.Write(ref _failure, null);
				_state = new LatestBackdropState
				{
					TraceObserved = TraceObserved
				};
				_stop = new CancellationTokenSource();
				LatestBackdropState state = _state;
				CancellationTokenSource stop = _stop;
				_thread = new Thread(() =>
				{
					RunOwned(state, stop);
				})
				{
					IsBackground = true,
					Name = "Independent compatible glass backdrop"
				};
				_thread.SetApartmentState(ApartmentState.STA);
				_thread.Start();
			}
			try
			{
				_state.Request(new PixelBounds(region.X, region.Y, region.Right, region.Bottom), width, height);
			}
			catch (ObjectDisposedException)
			{
				TraceObserved?.Invoke(new CaptureTrace("Consumer", "WorkerStopped", Stopwatch.GetTimestamp(), 0L, 0L, 0L, 0L, 0, 0L));
				return false;
			}
			return _state.TryRead(Stopwatch.GetTimestamp(), Stopwatch.Frequency * 120 / 1000, out frame);
		}
	}

	private void RunOwned(LatestBackdropState state, CancellationTokenSource stop)
	{
		try
		{
			_run(state, stop.Token);
		}
		catch (Exception ex)
		{
			Volatile.Write(ref _failure, ex.Message);
			Action<CaptureTrace>? traceObserved = TraceObserved;
			if (traceObserved != null)
			{
				string name = ex.GetType().Name;
				long timestamp = Stopwatch.GetTimestamp();
				long sourceTicks = 0L;
				long endTicks = 0L;
				long barrierTicks = 0L;
				long generation = 0L;
				long windowHandle = 0L;
				int hResult = ex.HResult;
				string message = ex.Message;
				traceObserved(new CaptureTrace("CaptureFailure", name, timestamp, sourceTicks, endTicks, barrierTicks, generation, 0, windowHandle, 0u, null, hResult, 0, null, null, null, null, null, 0, 0, 0, -1.0, -1.0, -1.0, 0, message));
			}
			Trace.WriteLine("Compatible sample worker failed: " + ex.Message);
		}
		finally
		{
			state.Dispose();
			state.RetireWaitHandle();
			lock (_gate)
			{
				if (!_stopping && Failure != null)
				{
					_policy.RecordFailure(Stopwatch.GetTimestamp());
				}
				stop.Dispose();
				if (_stop == stop)
				{
					_stop = null;
				}
			}
		}
	}

	internal void Suspend(bool deep = false)
	{
		lock (_gate)
		{
			_state?.Suspend();
			if (deep)
			{
				_state?.Dispose();
				_stopping = true;
				_stop?.Cancel();
			}
		}
	}

	internal void SetPerformanceSleeping(bool sleeping)
	{
		if (sleeping)
		{
			SetAnimationActive(active: false);
		}
		lock (_gate)
		{
			_performanceSleeping = sleeping;
			if (sleeping)
			{
				Suspend(deep: true);
			}
		}
	}

	private void Run(LatestBackdropState state, CancellationToken stop)
	{
		IDesktopSceneCapture desktopSceneCapture = null;
		PixelBounds outer = default;
		long num = 0L;
		long num2 = 0L;
		long num3 = 0L;
		long generation = BackdropRecovery.Generation;
		nint num4 = 0;
		try
		{
			num4 = SetThreadDpiAwarenessContext(-4);
			if (num4 == 0)
			{
				throw new InvalidOperationException("Physical capture DPI context unavailable.");
			}
			using NativeFrameResize nativeFrameResize = new NativeFrameResize();
			using NativeCadenceWait nativeCadenceWait = new NativeCadenceWait();
			using SamplePixelPool samplePixelPool = new SamplePixelPool();
			WaitHandle[] waitHandles = new WaitHandle[2] { stop.WaitHandle, state.Changed };
			while (!stop.IsCancellationRequested)
			{
				if (generation != BackdropRecovery.Generation)
				{
					generation = BackdropRecovery.Generation;
					desktopSceneCapture?.Dispose();
					desktopSceneCapture = null;
					Volatile.Write(ref _activeCaptureOwner, 0);
					outer = default;
					nativeFrameResize.Dispose();
					state.InvalidateScene();
					TraceObserved?.Invoke(new CaptureTrace("Recovery", "ResourcesRetired", Stopwatch.GetTimestamp(), 0L, 0L, 0L, 0L, 0, 0L));
				}
				BackdropRequest currentRequest = state.CurrentRequest;
				if ((object)currentRequest == null)
				{
					if (desktopSceneCapture != null)
					{
						if (num2 == 0L)
						{
							num2 = Stopwatch.GetTimestamp();
						}
						if (Stopwatch.GetElapsedTime(num2) >= _idleBufferRetention)
						{
							desktopSceneCapture.Dispose();
							desktopSceneCapture = null;
							outer = default;
							nativeFrameResize.Dispose();
							Volatile.Write(ref _activeCaptureOwner, 0);
							Interlocked.Increment(ref _ownersRetired);
							TraceObserved?.Invoke(new CaptureTrace("CaptureBuffers", "Released", Stopwatch.GetTimestamp(), 0L, 0L, 0L, 0L, 0, 0L));
							TraceObserved?.Invoke(new CaptureTrace("CaptureOwner", "Retired", Stopwatch.GetTimestamp(), 0L, 0L, 0L, 0L, 0, 0L));
							break;
						}
					}
					int millisecondsTimeout = ((desktopSceneCapture == null) ? (-1) : Math.Max(1, (int)Math.Ceiling((_idleBufferRetention - Stopwatch.GetElapsedTime(num2)).TotalMilliseconds)));
					WaitHandle.WaitAny(waitHandles, millisecondsTimeout);
					continue;
				}
				num2 = 0L;
				long timestamp = Stopwatch.GetTimestamp();
				double num5 = Volatile.Read(in _captureCadenceMilliseconds);
				if (num5 >= 16.0 && Volatile.Read(in _interactionActive) == 0)
				{
					num5 = 33.333333333333336;
				}
				if (num5 >= 16.0 && state.IsContentIdle(timestamp))
				{
					num5 = 100.0;
				}
				if (desktopSceneCapture == null || !Contains(outer, currentRequest.Region))
				{
					PixelBounds pixelBounds = new PixelBounds(currentRequest.Region.Left - 64, currentRequest.Region.Top - 64, currentRequest.Region.Right + 64, currentRequest.Region.Bottom + 64);
					PixelRect? pixelRect = DisplayService.Primary()?.Bounds;
					if (pixelRect.HasValue)
					{
						PixelRect valueOrDefault = pixelRect.GetValueOrDefault();
						if (valueOrDefault.X <= currentRequest.Region.Left && valueOrDefault.Y <= currentRequest.Region.Top && valueOrDefault.Right >= currentRequest.Region.Right && valueOrDefault.Bottom >= currentRequest.Region.Bottom)
						{
							pixelBounds = new PixelBounds(Math.Max(pixelBounds.Left, valueOrDefault.X), Math.Max(pixelBounds.Top, valueOrDefault.Y), Math.Min(pixelBounds.Right, valueOrDefault.Right), Math.Min(pixelBounds.Bottom, valueOrDefault.Bottom));
						}
					}
					desktopSceneCapture?.Dispose();
					outer = ((desktopSceneCapture == null) ? pixelBounds : new PixelBounds(Math.Min(outer.Left, pixelBounds.Left), Math.Min(outer.Top, pixelBounds.Top), Math.Max(outer.Right, pixelBounds.Right), Math.Max(outer.Bottom, pixelBounds.Bottom)));
					desktopSceneCapture = _create(new ScreenRect(outer.Left, outer.Top, outer.Width, outer.Height), DiagnosticCarrierOnScreen, TraceObserved);
					Volatile.Write(ref _activeCaptureOwner, 1);
					num3 = Stopwatch.GetTimestamp() + Stopwatch.Frequency * 6;
				}
				long timestamp2 = Stopwatch.GetTimestamp();
				Interlocked.Increment(ref _sceneUpdates);
				if (desktopSceneCapture.UpdateExclusions())
				{
					state.InvalidateScene();
				}
				TraceObserved?.Invoke(new CaptureTrace("CaptureCost", "SceneUpdate", Stopwatch.GetTimestamp(), 0L, timestamp2, 0L, 0L, 0, 0L));
				currentRequest = state.CurrentRequest;
				if ((object)currentRequest == null)
				{
					continue;
				}
				CaptureStarting?.Invoke(desktopSceneCapture.CarrierHandle);
				long sceneRevision = desktopSceneCapture.SceneRevision;
				long timestamp3 = Stopwatch.GetTimestamp();
				using SamplePixelLease samplePixelLease = samplePixelPool.TryRent(checked(currentRequest.Width * currentRequest.Height * 4));
				if (samplePixelLease == null)
				{
					TraceObserved?.Invoke(new CaptureTrace("Publish", "ReadersBusy", Stopwatch.GetTimestamp(), 0L, 0L, 0L, 0L, 0, 0L));
					Interlocked.Increment(ref _skippedForReaders);
					nativeCadenceWait.Wait(num5, stop);
					continue;
				}
				BackdropRequest backdropRequest = currentRequest;
				PixelBounds crop = new PixelBounds(currentRequest.Region.Left - outer.Left, currentRequest.Region.Top - outer.Top, currentRequest.Region.Right - outer.Left, currentRequest.Region.Bottom - outer.Top);
				TimeSpan waitForFrame = TimeSpan.FromMilliseconds(Math.Max(0.0, num5 - Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds));
				bool flag = desktopSceneCapture.SupportsSampling && CaptureObserved == null;
				CapturePixels capturePixels = (flag ? desktopSceneCapture.TryReadSampled(crop, currentRequest.Width, currentRequest.Height, samplePixelLease.Pixels, waitForFrame, stop, currentRequest.SourceNotBeforeTicks) : desktopSceneCapture.TryRead(waitForFrame, stop, currentRequest.SourceNotBeforeTicks, CaptureObserved == null));
				Interlocked.Increment(ref _captureAcquisitions);
				TraceObserved?.Invoke(new CaptureTrace("CaptureCost", "FrameAcquisition", Stopwatch.GetTimestamp(), 0L, timestamp3, 0L, 0L, 0, 0L));
				bool flag2 = desktopSceneCapture.SceneRevision != sceneRevision;
				if (flag2)
				{
					state.InvalidateScene();
				}
				if ((object)capturePixels != null)
				{
					num3 = 0L;
					lock (_gate)
					{
						_policy.RecordSuccess();
					}
				}
				else if (num3 != 0L && Stopwatch.GetTimestamp() > num3)
				{
					throw new InvalidOperationException("Desktop background capture did not produce an initial frame.");
				}
				currentRequest = state.CurrentRequest;
				if ((object)capturePixels != null && (object)currentRequest != null && Contains(outer, currentRequest.Region) && !flag2 && currentRequest.Width == backdropRequest.Width && currentRequest.Height == backdropRequest.Height && (!flag || currentRequest == backdropRequest))
				{
					long timestamp4 = Stopwatch.GetTimestamp();
					crop = new PixelBounds(currentRequest.Region.Left - outer.Left, currentRequest.Region.Top - outer.Top, currentRequest.Region.Right - outer.Left, currentRequest.Region.Bottom - outer.Top);
					if (!flag)
					{
						nativeFrameResize.ResizeInto(capturePixels, currentRequest.Width, currentRequest.Height, crop, samplePixelLease.Pixels);
					}
					byte[] pixels = samplePixelLease.Pixels;
					Interlocked.Increment(ref _resizes);
					TraceObserved?.Invoke(new CaptureTrace("Processing", "ResizeCompleted", Stopwatch.GetTimestamp(), capturePixels.SourceQpcTicks, timestamp4, 0L, 0L, 0, 0L));
					CaptureObserved?.Invoke(capturePixels, pixels);
					long timestamp5 = Stopwatch.GetTimestamp();
					double num6 = (double)(capturePixels.SourceQpcTicks - timestamp5) * 1000.0 / (double)Stopwatch.Frequency;
					if (num6 > 33.0)
					{
						TraceObserved?.Invoke(new CaptureTrace("SourceClock", "BeyondCadence", timestamp5, capturePixels.SourceQpcTicks, 0L, 0L, 0L, 0, 0L));
					}
					else
					{
						if (num6 > 0.0)
						{
							TraceObserved?.Invoke(new CaptureTrace("SourceClock", "Waiting", timestamp5, capturePixels.SourceQpcTicks, 0L, 0L, 0L, 0, 0L));
							while (!stop.IsCancellationRequested && (timestamp5 = Stopwatch.GetTimestamp()) < capturePixels.SourceQpcTicks)
							{
								nativeCadenceWait.Wait((double)(capturePixels.SourceQpcTicks - timestamp5) * 1000.0 / (double)Stopwatch.Frequency, stop);
							}
							TraceObserved?.Invoke(new CaptureTrace("SourceClock", stop.IsCancellationRequested ? "Cancelled" : "Reached", Stopwatch.GetTimestamp(), capturePixels.SourceQpcTicks, 0L, 0L, 0L, 0, 0L));
						}
						if (!stop.IsCancellationRequested && state.Publish(BackdropFrame.FromLease(currentRequest, ++num, timestamp, Stopwatch.GetTimestamp(), samplePixelLease.Acquire(), (capturePixels.SourceQpcTicks == 0L) ? ((long?)null) : new long?(capturePixels.SourceQpcTicks), capturePixels.IsStatic, capturePixels.Source)))
						{
							Interlocked.Increment(ref _publishedFrames);
						}
					}
				}
				Program.Pump(TimeSpan.FromMilliseconds(1L));
				num5 = Volatile.Read(in _captureCadenceMilliseconds);
				if (num5 >= 16.0 && Volatile.Read(in _interactionActive) == 0)
				{
					num5 = 33.333333333333336;
				}
				if (num5 >= 16.0 && state.IsContentIdle(Stopwatch.GetTimestamp()))
				{
					num5 = 100.0;
				}
				double num7 = num5 - Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
				if (num7 >= 30.0)
				{
					WaitHandle.WaitAny(waitHandles, (int)Math.Ceiling(num7));
				}
				else if (num7 > 0.0)
				{
					nativeCadenceWait.Wait(num7, stop);
				}
			}
		}
		catch (OperationCanceledException) when (stop.IsCancellationRequested)
		{
		}
		catch (Exception ex2)
		{
			Volatile.Write(ref _failure, ex2.Message);
			Action<CaptureTrace>? traceObserved = TraceObserved;
			if (traceObserved != null)
			{
				string name = ex2.GetType().Name;
				long timestamp6 = Stopwatch.GetTimestamp();
				long sourceTicks = 0L;
				long endTicks = 0L;
				long barrierTicks = 0L;
				long generation2 = 0L;
				long windowHandle = 0L;
				int hResult = ex2.HResult;
				string message = ex2.Message;
				traceObserved(new CaptureTrace("CaptureFailure", name, timestamp6, sourceTicks, endTicks, barrierTicks, generation2, 0, windowHandle, 0u, null, hResult, 0, null, null, null, null, null, 0, 0, 0, -1.0, -1.0, -1.0, 0, message));
			}
			Trace.WriteLine("Compatible sample source failed: " + ex2.Message);
			state.Dispose();
		}
		finally
		{
			try
			{
				if (desktopSceneCapture != null)
				{
					desktopSceneCapture.Dispose();
					Interlocked.Increment(ref _ownersRetired);
				}
			}
			finally
			{
				Volatile.Write(ref _activeCaptureOwner, 0);
				if (num4 != 0)
				{
					SetThreadDpiAwarenessContext(num4);
				}
			}
		}
	}

	private static bool Contains(PixelBounds outer, PixelBounds inner)
	{
		if (outer.Left <= inner.Left && outer.Top <= inner.Top && outer.Right >= inner.Right)
		{
			return outer.Bottom >= inner.Bottom;
		}
		return false;
	}

	public void Dispose()
	{
		lock (_gate)
		{
			_state?.Dispose();
			_stopping = true;
			_stop?.Cancel();
			Thread? thread = _thread;
			if (thread == null || !thread.IsAlive)
			{
				_thread = null;
				_state = null;
				_stop = null;
			}
		}
	}

	[DllImport("user32.dll")]
	private static extern nint SetThreadDpiAwarenessContext(nint context);
}
