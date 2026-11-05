using System;
using System.Collections.Generic;
using System.Text.Json;
using GlassDesk.Core.Input;
using GlassDesk.Core.Serialization;

namespace GlassDesk.Core.Models.Settings;

public static class SettingsPageEditor
{
	public static GlassDeskSettings Clone(GlassDeskSettings settings)
	{
		return JsonSerializer.Deserialize(JsonSerializer.SerializeToUtf8Bytes(settings, GlassDeskJsonContext.Default.GlassDeskSettings), GlassDeskJsonContext.Default.GlassDeskSettings);
	}

	public static void Apply(GlassDeskSettings target, GlassDeskSettings source)
	{
		target.UpdateNoticeSnoozedUntilUtc = source.UpdateNoticeSnoozedUntilUtc;
		target.Appearance = source.Appearance;
		target.Dock = source.Dock;
		target.Launcher = source.Launcher;
		target.Shortcuts = source.Shortcuts;
		target.Display = source.Display;
		target.Performance = source.Performance;
		target.DesktopTaskbar = source.DesktopTaskbar;
	}

	public static GlassDeskSettings ResetPage(GlassDeskSettings settings, string pageName)
	{
		GlassDeskSettings glassDeskSettings = Clone(settings);
		switch (pageName)
		{
		case "外观":
			glassDeskSettings.Appearance = new AppearanceSettings
			{
				DockCornerRadius = settings.Appearance.DockCornerRadius
			};
			break;
		case "Dock":
			glassDeskSettings.Dock = new DockSettings
			{
				TargetDisplayId = settings.Dock.TargetDisplayId
			};
			glassDeskSettings.Appearance.DockCornerRadius = 40.0;
			break;
		case "启动台":
			glassDeskSettings.Launcher = new LauncherSettings();
			break;
		case "快捷操作":
			glassDeskSettings.Shortcuts = new ShortcutSettings();
			break;
		case "显示器":
			glassDeskSettings.Display = new DisplaySettings();
			glassDeskSettings.Dock.TargetDisplayId = null;
			break;
		case "性能":
			glassDeskSettings.Performance = new PerformanceSettings();
			break;
		case "桌面与任务栏":
			glassDeskSettings.DesktopTaskbar = new DesktopTaskbarSettings();
			break;
		default:
			throw new ArgumentException("此页面没有可重置的配置。", "pageName");
		}
		return glassDeskSettings;
	}

	public static string? Validate(GlassDeskSettings settings)
	{
		DockSettings dock = settings.Dock;
		if ((dock.FrameLengthDip != 0.0 && !Range(dock.FrameLengthDip, 64.0, 10000.0)) || !Range(dock.SizeScale, 2.0 / 3.0, 1.5) || !Range(dock.StatusBarSizeScale, 0.75, 1.5) || !Range(dock.IconSize, 32.0, 96.0) || !Range(dock.IconSpacing, 0.0, 24.0) || !Range(dock.MagnificationPeak, 1.0, 2.0) || !Range(dock.OffsetX, -200.0, 200.0) || !Range(dock.OffsetY, -200.0, 200.0) || !Range(dock.StatusBarOffsetX, -10000.0, 10000.0) || !Range(dock.StatusBarOffsetY, -10000.0, 10000.0) || !Range(dock.AutoHideLeaveDelayMs, 0.0, 3000.0) || !Range(dock.EdgeHotzoneSize, 1.0, 64.0) || !Enum.IsDefined(dock.Placement) || !Enum.IsDefined(dock.StatusBarEdge))
		{
			return "Dock 数值超出允许范围。";
		}
		if (!Range(settings.Launcher.Rows, 2.0, 10.0) || !Range(settings.Launcher.Columns, 2.0, 12.0) || !Range(settings.Launcher.WindowWorkAreaRatio, 0.3, 1.0))
		{
			return "启动台网格或窗口比例超出允许范围。";
		}
		if (!Range(settings.Appearance.DockCornerRadius, 0.0, 40.0) || !Range(settings.Appearance.LauncherCornerRadius, 0.0, 80.0) || !Range(settings.Appearance.GlassTintOpacity, 0.0, 1.0) || !Range(settings.Appearance.GlassBackgroundClarity, 0.0, 1.0) || !Range(settings.Appearance.DockGlassTransparency, 0.0, 1.0) || !Range(settings.Appearance.LauncherGlassTransparency, 0.0, 1.0) || !Range(settings.Appearance.DockBuiltinIconBrightness, 0.0, 1.0) || !Range(settings.Appearance.LauncherTextBrightness, 0.0, 1.0) || !Range(settings.Appearance.StatusBarTextBrightness, 0.0, 1.0) || !Enum.IsDefined(settings.Appearance.Theme) || !Enum.IsDefined(settings.Appearance.GlassMaterial))
		{
			return "外观数值超出允许范围。";
		}
		ShortcutSettings shortcuts = settings.Shortcuts;
		if (!Range(shortcuts.HotCornerDwellMs, 100.0, 3000.0) || !Enum.IsDefined(shortcuts.HotCornerTarget) || !Enum.IsDefined(shortcuts.MouseBackAction) || !Enum.IsDefined(shortcuts.MouseForwardAction))
		{
			return "热角停留时间应为 100–3000 毫秒，且触发动作必须有效。";
		}
		if (shortcuts.HotCornersEnabled && shortcuts.HotCornerTarget == HotCorner.None)
		{
			return "开启热角后需要选择一个角落。";
		}
		HashSet<HotkeyChord> hashSet = new HashSet<HotkeyChord>();
		foreach (var item2 in HotkeyText.Bindings(shortcuts))
		{
			HotkeyDefinition item = item2.Definition;
			if (item.Enabled)
			{
				if (!HotkeyText.TryGetChord(item, out HotkeyChord chord, out string error))
				{
					return error;
				}
				if (!hashSet.Add(chord))
				{
					return "多个操作使用了同一个快捷键。";
				}
			}
		}
		return null;
		static bool Range(double value, double min, double max)
		{
			if (double.IsFinite(value) && value >= min)
			{
				return value <= max;
			}
			return false;
		}
	}
}
