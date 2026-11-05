internal readonly record struct PixelBounds(int Left, int Top, int Right, int Bottom)
{
	public int Width => Right - Left;

	public int Height => Bottom - Top;

	public bool Contains(int x, int y)
	{
		if (x >= Left && x < Right && y >= Top)
		{
			return y < Bottom;
		}
		return false;
	}
}
