using System;

namespace GlassDesk.Platform.Displays;

public static class WindowPlacementPolicy
{
	public static PixelRect Centered(PixelRect workArea, double ratio, bool fullscreen)
	{
		if (fullscreen)
		{
			return workArea;
		}
		ratio = (double.IsFinite(ratio) ? Math.Clamp(ratio, 0.3, 1.0) : 0.66);
		int num = Math.Max(1, (int)Math.Round((double)workArea.Width * ratio));
		int num2 = Math.Max(1, (int)Math.Round((double)workArea.Height * ratio));
		return new PixelRect(workArea.X + (workArea.Width - num) / 2, workArea.Y + (workArea.Height - num2) / 2, num, num2);
	}
}
