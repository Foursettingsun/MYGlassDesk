using System;
using System.Diagnostics;
using System.Windows.Media;

namespace GlassDesk.Dock;

internal sealed class DockRenderLoop(Func<double, bool> advance)
{
	private bool _active;

	private long _timestamp;

	private TimeSpan _lastRenderingTime = TimeSpan.MinValue;

	internal void Start()
	{
		if (!_active)
		{
			_active = true;
			_timestamp = Stopwatch.GetTimestamp();
			_lastRenderingTime = TimeSpan.MinValue;
			CompositionTarget.Rendering += OnRendering;
		}
	}

	internal void Stop()
	{
		if (_active)
		{
			CompositionTarget.Rendering -= OnRendering;
			_active = false;
			_timestamp = 0L;
			_lastRenderingTime = TimeSpan.MinValue;
		}
	}

	private void OnRendering(object? sender, EventArgs e)
	{
		if (e is RenderingEventArgs e2)
		{
			if (e2.RenderingTime == _lastRenderingTime)
			{
				return;
			}
			_lastRenderingTime = e2.RenderingTime;
		}
		long timestamp = Stopwatch.GetTimestamp();
		double arg = Math.Clamp(Stopwatch.GetElapsedTime(_timestamp, timestamp).TotalSeconds, 0.0, 0.1);
		_timestamp = timestamp;
		if (advance(arg))
		{
			Stop();
		}
	}
}
