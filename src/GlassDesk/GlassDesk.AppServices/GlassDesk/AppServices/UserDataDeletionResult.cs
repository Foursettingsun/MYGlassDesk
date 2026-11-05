namespace GlassDesk.AppServices;

public sealed record UserDataDeletionResult(UserDataDeletionStatus Status, int DeletedEntries = 0, string? ResidualDirectory = null, string? Error = null);
