namespace GlassDesk.AppServices;

public sealed record IconCacheClearResult(int Removed, int LeftAlone)
{
	public bool LeftOthers => LeftAlone > 0;
}
