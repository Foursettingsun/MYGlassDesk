using System;
using System.Threading;

namespace GlassDesk.Platform.Shell;

public sealed class PerformanceFullscreenMonitor : IDisposable
{
	private readonly Action<PerformanceFullscreenObservation> _publish;

	private FullscreenWatcher? _watcher;

	private Timer? _fallback;

	private readonly Lock _sync = new Lock();

	private bool _disposed;

	public bool UsesFallback
	{
		get
		{
			using (_sync.EnterScope())
			{
				return _fallback != null;
			}
		}
	}

	public PerformanceFullscreenMonitor(Action<PerformanceFullscreenObservation> publish)
	{
		_publish = publish ?? throw new ArgumentNullException("publish");
		_watcher = FullscreenWatcher.TryStart(null, null, OnInvalidated, null, null, publish);
		if (_watcher == null)
		{
			StartFallback();
		}
	}

	public void Refresh()
	{
		using (_sync.EnterScope())
		{
			if (_disposed)
			{
				return;
			}
		}
		FullscreenWatcher? watcher = _watcher;
		if (watcher != null && watcher.IsAvailable)
		{
			_watcher.Refresh();
			return;
		}
		StartFallback();
		PublishFallback(null);
	}

	private void OnInvalidated()
	{
		FullscreenWatcher watcher = _watcher;
		if (watcher != null && !watcher.IsAvailable)
		{
			StartFallback();
		}
	}

	private void StartFallback()
	{
		using (_sync.EnterScope())
		{
			if (!_disposed && _fallback == null)
			{
				_fallback = new Timer(PublishFallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(1L));
			}
		}
	}

	private void PublishFallback(object? state)
	{
		using (_sync.EnterScope())
		{
			if (_disposed)
			{
				return;
			}
		}
		try
		{
			_publish(FullscreenDetector.ReadPerformanceObservation());
		}
		catch (Exception)
		{
		}
	}

	public void Dispose()
	{
		using (_sync.EnterScope())
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
			_fallback?.Dispose();
			_fallback = null;
		}
		_watcher?.Dispose();
		_watcher = null;
	}
}
