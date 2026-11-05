using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GlassDesk.AppServices;
using GlassDesk.Core.Contracts;
using GlassDesk.Core.Imaging;
using GlassDesk.Core.Models;
using GlassDesk.Core.Storage;
using GlassDesk.Platform.Shell;
using GlassDesk.Platform.Storage;

namespace GlassDesk.App.Startup;

internal static class AppServiceRunner
{
	public static int Run(AppPaths paths, CommandLineOptions options)
	{
		ArgumentNullException.ThrowIfNull(paths, "paths");
		ArgumentNullException.ThrowIfNull(options, "options");
		AppServiceVerb verb = options.Verb;
		bool flag = ((verb == AppServiceVerb.Phase3Acceptance || verb == AppServiceVerb.Phase4Acceptance || (uint)(verb - 20) <= 1u) ? true : false);
		if (flag && options.Unknown.Count > 0)
		{
			return AcceptanceArgumentsError("验收命令必须独立运行且参数完整");
		}
		switch (options.Verb)
		{
		case AppServiceVerb.Phase3Acceptance:
			if (options.VerbArguments.Count != 3)
			{
				return AcceptanceArgumentsError("--phase3-acceptance <图像输出> <报告输出> <新数据目录>");
			}
			return AcceptanceEntryPoint.Run(paths, options.VerbArguments[2], new _003C_003Ez__ReadOnlyArray<AcceptanceOutput>(new AcceptanceOutput[2]
			{
				new AcceptanceOutput("image", options.VerbArguments[0]),
				new AcceptanceOutput("report", options.VerbArguments[1])
			}), (AcceptanceWorkspace workspace) => Phase3Acceptance.Run(paths, options.VerbArguments, workspace));
		case AppServiceVerb.Phase4Acceptance:
			if (options.VerbArguments.Count != 2)
			{
				return AcceptanceArgumentsError("--phase4-acceptance <报告输出> <新数据目录>");
			}
			return AcceptanceEntryPoint.Run(paths, options.VerbArguments[1], new _003C_003Ez__ReadOnlySingleElementList<AcceptanceOutput>(new AcceptanceOutput("report", options.VerbArguments[0])), (AcceptanceWorkspace workspace) => Phase4Acceptance.Run(paths, options.VerbArguments, workspace));
		case AppServiceVerb.Phase5Acceptance:
			if (options.VerbArguments.Count != 2)
			{
				return AcceptanceArgumentsError("--phase5-acceptance <报告输出> <新数据目录>");
			}
			return AcceptanceEntryPoint.Run(paths, options.VerbArguments[1], new _003C_003Ez__ReadOnlySingleElementList<AcceptanceOutput>(new AcceptanceOutput("report", options.VerbArguments[0])), (AcceptanceWorkspace workspace) => Phase5Acceptance.Run(paths, options.VerbArguments, workspace));
		case AppServiceVerb.Phase6Acceptance:
			if (options.VerbArguments.Count != 2)
			{
				return AcceptanceArgumentsError("--phase6-acceptance <报告输出> <新数据目录>");
			}
			return AcceptanceEntryPoint.Run(paths, options.VerbArguments[1], new _003C_003Ez__ReadOnlySingleElementList<AcceptanceOutput>(new AcceptanceOutput("report", options.VerbArguments[0])), (AcceptanceWorkspace workspace) => Phase6Acceptance.Run(paths, options.VerbArguments, workspace));
		default:
		{
			StringBuilder stringBuilder = new StringBuilder();
			int result = 0;
			switch (options.Verb)
			{
			case AppServiceVerb.ProbeAumid:
				return Phase4Probe.RunAumid(paths);
			case AppServiceVerb.ProbeRunEvents:
				return Phase4Probe.RunEvents(paths);
			case AppServiceVerb.ProbeDragDrop:
				return DragDropProbe.Run(paths);
			case AppServiceVerb.ProbeShowDesktop:
				return ShowDesktopProbe.Run(paths);
			default:
			{
				stringBuilder.AppendLine("GlassDesk 应用库命令");
				stringBuilder.AppendLine(new string('=', 46));
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder3 = stringBuilder2;
				IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider = invariantCulture;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(3, 1, stringBuilder2, invariantCulture);
				handler.AppendLiteral("命令：");
				handler.AppendFormatted(DescribeVerb(options.Verb));
				stringBuilder3.AppendLine(provider, ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder4 = stringBuilder2;
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider2 = invariantCulture;
				handler = new StringBuilder.AppendInterpolatedStringHandler(3, 1, stringBuilder2, invariantCulture);
				handler.AppendLiteral("时间：");
				handler.AppendFormatted(DateTimeOffset.Now, "yyyy-MM-dd HH:mm:ss zzz");
				stringBuilder4.AppendLine(provider2, ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder5 = stringBuilder2;
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider3 = invariantCulture;
				handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, stringBuilder2, invariantCulture);
				handler.AppendLiteral("数据目录：");
				handler.AppendFormatted(paths.RootDirectory);
				stringBuilder5.AppendLine(provider3, ref handler);
				stringBuilder.AppendLine();
				try
				{
					GlassDeskDataStore glassDeskDataStore = new GlassDeskDataStore(paths);
					foreach (string notice in glassDeskDataStore.LoadAll().Notices)
					{
						stringBuilder2 = stringBuilder;
						StringBuilder stringBuilder6 = stringBuilder2;
						invariantCulture = CultureInfo.InvariantCulture;
						IFormatProvider provider4 = invariantCulture;
						handler = new StringBuilder.AppendInterpolatedStringHandler(7, 1, stringBuilder2, invariantCulture);
						handler.AppendLiteral("数据加载提示：");
						handler.AppendFormatted(notice);
						stringBuilder6.AppendLine(provider4, ref handler);
					}
					using AppServiceHost host = new AppServiceHost(glassDeskDataStore);
					result = options.Verb switch
					{
						AppServiceVerb.Import => RunImport(host, options, stringBuilder), 
						AppServiceVerb.ScanStartMenu => RunScan(options, stringBuilder), 
						AppServiceVerb.ImportStartMenu => RunImportStartMenu(host, options, stringBuilder), 
						AppServiceVerb.ListLibrary => RunList(host, options, stringBuilder), 
						AppServiceVerb.CheckLibrary => RunCheck(host, stringBuilder), 
						AppServiceVerb.Relocate => RunRelocate(host, options, stringBuilder), 
						AppServiceVerb.Launch => RunLaunch(host, options, stringBuilder, activate: false), 
						AppServiceVerb.Activate => RunLaunch(host, options, stringBuilder, activate: true), 
						AppServiceVerb.VerifyIcons => RunVerifyIcons(host, stringBuilder), 
						AppServiceVerb.IconContactSheet => RunContactSheet(host, options, stringBuilder), 
						AppServiceVerb.ClearIconCache => RunClearIcons(host, stringBuilder), 
						AppServiceVerb.MakeSamples => RunMakeSamples(options, stringBuilder), 
						AppServiceVerb.SeedLibrary => RunSeed(host, options, stringBuilder), 
						_ => 0, 
					};
					verb = options.Verb;
					if ((verb == AppServiceVerb.Phase3Acceptance || verb == AppServiceVerb.Phase4Acceptance) ? true : false)
					{
						return result;
					}
				}
				catch (StorageException ex)
				{
					stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder7 = stringBuilder2;
					invariantCulture = CultureInfo.InvariantCulture;
					IFormatProvider provider5 = invariantCulture;
					handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2, invariantCulture);
					handler.AppendLiteral("✗ 数据无法写入：");
					handler.AppendFormatted(ex.Message);
					stringBuilder7.AppendLine(provider5, ref handler);
					result = 1;
				}
				ReportWriter.Write(stringBuilder.ToString(), Path.Combine(paths.LogDirectory, "appservices.log"));
				return result;
			}
			}
		}
		}
	}

