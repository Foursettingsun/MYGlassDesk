namespace GlassDesk.Platform.Interop;

internal struct Rect
{
	internal int Left;

	internal int Top;

	internal int Right;

	internal int Bottom;

	internal readonly int Width => Right - Left;

	internal readonly int Height => Bottom - Top;
}
