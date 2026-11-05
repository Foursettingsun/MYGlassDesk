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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using GlassDesk.AppServices;
using GlassDesk.Core.Contracts;
using GlassDesk.Core.Models;
using GlassDesk.Core.Models.Settings;
using GlassDesk.Core.Storage;
using GlassDesk.Dock;
using GlassDesk.Platform.Displays;
using GlassDesk.Platform.Shell;
using GlassDesk.Platform.Storage;

namespace GlassDesk.App.Startup;

internal static class Phase4Acceptance
{
	private const string ClassicTarget = "C:\\Windows\\System32\\charmap.exe";

	private const string NotepadAumid = "Microsoft.WindowsNotepad_8wekyb3d8bbwe!App";

	public static int Run(AppPaths realPaths, IReadOnlyList<string> arguments, AcceptanceWorkspace workspace)
	{
		ArgumentNullException.ThrowIfNull(realPaths, "realPaths");
		ArgumentNullException.ThrowIfNull(arguments, "arguments");
		StringBuilder stringBuilder = new StringBuilder();
		List<(string, CheckStatus, string)> list = new List<(string, CheckStatus, string)>();
		AppPaths appPaths = new AppPaths(workspace.DataRoot);
		appPaths.EnsureCreated();
		stringBuilder.AppendLine("GlassDesk 阶段 4 验收");
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
		GlassDeskDataStore glassDeskDataStore = new GlassDeskDataStore(appPaths);
		glassDeskDataStore.LoadAll();
		using AppServiceHost appServiceHost = new AppServiceHost(glassDeskDataStore);
		appServiceHost.Running.Start();
		Section(stringBuilder, "一、显式身份随导入存进条目");
		CheckImportedIdentity(appServiceHost, appPaths, list, stringBuilder);
		Section(stringBuilder, "二、窗口归属匹配规则（对伪造快照的纯函数断言）");
		CheckMatchingRules(list, stringBuilder);
		Section(stringBuilder, "三、运行状态：初始枚举与出现/消失调和");
		bool flag = CheckRunningTracker(appServiceHost, list, stringBuilder);
		Section(stringBuilder, "四、优先激活：第一次启动、第二次激活（D-09）");
		if (flag)
		{
			CheckActivateOrLaunch(appServiceHost, list, stringBuilder);
		}
		else
		{
			Skip(list, stringBuilder, "用户桌面已有字符映射表窗口，激活检查跳过（避免动用户的窗口）");
		}
		Section(stringBuilder, "五、默认值（D-21/D-22）：写入配置文件再读回来核对");
		CheckSettingsDefaults(appPaths, list, stringBuilder);
		Section(stringBuilder, "六、摆放：四种位置 × 每块显示器实机落位（D-15/D-23）");
		CheckPhysicalPlacement(list, stringBuilder);
		Section(stringBuilder, "七、放大峰值可调（D-17）：设置文件驱动几何");
		CheckMagnificationFromSettings(appPaths, list, stringBuilder);
		Section(stringBuilder, "八、固定、排序与移出（D-02/D-03/R-08 的服务层）");
		Section(stringBuilder, "十、500 条库与超长固定行的正确性");
		CheckScale(appServiceHost, appPaths, list, stringBuilder);
		CheckPinnedAndReorder(appServiceHost, appPaths, list, stringBuilder);
		Section(stringBuilder, "九、内置入口（D-14：回收站经外壳打开；拼错的 shell: 目标被拒绝）");
		CheckRecycleBinEntry(list, stringBuilder);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("检查汇总");
		stringBuilder.AppendLine(new string('-', 46));
		foreach (var item4 in list)
		{
			string item = item4.Item1;
			CheckStatus item2 = item4.Item2;
			string item3 = item4.Item3;
			string value = item2 switch
			{
				CheckStatus.Passed => "✓", 
				CheckStatus.Skipped => "○", 
				_ => "✗", 
			};
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder6 = stringBuilder2;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider4 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(1, 2, stringBuilder2, invariantCulture);
			handler.AppendFormatted(value);
			handler.AppendLiteral(" ");
			handler.AppendFormatted(item);
			stringBuilder6.AppendLine(provider4, ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder7 = stringBuilder2;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider5 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(4, 1, stringBuilder2, invariantCulture);
			handler.AppendLiteral("    ");
			handler.AppendFormatted(item3);
			stringBuilder7.AppendLine(provider5, ref handler);
		}
		int num = Enumerable.Count(list, ((string Label, CheckStatus Status, string Detail) check) => check.Status == CheckStatus.Failed);
		int value2 = Enumerable.Count(list, ((string Label, CheckStatus Status, string Detail) check) => check.Status == CheckStatus.Skipped);
		int value3 = Enumerable.Count(list, ((string Label, CheckStatus Status, string Detail) check) => check.Status == CheckStatus.Passed);
		stringBuilder.AppendLine();
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder8 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider6 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(25, 4, stringBuilder2, invariantCulture);
		handler.AppendLiteral("共 ");
		handler.AppendFormatted(list.Count);
		handler.AppendLiteral(" 项检查：通过 ");
		handler.AppendFormatted(value3);
		handler.AppendLiteral(" 项，失败 ");
		handler.AppendFormatted(num);
		handler.AppendLiteral(" 项，跳过 ");
		handler.AppendFormatted(value2);
		handler.AppendLiteral(" 项。");
		stringBuilder8.AppendLine(provider6, ref handler);
		stringBuilder.AppendLine("○ 是「条件不具备、没跑」的项——它们既不算通过也不算失败，交付报告里按未验证列出。");
		stringBuilder.AppendLine("打包应用与 UWP 的真实激活、多窗口选择列表的交互、运行状态点的界面呈现");
		stringBuilder.AppendLine("不由本命令完整覆盖；另跑 build/build.ps1 -UiAcceptance，并核对阶段4-自动交互验收报告中的外部覆盖状态。");
		Console.Write(stringBuilder.ToString());
		AcceptanceEntryPoint.WriteText(workspace.GetOutput("report"), stringBuilder.ToString());
		ReportWriter.Write(stringBuilder.ToString(), null);
		return (num != 0) ? 1 : 0;
	}

	private static void CheckSettingsDefaults(AppPaths paths, List<(string Label, CheckStatus Status, string Detail)> checks, StringBuilder report)
	{
		AppPaths appPaths = new AppPaths(Path.Combine(paths.RootDirectory, "默认值核对"));
		new SettingsStore(appPaths.SettingsFile).Save();
		SettingsStore settingsStore = new SettingsStore(appPaths.SettingsFile);
		settingsStore.Load();
		DockSettings dock = settingsStore.Current.Dock;
		AppearanceSettings appearance = settingsStore.Current.Appearance;
		Check(checks, "默认图标尺寸 48 DIP（D-21）", dock.IconSize == 48.0, $"{dock.IconSize}");
		Check(checks, "默认图标间距 8 DIP（D-21）", dock.IconSpacing == 8.0, $"{dock.IconSpacing}");
		Check(checks, "默认放大峰值 1.35×（D-21）", Math.Abs(dock.MagnificationPeak - 1.35) < 0.0001, $"{dock.MagnificationPeak}");
		Check(checks, "液态玻璃 Dock 圆角 40 DIP", appearance.DockCornerRadius == 40.0, $"{appearance.DockCornerRadius}");
		int autoHideLeaveDelayMs = dock.AutoHideLeaveDelayMs;
		Check(checks, "默认离开延迟约 300 ms（D-19）", autoHideLeaveDelayMs >= 250 && autoHideLeaveDelayMs <= 350, $"{dock.AutoHideLeaveDelayMs} ms");
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, report, invariantCulture);
		handler.AppendLiteral("核对文件：");
		handler.AppendFormatted(appPaths.SettingsFile);
		report.AppendLine(invariantCulture, ref handler);
		report.AppendLine();
	}

