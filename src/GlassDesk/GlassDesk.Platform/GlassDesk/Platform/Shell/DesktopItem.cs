namespace GlassDesk.Platform.Shell;

public sealed record DesktopItem(string Path, string DisplayName, bool IsDirectory, bool FromCurrentUser);
