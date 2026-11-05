using System;
using System.Numerics;
using System.Windows;

namespace GlassDesk.UI.Controls;

public static class GlassRayTracer
{
	public static bool TryTrace(Point frontPoint, Vector3 frontNormal, double height, double backgroundDistance, double indexOfRefraction, out Point backgroundPoint)
	{
		backgroundPoint = frontPoint;
		if (!double.IsFinite(frontPoint.X) || !double.IsFinite(frontPoint.Y) || !double.IsFinite(height) || height < 0.0 || !double.IsFinite(backgroundDistance) || backgroundDistance < 0.0 || !double.IsFinite(indexOfRefraction) || indexOfRefraction < 1.0)
		{
			return false;
		}
		if (!TryRefract(-Vector3.UnitZ, frontNormal, 1.0 / indexOfRefraction, out var transmitted) || transmitted.Z >= -1E-05f)
		{
			return false;
		}
		double num = (0.0 - height) / (double)transmitted.Z;
		double num2 = frontPoint.X + (double)transmitted.X * num;
		double num3 = frontPoint.Y + (double)transmitted.Y * num;
		if (!TryRefract(transmitted, -Vector3.UnitZ, indexOfRefraction, out var transmitted2) || transmitted2.Z >= -1E-05f)
		{
			return false;
		}
		double num4 = (0.0 - backgroundDistance) / (double)transmitted2.Z;
		double num5 = num2 + (double)transmitted2.X * num4;
		double num6 = num3 + (double)transmitted2.Y * num4;
		if (!double.IsFinite(num5) || !double.IsFinite(num6))
		{
			return false;
		}
		backgroundPoint = new Point(num5, num6);
		return true;
	}

	internal static bool TryRefract(Vector3 incoming, Vector3 normal, double eta, out Vector3 transmitted)
	{
		transmitted = default;
		if (!float.IsFinite(incoming.X) || !float.IsFinite(incoming.Y) || !float.IsFinite(incoming.Z) || !float.IsFinite(normal.X) || !float.IsFinite(normal.Y) || !float.IsFinite(normal.Z) || incoming.LengthSquared() < 1E-10f || normal.LengthSquared() < 1E-10f || !double.IsFinite(eta) || eta <= 0.0)
		{
			return false;
		}
		Vector3 vector = Vector3.Normalize(incoming);
		if (eta == 1.0)
		{
			transmitted = vector;
			return true;
		}
		Vector3 vector2 = Vector3.Normalize(normal);
		float num = Vector3.Dot(vector, vector2);
		if (num > 0f)
		{
			vector2 = -vector2;
			num = 0f - num;
		}
		double num2 = 1.0 - eta * eta * (double)(1f - num * num);
		if (num2 < 0.0)
		{
			return false;
		}
		Vector3 value = (float)eta * vector - (float)(eta * (double)num + Math.Sqrt(num2)) * vector2;
		if (value.LengthSquared() < 1E-10f)
		{
			return false;
		}
		transmitted = Vector3.Normalize(value);
		if (float.IsFinite(transmitted.X) && float.IsFinite(transmitted.Y))
		{
			return float.IsFinite(transmitted.Z);
		}
		return false;
	}
}
