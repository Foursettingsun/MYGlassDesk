using GlassDesk.Core.Models;

namespace GlassDesk.AppServices;

public sealed class LauncherEditSession(long layoutVersion, long libraryVersion, LauncherLayout draft)
{
	public long LayoutVersion { get; } = layoutVersion;

	public long LibraryVersion { get; } = libraryVersion;

	public LauncherLayout Draft { get; set; } = draft;

	internal bool IsGroupMode { get; init; }

	internal string? FilterGroupId { get; init; }

	internal int Capacity { get; init; }
}
