namespace GlassDesk.Core.Models.Settings;

public sealed class DesktopTaskbarSettings
{
	public HotkeyDefinition ToggleDesktopIcons { get; set; } = new HotkeyDefinition();

	public bool BlockTaskbarEdgeReveal { get; set; }

	public bool TaskbarAutoHideEnabled { get; set; }

	internal void Normalize()
	{
		if (ToggleDesktopIcons == null)
		{
			HotkeyDefinition hotkeyDefinition = (ToggleDesktopIcons = new HotkeyDefinition());
		}
	}
}
