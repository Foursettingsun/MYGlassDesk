using System;
using System.Windows;

namespace GlassDesk.UI.Controls;

internal static class FrostedGlassRenderer
{
	internal static byte[] Render(byte[] source, int width, int height, Size size, Rect bounds, double radius, bool squircle, bool dark, double strength, double clarity)
	{
		byte[] array = new byte[source.Length];
		byte[] scratch = new byte[source.Length];
		AndroidLiquidGlassRenderer.RenderFrostedBackdropInto(source, array, scratch, width, height, size, dark, strength, clarity);
		double num = Math.Max(size.Width / (double)width, size.Height / (double)height);
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				Point point = new Point(((double)j + 0.5) * size.Width / (double)width, ((double)i + 0.5) * size.Height / (double)height);
				double num2 = (squircle ? GlassSignedDistance.Squircle(bounds, radius, point) : GlassSignedDistance.Continuous(bounds, radius, point));
				double num3 = Math.Clamp(0.5 - num2 / num, 0.0, 1.0);
				int num4 = (i * width + j) * 4;
				array[num4 + 3] = (byte)Math.Round((double)(int)array[num4 + 3] * num3);
				if (num3 == 0.0)
				{
					array.AsSpan(num4, 4).Clear();
				}
			}
		}
		return array;
	}
}
