namespace GlassDesk.Core.Models.Settings;

public sealed class DisplaySettings
{
	public string? DockDisplayId { get; set; }

	public bool LauncherFollowsPointer { get; set; } = true;

	public bool SuppressTriggersWhenFullscreen { get; set; } = true;
}
