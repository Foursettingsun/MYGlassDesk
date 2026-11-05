using System;
using System.Windows;
using System.Windows.Media;

namespace GlassDesk.UI.Controls;

public static class ContinuousGlassGeometry
{
	public static Geometry Create(Rect bounds, double radius)
	{
		if (bounds.IsEmpty || bounds.Width <= 0.0 || bounds.Height <= 0.0)
		{
			return Geometry.Empty;
		}
		double num = Radius(bounds, radius);
		double left = bounds.Left;
		double top = bounds.Top;
		double right = bounds.Right;
		double bottom = bounds.Bottom;
		StreamGeometry streamGeometry = new StreamGeometry();
		using (StreamGeometryContext streamGeometryContext = streamGeometry.Open())
		{
			streamGeometryContext.BeginFigure(new Point(left + num, top), isFilled: true, isClosed: true);
			streamGeometryContext.LineTo(new Point(right - num, top), isStroked: true, isSmoothJoin: false);
			streamGeometryContext.BezierTo(new Point(right, top), new Point(right, top), new Point(right, top + num), isStroked: true, isSmoothJoin: false);
			streamGeometryContext.LineTo(new Point(right, bottom - num), isStroked: true, isSmoothJoin: false);
			streamGeometryContext.BezierTo(new Point(right, bottom), new Point(right, bottom), new Point(right - num, bottom), isStroked: true, isSmoothJoin: false);
			streamGeometryContext.LineTo(new Point(left + num, bottom), isStroked: true, isSmoothJoin: false);
			streamGeometryContext.BezierTo(new Point(left, bottom), new Point(left, bottom), new Point(left, bottom - num), isStroked: true, isSmoothJoin: false);
			streamGeometryContext.LineTo(new Point(left, top + num), isStroked: true, isSmoothJoin: false);
			streamGeometryContext.BezierTo(new Point(left, top), new Point(left, top), new Point(left + num, top), isStroked: true, isSmoothJoin: false);
		}
		streamGeometry.Freeze();
		return streamGeometry;
	}

	public static bool Contains(Rect bounds, double radius, Point point)
	{
		if (bounds.IsEmpty || !bounds.Contains(point))
		{
			return false;
		}
		double num = Radius(bounds, radius);
		double num2 = Math.Min(point.X - bounds.Left, bounds.Right - point.X);
		double num3 = Math.Min(point.Y - bounds.Top, bounds.Bottom - point.Y);
		if (num != 0.0 && !(num2 >= num) && !(num3 >= num))
		{
			return Math.Cbrt(num2 / num) + Math.Cbrt(num3 / num) >= 0.9999999999;
		}
		return true;
	}

	internal static bool TryGetEdge(Rect bounds, double radius, Point point, out double depth, out Vector inwardNormal)
	{
		depth = 0.0;
		inwardNormal = default;
		if (!Contains(bounds, radius, point))
		{
			return false;
		}
		double num = point.X - bounds.Left;
		double num2 = point.Y - bounds.Top;
		double num3 = bounds.Right - point.X;
		double num4 = bounds.Bottom - point.Y;
		double x = Math.Min(num, num3);
		double y = Math.Min(num2, num4);
		int num5 = ((num <= num3) ? 1 : (-1));
		int num6 = ((num2 <= num4) ? 1 : (-1));
		double r = Radius(bounds, radius);
		if (x >= r || y >= r || r <= 0.0)
		{
			depth = Math.Min(x, y);
			inwardNormal = ((x < y) ? new Vector(num5, 0.0) : new Vector(0.0, num6));
			return true;
		}
		double num7 = 0.0;
		double num8 = 1.0;
		for (int i = 0; i < 28; i++)
		{
			double num9 = (2.0 * num7 + num8) / 3.0;
			double num10 = (num7 + 2.0 * num8) / 3.0;
			if (Distance(num9) < Distance(num10))
			{
				num8 = num10;
			}
			else
			{
				num7 = num9;
			}
		}
		double num11 = (num7 + num8) / 2.0;
		depth = Math.Sqrt(Distance(num11));
		inwardNormal = new Vector(num11 * num11 * (double)num5, (1.0 - num11) * (1.0 - num11) * (double)num6);
		inwardNormal.Normalize();
		return true;
		double Distance(double t)
		{
			double num12 = r * Math.Pow(1.0 - t, 3.0);
			double num13 = r * t * t * t;
			return (num12 - x) * (num12 - x) + (num13 - y) * (num13 - y);
		}
	}

	private static double Radius(Rect bounds, double radius)
	{
		if (!double.IsFinite(radius))
		{
			return 0.0;
		}
		return Math.Clamp(radius, 0.0, Math.Min(bounds.Width, bounds.Height) / 2.0);
	}
}
