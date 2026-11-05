namespace GlassDesk.Platform.Installation;

public sealed record InstallationInfo(InstallationState State, string? InstallDirectory = null, string? UninstallerPath = null, string? Error = null);
