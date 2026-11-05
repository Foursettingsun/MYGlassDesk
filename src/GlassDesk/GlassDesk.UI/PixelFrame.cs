using System;

internal sealed record PixelFrame(PixelBounds Bounds, byte[] Pixels, long StartTicks, long EndTicks, DateTimeOffset StartedUtc);
