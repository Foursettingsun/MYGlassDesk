namespace GlassDesk.Core.Contracts;

public sealed class ImportRequest
{
	public string Source { get; set; } = string.Empty;

	public bool IsUrl { get; set; }

	public string? DisplayName { get; set; }
}
