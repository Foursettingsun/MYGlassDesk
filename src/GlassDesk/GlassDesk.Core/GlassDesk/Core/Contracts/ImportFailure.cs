namespace GlassDesk.Core.Contracts;

public sealed class ImportFailure
{
	public string Source { get; init; } = string.Empty;

	public ImportFailureReason Reason { get; init; }

	public string Message { get; init; } = string.Empty;
}
