using GlassDesk.Core.Input;

namespace GlassDesk.Core.Models.Settings;

public sealed class ShortcutSettings
{
	public InputAction MouseBackAction { get; set; } = InputAction.ToggleLauncher;

	public InputAction MouseForwardAction { get; set; }

	public HotkeyDefinition ToggleLauncher { get; set; } = new HotkeyDefinition();

	public HotkeyDefinition HideLauncher { get; set; } = new HotkeyDefinition();

	public HotkeyDefinition NextPage { get; set; } = new HotkeyDefinition
	{
		Enabled = true,
		Key = "WheelDown"
	};

	public HotkeyDefinition PreviousPage { get; set; } = new HotkeyDefinition
	{
		Enabled = true,
		Key = "WheelUp"
	};

	public HotkeyDefinition ToggleDesktopIcons { get; set; } = new HotkeyDefinition();

	public HotkeyDefinition ToggleDockRevealLock { get; set; } = new HotkeyDefinition();

	public HotkeyDefinition ToggleGameSleep { get; set; } = DefaultGameSleepHotkey();

	public bool HotCornersEnabled { get; set; }

	public HotCorner HotCornerTarget { get; set; }

	public int HotCornerDwellMs { get; set; } = 300;

	public bool MouseSideButtonsEnabled { get; set; }

	public HotkeyDefinition MouseBackButton { get; set; } = new HotkeyDefinition();

	public HotkeyDefinition MouseForwardButton { get; set; } = new HotkeyDefinition();

	private static HotkeyDefinition DefaultGameSleepHotkey()
	{
		return new HotkeyDefinition
		{
			Enabled = true,
			Control = true,
			Alt = true,
			Key = "G"
		};
	}

	internal void Normalize()
	{
		if (ToggleLauncher == null)
		{
			HotkeyDefinition hotkeyDefinition = (ToggleLauncher = new HotkeyDefinition());
		}
		if (HideLauncher == null)
		{
			HotkeyDefinition hotkeyDefinition = (HideLauncher = new HotkeyDefinition());
		}
		if (NextPage == null)
		{
			HotkeyDefinition hotkeyDefinition = (NextPage = new HotkeyDefinition());
		}
		if (PreviousPage == null)
		{
			HotkeyDefinition hotkeyDefinition = (PreviousPage = new HotkeyDefinition());
		}
		if (ToggleDesktopIcons == null)
		{
			HotkeyDefinition hotkeyDefinition = (ToggleDesktopIcons = new HotkeyDefinition());
		}
		if (ToggleDockRevealLock == null)
		{
			HotkeyDefinition hotkeyDefinition = (ToggleDockRevealLock = new HotkeyDefinition());
		}
		if (ToggleGameSleep == null)
		{
			HotkeyDefinition hotkeyDefinition = (ToggleGameSleep = DefaultGameSleepHotkey());
		}
		if (MouseBackButton == null)
		{
			HotkeyDefinition hotkeyDefinition = (MouseBackButton = new HotkeyDefinition());
		}
		if (MouseForwardButton == null)
		{
			HotkeyDefinition hotkeyDefinition = (MouseForwardButton = new HotkeyDefinition());
		}
	}
}
