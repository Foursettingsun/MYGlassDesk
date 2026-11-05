using System.Collections.Generic;

internal sealed record PixelResult(PixelStats Wallpaper, PixelStats Icons, IReadOnlyList<(int X, int Y, bool Icon)> Mismatches);
