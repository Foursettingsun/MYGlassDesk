using System;
using System.Collections.Generic;
using System.Linq;

namespace GlassDesk.App.Startup;

public sealed class CommandLineOptions
{
	public bool SelfTest { get; private init; }

	public bool Diagnose { get; private init; }

	public bool ShowLauncher { get; private init; }

	public bool ShowSettings { get; private init; }

	public bool Autostart { get; private init; }

	public AppearanceSample Sample { get; private init; }

	public bool Help { get; private init; }

	public bool Exit { get; private init; }

	public string? AcceptanceDataRoot { get; private init; }

	public string? AcceptanceReport { get; private init; }

	public IReadOnlyList<string> Unknown { get; private init; } = Array.Empty<string>();

	public AppServiceVerb Verb { get; private init; }

	public IReadOnlyList<string> VerbArguments { get; private init; } = Array.Empty<string>();

	public bool IsAppServiceCommand => Verb != AppServiceVerb.None;

	public static string HelpText => "GlassDesk（工作名称）\n\n用法：GlassDesk.exe [选项]\n\n  --selftest         运行环境自检并退出（不显示界面）\n  --selftest --acceptance-data-root <新目录> --acceptance-report <新报告文件>\n                     在隔离数据目录运行自检（构建与发布脚本使用）\n  --diagnose         输出显示与坐标空间诊断后退出\n  --diagnose --acceptance-data-root <新目录> --acceptance-report <新报告文件>\n                     隔离诊断数据和报告（仍会创建诊断窗口，需可用测试桌面）\n  --show-launcher    显示启动台（已在运行则通知现有实例）\n  --show-settings    显示设置中心（已在运行则通知现有实例）\n  --autostart        登录启动（已在运行则直接退出，不唤起界面）\n  --sample-light     以浅色外观启动（用于产出样例，不写回设置）\n  --sample-dark      以深色外观启动\n  --sample-degraded  以不请求系统材质的降级外观启动\n  --exit             通知正在运行的实例退出\n  --help             显示本帮助\n\n  --phase3-acceptance <联系表输出> <报告输出> <新数据目录>  阶段三验收\n  --phase4-acceptance <报告输出> <新数据目录>                  阶段四验收\n  --phase5-acceptance <报告输出> <新数据目录>                  阶段五命令验收\n  --phase6-acceptance <报告输出> <新数据目录>                  阶段六交互与设置验收\n\n应用库命令（阶段 3，无界面）：\n\n  --import <路径或网址>...   导入条目；可以是多个\n  --scan-start-menu          扫描「开始」菜单并列出，不改动应用库\n  --import-start-menu        扫描「开始」菜单并全部导入\n  --list-library             列出应用库内容\n  --check-library            复查失效目标并报告\n  --relocate <条目 id> <新路径>  把一条记录重新定位到新位置\n  --launch <条目 id>         打开一条记录\n  --activate <条目 id>       优先激活已有窗口，没有才启动\n  --verify-icons             用系统图像解码器读一遍缓存图标\n  --icon-contact-sheet [文件]  把缓存图标拼成一张联系表图片\n  --clear-icon-cache         清空图标缓存\n  --make-samples [目录]      造出中文、空格、失效路径等代表性样本\n  --seed-library <数量>      造出指定数量的合成条目\n\n不带选项启动时，程序坞会出现，启动台与设置中心可由托盘菜单打开。";

