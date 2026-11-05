using System;

namespace GlassDesk.Dock;

public sealed class AutoHideController
{
	private readonly double _leaveDelayMilliseconds;

	private int _suppressCount;

	private DateTime _leftAtUtc;

	private bool _waiting;

	public AutoHideState State { get; private set; }

	public bool Suppressed => _suppressCount > 0;

	public event EventHandler<AutoHideState>? StateChanged;

	public AutoHideController(double leaveDelayMilliseconds)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(leaveDelayMilliseconds, "leaveDelayMilliseconds");
		_leaveDelayMilliseconds = leaveDelayMilliseconds;
	}

	public void PointerEnter(DateTime utcNow)
	{
		_waiting = false;
		Transition(AutoHideState.Shown);
	}

	public void PointerLeave(DateTime utcNow)
	{
		if (State != AutoHideState.Hidden && !Suppressed && !_waiting)
		{
			_leftAtUtc = utcNow;
			_waiting = true;
			Transition(AutoHideState.WaitingToHide);
		}
	}

	public void Poll(DateTime utcNow)
	{
		if (_waiting && !Suppressed && (utcNow - _leftAtUtc).TotalMilliseconds >= _leaveDelayMilliseconds)
		{
			_waiting = false;
			Transition(AutoHideState.Hidden);
		}
	}

	public bool TryReveal(bool isCovered, DateTime utcNow)
	{
		if (State == AutoHideState.Shown)
		{
			return true;
		}
		if (State == AutoHideState.WaitingToHide)
		{
			PointerEnter(utcNow);
			return true;
		}
		if (isCovered)
		{
			return false;
		}
		PointerEnter(utcNow);
		return true;
	}

	public void PushSuppress(DateTime utcNow)
	{
		_suppressCount++;
	}

	public void PopSuppress(DateTime utcNow)
	{
		if (_suppressCount != 0)
		{
			_suppressCount--;
			if (_suppressCount == 0 && State == AutoHideState.Shown)
			{
				PointerLeave(utcNow);
			}
		}
	}

	public void ForceShown(DateTime utcNow)
	{
		PointerEnter(utcNow);
	}

	public void ForceHide(DateTime utcNow)
	{
		_leftAtUtc = utcNow;
		_waiting = false;
		Transition(AutoHideState.Hidden);
	}

	private void Transition(AutoHideState next)
	{
		if (State != next)
		{
			State = next;
			StateChanged?.Invoke(this, next);
		}
	}
}