	private static void CheckPhysicalPlacement(List<(string Label, CheckStatus Status, string Detail)> checks, StringBuilder report)
	{
		IReadOnlyList<DisplayInfo> readOnlyList = DisplayService.Enumerate();
		if (readOnlyList.Count == 0)
		{
			Check(checks, "至少枚举到一块显示器", passed: false, "枚举结果为空");
			return;
		}
		Check(checks, "显示器枚举可用", passed: true, $"枚举到 {readOnlyList.Count} 块");
		if (readOnlyList.Count < 2)
		{
			Skip(checks, report, "副屏不存在，多屏摆放只测了主屏");
		}
		Window window = new Window
		{
			Width = 900.0,
			Height = 90.0,
			WindowStyle = WindowStyle.None,
			ShowInTaskbar = false,
			ShowActivated = false,
			Content = new Border
			{
				Background = Brushes.Transparent
			}
		};
		try
		{
			window.Show();
			nint handle = new WindowInteropHelper(window).Handle;
			foreach (DisplayInfo item in readOnlyList)
			{
				DockPlacement[] values = Enum.GetValues<DockPlacement>();
				foreach (DockPlacement dockPlacement in values)
				{
					int windowWidthPixels = Math.Max(1, item.ToPixels(window.ActualWidth));
					int windowHeightPixels = Math.Max(1, item.ToPixels(window.ActualHeight));
					PixelRect pixelRect = DockPlacementCalculator.Calculate(item.WorkArea, dockPlacement, 0.0, 0.0, windowWidthPixels, windowHeightPixels, item.Dpi);
					DisplayService.MoveWindowToPixelRect(handle, pixelRect);
					PixelRect? windowPixelBounds = DisplayService.GetWindowPixelBounds(handle);
					bool passed = windowPixelBounds.HasValue && Math.Abs(windowPixelBounds.Value.X - pixelRect.X) <= 1 && Math.Abs(windowPixelBounds.Value.Y - pixelRect.Y) <= 1 && windowPixelBounds.Value.X >= item.WorkArea.X && windowPixelBounds.Value.Y >= item.WorkArea.Y && windowPixelBounds.Value.Right <= item.WorkArea.Right && windowPixelBounds.Value.Bottom <= item.WorkArea.Bottom;
					Check(checks, $"{item.DeviceName}{(item.IsPrimary ? "（主屏）" : string.Empty)} {dockPlacement} 按计算落位且不出工作区", passed, $"期望 {pixelRect}，实际 {windowPixelBounds?.ToString() ?? "读不到"}");
				}
			}
		}
		finally
		{
			window.Close();
		}
		report.AppendLine("每块显示器 × 四种位置逐一实机摆放并读回核对；副屏缺失时如实标注未验证。");
		report.AppendLine();
	}

