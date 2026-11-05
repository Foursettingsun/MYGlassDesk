using System;

namespace GlassDesk.App;

internal sealed class PerformanceCoordinator(DateTimeOffset now)
{
	private static readonly TimeSpan EnterFullscreenDelay = TimeSpan.FromMilliseconds(500L);

	private static readonly TimeSpan LeaveFullscreenDelay = TimeSpan.FromMilliseconds(300L);

	private static readonly TimeSpan KeepWarmDelay = TimeSpan.FromSeconds(5L);

	private bool _observedFullscreen;

	private bool _stableFullscreen;

	private bool _dockVisible = true;

	private bool _launcherVisible;

	private bool _temporaryActivation;

	private bool _sessionSuspended;

	private bool _desktopLayerPinned;

	private DateTimeOffset? _fullscreenDeadline;

	private DateTimeOffset? _hiddenSince;

	public PerformanceState State { get; private set; }

	public long Generation { get; private set; }

	public bool AutoSuspend { get; private set; } = true;

	public bool ManualSleep { get; private set; }

	public bool IsSuppressionRequested
	{
		get
		{
			if (!ManualSleep && !_sessionSuspended)
			{
				if (AutoSuspend && _stableFullscreen)
				{
					return !_desktopLayerPinned;
				}
				return false;
			}
			return true;
		}
	}

	public bool IsTemporaryActivation => _temporaryActivation;

	public DateTimeOffset? NextTransitionAt
	{
		get
		{
			DateTimeOffset? result = ((State == PerformanceState.WarmHidden && _hiddenSince.HasValue) ? new DateTimeOffset?(_hiddenSince.Value + KeepWarmDelay) : ((DateTimeOffset?)null));
			if (!_fullscreenDeadline.HasValue)
			{
				return result;
			}
			if (!result.HasValue)
			{
				return _fullscreenDeadline;
			}
			if (!(_fullscreenDeadline.Value < result.Value))
			{
				return result;
			}
			return _fullscreenDeadline;
		}
	}

	public void ObserveForeground(bool fullscreen, bool belongsToCurrentProcess, DateTimeOffset now)
	{
		if (!belongsToCurrentProcess)
		{
			if (_observedFullscreen != fullscreen)
			{
				_observedFullscreen = fullscreen;
				_fullscreenDeadline = ((_stableFullscreen == fullscreen) ? ((DateTimeOffset?)null) : new DateTimeOffset?(now + (fullscreen ? EnterFullscreenDelay : LeaveFullscreenDelay)));
			}
			Advance(now);
		}
	}

	public void SetAutoSuspend(bool enabled, DateTimeOffset now)
	{
		AutoSuspend = enabled;
		Advance(now);
	}

	public void SetDesktopLayerPinned(bool pinned, DateTimeOffset now)
	{
		_desktopLayerPinned = pinned;
		Advance(now);
	}

	public void SetManualSleep(bool enabled, DateTimeOffset now)
	{
		ManualSleep = enabled;
		if (enabled)
		{
			_temporaryActivation = false;
		}
		Advance(now);
	}

	public void SetSessionSuspended(bool suspended, DateTimeOffset now)
	{
		_sessionSuspended = suspended;
		if (suspended)
		{
			_temporaryActivation = false;
		}
		Advance(now);
	}

	public void SetVisibleSurfaces(bool dockVisible, bool launcherVisible, DateTimeOffset now)
	{
		if (_launcherVisible && !launcherVisible)
		{
			_temporaryActivation = false;
		}
		_dockVisible = dockVisible;
		_launcherVisible = launcherVisible;
		if (dockVisible | launcherVisible)
		{
			_hiddenSince = null;
		}
		else
		{
			DateTimeOffset valueOrDefault = _hiddenSince.GetValueOrDefault();
			if (!_hiddenSince.HasValue)
			{
				valueOrDefault = now;
				_hiddenSince = valueOrDefault;
			}
		}
		Advance(now);
	}

	public void RequestTemporaryActivation(DateTimeOffset now)
	{
		if (!_sessionSuspended)
		{
			_temporaryActivation = true;
			_launcherVisible = true;
			_hiddenSince = null;
			if (State == PerformanceState.Sleeping)
			{
				Transition(PerformanceState.Waking);
			}
			else
			{
				Advance(now);
			}
		}
	}

	public void CompleteWake(DateTimeOffset now)
	{
		if (State == PerformanceState.Waking)
		{
			Transition((_temporaryActivation && IsSuppressionRequested) ? PerformanceState.TemporaryActive : PerformanceState.Active);
			Advance(now);
		}
	}

	public void Advance(DateTimeOffset now)
	{
		DateTimeOffset? fullscreenDeadline = _fullscreenDeadline;
		if (fullscreenDeadline.HasValue)
		{
			DateTimeOffset valueOrDefault = fullscreenDeadline.GetValueOrDefault();
			if (now >= valueOrDefault)
			{
				_stableFullscreen = _observedFullscreen;
				_fullscreenDeadline = null;
			}
		}
		if ((IsSuppressionRequested && !_temporaryActivation) || _sessionSuspended)
		{
			Transition(PerformanceState.Sleeping);
		}
		else if (_dockVisible || _launcherVisible || _temporaryActivation)
		{
			_hiddenSince = null;
			if (State == PerformanceState.Sleeping)
			{
				Transition(PerformanceState.Waking);
			}
			else if (State != PerformanceState.Waking)
			{
				Transition((_temporaryActivation && IsSuppressionRequested) ? PerformanceState.TemporaryActive : PerformanceState.Active);
			}
		}
		else
		{
			DateTimeOffset valueOrDefault2 = _hiddenSince.GetValueOrDefault();
			if (!_hiddenSince.HasValue)
			{
				valueOrDefault2 = now;
				_hiddenSince = valueOrDefault2;
			}
			Transition((!(now >= _hiddenSince.Value + KeepWarmDelay)) ? PerformanceState.WarmHidden : PerformanceState.Sleeping);
		}
	}

	private void Transition(PerformanceState state)
	{
		if (State != state)
		{
			State = state;
			Generation++;
		}
	}
}
