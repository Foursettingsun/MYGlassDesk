using System;

namespace GlassDesk.Dock;

internal static class DockHoverScale
{
	internal static double Advance(double current, double target, double deltaSeconds)
	{
		double num = ((deltaSeconds <= 0.0) ? current : (current + (target - current) * (1.0 - Math.Exp((0.0 - deltaSeconds) / 0.075))));
		if (!(Math.Abs(target - num) <= 0.001))
		{
			return num;
		}
		return target;
	}
}
