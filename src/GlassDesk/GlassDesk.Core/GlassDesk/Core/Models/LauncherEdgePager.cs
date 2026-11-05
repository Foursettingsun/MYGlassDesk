using System;

namespace GlassDesk.Core.Models;

public sealed class LauncherEdgePager(TimeSpan threshold)
{
	private int _direction;

	private DateTimeOffset _enteredAt;

	private bool _fired;

	public bool Tick(int direction, DateTimeOffset now)
	{
		if ((direction < -1 || direction > 1) ? true : false)
		{
			throw new ArgumentOutOfRangeException("direction");
		}
		if (direction != _direction)
		{
			_direction = direction;
			_enteredAt = now;
			_fired = false;
		}
		if (direction == 0 || _fired || now - _enteredAt < threshold)
		{
			return false;
		}
		_fired = true;
		return true;
	}

	public void Reset()
	{
		_direction = 0;
	}
}
