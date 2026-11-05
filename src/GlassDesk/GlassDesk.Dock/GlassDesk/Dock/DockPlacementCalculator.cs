using System;
using GlassDesk.Core.Models.Settings;
using GlassDesk.Platform.Displays;

namespace GlassDesk.Dock;

public static class DockPlacementCalculator
{
	public static PixelRect Calculate(PixelRect workArea, DockPlacement placement, double offsetXDips, double offsetYDips, int windowWidthPixels, int windowHeightPixels, uint dpi)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(windowWidthPixels, "windowWidthPixels");
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(windowHeightPixels, "windowHeightPixels");
		int num = ToPixels(offsetXDips, dpi);
		int num2 = ToPixels(offsetYDips, dpi);
		(int, int) tuple = placement switch
		{
			DockPlacement.BottomCenter => (workArea.X + (workArea.Width - windowWidthPixels) / 2, workArea.Bottom - windowHeightPixels), 
			DockPlacement.BottomLeft => (workArea.X, workArea.Bottom - windowHeightPixels), 
			DockPlacement.BottomRight => (workArea.Right - windowWidthPixels, workArea.Bottom - windowHeightPixels), 
			DockPlacement.TopCenter => (workArea.X + (workArea.Width - windowWidthPixels) / 2, workArea.Y), 
			_ => (workArea.X + (workArea.Width - windowWidthPixels) / 2, workArea.Bottom - windowHeightPixels), 
		};
		int item = tuple.Item1;
		int item2 = tuple.Item2;
		int x = item + num;
		int y = item2 + num2;
		return ClampToWorkArea(new PixelRect(x, y, windowWidthPixels, windowHeightPixels), workArea);
	}

	public static PixelRect ClampToWorkArea(PixelRect rect, PixelRect workArea)
	{
		int num = Math.Min(rect.Width, workArea.Width);
		int num2 = Math.Min(rect.Height, workArea.Height);
		int num3 = rect.X;
		int num4 = rect.Y;
		if (num3 < workArea.X)
		{
			num3 = workArea.X;
		}
		else if (num3 + num > workArea.Right)
		{
			num3 = workArea.Right - num;
		}
		if (num4 < workArea.Y)
		{
			num4 = workArea.Y;
		}
		else if (num4 + num2 > workArea.Bottom)
		{
			num4 = workArea.Bottom - num2;
		}
		return new PixelRect(num3, num4, num, num2);
	}

	private static int ToPixels(double dips, uint dpi)
	{
		return (int)Math.Round(dips * (double)dpi / 96.0, MidpointRounding.AwayFromZero);
	}
}
