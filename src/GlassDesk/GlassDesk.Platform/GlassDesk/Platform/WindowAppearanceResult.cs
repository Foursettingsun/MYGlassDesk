namespace GlassDesk.Platform;

public readonly record struct WindowAppearanceResult(bool BackdropApplied, bool CornersApplied, bool DarkModeApplied)
{
	public bool IsFullyApplied
	{
		get
		{
			if (BackdropApplied && CornersApplied)
			{
				return DarkModeApplied;
			}
			return false;
		}
	}
}
