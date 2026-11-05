namespace GlassDesk.Core.Contracts;

public sealed class LaunchResult
{
	public bool Succeeded { get; init; }

	public LaunchFailureReason? Failure { get; init; }

	public string? Message { get; init; }
}