	private static int AcceptanceArgumentsError(string usage)
	{
		Console.Error.WriteLine("验收参数不足或冲突。用法：" + usage);
		return 2;
	}

	private static int RunImport(AppServiceHost host, CommandLineOptions options, StringBuilder report)
	{
		if (options.VerbArguments.Count == 0)
		{
			report.AppendLine("没有给出要导入的来源。");
			return 1;
		}
		List<ImportRequest> list = options.VerbArguments.Select(CreateRequest).ToList();
		StringBuilder stringBuilder = report;
		StringBuilder stringBuilder2 = stringBuilder;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("要导入的来源 ");
		handler.AppendFormatted(list.Count);
		handler.AppendLiteral(" 个：");
		stringBuilder2.AppendLine(provider, ref handler);
		foreach (ImportRequest item in list)
		{
			stringBuilder = report;
			StringBuilder stringBuilder3 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider2 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(5, 2, stringBuilder, invariantCulture);
			handler.AppendLiteral("  · ");
			handler.AppendFormatted(item.IsUrl ? "网址" : "路径");
			handler.AppendLiteral(" ");
			handler.AppendFormatted(item.Source);
			stringBuilder3.AppendLine(provider2, ref handler);
		}
		report.AppendLine();
		ImportResult importResult = host.Import.ImportItems(list);
		DescribeImport(importResult, report);
		DescribeIconWork(host, report);
		DescribeLibrarySize(host, report);
		if (importResult.Failures.Count <= 0 || importResult.Added.Count != 0)
		{
			return 0;
		}
		return 1;
	}

