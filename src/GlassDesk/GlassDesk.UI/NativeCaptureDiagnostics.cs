using System;
using System.Collections.Generic;
using System.Diagnostics;

internal sealed class NativeCaptureDiagnostics
{
	private readonly Queue<CaptureTrace> _events = new Queue<CaptureTrace>();

	private (bool Black, bool Transparent)? _pixelState;

	internal void Observe(CaptureTrace trace)
	{
		if (CaptureTraceLog.IsStateEvent(trace))
		{
			if (_events.Count == 64)
			{
				_events.Dequeue();
			}
			_events.Enqueue(trace);
		}
	}

	internal CaptureTrace[] Drain()
	{
		CaptureTrace[] result = _events.ToArray();
		_events.Clear();
		return result;
	}

	internal void ObservePixels(CapturePixels frame, long generation)
	{
		byte[] pixels = frame.Pixels;
		int num = pixels.Length / 4;
		int num2 = Math.Max(1, num / 512);
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		long num6 = 0L;
		for (int i = 0; i < num; i += num2)
		{
			int num7 = i * 4;
			num3++;
			if (Math.Max(pixels[num7], Math.Max(pixels[num7 + 1], pixels[num7 + 2])) <= 3)
			{
				num4++;
			}
			if (pixels[num7 + 3] == 0)
			{
				num5++;
			}
			num6 += pixels[num7] + pixels[num7 + 1] + pixels[num7 + 2];
		}
		double num8 = (double)num4 / (double)Math.Max(1, num3);
		double num9 = (double)num5 / (double)Math.Max(1, num3);
		(bool, bool) tuple = (num8 >= 0.95, num9 > 0.0);
		(bool, bool)? pixelState = _pixelState;
		(bool, bool) tuple2 = tuple;
		bool hasValue = pixelState.HasValue;
		if (hasValue)
		{
			if (!hasValue)
			{
				return;
			}
			(bool, bool) valueOrDefault = pixelState.GetValueOrDefault();
			if (valueOrDefault.Item1 == tuple2.Item1 && valueOrDefault.Item2 == tuple2.Item2)
			{
				return;
			}
		}
		_pixelState = tuple;
		long timestamp = Stopwatch.GetTimestamp();
		long sourceQpcTicks = frame.SourceQpcTicks;
		long endTicks = 0L;
		long barrierTicks = 0L;
		long windowHandle = 0L;
		int width = frame.Width;
		int height = frame.Height;
		double nearBlackFraction = num8;
		double transparentFraction = num9;
		double meanChannel = (double)num6 / (double)Math.Max(1, num3 * 3);
		Observe(new CaptureTrace("NativePixels", "StatisticsChanged", timestamp, sourceQpcTicks, endTicks, barrierTicks, generation, 0, windowHandle, 0u, null, 0, 0, null, null, null, null, null, 0, width, height, nearBlackFraction, transparentFraction, meanChannel));
	}
}
