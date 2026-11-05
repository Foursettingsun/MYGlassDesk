using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using GlassDesk.AppServices;
using GlassDesk.Core.Storage;
using GlassDesk.Platform;
using GlassDesk.Platform.Shell;

namespace GlassDesk.App.Startup;

internal static class Phase4Probe
{
	private static readonly StringBuilder Log = new StringBuilder();

	public static int RunAumid(AppPaths paths)
	{
		ArgumentNullException.ThrowIfNull(paths, "paths");
		Log.Clear();
		Log.AppendLine("阶段 4 探针 A：应用身份（AppUserModelID）");
		Log.AppendLine("==============================================");
		StringBuilder log = Log;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(3, 1, log, invariantCulture);
		handler.AppendLiteral("时间：");
		handler.AppendFormatted(DateTimeOffset.Now, "yyyy-MM-dd HH:mm:ss zzz");
		log.AppendLine(invariantCulture, ref handler);
		Log.AppendLine();
		ProbeShortcutAumids();
		ProbeRunningWindows();
		ProbeLaunchedTargets();
		Log.AppendLine();
		Log.AppendLine("—— 探针 A 结束 ——");
		ReportWriter.Write(Log.ToString(), Path.Combine(paths.LogDirectory, "phase4-probe-aumid.log"));
		return 0;
	}

	public static int RunEvents(AppPaths paths)
	{
		ArgumentNullException.ThrowIfNull(paths, "paths");
		Log.Clear();
		Log.AppendLine("阶段 4 探针 B：窗口事件源（WinEvent）");
		Log.AppendLine("==============================================");
		StringBuilder log = Log;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(3, 1, log, invariantCulture);
		handler.AppendLiteral("时间：");
		handler.AppendFormatted(DateTimeOffset.Now, "yyyy-MM-dd HH:mm:ss zzz");
		log.AppendLine(invariantCulture, ref handler);
		Log.AppendLine();
		RunEventsCore();
		Log.AppendLine();
		Log.AppendLine("—— 探针 B 结束 ——");
		ReportWriter.Write(Log.ToString(), Path.Combine(paths.LogDirectory, "phase4-probe-runevents.log"));
		return 0;
	}

