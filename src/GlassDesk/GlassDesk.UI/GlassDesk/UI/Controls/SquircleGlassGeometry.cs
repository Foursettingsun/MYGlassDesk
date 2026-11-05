using System;
using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Media;

namespace GlassDesk.UI.Controls;

public static class SquircleGlassGeometry
{
	private readonly record struct CubicCorner(Point Start, Point Control1, Point Control2, Point End);

	public const double Exponent = 2.6;

	private const int MinimumSegmentsPerCorner = 64;

	private const int MaximumSegmentsPerCorner = 256;

	private static readonly ConcurrentDictionary<int, Point[]> CornerSamples = new ConcurrentDictionary<int, Point[]>();

	private static readonly ConcurrentDictionary<int, CubicCorner[]> CompactCorners = new ConcurrentDictionary<int, CubicCorner[]>();

	public static Geometry Create(Rect bounds, double radius)
	{
		if (bounds.IsEmpty || bounds.Width <= 0.0 || bounds.Height <= 0.0)
		{
			return Geometry.Empty;
		}
		double num = Radius(bounds, radius);
		StreamGeometry streamGeometry = new StreamGeometry();
		using (StreamGeometryContext streamGeometryContext = streamGeometry.Open())
		{
			if (num == 0.0)
			{
				streamGeometryContext.BeginFigure(new Point(bounds.Left, bounds.Top), isFilled: true, isClosed: true);
				streamGeometryContext.LineTo(new Point(bounds.Right, bounds.Top), isStroked: true, isSmoothJoin: false);
				streamGeometryContext.LineTo(new Point(bounds.Right, bounds.Bottom), isStroked: true, isSmoothJoin: false);
				streamGeometryContext.LineTo(new Point(bounds.Left, bounds.Bottom), isStroked: true, isSmoothJoin: false);
			}
			else
			{
				streamGeometryContext.BeginFigure(new Point(bounds.Left + num, bounds.Top), isFilled: true, isClosed: true);
				streamGeometryContext.LineTo(new Point(bounds.Right - num, bounds.Top), isStroked: true, isSmoothJoin: false);
				AddCorner(streamGeometryContext, new Point(bounds.Right - num, bounds.Top + num), num, 0);
				streamGeometryContext.LineTo(new Point(bounds.Right, bounds.Bottom - num), isStroked: true, isSmoothJoin: false);
				AddCorner(streamGeometryContext, new Point(bounds.Right - num, bounds.Bottom - num), num, 1);
				streamGeometryContext.LineTo(new Point(bounds.Left + num, bounds.Bottom), isStroked: true, isSmoothJoin: false);
				AddCorner(streamGeometryContext, new Point(bounds.Left + num, bounds.Bottom - num), num, 2);
				streamGeometryContext.LineTo(new Point(bounds.Left, bounds.Top + num), isStroked: true, isSmoothJoin: false);
				AddCorner(streamGeometryContext, new Point(bounds.Left + num, bounds.Top + num), num, 3);
			}
		}
		return Freeze(streamGeometry);
		static Geometry Freeze(StreamGeometry shape)
		{
			shape.Freeze();
			return shape;
		}
	}

	public static Geometry CreateCompact(Rect bounds, double radius)
	{
		if (bounds.IsEmpty || bounds.Width <= 0.0 || bounds.Height <= 0.0)
		{
			return Geometry.Empty;
		}
		double num = Radius(bounds, radius);
		if (num == 0.0)
		{
			return Create(bounds, 0.0);
		}
		int num2 = 1;
		while (num > 256.0 * (double)num2 * (double)num2 && num2 < 32)
		{
			num2 *= 2;
		}
		CubicCorner[] orAdd = CompactCorners.GetOrAdd(num2, BuildCompactCorner);
		StreamGeometry streamGeometry = new StreamGeometry();
		using (StreamGeometryContext streamGeometryContext = streamGeometry.Open())
		{
			streamGeometryContext.BeginFigure(new Point(bounds.Left + num, bounds.Top), isFilled: true, isClosed: true);
			streamGeometryContext.LineTo(new Point(bounds.Right - num, bounds.Top), isStroked: true, isSmoothJoin: false);
			AddCompactCorner(streamGeometryContext, new Point(bounds.Right - num, bounds.Top + num), num, 0, orAdd);
			streamGeometryContext.LineTo(new Point(bounds.Right, bounds.Bottom - num), isStroked: true, isSmoothJoin: false);
			AddCompactCorner(streamGeometryContext, new Point(bounds.Right - num, bounds.Bottom - num), num, 1, orAdd);
			streamGeometryContext.LineTo(new Point(bounds.Left + num, bounds.Bottom), isStroked: true, isSmoothJoin: false);
			AddCompactCorner(streamGeometryContext, new Point(bounds.Left + num, bounds.Bottom - num), num, 2, orAdd);
			streamGeometryContext.LineTo(new Point(bounds.Left, bounds.Top + num), isStroked: true, isSmoothJoin: false);
			AddCompactCorner(streamGeometryContext, new Point(bounds.Left + num, bounds.Top + num), num, 3, orAdd);
		}
		streamGeometry.Freeze();
		return streamGeometry;
	}

