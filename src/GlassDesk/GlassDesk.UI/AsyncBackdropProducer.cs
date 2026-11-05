using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

internal sealed class AsyncBackdropProducer : IDisposable
{
	private readonly LatestBackdropState state;

	private readonly Func<IBackdropCapture> createCapture;

	private readonly TimeSpan interval;

	private readonly Func<ICadenceWait>? createWait;

	private readonly CancellationTokenSource cancel = new CancellationTokenSource();

	private readonly Thread thread;

	private readonly object gate = new object();

	private readonly List<double> captureMs = new List<double>();

	private readonly List<double> intervalsMs = new List<double>();

	private readonly List<double> requestedWaitMs = new List<double>();

	private readonly List<double> actualWaitMs = new List<double>();

	private int published;

	private int discarded;

	private string? failure;

	internal AsyncBackdropProducer(LatestBackdropState state, Func<IBackdropCapture> createCapture, TimeSpan interval, Func<ICadenceWait>? createWait = null)
	{
		if (interval <= TimeSpan.Zero)
		{
			throw new ArgumentException("Invalid cadence.");
		}
		this.state = state;
		this.createCapture = createCapture;
		this.interval = interval;
		this.createWait = createWait;
		thread = new Thread(Run)
		{
			IsBackground = true,
			Name = "Isolated backdrop capture"
		};
		thread.Start();
	}

	private void Run()
	{
		try
		{
			using IBackdropCapture backdropCapture = createCapture();
			using ICadenceWait cadenceWait = createWait?.Invoke();
			long num = 0L;
			long num2 = 0L;
			WaitHandle[] waitHandles = new WaitHandle[2]
			{
				cancel.Token.WaitHandle,
				state.Changed
			};
			while (!cancel.IsCancellationRequested)
			{
				BackdropRequest currentRequest = state.CurrentRequest;
				if ((object)currentRequest == null)
				{
					WaitHandle.WaitAny(waitHandles);
					continue;
				}
				long timestamp = Stopwatch.GetTimestamp();
				BackdropFrame frame = backdropCapture.Capture(currentRequest, ++num);
				long timestamp2 = Stopwatch.GetTimestamp();
				lock (gate)
				{
					captureMs.Add((double)(timestamp2 - timestamp) * 1000.0 / (double)Stopwatch.Frequency);
					if (num2 != 0L)
					{
						intervalsMs.Add((double)(timestamp - num2) * 1000.0 / (double)Stopwatch.Frequency);
					}
					num2 = timestamp;
					if (state.Publish(frame))
					{
						published++;
					}
					else
					{
						discarded++;
					}
				}
				double num3 = interval.TotalMilliseconds - (double)(Stopwatch.GetTimestamp() - timestamp) * 1000.0 / (double)Stopwatch.Frequency;
				if (num3 > 0.0)
				{
					long timestamp3 = Stopwatch.GetTimestamp();
					if (cadenceWait != null)
					{
						cadenceWait.Wait(num3, cancel.Token);
					}
					else
					{
						cancel.Token.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(num3));
					}
					lock (gate)
					{
						requestedWaitMs.Add(num3);
						actualWaitMs.Add((double)(Stopwatch.GetTimestamp() - timestamp3) * 1000.0 / (double)Stopwatch.Frequency);
					}
				}
			}
		}
		catch (Exception ex)
		{
			lock (gate)
			{
				failure = ex.Message;
			}
			state.Dispose();
		}
		finally
		{
			state.RetireWaitHandle();
		}
	}

	internal bool Stop(TimeSpan timeout)
	{
		state.Dispose();
		cancel.Cancel();
		return thread.Join(timeout);
	}

	internal object Statistics()
	{
		lock (gate)
		{
			return new
			{
				published = published,
				discarded = discarded,
				failure = failure,
				capture = DiagnosticMath.Distribution(captureMs),
				captureIntervals = DiagnosticMath.Distribution(intervalsMs),
				requestedWait = DiagnosticMath.Distribution(requestedWaitMs),
				actualWait = DiagnosticMath.Distribution(actualWaitMs)
			};
		}
	}

	public void Dispose()
	{
		if (Stop(TimeSpan.FromSeconds(1L)))
		{
			cancel.Dispose();
		}
	}
}
