using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

internal sealed class CaptureTraceLog
{
	private readonly object _gate = new object();

	private readonly Dictionary<(string Stage, string Reason), int> _counts = new Dictionary<(string, string), int>();

	private readonly HashSet<long> _nativeSources = new HashSet<long>();

	private readonly HashSet<long> _consumerSources = new HashSet<long>();

	private readonly Queue<CaptureTrace> _events = new Queue<CaptureTrace>();

	private readonly Queue<long> _arrivals = new Queue<long>();

	private readonly Dictionary<(string Stage, string Reason), (int Count, double Total, double Maximum)> _timings = new Dictionary<(string, string), (int, double, double)>();

	private int _nativeAcquisitions;

	private int _consumerReads;

	private int _consumerUnavailable;

	private int _repeatedConsumerReads;

	private bool _sourceLimitReached;

	internal void Observe(CaptureTrace trace)
	{
		lock (_gate)
		{
			(string, string) key = (trace.Stage, trace.Reason);
			_counts[key] = _counts.GetValueOrDefault(key) + 1;
			string stage = trace.Stage;
			bool flag = ((stage == "CaptureCost" || stage == "Processing") ? true : false);
			if (flag && trace.EndTicks > 0 && trace.AtTicks >= trace.EndTicks)
			{
				double num = (double)(trace.AtTicks - trace.EndTicks) * 1000.0 / (double)Stopwatch.Frequency;
				(int, double, double) valueOrDefault = _timings.GetValueOrDefault(key);
				_timings[key] = (valueOrDefault.Item1 + 1, valueOrDefault.Item2 + num, Math.Max(valueOrDefault.Item3, num));
			}
			if (trace.Stage == "Scene" && trace.Reason == "Delivered")
			{
				_nativeAcquisitions++;
				Track(_nativeSources, trace.SourceTicks);
			}
			if (trace.Stage == "Consumer")
			{
				if (trace.Reason == "Delivered")
				{
					_consumerReads++;
					if (_consumerSources.Contains(trace.SourceTicks))
					{
						_repeatedConsumerReads++;
					}
					Track(_consumerSources, trace.SourceTicks);
				}
				else
				{
					_consumerUnavailable++;
				}
			}
			if (trace.Stage == "FrameArrival")
			{
				if (_arrivals.Count == 1024)
				{
					_arrivals.Dequeue();
				}
				_arrivals.Enqueue(trace.AtTicks);
			}
			else if (IsStateEvent(trace))
			{
				if (_events.Count == 256)
				{
					_events.Dequeue();
				}
				_events.Enqueue(trace);
			}
		}
	}

	internal static bool IsStateEvent(CaptureTrace trace)
	{
		bool flag = trace.Reason != "Delivered";
		if (flag)
		{
			bool flag2;
			switch (trace.Stage)
			{
			case "Acquisition":
			case "CaptureCost":
			case "SourceClock":
			case "NativeWait":
			case "Processing":
			case "Consumer":
			case "Readback":
			case "ReadbackResource":
			case "FrameArrival":
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			flag = !flag2;
		}
		if (flag && (!(trace.Stage == "SceneSnapshot") || !(trace.Reason == "Completed")) && (!(trace.Stage == "NativeQueue") || !(trace.Reason == "EmptyPool")))
		{
			if (trace.Stage == "Scene")
			{
				return !(trace.Reason == "Unavailable");
			}
			return true;
		}
		return false;
	}

	private void Track(HashSet<long> sources, long ticks)
	{
		if (ticks != 0L && !sources.Contains(ticks))
		{
			if (sources.Count < 8192)
			{
				sources.Add(ticks);
			}
			else
			{
				_sourceLimitReached = true;
			}
		}
	}

	internal CaptureTraceSummary Snapshot()
	{
		lock (_gate)
		{
			long num = Stopwatch.GetTimestamp() - Stopwatch.Frequency * 60 * 15;
			while (_events.Count > 0 && _events.Peek().AtTicks < num)
			{
				_events.Dequeue();
			}
			while (_arrivals.Count > 0 && _arrivals.Peek() < num)
			{
				_arrivals.Dequeue();
			}
			return new CaptureTraceSummary((from x in _counts
				orderby x.Key.Stage, x.Key.Reason
				select new CaptureTraceCount(x.Key.Stage, x.Key.Reason, x.Value)).ToArray(), _nativeAcquisitions, _nativeSources.Count, _consumerReads, _consumerSources.Count, _repeatedConsumerReads, _consumerUnavailable, _sourceLimitReached, _events.ToArray(), _arrivals.ToArray(), (from x in _timings
				orderby x.Key.Stage, x.Key.Reason
				select new CaptureTimingCount(x.Key.Stage, x.Key.Reason, x.Value.Count, x.Value.Total / (double)x.Value.Count, x.Value.Maximum)).ToArray());
		}
	}
}
