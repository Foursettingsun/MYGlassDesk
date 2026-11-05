using System;

namespace GlassDesk.Core.Models.Settings;

public sealed class GlassDeskSettings
{
	public const int CurrentSchemaVersion = 1;

	public int SchemaVersion { get; set; } = 1;

	public bool? OnboardingDismissed { get; set; }

	public DateTimeOffset? UpdateNoticeSnoozedUntilUtc { get; set; }

	public AppearanceSettings Appearance { get; set; } = new AppearanceSettings();

	public DockSettings Dock { get; set; } = new DockSettings();

	public LauncherSettings Launcher { get; set; } = new LauncherSettings();

	public ShortcutSettings Shortcuts { get; set; } = new ShortcutSettings();

	public DisplaySettings Display { get; set; } = new DisplaySettings();

	public PerformanceSettings Performance { get; set; } = new PerformanceSettings();

	public DesktopTaskbarSettings DesktopTaskbar { get; set; } = new DesktopTaskbarSettings();

	public bool ShouldShowUpdateNotice(bool manual, DateTimeOffset now)
	{
		if (!manual)
		{
			DateTimeOffset? updateNoticeSnoozedUntilUtc = UpdateNoticeSnoozedUntilUtc;
			if (updateNoticeSnoozedUntilUtc.HasValue)
			{
				DateTimeOffset valueOrDefault = updateNoticeSnoozedUntilUtc.GetValueOrDefault();
				return now >= valueOrDefault;
			}
		}
		return true;
	}

	public void Normalize()
	{
		if (Appearance == null)
		{
			AppearanceSettings appearanceSettings = (Appearance = new AppearanceSettings());
		}
		if (Dock == null)
		{
			DockSettings dockSettings = (Dock = new DockSettings());
		}
		if (Launcher == null)
		{
			LauncherSettings launcherSettings = (Launcher = new LauncherSettings());
		}
		if (Shortcuts == null)
		{
			ShortcutSettings shortcutSettings = (Shortcuts = new ShortcutSettings());
		}
		if (Display == null)
		{
			DisplaySettings displaySettings = (Display = new DisplaySettings());
		}
		if (Performance == null)
		{
			PerformanceSettings performanceSettings = (Performance = new PerformanceSettings());
		}
		if (DesktopTaskbar == null)
		{
			DesktopTaskbarSettings desktopTaskbarSettings = (DesktopTaskbar = new DesktopTaskbarSettings());
		}
		Dock.EdgeRevealEnabled = false;
		Launcher.FullscreenMode = false;
		Shortcuts.HotCornersEnabled = false;
		Shortcuts.MouseSideButtonsEnabled = false;
		Appearance.Theme = ThemeMode.Dark;
		GlassMaterialMode glassMaterial = Appearance.GlassMaterial;
		if ((uint)(glassMaterial - 2) > 1u)
		{
			Appearance.GlassMaterial = GlassMaterialMode.AndroidLiquidGlass;
		}
		Dock.SizeScale = (double.IsFinite(Dock.SizeScale) ? Math.Clamp(Dock.SizeScale, 2.0 / 3.0, 1.5) : 1.0);
		Dock.StatusBarSizeScale = (double.IsFinite(Dock.StatusBarSizeScale) ? Math.Clamp(Dock.StatusBarSizeScale, 0.75, 1.5) : 1.0);
		Dock.StatusBarOffsetX = (double.IsFinite(Dock.StatusBarOffsetX) ? Math.Clamp(Dock.StatusBarOffsetX, -10000.0, 10000.0) : 0.0);
		if (!Enum.IsDefined(Dock.StatusBarEdge))
		{
			Dock.StatusBarEdge = StatusBarEdge.Top;
		}
		Dock.StatusBarOffsetY = 0.0;
		Appearance.ReduceTransparency = false;
		Appearance.DockGlassTransparency = (double.IsFinite(Appearance.DockGlassTransparency) ? Math.Clamp(Appearance.DockGlassTransparency, 0.0, 1.0) : 0.9);
		Appearance.LauncherGlassTransparency = (double.IsFinite(Appearance.LauncherGlassTransparency) ? Math.Clamp(Appearance.LauncherGlassTransparency, 0.0, 1.0) : 0.2);
		Appearance.DockBuiltinIconBrightness = (double.IsFinite(Appearance.DockBuiltinIconBrightness) ? Math.Clamp(Appearance.DockBuiltinIconBrightness, 0.0, 1.0) : 1.0);
		Appearance.LauncherTextBrightness = (double.IsFinite(Appearance.LauncherTextBrightness) ? Math.Clamp(Appearance.LauncherTextBrightness, 0.0, 1.0) : 1.0);
		Appearance.StatusBarTextBrightness = (double.IsFinite(Appearance.StatusBarTextBrightness) ? Math.Clamp(Appearance.StatusBarTextBrightness, 0.0, 1.0) : 1.0);
		Appearance.GlassTintOpacity = (double.IsFinite(Appearance.GlassTintOpacity) ? Math.Clamp(Appearance.GlassTintOpacity, 0.0, 1.0) : 0.16);
		Appearance.GlassBackgroundClarity = (double.IsFinite(Appearance.GlassBackgroundClarity) ? Math.Clamp(Appearance.GlassBackgroundClarity, 0.0, 1.0) : 0.5);
		Shortcuts.Normalize();
		DesktopTaskbar.Normalize();
	}
}
