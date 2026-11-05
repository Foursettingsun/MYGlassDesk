using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using GlassDesk.Core.Storage;
using GlassDesk.Platform;
using GlassDesk.Platform.Displays;

namespace GlassDesk.App.Startup;

internal static class DiagnosticRunner
{
	public static int Run(AppPaths paths, Stream? acceptanceReport = null)
	{
		ArgumentNullException.ThrowIfNull(paths, "paths");
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		stringBuilder.AppendLine("GlassDesk 显示与坐标空间诊断");
		stringBuilder.AppendLine(new string('=', 52));
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(3, 1, stringBuilder2, invariantCulture);
		handler.AppendLiteral("时间：");
		handler.AppendFormatted(DateTimeOffset.Now, "yyyy-MM-dd HH:mm:ss zzz");
		stringBuilder3.AppendLine(provider, ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider2 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(21, 2, stringBuilder2, invariantCulture);
		handler.AppendLiteral("操作系统：Windows ");
		handler.AppendFormatted(WindowsCapabilities.OsVersion);
		handler.AppendLiteral("（Build ");
		handler.AppendFormatted(WindowsCapabilities.BuildNumber);
		handler.AppendLiteral("）");
		stringBuilder4.AppendLine(provider2, ref handler);
		stringBuilder.AppendLine();
		DpiAwarenessInfo dpiAwarenessInfo = DisplayService.ProbeDpiAwareness();
		stringBuilder.AppendLine("【DPI 感知】");
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider3 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(6, 1, stringBuilder2, invariantCulture);
		handler.AppendLiteral("线程上下文：");
		handler.AppendFormatted(dpiAwarenessInfo.RawContext);
		stringBuilder5.AppendLine(provider3, ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider4 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, stringBuilder2, invariantCulture);
		handler.AppendLiteral("感知等级：");
		handler.AppendFormatted(dpiAwarenessInfo.Describe());
		stringBuilder6.AppendLine(provider4, ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder7 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider5 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(9, 2, stringBuilder2, invariantCulture);
		handler.AppendLiteral("系统 DPI：");
		handler.AppendFormatted(dpiAwarenessInfo.SystemDpi);
		handler.AppendLiteral("（");
		handler.AppendFormatted((double)dpiAwarenessInfo.SystemDpi / 96.0, "P0");
		handler.AppendLiteral("）");
		stringBuilder7.AppendLine(provider5, ref handler);
		if (dpiAwarenessInfo.Level != DpiAwarenessLevel.PerMonitorAware || !dpiAwarenessInfo.IsPerMonitorV2)
		{
			num++;
			stringBuilder.AppendLine("✗ 不是 PerMonitorV2。窗口跨屏时不会按新显示器的 DPI 重新缩放，");
			stringBuilder.AppendLine("  物理像素与 DIP 的换算也会失去唯一依据。见下方坐标对比。");
		}
		else
		{
			stringBuilder.AppendLine("✓ 已具备每屏 DPI 感知 V2，坐标可按显示器各自换算。");
		}
		stringBuilder.AppendLine();
		IReadOnlyList<DisplayInfo> readOnlyList = DisplayService.Enumerate();
		stringBuilder.AppendLine("【显示器：Win32 物理像素】");
		if (readOnlyList.Count == 0)
		{
			num++;
			stringBuilder.AppendLine("✗ 未枚举到任何显示器。");
		}
		foreach (DisplayInfo item in readOnlyList)
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder8 = stringBuilder2;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider6 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(2, 1, stringBuilder2, invariantCulture);
			handler.AppendLiteral("· ");
			handler.AppendFormatted(item);
			stringBuilder8.AppendLine(provider6, ref handler);
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【WPF 视图：DIP】");
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder9 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider7 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(18, 4, stringBuilder2, invariantCulture);
		handler.AppendLiteral("主屏工作区：");
		handler.AppendFormatted(SystemParameters.WorkArea.Width, "F0");
		handler.AppendLiteral("×");
		handler.AppendFormatted(SystemParameters.WorkArea.Height, "F0");
		handler.AppendLiteral(" DIP，原点 (");
		handler.AppendFormatted(SystemParameters.WorkArea.Left, "F0");
		handler.AppendLiteral(",");
		handler.AppendFormatted(SystemParameters.WorkArea.Top, "F0");
		handler.AppendLiteral(")");
		stringBuilder9.AppendLine(provider7, ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder10 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider8 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(17, 4, stringBuilder2, invariantCulture);
		handler.AppendLiteral("虚拟屏幕：");
		handler.AppendFormatted(SystemParameters.VirtualScreenWidth, "F0");
		handler.AppendLiteral("×");
		handler.AppendFormatted(SystemParameters.VirtualScreenHeight, "F0");
		handler.AppendLiteral(" DIP，原点 (");
		handler.AppendFormatted(SystemParameters.VirtualScreenLeft, "F0");
		handler.AppendLiteral(",");
		handler.AppendFormatted(SystemParameters.VirtualScreenTop, "F0");
		handler.AppendLiteral(")");
		stringBuilder10.AppendLine(provider8, ref handler);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【两个坐标空间的对照（关键）】");
		stringBuilder.AppendLine("  物理像素来自 Win32，DIP 来自 WPF。二者只在 100% 缩放且原点重合时才相等。");
		stringBuilder.AppendLine();
		foreach (DisplayInfo item2 in readOnlyList)
		{
			PixelRect bounds = item2.Bounds;
			double value = item2.ToDips(bounds.Width);
			double value2 = item2.ToDips(bounds.Height);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder11 = stringBuilder2;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider9 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(2, 2, stringBuilder2, invariantCulture);
			handler.AppendLiteral("· ");
			handler.AppendFormatted(item2.DeviceName);
			handler.AppendFormatted(item2.IsPrimary ? "（主屏）" : string.Empty);
			stringBuilder11.AppendLine(provider9, ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder12 = stringBuilder2;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider10 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(21, 4, stringBuilder2, invariantCulture);
			handler.AppendLiteral("    物理像素：原点 (");
			handler.AppendFormatted(bounds.X);
			handler.AppendLiteral(",");
			handler.AppendFormatted(bounds.Y);
			handler.AppendLiteral(")  尺寸 ");
			handler.AppendFormatted(bounds.Width);
			handler.AppendLiteral("×");
			handler.AppendFormatted(bounds.Height);
			stringBuilder12.AppendLine(provider10, ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder13 = stringBuilder2;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider11 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(25, 3, stringBuilder2, invariantCulture);
			handler.AppendLiteral("    对应 DIP：尺寸约 ");
			handler.AppendFormatted(value, "F0");
			handler.AppendLiteral("×");
			handler.AppendFormatted(value2, "F0");
			handler.AppendLiteral("（按本屏 ");
			handler.AppendFormatted(item2.ScaleFactor, "P0");
			handler.AppendLiteral(" 缩放）");
			stringBuilder13.AppendLine(provider11, ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder14 = stringBuilder2;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider12 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(23, 3, stringBuilder2, invariantCulture);
			handler.AppendLiteral("    工作区：物理 ");
			handler.AppendFormatted(item2.WorkArea);
			handler.AppendLiteral("  →  DIP 约 ");
			handler.AppendFormatted(item2.ToDips(item2.WorkArea.Width), "F0");
			handler.AppendLiteral("×");
			handler.AppendFormatted(item2.ToDips(item2.WorkArea.Height), "F0");
			stringBuilder14.AppendLine(provider12, ref handler);
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【一致性检查】");
		DisplayInfo displayInfo = readOnlyList.FirstOrDefault((DisplayInfo d) => d.IsPrimary);
		if ((object)displayInfo != null)
		{
			double num2 = displayInfo.ToDips(displayInfo.WorkArea.Width);
			double width = SystemParameters.WorkArea.Width;
			double num3 = Math.Abs(num2 - width);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder15 = stringBuilder2;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider13 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(36, 3, stringBuilder2, invariantCulture);
			handler.AppendLiteral("主屏工作区宽度：物理换算 ");
			handler.AppendFormatted(num2, "F0");
			handler.AppendLiteral(" DIP vs WPF 报告 ");
			handler.AppendFormatted(width, "F0");
			handler.AppendLiteral(" DIP（差 ");
			handler.AppendFormatted(num3, "F0");
			handler.AppendLiteral("）");
			stringBuilder15.AppendLine(provider13, ref handler);
			if (num3 > 2.0)
			{
				num++;
				stringBuilder.AppendLine("✗ 两者不一致。说明换算基准与 WPF 实际使用的基准不同，");
				stringBuilder.AppendLine("  任何「物理像素 ↔ DIP」的手工换算都会偏。这是需要先解决的问题。");
			}
			else
			{
				stringBuilder.AppendLine("✓ 换算基准与 WPF 一致，可以按显示器 DPI 做像素↔DIP 换算。");
			}
		}
		int num4 = readOnlyList.Select((DisplayInfo d) => d.Dpi).Distinct().Count();
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder16 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider14 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(17, 2, stringBuilder2, invariantCulture);
		handler.AppendLiteral("显示器数量：");
		handler.AppendFormatted(readOnlyList.Count);
		handler.AppendLiteral("，不同 DPI 档位：");
		handler.AppendFormatted(num4);
		stringBuilder16.AppendLine(provider14, ref handler);
		if (num4 > 1)
		{
			stringBuilder.AppendLine("· 本机是混合 DPI 环境：跨屏坐标必须在各自 DPI 下换算，不能用单一缩放系数。");
		}
		if (readOnlyList.Any((DisplayInfo d) => d.Bounds.X < 0 || d.Bounds.Y < 0))
		{
			stringBuilder.AppendLine("· 存在负坐标显示器（主屏左侧或上方）：定位计算必须支持负原点，不能假设从 0 开始。");
		}
		num += WindowPlacementProbe.Run(stringBuilder, readOnlyList);
		stringBuilder.AppendLine();
		num += FullscreenProbe.Run(stringBuilder, readOnlyList);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(new string('=', 52));
		stringBuilder.AppendLine((num == 0) ? "结论：坐标空间与全屏检测均可用于后续实现。" : $"结论：有 {num} 项需要先解决。");
		string logFilePath = Path.Combine(paths.LogDirectory, "diagnose.txt");
		string text = stringBuilder.ToString();
		if (acceptanceReport == null)
		{
			ReportWriter.Write(text, logFilePath);
		}
		else
		{
			AcceptanceEntryPoint.WriteText(acceptanceReport, text);
			ReportWriter.Write(text, null);
		}
		return (num != 0) ? 1 : 0;
	}
}
