namespace GlassDesk.Platform.Startup;

public sealed record StartupRegistrationStatus(StartupRegistrationState State, string? TargetPath, string? Error);