	public static CommandLineOptions Parse(IReadOnlyList<string> args)
	{
		ArgumentNullException.ThrowIfNull(args, "args");
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		bool flag5 = false;
		bool flag6 = false;
		bool flag7 = false;
		string text = null;
		string text2 = null;
		List<string> list = new List<string>();
		AppearanceSample appearanceSample = AppearanceSample.None;
		AppServiceVerb appServiceVerb = AppServiceVerb.None;
		List<string> list2 = new List<string>();
		for (int i = 0; i < args.Count; i++)
		{
			string text3 = args[i];
			string text4 = text3.Trim().ToLowerInvariant();
			if (TryParseVerb(text4, out var verb))
			{
				if (appServiceVerb != AppServiceVerb.None)
				{
					list.Add(text3);
				}
				else
				{
					appServiceVerb = verb;
				}
				continue;
			}
			if (appServiceVerb != AppServiceVerb.None && !text4.StartsWith("--", StringComparison.Ordinal))
			{
				list2.Add(text3);
				continue;
			}
			switch (text4)
			{
			case "--acceptance-data-root":
				if (i + 1 >= args.Count || args[i + 1].StartsWith("--", StringComparison.Ordinal))
				{
					list.Add(text3);
					text = string.Empty;
				}
				else if (text != null)
				{
					list.Add(text3);
				}
				else
				{
					text = args[++i];
				}
				break;
			case "--acceptance-report":
				if (i + 1 >= args.Count || args[i + 1].StartsWith("--", StringComparison.Ordinal))
				{
					list.Add(text3);
					text2 = string.Empty;
				}
				else if (text2 != null)
				{
					list.Add(text3);
				}
				else
				{
					text2 = args[++i];
				}
				break;
			case "--selftest":
			case "--self-test":
				flag = true;
				break;
			case "--diagnose":
				flag2 = true;
				break;
			case "--sample-light":
				appearanceSample = AppearanceSample.Light;
				break;
			case "--sample-dark":
				appearanceSample = AppearanceSample.Dark;
				break;
			case "--sample-degraded":
				appearanceSample = AppearanceSample.Degraded;
				break;
			case "--show-launcher":
				flag3 = true;
				break;
			case "--show-settings":
				flag4 = true;
				break;
			case "--autostart":
				flag5 = true;
				break;
			case "--exit":
				flag7 = true;
				break;
			case "--help":
			case "-h":
			case "/?":
				flag6 = true;
				break;
			default:
				list.Add(text3);
				break;
			}
		}
		bool flag8 = ((appServiceVerb == AppServiceVerb.Phase3Acceptance || appServiceVerb == AppServiceVerb.Phase4Acceptance || (uint)(appServiceVerb - 20) <= 1u) ? true : false);
		bool flag9 = flag8;
		if (flag5 && ((flag | flag2 | flag3 | flag4 | flag7 | flag6) || appearanceSample != AppearanceSample.None || appServiceVerb != AppServiceVerb.None || text != null || text2 != null || list.Count > 0))
		{
			list.Add("--autostart 必须单独使用，不能与其它参数组合");
		}
		if (((flag | flag9) || text != null || text2 != null) && new bool[8]
		{
			flag,
			flag2,
			flag7,
			flag3,
			flag4,
			flag6,
			appearanceSample != AppearanceSample.None,
			appServiceVerb != AppServiceVerb.None
		}.Count((bool active) => active) > 1)
		{
			list.Add("自检或验收不能与其它启动模式组合");
		}
		return new CommandLineOptions
		{
			SelfTest = flag,
			Diagnose = flag2,
			ShowLauncher = flag3,
			ShowSettings = flag4,
			Autostart = flag5,
			Sample = appearanceSample,
			Exit = flag7,
			AcceptanceDataRoot = text,
			AcceptanceReport = text2,
			Help = flag6,
			Unknown = list,
			Verb = appServiceVerb,
			VerbArguments = list2
		};
	}

	private static bool TryParseVerb(string token, out AppServiceVerb verb)
	{
		switch (token)
		{
		case "--import":
			verb = AppServiceVerb.Import;
			return true;
		case "--scan-start-menu":
			verb = AppServiceVerb.ScanStartMenu;
			return true;
		case "--import-start-menu":
			verb = AppServiceVerb.ImportStartMenu;
			return true;
		case "--list-library":
			verb = AppServiceVerb.ListLibrary;
			return true;
		case "--check-library":
			verb = AppServiceVerb.CheckLibrary;
			return true;
		case "--relocate":
			verb = AppServiceVerb.Relocate;
			return true;
		case "--launch":
			verb = AppServiceVerb.Launch;
			return true;
		case "--activate":
			verb = AppServiceVerb.Activate;
			return true;
		case "--verify-icons":
			verb = AppServiceVerb.VerifyIcons;
			return true;
		case "--clear-icon-cache":
			verb = AppServiceVerb.ClearIconCache;
			return true;
		case "--make-samples":
			verb = AppServiceVerb.MakeSamples;
			return true;
		case "--seed-library":
			verb = AppServiceVerb.SeedLibrary;
			return true;
		case "--icon-contact-sheet":
			verb = AppServiceVerb.IconContactSheet;
			return true;
		case "--phase3-acceptance":
			verb = AppServiceVerb.Phase3Acceptance;
			return true;
		case "--probe-aumid":
			verb = AppServiceVerb.ProbeAumid;
			return true;
		case "--probe-runevents":
			verb = AppServiceVerb.ProbeRunEvents;
			return true;
		case "--phase4-acceptance":
			verb = AppServiceVerb.Phase4Acceptance;
			return true;
		case "--phase5-acceptance":
			verb = AppServiceVerb.Phase5Acceptance;
			return true;
		case "--phase6-acceptance":
			verb = AppServiceVerb.Phase6Acceptance;
			return true;
		case "--probe-dragdrop":
			verb = AppServiceVerb.ProbeDragDrop;
			return true;
		case "--probe-showdesktop":
			verb = AppServiceVerb.ProbeShowDesktop;
			return true;
		default:
			verb = AppServiceVerb.None;
			return false;
		}
	}
}
