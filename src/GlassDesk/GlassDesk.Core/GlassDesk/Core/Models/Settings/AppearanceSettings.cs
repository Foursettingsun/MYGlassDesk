namespace GlassDesk.Core.Models.Settings;

public sealed class AppearanceSettings
{
	public const double DefaultGlassTintOpacity = 0.16;

	public const double DefaultGlassBackgroundClarity = 0.5;

	public const double DefaultDockGlassTransparency = 0.9;

	public const double DefaultLauncherGlassTransparency = 0.2;

	public bool PinBarsToDesktop { get; set; }

	public double DockGlassTransparency { get; set; } = 0.9;

	public double LauncherGlassTransparency { get; set; } = 0.2;

	public double DockBuiltinIconBrightness { get; set; } = 1.0;

	public bool LaunchpadIconFilled { get; set; }

	public double LauncherTextBrightness { get; set; } = 1.0;

	public double StatusBarTextBrightness { get; set; } = 1.0;

	public double GlassBackgroundClarity { get; set; } = 0.5;

	public double GlassTintOpacity { get; set; } = 0.16;

	public GlassMaterialMode GlassMaterial { get; set; }

	public ThemeMode Theme { get; set; } = ThemeMode.Dark;

	public string AccentColor { get; set; } = "#0A84FF";

	public bool UseSystemBackdrop { get; set; } = true;

	public bool ReduceMotion { get; set; }

	public bool LiveBackdrop { get; set; } = true;

	public bool ReduceTransparency { get; set; }

	public double DockCornerRadius { get; set; } = 40.0;

	public double LauncherCornerRadius { get; set; } = 80.0;
}