	private static int RunScan(CommandLineOptions options, StringBuilder report)
	{
		StartMenuScanResult startMenuScanResult = AppServiceHost.ScanStartMenu();
		report.AppendLine("「开始」菜单扫描");
		StringBuilder stringBuilder = report;
		StringBuilder stringBuilder2 = stringBuilder;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("扫描的根目录 ");
		handler.AppendFormatted(startMenuScanResult.Roots.Count);
		handler.AppendLiteral(" 个：");
		stringBuilder2.AppendLine(provider, ref handler);
		foreach (string root in startMenuScanResult.Roots)
		{
			stringBuilder = report;
			StringBuilder stringBuilder3 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider2 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(4, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("  · ");
			handler.AppendFormatted(root);
			stringBuilder3.AppendLine(provider2, ref handler);
		}
		report.AppendLine();
		stringBuilder = report;
		StringBuilder stringBuilder4 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider3 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("看到的 .lnk 文件：");
		handler.AppendFormatted(startMenuScanResult.ScannedFileCount);
		handler.AppendLiteral(" 个");
		stringBuilder4.AppendLine(provider3, ref handler);
		stringBuilder = report;
		StringBuilder stringBuilder5 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider4 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(15, 2, stringBuilder, invariantCulture);
		handler.AppendLiteral("去重后：");
		handler.AppendFormatted(startMenuScanResult.Shortcuts.Count);
		handler.AppendLiteral(" 个（同名让位 ");
		handler.AppendFormatted(startMenuScanResult.DuplicateNameCount);
		handler.AppendLiteral(" 个）");
		stringBuilder5.AppendLine(provider4, ref handler);
		int num = ReadIntOption(options.VerbArguments, 0, 40);
		report.AppendLine();
		stringBuilder = report;
		StringBuilder stringBuilder6 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider5 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("按分组与名称排序，列出前 ");
		handler.AppendFormatted(Math.Min(num, startMenuScanResult.Shortcuts.Count));
		handler.AppendLiteral(" 条：");
		stringBuilder6.AppendLine(provider5, ref handler);
		foreach (StartMenuShortcut item in startMenuScanResult.Shortcuts.Take(num))
		{
			string value = ((item.Group.Length == 0) ? "（根）" : item.Group);
			string value2 = (item.FromCurrentUser ? "当前用户" : "所有用户");
			stringBuilder = report;
			StringBuilder stringBuilder7 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider6 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 3, stringBuilder, invariantCulture);
			handler.AppendLiteral("  · [");
			handler.AppendFormatted(value2);
			handler.AppendLiteral("] ");
			handler.AppendFormatted(value);
			handler.AppendLiteral(" / ");
			handler.AppendFormatted(item.Name);
			stringBuilder7.AppendLine(provider6, ref handler);
			stringBuilder = report;
			StringBuilder stringBuilder8 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider7 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(6, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("      ");
			handler.AppendFormatted(item.Path);
			stringBuilder8.AppendLine(provider7, ref handler);
		}
		return 0;
	}

	private static int RunImportStartMenu(AppServiceHost host, CommandLineOptions options, StringBuilder report)
	{
		StartMenuScanResult startMenuScanResult = AppServiceHost.ScanStartMenu();
		StringBuilder stringBuilder = report;
		StringBuilder stringBuilder2 = stringBuilder;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(28, 3, stringBuilder, invariantCulture);
		handler.AppendLiteral("「开始」菜单里找到 ");
		handler.AppendFormatted(startMenuScanResult.Shortcuts.Count);
		handler.AppendLiteral(" 条（原始 ");
		handler.AppendFormatted(startMenuScanResult.ScannedFileCount);
		handler.AppendLiteral(" 条，同名让位 ");
		handler.AppendFormatted(startMenuScanResult.DuplicateNameCount);
		handler.AppendLiteral(" 条）。");
		stringBuilder2.AppendLine(provider, ref handler);
		int count = ReadIntOption(options.VerbArguments, 0, int.MaxValue);
		List<StartMenuShortcut> list = startMenuScanResult.Shortcuts.Take(count).ToList();
		stringBuilder = report;
		StringBuilder stringBuilder3 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider2 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("本次导入其中 ");
		handler.AppendFormatted(list.Count);
		handler.AppendLiteral(" 条。");
		stringBuilder3.AppendLine(provider2, ref handler);
		report.AppendLine();
		DescribeImport(host.ImportStartMenuShortcuts(list), report);
		DescribeIconWork(host, report);
		DescribeLibrarySize(host, report);
		return 0;
	}

	private static int RunList(AppServiceHost host, CommandLineOptions options, StringBuilder report)
	{
		AppLibraryStore library = host.Library;
		List<AppEntry> list = library.Items.Where((AppEntry entry) => entry?.IsUsable ?? false).ToList();
		int num = library.Items.Count - list.Count;
		StringBuilder stringBuilder = report;
		StringBuilder stringBuilder2 = stringBuilder;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("应用库共 ");
		handler.AppendFormatted(list.Count);
		handler.AppendLiteral(" 条可用的记录。");
		stringBuilder2.AppendLine(provider, ref handler);
		if (num > 0)
		{
			stringBuilder = report;
			StringBuilder stringBuilder3 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider2 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(28, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("另有 ");
			handler.AppendFormatted(num);
			handler.AppendLiteral(" 条无法使用（空条目或缺字段），保留在文件中未动。");
			stringBuilder3.AppendLine(provider2, ref handler);
		}
		int value = Enumerable.Count(list, (AppEntry entry) => entry.IsTargetMissing);
		stringBuilder = report;
		StringBuilder stringBuilder4 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider3 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("其中标记为失效的：");
		handler.AppendFormatted(value);
		handler.AppendLiteral(" 条");
		stringBuilder4.AppendLine(provider3, ref handler);
		int value2 = Enumerable.Count(list, (AppEntry entry) => !string.IsNullOrEmpty(entry.Arguments));
		stringBuilder = report;
		StringBuilder stringBuilder5 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider4 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("带启动参数的：");
		handler.AppendFormatted(value2);
		handler.AppendLiteral(" 条");
		stringBuilder5.AppendLine(provider4, ref handler);
		int value3 = Enumerable.Count(list, (AppEntry entry) => !string.IsNullOrEmpty(entry.CustomIconPath));
		stringBuilder = report;
		StringBuilder stringBuilder6 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider5 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("带自定义图标的：");
		handler.AppendFormatted(value3);
		handler.AppendLiteral(" 条");
		stringBuilder6.AppendLine(provider5, ref handler);
		report.AppendLine();
		int num2 = ReadIntOption(options.VerbArguments, 0, 20);
		stringBuilder = report;
		StringBuilder stringBuilder7 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider6 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("明细（按库中顺序，最多 ");
		handler.AppendFormatted(num2);
		handler.AppendLiteral(" 条）：");
		stringBuilder7.AppendLine(provider6, ref handler);
		int num3 = 0;
		foreach (AppEntry item in list)
		{
			num3++;
			string value4 = (item.IsTargetMissing ? "失效" : "正常");
			stringBuilder = report;
			StringBuilder stringBuilder8 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider7 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(8, 4, stringBuilder, invariantCulture);
			handler.AppendLiteral("  ");
			handler.AppendFormatted(num3, 3);
			handler.AppendLiteral(". [");
			handler.AppendFormatted(DescribeKind(item.Kind));
			handler.AppendLiteral("/");
			handler.AppendFormatted(value4);
			handler.AppendLiteral("] ");
			handler.AppendFormatted(item.DisplayName);
			stringBuilder8.AppendLine(provider7, ref handler);
			stringBuilder = report;
			StringBuilder stringBuilder9 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider8 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("       id       ");
			handler.AppendFormatted(item.Id);
			stringBuilder9.AppendLine(provider8, ref handler);
			stringBuilder = report;
			StringBuilder stringBuilder10 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider9 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("       目标     ");
			handler.AppendFormatted(item.Target);
			stringBuilder10.AppendLine(provider9, ref handler);
			if (!string.IsNullOrEmpty(item.Arguments))
			{
				stringBuilder = report;
				StringBuilder stringBuilder11 = stringBuilder;
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider10 = invariantCulture;
				handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder, invariantCulture);
				handler.AppendLiteral("       参数     ");
				handler.AppendFormatted(item.Arguments);
				stringBuilder11.AppendLine(provider10, ref handler);
			}
			if (!string.IsNullOrEmpty(item.WorkingDirectory))
			{
				stringBuilder = report;
				StringBuilder stringBuilder12 = stringBuilder;
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider11 = invariantCulture;
				handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder, invariantCulture);
				handler.AppendLiteral("       工作目录 ");
				handler.AppendFormatted(item.WorkingDirectory);
				stringBuilder12.AppendLine(provider11, ref handler);
			}
			if (!string.IsNullOrEmpty(item.CustomIconPath))
			{
				stringBuilder = report;
				StringBuilder stringBuilder13 = stringBuilder;
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider12 = invariantCulture;
				handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, stringBuilder, invariantCulture);
				handler.AppendLiteral("       自定义图标 ");
				handler.AppendFormatted(item.CustomIconPath);
				stringBuilder13.AppendLine(provider12, ref handler);
			}
			if (num2 > 0 && num3 >= num2 && num3 < list.Count)
			{
				stringBuilder = report;
				StringBuilder stringBuilder14 = stringBuilder;
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider13 = invariantCulture;
				handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder, invariantCulture);
				handler.AppendLiteral("  ……（其余 ");
				handler.AppendFormatted(list.Count - num3);
				handler.AppendLiteral(" 条未列出）");
				stringBuilder14.AppendLine(provider13, ref handler);
				break;
			}
		}
		return 0;
	}

	private static int RunCheck(AppServiceHost host, StringBuilder report)
	{
		MissingTargetReport missingTargetReport = host.Maintenance.RefreshMissingTargets();
		report.AppendLine("失效目标复查");
		StringBuilder stringBuilder = report;
		StringBuilder stringBuilder2 = stringBuilder;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("检查条目：");
		handler.AppendFormatted(missingTargetReport.Checked);
		stringBuilder2.AppendLine(provider, ref handler);
		stringBuilder = report;
		StringBuilder stringBuilder3 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider2 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(6, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("目标找不到：");
		handler.AppendFormatted(missingTargetReport.Missing);
		stringBuilder3.AppendLine(provider2, ref handler);
		stringBuilder = report;
		StringBuilder stringBuilder4 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider3 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(6, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("状态有变化：");
		handler.AppendFormatted(missingTargetReport.Changed ? "是，已保存" : "否，未写文件");
		stringBuilder4.AppendLine(provider3, ref handler);
		report.AppendLine();
		if (missingTargetReport.NewlyBroken.Count > 0)
		{
			report.AppendLine("本次新发现失效（条目保留、只标记）：");
			foreach (AppEntry item in missingTargetReport.NewlyBroken)
			{
				stringBuilder = report;
				StringBuilder stringBuilder5 = stringBuilder;
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider4 = invariantCulture;
				handler = new StringBuilder.AppendInterpolatedStringHandler(4, 1, stringBuilder, invariantCulture);
				handler.AppendLiteral("  · ");
				handler.AppendFormatted(item.DisplayName);
				stringBuilder5.AppendLine(provider4, ref handler);
				stringBuilder = report;
				StringBuilder stringBuilder6 = stringBuilder;
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider5 = invariantCulture;
				handler = new StringBuilder.AppendInterpolatedStringHandler(6, 1, stringBuilder, invariantCulture);
				handler.AppendLiteral("      ");
				handler.AppendFormatted(item.Target);
				stringBuilder6.AppendLine(provider5, ref handler);
			}
			report.AppendLine();
		}
		if (missingTargetReport.Recovered.Count > 0)
		{
			report.AppendLine("目标又回来了，已取消失效标记：");
			foreach (AppEntry item2 in missingTargetReport.Recovered)
			{
				stringBuilder = report;
				StringBuilder stringBuilder7 = stringBuilder;
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider6 = invariantCulture;
				handler = new StringBuilder.AppendInterpolatedStringHandler(4, 1, stringBuilder, invariantCulture);
				handler.AppendLiteral("  · ");
				handler.AppendFormatted(item2.DisplayName);
				stringBuilder7.AppendLine(provider6, ref handler);
			}
			report.AppendLine();
		}
		if (!missingTargetReport.Changed && missingTargetReport.Missing == 0)
		{
			report.AppendLine("全部条目的目标都在。");
		}
		return 0;
	}

	private static int RunRelocate(AppServiceHost host, CommandLineOptions options, StringBuilder report)
	{
		if (options.VerbArguments.Count < 2)
		{
			report.AppendLine("用法：--relocate <条目 id> <新路径>");
			return 1;
		}
		RelocationOutcome relocationOutcome = host.Maintenance.Relocate(options.VerbArguments[0], options.VerbArguments[1]);
		StringBuilder stringBuilder = report;
		StringBuilder stringBuilder2 = stringBuilder;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(3, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("结果：");
		handler.AppendFormatted(relocationOutcome.Result);
		stringBuilder2.AppendLine(provider, ref handler);
		stringBuilder = report;
		StringBuilder stringBuilder3 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider2 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(3, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("说明：");
		handler.AppendFormatted(relocationOutcome.Message);
		stringBuilder3.AppendLine(provider2, ref handler);
		if (relocationOutcome.Result == RelocateResult.Relocated)
		{
			AppEntry appEntry = host.Library.Find(options.VerbArguments[0]);
			if (appEntry != null)
			{
				report.AppendLine();
				stringBuilder = report;
				StringBuilder stringBuilder4 = stringBuilder;
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider3 = invariantCulture;
				handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder, invariantCulture);
				handler.AppendLiteral("  名称     ");
				handler.AppendFormatted(appEntry.DisplayName);
				stringBuilder4.AppendLine(provider3, ref handler);
				stringBuilder = report;
				StringBuilder stringBuilder5 = stringBuilder;
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider4 = invariantCulture;
				handler = new StringBuilder.AppendInterpolatedStringHandler(8, 1, stringBuilder, invariantCulture);
				handler.AppendLiteral("  新目标   ");
				handler.AppendFormatted(appEntry.Target);
				stringBuilder5.AppendLine(provider4, ref handler);
				stringBuilder = report;
				StringBuilder stringBuilder6 = stringBuilder;
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider5 = invariantCulture;
				handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder, invariantCulture);
				handler.AppendLiteral("  类型     ");
				handler.AppendFormatted(DescribeKind(appEntry.Kind));
				stringBuilder6.AppendLine(provider5, ref handler);
				stringBuilder = report;
				StringBuilder stringBuilder7 = stringBuilder;
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider6 = invariantCulture;
				handler = new StringBuilder.AppendInterpolatedStringHandler(7, 1, stringBuilder, invariantCulture);
				handler.AppendLiteral("  失效标记 ");
				handler.AppendFormatted(appEntry.IsTargetMissing);
				stringBuilder7.AppendLine(provider6, ref handler);
				stringBuilder = report;
				StringBuilder stringBuilder8 = stringBuilder;
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider7 = invariantCulture;
				handler = new StringBuilder.AppendInterpolatedStringHandler(7, 1, stringBuilder, invariantCulture);
				handler.AppendLiteral("  图标来源 ");
				handler.AppendFormatted(appEntry.IconSource);
				stringBuilder8.AppendLine(provider7, ref handler);
			}
		}
		return (relocationOutcome.Result != RelocateResult.Relocated) ? 1 : 0;
	}

	private static int RunLaunch(AppServiceHost host, CommandLineOptions options, StringBuilder report, bool activate)
	{
		if (options.VerbArguments.Count == 0)
		{
			report.AppendLine(activate ? "用法：--activate <条目 id>" : "用法：--launch <条目 id>");
			return 1;
		}
		string text = options.VerbArguments[0];
		AppEntry appEntry = host.Library.Find(text);
		StringBuilder stringBuilder;
		IFormatProvider invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler;
		if (appEntry != null)
		{
			stringBuilder = report;
			StringBuilder stringBuilder2 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(5, 2, stringBuilder, invariantCulture);
			handler.AppendLiteral("条目：");
			handler.AppendFormatted(appEntry.DisplayName);
			handler.AppendLiteral("（");
			handler.AppendFormatted(DescribeKind(appEntry.Kind));
			handler.AppendLiteral("）");
			stringBuilder2.AppendLine(provider, ref handler);
			stringBuilder = report;
			StringBuilder stringBuilder3 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider2 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(3, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("目标：");
			handler.AppendFormatted(appEntry.Target);
			stringBuilder3.AppendLine(provider2, ref handler);
			if (!string.IsNullOrEmpty(appEntry.Arguments))
			{
				stringBuilder = report;
				StringBuilder stringBuilder4 = stringBuilder;
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider3 = invariantCulture;
				handler = new StringBuilder.AppendInterpolatedStringHandler(3, 1, stringBuilder, invariantCulture);
				handler.AppendLiteral("参数：");
				handler.AppendFormatted(appEntry.Arguments);
				stringBuilder4.AppendLine(provider3, ref handler);
			}
			report.AppendLine();
		}
		if (activate)
		{
			ActivationResult activationResult = host.Launch.ActivateOrLaunch(text);
			stringBuilder = report;
			StringBuilder stringBuilder5 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider4 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(3, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("结果：");
			handler.AppendFormatted(DescribeActivation(activationResult.Outcome));
			stringBuilder5.AppendLine(provider4, ref handler);
			foreach (WindowCandidate candidate in activationResult.Candidates)
			{
				stringBuilder = report;
				StringBuilder stringBuilder6 = stringBuilder;
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider5 = invariantCulture;
				handler = new StringBuilder.AppendInterpolatedStringHandler(12, 2, stringBuilder, invariantCulture);
				handler.AppendLiteral("  · 候选窗口 0x");
				handler.AppendFormatted(candidate.Handle, "X");
				handler.AppendLiteral(" ");
				handler.AppendFormatted(candidate.Title);
				stringBuilder6.AppendLine(provider5, ref handler);
			}
			if (!string.IsNullOrEmpty(activationResult.Message))
			{
				stringBuilder = report;
				StringBuilder stringBuilder7 = stringBuilder;
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider6 = invariantCulture;
				handler = new StringBuilder.AppendInterpolatedStringHandler(3, 1, stringBuilder, invariantCulture);
				handler.AppendLiteral("说明：");
				handler.AppendFormatted(activationResult.Message);
				stringBuilder7.AppendLine(provider6, ref handler);
			}
			ActivationOutcome outcome = activationResult.Outcome;
			bool flag = (((uint)outcome <= 2u || outcome == ActivationOutcome.Minimized) ? true : false);
			return (!flag) ? 1 : 0;
		}
		LaunchResult launchResult = host.Launch.LaunchItem(text);
		stringBuilder = report;
		StringBuilder stringBuilder8 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider7 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(3, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("结果：");
		handler.AppendFormatted(launchResult.Succeeded ? "已交给外壳打开" : "未能打开");
		stringBuilder8.AppendLine(provider7, ref handler);
		if (launchResult.Failure.HasValue)
		{
			stringBuilder = report;
			StringBuilder stringBuilder9 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider8 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("失败分类：");
			handler.AppendFormatted(launchResult.Failure);
			stringBuilder9.AppendLine(provider8, ref handler);
		}
		if (!string.IsNullOrEmpty(launchResult.Message))
		{
			stringBuilder = report;
			StringBuilder stringBuilder10 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider9 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(3, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("说明：");
			handler.AppendFormatted(launchResult.Message);
			stringBuilder10.AppendLine(provider9, ref handler);
		}
		if (launchResult.Succeeded)
		{
			stringBuilder = report;
			StringBuilder stringBuilder11 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider10 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("被启动的进程 id：");
			handler.AppendFormatted(host.Launch.LastLaunchedProcessId);
			stringBuilder11.AppendLine(provider10, ref handler);
		}
		return (!launchResult.Succeeded) ? 1 : 0;
	}

	private static int RunVerifyIcons(AppServiceHost host, StringBuilder report)
	{
		string iconCacheDirectory = host.Paths.IconCacheDirectory;
		if (!Directory.Exists(iconCacheDirectory))
		{
			report.AppendLine("图标缓存目录不存在。");
			return 0;
		}
		string[] files = Directory.GetFiles(iconCacheDirectory, "*.png");
		StringBuilder stringBuilder = report;
		StringBuilder stringBuilder2 = stringBuilder;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("缓存目录：");
		handler.AppendFormatted(iconCacheDirectory);
		stringBuilder2.AppendLine(provider, ref handler);
		stringBuilder = report;
		StringBuilder stringBuilder3 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider2 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("PNG 文件：");
		handler.AppendFormatted(files.Length);
		handler.AppendLiteral(" 个");
		stringBuilder3.AppendLine(provider2, ref handler);
		report.AppendLine();
		int num = 0;
		long num2 = 0L;
		long num3 = long.MaxValue;
		long num4 = 0L;
		string[] array = files;
		foreach (string path in array)
		{
			try
			{
				using FileStream bitmapStream = File.OpenRead(path);
				BitmapFrame bitmapFrame = new PngBitmapDecoder(bitmapStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
				int num5 = CountVisiblePixels(bitmapFrame);
				long num6 = (long)bitmapFrame.PixelWidth * (long)bitmapFrame.PixelHeight;
				num2 += num6;
				num3 = Math.Min(num3, num6);
				num4 = Math.Max(num4, num6);
				if (num5 == 0)
				{
					num++;
					stringBuilder = report;
					StringBuilder stringBuilder4 = stringBuilder;
					invariantCulture = CultureInfo.InvariantCulture;
					IFormatProvider provider3 = invariantCulture;
					handler = new StringBuilder.AppendInterpolatedStringHandler(13, 3, stringBuilder, invariantCulture);
					handler.AppendLiteral("  ✗ ");
					handler.AppendFormatted(Path.GetFileName(path));
					handler.AppendLiteral(" ");
					handler.AppendFormatted(bitmapFrame.PixelWidth);
					handler.AppendLiteral("×");
					handler.AppendFormatted(bitmapFrame.PixelHeight);
					handler.AppendLiteral(" 全是透明像素");
					stringBuilder4.AppendLine(provider3, ref handler);
				}
				else
				{
					stringBuilder = report;
					StringBuilder stringBuilder5 = stringBuilder;
					invariantCulture = CultureInfo.InvariantCulture;
					IFormatProvider provider4 = invariantCulture;
					handler = new StringBuilder.AppendInterpolatedStringHandler(13, 5, stringBuilder, invariantCulture);
					handler.AppendLiteral("  ✓ ");
					handler.AppendFormatted(Path.GetFileName(path));
					handler.AppendLiteral(" ");
					handler.AppendFormatted(bitmapFrame.PixelWidth);
					handler.AppendLiteral("×");
					handler.AppendFormatted(bitmapFrame.PixelHeight);
					handler.AppendLiteral(" 可见像素 ");
					handler.AppendFormatted(num5);
					handler.AppendLiteral(" ");
					handler.AppendFormatted(bitmapFrame.Format);
					stringBuilder5.AppendLine(provider4, ref handler);
				}
			}
			catch (Exception ex) when ((ex is FileFormatException || ex is NotSupportedException || ex is IOException) ? true : false)
			{
				num++;
				stringBuilder = report;
				StringBuilder stringBuilder6 = stringBuilder;
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider5 = invariantCulture;
				handler = new StringBuilder.AppendInterpolatedStringHandler(10, 2, stringBuilder, invariantCulture);
				handler.AppendLiteral("  ✗ ");
				handler.AppendFormatted(Path.GetFileName(path));
				handler.AppendLiteral(" 解码失败：");
				handler.AppendFormatted(ex.Message);
				stringBuilder6.AppendLine(provider5, ref handler);
			}
		}
		report.AppendLine();
		stringBuilder = report;
		StringBuilder stringBuilder7 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider6 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("像素总数：");
		handler.AppendFormatted(num2);
		stringBuilder7.AppendLine(provider6, ref handler);
		if (files.Length != 0)
		{
			stringBuilder = report;
			StringBuilder stringBuilder8 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider7 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(15, 2, stringBuilder, invariantCulture);
			handler.AppendLiteral("单张最小 ");
			handler.AppendFormatted(num3);
			handler.AppendLiteral(" 像素，最大 ");
			handler.AppendFormatted(num4);
			handler.AppendLiteral(" 像素");
			stringBuilder8.AppendLine(provider7, ref handler);
		}
		string value = ((num == 0) ? "全部图标都能被系统解码器读出，且有可见内容。" : $"有 {num} 个文件不可用。");
		report.AppendLine(value);
		return (num != 0) ? 1 : 0;
	}

	private static int CountVisiblePixels(BitmapSource frame)
	{
		FormatConvertedBitmap formatConvertedBitmap = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0.0);
		int num = formatConvertedBitmap.PixelWidth * 4;
		byte[] array = new byte[num * formatConvertedBitmap.PixelHeight];
		formatConvertedBitmap.CopyPixels(array, num, 0);
		int num2 = 0;
		for (int i = 3; i < array.Length; i += 4)
		{
			if (array[i] != 0)
			{
				num2++;
			}
		}
		return num2;
	}

	private static int RunContactSheet(AppServiceHost host, CommandLineOptions options, StringBuilder report)
	{
		List<AppEntry> list = host.Library.Items.Where((AppEntry entry) => entry != null && entry.IsUsable && !string.IsNullOrWhiteSpace(entry.IconSource)).Take(ReadIntOption(options.VerbArguments, 0, 60)).ToList();
		if (list.Count == 0)
		{
			report.AppendLine("应用库里没有带图标来源的记录。");
			return 1;
		}
		int num = (list.Count + 10 - 1) / 10;
		int num2 = 2560;
		byte[] array = new byte[num2 * num * 64];
		int num3 = 0;
		for (int num4 = 0; num4 < list.Count; num4++)
		{
			AppEntry appEntry = list[num4];
			string text = host.Icons.EnsureFor(appEntry.IconSource)?.Path;
			if (string.IsNullOrWhiteSpace(text) || !File.Exists(text))
			{
				continue;
			}
			try
			{
				using FileStream bitmapStream = File.OpenRead(text);
				FormatConvertedBitmap formatConvertedBitmap = new FormatConvertedBitmap(new PngBitmapDecoder(bitmapStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0], PixelFormats.Bgra32, null, 0.0);
				int pixelWidth = formatConvertedBitmap.PixelWidth;
				int pixelHeight = formatConvertedBitmap.PixelHeight;
				int num5 = pixelWidth * 4;
				byte[] array2 = new byte[num5 * pixelHeight];
				formatConvertedBitmap.CopyPixels(array2, num5, 0);
				int num6 = num4 % 10;
				int num7 = num4 / 10;
				for (int num8 = 0; num8 < 64; num8++)
				{
					int num9 = num8 * pixelHeight / 64;
					for (int num10 = 0; num10 < 64; num10++)
					{
						int num11 = num10 * pixelWidth / 64;
						int num12 = num9 * num5 + num11 * 4;
						int num13 = (num7 * 64 + num8) * num2 + (num6 * 64 + num10) * 4;
						array[num13] = array2[num12];
						array[num13 + 1] = array2[num12 + 1];
						array[num13 + 2] = array2[num12 + 2];
						array[num13 + 3] = array2[num12 + 3];
					}
				}
				num3++;
			}
			catch (Exception ex) when ((ex is FileFormatException || ex is IOException) ? true : false)
			{
			}
		}
		string text2 = ((options.VerbArguments.Count > 1) ? options.VerbArguments[1] : Path.Combine(Path.GetTempPath(), "glassdesk-icon-contact-sheet.png"));
		byte[] array3 = PngWriter.EncodeBgra(array, 640, num * 64);
		File.WriteAllBytes(text2, array3);
		StringBuilder stringBuilder = report;
		StringBuilder stringBuilder2 = stringBuilder;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("库中带图标来源的记录：");
		handler.AppendFormatted(list.Count);
		stringBuilder2.AppendLine(provider, ref handler);
		stringBuilder = report;
		StringBuilder stringBuilder3 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider2 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("实际画出：");
		handler.AppendFormatted(num3);
		stringBuilder3.AppendLine(provider2, ref handler);
		stringBuilder = report;
		StringBuilder stringBuilder4 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider3 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(11, 4, stringBuilder, invariantCulture);
		handler.AppendLiteral("联系表：");
		handler.AppendFormatted(text2);
		handler.AppendLiteral("（");
		handler.AppendFormatted(640);
		handler.AppendLiteral("×");
		handler.AppendFormatted(num * 64);
		handler.AppendLiteral("，");
		handler.AppendFormatted(array3.Length);
		handler.AppendLiteral(" 字节）");
		stringBuilder4.AppendLine(provider3, ref handler);
		return (num3 <= 0) ? 1 : 0;
	}

	private static int RunClearIcons(AppServiceHost host, StringBuilder report)
	{
		IconCacheClearResult iconCacheClearResult = host.Icons.Clear();
		StringBuilder stringBuilder = report;
		StringBuilder stringBuilder2 = stringBuilder;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("已删除 ");
		handler.AppendFormatted(iconCacheClearResult.Removed);
		handler.AppendLiteral(" 个可再生缓存图标文件。");
		stringBuilder2.AppendLine(provider, ref handler);
		report.AppendLine("自定义图标（custom-*）不清：它们不是缓存，是从用户选的文件复制进来的，删了找不回来。");
		if (iconCacheClearResult.LeftOthers)
		{
			stringBuilder = report;
			StringBuilder stringBuilder3 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider2 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(19, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("另有 ");
			handler.AppendFormatted(iconCacheClearResult.LeftAlone);
			handler.AppendLiteral(" 个文件不属于缓存，已原样保留。");
			stringBuilder3.AppendLine(provider2, ref handler);
		}
		report.AppendLine("条目上的图标来源保持不变，下次导入或重新定位时会重新提取。");
		return 0;
	}

	private static int RunMakeSamples(CommandLineOptions options, StringBuilder report)
	{
		SampleSet sampleSet = SampleDataBuilder.Build((options.VerbArguments.Count > 0) ? options.VerbArguments[0] : null);
		report.Append(SampleDataBuilder.Describe(sampleSet));
		report.AppendLine();
		report.AppendLine("下一步：用 --import 导入上面「应当成功导入」的那些来源，");
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(4, 1, report, invariantCulture);
		handler.AppendLiteral("再删掉 ");
		handler.AppendFormatted(sampleSet.BrokenTarget);
		report.AppendLine(invariantCulture, ref handler);
		report.AppendLine("然后用 --check-library 观察失效标记，用 --relocate 重新定位。");
		return 0;
	}

	private static int RunSeed(AppServiceHost host, CommandLineOptions options, StringBuilder report)
	{
		int num = ReadIntOption(options.VerbArguments, 0, 500);
		AppLibraryStore library = host.Library;
		int num2 = 0;
		int num3 = 0;
		IFormatProvider invariantCulture;
		for (int i = 0; i < num; i++)
		{
			AppEntry appEntry = new AppEntry
			{
				Id = AppLibraryStore.NewId(),
				Kind = AppItemKind.File,
				Target = "C:\\Windows\\System32\\notepad.exe"
			};
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider = invariantCulture;
			DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(5, 1, invariantCulture);
			handler.AppendLiteral("合成条目 ");
			handler.AppendFormatted(i, "D4");
			appEntry.DisplayName = string.Create(provider, ref handler);
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider2 = invariantCulture;
			DefaultInterpolatedStringHandler handler2 = new DefaultInterpolatedStringHandler(7, 1, invariantCulture);
			handler2.AppendLiteral("--seed ");
			handler2.AppendFormatted(i);
			appEntry.Arguments = string.Create(provider2, ref handler2);
			appEntry.IconSource = "C:\\Windows\\System32\\notepad.exe";
			AppEntry entry = appEntry;
			if (library.Add(entry) == AddEntryResult.Added)
			{
				num2++;
			}
			else
			{
				num3++;
			}
		}
		library.Save();
		StringBuilder stringBuilder = report;
		StringBuilder stringBuilder2 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider3 = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler3 = new StringBuilder.AppendInterpolatedStringHandler(25, 3, stringBuilder, invariantCulture);
		handler3.AppendLiteral("要求造出 ");
		handler3.AppendFormatted(num);
		handler3.AppendLiteral(" 条，实际新增 ");
		handler3.AppendFormatted(num2);
		handler3.AppendLiteral(" 条，因去重跳过 ");
		handler3.AppendFormatted(num3);
		handler3.AppendLiteral(" 条。");
		stringBuilder2.AppendLine(provider3, ref handler3);
		stringBuilder = report;
		StringBuilder stringBuilder3 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider4 = invariantCulture;
		handler3 = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder, invariantCulture);
		handler3.AppendLiteral("应用库现在共 ");
		handler3.AppendFormatted(library.Items.Count);
		handler3.AppendLiteral(" 条。");
		stringBuilder3.AppendLine(provider4, ref handler3);
		return 0;
	}

	private static ImportRequest CreateRequest(string raw)
	{
		bool isUrl = Uri.TryCreate(raw, UriKind.Absolute, out Uri result) && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
		return new ImportRequest
		{
			Source = raw,
			IsUrl = isUrl
		};
	}

	private static void DescribeImport(ImportResult result, StringBuilder report)
	{
		report.AppendLine("导入结果");
		StringBuilder stringBuilder = report;
		StringBuilder stringBuilder2 = stringBuilder;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(7, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("  新增 ");
		handler.AppendFormatted(result.Added.Count);
		handler.AppendLiteral(" 条");
		stringBuilder2.AppendLine(provider, ref handler);
		stringBuilder = report;
		StringBuilder stringBuilder3 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider2 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(7, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("  重复 ");
		handler.AppendFormatted(result.Duplicates.Count);
		handler.AppendLiteral(" 条");
		stringBuilder3.AppendLine(provider2, ref handler);
		stringBuilder = report;
		StringBuilder stringBuilder4 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider3 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(7, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("  失败 ");
		handler.AppendFormatted(result.Failures.Count);
		handler.AppendLiteral(" 条");
		stringBuilder4.AppendLine(provider3, ref handler);
		report.AppendLine();
		foreach (AppEntry item in result.Added)
		{
			string value = (item.IsTargetMissing ? "（目标暂时找不到，已标记为失效）" : string.Empty);
			stringBuilder = report;
			StringBuilder stringBuilder5 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider4 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(7, 3, stringBuilder, invariantCulture);
			handler.AppendLiteral("  + [");
			handler.AppendFormatted(DescribeKind(item.Kind));
			handler.AppendLiteral("] ");
			handler.AppendFormatted(item.DisplayName);
			handler.AppendFormatted(value);
			stringBuilder5.AppendLine(provider4, ref handler);
			stringBuilder = report;
			StringBuilder stringBuilder6 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider5 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("      目标 ");
			handler.AppendFormatted(item.Target);
			stringBuilder6.AppendLine(provider5, ref handler);
		}
		foreach (AppEntry duplicate in result.Duplicates)
		{
			stringBuilder = report;
			StringBuilder stringBuilder7 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider6 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(14, 3, stringBuilder, invariantCulture);
			handler.AppendLiteral("  = 已存在：[");
			handler.AppendFormatted(DescribeKind(duplicate.Kind));
			handler.AppendLiteral("] ");
			handler.AppendFormatted(duplicate.DisplayName);
			handler.AppendLiteral(" → ");
			handler.AppendFormatted(duplicate.Target);
			stringBuilder7.AppendLine(provider6, ref handler);
		}
		foreach (ImportFailure failure in result.Failures)
		{
			stringBuilder = report;
			StringBuilder stringBuilder8 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider7 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(5, 2, stringBuilder, invariantCulture);
			handler.AppendLiteral("  ✗ ");
			handler.AppendFormatted(failure.Reason);
			handler.AppendLiteral("：");
			handler.AppendFormatted(failure.Message);
			stringBuilder8.AppendLine(provider7, ref handler);
		}
	}

	private static void DescribeIconWork(AppServiceHost host, StringBuilder report)
	{
		report.AppendLine();
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(22, 3, report, invariantCulture);
		handler.AppendLiteral("图标文件：新写 ");
		handler.AppendFormatted(host.Import.IconsWritten);
		handler.AppendLiteral("，复用 ");
		handler.AppendFormatted(host.Import.IconsReused);
		handler.AppendLiteral("，缓存目录现有 ");
		handler.AppendFormatted(host.Icons.FileCount);
		handler.AppendLiteral(" 个");
		report.AppendLine(invariantCulture, ref handler);
	}

	private static void DescribeLibrarySize(AppServiceHost host, StringBuilder report)
	{
		int value = Enumerable.Count(host.Library.Items, (AppEntry entry) => entry?.IsUsable ?? false);
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, report, invariantCulture);
		handler.AppendLiteral("应用库现有 ");
		handler.AppendFormatted(value);
		handler.AppendLiteral(" 条记录。");
		report.AppendLine(invariantCulture, ref handler);
	}

	private static string DescribeKind(AppItemKind kind)
	{
		return kind switch
		{
			AppItemKind.Program => "程序", 
			AppItemKind.Shortcut => "快捷方式", 
			AppItemKind.Folder => "文件夹", 
			AppItemKind.File => "文件", 
			AppItemKind.Url => "网页", 
			_ => kind.ToString(), 
		};
	}

	private static string DescribeActivation(ActivationOutcome outcome)
	{
		return outcome switch
		{
			ActivationOutcome.Activated => "已把现有窗口提到前台", 
			ActivationOutcome.Minimized => "已最小化当前窗口", 
			ActivationOutcome.Launched => "没有已运行的窗口，已启动", 
			ActivationOutcome.MultipleWindowsFound => "找到多个窗口，需要用户选择", 
			ActivationOutcome.Failed => "失败", 
			_ => outcome.ToString(), 
		};
	}

	private static string DescribeVerb(AppServiceVerb verb)
	{
		return verb switch
		{
			AppServiceVerb.Import => "导入条目", 
			AppServiceVerb.ScanStartMenu => "扫描「开始」菜单", 
			AppServiceVerb.ImportStartMenu => "导入「开始」菜单", 
			AppServiceVerb.ListLibrary => "列出应用库", 
			AppServiceVerb.CheckLibrary => "复查失效目标", 
			AppServiceVerb.Relocate => "重新定位条目", 
			AppServiceVerb.Launch => "打开条目", 
			AppServiceVerb.Activate => "激活或启动条目", 
			AppServiceVerb.VerifyIcons => "校验缓存图标", 
			AppServiceVerb.IconContactSheet => "生成图标联系表", 
			AppServiceVerb.Phase3Acceptance => "阶段 3 验收", 
			AppServiceVerb.ClearIconCache => "清空图标缓存", 
			AppServiceVerb.MakeSamples => "造验收样本", 
			AppServiceVerb.SeedLibrary => "造合成条目", 
			_ => verb.ToString(), 
		};
	}

	private static int ReadIntOption(IReadOnlyList<string> arguments, int index, int fallback)
	{
		if (index >= arguments.Count)
		{
			return fallback;
		}
		if (!int.TryParse(arguments[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) || result <= 0)
		{
			return fallback;
		}
		return result;
	}
}
