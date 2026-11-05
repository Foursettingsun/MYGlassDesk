namespace GlassDesk.Core.Models.Settings;

public sealed class DockSettings
{
	public const double FixedIconSize = 48.0;

	public const double FixedIconSpacing = 8.0;

	public const double FixedMagnificationPeak = 1.15;

	public const double MinimumFrameLengthDip = 180.0;

	public const double MaximumFrameLengthDip = 10000.0;

	public const double MinimumSizeScale = 2.0 / 3.0;

	public const double MaximumSizeScale = 1.5;

	public const double MinimumStatusBarSizeScale = 0.75;

	public const double MaximumStatusBarSizeScale = 1.5;

	public double SizeScale { get; set; } = 1.0;

	public bool ShowLauncherEntry { get; set; } = true;

	public bool ShowThisPcEntry { get; set; } = true;

	public bool ShowDesktopIconsEntry { get; set; } = true;

	public string? LauncherCustomIconPath { get; set; }

	public string? ThisPcCustomIconPath { get; set; }

	public string? DesktopIconsCustomIconPath { get; set; }

	public bool ShowStatusBar { get; set; } = true;

	public bool ShowStatusBarNetworkSpeed { get; set; }

	public double StatusBarSizeScale { get; set; } = 1.0;

	public StatusBarEdge StatusBarEdge { get; set; }

	public double StatusBarOffsetX { get; set; }

	public double StatusBarOffsetY { get; set; }

	public double FrameLengthDip { get; set; }

	public double IconSize { get; set; } = 48.0;

	public double IconSpacing { get; set; } = 8.0;

	public double MagnificationPeak { get; set; } = 1.35;

	public DockPlacement Placement { get; set; }

	public double OffsetX { get; set; }

	public double OffsetY { get; set; }

	public bool AutoHide { get; set; }

	public int AutoHideLeaveDelayMs { get; set; } = 300;

	public bool EdgeRevealEnabled { get; set; }

	public int EdgeHotzoneSize { get; set; } = 14;

	public bool BlockRevealWhenCovered { get; set; } = true;

	public string? TargetDisplayId { get; set; }

	public bool ShowRunningIndicator { get; set; } = true;
}
