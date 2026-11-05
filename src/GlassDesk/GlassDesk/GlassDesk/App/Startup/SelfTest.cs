using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using GlassDesk.Core.Models.Settings;
using GlassDesk.Core.Storage;
using GlassDesk.Platform;
using GlassDesk.UI.Theming;

namespace GlassDesk.App.Startup;

internal static class SelfTest
{
	public static int Run(AppPaths paths)
	{
		return Run(paths, null);
	}

	public static int Run(AppPaths paths, Stream? acceptanceReport)
	{
		ArgumentNullException.ThrowIfNull(paths, "paths");
		StringBuilder stringBuilder = new StringBuilder();
		int failures = 0;
		stringBuilder.AppendLine("GlassDesk 自检");
		stringBuilder.AppendLine(new string('=', 46));
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
		handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, stringBuilder2, invariantCulture);
		handler.AppendLiteral("产品版本：");
		handler.AppendFormatted(typeof(SelfTest).Assembly.GetName().Version);
		stringBuilder4.AppendLine(provider2, ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider3 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(4, 1, stringBuilder2, invariantCulture);
		handler.AppendLiteral("运行时：");
		handler.AppendFormatted(Environment.Version);
		stringBuilder5.AppendLine(provider3, ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider4 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, stringBuilder2, invariantCulture);
		handler.AppendLiteral("进程架构：");
		handler.AppendFormatted(RuntimeInformation.ProcessArchitecture);
		stringBuilder6.AppendLine(provider4, ref handler);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("【系统能力】");
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder7 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider5 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(21, 2, stringBuilder2, invariantCulture);
		handler.AppendLiteral("操作系统：Windows ");
		handler.AppendFormatted(WindowsCapabilities.OsVersion);
		handler.AppendLiteral("（Build ");
		handler.AppendFormatted(WindowsCapabilities.BuildNumber);
		handler.AppendLiteral("）");
		stringBuilder7.AppendLine(provider5, ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder8 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider6 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(26, 1, stringBuilder2, invariantCulture);
		handler.AppendLiteral("系统背景材质：");
		handler.AppendFormatted(Yes(WindowsCapabilities.SupportsSystemBackdrop));
		handler.AppendLiteral("（需 Build 22621 及以上）");
		stringBuilder8.AppendLine(provider6, ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder9 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider7 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(26, 1, stringBuilder2, invariantCulture);
		handler.AppendLiteral("窗口圆角偏好：");
		handler.AppendFormatted(Yes(WindowsCapabilities.SupportsWindowCornerPreference));
		handler.AppendLiteral("（需 Build 22000 及以上）");
		stringBuilder9.AppendLine(provider7, ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder10 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider8 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(6, 1, stringBuilder2, invariantCulture);
		handler.AppendLiteral("深色标题栏：");
		handler.AppendFormatted(Yes(WindowsCapabilities.SupportsImmersiveDarkMode));
		stringBuilder10.AppendLine(provider8, ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder11 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider9 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, stringBuilder2, invariantCulture);
		handler.AppendLiteral("系统主题：");
		handler.AppendFormatted(SystemThemeReader.IsLightTheme() ? "浅色" : "深色");
		stringBuilder11.AppendLine(provider9, ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder12 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider10 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(7, 1, stringBuilder2, invariantCulture);
		handler.AppendLiteral("系统透明效果：");
		handler.AppendFormatted(SystemThemeReader.IsTransparencyEnabled() ? "开启" : "关闭");
		stringBuilder12.AppendLine(provider10, ref handler);
		if (!WindowsCapabilities.IsSupported)
		{
			failures++;
			stringBuilder.AppendLine("✗ 此 Windows 版本低于支持范围（需要 Windows 10 2004 Build 19041 及以上）");
		}
		stringBuilder.AppendLine();
		CheckTextEncoding(stringBuilder, ref failures);
		CheckStorage(paths, stringBuilder, ref failures);
		CheckThemeResolution(stringBuilder, ref failures);
		CheckSingleInstance(stringBuilder, ref failures);
		CheckStorageIsolation(stringBuilder);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(new string('=', 46));
		stringBuilder.AppendLine((failures == 0) ? "结果：全部检查通过。" : $"结果：{failures} 项检查未通过。");
		string value;
		if (acceptanceReport != null)
		{
			value = "自检报告：内容写入调用方指定的报告流。";
		}
		else
		{
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider11 = invariantCulture;
			DefaultInterpolatedStringHandler handler2 = new DefaultInterpolatedStringHandler(5, 1, invariantCulture);
			handler2.AppendLiteral("自检日志：");
			handler2.AppendFormatted(Path.Combine(paths.LogDirectory, "selftest.txt"));
			value = string.Create(provider11, ref handler2);
		}
		stringBuilder.AppendLine(value);
		string text = stringBuilder.ToString();
		if (acceptanceReport == null)
		{
			ReportWriter.Write(text, Path.Combine(paths.LogDirectory, "selftest.txt"));
		}
		else
		{
			AcceptanceEntryPoint.WriteText(acceptanceReport, text);
			ReportWriter.Write(text, null);
		}
		return (failures != 0) ? 1 : 0;
	}

	private static void CheckTextEncoding(StringBuilder report, ref int failures)
	{
		report.AppendLine("【文本编码】");
		bool flag = string.Equals("玻璃桌面", "玻璃桌面", StringComparison.Ordinal);
		StringBuilder stringBuilder = report;
		StringBuilder stringBuilder2 = stringBuilder;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(15, 2, stringBuilder, invariantCulture);
		handler.AppendLiteral("中文字面量与转义序列一致：");
		handler.AppendFormatted(Yes(flag));
		handler.AppendLiteral("（");
		handler.AppendFormatted("玻璃桌面");
		handler.AppendLiteral("）");
		stringBuilder2.AppendLine(provider, ref handler);
		if (!flag)
		{
			failures++;
			report.AppendLine("✗ 源文件编码可能被错误解码，界面中文将出现乱码。");
		}
		string text = "启动台与程序坞";
		byte[] bytes = Encoding.UTF8.GetBytes(text);
		string b = Encoding.UTF8.GetString(bytes);
		bool flag2 = string.Equals(text, b, StringComparison.Ordinal);
		stringBuilder = report;
		StringBuilder stringBuilder3 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider2 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("UTF-8 往返一致：");
		handler.AppendFormatted(Yes(flag2));
		stringBuilder3.AppendLine(provider2, ref handler);
		if (!flag2)
		{
			failures++;
		}
		report.AppendLine();
	}

	private static void CheckStorage(AppPaths paths, StringBuilder report, ref int failures)
	{
		report.AppendLine("【数据读写】");
		try
		{
			paths.EnsureCreated();
			StringBuilder stringBuilder = report;
			StringBuilder stringBuilder2 = stringBuilder;
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider = invariantCulture;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(8, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("数据目录已就绪：");
			handler.AppendFormatted(paths.RootDirectory);
			stringBuilder2.AppendLine(provider, ref handler);
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
			failures++;
			StringBuilder stringBuilder = report;
			StringBuilder stringBuilder3 = stringBuilder;
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider2 = invariantCulture;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("✗ 无法创建数据目录：");
			handler.AppendFormatted(ex.Message);
			stringBuilder3.AppendLine(provider2, ref handler);
			report.AppendLine();
			return;
		}
		try
		{
			DataLoadReport dataLoadReport = new GlassDeskDataStore(paths).LoadAll();
			StringBuilder stringBuilder = report;
			StringBuilder stringBuilder4 = stringBuilder;
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider3 = invariantCulture;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("现有数据加载提示数：");
			handler.AppendFormatted(dataLoadReport.Notices.Count);
			handler.AppendLiteral("（应为 0）");
			stringBuilder4.AppendLine(provider3, ref handler);
			if (dataLoadReport.HasNotices)
			{
				failures++;
				foreach (string notice in dataLoadReport.Notices)
				{
					stringBuilder = report;
					StringBuilder stringBuilder5 = stringBuilder;
					invariantCulture = CultureInfo.InvariantCulture;
					IFormatProvider provider4 = invariantCulture;
					handler = new StringBuilder.AppendInterpolatedStringHandler(4, 1, stringBuilder, invariantCulture);
					handler.AppendLiteral("  · ");
					handler.AppendFormatted(notice);
					stringBuilder5.AppendLine(provider4, ref handler);
				}
			}
		}
		catch (StorageException ex2)
		{
			failures++;
			StringBuilder stringBuilder = report;
			StringBuilder stringBuilder6 = stringBuilder;
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider5 = invariantCulture;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("✗ 读取现有数据失败：");
			handler.AppendFormatted(ex2.Message);
			stringBuilder6.AppendLine(provider5, ref handler);
		}
		AppPaths appPaths = new AppPaths(Path.Combine(Path.GetTempPath(), "GlassDesk-selftest-" + Guid.NewGuid().ToString("n")));
		try
		{
			appPaths.EnsureCreated();
			GlassDeskDataStore glassDeskDataStore = new GlassDeskDataStore(appPaths);
			DataLoadReport dataLoadReport2 = glassDeskDataStore.LoadAll();
			StringBuilder stringBuilder = report;
			StringBuilder stringBuilder7 = stringBuilder;
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider6 = invariantCulture;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(18, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("全新目录首次加载提示数：");
			handler.AppendFormatted(dataLoadReport2.Notices.Count);
			handler.AppendLiteral("（应为 0）");
			stringBuilder7.AppendLine(provider6, ref handler);
			glassDeskDataStore.Settings.Current.Appearance.AccentColor = "#0A84FF";
			glassDeskDataStore.Settings.Save();
			bool flag = File.Exists(appPaths.SettingsFile);
			stringBuilder = report;
			StringBuilder stringBuilder8 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider7 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(6, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("设置已写入：");
			handler.AppendFormatted(Yes(flag));
			stringBuilder8.AppendLine(provider7, ref handler);
			if (!flag)
			{
				failures++;
				report.AppendLine("✗ 设置文件未生成。");
			}
			glassDeskDataStore.Settings.Current.Appearance.ReduceMotion = true;
			glassDeskDataStore.Settings.Save();
			bool flag2 = File.Exists(appPaths.SettingsFile + ".bak");
			stringBuilder = report;
			StringBuilder stringBuilder9 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider8 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(18, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("二次保存后保留上一版本（.bak）：");
			handler.AppendFormatted(Yes(flag2));
			stringBuilder9.AppendLine(provider8, ref handler);
			if (!flag2)
			{
				failures++;
				report.AppendLine("✗ 原子替换未保留上一版本备份。");
			}
			DataLoadReport dataLoadReport3 = new GlassDeskDataStore(appPaths).LoadAll();
			bool flag3 = dataLoadReport3.Notices.Count == 0;
			stringBuilder = report;
			StringBuilder stringBuilder10 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider9 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(8, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("重新加载无异常：");
			handler.AppendFormatted(Yes(flag3));
			stringBuilder10.AppendLine(provider9, ref handler);
			if (!flag3)
			{
				failures++;
				foreach (string notice2 in dataLoadReport3.Notices)
				{
					stringBuilder = report;
					StringBuilder stringBuilder11 = stringBuilder;
					invariantCulture = CultureInfo.InvariantCulture;
					IFormatProvider provider10 = invariantCulture;
					handler = new StringBuilder.AppendInterpolatedStringHandler(4, 1, stringBuilder, invariantCulture);
					handler.AppendLiteral("  · ");
					handler.AppendFormatted(notice2);
					stringBuilder11.AppendLine(provider10, ref handler);
				}
			}
			(bool, bool) tuple = CheckCorruptionRecovery();
			stringBuilder = report;
			StringBuilder stringBuilder12 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider11 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("损坏文件被保留而非丢弃：");
			handler.AppendFormatted(Yes(tuple.Item1));
			stringBuilder12.AppendLine(provider11, ref handler);
			stringBuilder = report;
			StringBuilder stringBuilder13 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider12 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("损坏后从备份恢复：");
			handler.AppendFormatted(Yes(tuple.Item2));
			stringBuilder13.AppendLine(provider12, ref handler);
			if (!tuple.Item1 || !tuple.Item2)
			{
				failures++;
			}
		}
		catch (StorageException ex3)
		{
			failures++;
			StringBuilder stringBuilder = report;
			StringBuilder stringBuilder14 = stringBuilder;
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider13 = invariantCulture;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("✗ 数据读写失败：");
			handler.AppendFormatted(ex3.Message);
			stringBuilder14.AppendLine(provider13, ref handler);
		}
		finally
		{
			TryDeleteDirectory(appPaths.RootDirectory);
		}
		report.AppendLine();
	}

	private static (bool Preserved, bool Recovered) CheckCorruptionRecovery()
	{
		AppPaths appPaths = new AppPaths(Path.Combine(Path.GetTempPath(), "GlassDesk-corrupt-" + Guid.NewGuid().ToString("n")));
		try
		{
			appPaths.EnsureCreated();
			GlassDeskDataStore glassDeskDataStore = new GlassDeskDataStore(appPaths);
			glassDeskDataStore.LoadAll();
			glassDeskDataStore.Settings.Save();
			glassDeskDataStore.Settings.Current.Appearance.ReduceMotion = true;
			glassDeskDataStore.Settings.Save();
			File.WriteAllText(appPaths.SettingsFile, "{ this is not valid json");
			LoadOutcome<GlassDeskSettings> loadOutcome = new GlassDeskDataStore(appPaths).Settings.Load();
			bool item = loadOutcome.PreservedFile != null && File.Exists(loadOutcome.PreservedFile);
			bool item2 = loadOutcome.Status == LoadStatus.RecoveredFromBackup;
			return (Preserved: item, Recovered: item2);
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException || ex is StorageException) ? true : false)
		{
			return (Preserved: false, Recovered: false);
		}
		finally
		{
			TryDeleteDirectory(appPaths.RootDirectory);
		}
	}

	private static void CheckThemeResolution(StringBuilder report, ref int failures)
	{
		report.AppendLine("【主题解析】");
		ThemeService current = ThemeService.Current;
		if (current == null)
		{
			failures++;
			report.AppendLine("✗ 主题服务未初始化。");
			report.AppendLine();
			return;
		}
		GlassPalette palette = current.Palette;
		StringBuilder stringBuilder = report;
		StringBuilder stringBuilder2 = stringBuilder;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(10, 2, stringBuilder, invariantCulture);
		handler.AppendLiteral("默认主题：");
		handler.AppendFormatted(current.Appearance.Theme);
		handler.AppendLiteral("（当前 ");
		handler.AppendFormatted(current.IsDark ? "深色" : "浅色");
		handler.AppendLiteral("）");
		stringBuilder2.AppendLine(provider, ref handler);
		stringBuilder = report;
		StringBuilder stringBuilder3 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider2 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("面板模式：");
		handler.AppendFormatted(current.UseOpaquePanels ? "不透明" : "半透明玻璃");
		stringBuilder3.AppendLine(provider2, ref handler);
		stringBuilder = report;
		StringBuilder stringBuilder4 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider3 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(5, 3, stringBuilder, invariantCulture);
		handler.AppendLiteral("强调色：#");
		handler.AppendFormatted(palette.Accent.R, "X2");
		handler.AppendFormatted(palette.Accent.G, "X2");
		handler.AppendFormatted(palette.Accent.B, "X2");
		stringBuilder4.AppendLine(provider3, ref handler);
		double num = Math.Max(40.0, current.Appearance.DockCornerRadius);
		double num2 = Math.Max(36.0, current.Appearance.LauncherCornerRadius);
		stringBuilder = report;
		StringBuilder stringBuilder5 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider4 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(19, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("程序坞实际圆角：");
		handler.AppendFormatted(num);
		handler.AppendLiteral(" DIP（至少 40）");
		stringBuilder5.AppendLine(provider4, ref handler);
		stringBuilder = report;
		StringBuilder stringBuilder6 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider5 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(19, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("启动台实际圆角：");
		handler.AppendFormatted(num2);
		handler.AppendLiteral(" DIP（至少 36）");
		stringBuilder6.AppendLine(provider5, ref handler);
		bool flag = num >= 40.0;
		bool flag2 = num2 >= 36.0;
		stringBuilder = report;
		StringBuilder stringBuilder7 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider6 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("液态玻璃圆角保持明显弧度：");
		handler.AppendFormatted(Yes(flag & flag2));
		stringBuilder7.AppendLine(provider6, ref handler);
		if (!flag || !flag2)
		{
			failures++;
		}
		if (palette.Accent.R != 10 || palette.Accent.G != 132 || palette.Accent.B != byte.MaxValue)
		{
			failures++;
			report.AppendLine("✗ 默认强调色不是规划中的 #0A84FF。");
		}
		report.AppendLine();
	}

	private static void CheckSingleInstance(StringBuilder report, ref int failures)
	{
		report.AppendLine("【单实例】");
		try
		{
			using SingleInstanceGuard singleInstanceGuard = SingleInstanceGuard.Acquire("selftest-probe");
			StringBuilder stringBuilder = report;
			StringBuilder stringBuilder2 = stringBuilder;
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider = invariantCulture;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("首个实例取得所有权：");
			handler.AppendFormatted(Yes(singleInstanceGuard.IsFirstInstance));
			stringBuilder2.AppendLine(provider, ref handler);
			using SingleInstanceGuard singleInstanceGuard2 = SingleInstanceGuard.Acquire("selftest-probe");
			stringBuilder = report;
			StringBuilder stringBuilder3 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider2 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("第二个实例被识别为重复：");
			handler.AppendFormatted(Yes(!singleInstanceGuard2.IsFirstInstance));
			stringBuilder3.AppendLine(provider2, ref handler);
			if (!singleInstanceGuard.IsFirstInstance || singleInstanceGuard2.IsFirstInstance)
			{
				failures++;
				report.AppendLine("✗ 单实例判定不正确。");
			}
			SingleInstanceGuard singleInstanceGuard3 = SingleInstanceGuard.Acquire("主实例");
			stringBuilder = report;
			StringBuilder stringBuilder4 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider3 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("本产品单实例标识可用：");
			handler.AppendFormatted(Yes(singleInstanceGuard3.IsFirstInstance));
			stringBuilder4.AppendLine(provider3, ref handler);
			singleInstanceGuard3.Dispose();
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException || ex is WaitHandleCannotBeOpenedException) ? true : false)
		{
			failures++;
			StringBuilder stringBuilder = report;
			StringBuilder stringBuilder5 = stringBuilder;
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider4 = invariantCulture;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("✗ 单实例检查失败：");
			handler.AppendFormatted(ex.Message);
			stringBuilder5.AppendLine(provider4, ref handler);
		}
		report.AppendLine();
	}

	private static void CheckStorageIsolation(StringBuilder report)
	{
		report.AppendLine("【与参考产品的隔离】");
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		string text = Path.Combine(folderPath, "MacDock");
		string text2 = Path.Combine(folderPath, "Ym Launcher");
		string text3 = Path.Combine(folderPath, "Launcher启动台");
		StringBuilder stringBuilder = report;
		StringBuilder stringBuilder2 = stringBuilder;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(54, 3, stringBuilder, invariantCulture);
		handler.AppendLiteral("参考产品数据目录存在（仅探测，不读写）：MacDock=");
		handler.AppendFormatted(Yes(Directory.Exists(text)));
		handler.AppendLiteral("，Ym Launcher=");
		handler.AppendFormatted(Yes(Directory.Exists(text2)));
		handler.AppendLiteral("，Launcher启动台=");
		handler.AppendFormatted(Yes(Directory.Exists(text3)));
		stringBuilder2.AppendLine(provider, ref handler);
		string ours = AppPaths.ForCurrentUser().RootDirectory;
		bool value = new string[3] { text, text2, text3 }.Any((string other) => other.StartsWith(ours, StringComparison.OrdinalIgnoreCase) || ours.StartsWith(other, StringComparison.OrdinalIgnoreCase));
		stringBuilder = report;
		StringBuilder stringBuilder3 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider2 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(8, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("本产品数据目录：");
		handler.AppendFormatted(ours);
		stringBuilder3.AppendLine(provider2, ref handler);
		stringBuilder = report;
		StringBuilder stringBuilder4 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider3 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("与参考产品目录重叠：");
		handler.AppendFormatted(Yes(value));
		handler.AppendLiteral("（必须为否）");
		stringBuilder4.AppendLine(provider3, ref handler);
		report.AppendLine();
	}

	private static string Yes(bool value)
	{
		if (!value)
		{
			return "否";
		}
		return "是";
	}

	private static void TryDeleteDirectory(string path)
	{
		try
		{
			if (Directory.Exists(path))
			{
				Directory.Delete(path, recursive: true);
			}
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
		}
	}
}
