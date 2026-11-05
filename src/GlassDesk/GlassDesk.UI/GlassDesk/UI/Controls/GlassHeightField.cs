using System;
using System.Numerics;
using System.Windows;

namespace GlassDesk.UI.Controls;

public static class GlassHeightField
{
	public static GlassSurfaceSample Sample(Rect boundsDip, double radiusDip, bool squircle, Point pointDip, double normalTransitionDip, double glassThicknessDip)
	{
		return SampleCore(boundsDip, radiusDip, squircle ? GlassFieldShape.Squircle : GlassFieldShape.RoundedRectangle, pointDip, normalTransitionDip, glassThicknessDip, 1.0);
	}

	public static GlassSurfaceSample SamplePhysical(Rect boundsDip, double radiusDip, bool squircle, Point physicalPixel, Size physicalPixelSize, double normalTransitionDip, double glassThicknessDip)
	{
		return SamplePhysical(boundsDip, radiusDip, squircle ? GlassFieldShape.Squircle : GlassFieldShape.RoundedRectangle, physicalPixel, physicalPixelSize, normalTransitionDip, glassThicknessDip);
	}

	internal static GlassSurfaceSample SamplePhysical(Rect boundsDip, double radiusDip, GlassFieldShape shape, Point physicalPixel, Size physicalPixelSize, double normalTransitionDip, double glassThicknessDip)
	{
		return SamplePhysical(boundsDip, radiusDip, shape, physicalPixel, physicalPixelSize, new Rect(boundsDip.X, boundsDip.Y, boundsDip.Width, boundsDip.Height), normalTransitionDip, glassThicknessDip);
	}

	internal static GlassSurfaceSample SamplePhysical(Rect boundsDip, double radiusDip, GlassFieldShape shape, Point physicalPixel, Size physicalPixelSize, Rect sampleBoundsDip, double normalTransitionDip, double glassThicknessDip)
	{
		if (!double.IsFinite(physicalPixelSize.Width) || !double.IsFinite(physicalPixelSize.Height) || physicalPixelSize.Width <= 0.0 || physicalPixelSize.Height <= 0.0 || sampleBoundsDip.IsEmpty || !double.IsFinite(sampleBoundsDip.Width) || !double.IsFinite(sampleBoundsDip.Height) || sampleBoundsDip.Width <= 0.0 || sampleBoundsDip.Height <= 0.0)
		{
			throw new ArgumentOutOfRangeException("physicalPixelSize");
		}
		Point point = new Point(sampleBoundsDip.X + physicalPixel.X * sampleBoundsDip.Width / physicalPixelSize.Width, sampleBoundsDip.Y + physicalPixel.Y * sampleBoundsDip.Height / physicalPixelSize.Height);
		double xSlopeScale = physicalPixelSize.Height / sampleBoundsDip.Height / (physicalPixelSize.Width / sampleBoundsDip.Width);
		return SampleCore(boundsDip, radiusDip, shape, point, normalTransitionDip, glassThicknessDip, xSlopeScale);
	}

	private static GlassSurfaceSample SampleCore(Rect bounds, double radius, GlassFieldShape shape, Point point, double transition, double thickness, double xSlopeScale)
	{
		if (bounds.IsEmpty || !double.IsFinite(bounds.Width) || !double.IsFinite(bounds.Height) || bounds.Width <= 0.0 || bounds.Height <= 0.0 || !double.IsFinite(radius) || !double.IsFinite(transition) || transition <= 0.0 || !double.IsFinite(thickness) || thickness < 0.0)
		{
			throw new ArgumentOutOfRangeException("bounds");
		}
		double signedDistanceDip = Distance(point);
		double heightDip = Height(point);
		double num = Math.Clamp(transition / bounds.Height * 0.13, 0.003, 0.012) * bounds.Height;
		double num2 = (Height(new Point(point.X + num, point.Y)) - Height(new Point(point.X - num, point.Y))) / (2.0 * num);
		double num3 = (Height(new Point(point.X, point.Y + num)) - Height(new Point(point.X, point.Y - num))) / (2.0 * num);
		Vector3 normal = Vector3.Normalize(new Vector3((float)((0.0 - num2) * xSlopeScale), (float)(0.0 - num3), 1f));
		return new GlassSurfaceSample(signedDistanceDip, heightDip, normal);
		double Distance(Point at)
		{
			return shape switch
			{
				GlassFieldShape.Squircle => GlassSignedDistance.Squircle(bounds, radius, at), 
				GlassFieldShape.Continuous => GlassSignedDistance.Continuous(bounds, radius, at), 
				_ => GlassSignedDistance.RoundedRectangle(bounds, radius, at), 
			};
		}
		double Height(Point at)
		{
			return thickness * HeightFractionFromInsideDistance(Math.Max(0.0 - Distance(at), 0.0), transition);
		}
	}

	internal static double HeightFractionFromInsideDistance(double interiorDistanceDip, double transitionDip)
	{
		double num = Math.Clamp(Math.Max(0.0, interiorDistanceDip) / (transitionDip * 0.82), 0.0, 1.0);
		return num * num * num * (num * (num * 6.0 - 15.0) + 10.0);
	}
}
