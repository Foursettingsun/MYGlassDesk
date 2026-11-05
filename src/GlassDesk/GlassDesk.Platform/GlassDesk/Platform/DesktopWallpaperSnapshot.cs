using System.Collections.Generic;

namespace GlassDesk.Platform;

public sealed record DesktopWallpaperSnapshot(IReadOnlyList<WallpaperMonitor> Monitors, DesktopWallpaperPosition Position, uint BackgroundColor);
