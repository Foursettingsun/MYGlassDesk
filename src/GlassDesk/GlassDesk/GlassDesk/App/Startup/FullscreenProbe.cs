using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using GlassDesk.Platform;
using GlassDesk.Platform.Displays;
using GlassDesk.Platform.Shell;

namespace GlassDesk.App.Startup;

internal static class FullscreenProbe
{
	private sealed class ChangeCounter
	{
		private readonly List<bool> _values = new List<bool>();

		public void Record(bool value)
		{
			lock (_values)
			{
				_values.Add(value);
			}
		}

		public bool[] Snapshot()
		{
			lock (_values)
			{
				return _values.ToArray();
			}
		}
	}

	private const int TimingIterations = 50;

	private const int CheapTimingIterations = 200;

	private const int FullscreenHoldMilliseconds = 500;

	public static int Run(StringBuilder report, IReadOnlyList<DisplayInfo> displays)
	{
		ArgumentNullException.ThrowIfNull(report, "report");
		ArgumentNullException.ThrowIfNull(displays, "displays");
		CultureInfo invariantCulture = CultureInfo.InvariantCulture;
		int num = 0;
		report.AppendLine("【全屏检测】");
		report.AppendLine("  路径一：前台窗口矩形与所在显示器边界比较（GetWindowRect，并用 DWM 可见边框修正）。");
		report.AppendLine("  路径二：SHQueryUserNotificationState，即 Windows 判断「要不要弹通知」时用的那套。");
		StringBuilder stringBuilder = report;
		StringBuilder stringBuilder2 = stringBuilder;
		IFormatProvider formatProvider = invariantCulture;
		IFormatProvider provider = formatProvider;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder, formatProvider);
		handler.AppendLiteral("  任务栏自动隐藏：");
		handler.AppendFormatted(FullscreenDetector.IsTaskbarAutoHidden() ? "是——此时最大化窗口与全屏在几何上无法区分" : "否");
		stringBuilder2.AppendLine(provider, ref handler);
		report.AppendLine();
		FullscreenSnapshot snapshot = FullscreenDetector.Probe();
		AppendSnapshot(report, "当前实际前台窗口", snapshot);
		report.AppendLine();
		report.AppendLine("  · 查询耗时");
		double[] sorted = Sample(50, () => FullscreenDetector.Probe().WindowPath.Duration);
		double[] array = Sample(50, () => FullscreenDetector.Probe().ShellPath.Duration);
		Stopwatch stopwatch = Stopwatch.StartNew();
		for (int num2 = 0; num2 < 200; num2++)
		{
			FullscreenDetector.IsFullscreenActive();
		}
		stopwatch.Stop();
		stringBuilder = report;
		StringBuilder stringBuilder3 = stringBuilder;
		formatProvider = invariantCulture;
		IFormatProvider provider2 = formatProvider;
		handler = new StringBuilder.AppendInterpolatedStringHandler(28, 1, stringBuilder, formatProvider);
		handler.AppendLiteral("      路径一（完整探测，含取类名/标题/进程名）：");
		handler.AppendFormatted(Describe(sorted));
		stringBuilder3.AppendLine(provider2, ref handler);
		stringBuilder = report;
		StringBuilder stringBuilder4 = stringBuilder;
		formatProvider = invariantCulture;
		IFormatProvider provider3 = formatProvider;
		handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder, formatProvider);
		handler.AppendLiteral("      路径二（外壳调用）：");
		handler.AppendFormatted(Describe(array));
		stringBuilder4.AppendLine(provider3, ref handler);
		stringBuilder = report;
		StringBuilder stringBuilder5 = stringBuilder;
		formatProvider = invariantCulture;
		IFormatProvider provider4 = formatProvider;
		handler = new StringBuilder.AppendInterpolatedStringHandler(55, 2, stringBuilder, formatProvider);
		handler.AppendLiteral("      廉价查询 IsFullscreenActive（只走路径一，不做描述性查询）：");
		handler.AppendLiteral("平均 ");
		handler.AppendFormatted(stopwatch.Elapsed.TotalMicroseconds / 200.0, "F2");
		handler.AppendLiteral(" 微秒（");
		handler.AppendFormatted(200);
		handler.AppendLiteral(" 次）");
		stringBuilder5.AppendLine(provider4, ref handler);
		double num3 = stopwatch.Elapsed.TotalMicroseconds / 200.0;
		double num4 = array[array.Length / 2];
		stringBuilder = report;
		StringBuilder stringBuilder6 = stringBuilder;
		formatProvider = invariantCulture;
		IFormatProvider provider5 = formatProvider;
		handler = new StringBuilder.AppendInterpolatedStringHandler(66, 2, stringBuilder, formatProvider);
		handler.AppendLiteral("      → 外壳调用中位数为廉价查询的 ");
		handler.AppendFormatted(num4 / num3, "F1");
		handler.AppendLiteral(" 倍（最大 ");
		handler.AppendFormatted(array[^1], "F0");
		handler.AppendLiteral(" 微秒，且随状态波动）；因此触发路径只用路径一，外壳状态留给诊断与低频确认。");
		stringBuilder6.AppendLine(provider5, ref handler);
		report.AppendLine("        另一条理由在下一段实测里：外壳那条路径看不到副屏上的全屏窗口。");
		report.AppendLine();
		num += RunLiveFullscreenTest(report, displays);
		report.AppendLine();
		AppendKnownBoundaries(report);
		report.AppendLine();
		return num;
	}

	private static void AppendKnownBoundaries(StringBuilder report)
	{
		report.AppendLine("  · 已知边界（本机实测，完整数据见 tmp/fullscreen-probe 的输出）");
		report.AppendLine("      前提：进程必须声明 PerMonitorV2。缺清单时 GetWindowRect 返回虚拟坐标、DWM 可见边框");
		report.AppendLine("            返回物理像素，两者混用会让副屏全屏被误判成非全屏（实测过）。");
		report.AppendLine("      ✓ 无边框铺满主屏：两条路径一致判为全屏");
		report.AppendLine("      ✓ 无边框 + 最大化：两条路径一致判为全屏");
		report.AppendLine("      ✓ 普通窗口最大化：可见边框停在工作区（1368 < 1440），两条路径一致判为非全屏");
		report.AppendLine("      ✓ 带边框窗口被放大到整屏：GetWindowRect 恰好等于整屏，可见边框 (9,0 2542×1431) 小一圈，");
		report.AppendLine("        判为非全屏。DWM 可见边框的修正正是为这一条而加——只看 GetWindowRect 会误判成全屏。");
		report.AppendLine("      ✓ 同一窗口内切换全屏（F11 式，前台窗口不变）：靠位置变化事件，两次翻转都收到");
		report.AppendLine("      ✓ 全屏窗口最小化：判定立刻解除");
		report.AppendLine("      ✗ 副屏上的全屏窗口：路径一判为全屏，路径二漏检（报非全屏），两者不一致");
		report.AppendLine("      ✗ 后台有全屏窗口、前台是别的小窗口：不抑制。检测只看前台窗口，这是取舍");
		report.AppendLine("      ⚠ 任务栏自动隐藏时：最大化窗口与全屏在几何上无法区分，任何基于矩形的判定都分不开");
		report.AppendLine("      ⚠ 独占全屏（D3D）的游戏：本机无法造出，未实测；路径二有专门状态可识别");
		report.AppendLine("      ⚠ 路径二自身不稳定：实测耗时随状态在 1.9 微秒到 2.5 毫秒之间波动，且见过「非全屏窗口在前台却报 Busy」");
	}

	private static int RunLiveFullscreenTest(StringBuilder report, IReadOnlyList<DisplayInfo> displays)
	{
		CultureInfo invariantCulture = CultureInfo.InvariantCulture;
		int num = 0;
		DisplayInfo displayInfo = displays.FirstOrDefault((DisplayInfo d) => d.IsPrimary) ?? ((displays.Count > 0) ? displays[0] : null);
		if ((object)displayInfo == null)
		{
			report.AppendLine("  · 实测全屏窗口：跳过——没有可用于铺满的显示器。");
			return 1;
		}
		report.AppendLine("  · 实测全屏窗口（无边框、置顶、铺满主屏；会短暂全屏显示，属预期）");
		ChangeCounter changeCounter = new ChangeCounter();
		using FullscreenWatcher fullscreenWatcher = FullscreenWatcher.TryStart(changeCounter.Record, new FullscreenOptions
		{
			ExcludeCurrentProcess = false
		});
		ChangeCounter changeCounter2 = new ChangeCounter();
		using (FullscreenWatcher.TryStart(changeCounter2.Record))
		{
			StringBuilder stringBuilder = report;
			StringBuilder stringBuilder2 = stringBuilder;
			IFormatProvider formatProvider = invariantCulture;
			IFormatProvider provider = formatProvider;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder, formatProvider);
			handler.AppendLiteral("      事件钩子：");
			handler.AppendFormatted((fullscreenWatcher == null) ? "✗ 安装失败（SetWinEventHook 返回 0）" : "✓ 已安装（EVENT_SYSTEM_FOREGROUND + EVENT_OBJECT_LOCATIONCHANGE）");
			stringBuilder2.AppendLine(provider, ref handler);
			if (fullscreenWatcher == null)
			{
				report.AppendLine("        钩子不可用时按需查询仍然有效，因此不计为失败。");
			}
			Window window = new Window
			{
				WindowStyle = WindowStyle.None,
				ResizeMode = ResizeMode.NoResize,
				ShowInTaskbar = false,
				Topmost = true,
				WindowStartupLocation = WindowStartupLocation.Manual,
				Background = new SolidColorBrush(Color.FromRgb(18, 18, 24)),
				Content = new TextBlock
				{
					Text = "GlassDesk 全屏检测实测中（约 1 秒后自动退出）",
					Foreground = Brushes.White,
					FontSize = 20.0,
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center
				}
			};
			try
			{
				window.Show();
				nint handle = new WindowInteropHelper(window).Handle;
				if (handle == 0)
				{
					report.AppendLine("      ✗ 探测窗口没有取得句柄，未能实测。");
					return 1;
				}
				DisplayService.MoveWindowToPixelRect(handle, displayInfo.Bounds);
				PumpFor(60);
				DisplayService.MoveWindowToPixelRect(handle, displayInfo.Bounds);
				window.Activate();
				WindowActivation.TryBringToForeground(handle);
				PumpFor(500);
				PixelRect? windowPixelBounds = DisplayService.GetWindowPixelBounds(handle);
				FullscreenSnapshot fullscreenSnapshot = FullscreenDetector.Probe();
				FullscreenSnapshot fullscreenSnapshot2 = FullscreenDetector.Probe(new FullscreenOptions
				{
					ExcludeCurrentProcess = false
				});
				bool flag = fullscreenSnapshot.Foreground.Handle == handle;
				stringBuilder = report;
				StringBuilder stringBuilder3 = stringBuilder;
				formatProvider = invariantCulture;
				IFormatProvider provider2 = formatProvider;
				handler = new StringBuilder.AppendInterpolatedStringHandler(29, 3, stringBuilder, formatProvider);
				handler.AppendLiteral("      探测窗口：句柄 0x");
				handler.AppendFormatted(handle, "X");
				handler.AppendLiteral("，摆放结果 ");
				handler.AppendFormatted(windowPixelBounds);
				handler.AppendLiteral("（目标主屏 ");
				handler.AppendFormatted(displayInfo.Bounds);
				handler.AppendLiteral("）");
				stringBuilder3.AppendLine(provider2, ref handler);
				bool flag2 = flag;
				if (!flag2)
				{
					stringBuilder = report;
					StringBuilder stringBuilder4 = stringBuilder;
					formatProvider = invariantCulture;
					IFormatProvider provider3 = formatProvider;
					handler = new StringBuilder.AppendInterpolatedStringHandler(29, 1, stringBuilder, formatProvider);
					handler.AppendLiteral("      ⚠ 没能把探测窗口切到前台（当前前台是「");
					handler.AppendFormatted(fullscreenSnapshot.Foreground.ProcessName);
					handler.AppendLiteral("」）。");
					stringBuilder4.AppendLine(provider3, ref handler);
					report.AppendLine("        **本次全屏检测的阳性用例未生效，以下三项标记为「未验证」，不计入失败。**");
					report.AppendLine("        它们只说明代码没有崩，不能作为检测正确性的证据。");
					report.AppendLine("        从脚本或后台启动时 Windows 会拒绝前台切换，属可预期的情形；");
					report.AppendLine("        要以人工方式复测，请在交互式会话里直接运行 --diagnose。");
				}
				int num2 = 0;
				stringBuilder = report;
				StringBuilder stringBuilder5 = stringBuilder;
				formatProvider = invariantCulture;
				IFormatProvider provider4 = formatProvider;
				handler = new StringBuilder.AppendInterpolatedStringHandler(34, 2, stringBuilder, formatProvider);
				handler.AppendLiteral("      排除自身时：路径一 = ");
				handler.AppendFormatted(DescribeVerdict(fullscreenSnapshot.WindowPath.Verdict));
				handler.AppendLiteral("（期望「已排除」），路径二 = ");
				handler.AppendFormatted(fullscreenSnapshot.ShellPath);
				stringBuilder5.AppendLine(provider4, ref handler);
				if (!flag2)
				{
					num2++;
					report.AppendLine("      ○ 未验证：自身窗口排除（需要探测窗口真正在前台）");
				}
				else if (fullscreenSnapshot.WindowPath.Verdict != FullscreenVerdict.Excluded)
				{
					num++;
					report.AppendLine("      ✗ 本产品自己的全屏窗口没有被排除。这会让 Dock 把自己判成全屏应用，从此不再响应唤出。");
				}
				else
				{
					report.AppendLine("      ✓ 自身窗口被正确排除：Dock 不会因为自己的窗口而自我抑制。");
				}
				stringBuilder = report;
				StringBuilder stringBuilder6 = stringBuilder;
				formatProvider = invariantCulture;
				IFormatProvider provider5 = formatProvider;
				handler = new StringBuilder.AppendInterpolatedStringHandler(34, 2, stringBuilder, formatProvider);
				handler.AppendLiteral("      不排除自身时：路径一 = ");
				handler.AppendFormatted(DescribeVerdict(fullscreenSnapshot2.WindowPath.Verdict));
				handler.AppendLiteral("（期望「全屏」），路径二 = ");
				handler.AppendFormatted(fullscreenSnapshot2.ShellPath);
				stringBuilder6.AppendLine(provider5, ref handler);
				if (!flag2)
				{
					num2++;
					report.AppendLine("      ○ 未验证：路径一把铺满窗口判为全屏");
				}
				else if (fullscreenSnapshot2.WindowPath.Verdict != FullscreenVerdict.Fullscreen)
				{
					num++;
					report.AppendLine("      ✗ 路径一没有把这个窗口判成全屏，说明铺满判定有问题。");
				}
				else if (!fullscreenSnapshot2.ShellPath.IsFullscreen)
				{
					report.AppendLine("      ⚠ 路径二没有报告全屏（它不把「无边框窗口铺满整屏」算作全屏）。这是两条路径的已知差异，");
					report.AppendLine("        产品对此取「或」，因此不会漏掉这种形态。副屏上的全屏窗口路径二也看不到——");
					report.AppendLine("        见 tmp/fullscreen-probe 的跨进程实测：路径一说全屏、路径二说非全屏。");
				}
				else
				{
					report.AppendLine("      ✓ 两条路径都判为全屏。");
				}
				bool[] array = changeCounter.Snapshot();
				bool[] array2 = changeCounter2.Snapshot();
				if (fullscreenWatcher != null)
				{
					string value = ((array.Length == 0) ? string.Empty : ("（" + string.Join(" → ", array.Select((bool v) => (!v) ? "非全屏" : "全屏")) + "）"));
					stringBuilder = report;
					StringBuilder stringBuilder7 = stringBuilder;
					formatProvider = invariantCulture;
					IFormatProvider provider6 = formatProvider;
					handler = new StringBuilder.AppendInterpolatedStringHandler(19, 2, stringBuilder, formatProvider);
					handler.AppendLiteral("      事件钩子收到判定变化：");
					handler.AppendFormatted(array.Length);
					handler.AppendLiteral(" 次");
					handler.AppendFormatted(value);
					stringBuilder7.AppendLine(provider6, ref handler);
					if (array.Length == 0 && !flag2)
					{
						num2++;
						report.AppendLine("      ○ 未验证：事件钩子收到判定变化（窗口没能成为前台，本就不该有变化）");
					}
					else if (array.Length == 0)
					{
						num++;
						report.AppendLine("      ✗ 钩子装了但没收到任何变化，事件链路有问题（判定最多滞后 40 毫秒的事件合并窗口）。");
					}
					else if (array[0])
					{
						report.AppendLine("      ✓ 事件驱动链路有效：没有轮询，全靠 SetWinEventHook 触发的重算。");
					}
				}
				stringBuilder = report;
				StringBuilder stringBuilder8 = stringBuilder;
				formatProvider = invariantCulture;
				IFormatProvider provider7 = formatProvider;
				handler = new StringBuilder.AppendInterpolatedStringHandler(39, 1, stringBuilder, formatProvider);
				handler.AppendLiteral("      排除自身的监视器收到变化：");
				handler.AppendFormatted(array2.Length);
				handler.AppendLiteral(" 次（期望 0——自身窗口不该改变判定）");
				stringBuilder8.AppendLine(provider7, ref handler);
				if (array2.Length != 0)
				{
					num++;
					report.AppendLine("      ✗ 自身窗口触发了判定变化，与「排除自身」的约定矛盾。");
				}
				if (num2 > 0)
				{
					stringBuilder = report;
					StringBuilder stringBuilder9 = stringBuilder;
					formatProvider = invariantCulture;
					IFormatProvider provider8 = formatProvider;
					handler = new StringBuilder.AppendInterpolatedStringHandler(41, 1, stringBuilder, formatProvider);
					handler.AppendLiteral("      —— 本次有 ");
					handler.AppendFormatted(num2);
					handler.AppendLiteral(" 项未验证（详见上方「未生效」说明），失败计数不含它们。");
					stringBuilder9.AppendLine(provider8, ref handler);
				}
			}
			finally
			{
				window.Close();
			}
			PumpFor(500);
			FullscreenSnapshot fullscreenSnapshot3 = FullscreenDetector.Probe();
			stringBuilder = report;
			StringBuilder stringBuilder10 = stringBuilder;
			formatProvider = invariantCulture;
			IFormatProvider provider9 = formatProvider;
			handler = new StringBuilder.AppendInterpolatedStringHandler(14, 2, stringBuilder, formatProvider);
			handler.AppendLiteral("      关闭后：");
			handler.AppendFormatted(fullscreenSnapshot3.Describe());
			handler.AppendLiteral("；结论：");
			handler.AppendFormatted(fullscreenSnapshot3.ShouldSuppressTriggers ? "仍抑制触发" : "不再抑制");
			stringBuilder10.AppendLine(provider9, ref handler);
			if (fullscreenSnapshot3.WindowPath.Verdict == FullscreenVerdict.Fullscreen)
			{
				num++;
				report.AppendLine("      ✗ 全屏窗口已关闭，路径一仍判为全屏。");
			}
			return num;
		}
	}

	private static void AppendSnapshot(StringBuilder report, string title, FullscreenSnapshot snapshot)
	{
		CultureInfo invariantCulture = CultureInfo.InvariantCulture;
		StringBuilder stringBuilder = report;
		StringBuilder stringBuilder2 = stringBuilder;
		IFormatProvider formatProvider = invariantCulture;
		IFormatProvider provider = formatProvider;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(4, 1, stringBuilder, formatProvider);
		handler.AppendLiteral("  · ");
		handler.AppendFormatted(title);
		stringBuilder2.AppendLine(provider, ref handler);
		stringBuilder = report;
		StringBuilder stringBuilder3 = stringBuilder;
		formatProvider = invariantCulture;
		IFormatProvider provider2 = formatProvider;
		handler = new StringBuilder.AppendInterpolatedStringHandler(21, 4, stringBuilder, formatProvider);
		handler.AppendLiteral("      前台窗口：");
		handler.AppendFormatted(snapshot.Foreground.ProcessName);
		handler.AppendLiteral("（进程 ");
		handler.AppendFormatted(snapshot.Foreground.ProcessId);
		handler.AppendLiteral("）");
		handler.AppendLiteral("  类名 ");
		handler.AppendFormatted(snapshot.Foreground.ClassName);
		handler.AppendFormatted(string.IsNullOrEmpty(snapshot.Foreground.Title) ? string.Empty : ("  标题「" + snapshot.Foreground.Title + "」"));
		stringBuilder3.AppendLine(provider2, ref handler);
		stringBuilder = report;
		StringBuilder stringBuilder4 = stringBuilder;
		formatProvider = invariantCulture;
		IFormatProvider provider3 = formatProvider;
		handler = new StringBuilder.AppendInterpolatedStringHandler(12, 2, stringBuilder, formatProvider);
		handler.AppendLiteral("      路径一：");
		handler.AppendFormatted(DescribeVerdict(snapshot.WindowPath.Verdict));
		handler.AppendLiteral("——");
		handler.AppendFormatted(snapshot.WindowPath.Reason);
		stringBuilder4.AppendLine(provider3, ref handler);
		stringBuilder = report;
		StringBuilder stringBuilder5 = stringBuilder;
		formatProvider = invariantCulture;
		IFormatProvider provider4 = formatProvider;
		handler = new StringBuilder.AppendInterpolatedStringHandler(24, 1, stringBuilder, formatProvider);
		handler.AppendLiteral("        GetWindowRect = ");
		handler.AppendFormatted(Describe(snapshot.WindowPath.WindowRect));
		stringBuilder5.AppendLine(provider4, ref handler);
		stringBuilder = report;
		StringBuilder stringBuilder6 = stringBuilder;
		formatProvider = invariantCulture;
		IFormatProvider provider5 = formatProvider;
		handler = new StringBuilder.AppendInterpolatedStringHandler(19, 1, stringBuilder, formatProvider);
		handler.AppendLiteral("        DWM 可见边框 = ");
		handler.AppendFormatted(Describe(snapshot.WindowPath.VisibleFrame));
		stringBuilder6.AppendLine(provider5, ref handler);
		stringBuilder = report;
		StringBuilder stringBuilder7 = stringBuilder;
		formatProvider = invariantCulture;
		IFormatProvider provider6 = formatProvider;
		handler = new StringBuilder.AppendInterpolatedStringHandler(17, 2, stringBuilder, formatProvider);
		handler.AppendLiteral("        所在显示器 = ");
		handler.AppendFormatted(snapshot.WindowPath.MonitorDeviceName);
		handler.AppendLiteral(" ");
		handler.AppendFormatted(Describe(snapshot.WindowPath.MonitorBounds));
		stringBuilder7.AppendLine(provider6, ref handler);
		stringBuilder = report;
		StringBuilder stringBuilder8 = stringBuilder;
		formatProvider = invariantCulture;
		IFormatProvider provider7 = formatProvider;
		handler = new StringBuilder.AppendInterpolatedStringHandler(12, 3, stringBuilder, formatProvider);
		handler.AppendLiteral("      路径二：");
		handler.AppendFormatted(snapshot.ShellPath.State);
		handler.AppendLiteral("（");
		handler.AppendFormatted(snapshot.ShellPath.IsFullscreen ? "全屏" : "非全屏");
		handler.AppendLiteral("）");
		handler.AppendFormatted((snapshot.ShellPath.Error == null) ? string.Empty : ("——" + snapshot.ShellPath.Error));
		stringBuilder8.AppendLine(provider7, ref handler);
		stringBuilder = report;
		StringBuilder stringBuilder9 = stringBuilder;
		formatProvider = invariantCulture;
		IFormatProvider provider8 = formatProvider;
		handler = new StringBuilder.AppendInterpolatedStringHandler(17, 2, stringBuilder, formatProvider);
		handler.AppendLiteral("      两路一致：");
		handler.AppendFormatted(snapshot.PathsAgree ? "是" : "否");
		handler.AppendLiteral("；抑制触发：");
		handler.AppendFormatted(snapshot.ShouldSuppressTriggers ? "是" : "否");
		stringBuilder9.AppendLine(provider8, ref handler);
	}

	private static double[] Sample(int iterations, Func<TimeSpan> measure)
	{
		double[] array = new double[iterations];
		for (int i = 0; i < iterations; i++)
		{
			array[i] = measure().TotalMicroseconds;
		}
		Array.Sort(array);
		return array;
	}

	private static string Describe(double[] sorted)
	{
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(20, 3, invariantCulture);
		handler.AppendLiteral("最小 ");
		handler.AppendFormatted(sorted[0], "F2");
		handler.AppendLiteral(" 微秒，中位 ");
		handler.AppendFormatted(sorted[sorted.Length / 2], "F2");
		handler.AppendLiteral(" 微秒，最大 ");
		handler.AppendFormatted(sorted[^1], "F2");
		handler.AppendLiteral(" 微秒");
		return string.Create(invariantCulture, ref handler);
	}

	private static string Describe(PixelRect? rect)
	{
		return rect?.ToString() ?? "（不可用）";
	}

	private static string DescribeVerdict(FullscreenVerdict verdict)
	{
		return verdict switch
		{
			FullscreenVerdict.Fullscreen => "全屏", 
			FullscreenVerdict.NotFullscreen => "非全屏", 
			FullscreenVerdict.Excluded => "已排除", 
			_ => "无法判定", 
		};
	}

	private static void PumpFor(int milliseconds)
	{
		Dispatcher currentDispatcher = Dispatcher.CurrentDispatcher;
		DispatcherFrame frame = new DispatcherFrame();
		DispatcherTimer dispatcherTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(milliseconds), DispatcherPriority.Background, (object? _, EventArgs _) =>
		{
			frame.Continue = false;
		}, currentDispatcher);
		dispatcherTimer.Start();
		try
		{
			Dispatcher.PushFrame(frame);
		}
		finally
		{
			dispatcherTimer.Stop();
		}
	}
}
