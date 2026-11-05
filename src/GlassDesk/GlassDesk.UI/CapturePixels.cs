using System;
using GlassDesk.UI.Controls;

internal sealed record CapturePixels(int Width, int Height, byte[] Pixels)
{
	internal long SourceQpcTicks { get; init; }

	internal GlassBackdropSource Source { get; init; } = GlassBackdropSource.LiveDesktop;

	internal bool IsStatic { get; init; }

	public byte[] Center => Pixels.AsSpan((Height / 2 * Width + Width / 2) * 4, 4).ToArray();
}
