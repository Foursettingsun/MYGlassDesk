namespace GlassDesk.Platform.Shell;

public sealed record ShortcutDefinition(string TargetPath, string Arguments = "", string WorkingDirectory = "", string IconLocation = "", string Description = "", string Aumid = "");
