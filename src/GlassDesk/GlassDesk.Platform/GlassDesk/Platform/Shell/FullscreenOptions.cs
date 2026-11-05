namespace GlassDesk.Platform.Shell;

public sealed record FullscreenOptions
{
	public static FullscreenOptions Default { get; } = new FullscreenOptions();

	public bool ExcludeCurrentProcess { get; init; } = true;

	public bool IncludeShellState { get; init; }
}
