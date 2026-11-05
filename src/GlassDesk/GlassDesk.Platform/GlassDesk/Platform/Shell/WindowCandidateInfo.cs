namespace GlassDesk.Platform.Shell;

public sealed record WindowCandidateInfo(nint Handle, string Title, uint ProcessId, bool IsMinimized);