	private static void AddCompactCorner(StreamGeometryContext context, Point center, double radius, int quarterTurn, CubicCorner[] curves)
	{
		for (int i = 0; i < curves.Length; i++)
		{
			CubicCorner cubicCorner = curves[i];
			context.BezierTo(Transform(Scale(cubicCorner.Control1), center, quarterTurn), Transform(Scale(cubicCorner.Control2), center, quarterTurn), Transform(Scale(cubicCorner.End), center, quarterTurn), isStroked: true, isSmoothJoin: false);
		}
		Point Scale(Point point)
		{
			return new Point(point.X * radius, point.Y * radius);
		}
	}

	private static CubicCorner[] BuildCompactCorner(int subdivisions)
	{
		double[] array = new double[5] { 0.0, 0.12, 0.45, 0.78, 1.0 };
		double num = Math.Pow(0.5, 0.3846153846153846);
		int num2 = 4 * subdivisions;
		double[] array2 = new double[num2 + 1];
		double[] array3 = new double[num2 + 1];
		for (int i = 0; i < num2; i++)
		{
			int num3 = i / subdivisions;
			double num4 = (double)(i % subdivisions) / (double)subdivisions;
			array2[i] = num * (array[num3] + num4 * (array[num3 + 1] - array[num3]));
			array3[i] = 0.0 - Math.Pow(1.0 - Math.Pow(array2[i], 2.6), 0.3846153846153846);
		}
		array2[num2] = num;
		array3[num2] = 0.0 - num;
		double[] array4 = SolveCornerSpline(array2, array3);
		double[] array5 = (double[])array3.Clone();
		array5[1]++;
		double[] array6 = SolveCornerSpline(array2, array5);
		array3[1] -= array4[0] / (array6[0] - array4[0]);
		array4 = SolveCornerSpline(array2, array3);
		array4[0] = 0.0;
		CubicCorner[] array7 = new CubicCorner[num2 * 2];
		for (int j = 0; j < num2; j++)
		{
			double num5 = array2[j + 1] - array2[j];
			double num6 = (array3[j + 1] - array3[j]) / num5;
			double num7 = num6 - num5 * (2.0 * array4[j] + array4[j + 1]) / 6.0;
			double num8 = num6 + num5 * (array4[j] + 2.0 * array4[j + 1]) / 6.0;
			Point control = new Point(array2[j] + num5 / 3.0, array3[j] + num5 * num7 / 3.0);
			Point control2 = new Point(array2[j + 1] - num5 / 3.0, array3[j + 1] - num5 * num8 / 3.0);
			if (j == 0)
			{
				control.Y = -1.0;
				control2.Y = -1.0;
			}
			if (j == num2 - 1)
			{
				control2 = new Point(num - num5 / 3.0, 0.0 - num - num5 / 3.0);
			}
			array7[j] = new CubicCorner(new Point(array2[j], array3[j]), control, control2, new Point(array2[j + 1], array3[j + 1]));
		}
		for (int k = 0; k < num2; k++)
		{
			CubicCorner cubicCorner = array7[num2 - 1 - k];
			array7[num2 + k] = new CubicCorner(Reflect(cubicCorner.End), Reflect(cubicCorner.Control2), Reflect(cubicCorner.Control1), Reflect(cubicCorner.Start));
		}
		return array7;
		static Point Reflect(Point point)
		{
			return new Point(0.0 - point.Y, 0.0 - point.X);
		}
	}

