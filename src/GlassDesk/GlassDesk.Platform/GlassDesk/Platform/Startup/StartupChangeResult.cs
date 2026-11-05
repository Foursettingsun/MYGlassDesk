namespace GlassDesk.Platform.Startup;

public sealed record StartupChangeResult(bool Succeeded, StartupRegistrationStatus Status, string? Error);
