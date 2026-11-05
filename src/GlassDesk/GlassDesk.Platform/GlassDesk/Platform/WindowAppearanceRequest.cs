namespace GlassDesk.Platform;

public sealed class WindowAppearanceRequest
{
	public SystemBackdropType Backdrop { get; set; } = SystemBackdropType.None;

	public WindowCornerPreference Corners { get; set; } = WindowCornerPreference.Round;

	public bool DarkMode { get; set; }

	public bool ExtendFrameIntoClientArea { get; set; }
}
