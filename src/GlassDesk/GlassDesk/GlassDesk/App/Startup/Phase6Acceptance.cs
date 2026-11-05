using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using GlassDesk.Core.Models.Settings;
using GlassDesk.Core.Storage;
using GlassDesk.Platform.Storage;

namespace GlassDesk.App.Startup;

internal static class Phase6Acceptance
{
	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
	{
		WriteIndented = true,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};

	public static int Run(AppPaths realPaths, IReadOnlyList<string> arguments)
	{
		if (arguments.Count != 2)
		{
			return 2;
		}
		return AcceptanceEntryPoint.Run(realPaths, arguments[1], new _003C_003Ez__ReadOnlySingleElementList<AcceptanceOutput>(new AcceptanceOutput("report", arguments[0])), (AcceptanceWorkspace workspace) => Run(realPaths, arguments, workspace));
	}

	public static int Run(AppPaths realPaths, IReadOnlyList<string> arguments, AcceptanceWorkspace workspace)
	{
		string text;
		if (arguments.Count <= 0 || string.IsNullOrWhiteSpace(arguments[0]))
		{
			InlineArray5<string> buffer = default;
			buffer[0] = Environment.CurrentDirectory;
			buffer[1] = "docs";
			buffer[2] = "evidence";
			buffer[3] = "阶段6";
			buffer[4] = "phase6-cli.json";
			text = Path.Combine(buffer);
		}
		else
		{
			text = Path.GetFullPath(arguments[0]);
		}
		string text2 = text;
		string dataRoot = workspace.DataRoot;
		List<object> cases = new List<object>();
		int failed = 0;
		if (DataDirectoryGuard.Overlaps(dataRoot, realPaths.RootDirectory))
		{
			Check("数据目录重叠保护", okay: false, "拒绝与当前用户数据目录相同或互为父子目录", "候选目录重叠，未读取或创建候选目录");
			Write(workspace.GetOutput("report"), text2, dataRoot, cases, 0, failed, 2);
			return 2;
		}
		StringComparison value = (OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
		Check("真实用户目录自身受保护", DataDirectoryGuard.Overlaps(realPaths.RootDirectory, realPaths.RootDirectory), "返回重叠", "使用DataDirectoryGuard.Overlaps");
		Check("真实用户目录的子目录受保护", DataDirectoryGuard.Overlaps(Path.Combine(realPaths.RootDirectory, "phase6-probe"), realPaths.RootDirectory), "返回重叠", "使用DataDirectoryGuard.Overlaps");
		string text3 = Path.Combine(Path.GetDirectoryName(realPaths.RootDirectory), "GlassDesk-Phase6-Sibling");
		Check("同级目录不误判为重叠", !DataDirectoryGuard.Overlaps(text3, realPaths.RootDirectory), "返回不重叠", $"真实目录={realPaths.RootDirectory}; 候选={text3}; comparison={value}");
		AppPaths appPaths = new AppPaths(dataRoot);
		try
		{
			appPaths.EnsureCreated();
			GlassDeskDataStore glassDeskDataStore = new GlassDeskDataStore(appPaths);
			glassDeskDataStore.LoadAll();
			GlassDeskSettings glassDeskSettings = SettingsPageEditor.Clone(glassDeskDataStore.Settings.Current);
			glassDeskSettings.Launcher.Rows = 4;
			glassDeskSettings.Shortcuts.HotCornerDwellMs = 777;
			SettingsPageEditor.Apply(glassDeskDataStore.Settings.Current, glassDeskSettings);
			glassDeskDataStore.Settings.Save();
			GlassDeskDataStore glassDeskDataStore2 = new GlassDeskDataStore(appPaths);
			glassDeskDataStore2.LoadAll();
			Check("新增设置保存后可由新实例重载", glassDeskDataStore2.Settings.Current.Launcher.Rows == 4 && glassDeskDataStore2.Settings.Current.Shortcuts.HotCornerDwellMs == 777, "Launcher.Rows=4且停留时间=777", $"Launcher.Rows={glassDeskDataStore2.Settings.Current.Launcher.Rows}; dwell={glassDeskDataStore2.Settings.Current.Shortcuts.HotCornerDwellMs}");
			GlassDeskSettings glassDeskSettings2 = SettingsPageEditor.Clone(glassDeskDataStore2.Settings.Current);
			GlassDeskSettings glassDeskSettings3 = SettingsPageEditor.ResetPage(glassDeskSettings2, "快捷操作");
			Check("单页恢复默认保留启动台设置", glassDeskSettings3.Launcher.Rows == glassDeskSettings2.Launcher.Rows && glassDeskSettings3.Shortcuts.HotCornerDwellMs == new ShortcutSettings().HotCornerDwellMs, $"启动台行数保留{glassDeskSettings2.Launcher.Rows}且快捷操作回默认", $"行数={glassDeskSettings3.Launcher.Rows}; dwell={glassDeskSettings3.Shortcuts.HotCornerDwellMs}");
			glassDeskSettings3.Shortcuts.HotCornerDwellMs = 99;
			Check("范围规则拒绝低于边界的停留时间", SettingsPageEditor.Validate(glassDeskSettings3) != null, "验证返回错误", SettingsPageEditor.Validate(glassDeskSettings3) ?? "未返回错误");
			glassDeskSettings3.Shortcuts.HotCornerDwellMs = 3001;
			Check("范围规则拒绝高于边界的停留时间", SettingsPageEditor.Validate(glassDeskSettings3) != null, "验证返回错误", SettingsPageEditor.Validate(glassDeskSettings3) ?? "未返回错误");
			GlassDeskSettings glassDeskSettings4 = SettingsPageEditor.Clone(glassDeskDataStore2.Settings.Current);
			glassDeskSettings4.Shortcuts.ToggleLauncher = new HotkeyDefinition
			{
				Enabled = true,
				Control = true,
				Key = "NOT_A_KEY"
			};
			Check("快捷键键名规则拒绝非法值", SettingsPageEditor.Validate(glassDeskSettings4) != null, "验证返回键名错误", SettingsPageEditor.Validate(glassDeskSettings4) ?? "未返回错误");
			GlassDeskSettings glassDeskSettings5 = SettingsPageEditor.Clone(glassDeskDataStore2.Settings.Current);
			glassDeskSettings5.Shortcuts.ToggleLauncher = new HotkeyDefinition
			{
				Enabled = true,
				Control = true,
				Key = "F13"
			};
			glassDeskSettings5.Shortcuts.HideLauncher = new HotkeyDefinition
			{
				Enabled = true,
				Control = true,
				Key = "F13"
			};
			Check("快捷键规则拒绝同组合重复绑定", SettingsPageEditor.Validate(glassDeskSettings5)?.Contains("同一个", StringComparison.Ordinal) ?? false, "返回组合重复错误", SettingsPageEditor.Validate(glassDeskSettings5) ?? "未返回错误");
			byte[] array = File.ReadAllBytes(appPaths.SettingsFile);
			try
			{
				glassDeskDataStore.Settings.Current.Shortcuts.HotCornerDwellMs = 99;
				string text4 = SettingsPageEditor.Validate(glassDeskDataStore.Settings.Current);
				if (text4 == null)
				{
					glassDeskDataStore.Settings.Save();
				}
				Check("无效设置不提交且磁盘内容保持", text4 != null && array.SequenceEqual(File.ReadAllBytes(appPaths.SettingsFile)), "未执行Save且文件字节不变", text4 ?? "设置被接受");
			}
			finally
			{
				glassDeskDataStore.Settings.Current.Shortcuts.HotCornerDwellMs = 777;
			}
		}
		catch (Exception ex)
		{
			Check("纯文件与规则验收未发生未处理异常", okay: false, "无异常", ex.ToString());
		}
		Write(workspace.GetOutput("report"), text2, dataRoot, cases, cases.Count - failed, failed, (failed != 0 || cases.Count <= 0) ? 1 : 0);
		Console.WriteLine($"Phase 6 CLI: {cases.Count - failed} passed, {failed} failed. Report: {text2}");
		if (cases.Count <= 0 || failed != 0)
		{
			return 1;
		}
		return 0;
		void Check(string name, bool okay, string expected, string actual)
		{
			if (!okay)
			{
				failed++;
			}
			cases.Add(new
			{
				name = name,
				driver = "settings rules and isolated files",
				expected = expected,
				actual = actual,
				status = (okay ? "Passed" : "Failed"),
				timestamp = DateTimeOffset.Now
			});
		}
	}

	private static void Write(Stream output, string reportPath, string root, List<object> cases, int passed, int failed, int exitCode)
	{
		AcceptanceEntryPoint.WriteText(output, JsonSerializer.Serialize(new
		{
			timestamp = DateTimeOffset.Now,
			artifact = typeof(Phase6Acceptance).Assembly.FullName,
			environment = Environment.OSVersion.ToString(),
			machine = Environment.MachineName,
			dataDirectory = root,
			driver = "isolated settings files and validation rules; no product windows or system input",
			passed = passed,
			failed = failed,
			exitCode = exitCode,
			cases = cases
		}, JsonOptions));
	}
}
