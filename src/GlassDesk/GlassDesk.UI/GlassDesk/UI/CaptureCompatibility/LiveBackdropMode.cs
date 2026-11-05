using GlassDesk.UI.Theming;

namespace GlassDesk.UI.CaptureCompatibility;

internal static class LiveBackdropMode
{
	internal static bool Enabled => ThemeService.Current?.Appearance.LiveBackdrop ?? true;
}
