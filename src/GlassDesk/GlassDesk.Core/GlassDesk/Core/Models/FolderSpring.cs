using System;

namespace GlassDesk.Core.Models;

public sealed class FolderSpring
{
	private double _velocity;

	public double Value { get; private set; }

	public double Target { get; set; }

	public bool IsMoving
	{
		get
		{
			if (!(Math.Abs(Value - Target) > 0.0005))
			{
				return Math.Abs(_velocity) > 0.002;
			}
			return true;
		}
	}

	public void Snap(double value)
	{
		double value2 = (Target = value);
		Value = value2;
		_velocity = 0.0;
	}

	public double Advance(double seconds)
	{
		double num = (double.IsFinite(seconds) ? Math.Clamp(seconds, 0.0, 0.05) : 0.0);
		while (num > 0.0)
		{
			double num2 = Math.Min(num, 1.0 / 120.0);
			_velocity += ((Target - Value) * 260.0 - _velocity * 25.0) * num2;
			Value += _velocity * num2;
			num -= num2;
		}
		if (!IsMoving)
		{
			Snap(Target);
		}
		return Value;
	}
}
