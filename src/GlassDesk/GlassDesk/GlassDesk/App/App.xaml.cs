using System;
using System.Windows;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using GlassDesk.App.Startup;
using GlassDesk.Core.Storage;
using GlassDesk.Platform;
using GlassDesk.Platform.Storage;
using GlassDesk.UI;
using GlassDesk.UI.Theming;

namespace GlassDesk.App;

public partial class App : Application, IDisposable
{
	private AppHost? _host;

	private CrashDiagnostics? _crashDiagnostics;

	private AcceptanceWorkspace? _pendingDiagnosticWorkspace;

	private bool _shaderFailureSubscribed;

	public void Dispose()
	{
		if (_shaderFailureSubscribed)
		{
			PixelShader.InvalidPixelShaderEncountered -= OnBackdropShaderFailure;
			_shaderFailureSubscribed = false;
		}
		_host?.Dispose();
		_host = null;
		_pendingDiagnosticWorkspace?.Dispose();
		_pendingDiagnosticWorkspace = null;
		_crashDiagnostics?.Dispose();
		_crashDiagnostics = null;
		GC.SuppressFinalize(this);
	}

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);
		CommandLineOptions commandLineOptions = CommandLineOptions.Parse(e.Args);
		if (!WindowsCapabilities.IsSupported)
		{
			MessageBox.Show("此版本需要 Windows 10 2004（Build 19041）及以上。", "GlassDesk", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			Shutdown(2);
			return;
		}
		PixelShader.InvalidPixelShaderEncountered += OnBackdropShaderFailure;
		_shaderFailureSubscribed = true;
		if (commandLineOptions.Autostart && commandLineOptions.Unknown.Count > 0)
		{
			Console.Error.WriteLine("--autostart 不能与其它启动参数组合；拒绝执行。");
			Shutdown(2);
			return;
		}
		if (commandLineOptions.SelfTest && (commandLineOptions.Verb != AppServiceVerb.None || commandLineOptions.Diagnose || commandLineOptions.Exit || commandLineOptions.ShowLauncher || commandLineOptions.ShowSettings || commandLineOptions.Sample != AppearanceSample.None || commandLineOptions.Help))
		{
			Console.Error.WriteLine("--selftest 不能与其它启动或应用服务模式组合。");
			Shutdown(2);
			return;
		}
		bool flag = commandLineOptions.Unknown.Count > 0;
		if (flag)
		{
			bool flag2 = commandLineOptions.SelfTest;
			if (!flag2)
			{
				AppServiceVerb verb = commandLineOptions.Verb;
				bool flag3 = ((verb == AppServiceVerb.Phase3Acceptance || verb == AppServiceVerb.Phase4Acceptance || (uint)(verb - 20) <= 1u) ? true : false);
				flag2 = flag3;
			}
			flag = flag2;
		}
		if (flag)
		{
			Console.Error.WriteLine("自检或阶段验收参数冲突或无法识别；拒绝启动验收动作。");
			Shutdown(2);
			return;
		}
		bool flag4 = commandLineOptions.AcceptanceDataRoot != null || commandLineOptions.AcceptanceReport != null;
		if (flag4 && !AcceptanceEntryPoint.IsValidIsolatedRun(commandLineOptions))
		{
			Console.Error.WriteLine("验收隔离参数只允许与独立的 --selftest 或 --diagnose 及完整的数据目录、报告参数一起使用。");
			Shutdown(2);
			return;
		}
		if (commandLineOptions.Help)
		{
			ReportWriter.Write(CommandLineOptions.HelpText, null);
			Shutdown(0);
			return;
		}
		AppPaths paths = AppPaths.ForCurrentUser();
		if (flag4)
		{
			if (commandLineOptions.Diagnose)
			{
				_pendingDiagnosticWorkspace = AcceptanceEntryPoint.Prepare(paths, commandLineOptions.AcceptanceDataRoot, new _003C_003Ez__ReadOnlySingleElementList<AcceptanceOutput>(new AcceptanceOutput("report", commandLineOptions.AcceptanceReport)));
				if (_pendingDiagnosticWorkspace == null)
				{
					Shutdown(2);
					return;
				}
				Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)(() =>
				{
					AcceptanceWorkspace pendingDiagnosticWorkspace = _pendingDiagnosticWorkspace;
					_pendingDiagnosticWorkspace = null;
					if (pendingDiagnosticWorkspace != null)
					{
						int exitCode4 = AcceptanceEntryPoint.Run(pendingDiagnosticWorkspace, (AcceptanceWorkspace prepared) =>
						{
							ThemeService.Initialize(Dispatcher);
							return DiagnosticRunner.Run(new AppPaths(prepared.DataRoot), prepared.GetOutput("report"));
						});
						Shutdown(exitCode4);
					}
				}));
			}
			else
			{
				int exitCode = AcceptanceEntryPoint.Run(paths, commandLineOptions.AcceptanceDataRoot, new _003C_003Ez__ReadOnlySingleElementList<AcceptanceOutput>(new AcceptanceOutput("report", commandLineOptions.AcceptanceReport)), (AcceptanceWorkspace workspace) =>
				{
					ThemeService.Initialize(Dispatcher);
					return SelfTest.Run(new AppPaths(workspace.DataRoot), workspace.GetOutput("report"));
				});
				Shutdown(exitCode);
			}
		}
		else if (commandLineOptions.IsAppServiceCommand)
		{
			int exitCode2 = AppServiceRunner.Run(paths, commandLineOptions);
			Shutdown(exitCode2);
		}
		else if (commandLineOptions.SelfTest)
		{
			ThemeService.Initialize(Dispatcher);
			int exitCode3 = SelfTest.Run(paths);
			Shutdown(exitCode3);
		}
		else if (commandLineOptions.Diagnose)
		{
			ThemeService.Initialize(Dispatcher);
			Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)(() =>
			{
				int exitCode4 = DiagnosticRunner.Run(paths);
				Shutdown(exitCode4);
			}));
		}
		else
		{
			if (commandLineOptions.Unknown.Count > 0)
			{
				ReportWriter.Write("忽略无法识别的参数：" + string.Join(' ', commandLineOptions.Unknown) + Environment.NewLine + CommandLineOptions.HelpText, null);
			}
			_crashDiagnostics = new CrashDiagnostics(this, paths.LogDirectory);
			_host = new AppHost(this, commandLineOptions);
			switch (_host.Start())
			{
			case StartupOutcome.NothingToDo:
				ReportWriter.Write("没有正在运行的 GlassDesk 实例，无需退出。", null);
				Shutdown(0);
				break;
			case StartupOutcome.LegacyInstanceBlocked:
			case StartupOutcome.UserDataLeaseUnavailable:
			case StartupOutcome.PrimaryDisplayUnavailable:
				Shutdown(2);
				break;
			default:
				Shutdown(0);
				break;
			case StartupOutcome.Started:
				break;
			}
		}
	}

	private static void OnBackdropShaderFailure(object? sender, EventArgs e)
	{
		BackdropRecovery.UseBasicRendering();
	}

	protected override void OnExit(ExitEventArgs e)
	{
		Dispose();
		base.OnExit(e);
	}
}