	private static void ProbeShortcutAumids()
	{
		StartMenuScanResult startMenuScanResult = AppServiceHost.ScanStartMenu();
		Log.AppendLine("[A1] 开始菜单 .lnk 的显式身份");
		StringBuilder log = Log;
		StringBuilder stringBuilder = log;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, log, invariantCulture);
		handler.AppendLiteral("  扫描到 ");
		handler.AppendFormatted(startMenuScanResult.Shortcuts.Count);
		handler.AppendLiteral(" 个 .lnk");
		stringBuilder.AppendLine(provider, ref handler);
		List<(string, string, string)> list = new List<(string, string, string)>();
		foreach (StartMenuShortcut shortcut in startMenuScanResult.Shortcuts)
		{
			string text = WindowIdentity.TryGetShortcutAumid(shortcut.Path);
			if (text != null)
			{
				list.Add((shortcut.Name, shortcut.Path, text));
			}
		}
		log = Log;
		StringBuilder stringBuilder2 = log;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider2 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(20, 3, log, invariantCulture);
		handler.AppendLiteral("  带显式 AUMID 的：");
		handler.AppendFormatted(list.Count);
		handler.AppendLiteral(" / ");
		handler.AppendFormatted(startMenuScanResult.Shortcuts.Count);
		handler.AppendLiteral("（");
		handler.AppendFormatted((double)list.Count * 100.0 / (double)Math.Max(1, startMenuScanResult.Shortcuts.Count), "F1");
		handler.AppendLiteral("%）");
		stringBuilder2.AppendLine(provider2, ref handler);
		Log.AppendLine("  样例（最多 12 条，注意打包应用与 UWP 的形态）：");
		foreach (var item4 in list.Take(12))
		{
			string item = item4.Item1;
			string item2 = item4.Item2;
			string item3 = item4.Item3;
			log = Log;
			StringBuilder stringBuilder3 = log;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider3 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(6, 1, log, invariantCulture);
			handler.AppendLiteral("    · ");
			handler.AppendFormatted(item);
			stringBuilder3.AppendLine(provider3, ref handler);
			log = Log;
			StringBuilder stringBuilder4 = log;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider4 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, log, invariantCulture);
			handler.AppendLiteral("      .lnk ");
			handler.AppendFormatted(item2);
			stringBuilder4.AppendLine(provider4, ref handler);
			log = Log;
			StringBuilder stringBuilder5 = log;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider5 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, log, invariantCulture);
			handler.AppendLiteral("      AUMID ");
			handler.AppendFormatted(item3);
			stringBuilder5.AppendLine(provider5, ref handler);
		}
		Log.AppendLine("  三类代表目标的显式身份（没有则写「无」）：");
		ReportIdentityOf(startMenuScanResult, new string[2] { "记事本", "Notepad" });
		ReportIdentityOf(startMenuScanResult, new string[4] { "终端", "Terminal", "命令提示符", "Command" });
		ReportIdentityOf(startMenuScanResult, new string[2] { "计算器", "Calculator" });
		Log.AppendLine();
	}

	private static void ReportIdentityOf(StartMenuScanResult scan, string[] keywords)
	{
		StringBuilder log;
		IFormatProvider invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler;
		foreach (StartMenuShortcut shortcut in scan.Shortcuts)
		{
			if (keywords.Any((string k) => shortcut.Name.Contains(k, StringComparison.OrdinalIgnoreCase)))
			{
				string text = WindowIdentity.TryGetShortcutAumid(shortcut.Path);
				string value = ((text == null) ? "无（快捷方式上没有显式身份）" : text);
				log = Log;
				StringBuilder stringBuilder = log;
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider = invariantCulture;
				handler = new StringBuilder.AppendInterpolatedStringHandler(9, 2, log, invariantCulture);
				handler.AppendLiteral("    · ");
				handler.AppendFormatted(shortcut.Name);
				handler.AppendLiteral(" → ");
				handler.AppendFormatted(value);
				stringBuilder.AppendLine(provider, ref handler);
				return;
			}
		}
		log = Log;
		StringBuilder stringBuilder2 = log;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider2 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(25, 1, log, invariantCulture);
		handler.AppendLiteral("    · （开始菜单里没有名称含 ");
		handler.AppendFormatted(string.Join('/', keywords));
		handler.AppendLiteral(" 的快捷方式）");
		stringBuilder2.AppendLine(provider2, ref handler);
	}

	private static void ProbeRunningWindows()
	{
		Log.AppendLine("[A2] 当前桌面的候选窗口与两侧身份（只读枚举）");
		IReadOnlyList<WindowIdentityInfo> readOnlyList = WindowLocator.EnumerateTopLevelWindows();
		StringBuilder log = Log;
		StringBuilder stringBuilder = log;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, log, invariantCulture);
		handler.AppendLiteral("  候选顶层窗口：");
		handler.AppendFormatted(readOnlyList.Count);
		handler.AppendLiteral(" 个");
		stringBuilder.AppendLine(provider, ref handler);
		foreach (WindowIdentityInfo item in readOnlyList.Take(40))
		{
			string value = item.WindowAumid ?? item.ProcessAumid ?? "（两侧都读不出身份）";
			log = Log;
			StringBuilder stringBuilder2 = log;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider2 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(15, 3, log, invariantCulture);
			handler.AppendLiteral("    · 0x");
			handler.AppendFormatted(item.Handle, "X");
			handler.AppendLiteral(" 「");
			handler.AppendFormatted(Truncate(item.Title, 40));
			handler.AppendLiteral("」pid ");
			handler.AppendFormatted(item.ProcessId);
			stringBuilder2.AppendLine(provider2, ref handler);
			log = Log;
			StringBuilder stringBuilder3 = log;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider3 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, log, invariantCulture);
			handler.AppendLiteral("      进程 ");
			handler.AppendFormatted(item.ImagePath ?? "（读不到）");
			stringBuilder3.AppendLine(provider3, ref handler);
			log = Log;
			StringBuilder stringBuilder4 = log;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider4 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(21, 3, log, invariantCulture);
			handler.AppendLiteral("      窗口身份 ");
			handler.AppendFormatted(item.WindowAumid ?? "无");
			handler.AppendLiteral("｜进程身份 ");
			handler.AppendFormatted(item.ProcessAumid ?? "无");
			handler.AppendLiteral("｜有效 ");
			handler.AppendFormatted(value);
			stringBuilder4.AppendLine(provider4, ref handler);
		}
		Log.AppendLine("  控制台宿主核对：凡窗口归属 WindowsTerminal.exe 的，其身份与");
		Log.AppendLine("  cmd.exe 的映像路径（C:\\Windows\\System32\\cmd.exe）必然不同——这是「控制台程序");
		Log.AppendLine("  不能按映像路径匹配」的实测依据，不再单独启动 cmd 验证。");
		Log.AppendLine();
	}

	private static void ProbeLaunchedTargets()
	{
		Log.AppendLine("[A3] 自启靶子的身份（只启动经典程序；打包应用不启动，证据来自 A2 只读枚举）");
		ProbeOneTarget("经典 Win32 程序（App Paths 注册名）", "charmap.exe", (WindowIdentityInfo entry) =>
		{
			bool flag = entry.WindowAumid == null && entry.ProcessAumid == null && (entry.ImagePath?.EndsWith("charmap.exe", StringComparison.OrdinalIgnoreCase) ?? false);
			StringBuilder log = Log;
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, log, invariantCulture);
			handler.AppendLiteral("  判读：");
			handler.AppendFormatted(flag ? "符合预期（身份为空、映像路径可匹配）" : "与预期不符，需人工核对");
			log.AppendLine(invariantCulture, ref handler);
		});
		Log.AppendLine("  说明：打包应用（记事本）与 UWP 的身份形态见 A2 的只读枚举——");
		Log.AppendLine("  本机实测记事本窗口的进程包身份为 Microsoft.WindowsNotepad_8wekyb3d8bbwe!App，");
		Log.AppendLine("  UWP「设置」窗口的显式身份挂在 ApplicationFrameHost 的框架窗口上。");
		Log.AppendLine("  这样不必为了同一份证据在用户桌面上留一个关不掉的记事本窗口。");
		Log.AppendLine();
	}

	private static void ProbeOneTarget(string label, string executable, Action<WindowIdentityInfo> judge)
	{
		StringBuilder log = Log;
		StringBuilder stringBuilder = log;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(12, 2, log, invariantCulture);
		handler.AppendLiteral("  —— 启动 ");
		handler.AppendFormatted(label);
		handler.AppendLiteral("：");
		handler.AppendFormatted(executable);
		handler.AppendLiteral(" ——");
		stringBuilder.AppendLine(provider, ref handler);
		HashSet<nint> hashSet = (from w in WindowLocator.EnumerateTopLevelWindows()
			select w.Handle).ToHashSet();
		ShellLaunchOutcome shellLaunchOutcome = ShellExecutor.Open(executable);
		if (!shellLaunchOutcome.Started || shellLaunchOutcome.ProcessId == 0)
		{
			log = Log;
			StringBuilder stringBuilder2 = log;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider2 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(18, 1, log, invariantCulture);
			handler.AppendLiteral("  启动失败：错误码 ");
			handler.AppendFormatted(shellLaunchOutcome.ErrorCode);
			handler.AppendLiteral("。跳过这一段。");
			stringBuilder2.AppendLine(provider2, ref handler);
			return;
		}
		log = Log;
		StringBuilder stringBuilder3 = log;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider3 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(13, 1, log, invariantCulture);
		handler.AppendLiteral("  记录的靶子进程 id：");
		handler.AppendFormatted(shellLaunchOutcome.ProcessId);
		stringBuilder3.AppendLine(provider3, ref handler);
		Thread.Sleep(2500);
		WindowIdentityInfo windowIdentityInfo = null;
		foreach (WindowIdentityInfo item in WindowLocator.EnumerateTopLevelWindows())
		{
			if (!hashSet.Contains(item.Handle))
			{
				windowIdentityInfo = item;
				break;
			}
		}
		if ((object)windowIdentityInfo == null)
		{
			Log.AppendLine("  启动前后没有出现新窗口（可能复用了已有实例或宿主窗口）。");
		}
		else
		{
			log = Log;
			StringBuilder stringBuilder4 = log;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider4 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(15, 3, log, invariantCulture);
			handler.AppendLiteral("  新窗口 0x");
			handler.AppendFormatted(windowIdentityInfo.Handle, "X");
			handler.AppendLiteral(" 「");
			handler.AppendFormatted(windowIdentityInfo.Title);
			handler.AppendLiteral("」pid ");
			handler.AppendFormatted(windowIdentityInfo.ProcessId);
			stringBuilder4.AppendLine(provider4, ref handler);
			log = Log;
			StringBuilder stringBuilder5 = log;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider5 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, log, invariantCulture);
			handler.AppendLiteral("    映像路径 ");
			handler.AppendFormatted(windowIdentityInfo.ImagePath ?? "（读不到）");
			stringBuilder5.AppendLine(provider5, ref handler);
			log = Log;
			StringBuilder stringBuilder6 = log;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider6 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(15, 2, log, invariantCulture);
			handler.AppendLiteral("    窗口身份 ");
			handler.AppendFormatted(windowIdentityInfo.WindowAumid ?? "无");
			handler.AppendLiteral("｜进程身份 ");
			handler.AppendFormatted(windowIdentityInfo.ProcessAumid ?? "无");
			stringBuilder6.AppendLine(provider6, ref handler);
			judge(windowIdentityInfo);
		}
		KillSpawnedTarget(executable, shellLaunchOutcome.ProcessId, windowIdentityInfo?.Handle ?? 0);
	}

	private static void KillSpawnedTarget(string executable, uint processId, nint newWindowHandle)
	{
		try
		{
			using Process process = Process.GetProcessById((int)processId);
			if (!process.HasExited)
			{
				process.Kill(entireProcessTree: false);
				StringBuilder log = Log;
				StringBuilder stringBuilder = log;
				IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider = invariantCulture;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(16, 2, log, invariantCulture);
				handler.AppendLiteral("  已结束记录的靶子进程 ");
				handler.AppendFormatted(processId);
				handler.AppendLiteral("（");
				handler.AppendFormatted(executable);
				handler.AppendLiteral("）。");
				stringBuilder.AppendLine(provider, ref handler);
			}
		}
		catch (Exception ex) when ((ex is ArgumentException || ex is InvalidOperationException || ex is Win32Exception) ? true : false)
		{
			StringBuilder log = Log;
			StringBuilder stringBuilder2 = log;
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider2 = invariantCulture;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(17, 2, log, invariantCulture);
			handler.AppendLiteral("  记录的靶子进程 ");
			handler.AppendFormatted(processId);
			handler.AppendLiteral(" 已不在（");
			handler.AppendFormatted(ex.GetType().Name);
			handler.AppendLiteral("）。");
			stringBuilder2.AppendLine(provider2, ref handler);
		}
		if (WindowControl.IsAlive(newWindowHandle))
		{
			WindowControl.Close(newWindowHandle);
			Thread.Sleep(500);
			if (WindowControl.IsAlive(newWindowHandle))
			{
				StringBuilder log = Log;
				StringBuilder stringBuilder3 = log;
				IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider3 = invariantCulture;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(29, 1, log, invariantCulture);
				handler.AppendLiteral("  新增窗口 0x");
				handler.AppendFormatted(newWindowHandle, "X");
				handler.AppendLiteral(" 仍在（可能有保存提示），留给人工核对。");
				stringBuilder3.AppendLine(provider3, ref handler);
			}
			else
			{
				StringBuilder log = Log;
				StringBuilder stringBuilder4 = log;
				IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider4 = invariantCulture;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, log, invariantCulture);
				handler.AppendLiteral("  新增窗口 0x");
				handler.AppendFormatted(newWindowHandle, "X");
				handler.AppendLiteral(" 已关闭。");
				stringBuilder4.AppendLine(provider4, ref handler);
			}
		}
	}

	private static void RunEventsCore()
	{
		List<string> events = new List<string>();
		int reconcileCount = 0;
		WindowEventWatcher windowEventWatcher = WindowEventWatcher.TryStart(() =>
		{
			reconcileCount++;
		}, (uint eventType, nint window, int _, int _) =>
		{
			events.Add($"{DescribeEvent(eventType),-22} 0x{window:X} 「{WindowControl.GetTitle(window)}」");
		});
		if (windowEventWatcher == null)
		{
			Log.AppendLine("✗ 事件源（WinEvent 钩子）安装失败。结论：这台机器上运行状态必须退回按需枚举。");
			return;
		}
		using (windowEventWatcher)
		{
			StringBuilder log = Log;
			StringBuilder stringBuilder = log;
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider = invariantCulture;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(22, 1, log, invariantCulture);
			handler.AppendLiteral("[B1] 事件源安装成功，回调线程 id ");
			handler.AppendFormatted(windowEventWatcher.HookThreadId);
			handler.AppendLiteral("。");
			stringBuilder.AppendLine(provider, ref handler);
			IReadOnlyList<WindowIdentityInfo> readOnlyList = WindowLocator.EnumerateTopLevelWindows();
			log = Log;
			StringBuilder stringBuilder2 = log;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider2 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(32, 1, log, invariantCulture);
			handler.AppendLiteral("[B2] 启动前已有候选窗口 ");
			handler.AppendFormatted(readOnlyList.Count);
			handler.AppendLiteral(" 个——运行状态必须先做初始枚举，");
			stringBuilder2.AppendLine(provider2, ref handler);
			Log.AppendLine("     否则 Dock 启动之前就在运行的窗口会全部漏掉。");
			HashSet<nint> before = readOnlyList.Select((WindowIdentityInfo w) => w.Handle).ToHashSet();
			ShellLaunchOutcome shellLaunchOutcome = ShellExecutor.Open("C:\\Windows\\System32\\charmap.exe");
			if (!shellLaunchOutcome.Started || shellLaunchOutcome.ProcessId == 0)
			{
				log = Log;
				StringBuilder stringBuilder3 = log;
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider3 = invariantCulture;
				handler = new StringBuilder.AppendInterpolatedStringHandler(25, 1, log, invariantCulture);
				handler.AppendLiteral("[B3] 靶子启动失败（错误码 ");
				handler.AppendFormatted(shellLaunchOutcome.ErrorCode);
				handler.AppendLiteral("），后续动作跳过。");
				stringBuilder3.AppendLine(provider3, ref handler);
				return;
			}
			log = Log;
			StringBuilder stringBuilder4 = log;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider4 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(34, 1, log, invariantCulture);
			handler.AppendLiteral("[B3] 靶子 charmap 已启动，进程 id ");
			handler.AppendFormatted(shellLaunchOutcome.ProcessId);
			handler.AppendLiteral("，等窗口出现……");
			stringBuilder4.AppendLine(provider4, ref handler);
			WaitVisible(() => WindowLocator.EnumerateTopLevelWindows().Any((WindowIdentityInfo w) => !before.Contains(w.Handle)));
			WindowIdentityInfo target = WindowLocator.EnumerateTopLevelWindows().FirstOrDefault((WindowIdentityInfo w) => !before.Contains(w.Handle));
			if ((object)target == null)
			{
				Log.AppendLine("  没等到新窗口，事件线只对启动阶段有意义。");
			}
			else
			{
				log = Log;
				StringBuilder stringBuilder5 = log;
				invariantCulture = CultureInfo.InvariantCulture;
				IFormatProvider provider5 = invariantCulture;
				handler = new StringBuilder.AppendInterpolatedStringHandler(13, 2, log, invariantCulture);
				handler.AppendLiteral("  靶子窗口 0x");
				handler.AppendFormatted(target.Handle, "X");
				handler.AppendLiteral(" 「");
				handler.AppendFormatted(target.Title);
				handler.AppendLiteral("」。");
				stringBuilder5.AppendLine(provider5, ref handler);
				Step("最小化", () =>
				{
					WindowControl.Minimize(target.Handle);
				});
				Step("还原", () =>
				{
					WindowControl.Restore(target.Handle);
				});
				Step("隐藏（SW_HIDE）", () =>
				{
					WindowControl.Hide(target.Handle);
				});
				Step("重新显示（SW_SHOW）", () =>
				{
					WindowControl.Show(target.Handle);
				});
			}
			int count = events.Count;
			if ((object)target != null)
			{
				WindowControl.Close(target.Handle);
			}
			try
			{
				using Process process = Process.GetProcessById((int)shellLaunchOutcome.ProcessId);
				if (!process.WaitForExit(3000))
				{
					process.Kill(entireProcessTree: false);
					Log.AppendLine("  靶子 3 秒内未退出，已按记录的 pid 结束。");
				}
			}
			catch (Exception ex) when ((ex is ArgumentException || ex is InvalidOperationException || ex is Win32Exception) ? true : false)
			{
				Log.AppendLine("  记录的靶子进程已不在。");
			}
			Thread.Sleep(800);
			log = Log;
			StringBuilder stringBuilder6 = log;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider6 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(22, 1, log, invariantCulture);
			handler.AppendLiteral("[B4] 靶子已关闭；关闭后新增 ");
			handler.AppendFormatted(events.Count - count);
			handler.AppendLiteral(" 条事件。");
			stringBuilder6.AppendLine(provider6, ref handler);
			bool flag = WindowLocator.EnumerateTopLevelWindows().Any((WindowIdentityInfo w) => w.Handle == (target?.Handle ?? 0));
			log = Log;
			StringBuilder stringBuilder7 = log;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider7 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(18, 1, log, invariantCulture);
			handler.AppendLiteral("[B5] 关闭后重新枚举：靶子窗口");
			handler.AppendFormatted(flag ? "仍在（调和会纠正状态点）" : "已不在（事件与枚举一致）");
			handler.AppendLiteral("。");
			stringBuilder7.AppendLine(provider7, ref handler);
			Thread.Sleep(500);
			PrintTimeline(events);
			Log.AppendLine();
			log = Log;
			StringBuilder stringBuilder8 = log;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider8 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(27, 2, log, invariantCulture);
			handler.AppendLiteral("[B6] 共记录 ");
			handler.AppendFormatted(events.Count);
			handler.AppendLiteral(" 条原始事件；合并后的重算触发 ");
			handler.AppendFormatted(reconcileCount);
			handler.AppendLiteral(" 次");
			stringBuilder8.AppendLine(provider8, ref handler);
			Log.AppendLine("     （原始事件远多于重算次数，证明回调只入队、重算被合并——钩子回调里没有任何查询。）");
			PrintByEvent(events);
		}
		void Step(string label, Action action, int settleMilliseconds = 700)
		{
			int count2 = events.Count;
			action();
			Thread.Sleep(settleMilliseconds);
			StringBuilder log2 = Log;
			IFormatProvider invariantCulture2 = CultureInfo.InvariantCulture;
			StringBuilder.AppendInterpolatedStringHandler handler2 = new StringBuilder.AppendInterpolatedStringHandler(15, 2, log2, invariantCulture2);
			handler2.AppendLiteral("[B] ");
			handler2.AppendFormatted(label);
			handler2.AppendLiteral("（此后新增 ");
			handler2.AppendFormatted(events.Count - count2);
			handler2.AppendLiteral(" 条事件）");
			log2.AppendLine(invariantCulture2, ref handler2);
		}
	}

	private static string DescribeEvent(uint eventType)
	{
		return eventType switch
		{
			32768u => "OBJECT_CREATE", 
			32769u => "OBJECT_DESTROY", 
			32770u => "OBJECT_SHOW", 
			32771u => "OBJECT_HIDE", 
			22u => "SYSTEM_MINIMIZESTART", 
			23u => "SYSTEM_MINIMIZEEND", 
			3u => "SYSTEM_FOREGROUND", 
			_ => $"0x{eventType:X}", 
		};
	}

	private static void WaitVisible(Func<bool> condition)
	{
		for (int i = 0; i < 20; i++)
		{
			if (condition())
			{
				break;
			}
			Thread.Sleep(250);
		}
	}

	private static void PrintTimeline(List<string> events)
	{
		Log.AppendLine();
		Log.AppendLine("[B7] 事件时间线（最多 60 条）：");
		foreach (string item in events.Take(60))
		{
			StringBuilder log = Log;
			StringBuilder stringBuilder = log;
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider = invariantCulture;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(2, 1, log, invariantCulture);
			handler.AppendLiteral("  ");
			handler.AppendFormatted(item);
			stringBuilder.AppendLine(provider, ref handler);
		}
		if (events.Count > 60)
		{
			StringBuilder log = Log;
			StringBuilder stringBuilder2 = log;
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider2 = invariantCulture;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, log, invariantCulture);
			handler.AppendLiteral("  ……（其余 ");
			handler.AppendFormatted(events.Count - 60);
			handler.AppendLiteral(" 条略）");
			stringBuilder2.AppendLine(provider2, ref handler);
		}
	}

	private static void PrintByEvent(List<string> events)
	{
		Log.AppendLine();
		Log.AppendLine("[B8] 按事件类型统计：");
		foreach (IGrouping<string, string> item in from e in events
			select e.Split(' ')[0] into e
			group e by e into g
			orderby g.Key
			select g)
		{
			StringBuilder log = Log;
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(5, 2, log, invariantCulture);
			handler.AppendLiteral("  ");
			handler.AppendFormatted(item.Key);
			handler.AppendLiteral("：");
			handler.AppendFormatted(item.Count());
			handler.AppendLiteral(" 条");
			log.AppendLine(invariantCulture, ref handler);
		}
	}

	private static string Truncate(string text, int maxLength)
	{
		if (text.Length > maxLength)
		{
			return text.Substring(0, maxLength) + "…";
		}
		return text;
	}
}