	private static void CheckMagnificationFromSettings(AppPaths paths, List<(string Label, CheckStatus Status, string Detail)> checks, StringBuilder report)
	{
		AppPaths appPaths = new AppPaths(Path.Combine(paths.RootDirectory, "放大峰值核对"));
		SettingsStore settingsStore = new SettingsStore(appPaths.SettingsFile);
		settingsStore.Load();
		settingsStore.Current.Dock.MagnificationPeak = 2.0;
		settingsStore.Save();
		SettingsStore settingsStore2 = new SettingsStore(appPaths.SettingsFile);
		settingsStore2.Load();
		double magnificationPeak = settingsStore2.Current.Dock.MagnificationPeak;
		Check(checks, "设置文件里的放大峰值能被原样读回", Math.Abs(magnificationPeak - 2.0) < 0.0001, $"{magnificationPeak}");
		TileGeometry[] array = DockMagnification.Compute(5, settingsStore2.Current.Dock.IconSize, settingsStore2.Current.Dock.IconSpacing, magnificationPeak, DockMagnification.SlotCenter(2, settingsStore2.Current.Dock.IconSize, settingsStore2.Current.Dock.IconSpacing, magnificationPeak));
		Check(checks, "读回的峰值驱动几何：指针下方磁贴取峰值", Math.Abs(array[2].Scale - 2.0) < 0.0001, $"第 2 枚缩放 {array[2].Scale}");
	}

