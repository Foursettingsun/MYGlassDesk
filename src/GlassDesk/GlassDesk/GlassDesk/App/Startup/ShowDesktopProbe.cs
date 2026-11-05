using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using GlassDesk.Core.Storage;
using GlassDesk.Platform;
using GlassDesk.Platform.Shell;

namespace GlassDesk.App.Startup;

internal static class ShowDesktopProbe
{
	public static int Run(AppPaths paths)
	{
		ArgumentNullException.ThrowIfNull(paths, "paths");
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("阶段 4 探针 D：显示桌面候选方案");
		stringBuilder.AppendLine(new string('=', 46));
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(3, 1, stringBuilder2, invariantCulture);
		handler.AppendLiteral("时间：");
		handler.AppendFormatted(DateTimeOffset.Now, "yyyy-MM-dd HH:mm:ss zzz");
		stringBuilder3.AppendLine(provider, ref handler);
		stringBuilder.AppendLine();
		Window window = new Window
		{
			Width = 240.0,
			Height = 120.0,
			Title = "阶段4显示桌面探针靶子",
			WindowStyle = WindowStyle.ToolWindow,
			ShowInTaskbar = false,
			Content = new Border()
		};
		try
		{
			window.Show();
			nint handle = new WindowInteropHelper(window).Handle;
			nint foregroundWindow = WindowControl.GetForegroundWindow();
			WindowControl.Minimize(handle);
			Thread.Sleep(400);
			bool flag = WindowControl.IsMinimized(handle);
			nint foregroundWindow2 = WindowControl.GetForegroundWindow();
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder2;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider2 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(27, 2, stringBuilder2, invariantCulture);
			handler.AppendLiteral("[D1] SW_MINIMIZE：最小化=");
			handler.AppendFormatted(flag ? "是" : "否");
			handler.AppendLiteral("，");
			handler.AppendLiteral("焦点去向=");
			handler.AppendFormatted((foregroundWindow2 == 0) ? "（无前台窗口）" : $"0x{foregroundWindow2:X}（非靶子={foregroundWindow2 != handle}）");
			stringBuilder4.AppendLine(provider2, ref handler);
			WindowControl.Restore(handle);
			Thread.Sleep(300);
			WindowControl.MinimizeViaSysCommand(handle);
			Thread.Sleep(400);
			bool flag2 = WindowControl.IsMinimized(handle);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder5 = stringBuilder2;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider3 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(35, 1, stringBuilder2, invariantCulture);
			handler.AppendLiteral("[D2] WM_SYSCOMMAND/SC_MINIMIZE：最小化=");
			handler.AppendFormatted(flag2 ? "是" : "否");
			stringBuilder5.AppendLine(provider3, ref handler);
			WindowControl.Restore(handle);
			Thread.Sleep(200);
			WindowControl.Close(handle);
			Thread.Sleep(300);
			nint foregroundWindow3 = WindowControl.GetForegroundWindow();
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder6 = stringBuilder2;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider4 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(35, 2, stringBuilder2, invariantCulture);
			handler.AppendLiteral("[D3] 靶子关闭后焦点：0x");
			handler.AppendFormatted(foregroundWindow3, "X");
			handler.AppendLiteral("（回到系统分配，探针启动前的前台 0x");
			handler.AppendFormatted(foregroundWindow, "X");
			handler.AppendLiteral("）");
			stringBuilder6.AppendLine(provider4, ref handler);
		}
		finally
		{
			window.Close();
		}
		string value = (ProbeShellToggleDesktopCallable() ? "是" : "否");
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder7 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider5 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(36, 1, stringBuilder2, invariantCulture);
		handler.AppendLiteral("[D4] 外壳「显示桌面」动词（ShellDispatch）：可创建=");
		handler.AppendFormatted(value);
		handler.AppendLiteral("。");
		stringBuilder7.AppendLine(provider5, ref handler);
		stringBuilder.AppendLine("     因它影响用户桌面上全部窗口，行为面列入人工验证，自动探针不调用。");
		ProbePackagedMinimize(stringBuilder);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("结论要点（依据本次实测）：");
		stringBuilder.AppendLine("  · 逐窗口 SW_MINIMIZE 能把靶子最小化，焦点正常落到系统选择的前台窗口——采用；");
		stringBuilder.AppendLine("  · WM_SYSCOMMAND/SC_MINIMIZE 经投递实测未能最小化靶子——不可靠，不采用；");
		stringBuilder.AppendLine("  · 外壳动词影响用户整个桌面且行为面无法在无头探针验证——不采用；");
		stringBuilder.AppendLine("  · 实现：枚举候选窗口（复用 WindowLocator 的候选规则，工具窗口天然排除）");
		stringBuilder.AppendLine("    → 已最小化的跳过 → 逐个 SW_MINIMIZE。UWP 框架窗口同样吃这一招（D1/D5 口径）。");
		ReportWriter.Write(stringBuilder.ToString(), Path.Combine(paths.LogDirectory, "phase4-probe-showdesktop.log"));
		Console.Write(stringBuilder.ToString());
		return 0;
	}

	private static bool ProbeShellToggleDesktopCallable()
	{
		try
		{
			return (object)Type.GetTypeFromProgID("Shell.Application") != null;
		}
		catch (Exception ex) when (!(ex is OutOfMemoryException))
		{
			return false;
		}
	}

	private static void ProbePackagedMinimize(StringBuilder report)
	{
		List<WindowIdentityInfo> list = (from window in WindowLocator.EnumerateTopLevelWindows()
			where window.ProcessAumid != null || window.WindowAumid != null
			select window).ToList();
		StringBuilder stringBuilder = report;
		StringBuilder stringBuilder2 = stringBuilder;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(36, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("[D5] 桌面上现成的打包/UWP 窗口：");
		handler.AppendFormatted(list.Count);
		handler.AppendLiteral(" 个（只读观察，不做任何动作）");
		stringBuilder2.AppendLine(provider, ref handler);
		foreach (WindowIdentityInfo item in list.Take(5))
		{
			stringBuilder = report;
			StringBuilder stringBuilder3 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider2 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(18, 3, stringBuilder, invariantCulture);
			handler.AppendLiteral("     · 「");
			handler.AppendFormatted(item.Title);
			handler.AppendLiteral("」身份 ");
			handler.AppendFormatted(item.EffectiveAumid);
			handler.AppendLiteral("，已最小化=");
			handler.AppendFormatted(item.IsMinimized);
			stringBuilder3.AppendLine(provider2, ref handler);
		}
		report.AppendLine("     这些窗口能否被 SW_MINIMIZE 收掉，由「显示桌面」的人工核对覆盖——");
		report.AppendLine("     自动探针不去动用户已经打开的窗口。");
	}
}
