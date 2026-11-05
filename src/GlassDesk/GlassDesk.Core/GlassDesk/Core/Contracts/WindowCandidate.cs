namespace GlassDesk.Core.Contracts;

public sealed class WindowCandidate
{
	public nint Handle { get; init; }

	public string Title { get; init; } = string.Empty;
}