	private static double[] SolveCornerSpline(double[] x, double[] y)
	{
		int num = x.Length - 1;
		double[] array = new double[num + 1];
		double[] array2 = new double[num + 1];
		double[] array3 = new double[num + 1];
		double num2 = x[1] - x[0];
		array[0] = 2.0 * num2;
		array2[0] = num2;
		array3[0] = 6.0 * (y[1] - y[0]) / num2;
		for (int i = 1; i <= num; i++)
		{
			double num3 = x[i] - x[i - 1];
			if (i == num)
			{
				array[i] = 2.0 * num3;
				array3[i] = 6.0 * (1.0 - (y[i] - y[i - 1]) / num3);
			}
			else
			{
				double num4 = x[i + 1] - x[i];
				array[i] = 2.0 * (num3 + num4);
				array2[i] = num4;
				array3[i] = 6.0 * ((y[i + 1] - y[i]) / num4 - (y[i] - y[i - 1]) / num3);
			}
			double num5 = num3 / array[i - 1];
			array[i] -= num5 * array2[i - 1];
			array3[i] -= num5 * array3[i - 1];
		}
		double[] array4 = new double[num + 1];
		array4[num] = array3[num] / array[num];
		for (int num6 = num - 1; num6 >= 0; num6--)
		{
			array4[num6] = (array3[num6] - array2[num6] * array4[num6 + 1]) / array[num6];
		}
		return array4;
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
			return Math.Pow(1.0 - num2 / num, 2.6) + Math.Pow(1.0 - num3 / num, 2.6) <= 1.000000000001;
		}
		return true;
	}

	public static bool TryGetEdge(Rect bounds, double radius, Point point, out double depth, out Vector inwardNormal)
	{
		depth = 0.0;
		inwardNormal = default;
		if (!Contains(bounds, radius, point))
		{
			return false;
		}
		double num = point.X - bounds.Left;
		double num2 = bounds.Right - point.X;
		double num3 = point.Y - bounds.Top;
		double num4 = bounds.Bottom - point.Y;
		double num5 = Math.Min(num, num2);
		double num6 = Math.Min(num3, num4);
		int num7 = ((num <= num2) ? 1 : (-1));
		int num8 = ((num3 <= num4) ? 1 : (-1));
		double num9 = Radius(bounds, radius);
		if (num9 <= 0.0 || num5 >= num9 || num6 >= num9)
		{
			if (num5 < num6)
			{
				depth = num5;
				inwardNormal = new Vector(num7, 0.0);
			}
			else
			{
				depth = num6;
				inwardNormal = new Vector(0.0, num8);
			}
			return true;
		}
		double x = 1.0 - num5 / num9;
		double x2 = 1.0 - num6 / num9;
		double num10 = Math.Pow(x, 2.6);
		double num11 = Math.Pow(x2, 2.6);
		double num12 = 2.6 * Math.Pow(x, 1.6) / num9;
		double num13 = 2.6 * Math.Pow(x2, 1.6) / num9;
		double num14 = Math.Sqrt(num12 * num12 + num13 * num13);
		depth = ((num14 > 0.0) ? ((1.0 - num10 - num11) / num14) : 0.0);
		inwardNormal = new Vector((double)num7 * num12, (double)num8 * num13);
		inwardNormal.Normalize();
		return true;
	}

	private static void AddCorner(StreamGeometryContext context, Point center, double radius, int quarterTurn)
	{
		int key = Math.Clamp((int)Math.Ceiling(radius * 3.0), 64, 256);
		Point[] orAdd = CornerSamples.GetOrAdd(key, (int segments) =>
		{
			Point[] array = new Point[segments];
			for (int i = 1; i <= segments; i++)
			{
				double num2 = (double)i / (double)segments;
				double num3 = num2 * num2 * (3.0 - 2.0 * num2) * Math.PI / 2.0;
				array[i - 1] = ((i == segments) ? new Point(1.0, 0.0) : new Point(Math.Pow(Math.Sin(num3), 0.7692307692307692), 0.0 - Math.Pow(Math.Cos(num3), 0.7692307692307692)));
			}
			return array;
		});
		for (int num = 0; num < orAdd.Length; num++)
		{
			Point point = orAdd[num];
			Point point2 = new Point(radius * point.X, radius * point.Y);
			context.LineTo(Transform(point2, center, quarterTurn), isStroked: true, isSmoothJoin: false);
		}
	}

	private static Point Transform(Point point, Point center, int quarterTurn)
	{
		return center + TransformVector(new Vector(point.X, point.Y), quarterTurn);
	}

	private static Vector TransformVector(Vector vector, int quarterTurn)
	{
		return quarterTurn switch
		{
			0 => vector, 
			1 => new Vector(0.0 - vector.Y, vector.X), 
			2 => new Vector(0.0 - vector.X, 0.0 - vector.Y), 
			_ => new Vector(vector.Y, 0.0 - vector.X), 
		};
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
