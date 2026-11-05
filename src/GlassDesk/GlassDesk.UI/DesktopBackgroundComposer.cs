using System;
using System.Collections.Generic;
using System.Linq;

internal sealed class DesktopBackgroundComposer
{
	private readonly record struct PixelSpan(int Offset, int Length);

	private readonly record struct CoverageRect(int Left, int Top, int Right, int Bottom);

	private readonly PixelSpan[] _uncovered;

	private readonly int _length;

	internal DesktopBackgroundComposer(int width, int height, IReadOnlyList<ScreenRect> foreground)
	{
		_length = checked(width * height * 4);
		CoverageRect[] array = (from bounds in foreground
			select new CoverageRect(Math.Clamp(bounds.X, 0, width), Math.Clamp(bounds.Y, 0, height), Math.Clamp(bounds.X + bounds.Width, 0, width), Math.Clamp(bounds.Y + bounds.Height, 0, height)) into bounds
			where bounds.Right > bounds.Left && bounds.Bottom > bounds.Top
			orderby bounds.Left
			select bounds).ToArray();
		List<PixelSpan> list = new List<PixelSpan>();
		for (int num = 0; num < height; num++)
		{
			int num2 = 0;
			CoverageRect[] array2 = array;
			for (int num3 = 0; num3 < array2.Length; num3++)
			{
				CoverageRect coverageRect = array2[num3];
				if (num >= coverageRect.Top && num < coverageRect.Bottom)
				{
					if (coverageRect.Left > num2)
					{
						AddSpan(list, (num * width + num2) * 4, (coverageRect.Left - num2) * 4);
					}
					num2 = Math.Max(num2, coverageRect.Right);
				}
			}
			if (num2 < width)
			{
				AddSpan(list, (num * width + num2) * 4, (width - num2) * 4);
			}
		}
		_uncovered = list.ToArray();
	}

	private static void AddSpan(List<PixelSpan> spans, int offset, int length)
	{
		if (spans.Count > 0)
		{
			if (spans[spans.Count - 1].Offset + spans[spans.Count - 1].Length == offset)
			{
				PixelSpan pixelSpan = spans[spans.Count - 1];
				spans[spans.Count - 1] = pixelSpan with
				{
					Length = pixelSpan.Length + length
				};
				return;
			}
		}
		spans.Add(new PixelSpan(offset, length));
	}

	internal void Apply(byte[] frame, byte[] background)
	{
		if (frame.Length != _length || background.Length != _length)
		{
			throw new ArgumentException("Desktop and scene pixels must have matching geometry.");
		}
		PixelSpan[] uncovered = _uncovered;
		for (int i = 0; i < uncovered.Length; i++)
		{
			PixelSpan pixelSpan = uncovered[i];
			Buffer.BlockCopy(background, pixelSpan.Offset, frame, pixelSpan.Offset, pixelSpan.Length);
		}
	}
}