	private static void CheckPinnedAndReorder(AppServiceHost host, AppPaths paths, List<(string Label, CheckStatus Status, string Detail)> checks, StringBuilder report)
	{
		ImportResult importResult = host.Import.ImportItems(new _003C_003Ez__ReadOnlyArray<ImportRequest>(new ImportRequest[2]
		{
			new ImportRequest
			{
				Source = "C:\\Windows\\System32\\charmap.exe"
			},
			new ImportRequest
			{
				Source = "C:\\Windows\\System32\\drivers\\etc\\hosts"
			}
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
		AppEntry appEntry2;
		if (importResult.Added.Count > 1)
		{
			appEntry2 = importResult.Added[1];
		}
		else
		{
			appEntry2 = ((importResult.Duplicates.Count > 0) ? importResult.Duplicates[0] : null);
		}
		if (appEntry == null || appEntry2 == null || appEntry.Id == appEntry2.Id)
		{
			Check(checks, "排序检查的准备条目导入成功", passed: false, $"新增 {importResult.Added.Count}，重复 {importResult.Duplicates.Count}，失败 {importResult.Failures.Count}");
			return;
		}
		LayoutStore layout = host.Layouts;
		layout.LoadDock();
		int saveCount = 0;
		DockReorderModel dockReorderModel = new DockReorderModel((IReadOnlyList<string> order) =>
		{
			layout.Dock.PinnedItemIds = order.ToList();
			layout.SaveDock();
			saveCount++;
		});
		bool flag = File.Exists(layout.DockFilePath);
		byte[] array = (flag ? File.ReadAllBytes(layout.DockFilePath) : null);
		dockReorderModel.Begin(new _003C_003Ez__ReadOnlyArray<string>(new string[2] { appEntry.Id, appEntry2.Id }));
		dockReorderModel.PreviewMove(appEntry2.Id, 0);
		dockReorderModel.PreviewMove(appEntry2.Id, 1);
		dockReorderModel.PreviewMove(appEntry2.Id, 0);
		bool passed = (flag ? array.SequenceEqual(File.ReadAllBytes(layout.DockFilePath)) : (!File.Exists(layout.DockFilePath)));
		Check(checks, "拖拽预览期间布局文件一个字节都没变（R-08）", passed, flag ? "预览 3 次后文件仍为原始内容" : "预览期间布局文件根本没有被创建");
		ReorderOutcome reorderOutcome = dockReorderModel.Commit();
		Check(checks, "提交恰好落盘一次", reorderOutcome == ReorderOutcome.Committed && saveCount == 1, $"结果 {reorderOutcome}，保存次数 {saveCount}");
		LayoutStore layoutStore = new LayoutStore(Path.Combine(paths.RootDirectory, "dock-layout.json"), Path.Combine(paths.RootDirectory, "launcher-layout.json"));
		layoutStore.LoadDock();
		Check(checks, "重新加载（模拟重启）后顺序一致（D-02）", layoutStore.Dock.PinnedItemIds.Count == 2 && layoutStore.Dock.PinnedItemIds[0] == appEntry2.Id, "顺序：" + string.Join(',', layoutStore.Dock.PinnedItemIds));
		saveCount = 0;
		dockReorderModel.Begin(new _003C_003Ez__ReadOnlyArray<string>(new string[2] { appEntry2.Id, appEntry.Id }));
		dockReorderModel.PreviewMove(appEntry.Id, 0);
		dockReorderModel.Cancel();
		Check(checks, "取消拖拽（Esc）零落盘（R-08）", saveCount == 0 && layoutStore.Dock.PinnedItemIds[0] == appEntry2.Id, $"保存次数 {saveCount}");
		DockReorderModel dockReorderModel2 = new DockReorderModel((IReadOnlyList<string> _) =>
		{
			throw new IOException("演练：磁盘拒绝");
		});
		dockReorderModel2.Begin(new _003C_003Ez__ReadOnlyArray<string>(new string[2] { appEntry.Id, appEntry2.Id }));
		dockReorderModel2.PreviewMove(appEntry.Id, 1);
		ReorderOutcome reorderOutcome2 = dockReorderModel2.Commit();
		Check(checks, "提交失败时恢复原顺序（R-08）", reorderOutcome2 == ReorderOutcome.SaveFailed && dockReorderModel2.Order.SequenceEqual(new _003C_003Ez__ReadOnlyArray<string>(new string[2] { appEntry.Id, appEntry2.Id })), $"结果 {reorderOutcome2}");
		CheckFiveAppReorder(host, paths, checks, report);
		layout.UnpinFromDock(appEntry2.Id);
		layout.SaveDock();
		LayoutStore layoutStore2 = new LayoutStore(Path.Combine(paths.RootDirectory, "dock-layout.json"), Path.Combine(paths.RootDirectory, "launcher-layout.json"));
		layoutStore2.LoadDock();
		bool passed2 = host.Library.Find(appEntry2.Id) != null && !layoutStore2.Dock.PinnedItemIds.Contains(appEntry2.Id);
		Check(checks, "移出后重启：布局没有它，启动台（应用库）仍有该项（D-03）", passed2, $"库中存在={host.Library.Find(appEntry2.Id) != null}，固定数={layoutStore2.Dock.PinnedItemIds.Count}");
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, report, invariantCulture);
		handler.AppendLiteral("布局文件：");
		handler.AppendFormatted(layout.DockFilePath);
		report.AppendLine(invariantCulture, ref handler);
		report.AppendLine();
	}

	private static void CheckRecycleBinEntry(List<(string Label, CheckStatus Status, string Detail)> checks, StringBuilder report)
	{
		bool? flag = ShellTargets.CanResolve("shell:RecycleBinFolder");
		bool? flag2 = ShellTargets.CanResolve("shell:GlassDeskNoSuchFolder");
		if (!flag.HasValue || !flag2.HasValue)
		{
			Skip(checks, report, "外壳解析接口不可用，回收站目标的检查跳过");
			return;
		}
		Check(checks, "外壳接受 shell:RecycleBinFolder（回收站入口的链路）", flag == true, "SHParseDisplayName 返回成功");
		Check(checks, "外壳拒绝拼错的 shell: 目标（存在性检查没有被放宽）", flag2 == false, "SHParseDisplayName 返回失败");
		report.AppendLine("这一节只做解析检查，不开窗口、不弹框；真实打开由人工核对覆盖。");
		report.AppendLine();
	}

	private static void CheckScale(AppServiceHost host, AppPaths paths, List<(string Label, CheckStatus Status, string Detail)> checks, StringBuilder report)
	{
		int count = host.Library.Items.Count;
		int num = 500;
		int num2 = 0;
		for (int i = count; i < num; i++)
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
			handler.AppendLiteral("规模条目 ");
			handler.AppendFormatted(i, "D4");
			appEntry.DisplayName = string.Create(provider, ref handler);
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider2 = invariantCulture;
			DefaultInterpolatedStringHandler handler2 = new DefaultInterpolatedStringHandler(8, 1, invariantCulture);
			handler2.AppendLiteral("--scale ");
			handler2.AppendFormatted(i);
			appEntry.Arguments = string.Create(provider2, ref handler2);
			appEntry.IconSource = "C:\\Windows\\System32\\notepad.exe";
			AppEntry entry = appEntry;
			if (host.Library.Add(entry) == AddEntryResult.Added)
			{
				num2++;
			}
		}
		host.Library.Save();
		Check(checks, "应用库补足到 500 条以上", host.Library.Items.Count >= num, $"库共 {host.Library.Items.Count} 条（本次新增 {num2}）");
		LayoutStore layouts = host.Layouts;
		List<string> list = new List<string>(layouts.Dock.PinnedItemIds);
		foreach (AppEntry item in host.Library.Items.Take(200))
		{
			if (item != null && item.IsUsable && !list.Contains(item.Id))
			{
				list.Add(item.Id);
			}
		}
		layouts.Dock.PinnedItemIds = list;
		layouts.SaveDock();
		LayoutStore layoutStore = new LayoutStore(Path.Combine(paths.RootDirectory, "dock-layout.json"), Path.Combine(paths.RootDirectory, "launcher-layout.json"));
		layoutStore.LoadDock();
		Check(checks, "200 条固定行保存后重新加载顺序一致", layoutStore.Dock.PinnedItemIds.Count == list.Count && list.SequenceEqual(layoutStore.Dock.PinnedItemIds), $"固定 {list.Count} 项，重载 {layoutStore.Dock.PinnedItemIds.Count} 项");
		double num3 = DockMagnification.RequiredLength(list.Count, 48.0, 8.0, 1.35);
		TileGeometry[] array = DockMagnification.Compute(list.Count, 48.0, 8.0, 1.35, null);
		int num4 = DockMagnification.HitTest(array, 48.0, array[199].Center);
		Check(checks, "超长固定行的几何计算在 200 项下正确", num3 > 0.0 && num4 == 199, $"总长 {num3:F0} DIP，末位命中索引 {num4}");
		AppLibraryStore appLibraryStore = new AppLibraryStore(host.Paths.AppLibraryFile);
		appLibraryStore.Load();
		Check(checks, "重启后应用库条数一致", appLibraryStore.Items.Count == host.Library.Items.Count, $"重载 {appLibraryStore.Items.Count} 条");
		report.AppendLine();
	}

	private static void CheckFiveAppReorder(AppServiceHost host, AppPaths paths, List<(string Label, CheckStatus Status, string Detail)> checks, StringBuilder report)
	{
		LayoutStore layout = host.Layouts;
		string dockFilePath = layout.DockFilePath;
		List<string> list = (from entry in host.Library.Items.Where((AppEntry entry) => entry?.IsUsable ?? false).Take(5)
			select entry.Id).ToList();
		if (list.Count < 5)
		{
			Skip(checks, report, "库里不足 5 条可用条目，5 应用排序检查跳过");
			return;
		}
		int saveCount = 0;
		DockReorderModel dockReorderModel = new DockReorderModel((IReadOnlyList<string> order) =>
		{
			List<string> pinnedItemIds3 = DockLayoutPolicy.Sanitize(order, (string id) => host.Library.Find(id) != null);
			layout.Dock.PinnedItemIds = pinnedItemIds3;
			layout.SaveDock();
			saveCount++;
		});
		dockReorderModel.Begin(list);
		dockReorderModel.PreviewMove(list[4], 0);
		dockReorderModel.PreviewMove(list[0], 2);
		ReorderOutcome reorderOutcome = dockReorderModel.Commit();
		string[] array = new string[5]
		{
			list[4],
			list[1],
			list[0],
			list[2],
			list[3]
		};
		Check(checks, "5 个应用拖拽排序提交成功", reorderOutcome == ReorderOutcome.Committed && saveCount == 1, $"结果 {reorderOutcome}，保存次数 {saveCount}");
		LayoutStore layoutStore = new LayoutStore(dockFilePath, Path.Combine(paths.RootDirectory, "launcher-layout.json"));
		layoutStore.LoadDock();
		List<string> pinnedItemIds = layoutStore.Dock.PinnedItemIds;
		Check(checks, "重启后 5 个应用顺序正确（D-02）", pinnedItemIds.Count == 5 && pinnedItemIds.SequenceEqual(array), "落盘顺序：" + string.Join(',', pinnedItemIds));
		Check(checks, "布局文件里只有 5 个应用 ID，没有内置入口（缺陷 1 的验收标准）", pinnedItemIds.Count == 5 && !pinnedItemIds.Any(DockLayoutPolicy.IsEntryTile), $"共 {pinnedItemIds.Count} 项，其中 entry:* {Enumerable.Count(pinnedItemIds, DockLayoutPolicy.IsEntryTile)} 项");
		List<string> list2 = new List<string>(array);
		list2.Insert(1, "entry:launcher");
		list2.Add("entry:showdesktop");
		list2.Add("entry:recyclebin");
		list2.Add("早就删掉的条目id");
		layout.Dock.PinnedItemIds = list2;
		layout.SaveDock();
		List<string> pinnedItemIds2 = DockLayoutPolicy.Sanitize(layout.Dock.PinnedItemIds, (string id) => host.Library.Find(id) != null);
		LayoutStore layoutStore2 = new LayoutStore(dockFilePath, Path.Combine(paths.RootDirectory, "launcher-layout.json"));
		layout.Dock.PinnedItemIds = pinnedItemIds2;
		layout.SaveDock();
		layoutStore2.LoadDock();
		Check(checks, "旧布局里的三个入口与陌生 id 被清掉，应用顺序不变", layoutStore2.Dock.PinnedItemIds.SequenceEqual(array), "清理后：" + string.Join(',', layoutStore2.Dock.PinnedItemIds));
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, report, invariantCulture);
		handler.AppendLiteral("布局文件：");
		handler.AppendFormatted(dockFilePath);
		report.AppendLine(invariantCulture, ref handler);
		report.AppendLine();
	}

