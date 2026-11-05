using System;
using GlassDesk.Core.Models.Settings;
using GlassDesk.Platform.Displays;

namespace GlassDesk.Launcher.Repair;

public static class LauncherPlacementRepair
{
    public static PixelRect ApplyVerticalOffset(PixelRect bounds, DisplayInfo? display, double offsetY)
    {
        if (display == null || !double.IsFinite(offsetY) || offsetY == 0)
        {
            return bounds;
        }

        double dpi = display.Dpi == 0 ? 96 : display.Dpi;
        PixelRect workArea = display.WorkArea;
        double maximumY = Math.Max(workArea.Y, (double)workArea.Bottom - bounds.Height);
        double pixelOffset = Math.Round(offsetY * dpi / 96.0, MidpointRounding.AwayFromZero);
        double requestedY = bounds.Y + pixelOffset;
        int y = (int)Math.Clamp(requestedY, workArea.Y, maximumY);
        return new PixelRect(bounds.X, y, bounds.Width, bounds.Height);
    }

    public static bool MoveWindowIfNeeded(IntPtr handle, PixelRect bounds)
    {
        PixelRect? current = DisplayService.GetWindowPixelBounds(handle);
        if (current.HasValue && current.Value == bounds)
        {
            return true;
        }

        return DisplayService.MoveWindowToPixelRect(handle, bounds);
    }

    public static bool IsPositionOnly(LauncherSettings current, LauncherSettings next, double previousOffset, double nextOffset)
    {
        return previousOffset != nextOffset
            && current.Rows == next.Rows
            && current.Columns == next.Columns
            && current.WindowWorkAreaRatio == next.WindowWorkAreaRatio
            && current.FullscreenMode == next.FullscreenMode
            && current.GroupModeEnabled == next.GroupModeEnabled
            && current.FocusSearchOnOpen == next.FocusSearchOnOpen
            && current.RememberLastPage == next.RememberLastPage
            && current.HighRefreshAnimationsEnabled == next.HighRefreshAnimationsEnabled;
    }
}
