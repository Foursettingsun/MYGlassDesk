using System;

namespace GlassDesk.Core.Geometry;

public static class RoundedRect
{
	public static bool Contains(double x, double y, double width, double height, double radius)
	{
		if (width <= 0.0 || height <= 0.0)
		{
			return false;
		}
		if (x < 0.0 || y < 0.0 || x > width || y > height)
		{
			return false;
		}
		double num = Math.Min(radius, Math.Min(width, height) / 2.0);
		if (num <= 0.0)
		{
			return true;
		}
		bool flag = x < num;
		bool flag2 = x > width - num;
		bool flag3 = y < num;
		bool flag4 = y > height - num;
		if ((!flag && !flag2) || (!flag3 && !flag4))
		{
			return true;
		}
		double num2 = (flag ? num : (width - num));
		double num3 = (flag3 ? num : (height - num));
		double num4 = x - num2;
		double num5 = y - num3;
		return num4 * num4 + num5 * num5 <= (num + 0.5) * (num + 0.5);
	}
}
