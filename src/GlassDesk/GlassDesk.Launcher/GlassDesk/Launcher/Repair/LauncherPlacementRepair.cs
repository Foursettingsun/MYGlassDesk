using System;
using GlassDesk.Core.Models.Settings;
using GlassDesk.Platform.Displays;

namespace GlassDesk.Launcher.Repair;

public static class LauncherPlacementRepair
{
	public static PixelRect ApplyVerticalOffset(PixelRect bounds, DisplayInfo display, double offsetY)
	{
		if (display == null || !double.IsFinite(offsetY) || offsetY == 0.0)
		{
			return bounds;
		}
		double num = ((display.Dpi == 0) ? 96u : display.Dpi);
		PixelRect workArea = display.WorkArea;
		double max = Math.Max(workArea.Y, (double)workArea.Bottom - (double)bounds.Height);
		double num2 = Math.Round(offsetY * num / 96.0, MidpointRounding.AwayFromZero);
		double value = (double)bounds.Y + num2;
		int y = (int)Math.Clamp(value, workArea.Y, max);
		return new PixelRect(bounds.X, y, bounds.Width, bounds.Height);
	}

	public static bool MoveWindowIfNeeded(nint handle, PixelRect bounds)
	{
		PixelRect? windowPixelBounds = DisplayService.GetWindowPixelBounds(handle);
		if (windowPixelBounds.HasValue && windowPixelBounds.Value == bounds)
		{
			return true;
		}
		return DisplayService.MoveWindowToPixelRect(handle, bounds);
	}

	public static bool IsPositionOnly(LauncherSettings current, LauncherSettings next, double previousOffset, double nextOffset)
	{
		return previousOffset != nextOffset && current.Rows == next.Rows && current.Columns == next.Columns && current.WindowWorkAreaRatio == next.WindowWorkAreaRatio && current.FullscreenMode == next.FullscreenMode && current.GroupModeEnabled == next.GroupModeEnabled && current.FocusSearchOnOpen == next.FocusSearchOnOpen && current.RememberLastPage == next.RememberLastPage && current.HighRefreshAnimationsEnabled == next.HighRefreshAnimationsEnabled;
	}
}
