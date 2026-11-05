using System;
using GlassDesk.Platform;

namespace GlassDesk.App.Startup;

internal static class StartupIntent
{
	public static string? GetDuplicateInstanceCommand(CommandLineOptions options)
	{
		ArgumentNullException.ThrowIfNull(options, "options");
		if (options.Autostart)
		{
			return null;
		}
		if (options != null)
		{
			if (options.Exit)
			{
				return "exit";
			}
			if (options.ShowSettings)
			{
				return "show-settings";
			}
			if (options.ShowLauncher)
			{
				return "show-launcher";
			}
		}
		return "show-launcher";
	}

	public static bool ShouldOpenLauncher(CommandLineOptions options)
	{
		if (!options.Autostart)
		{
			return options.ShowLauncher;
		}
		return false;
	}

	public static bool ShouldOpenSettings(CommandLineOptions options)
	{
		if (!options.Autostart)
		{
			return options.ShowSettings;
		}
		return false;
	}

	public static bool ShouldExitWithoutExistingInstance(CommandLineOptions options)
	{
		return options.Exit;
	}

	public static string GetLegacyInstanceBlockMessage(LegacyInstanceStatus status)
	{
		return status switch
		{
			LegacyInstanceStatus.Running => "检测到仍在运行的旧版 GlassDesk 实例。为避免新旧版本同时修改同一份用户数据，请先从旧版托盘菜单退出后重试。", 
			LegacyInstanceStatus.Unknown => "无法确认旧版 GlassDesk 实例是否仍在运行。为保护用户数据，本次启动已阻止；请确认旧版完全退出后重试。", 
			_ => string.Empty, 
		};
	}
}
