using System;
using System.Windows;

namespace GlassDesk.UI.Controls;

public static class GlassSignedDistance
{
	public static double Circle(Point point, Point center, double radius)
	{
		return (point - center).Length - radius;
	}

	public static double RoundedRectangle(Rect bounds, double radius, Point point)
	{
		double num = Math.Clamp(radius, 0.0, Math.Min(bounds.Width, bounds.Height) / 2.0);
		double val = Math.Abs(point.X - (bounds.Left + bounds.Right) / 2.0) - bounds.Width / 2.0 + num;
		double num2 = Math.Abs(point.Y - (bounds.Top + bounds.Bottom) / 2.0) - bounds.Height / 2.0 + num;
		return Math.Min(Math.Max(val, num2), 0.0) + Math.Sqrt(Math.Max(val, 0.0) * Math.Max(val, 0.0) + Math.Max(num2, 0.0) * Math.Max(num2, 0.0)) - num;
	}

	public static double Squircle(Rect bounds, double radius, Point point)
	{
		if (bounds.IsEmpty || bounds.Width <= 0.0 || bounds.Height <= 0.0)
		{
			throw new ArgumentOutOfRangeException("bounds");
		}
		double r = (double.IsFinite(radius) ? Math.Clamp(radius, 0.0, Math.Min(bounds.Width, bounds.Height) / 2.0) : 0.0);
		if (r == 0.0)
		{
			return RoundedRectangle(bounds, 0.0, point);
		}
		double x = Math.Min(point.X - bounds.Left, bounds.Right - point.X);
		double y = Math.Min(point.Y - bounds.Top, bounds.Bottom - point.Y);
		if (x >= r || y >= r)
		{
			return 0.0 - Math.Min(x, y);
		}
		double num = 0.0;
		double num2 = 1.0;
		double num3 = num2 - (num2 - num) * 0.6180339887498949;
		double num4 = num + (num2 - num) * 0.6180339887498949;
		double num5 = DistanceSquared(num3);
		double num6 = DistanceSquared(num4);
		for (int i = 0; i < 28; i++)
		{
			if (num5 < num6)
			{
				num2 = num4;
				num4 = num3;
				num6 = num5;
				num3 = num2 - (num2 - num) * 0.6180339887498949;
				num5 = DistanceSquared(num3);
			}
			else
			{
				num = num3;
				num3 = num4;
				num5 = num6;
				num4 = num + (num2 - num) * 0.6180339887498949;
				num6 = DistanceSquared(num4);
			}
		}
		double num7 = Math.Sqrt(Math.Min(Math.Min(num5, num6), Math.Min(DistanceSquared(0.0), DistanceSquared(1.0))));
		if (!SquircleGlassGeometry.Contains(bounds, r, point))
		{
			return num7;
		}
		return 0.0 - num7;
		double DistanceSquared(double t)
		{
			double num8 = r * (1.0 - Math.Pow(t, 0.3846153846153846));
			double num9 = r * (1.0 - Math.Pow(1.0 - t, 0.3846153846153846));
			double num10 = num8 - x;
			double num11 = num9 - y;
			return num10 * num10 + num11 * num11;
		}
	}

	public static double Continuous(Rect bounds, double radius, Point point)
	{
		if (bounds.IsEmpty || bounds.Width <= 0.0 || bounds.Height <= 0.0)
		{
			throw new ArgumentOutOfRangeException("bounds");
		}
		double r = (double.IsFinite(radius) ? Math.Clamp(radius, 0.0, Math.Min(bounds.Width, bounds.Height) / 2.0) : 0.0);
		if (r == 0.0)
		{
			return RoundedRectangle(bounds, 0.0, point);
		}
		double x = Math.Min(point.X - bounds.Left, bounds.Right - point.X);
		double y = Math.Min(point.Y - bounds.Top, bounds.Bottom - point.Y);
		if (x >= r || y >= r)
		{
			return 0.0 - Math.Min(x, y);
		}
		double num = 0.0;
		double num2 = 1.0;
		double num3 = num2 - 0.6180339887498949;
		double num4 = 0.6180339887498949;
		double num5 = DistanceSquared(num3);
		double num6 = DistanceSquared(num4);
		for (int i = 0; i < 28; i++)
		{
			if (num5 < num6)
			{
				num2 = num4;
				num4 = num3;
				num6 = num5;
				num3 = num2 - (num2 - num) * 0.6180339887498949;
				num5 = DistanceSquared(num3);
			}
			else
			{
				num = num3;
				num3 = num4;
				num5 = num6;
				num4 = num + (num2 - num) * 0.6180339887498949;
				num6 = DistanceSquared(num4);
			}
		}
		double num7 = Math.Sqrt(Math.Min(Math.Min(num5, num6), Math.Min(DistanceSquared(0.0), DistanceSquared(1.0))));
		if (!ContinuousGlassGeometry.Contains(bounds, r, point))
		{
			return num7;
		}
		return 0.0 - num7;
		double DistanceSquared(double t)
		{
			double num8 = 1.0 - t;
			double num9 = r * num8 * num8 * num8;
			double num10 = r * t * t * t;
			double num11 = num9 - x;
			double num12 = num10 - y;
			return num11 * num11 + num12 * num12;
		}
	}

	public static double SmoothUnion(double a, double b, double radius)
	{
		if (radius <= 0.0)
		{
			return Math.Min(a, b);
		}
		double num = Math.Clamp(0.5 + 0.5 * (b - a) / radius, 0.0, 1.0);
		return b + (a - b) * num - radius * num * (1.0 - num);
	}
}
