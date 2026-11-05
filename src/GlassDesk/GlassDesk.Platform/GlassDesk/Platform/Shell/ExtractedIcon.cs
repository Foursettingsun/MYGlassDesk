namespace GlassDesk.Platform.Shell;

public sealed record ExtractedIcon(int Width, int Height, byte[] Bgra)
{
	public int VisiblePixelCount
	{
		get
		{
			int num = 0;
			for (int i = 3; i < Bgra.Length; i += 4)
			{
				if (Bgra[i] != 0)
				{
					num++;
				}
			}
			return num;
		}
	}
}
