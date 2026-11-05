namespace GlassDesk.Platform.Shell;

public sealed record WindowIdentityInfo(nint Handle, string Title, uint ProcessId, bool IsMinimized, string? ImagePath, string? WindowAumid, string? ProcessAumid)
{
	public string? EffectiveAumid => WindowAumid ?? ProcessAumid;
}