	private static void CheckImportedIdentity(AppServiceHost host, AppPaths paths, List<(string Label, CheckStatus Status, string Detail)> checks, StringBuilder report)
	{
		string text = Path.Combine(paths.RootDirectory, "身份样本");
		Directory.CreateDirectory(text);
		string text2 = Path.Combine(text, "带 身份 的记事本.lnk");
		bool flag = ShortcutWriter.Write(text2, new ShortcutDefinition("C:\\Windows\\System32\\notepad.exe", "", "", "", "阶段 4 验收样本", "Microsoft.WindowsNotepad_8wekyb3d8bbwe!App"));
		Check(checks, "带身份的样本快捷方式写出成功", flag, text2);
		if (flag)
		{
			ShortcutInfo shortcutInfo = ShortcutResolver.Resolve(text2);
			Check(checks, "样本上的显式身份能被读回", shortcutInfo?.Aumid == "Microsoft.WindowsNotepad_8wekyb3d8bbwe!App", "读回：" + (shortcutInfo?.Aumid ?? "null"));
			ImportResult importResult = host.Import.ImportItems(new _003C_003Ez__ReadOnlySingleElementList<ImportRequest>(new ImportRequest
			{
				Source = text2
			}));
			Check(checks, "带身份的样本导入成功", importResult.Added.Count == 1, $"新增 {importResult.Added.Count}，失败 {importResult.Failures.Count}");
			AppEntry appEntry = ((importResult.Added.Count > 0) ? importResult.Added[0] : null);
			Check(checks, "条目上的身份与样本一致", appEntry?.AppUserModelId == "Microsoft.WindowsNotepad_8wekyb3d8bbwe!App", "条目身份：" + (appEntry?.AppUserModelId ?? "null"));
			Check(checks, "会话内解析不再读文件也返回同一身份", WindowMatcher.ResolveAumid(appEntry) == "Microsoft.WindowsNotepad_8wekyb3d8bbwe!App", WindowMatcher.ResolveAumid(appEntry) ?? "null");
			string text3 = Path.Combine(text, "普通 的快捷方式.lnk");
			if (ShortcutWriter.Write(text3, new ShortcutDefinition("C:\\Windows\\System32\\charmap.exe", "", "", "", "阶段 4 验收对照样本")))
			{
				AppEntry appEntry2 = host.Import.ImportItems(new _003C_003Ez__ReadOnlySingleElementList<ImportRequest>(new ImportRequest
				{
					Source = text3
				})).Added[0];
				Check(checks, "不带身份的快捷方式导入后身份为 null", appEntry2 != null && appEntry2.AppUserModelId == null, "条目身份：" + (appEntry2?.AppUserModelId ?? "null"));
			}
			else
			{
				Check(checks, "不带身份的快捷方式导入后身份为 null", passed: false, "对照样本写出失败");
			}
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, report, invariantCulture);
			handler.AppendLiteral("样本目录：");
			handler.AppendFormatted(text);
			report.AppendLine(invariantCulture, ref handler);
			report.AppendLine();
		}
	}

	private static void CheckMatchingRules(List<(string Label, CheckStatus Status, string Detail)> checks, StringBuilder report)
	{
		IReadOnlyList<WindowIdentityInfo> readOnlyList = WindowMatcher.MatchWindows(new AppEntry
		{
			Id = "c",
			Kind = AppItemKind.Program,
			Target = "C:\\Windows\\System32\\charmap.exe"
		}, new _003C_003Ez__ReadOnlySingleElementList<WindowIdentityInfo>(Window(1, "C:\\WINDOWS\\SYSTEM32\\CHARMAP.EXE", null, null)), null);
		Check(checks, "经典程序按映像路径匹配（大小写不敏感）", readOnlyList.Count == 1 && readOnlyList[0].Handle == 1, $"匹配 {readOnlyList.Count} 个");
		IReadOnlyList<WindowIdentityInfo> readOnlyList2 = WindowMatcher.MatchWindows(new AppEntry
		{
			Id = "p",
			Kind = AppItemKind.Program,
			Target = "C:\\Windows\\System32\\notepad.exe",
			AppUserModelId = "Microsoft.WindowsNotepad_8wekyb3d8bbwe!App"
		}, new _003C_003Ez__ReadOnlySingleElementList<WindowIdentityInfo>(Window(2, "C:\\Program Files\\WindowsApps\\Microsoft.WindowsNotepad_...\\Notepad.exe", null, "Microsoft.WindowsNotepad_8wekyb3d8bbwe!App")), "Microsoft.WindowsNotepad_8wekyb3d8bbwe!App");
		Check(checks, "打包应用按进程包身份匹配", readOnlyList2.Count == 1 && readOnlyList2[0].Handle == 2, $"匹配 {readOnlyList2.Count} 个");
		IReadOnlyList<WindowIdentityInfo> readOnlyList3 = WindowMatcher.MatchWindows(new AppEntry
		{
			Id = "u",
			Kind = AppItemKind.Shortcut,
			Target = "C:\\某\\设置.lnk",
			AppUserModelId = "windows.immersivecontrolpanel_cw5n1h2txyewy!microsoft.windows.immersivecontrolpanel"
		}, new _003C_003Ez__ReadOnlySingleElementList<WindowIdentityInfo>(Window(3, "C:\\Windows\\System32\\ApplicationFrameHost.exe", "windows.immersivecontrolpanel_cw5n1h2txyewy!microsoft.windows.immersivecontrolpanel", null)), "windows.immersivecontrolpanel_cw5n1h2txyewy!microsoft.windows.immersivecontrolpanel");
		Check(checks, "UWP 框架窗口按窗口显式身份匹配", readOnlyList3.Count == 1 && readOnlyList3[0].Handle == 3, $"匹配 {readOnlyList3.Count} 个");
		IReadOnlyList<WindowIdentityInfo> readOnlyList4 = WindowMatcher.MatchWindows(new AppEntry
		{
			Id = "x",
			Kind = AppItemKind.Shortcut,
			Target = "C:\\某\\应用A.lnk",
			AppUserModelId = "com.app.a"
		}, new _003C_003Ez__ReadOnlySingleElementList<WindowIdentityInfo>(Window(4, "C:\\Windows\\System32\\ApplicationFrameHost.exe", "com.app.b", null)), "com.app.a");
		Check(checks, "身份不一致的窗口不匹配，也不回退到宿主进程路径", readOnlyList4.Count == 0, $"匹配 {readOnlyList4.Count} 个");
		IReadOnlyList<WindowIdentityInfo> readOnlyList5 = WindowMatcher.MatchWindows(new AppEntry
		{
			Id = "s",
			Kind = AppItemKind.Program,
			Target = "C:\\tools\\run.bat"
		}, new _003C_003Ez__ReadOnlySingleElementList<WindowIdentityInfo>(Window(5, "C:\\tools\\run.bat", null, null)), null);
		Check(checks, "脚本目标不做映像匹配（退化成老实启动）", readOnlyList5.Count == 0, $"匹配 {readOnlyList5.Count} 个");
		report.AppendLine("规则依据：阶段 4 探针 A 实测（docs/evidence/阶段4-探针实测输出.txt）。");
		report.AppendLine();
		static WindowIdentityInfo Window(nint handle, string? imagePath, string? windowAumid, string? processAumid)
		{
			return new WindowIdentityInfo(handle, $"窗口 {handle}", 1000u, IsMinimized: false, imagePath, windowAumid, processAumid);
		}
	}

	private static bool CheckRunningTracker(AppServiceHost host, List<(string Label, CheckStatus Status, string Detail)> checks, StringBuilder report)
	{
		int count = host.Running.LastWindows.Count;
		Check(checks, "启动时已完成初始枚举（看见启动前已有的窗口）", count > 0, $"初始窗口 {count} 个");
		if (WindowLocator.FindWindowsForImage("C:\\Windows\\System32\\charmap.exe").Count > 0)
		{
			report.AppendLine("用户桌面已有字符映射表窗口；出现/消失调和检查跳过并标注为未验证。");
			report.AppendLine();
			Skip(checks, report, "运行集合的调和检查跳过（用户桌面已有靶子窗口）");
			return false;
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
			Check(checks, "靶子条目导入成功", passed: false, "导入失败");
			return false;
		}
		Check(checks, "靶子启动前不在运行集合中", !host.Running.IsRunning(appEntry), "未运行");
		ShellLaunchOutcome shellLaunchOutcome = ShellExecutor.Open("C:\\Windows\\System32\\charmap.exe");
		if (!shellLaunchOutcome.Started || shellLaunchOutcome.ProcessId == 0)
		{
			Check(checks, "靶子启动成功", passed: false, $"外壳错误码 {shellLaunchOutcome.ErrorCode}");
			return true;
		}
		try
		{
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, report, invariantCulture);
			handler.AppendLiteral("靶子 charmap 进程 id：");
			handler.AppendFormatted(shellLaunchOutcome.ProcessId);
			report.AppendLine(invariantCulture, ref handler);
			bool flag = false;
			DateTime dateTime = DateTime.UtcNow.AddSeconds(10.0);
			while (DateTime.UtcNow < dateTime)
			{
				if (host.Running.Reconcile() > 0 && host.Running.IsRunning(appEntry))
				{
					flag = true;
					break;
				}
				Thread.Sleep(300);
			}
			Check(checks, "靶子窗口出现后进入运行集合（D-07 的服务层）", flag, flag ? "调和后 IsRunning = true" : "10 秒内未观察到窗口");
			IReadOnlyList<WindowIdentityInfo> readOnlyList = host.Running.FindWindows(appEntry);
			Check(checks, "匹配到的窗口确实是靶子的", readOnlyList.Count == 0 || readOnlyList.All((WindowIdentityInfo w) => w.ImagePath != null && w.ImagePath.EndsWith("charmap.exe", StringComparison.OrdinalIgnoreCase)), $"匹配 {readOnlyList.Count} 个");
		}
		finally
		{
			try
			{
				using Process process = Process.GetProcessById((int)shellLaunchOutcome.ProcessId);
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
		bool flag2 = false;
		DateTime dateTime2 = DateTime.UtcNow.AddSeconds(10.0);
		while (DateTime.UtcNow < dateTime2)
		{
			Thread.Sleep(300);
			host.Running.Reconcile();
			if (!host.Running.IsRunning(appEntry))
			{
				flag2 = true;
				break;
			}
		}
		Check(checks, "靶子进程结束后退出运行集合（调和覆盖「消失」）", flag2, flag2 ? "调和后 IsRunning = false" : "10 秒后仍显示运行中");
		report.AppendLine();
		return true;
	}

	private static void CheckActivateOrLaunch(AppServiceHost host, List<(string Label, CheckStatus Status, string Detail)> checks, StringBuilder report)
	{
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
			Check(checks, "激活检查的靶子条目存在", passed: false, "导入失败");
			return;
		}
		ActivationResult activationResult = host.Launch.ActivateOrLaunch(appEntry.Id);
		uint lastLaunchedProcessId = host.Launch.LastLaunchedProcessId;
		StringBuilder stringBuilder = report;
		StringBuilder stringBuilder2 = stringBuilder;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(23, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("第一次 ActivateOrLaunch → ");
		handler.AppendFormatted(activationResult.Outcome);
		stringBuilder2.AppendLine(provider, ref handler);
		try
		{
			DateTime dateTime = DateTime.UtcNow.AddSeconds(10.0);
			bool flag = false;
			while (DateTime.UtcNow < dateTime)
			{
				if (WindowLocator.FindWindowsForImage("C:\\Windows\\System32\\charmap.exe").Count > 0)
				{
					flag = true;
					break;
				}
				Thread.Sleep(300);
			}
			Check(checks, "第一次请求启动了应用（Launched）", activationResult.Outcome == ActivationOutcome.Launched, activationResult.Outcome.ToString());
			Check(checks, "窗口随后真实出现", flag, flag ? "已出现" : "10 秒内未出现");
			ActivationResult activationResult2 = host.Launch.ActivateOrLaunch(appEntry.Id);
			stringBuilder = report;
			StringBuilder stringBuilder3 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider2 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(23, 1, stringBuilder, invariantCulture);
			handler.AppendLiteral("第二次 ActivateOrLaunch → ");
			handler.AppendFormatted(activationResult2.Outcome);
			stringBuilder3.AppendLine(provider2, ref handler);
			ActivationOutcome outcome = activationResult2.Outcome;
			bool passed = ((outcome == ActivationOutcome.Activated || outcome == ActivationOutcome.MultipleWindowsFound) ? true : false);
			Check(checks, "第二次请求激活已有窗口而不是再起一个（D-09）", passed, activationResult2.Outcome.ToString());
		}
		finally
		{
			Phase3Acceptance.KillRecordedProcess(lastLaunchedProcessId);
		}
		report.AppendLine();
	}

	private static void Skip(List<(string Label, CheckStatus Status, string Detail)> checks, StringBuilder report, string reason)
	{
		checks.Add((reason, CheckStatus.Skipped, reason));
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(8, 1, report, invariantCulture);
		handler.AppendLiteral("跳过（未验证）：");
		handler.AppendFormatted(reason);
		report.AppendLine(invariantCulture, ref handler);
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
}
