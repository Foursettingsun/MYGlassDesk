namespace GlassDesk.Platform.Displays;

public readonly record struct PixelRect(int X, int Y, int Width, int Height)
{
	public int Right => X + Width;

	public int Bottom => Y + Height;

	public bool Contains(int px, int py)
	{
		if (px >= X && px < Right && py >= Y)
		{
			return py < Bottom;
		}
		return false;
	}

	public override string ToString()
	{
		return $"({X},{Y} {Width}×{Height})";
	}
}
