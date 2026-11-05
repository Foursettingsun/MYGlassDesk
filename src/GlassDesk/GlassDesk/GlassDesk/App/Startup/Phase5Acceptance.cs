using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using GlassDesk.AppServices;
using GlassDesk.Core.Contracts;
using GlassDesk.Core.Models;
using GlassDesk.Core.Storage;
using GlassDesk.Platform.Storage;

namespace GlassDesk.App.Startup;

internal static class Phase5Acceptance
{
	public static int Run(AppPaths realPaths, IReadOnlyList<string> arguments, AcceptanceWorkspace acceptanceWorkspace)
	{
		if (arguments.Count <= 0)
		{
			Path.Combine(Environment.CurrentDirectory, "phase5-acceptance.log");
		}
		else
		{
			Path.GetFullPath(arguments[0]);
		}
		string dataRoot = acceptanceWorkspace.DataRoot;
		AppPaths appPaths = new AppPaths(dataRoot);
		appPaths.EnsureCreated();
		StringBuilder log = new StringBuilder();
		int failed = 0;
		int passed = 0;
		StringBuilder stringBuilder = log;
		StringBuilder stringBuilder2 = stringBuilder;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(20, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("GlassDesk 阶段五命令验收 | ");
		handler.AppendFormatted(DateTimeOffset.Now, "O");
		stringBuilder2.AppendLine(provider, ref handler);
		stringBuilder = log;
		StringBuilder stringBuilder3 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider2 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(3, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("产物：");
		handler.AppendFormatted(typeof(Phase5Acceptance).Assembly.FullName);
		stringBuilder3.AppendLine(provider2, ref handler);
		stringBuilder = log;
		StringBuilder stringBuilder4 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider3 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(5, 2, stringBuilder, invariantCulture);
		handler.AppendLiteral("环境：");
		handler.AppendFormatted(Environment.OSVersion);
		handler.AppendLiteral("; ");
		handler.AppendFormatted(Environment.MachineName);
		stringBuilder4.AppendLine(provider3, ref handler);
		stringBuilder = log;
		StringBuilder stringBuilder5 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider4 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, stringBuilder, invariantCulture);
		handler.AppendLiteral("测试数据：");
		handler.AppendFormatted(dataRoot);
		stringBuilder5.AppendLine(provider4, ref handler);
		log.AppendLine("驱动：真实文件持久化与纯服务；无 WPF 路由、无系统输入注入");
		try
		{
			GlassDeskDataStore glassDeskDataStore = new GlassDeskDataStore(appPaths);
			glassDeskDataStore.LoadAll();
			for (int i = 0; i < 500; i++)
			{
				AppLibraryStore library = glassDeskDataStore.Library;
				AppEntry appEntry = new AppEntry();
				appEntry.Id = $"app-{i:000}";
				AppEntry appEntry2 = appEntry;
				appEntry2.DisplayName = i switch
				{
					0 => "启动台", 
					1 => "Visual Studio Code", 
					499 => "终点 工具 499", 
					_ => $"应用 App {i:000}", 
				};
				appEntry.Target = Path.Combine(dataRoot, $"中文 路径 {i:000}");
				appEntry.IsTargetMissing = i % 10 == 0;
				List<string> list;
				if (i != 499)
				{
					list = new List<string>();
				}
				else
				{
					int num = 1;
					list = new List<string>(num);
					CollectionsMarshal.SetCount(list, num);
					CollectionsMarshal.AsSpan(list)[0] = "last-item";
				}
				appEntry.SearchAliases = list;
				library.Add(appEntry);
			}
			glassDeskDataStore.Library.Save();
			using AppServiceHost appServiceHost = new AppServiceHost(glassDeskDataStore);
			LauncherWorkspace launcherWorkspace = new LauncherWorkspace(appServiceHost);
			launcherWorkspace.Reconcile(35);
			int okay;
			if (launcherWorkspace.Layout.Pages.Count == 15)
			{
				List<LauncherPage> pages = launcherWorkspace.Layout.Pages;
				okay = ((pages[pages.Count - 1].Slots.Count == 10) ? 1 : 0);
			}
			else
			{
				okay = 0;
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(8, 2);
			defaultInterpolatedStringHandler.AppendFormatted(launcherWorkspace.Layout.Pages.Count);
			defaultInterpolatedStringHandler.AppendLiteral(" 页，末页 ");
			List<LauncherPage> pages2 = launcherWorkspace.Layout.Pages;
			defaultInterpolatedStringHandler.AppendFormatted(pages2[pages2.Count - 1].Slots.Count);
			defaultInterpolatedStringHandler.AppendLiteral(" 条");
			Check("500 条默认分页", "L-07/L-08", (byte)okay != 0, "15 页，末页 10 条", defaultInterpolatedStringHandler.ToStringAndClear());
			DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(glassDeskDataStore.Layouts.LauncherFilePath);
			byte[] array = File.ReadAllBytes(glassDeskDataStore.Layouts.LauncherFilePath);
			Stopwatch stopwatch = Stopwatch.StartNew();
			appServiceHost.Search.SearchApps("last-item", 500);
			stopwatch.Stop();
			Stopwatch stopwatch2 = Stopwatch.StartNew();
			for (int j = 0; j < 100; j++)
			{
				appServiceHost.Search.SearchApps("last-item", 500);
			}
			stopwatch2.Stop();
			string text = appServiceHost.Search.SearchApps("last-item", 500).SingleOrDefault()?.Item.Id;
			Check("最后一条可搜索", "L-01-L-06", text == "app-499", "app-499", text ?? "无结果");
			Check("全拼与首字母", "L-03/L-04", appServiceHost.Search.SearchApps("qidongtai", 500).Any((SearchHit hit) => hit.Item.Id == "app-000") && appServiceHost.Search.SearchApps("qdt", 500).Any((SearchHit hit) => hit.Item.Id == "app-000"), "启动台两种拼音均命中", "查询已执行");
			Check("搜索不写布局", "R-08", lastWriteTimeUtc == File.GetLastWriteTimeUtc(glassDeskDataStore.Layouts.LauncherFilePath) && array.SequenceEqual(File.ReadAllBytes(glassDeskDataStore.Layouts.LauncherFilePath)), "字节与修改时间不变", "已核对");
			stringBuilder = log;
			StringBuilder stringBuilder6 = stringBuilder;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider5 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(71, 4, stringBuilder, invariantCulture);
			handler.AppendLiteral("性能样本：500 条；本机 ");
			handler.AppendFormatted(Environment.MachineName);
			handler.AppendLiteral("；首次冷态 ");
			handler.AppendFormatted(stopwatch.Elapsed.TotalMilliseconds, "F2");
			handler.AppendLiteral(" ms；100 次暖态全库搜索总耗时 ");
			handler.AppendFormatted(stopwatch2.Elapsed.TotalMilliseconds, "F2");
			handler.AppendLiteral(" ms，平均 ");
			handler.AppendFormatted(stopwatch2.Elapsed.TotalMilliseconds / 100.0, "F3");
			handler.AppendLiteral(" ms/次。包含匹配排序，不含进程启动与磁盘读取。");
			stringBuilder6.AppendLine(provider5, ref handler);
			LauncherEditSession launcherEditSession = launcherWorkspace.BeginEdit();
			launcherEditSession.Draft = LauncherLayoutEditor.CreateGroup(launcherEditSession.Draft, "app-000", "app-001", "group-a", 35);
			launcherEditSession.Draft = LauncherLayoutEditor.CreateGroup(launcherEditSession.Draft, "app-002", "app-003", "group-b", 35);
			launcherEditSession.Draft = LauncherLayoutEditor.Move(launcherEditSession.Draft, "app-499", 0, 2, 35);
			bool flag = launcherWorkspace.Commit(launcherEditSession);
			List<LauncherGroup> list2 = (from slot in launcherWorkspace.Layout.Pages.SelectMany((LauncherPage page) => page.Slots)
				where slot.Group != null
				select slot.Group).ToList();
			LauncherGroup launcherGroup = list2.SingleOrDefault((LauncherGroup group) => group.Id == "group-a");
			LauncherGroup launcherGroup2 = list2.SingleOrDefault((LauncherGroup group) => group.Id == "group-b");
			bool flag2 = launcherWorkspace.Layout.Pages.SelectMany((LauncherPage page) => page.Slots).Select((LauncherSlot slot) =>
			{
				string text2 = slot.ItemId;
				if (text2 == null)
				{
					LauncherGroup? launcherGroup3 = slot.Group;
					if (launcherGroup3 == null)
					{
						return (string)null;
					}
					text2 = launcherGroup3.Id;
				}
				return text2;
			}).ToList()
				.IndexOf("app-499") == 2;
			bool flag3 = list2.Count == 2 && launcherGroup != null && launcherGroup.ItemIds.SequenceEqual(new _003C_003Ez__ReadOnlyArray<string>(new string[2] { "app-001", "app-000" })) && (launcherGroup2?.ItemIds.SequenceEqual(new _003C_003Ez__ReadOnlyArray<string>(new string[2] { "app-003", "app-002" })) ?? false);
			Check("多分组与跨页排序提交", "L-09/L-11-L-16", flag & flag3 & flag2, "两个单层分组成员及顶层顺序正确", $"已提交={flag}；分组数={list2.Count}；末项新索引={launcherWorkspace.Layout.Pages.SelectMany((LauncherPage page) => page.Slots).Select((LauncherSlot slot) =>
			{
				string text2 = slot.ItemId;
				if (text2 == null)
				{
					LauncherGroup? launcherGroup3 = slot.Group;
					if (launcherGroup3 == null)
					{
						return (string)null;
					}
					text2 = launcherGroup3.Id;
				}
				return text2;
			}).ToList().IndexOf("app-499")}");
			IReadOnlyList<string> readOnlyList = LauncherLayoutRules.AppIds(launcherWorkspace.Layout);
			Check("整理无重复无丢失", "L-09/L-11/L-16", readOnlyList.Count == 500 && readOnlyList.Distinct().Count() == 500, "500 个唯一 Id", $"{readOnlyList.Count} 个，唯一 {readOnlyList.Distinct().Count()} 个");
			GlassDeskDataStore glassDeskDataStore2 = new GlassDeskDataStore(appPaths);
			glassDeskDataStore2.LoadAll();
			Check("新存储实例重载布局", "L-09/L-11/L-13", LauncherLayoutRules.AppIds(glassDeskDataStore2.Layouts.Launcher).SequenceEqual(readOnlyList), "顺序一致", "新实例重新读盘核对");
			byte[] array2 = File.ReadAllBytes(glassDeskDataStore.Layouts.LauncherFilePath);
			LauncherEditSession launcherEditSession2 = launcherWorkspace.BeginEdit();
			launcherEditSession2.Draft = LauncherLayoutEditor.Dissolve(launcherEditSession2.Draft, "group-a", 35);
			Check("取消预览不写盘", "R-08", array2.SequenceEqual(File.ReadAllBytes(glassDeskDataStore.Layouts.LauncherFilePath)), "字节不变", "未调用提交");
			launcherWorkspace.Reconcile(20);
			Check("改网格后不丢成员", "L-07", LauncherLayoutRules.AppIds(launcherWorkspace.Layout).Count == 500, "500 个成员", $"{LauncherLayoutRules.AppIds(launcherWorkspace.Layout).Count} 个成员");
		}
		catch (Exception ex)
		{
			failed++;
			log.AppendLine("[Failed] runner | L-01-L-25 | 预期：无异常 | 实际：" + ex);
		}
		stringBuilder = log;
		StringBuilder stringBuilder7 = stringBuilder;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider6 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(18, 2, stringBuilder, invariantCulture);
		handler.AppendLiteral("合计：Passed=");
		handler.AppendFormatted(passed);
		handler.AppendLiteral(" Failed=");
		handler.AppendFormatted(failed);
		stringBuilder7.AppendLine(provider6, ref handler);
		AcceptanceEntryPoint.WriteText(acceptanceWorkspace.GetOutput("report"), log.ToString());
		ReportWriter.Write(log.ToString(), null);
		return (failed != 0) ? 1 : 0;
		void Check(string name, string ids, bool flag4, string expected, string actual)
		{
			if (flag4)
			{
				passed++;
			}
			else
			{
				failed++;
			}
			StringBuilder stringBuilder8 = log;
			IFormatProvider invariantCulture2 = CultureInfo.InvariantCulture;
			StringBuilder.AppendInterpolatedStringHandler handler2 = new StringBuilder.AppendInterpolatedStringHandler(21, 6, stringBuilder8, invariantCulture2);
			handler2.AppendLiteral("[");
			handler2.AppendFormatted(flag4 ? "Passed" : "Failed");
			handler2.AppendLiteral("] ");
			handler2.AppendFormatted(name);
			handler2.AppendLiteral(" | ");
			handler2.AppendFormatted(ids);
			handler2.AppendLiteral(" | 预期：");
			handler2.AppendFormatted(expected);
			handler2.AppendLiteral(" | 实际：");
			handler2.AppendFormatted(actual);
			handler2.AppendLiteral(" | ");
			handler2.AppendFormatted(DateTimeOffset.Now, "O");
			stringBuilder8.AppendLine(invariantCulture2, ref handler2);
		}
	}
}
