internal sealed class PixelStats
{
	public int Candidates { get; set; }

	public int Occluded { get; set; }

	public int Unstable { get; set; }

	public int Valid { get; set; }

	public int Mismatched { get; set; }

	public int[] MaxBgrError { get; } = new int[3];

	public bool Passed
	{
		get
		{
			if (Valid >= 50)
			{
				return Mismatched == 0;
			}
			return false;
		}
	}
}
