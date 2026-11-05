using System;
using System.Diagnostics;
using System.Threading;

internal sealed class LatestBackdropState : IDisposable
{
	private static long nextGeneration;

	private readonly object gate = new object();

	private BackdropRequest? current;

	private BackdropFrame? latest;

	private long generation;

	private long contentRevision;

	private long contentChangedAt;

	private bool disposed;

	private bool resumed;

	private long sourceNotBeforeTicks;

	private AutoResetEvent? _changed;

	internal Action<CaptureTrace>? TraceObserved { get; init; }

	internal WaitHandle Changed
	{
		get
		{
			lock (gate)
			{
				return _changed ?? (_changed = new AutoResetEvent(initialState: false));
			}
		}
	}

	internal BackdropRequest? CurrentRequest
	{
		get
		{
			lock (gate)
			{
				return disposed ? null : current;
			}
		}
	}

	private long NextGeneration()
	{
		return generation = Interlocked.Increment(ref nextGeneration);
	}

	internal void RetireWaitHandle()
	{
		lock (gate)
		{
			_changed?.Dispose();
			_changed = null;
		}
	}

	private void ClearLatest()
	{
		latest?.Dispose();
		latest = null;
	}

	internal BackdropRequest Request(PixelBounds region, int width, int height)
	{
		if (region.Width <= 0 || region.Height <= 0 || width <= 0 || height <= 0 || (long)width * (long)height > 1000000)
		{
			throw new ArgumentException("Invalid backdrop request.");
		}
		lock (gate)
		{
			ObjectDisposedException.ThrowIf(disposed, this);
			if ((object)current != null && current.Region == region && current.Width == width && current.Height == height)
			{
				return current;
			}
			ClearLatest();
			if (resumed)
			{
				sourceNotBeforeTicks = Stopwatch.GetTimestamp();
				resumed = false;
			}
			current = new BackdropRequest(region, width, height, NextGeneration())
			{
				SourceNotBeforeTicks = sourceNotBeforeTicks
			};
			_changed?.Set();
			TraceObserved?.Invoke(new CaptureTrace("Geometry", "Changed", Stopwatch.GetTimestamp(), 0L, 0L, 0L, generation, 0, 0L));
			return current;
		}
	}

	internal bool IsContentIdle(long now)
	{
		lock (gate)
		{
			return latest != null && latest.Request == current && contentChangedAt != 0L && now - contentChangedAt >= Stopwatch.Frequency / 2;
		}
	}

	internal void InvalidateScene()
	{
		lock (gate)
		{
			if (!disposed)
			{
				ClearLatest();
				sourceNotBeforeTicks = Stopwatch.GetTimestamp();
				if ((object)current != null)
				{
					current = current with
					{
						Generation = NextGeneration(),
						SourceNotBeforeTicks = sourceNotBeforeTicks
					};
				}
				_changed?.Set();
			}
		}
	}

	internal void Suspend()
	{
		lock (gate)
		{
			if (!disposed)
			{
				resumed = true;
				if ((object)current != null)
				{
					current = null;
					ClearLatest();
					NextGeneration();
					_changed?.Set();
				}
			}
		}
	}

	internal object InspectTiming(long now)
	{
		lock (gate)
		{
			return new
			{
				disposed = disposed,
				generation = generation,
				sequence = latest?.Sequence,
				currentMatches = (latest != null && latest.Request == current),
				readAgeTicks = ((latest == null) ? ((long?)null) : new long?(now - latest.EndTicks)),
				sourceAgeTicks = ((latest == null) ? ((long?)null) : new long?(now - latest.SourceTicks))
			};
		}
	}

	internal bool Publish(BackdropFrame frame)
	{
		lock (gate)
		{
			string text;
			if (disposed)
			{
				text = "WorkerStopped";
			}
			else if (frame.Request != current)
			{
				text = "GenerationChanged";
			}
			else if (frame.SourceTicks < frame.Request.SourceNotBeforeTicks)
			{
				text = "BeforeResume";
			}
			else
			{
				text = ((latest != null && frame.Sequence <= latest.Sequence) ? "OutOfOrder" : "Delivered");
			}
			TraceObserved?.Invoke(new CaptureTrace("Publish", text, Stopwatch.GetTimestamp(), frame.SourceTicks, frame.EndTicks, 0L, frame.Request.Generation, 0, 0L));
			if (text != "Delivered")
			{
				if (latest != frame)
				{
					frame.Dispose();
				}
				return false;
			}
			int num;
			if (latest != null && latest.Source == frame.Source && latest.Request == frame.Request)
			{
				ReadOnlyMemory<byte> pixels = latest.Pixels;
				ReadOnlySpan<byte> span = pixels.Span;
				pixels = frame.Pixels;
				num = (span.SequenceEqual(pixels.Span) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			bool flag = (byte)num != 0;
			frame.ContentRevision = (flag ? latest.ContentRevision : (++contentRevision));
			if (!flag)
			{
				contentChangedAt = Stopwatch.GetTimestamp();
			}
			if (latest != frame)
			{
				ClearLatest();
			}
			latest = frame;
			return true;
		}
	}

	internal bool TryRead(long now, long maximumAgeTicks, out BackdropFrame? frame)
	{
		lock (gate)
		{
			frame = null;
			string text;
			if (disposed)
			{
				text = "WorkerStopped";
			}
			else if (latest == null)
			{
				text = "NoFrame";
			}
			else if (latest.Request != current)
			{
				text = "GeometryMismatch";
			}
			else if (now < latest.EndTicks)
			{
				text = "FutureReadback";
			}
			else if (maximumAgeTicks < 0)
			{
				text = "InvalidAgeBudget";
			}
			else
			{
				text = ((now < latest.SourceTicks) ? "FutureSource" : "Delivered");
			}
			TraceObserved?.Invoke(new CaptureTrace("Consumer", text, now, latest?.SourceTicks ?? 0, latest?.EndTicks ?? 0, 0L, generation, 0, 0L));
			if (text != "Delivered")
			{
				return false;
			}
			frame = latest.Acquire();
			return true;
		}
	}

	public void Dispose()
	{
		lock (gate)
		{
			disposed = true;
			ClearLatest();
			current = null;
			_changed?.Set();
		}
	}
}
