using System;
using GlassDesk.Platform.Displays;

namespace GlassDesk.UI.Controls;

public readonly record struct ContainerTransformFrame(PixelRect Bounds, double CornerRadius, double Opacity, double GlassStrength, double BlurRadius, double ContentOpacity, double ContentScale)
{
	public const double DurationMilliseconds = 420.0;

	private const double ContentRevealStart = 0.04;

	public static ContainerTransformFrame Create(PixelRect source, PixelRect destination, double progress, double sourceRadius, double destinationRadius)
	{
		progress = (double.IsFinite(progress) ? Math.Clamp(progress, 0.0, 1.0) : 0.0);
		double num = EaseOutCubic(progress);
		(double, double) tuple = ContentForRow(progress, 0, 1);
		PixelRect bounds;
		if (progress <= 0.0)
		{
			bounds = source;
		}
		else
		{
			bounds = ((!(progress >= 1.0)) ? new PixelRect(Lerp(source.X, destination.X, num), Lerp(source.Y, destination.Y, num), Math.Max(1, Lerp(source.Width, destination.Width, num)), Math.Max(1, Lerp(source.Height, destination.Height, num))) : destination);
		}
		double num2 = (double.IsFinite(sourceRadius) ? Math.Max(0.0, sourceRadius) : 0.0);
		double num3 = (double.IsFinite(destinationRadius) ? Math.Max(0.0, destinationRadius) : 0.0);
		double cornerRadius;
		if (progress <= 0.0)
		{
			cornerRadius = num2;
		}
		else
		{
			cornerRadius = ((progress >= 1.0) ? num3 : (num2 + (num3 - num2) * num));
		}
		return new ContainerTransformFrame(bounds, cornerRadius, 1.0, num, 0.8 * (1.0 - num), tuple.Item1, tuple.Item2);
	}

	public static (double Opacity, double Scale) ContentForRow(double timeline, int row, int rows)
	{
		timeline = (double.IsFinite(timeline) ? Math.Clamp(timeline, 0.0, 1.0) : 0.0);
		rows = Math.Clamp(rows, 1, 10);
		row = Math.Clamp(row, 0, rows - 1);
		int num = row * Math.Min(rows, 5) / rows;
		double num2 = Math.Clamp(((timeline - 0.04) * 420.0 - (double)(num * 20)) / 100.0, 0.0, 1.0);
		double num3 = num2 * num2 * (3.0 - 2.0 * num2);
		return (Opacity: num3, Scale: 0.92 + 0.08 * num3);
	}

	private static double EaseOutCubic(double progress)
	{
		return 1.0 - Math.Pow(1.0 - progress, 3.0);
	}

	private static int Lerp(int from, int to, double progress)
	{
		return (int)Math.Round((double)from + ((double)to - (double)from) * progress, MidpointRounding.AwayFromZero);
	}
}
