using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
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

internal static class Phase3Acceptance
{
	private sealed record CrossDirectoryRestoreResult(bool FileRestored, bool ReferenceRepointed, bool StillReadable, string Detail);

	public static int Run(AppPaths realPaths, IReadOnlyList<string> arguments, AcceptanceWorkspace workspace)
	{
		ArgumentNullException.ThrowIfNull(realPaths, "realPaths");
		ArgumentNullException.ThrowIfNull(arguments, "arguments");
		StringBuilder stringBuilder = new StringBuilder();
		List<(string, CheckStatus, string)> list = new List<(string, CheckStatus, string)>();
		AppPaths appPaths = new AppPaths(workspace.DataRoot);
		appPaths.EnsureCreated();
		stringBuilder.AppendLine("GlassDesk 阶段 3 验收");
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
		handler = new StringBuilder.AppendInterpolatedStringHandler(24, 1, stringBuilder2, invariantCulture);
		handler.AppendLiteral("验收数据目录：");
		handler.AppendFormatted(appPaths.RootDirectory);
		handler.AppendLiteral("（隔离的临时目录，跑完保留供核对）");
		stringBuilder4.AppendLine(provider2, ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider3 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(18, 1, stringBuilder2, invariantCulture);
		handler.AppendLiteral("用户真正的数据目录：");
		handler.AppendFormatted(realPaths.RootDirectory);
		handler.AppendLiteral("——全程未被改动");
		stringBuilder5.AppendLine(provider3, ref handler);
		stringBuilder.AppendLine();
		SampleSet sampleSet = SampleDataBuilder.Build();
		stringBuilder.AppendLine(SampleDataBuilder.Describe(sampleSet));
		GlassDeskDataStore glassDeskDataStore = new GlassDeskDataStore(appPaths);
		glassDeskDataStore.LoadAll();
		using AppServiceHost appServiceHost = new AppServiceHost(glassDeskDataStore);
		Section(stringBuilder, "一、导入应当被接受的来源");
		ImportResult importResult = ImportAll(appServiceHost, sampleSet, stringBuilder);
		Check(list, "全部被接受的来源都新增成功", importResult.Added.Count == sampleSet.ShouldImport.Count, $"期望 {sampleSet.ShouldImport.Count} 条，实际 {importResult.Added.Count} 条");
		Check(list, "被接受的来源没有一条失败", importResult.Failures.Count == 0, $"失败 {importResult.Failures.Count} 条");
		Section(stringBuilder, "二、应当被拒绝的来源逐条报告失败");
		ImportResult importResult2 = ImportRejected(appServiceHost, sampleSet, stringBuilder);
		Check(list, "被拒绝的来源没有一条进入应用库", importResult2.Added.Count == 0, $"新增 {importResult2.Added.Count} 条");
		Check(list, "每一个坏来源都被逐条报告", importResult2.Failures.Count == sampleSet.ShouldFail.Count, $"期望 {sampleSet.ShouldFail.Count} 条失败，实际 {importResult2.Failures.Count} 条（{DescribeFailures(importResult2)}）");
		Check(list, "不存在的路径报为 NotFound", importResult2.Failures.Any((ImportFailure failure) => failure.Reason == ImportFailureReason.NotFound), DescribeFailures(importResult2));
		Check(list, "读不出目标的快捷方式报为 ShortcutUnreadable", importResult2.Failures.Any((ImportFailure failure) => failure.Reason == ImportFailureReason.ShortcutUnreadable), DescribeFailures(importResult2));
		Section(stringBuilder, "三、四类条目与网页都在库里");
		Dictionary<string, AppEntry> dictionary = MapByKey(appServiceHost, sampleSet);
		Check(list, "程序条目存在", dictionary.ContainsKey("program"), $"对上 {dictionary.Count} 个样本：{DescribeKeys(dictionary)}");
		Check(list, "文件夹条目存在", dictionary.ContainsKey("folder"), $"对上 {dictionary.Count} 个样本：{DescribeKeys(dictionary)}");
		Check(list, "文件条目存在", dictionary.ContainsKey("file"), $"对上 {dictionary.Count} 个样本：{DescribeKeys(dictionary)}");
		Check(list, "快捷方式（带参数）条目存在", dictionary.ContainsKey("shortcut-arguments"), $"对上 {dictionary.Count} 个样本：{DescribeKeys(dictionary)}");
		Check(list, "网页条目存在（.url 文件）", dictionary.ContainsKey("url-file"), $"对上 {dictionary.Count} 个样本：{DescribeKeys(dictionary)}");
		Check(list, "网页条目存在（指向网址的快捷方式）", dictionary.ContainsKey("shortcut-url"), $"对上 {dictionary.Count} 个样本：{DescribeKeys(dictionary)}");
		Check(list, "只差端口的两个网址被当成两条记录", dictionary.TryGetValue("url-port-8001", out var value) && dictionary.TryGetValue("url-port-8002", out var value2) && !string.Equals(value.Target, value2.Target, StringComparison.Ordinal), $"8001 → {dictionary.GetValueOrDefault("url-port-8001")?.Target ?? "（缺失）"}；8002 → {dictionary.GetValueOrDefault("url-port-8002")?.Target ?? "（缺失）"}");
		Check(list, "后缀是 .url 但内容不是网页的文件退回按普通文件处理", dictionary.TryGetValue("bogus-url-file", out var value3) && value3.Kind == AppItemKind.File, dictionary.TryGetValue("bogus-url-file", out var value4) ? $"实际 [{value4.Kind}] {value4.Target}" : "条目缺失");
		Check(list, "带参数的快捷方式保留了参数", dictionary.TryGetValue("shortcut-arguments", out var value5) && value5.Arguments == "--flag \"带 空格 的值\" --second", dictionary.TryGetValue("shortcut-arguments", out var value6) ? ("实际「" + value6.Arguments + "」") : "条目缺失");
		Check(list, "带参数的快捷方式保留了工作目录", dictionary.TryGetValue("shortcut-arguments", out var value7) && !string.IsNullOrEmpty(value7.WorkingDirectory), dictionary.TryGetValue("shortcut-arguments", out var value8) ? ("实际「" + value8.WorkingDirectory + "」") : "条目缺失");
		Check(list, "快捷方式的图标索引没有被拉平成目标本身", dictionary.TryGetValue("shortcut-arguments", out var value9) && value9.IconSource != null && value9.IconSource.EndsWith(",3", StringComparison.Ordinal), dictionary.TryGetValue("shortcut-arguments", out var value10) ? ("实际「" + value10.IconSource + "」") : "条目缺失");
		Check(list, "指向网址的快捷方式被识别为网页，而不是降级保留 .lnk", dictionary.TryGetValue("shortcut-url", out var value11) && value11.Kind == AppItemKind.Url && value11.Target.StartsWith("https://", StringComparison.OrdinalIgnoreCase), dictionary.TryGetValue("shortcut-url", out var value12) ? $"实际 [{value12.Kind}] {value12.Target}" : "条目缺失");
		Check(list, "中文与空格路径被原样保留", dictionary.TryGetValue("file", out var value13) && value13.Target.Contains("季度 报告 2026", StringComparison.Ordinal), dictionary.TryGetValue("file", out var value14) ? ("实际「" + value14.Target + "」") : "条目缺失");
		Section(stringBuilder, "四、图标缓存");
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider4 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(20, 3, stringBuilder2, invariantCulture);
		handler.AppendLiteral("新写 ");
		handler.AppendFormatted(appServiceHost.Import.IconsWritten);
		handler.AppendLiteral(" 个，复用 ");
		handler.AppendFormatted(appServiceHost.Import.IconsReused);
		handler.AppendLiteral(" 个，缓存目录共 ");
		handler.AppendFormatted(appServiceHost.Icons.FileCount);
		handler.AppendLiteral(" 个");
		stringBuilder6.AppendLine(provider4, ref handler);
		stringBuilder.AppendLine();
		(int, int) tuple = VerifyIcons(appPaths, stringBuilder);
		Check(list, "每个图标的来源都不相同，因此没有重复文件", appServiceHost.Import.IconsReused == 0, $"复用 {appServiceHost.Import.IconsReused} 个");
		Check(list, "全部缓存图标都能被系统解码器读出且有可见内容", tuple.Item2 == 0, $"失败 {tuple.Item2} 个，共 {tuple.Item1} 个");
		Section(stringBuilder, "五、重启之后条目仍在（换一个进程重新加载文件）");
		AppLibraryStore appLibraryStore = new AppLibraryStore(appPaths.AppLibraryFile);
		appLibraryStore.Load();
		int num = Enumerable.Count(appLibraryStore.Items, (AppEntry entry) => entry?.IsUsable ?? false);
		Check(list, "重新加载后条目数不变", num == Enumerable.Count(appServiceHost.Library.Items, (AppEntry entry) => entry?.IsUsable ?? false), $"重新加载 {num} 条，原 {appServiceHost.Library.Items.Count} 条");
		Check(list, "重新加载后参数仍在", appLibraryStore.Items.Any((AppEntry entry) => entry?.Arguments == "--flag \"带 空格 的值\" --second"), $"重新加载 {num} 条");
		Section(stringBuilder, "六、目标失效：条目保留、标记失效、可重新定位");
		bool flag = SampleDataBuilder.DeleteTarget(sampleSet.BrokenTarget);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder7 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider5 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(8, 2, stringBuilder2, invariantCulture);
		handler.AppendLiteral("删除目标：");
		handler.AppendFormatted(flag ? "成功" : "失败");
		handler.AppendLiteral(" → ");
		handler.AppendFormatted(sampleSet.BrokenTarget);
		stringBuilder7.AppendLine(provider5, ref handler);
		stringBuilder.AppendLine();
		AppEntry valueOrDefault = dictionary.GetValueOrDefault("broken-later");
		Check(list, "失效样本的目标已被删除", flag, sampleSet.BrokenTarget);
		MissingTargetReport missingTargetReport = appServiceHost.Maintenance.RefreshMissingTargets();
		Check(list, "复查发现恰好一条失效", missingTargetReport.Missing == 1, $"实际 {missingTargetReport.Missing} 条");
		Check(list, "失效状态有变化所以写回了文件", missingTargetReport.Changed, $"Changed={missingTargetReport.Changed}，失效 {missingTargetReport.Missing} 条");
		AppLibraryStore appLibraryStore2 = new AppLibraryStore(appPaths.AppLibraryFile);
		appLibraryStore2.Load();
		bool flag2 = valueOrDefault != null && appLibraryStore2.Find(valueOrDefault.Id) != null;
		Check(list, "失效条目仍然在文件里，没有被丢掉", flag2, flag2 ? "记录仍在文件中" : "记录不见了");
		Check(list, "重新加载后该条目标记为失效", valueOrDefault != null && (appLibraryStore2.Find(valueOrDefault.Id)?.IsTargetMissing ?? false), (valueOrDefault == null) ? "样本条目缺失" : $"持久化后的标记={appLibraryStore2.Find(valueOrDefault.Id)?.IsTargetMissing}");
		Section(stringBuilder, "七、失效条目不影响启动");
		if (valueOrDefault != null)
		{
			LaunchResult launchResult = appServiceHost.Launch.LaunchItem(valueOrDefault.Id);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder8 = stringBuilder2;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider6 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(20, 3, stringBuilder2, invariantCulture);
			handler.AppendLiteral("打开失效条目 → 成功=");
			handler.AppendFormatted(launchResult.Succeeded);
			handler.AppendLiteral("，分类=");
			handler.AppendFormatted(launchResult.Failure);
			handler.AppendLiteral("，说明=");
			handler.AppendFormatted(launchResult.Message);
			stringBuilder8.AppendLine(provider6, ref handler);
			stringBuilder.AppendLine();
			Check(list, "打开失效条目失败但给出了分类", !launchResult.Succeeded && launchResult.Failure == LaunchFailureReason.TargetMissing, $"成功={launchResult.Succeeded}，分类={launchResult.Failure}");
			Check(list, "给出的说明是中文且指向重新定位", launchResult.Message?.Contains("重新定位", StringComparison.Ordinal) ?? false, launchResult.Message ?? "（没有说明）");
		}
		AppEntry valueOrDefault2 = dictionary.GetValueOrDefault("program");
		if (valueOrDefault2 != null)
		{
			bool flag3 = LaunchService.IsTargetGone(valueOrDefault2);
			bool flag4 = valueOrDefault == null || appServiceHost.Launch.LaunchItem(valueOrDefault.Id).Failure == LaunchFailureReason.TargetMissing;
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder9 = stringBuilder2;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider7 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(18, 3, stringBuilder2, invariantCulture);
			handler.AppendLiteral("正常条目（");
			handler.AppendFormatted(valueOrDefault2.DisplayName);
			handler.AppendLiteral("）目标存在=");
			handler.AppendFormatted(!flag3);
			handler.AppendLiteral("，未标记失效=");
			handler.AppendFormatted(!valueOrDefault2.IsTargetMissing);
			stringBuilder9.AppendLine(provider7, ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder10 = stringBuilder2;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider8 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(26, 1, stringBuilder2, invariantCulture);
			handler.AppendLiteral("失效条目仍被如实报告为 TargetMissing=");
			handler.AppendFormatted(flag4);
			stringBuilder10.AppendLine(provider8, ref handler);
			stringBuilder.AppendLine();
			Check(list, "库里有失效条目时，正常条目不失效", (!flag3 && !valueOrDefault2.IsTargetMissing) & flag4, flag3 ? "正常条目的目标被判为不存在" : "正常条目判定正常，失效条目仍被如实报告");
		}
		Section(stringBuilder, "八、重新定位");
		string text = Path.Combine(sampleSet.Directory, "重新定位后的目标.cmd");
		File.WriteAllText(text, "@echo off" + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		if (valueOrDefault != null)
		{
			string target = valueOrDefault.Target;
			RelocationOutcome relocationOutcome = appServiceHost.Maintenance.Relocate(valueOrDefault.Id, text);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder11 = stringBuilder2;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider9 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(8, 2, stringBuilder2, invariantCulture);
			handler.AppendLiteral("重新定位 → ");
			handler.AppendFormatted(relocationOutcome.Result);
			handler.AppendLiteral("：");
			handler.AppendFormatted(relocationOutcome.Message);
			stringBuilder11.AppendLine(provider9, ref handler);
			stringBuilder.AppendLine();
			Check(list, "重新定位成功", relocationOutcome.Result == RelocateResult.Relocated, relocationOutcome.Message);
			AppLibraryStore appLibraryStore3 = new AppLibraryStore(appPaths.AppLibraryFile);
			appLibraryStore3.Load();
			AppEntry appEntry = appLibraryStore3.Find(valueOrDefault.Id);
			Check(list, "新目标被持久化", appEntry?.Target == text, appEntry?.Target ?? "记录不见了");
			Check(list, "失效标记被清除", appEntry != null && !appEntry.IsTargetMissing, $"{appEntry?.IsTargetMissing}");
			Check(list, "图标来源跟着目标走", appEntry?.IconSource == text, appEntry?.IconSource ?? "（空）");
			RelocationOutcome relocationOutcome2 = appServiceHost.Maintenance.Relocate(valueOrDefault2?.Id ?? string.Empty, text);
			Check(list, "重新定位到已占用的位置会被拒绝", relocationOutcome2.Result == RelocateResult.ConflictsWithExisting, $"实际 {relocationOutcome2.Result}");
			AppEntry valueOrDefault3 = dictionary.GetValueOrDefault("url-file");
			if (valueOrDefault3 != null)
			{
				RelocationOutcome relocationOutcome3 = appServiceHost.Maintenance.Relocate(valueOrDefault3.Id, text);
				Check(list, "网址不接受重新定位", relocationOutcome3.Result == RelocateResult.NotRelocatable, $"实际 {relocationOutcome3.Result}");
			}
			File.WriteAllText(target, "@echo off" + Environment.NewLine);
			ImportResult importResult3 = appServiceHost.Import.ImportItems(new _003C_003Ez__ReadOnlySingleElementList<ImportRequest>(new ImportRequest
			{
				Source = target
			}));
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder12 = stringBuilder2;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider10 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(21, 4, stringBuilder2, invariantCulture);
			handler.AppendLiteral("再导入旧位置 ");
			handler.AppendFormatted(target);
			handler.AppendLiteral(" → 新增 ");
			handler.AppendFormatted(importResult3.Added.Count);
			handler.AppendLiteral("，重复 ");
			handler.AppendFormatted(importResult3.Duplicates.Count);
			handler.AppendLiteral("，失败 ");
			handler.AppendFormatted(importResult3.Failures.Count);
			stringBuilder12.AppendLine(provider10, ref handler);
			stringBuilder.AppendLine();
			Check(list, "重新定位之后旧位置可以再次被导入", importResult3.Added.Count == 1 && importResult3.Duplicates.Count == 0 && importResult3.Failures.Count == 0, (importResult3.Failures.Count > 0) ? ("失败：" + importResult3.Failures[0].Message) : $"新增 {importResult3.Added.Count}，重复 {importResult3.Duplicates.Count}");
		}
		Section(stringBuilder, "九、「开始」菜单扫描");
		StartMenuScanResult startMenuScanResult = AppServiceHost.ScanStartMenu();
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder13 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider11 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(6, 1, stringBuilder2, invariantCulture);
		handler.AppendLiteral("扫描根目录：");
		handler.AppendFormatted(string.Join("；", startMenuScanResult.Roots));
		stringBuilder13.AppendLine(provider11, ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder14 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider12 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(26, 3, stringBuilder2, invariantCulture);
		handler.AppendLiteral("看到 .lnk ");
		handler.AppendFormatted(startMenuScanResult.ScannedFileCount);
		handler.AppendLiteral(" 个，去重后 ");
		handler.AppendFormatted(startMenuScanResult.Shortcuts.Count);
		handler.AppendLiteral(" 个（同名让位 ");
		handler.AppendFormatted(startMenuScanResult.DuplicateNameCount);
		handler.AppendLiteral(" 个）");
		stringBuilder14.AppendLine(provider12, ref handler);
		foreach (IGrouping<string, StartMenuShortcut> item4 in (from @group in startMenuScanResult.Shortcuts.GroupBy((StartMenuShortcut shortcut) => (shortcut.Group.Length != 0) ? shortcut.Group : "（根）", StringComparer.CurrentCulture)
			orderby @group.Count() descending
			select @group).Take(15).ToList())
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder15 = stringBuilder2;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider13 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(6, 2, stringBuilder2, invariantCulture);
			handler.AppendLiteral("  ");
			handler.AppendFormatted(item4.Count(), 3);
			handler.AppendLiteral("  · ");
			handler.AppendFormatted(item4.Key);
			stringBuilder15.AppendLine(provider13, ref handler);
		}
		stringBuilder.AppendLine();
		Check(list, "两个「开始」菜单根目录都被扫描", startMenuScanResult.Roots.Count == 2, $"实际 {startMenuScanResult.Roots.Count} 个");
		Check(list, "扫描到了真实的快捷方式", startMenuScanResult.ScannedFileCount > 0, $"扫到 {startMenuScanResult.ScannedFileCount} 个 .lnk");
		Check(list, "去重确实生效或确实无需去重", startMenuScanResult.ScannedFileCount >= startMenuScanResult.Shortcuts.Count, $"原始 {startMenuScanResult.ScannedFileCount}，去重后 {startMenuScanResult.Shortcuts.Count}");
		Section(stringBuilder, "十、开始菜单条目真的能导入（取前 40 条）");
		List<StartMenuShortcut> list2 = startMenuScanResult.Shortcuts.Take(40).ToList();
		ImportResult importResult4 = appServiceHost.ImportStartMenuShortcuts(list2);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder16 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider14 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(19, 4, stringBuilder2, invariantCulture);
		handler.AppendLiteral("尝试导入 ");
		handler.AppendFormatted(list2.Count);
		handler.AppendLiteral(" 条：新增 ");
		handler.AppendFormatted(importResult4.Added.Count);
		handler.AppendLiteral("，重复 ");
		handler.AppendFormatted(importResult4.Duplicates.Count);
		handler.AppendLiteral("，失败 ");
		handler.AppendFormatted(importResult4.Failures.Count);
		stringBuilder16.AppendLine(provider14, ref handler);
		if (importResult4.Failures.Count > 0)
		{
			stringBuilder.AppendLine("失败明细：");
			foreach (ImportFailure failure in importResult4.Failures)
			{
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder17 = stringBuilder2;
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider15 = invariantCulture;
				handler = new StringBuilder.AppendInterpolatedStringHandler(5, 2, stringBuilder2, invariantCulture);
				handler.AppendLiteral("  ✗ ");
				handler.AppendFormatted(failure.Reason);
				handler.AppendLiteral("：");
				handler.AppendFormatted(failure.Message);
				stringBuilder17.AppendLine(provider15, ref handler);
			}
		}
		stringBuilder.AppendLine();
		List<string> values = (from entry in importResult4.Added
			group entry by entry.Kind into @group
			orderby @group.Count() descending
			select $"{@group.Key}={@group.Count()}").ToList();
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder18 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider16 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2, invariantCulture);
		handler.AppendLiteral("新增条目的类型分布：");
		handler.AppendFormatted(string.Join("，", values));
		stringBuilder18.AppendLine(provider16, ref handler);
		stringBuilder.AppendLine();
		Check(list, "开始菜单里的条目大部分能导入", importResult4.Added.Count >= list2.Count / 2, $"尝试 {list2.Count} 条，新增 {importResult4.Added.Count} 条，失败 {importResult4.Failures.Count} 条");
		Section(stringBuilder, "十一、500 条规模");
		(int, int) tuple2 = Seed(appServiceHost, 500);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder19 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider17 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(18, 2, stringBuilder2, invariantCulture);
		handler.AppendLiteral("合成条目：新增 ");
		handler.AppendFormatted(tuple2.Item1);
		handler.AppendLiteral(" 条，去重跳过 ");
		handler.AppendFormatted(tuple2.Item2);
		handler.AppendLiteral(" 条");
		stringBuilder19.AppendLine(provider17, ref handler);
		AppLibraryStore appLibraryStore4 = new AppLibraryStore(appPaths.AppLibraryFile);
		appLibraryStore4.Load();
		int num2 = Enumerable.Count(appLibraryStore4.Items, (AppEntry entry) => entry?.IsUsable ?? false);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder20 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider18 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(7, 1, stringBuilder2, invariantCulture);
		handler.AppendLiteral("重新加载：");
		handler.AppendFormatted(num2);
		handler.AppendLiteral(" 条");
		stringBuilder20.AppendLine(provider18, ref handler);
		Stopwatch stopwatch = Stopwatch.StartNew();
		MissingTargetReport missingTargetReport2 = appServiceHost.Maintenance.RefreshMissingTargets(persist: false);
		stopwatch.Stop();
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder21 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider19 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(19, 2, stringBuilder2, invariantCulture);
		handler.AppendLiteral("全库失效复查耗时：");
		handler.AppendFormatted(stopwatch.Elapsed.TotalMilliseconds, "F1");
		handler.AppendLiteral(" 毫秒（检查 ");
		handler.AppendFormatted(missingTargetReport2.Checked);
		handler.AppendLiteral(" 条）");
		stringBuilder21.AppendLine(provider19, ref handler);
		stringBuilder.AppendLine();
		Check(list, "应用库达到 500 条以上", num2 >= 500, $"实际 {num2} 条");
		Check(list, "重新加载 500 条没有异常", num2 == Enumerable.Count(appServiceHost.Library.Items, (AppEntry entry) => entry?.IsUsable ?? false), "重新加载后的条数与内存中的不一致");
		Check(list, "全库复查在 2 秒内完成", stopwatch.Elapsed.TotalSeconds < 2.0, $"耗时 {stopwatch.Elapsed.TotalMilliseconds:F0} 毫秒");
		Section(stringBuilder, "十二、窗口激活：已运行就不起第二个实例");
		(bool, bool, bool, bool, string) tuple3 = RunActivationCheck(appServiceHost, stringBuilder);
		if (tuple3.Item4)
		{
			Skipped(list, "窗口激活检查", tuple3.Item5);
		}
		else
		{
			Check(list, "第一次请求会启动应用", tuple3.Item1, tuple3.Item5);
			Check(list, "第二次请求找到了已运行的窗口", tuple3.Item2, tuple3.Item5);
			Check(list, "第二次请求把窗口提到前台而不是再起一个", tuple3.Item3, tuple3.Item5);
		}
		Section(stringBuilder, "十三、导出图标联系表作为人工核对材料");
		string outputPath = arguments[0];
		int num3 = RenderContactSheet(appServiceHost, workspace.GetOutput("image"), outputPath, stringBuilder);
		Check(list, "联系表里有图标", num3 > 0, $"画出 {num3} 个");
		Section(stringBuilder, "十四、跨目录恢复：换一个数据目录也能用");
		CrossDirectoryRestoreResult crossDirectoryRestoreResult = RunCrossDirectoryRestore(appServiceHost, appPaths, workspace, stringBuilder);
		Check(list, "恢复到另一个数据目录后图标文件随之到位", crossDirectoryRestoreResult.FileRestored, crossDirectoryRestoreResult.Detail);
		Check(list, "恢复后的 apps.json 指向新目录而不是旧目录", crossDirectoryRestoreResult.ReferenceRepointed, crossDirectoryRestoreResult.Detail);
		Check(list, "删掉原目录之后图标仍能读取", crossDirectoryRestoreResult.StillReadable, crossDirectoryRestoreResult.Detail);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("检查汇总");
		stringBuilder.AppendLine(new string('-', 46));
		foreach (var item5 in list)
		{
			string item = item5.Item1;
			CheckStatus item2 = item5.Item2;
			string item3 = item5.Item3;
			string value15 = item2 switch
			{
				CheckStatus.Passed => "✓", 
				CheckStatus.Skipped => "○", 
				_ => "✗", 
			};
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder22 = stringBuilder2;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider20 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(1, 2, stringBuilder2, invariantCulture);
			handler.AppendFormatted(value15);
			handler.AppendLiteral(" ");
			handler.AppendFormatted(item);
			stringBuilder22.AppendLine(provider20, ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder23 = stringBuilder2;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider21 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(4, 1, stringBuilder2, invariantCulture);
			handler.AppendLiteral("    ");
			handler.AppendFormatted(item3);
			stringBuilder23.AppendLine(provider21, ref handler);
		}
		int num4 = Enumerable.Count(list, ((string Label, CheckStatus Status, string Detail) check) => check.Status == CheckStatus.Failed);
		int value16 = Enumerable.Count(list, ((string Label, CheckStatus Status, string Detail) check) => check.Status == CheckStatus.Skipped);
		int value17 = Enumerable.Count(list, ((string Label, CheckStatus Status, string Detail) check) => check.Status == CheckStatus.Passed);
		stringBuilder.AppendLine();
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder24 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider22 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(41, 4, stringBuilder2, invariantCulture);
		handler.AppendLiteral("共 ");
		handler.AppendFormatted(list.Count);
		handler.AppendLiteral(" 项检查：通过 ");
		handler.AppendFormatted(value17);
		handler.AppendLiteral(" 项，失败 ");
		handler.AppendFormatted(num4);
		handler.AppendLiteral(" 项，跳过 ");
		handler.AppendFormatted(value16);
		handler.AppendLiteral(" 项（○ 是条件不具备、未验证的项）。");
		stringBuilder24.AppendLine(provider22, ref handler);
		AcceptanceEntryPoint.WriteText(workspace.GetOutput("report"), stringBuilder.ToString());
		ReportWriter.Write(stringBuilder.ToString(), null);
		return (num4 != 0) ? 1 : 0;
	}

	private static ImportResult ImportAll(AppServiceHost host, SampleSet set, StringBuilder report)
	{
		List<ImportRequest> list = set.ShouldImport.Select((SampleSource source) => new ImportRequest
		{
			Source = source.Path,
			IsUrl = (Uri.TryCreate(source.Path, UriKind.Absolute, out Uri result) && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps))
		}).ToList();
		ImportResult importResult = host.Import.ImportItems(list);
		StringBuilder stringBuilder = report;
		StringBuilder stringBuilder2 = stringBuilder;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(19, 4, stringBuilder, invariantCulture);
		handler.AppendLiteral("导入 ");
		handler.AppendFormatted(list.Count);
		handler.AppendLiteral(" 个来源：新增 ");
		handler.AppendFormatted(importResult.Added.Count);
		handler.AppendLiteral("，重复 ");
		handler.AppendFormatted(importResult.Duplicates.Count);
		handler.AppendLiteral("，失败 ");
		handler.AppendFormatted(importResult.Failures.Count);
		stringBuilder2.AppendLine(provider, ref handler);
		report.AppendLine();
		foreach (AppEntry item in importResult.Added)
		{
			string value = (item.IsTargetMissing ? "（已标记失效）" : string.Empty);
			stringBuilder = report;
			StringBuilder stringBuilder3 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider2 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(7, 3, stringBuilder, invariantCulture);
			handler.AppendLiteral("  + [");
			handler.AppendFormatted(item.Kind);
			handler.AppendLiteral("] ");
			handler.AppendFormatted(item.DisplayName);
			handler.AppendFormatted(value);
			stringBuilder3.AppendLine(provider2, ref handler);
			stringBuilder = report;
			StringBuilder stringBuilder4 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider3 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("      目标 ");
			handler.AppendFormatted(item.Target);
			stringBuilder4.AppendLine(provider3, ref handler);
			if (!string.IsNullOrEmpty(item.Arguments))
			{
				stringBuilder = report;
				StringBuilder stringBuilder5 = stringBuilder;
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider4 = invariantCulture;
				handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder, invariantCulture);
				handler.AppendLiteral("      参数 ");
				handler.AppendFormatted(item.Arguments);
				stringBuilder5.AppendLine(provider4, ref handler);
			}
		}
		foreach (ImportFailure failure in importResult.Failures)
		{
			stringBuilder = report;
			StringBuilder stringBuilder6 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider5 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(5, 2, stringBuilder, invariantCulture);
			handler.AppendLiteral("  ✗ ");
			handler.AppendFormatted(failure.Reason);
			handler.AppendLiteral("：");
			handler.AppendFormatted(failure.Message);
			stringBuilder6.AppendLine(provider5, ref handler);
		}
		report.AppendLine();
		return importResult;
	}

	private static ImportResult ImportRejected(AppServiceHost host, SampleSet set, StringBuilder report)
	{
		List<ImportRequest> list = set.ShouldFail.Select((SampleSource source) => new ImportRequest
		{
			Source = source.Path
		}).ToList();
		ImportResult importResult = host.Import.ImportItems(list);
		StringBuilder stringBuilder = report;
		StringBuilder stringBuilder2 = stringBuilder;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(16, 3, stringBuilder, invariantCulture);
		handler.AppendLiteral("导入 ");
		handler.AppendFormatted(list.Count);
		handler.AppendLiteral(" 个坏来源：新增 ");
		handler.AppendFormatted(importResult.Added.Count);
		handler.AppendLiteral("，失败 ");
		handler.AppendFormatted(importResult.Failures.Count);
		stringBuilder2.AppendLine(provider, ref handler);
		report.AppendLine();
		foreach (ImportFailure failure in importResult.Failures)
		{
			stringBuilder = report;
			StringBuilder stringBuilder3 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider2 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(5, 2, stringBuilder, invariantCulture);
			handler.AppendLiteral("  ✗ ");
			handler.AppendFormatted(failure.Reason);
			handler.AppendLiteral("：");
			handler.AppendFormatted(failure.Message);
			stringBuilder3.AppendLine(provider2, ref handler);
		}
		report.AppendLine();
		return importResult;
	}

	private static Dictionary<string, AppEntry> MapByKey(AppServiceHost host, SampleSet set)
	{
		Dictionary<string, AppEntry> dictionary = new Dictionary<string, AppEntry>(StringComparer.Ordinal);
		List<AppEntry> source = host.Library.Items.Where((AppEntry entry) => entry?.IsUsable ?? false).ToList();
		foreach (SampleSource source2 in set.ShouldImport)
		{
			Func<AppEntry, bool> predicate;
			if (source2.Path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || source2.Path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
			{
				predicate = (AppEntry entry) => string.Equals(entry.Target, source2.Path, StringComparison.OrdinalIgnoreCase);
			}
			else
			{
				string fileName = Path.GetFileNameWithoutExtension(source2.Path);
				predicate = (AppEntry entry) => entry.DisplayName == fileName || entry.Target.Contains(fileName, StringComparison.Ordinal);
			}
			AppEntry appEntry = source.FirstOrDefault(predicate);
			if (appEntry != null)
			{
				dictionary[source2.Key] = appEntry;
			}
		}
		return dictionary;
	}

	private static (int Total, int Failures) VerifyIcons(AppPaths paths, StringBuilder report)
	{
		if (!Directory.Exists(paths.IconCacheDirectory))
		{
			return (Total: 0, Failures: 0);
		}
		string[] files = Directory.GetFiles(paths.IconCacheDirectory, "*.png");
		int num = 0;
		string[] array = files;
		foreach (string path in array)
		{
			try
			{
				using FileStream bitmapStream = File.OpenRead(path);
				BitmapFrame bitmapFrame = new PngBitmapDecoder(bitmapStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
				int num2 = CountVisiblePixels(bitmapFrame);
				if (num2 == 0)
				{
					num++;
					StringBuilder stringBuilder = report;
					StringBuilder stringBuilder2 = stringBuilder;
					IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
					IFormatProvider provider = invariantCulture;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 3, stringBuilder, invariantCulture);
					handler.AppendLiteral("  ✗ ");
					handler.AppendFormatted(Path.GetFileName(path));
					handler.AppendLiteral(" ");
					handler.AppendFormatted(bitmapFrame.PixelWidth);
					handler.AppendLiteral("×");
					handler.AppendFormatted(bitmapFrame.PixelHeight);
					handler.AppendLiteral(" 全是透明像素");
					stringBuilder2.AppendLine(provider, ref handler);
				}
				else
				{
					StringBuilder stringBuilder = report;
					StringBuilder stringBuilder3 = stringBuilder;
					IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
					IFormatProvider provider2 = invariantCulture;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(12, 4, stringBuilder, invariantCulture);
					handler.AppendLiteral("  ✓ ");
					handler.AppendFormatted(Path.GetFileName(path));
					handler.AppendLiteral(" ");
					handler.AppendFormatted(bitmapFrame.PixelWidth);
					handler.AppendLiteral("×");
					handler.AppendFormatted(bitmapFrame.PixelHeight);
					handler.AppendLiteral(" 可见像素 ");
					handler.AppendFormatted(num2);
					stringBuilder3.AppendLine(provider2, ref handler);
				}
			}
			catch (Exception ex) when ((ex is FileFormatException || ex is NotSupportedException || ex is IOException) ? true : false)
			{
				num++;
				StringBuilder stringBuilder = report;
				StringBuilder stringBuilder4 = stringBuilder;
				IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider3 = invariantCulture;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(10, 2, stringBuilder, invariantCulture);
				handler.AppendLiteral("  ✗ ");
				handler.AppendFormatted(Path.GetFileName(path));
				handler.AppendLiteral(" 解码失败：");
				handler.AppendFormatted(ex.Message);
				stringBuilder4.AppendLine(provider3, ref handler);
			}
		}
		report.AppendLine();
		return (Total: files.Length, Failures: num);
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

	private static (int Added, int Skipped) Seed(AppServiceHost host, int count)
	{
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < count; i++)
		{
			AppEntry appEntry = new AppEntry
			{
				Id = AppLibraryStore.NewId(),
				Kind = AppItemKind.File,
				Target = "C:\\Windows\\System32\\notepad.exe"
			};
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
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
			if (host.Library.Add(entry) == AddEntryResult.Added)
			{
				num++;
			}
			else
			{
				num2++;
			}
		}
		host.Library.Save();
		return (Added: num, Skipped: num2);
	}

	private static int RenderContactSheet(AppServiceHost host, Stream output, string outputPath, StringBuilder report)
	{
		List<AppEntry> list = (from @group in host.Library.Items.Where((AppEntry entry) => entry != null && entry.IsUsable && !string.IsNullOrWhiteSpace(entry.IconSource)).GroupBy((AppEntry entry) => entry.IconSource, StringComparer.OrdinalIgnoreCase)
			select @group.First()).Take(60).ToList();
		int num = Math.Max(1, (list.Count + 10 - 1) / 10);
		int num2 = 2560;
		byte[] array = new byte[num2 * num * 64];
		int num3 = 0;
		for (int num4 = 0; num4 < list.Count; num4++)
		{
			string path = host.Icons.PathFor(list[num4].IconSource);
			if (!File.Exists(path))
			{
				continue;
			}
			try
			{
				using FileStream bitmapStream = File.OpenRead(path);
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
		byte[] array3 = PngWriter.EncodeBgra(array, 640, num * 64);
		output.Write(array3);
		output.Flush();
		StringBuilder stringBuilder = report;
		StringBuilder stringBuilder2 = stringBuilder;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(26, 2, stringBuilder, invariantCulture);
		handler.AppendLiteral("带图标来源的条目 ");
		handler.AppendFormatted(list.Count);
		handler.AppendLiteral(" 条（按来源去重），实际画出 ");
		handler.AppendFormatted(num3);
		handler.AppendLiteral(" 个");
		stringBuilder2.AppendLine(provider, ref handler);
		stringBuilder = report;
		StringBuilder stringBuilder3 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider2 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(7, 3, stringBuilder, invariantCulture);
		handler.AppendLiteral("联系表：");
		handler.AppendFormatted(outputPath);
		handler.AppendLiteral("（");
		handler.AppendFormatted(640);
		handler.AppendLiteral("×");
		handler.AppendFormatted(num * 64);
		handler.AppendLiteral("）");
		stringBuilder3.AppendLine(provider2, ref handler);
		report.AppendLine();
		return num3;
	}

	private static (bool Launched, bool FoundWindow, bool Activated, bool Skipped, string Detail) RunActivationCheck(AppServiceHost host, StringBuilder report)
	{
		if (!File.Exists("C:\\Windows\\System32\\charmap.exe"))
		{
			report.AppendLine("本机没有 C:\\Windows\\System32\\charmap.exe，跳过窗口激活检查。");
			report.AppendLine();
			return (Launched: false, FoundWindow: false, Activated: false, Skipped: true, Detail: "本机缺少靶子程序，未执行");
		}
		ImportResult importResult = host.Import.ImportItems(new _003C_003Ez__ReadOnlySingleElementList<ImportRequest>(new ImportRequest
		{
			Source = "C:\\Windows\\System32\\charmap.exe"
		}));
		AppEntry appEntry;
		if (importResult.Added.Count > 0)
		{
			appEntry = importResult.Added[0];
		}
		else
		{
			appEntry = ((importResult.Duplicates.Count > 0) ? importResult.Duplicates[0] : null);
		}
		if (appEntry == null)
		{
			report.AppendLine("无法把靶子程序导入应用库，跳过窗口激活检查。");
			report.AppendLine();
			return (Launched: false, FoundWindow: false, Activated: false, Skipped: false, Detail: "靶子条目创建失败");
		}
		if (WindowLocator.FindWindowsForImage("C:\\Windows\\System32\\charmap.exe").Count > 0)
		{
			report.AppendLine("用户桌面上已有字符映射表窗口在运行；");
			report.AppendLine("为不碰用户窗口，本次激活检查跳过并标注为未验证。");
			report.AppendLine();
			return (Launched: false, FoundWindow: false, Activated: false, Skipped: true, Detail: "用户桌面已有靶子窗口在运行，为不碰用户窗口而跳过");
		}
		ActivationResult activationResult = host.Launch.ActivateOrLaunch(appEntry.Id);
		StringBuilder stringBuilder = report;
		StringBuilder stringBuilder2 = stringBuilder;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(31, 2, stringBuilder, invariantCulture);
		handler.AppendLiteral("第一次 ActivateOrLaunch → ");
		handler.AppendFormatted(activationResult.Outcome);
		handler.AppendLiteral("（进程 id ");
		handler.AppendFormatted(host.Launch.LastLaunchedProcessId);
		handler.AppendLiteral("）");
		stringBuilder2.AppendLine(provider, ref handler);
		uint lastLaunchedProcessId = host.Launch.LastLaunchedProcessId;
		WindowCandidate[] array = Array.Empty<WindowCandidate>();
		DateTime dateTime = DateTime.UtcNow.AddSeconds(10.0);
		string text = string.Empty;
		while (DateTime.UtcNow < dateTime)
		{
			IReadOnlyList<WindowCandidateInfo> readOnlyList = WindowLocator.FindWindowsForImage("C:\\Windows\\System32\\charmap.exe");
			if (readOnlyList.Count > 0)
			{
				array = readOnlyList.Select((WindowCandidateInfo window) => new WindowCandidate
				{
					Handle = window.Handle,
					Title = window.Title
				}).ToArray();
				text = readOnlyList[0].Title;
				break;
			}
			Thread.Sleep(250);
		}
		stringBuilder = report;
		StringBuilder stringBuilder3 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider2 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(13, 2, stringBuilder, invariantCulture);
		handler.AppendLiteral("按映像路径找到的窗口：");
		handler.AppendFormatted(array.Length);
		handler.AppendLiteral(" 个");
		handler.AppendFormatted((text.Length > 0) ? ("，标题「" + text + "」") : string.Empty);
		stringBuilder3.AppendLine(provider2, ref handler);
		ActivationResult activationResult2 = host.Launch.ActivateOrLaunch(appEntry.Id);
		stringBuilder = report;
		StringBuilder stringBuilder4 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider3 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(23, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("第二次 ActivateOrLaunch → ");
		handler.AppendFormatted(activationResult2.Outcome);
		stringBuilder4.AppendLine(provider3, ref handler);
		if (activationResult2.Candidates.Count > 0)
		{
			foreach (WindowCandidate candidate in activationResult2.Candidates)
			{
				stringBuilder = report;
				StringBuilder stringBuilder5 = stringBuilder;
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider4 = invariantCulture;
				handler = new StringBuilder.AppendInterpolatedStringHandler(12, 2, stringBuilder, invariantCulture);
				handler.AppendLiteral("  · 候选窗口 0x");
				handler.AppendFormatted(candidate.Handle, "X");
				handler.AppendLiteral(" ");
				handler.AppendFormatted(candidate.Title);
				stringBuilder5.AppendLine(provider4, ref handler);
			}
		}
		if (!string.IsNullOrEmpty(activationResult2.Message))
		{
			stringBuilder = report;
			StringBuilder stringBuilder6 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider5 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(3, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("说明：");
			handler.AppendFormatted(activationResult2.Message);
			stringBuilder6.AppendLine(provider5, ref handler);
		}
		report.AppendLine();
		KillRecordedProcess(lastLaunchedProcessId);
		bool item = activationResult.Outcome == ActivationOutcome.Launched;
		bool item2 = array.Length != 0;
		ActivationOutcome outcome = activationResult2.Outcome;
		bool flag = ((outcome == ActivationOutcome.Activated || outcome == ActivationOutcome.MultipleWindowsFound) ? true : false);
		bool item3 = flag;
		string item4 = ((array.Length == 0) ? "一个窗口都没匹配到——映像路径匹配这一环可能失效了" : $"第一次 {activationResult.Outcome}，第二次 {activationResult2.Outcome}，窗口「{text}」");
		return (Launched: item, FoundWindow: item2, Activated: item3, Skipped: false, Detail: item4);
	}

	private static CrossDirectoryRestoreResult RunCrossDirectoryRestore(AppServiceHost host, AppPaths paths, AcceptanceWorkspace workspace, StringBuilder report)
	{
		AppEntry appEntry = host.Library.Items.FirstOrDefault((AppEntry item) => item?.IsUsable ?? false);
		if (appEntry == null)
		{
			return new CrossDirectoryRestoreResult(FileRestored: false, ReferenceRepointed: false, StillReadable: false, "应用库里没有可用条目");
		}
		string text = Path.Combine(paths.RootDirectory, "验收 自定义图标.png");
		File.WriteAllBytes(text, PngWriter.EncodeBgra(new byte[1024], 16, 16));
		if (!host.Maintenance.SetCustomIcon(appEntry.Id, text))
		{
			return new CrossDirectoryRestoreResult(FileRestored: false, ReferenceRepointed: false, StillReadable: false, "设置自定义图标失败");
		}
		string customIconPath = host.Library.Find(appEntry.Id).CustomIconPath;
		string text2 = Path.Combine(paths.BackupDirectory, "验收 归档");
		TransferResult transferResult = new BackupService(paths).BackupTo(text2);
		if (!transferResult.Succeeded)
		{
			return new CrossDirectoryRestoreResult(FileRestored: false, ReferenceRepointed: false, StillReadable: false, transferResult.Error ?? "备份失败");
		}
		AppPaths appPaths = new AppPaths(workspace.CreateChildDataRoot("phase3-cross-directory-restore"));
		TransferResult transferResult2 = new BackupService(appPaths).RestoreFrom(text2);
		StringBuilder stringBuilder = report;
		StringBuilder stringBuilder2 = stringBuilder;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("备份 → ");
		handler.AppendFormatted(text2);
		stringBuilder2.AppendLine(provider, ref handler);
		stringBuilder = report;
		StringBuilder stringBuilder3 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider2 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(6, 2, stringBuilder, invariantCulture);
		handler.AppendLiteral("恢复 → ");
		handler.AppendFormatted(appPaths.RootDirectory);
		handler.AppendLiteral("：");
		handler.AppendFormatted(transferResult2.Succeeded ? "成功" : transferResult2.Error);
		stringBuilder3.AppendLine(provider2, ref handler);
		report.AppendLine("恢复目标位于本次隔离验收工作区内，保留供核对。");
		stringBuilder = report;
		StringBuilder stringBuilder4 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider3 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("改写图标引用 ");
		handler.AppendFormatted(transferResult2.RepairedIconReferences);
		handler.AppendLiteral(" 条");
		stringBuilder4.AppendLine(provider3, ref handler);
		report.AppendLine();
		if (!transferResult2.Succeeded)
		{
			return new CrossDirectoryRestoreResult(FileRestored: false, ReferenceRepointed: false, StillReadable: false, transferResult2.Error ?? "恢复失败");
		}
		string text3 = AppPaths.ManagedCustomIconPath(appPaths.RootDirectory, Path.GetFileName(customIconPath));
		bool flag = File.Exists(text3);
		AppLibraryStore appLibraryStore = new AppLibraryStore(appPaths.AppLibraryFile);
		appLibraryStore.Load();
		string text4 = appLibraryStore.Find(appEntry.Id)?.CustomIconPath;
		bool flag2 = string.Equals(text4, text3, StringComparison.OrdinalIgnoreCase);
		stringBuilder = report;
		StringBuilder stringBuilder5 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider4 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(12, 2, stringBuilder, invariantCulture);
		handler.AppendLiteral("新目录里的图标文件：");
		handler.AppendFormatted(text3);
		handler.AppendLiteral("（");
		handler.AppendFormatted(flag ? "存在" : "缺失");
		handler.AppendLiteral("）");
		stringBuilder5.AppendLine(provider4, ref handler);
		stringBuilder = report;
		StringBuilder stringBuilder6 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider5 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("落盘引用：");
		handler.AppendFormatted(text4);
		stringBuilder6.AppendLine(provider5, ref handler);
		report.AppendLine();
		File.Delete(customIconPath);
		AppLibraryStore appLibraryStore2 = new AppLibraryStore(appPaths.AppLibraryFile);
		appLibraryStore2.Load();
		string text5 = appLibraryStore2.Find(appEntry.Id)?.CustomIconPath;
		bool flag3 = File.Exists(text5);
		stringBuilder = report;
		StringBuilder stringBuilder7 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider6 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(15, 2, stringBuilder, invariantCulture);
		handler.AppendLiteral("删除原始图标之后：引用 ");
		handler.AppendFormatted(text5);
		handler.AppendLiteral("，文件");
		handler.AppendFormatted(flag3 ? "可读" : "缺失");
		stringBuilder7.AppendLine(provider6, ref handler);
		report.AppendLine();
		string detail = $"图标 {Path.GetFileName(customIconPath)} → 新目录{(flag ? "已到位" : "缺失")}，引用{(flag2 ? "已改写" : ("仍为 " + text4))}，删除原始图标后{(flag3 ? "仍可读" : "失效")}";
		return new CrossDirectoryRestoreResult(flag, flag2, flag3, detail);
	}

	internal static void KillRecordedProcess(uint processId)
	{
		if (processId == 0)
		{
			return;
		}
		try
		{
			using Process process = Process.GetProcessById((int)processId);
			if (!process.HasExited)
			{
				process.Kill();
				process.WaitForExit(3000);
			}
		}
		catch (Exception ex) when ((ex is ArgumentException || ex is InvalidOperationException || ex is Win32Exception || ex is NotSupportedException) ? true : false)
		{
		}
	}

	private static void Section(StringBuilder report, string title)
	{
		report.AppendLine();
		report.AppendLine(title);
		report.AppendLine(new string('-', 46));
	}

	private static void Check(List<(string Label, CheckStatus Status, string Detail)> checks, string label, bool passed, string detail)
	{
		checks.Add((label, (!passed) ? CheckStatus.Failed : CheckStatus.Passed, detail));
	}

	private static void Skipped(List<(string Label, CheckStatus Status, string Detail)> checks, string label, string detail)
	{
		checks.Add((label, CheckStatus.Skipped, detail));
	}

	private static string DescribeFailures(ImportResult result)
	{
		if (result.Failures.Count != 0)
		{
			return string.Join("；", result.Failures.Select((ImportFailure failure) => $"{failure.Reason}"));
		}
		return "一条失败都没有";
	}

	private static string DescribeKeys(Dictionary<string, AppEntry> map)
	{
		if (map.Count != 0)
		{
			return string.Join("、", map.Keys);
		}
		return "一条都没对上";
	}
}
