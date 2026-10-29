using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GlassDesk.App.Startup;
using GlassDesk.App.Tray;
using GlassDesk.App.Updates;
using GlassDesk.AppServices;
using GlassDesk.AppServices.Updates;
using GlassDesk.Core.Input;
using GlassDesk.Core.Models;
using GlassDesk.Core.Models.Settings;
using GlassDesk.Core.Storage;
using GlassDesk.Dock;
using GlassDesk.Launcher;
using GlassDesk.Platform;
using GlassDesk.Platform.Displays;
using GlassDesk.Platform.Installation;
using GlassDesk.Platform.Shell;
using GlassDesk.Platform.Startup;
using GlassDesk.Platform.Storage;
using GlassDesk.Settings;
using GlassDesk.UI;
using GlassDesk.UI.Controls;
using GlassDesk.UI.Settings;
using GlassDesk.UI.Theming;
using Microsoft.Win32;

namespace GlassDesk.App;

internal sealed class AppHost : IDisposable
{
	private const string InstanceName = "GlassDesk.Host";

	private const string MessageWindowName = "GlassDesk.TrayMessageWindow";

	private readonly Application _application;

	private readonly CommandLineOptions _options;

	private SingleInstanceGuard? _guard;

	private UserDataLease? _userDataLease;

	private GlassDeskDataStore? _data;

	private AppServiceHost? _services;

	private DataLoadReport? _loadReport;

	private HwndSource? _messageWindow;

	private TrayIcon? _tray;

	private nint _trayIconHandle;

	private MissingTargetReport? _missingReport;

	private PrimaryDisplaySession? _primaryDisplaySession;

	private IDisposable? _backdropDiagnostics;

	private DockWindow? _dock;

	private StatusBarWindow? _statusBar;

	private LauncherWindow? _launcher;

	private SettingsWindow? _settings;

	private DockSettingsHost? _dockSettings;

	private bool _shuttingDown;

	private bool _disposed;

	private readonly DeferredCommandGate _deferredCommandGate = new DeferredCommandGate();

	private InteractionController? _interaction;

	private int _displayRefreshPending;

	private bool _restoreDockAfterDisplayChange;

	private bool _restoreLauncherAfterDisplayChange;

	private bool _restoreSettingsAfterDisplayChange;

	private int _maintenanceStarted;

	private OnboardingWindow? _onboarding;

	private static readonly TimeSpan LauncherWarmRetention = TimeSpan.FromSeconds(10L);

	private PerformanceCoordinator? _performance;

	private PerformanceFullscreenMonitor? _performanceFullscreen;

	private DispatcherTimer? _performanceDeadlineTimer;

	private DateTimeOffset? _launcherHiddenSince;

	private bool _applyingPerformance;

	private bool _performanceApplyPending;

	private bool _launcherPerformanceSleeping;

	private bool _libraryDirtyWhileSleeping;

	private bool _launcherLibraryDirty;

	private bool _performanceActivationPending;

	private volatile bool _performancePointerInputSuppressed;

	private long _foregroundObservationSequence;

	private DispatcherTimer? _trayRecoveryTimer;

	private readonly CancellationTokenSource _updateLifetime = new CancellationTokenSource();

	private readonly HttpClient _updateHttp = new HttpClient
	{
		Timeout = Timeout.InfiniteTimeSpan
	};

	private CloudDownloadWindow? _updateWindow;

	private bool _checkingUpdate;

	internal PerformanceState RuntimePerformanceState => _performance?.State ?? PerformanceState.Active;

	internal long RuntimePerformanceGeneration => _performance?.Generation ?? 0;

	internal bool RuntimeManualGameSleep => _performance?.ManualSleep ?? false;

	internal bool RuntimeFullscreenFallback => _performanceFullscreen?.UsesFallback ?? false;

	internal static Version ProductVersion
	{
		get
		{
			AssemblyInformationalVersionAttribute? customAttribute = typeof(AppHost).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
			if (!Version.TryParse((customAttribute != null) ? customAttribute.InformationalVersion.Split('+')[0].Split('-')[0] : null, out Version result))
			{
				return new Version(0, 2, 9);
			}
			return new Version(result.Major, result.Minor, Math.Max(0, result.Build));
		}
	}

	private void InitializeDefaultAutostart()
	{
		if (_options.Sample != AppearanceSample.None || _data == null || _loadReport == null)
		{
			return;
		}
		string executablePath = Environment.ProcessPath ?? string.Empty;
		StartupChangeResult startupChangeResult = DefaultStartupRegistration.Apply(_data.Settings, _loadReport.SettingsStatus, new InstallationLocator().Inspect(executablePath).State, new StartupRegistration(), executablePath);
		if ((object)startupChangeResult != null)
		{
			_settings?.RefreshStartupRegistration();
			if (!startupChangeResult.Succeeded)
			{
				_settings?.ShowStartupRegistrationFailure(startupChangeResult.Error ?? "未知错误。");
			}
		}
	}

	public AppHost(Application application, CommandLineOptions options)
	{
		ArgumentNullException.ThrowIfNull(application, "application");
		ArgumentNullException.ThrowIfNull(options, "options");
		_application = application;
		_options = options;
	}

	public StartupOutcome Start()
	{
		_guard = SingleInstanceGuard.Acquire("GlassDesk.Host");
		if (!_guard.IsFirstInstance)
		{
			HandOffToRunningInstance();
			return StartupOutcome.HandedOffToRunningInstance;
		}
		if (!_guard.TryAcquireLegacyProtocolLease(out var status))
		{
			_guard.Dispose();
			_guard = null;
			MessageBox.Show(StartupIntent.GetLegacyInstanceBlockMessage(status), "GlassDesk 启动受阻", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			return StartupOutcome.LegacyInstanceBlocked;
		}
		if (StartupIntent.ShouldExitWithoutExistingInstance(_options))
		{
			return StartupOutcome.NothingToDo;
		}
		if (!UserDataLease.TryAcquireShared(AppPaths.ForCurrentUser().RootDirectory, out _userDataLease))
		{
			MessageBox.Show("无法取得 GlassDesk 用户数据的运行租约。数据可能正在维护，或当前文件夹不可访问；为保护数据，本次启动已停止。请稍后重试。", "GlassDesk 数据暂不可用", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			return StartupOutcome.UserDataLeaseUnavailable;
		}
		_guard.CommandReceived += OnCommandReceived;
		_guard.StartListening();
		DisplayInfo displayInfo = DisplayService.Primary();
		if ((object)displayInfo == null)
		{
			MessageBox.Show("无法读取 Windows 当前主显示器。GlassDesk 未创建任何可见窗口。", "GlassDesk 启动受阻", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			return StartupOutcome.PrimaryDisplayUnavailable;
		}
		_primaryDisplaySession = new PrimaryDisplaySession(displayInfo);
		ThemeService themeService = ThemeService.Initialize(_application.Dispatcher);
		_data = new GlassDeskDataStore(AppPaths.ForCurrentUser());
		_loadReport = _data.LoadAll();
		_backdropDiagnostics = BackdropRecovery.BeginAutomaticDiagnostics(_data.Paths.LogDirectory);
		_services = new AppServiceHost(_data);
		_missingReport = _services.Maintenance.RefreshMissingTargets();
		AppearanceSettings appearance = _data.Settings.Current.Appearance;
		ApplySampleOverride(appearance, _options.Sample);
		themeService.Update(appearance);
		_dockSettings = DockSettingsHost.Initialize(_application.Dispatcher);
		_dockSettings.Update(_data.Settings.Current.Dock);
		_services.Running.Start();
		CreateMessageWindow();
		CreateTrayIcon();
		CreateWindows();
		InitializeDefaultAutostart();
		InitializePerformanceCoordinator();
		InitializeInteractionController();
		_application.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)delegate
		{
			if (!_disposed && !_shuttingDown)
			{
				LauncherWindow? launcher = _launcher;
				if (launcher == null || !launcher.IsVisible)
				{
					_launcher?.PrewarmNextRevealMaterial();
				}
			}
		});
		themeService.Changed += OnThemeChanged;
		WriteStartupLog();
		InitializeUpdates();
		InitializeOnboarding();
		return StartupOutcome.Started;
	}

	private static void ApplySampleOverride(AppearanceSettings appearance, AppearanceSample sample)
	{
		ArgumentNullException.ThrowIfNull(appearance, "appearance");
		switch (sample)
		{
		case AppearanceSample.Light:
			appearance.Theme = GlassDesk.Core.Models.ThemeMode.Light;
			appearance.UseSystemBackdrop = true;
			appearance.ReduceTransparency = false;
			break;
		case AppearanceSample.Dark:
			appearance.Theme = GlassDesk.Core.Models.ThemeMode.Dark;
			appearance.UseSystemBackdrop = true;
			appearance.ReduceTransparency = false;
			break;
		case AppearanceSample.Degraded:
			appearance.Theme = GlassDesk.Core.Models.ThemeMode.Light;
			appearance.UseSystemBackdrop = false;
			appearance.ReduceTransparency = false;
			break;
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_deferredCommandGate.Close();
			DisposeOnboarding();
			DisposeUpdates();
			ThemeService current = ThemeService.Current;
			if (current != null)
			{
				current.Changed -= OnThemeChanged;
			}
			if (_guard != null)
			{
				_guard.CommandReceived -= OnCommandReceived;
			}
			DisposeInteractionController();
			DisposePerformanceCoordinator();
			_backdropDiagnostics?.Dispose();
			_backdropDiagnostics = null;
			StopTrayRecovery();
			_tray?.Dispose();
			_tray = null;
			_services?.Dispose();
			_services = null;
			_messageWindow?.Dispose();
			_messageWindow = null;
			_userDataLease?.Dispose();
			_userDataLease = null;
			_guard?.Dispose();
			_guard = null;
			ThemeService.Current?.Dispose();
			_disposed = true;
		}
	}

	private void ShowLauncher()
	{
		ShowLauncher(null);
	}

	private void ShowLauncher(PixelRect? sourceBounds)
	{
		if (_launcher == null)
		{
			return;
		}
		PrimaryDisplaySession? primaryDisplaySession = _primaryDisplaySession;
		if (primaryDisplaySession == null || !primaryDisplaySession.IsAvailable)
		{
			return;
		}
		RequestPerformanceActivation();
		try
		{
			_launcher.RefreshDiagnostics();
			_dock?.SetLauncherContainerState(expanded: true);
			_launcher.ShowWithTransition(sourceBounds ?? _dock?.GetLauncherButtonPixelBounds());
			_launcher.Activate();
		}
		finally
		{
			_performanceActivationPending = false;
			ReconcilePerformance();
		}
	}

	private void HideLauncher()
	{
		_launcher?.CloseWithTransition();
	}

	private void ShowSettings()
	{
		if (_settings == null)
		{
			return;
		}
		PrimaryDisplaySession? primaryDisplaySession = _primaryDisplaySession;
		if (primaryDisplaySession != null && primaryDisplaySession.IsAvailable)
		{
			_settings.RefreshDiagnostics(_loadReport);
			_settings.RefreshStartupRegistration();
			_settings.RefreshInstallationStatus(() => new InstallationLocator().Inspect(Environment.ProcessPath ?? string.Empty));
			if (!_settings.IsVisible)
			{
				_settings.PositionOnPrimary();
				_settings.Show();
			}
			_settings.Activate();
		}
	}

	private void ExitApplication()
	{
		_deferredCommandGate.Close();
		_shuttingDown = true;
		_application.Shutdown();
	}

	private void HandOffToRunningInstance()
	{
		string duplicateInstanceCommand = StartupIntent.GetDuplicateInstanceCommand(_options);
		if (duplicateInstanceCommand != null)
		{
			SingleInstanceGuard.TrySendCommandAsync("GlassDesk.Host", duplicateInstanceCommand, TimeSpan.FromSeconds(3L)).GetAwaiter().GetResult();
		}
	}

	private void CreateWindows()
	{
		_dock = new DockWindow();
		_statusBar = new StatusBarWindow();
		_launcher = new LauncherWindow();
		PrimaryDisplaySession session = _primaryDisplaySession ?? throw new InvalidOperationException("Primary Display must be captured before creating GlassDesk windows.");
		_dock.BindPrimaryDisplay(session);
		_statusBar.BindPrimaryDisplay(session);
		_statusBar.AttachSettings(_dockSettings);
		_statusBar.IsLauncherActive = () => _launcher?.IsVisible ?? false;
		_statusBar.ConfigureGameSleep(() => RuntimeManualGameSleep, SetManualGameSleep);
		_statusBar.ConfigureSettings(ShowSettings);
		_statusBar.ConfigurePlacement(() => (X: _data.Settings.Current.Dock.StatusBarOffsetX, Edge: _data.Settings.Current.Dock.StatusBarEdge));
		_statusBar.ConfigureSizeScale(() => _data.Settings.Current.Dock.StatusBarSizeScale);
		_launcher.BindPrimaryDisplay(session);
		_launcher.ContainerAnchorProvider = () => _dock?.GetLauncherButtonPixelBounds();
		_launcher.IsVisibleChanged += delegate(object _, DependencyPropertyChangedEventArgs e)
		{
			DockWindow? dock = _dock;
			if (dock != null)
			{
				object newValue = e.NewValue;
				dock.SetLauncherContainerState(newValue is bool && (bool)newValue);
			}
		};
		_settings = new SettingsWindow();
		_settings.BindPrimaryDisplay(session);
		_settings.AttachDataStore(_data, _loadReport);
		_settings.AttachStartupRegistration(new StartupRegistration(), Environment.ProcessPath ?? string.Empty);
		_settings.AttachInstallation(new InstallationLocator().Inspect(Environment.ProcessPath ?? string.Empty));
		_dock.AttachServices(_services, _dockSettings, delegate(PixelRect? source)
		{
			ToggleLauncherFromDock(_launcher, source, ShowLauncher);
		});
		_dock.ToggleDesktopIcons = delegate
		{
			ExecuteInteraction(InputAction.ToggleDesktopIcons);
		};
		LauncherWindow? launcher = _launcher;
		AppServiceHost? services = _services;
		DataLoadReport? loadReport = _loadReport;
		launcher.AttachServices(services, loadReport == null || loadReport.LibraryStatus != LoadStatus.Unrecoverable);
		_launcher.InternalDragStarted += delegate
		{
			_dock?.RaiseAboveLauncherDuringDrag();
		};
		_launcher.InternalDragFinished += delegate
		{
			_launcher?.RaiseAboveDockAfterDrag();
		};
		_services.LibraryChanged += delegate
		{
			if (_application.Dispatcher.CheckAccess())
			{
				RefreshLibraryForPerformance();
			}
			else
			{
				_application.Dispatcher.BeginInvoke(new Action(RefreshLibraryForPerformance));
			}
		};
		_settings.LauncherSettingsChanged += delegate(object? _, LauncherSettings settings)
		{
			_launcher?.ApplySettings(settings);
		};
		_launcher.ApplySettings(_data.Settings.Current.Launcher);
		BitmapSource icon = CreateWindowIcon();
		Window[] array = new Window[4] { _dock, _statusBar, _launcher, _settings };
		foreach (Window obj in array)
		{
			obj.Icon = icon;
			obj.Closing += OnWindowClosing;
		}
		_dock.Show();
		_statusBar.Start();
		if (StartupIntent.ShouldOpenLauncher(_options))
		{
			ShowLauncher();
		}
		if (StartupIntent.ShouldOpenSettings(_options))
		{
			ShowSettings();
		}
	}

	private static BitmapSource CreateWindowIcon()
	{
		return TrayIconArtwork.RenderBitmap(32);
	}

	private void CreateMessageWindow()
	{
		HwndSourceParameters hwndSourceParameters = new HwndSourceParameters("GlassDesk.TrayMessageWindow");
		hwndSourceParameters.Width = 0;
		hwndSourceParameters.Height = 0;
		hwndSourceParameters.PositionX = 0;
		hwndSourceParameters.PositionY = 0;
		hwndSourceParameters.WindowStyle = 0;
		hwndSourceParameters.ParentWindow = 0;
		HwndSourceParameters parameters = hwndSourceParameters;
		_messageWindow = new HwndSource(parameters);
		_messageWindow.AddHook(OnMessageWindowProc);
	}

	private void CreateTrayIcon()
	{
		if (_messageWindow != null)
		{
			_trayIconHandle = TrayIconArtwork.CreateHandle(32);
			_tray = new TrayIcon(_messageWindow.Handle);
			_tray.Show(_trayIconHandle, "GlassDesk");
			StartTrayRecovery();
		}
	}

	private nint OnMessageWindowProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
	{
		if (TrayIcon.TaskbarCreatedMessage != 0 && msg == TrayIcon.TaskbarCreatedMessage)
		{
			RefreshTrayRegistration();
			handled = true;
			return 0;
		}
		switch (msg)
		{
		case 786:
		{
			SettingsWindow? settings = _settings;
			if (settings == null || !settings.TryCaptureRegisteredHotkey(lParam))
			{
				uint num = (uint)((((IntPtr)lParam).ToInt64() >> 16) & 0xFFFF);
				PerformanceCoordinator? performance = _performance;
				int num2;
				if (performance != null && performance.IsSuppressionRequested)
				{
					LauncherWindow? launcher = _launcher;
					num2 = ((launcher == null || !launcher.IsVisible) ? 1 : 0);
				}
				else
				{
					num2 = 0;
				}
				bool flag = (byte)num2 != 0;
				if (flag)
				{
					bool flag2 = ((num - 5 <= 1 || num - 256 <= 1) ? true : false);
					flag = flag2;
				}
				if (flag)
				{
					InteractionController? interaction = _interaction;
					if (interaction == null || !interaction.IsGameSleepHotkey(((IntPtr)wParam).ToInt32()))
					{
						handled = true;
						return 0;
					}
				}
				_interaction?.HandleHotkey(((IntPtr)wParam).ToInt32());
			}
			handled = true;
			return 0;
		}
		default:
			return 0;
		case 2048:
			switch (TrayIcon.Classify((int)lParam))
			{
			case TrayEvent.Activate:
			case TrayEvent.DoubleClick:
				ShowLauncher();
				handled = true;
				break;
			case TrayEvent.ContextMenu:
				ShowTrayMenu(hwnd);
				handled = true;
				break;
			}
			return 0;
		}
	}

	private void ShowTrayMenu(nint hwnd)
	{
		ContextMenu obj = new ContextMenu
		{
			Placement = PlacementMode.MousePoint,
			PlacementTarget = _dock,
			StaysOpen = false,
			Items = 
			{
				(object)CreateMenuItem("打开启动台", ShowLauncher),
				(object)CreateMenuItem("隐藏启动台", HideLauncher)
			}
		};
		MenuItem gameSleep = new MenuItem
		{
			Header = "游戏休眠",
			IsCheckable = true,
			IsChecked = RuntimeManualGameSleep
		};
		gameSleep.Click += delegate
		{
			SetManualGameSleep(gameSleep.IsChecked);
		};
		obj.Items.Add(gameSleep);
		obj.Items.Add(new Separator());
		obj.Items.Add(CreateMenuItem("设置…", ShowSettings));
		obj.Items.Add(CreateMenuItem("打开数据文件夹", OpenDataFolder));
		obj.Items.Add(new Separator());
		obj.Items.Add(CreateMenuItem("退出", ExitApplication));
		WindowActivation.TryBringToForeground(hwnd);
		obj.IsOpen = true;
	}

	private static MenuItem CreateMenuItem(string header, Action action)
	{
		MenuItem menuItem = new MenuItem();
		menuItem.Header = header;
		menuItem.Click += delegate
		{
			action();
		};
		return menuItem;
	}

	private void OpenDataFolder()
	{
		if (_data == null)
		{
			return;
		}
		try
		{
			_data.Paths.EnsureCreated();
			Process.Start(new ProcessStartInfo
			{
				FileName = _data.Paths.RootDirectory,
				UseShellExecute = true
			});
		}
		catch (Exception ex) when (((ex is Win32Exception || ex is InvalidOperationException || ex is IOException) ? 1 : 0) != 0)
		{
			AppendToLog(new global::<>z__ReadOnlySingleElementList<string>($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}] 无法打开数据文件夹：{ex.Message}"));
		}
	}

	private void ExportBackdropDiagnostics()
	{
		SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			Title = "导出背景诊断（不包含屏幕画面）",
			Filter = "JSON 报告|*.json",
			FileName = "GlassDesk-background-diagnostics.json",
			AddExtension = true,
			DefaultExt = ".json"
		};
		if (saveFileDialog.ShowDialog() != true)
		{
			return;
		}
		try
		{
			BackdropRecovery.ExportDiagnosticReport(saveFileDialog.FileName);
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			ReportInteractionError("无法导出背景诊断：" + ex.Message);
		}
	}

	private void OnCommandReceived(object? sender, string command)
	{
		if (_deferredCommandGate.IsClosed || _shuttingDown || _application.Dispatcher.HasShutdownStarted || _application.Dispatcher.HasShutdownFinished)
		{
			return;
		}
		_application.Dispatcher.BeginInvoke((Func<bool>)(() => _deferredCommandGate.TryRun(delegate
		{
			if (!_shuttingDown && !_application.Dispatcher.HasShutdownStarted && !_application.Dispatcher.HasShutdownFinished)
			{
				switch (command)
				{
				case "show-launcher":
					ShowLauncher();
					break;
				case "hide-launcher":
					HideLauncher();
					break;
				case "show-settings":
					ShowSettings();
					break;
				case "exit":
					ExitApplication();
					break;
				}
			}
		})));
	}

	private void OnThemeChanged(object? sender, EventArgs e)
	{
		OnPerformanceSettingsChanged(sender, e);
		if (_tray != null)
		{
			_tray.Update(TrayIconArtwork.CreateHandle(32), "GlassDesk");
			if (_trayRecoveryTimer != null)
			{
				_trayRecoveryTimer.Interval = TimeSpan.FromSeconds(_tray.IsVisible ? 30 : 2);
			}
			BitmapSource icon = CreateWindowIcon();
			Window[] array = new Window[4] { _dock, _statusBar, _launcher, _settings };
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Icon = icon;
			}
		}
	}

	private void OnWindowClosing(object? sender, CancelEventArgs e)
	{
		if (_shuttingDown)
		{
			return;
		}
		e.Cancel = true;
		if (sender is Window window)
		{
			if (window is LauncherWindow launcherWindow)
			{
				launcherWindow.CloseWithTransition();
			}
			else
			{
				window.Hide();
			}
		}
	}

	private void WriteStartupLog()
	{
		if (_data == null)
		{
			return;
		}
		List<string> list = new List<string>
		{
			$"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}] GlassDesk 启动",
			$"  版本：{typeof(AppHost).Assembly.GetName().Version}",
			"  数据目录：" + _data.Paths.RootDirectory,
			"  " + WindowsCapabilities.Describe(),
			"  " + ThemeService.Current?.Describe(),
			$"  应用库条目：{_data.Library.Items.Count}"
		};
		if ((object)_missingReport != null)
		{
			list.Add($"  目标失效：{_missingReport.Missing} 条（检查 {_missingReport.Checked} 条，新发现 {_missingReport.NewlyBroken.Count} 条，恢复 {_missingReport.Recovered.Count} 条）");
		}
		int value = _data.Library.Items.Count((AppEntry entry) => entry != null && !string.IsNullOrEmpty(entry.Arguments));
		list.Add($"  带启动参数：{value} 条");
		list.Add($"  缓存图标：{_services?.Icons.FileCount ?? 0} 个");
		DataLoadReport? loadReport = _loadReport;
		if (loadReport != null && loadReport.HasNotices)
		{
			list.Add("  数据加载提示：");
			list.AddRange(_loadReport.Notices.Select((string notice) => "    · " + notice));
		}
		DataLoadReport? loadReport2 = _loadReport;
		if (loadReport2 != null && loadReport2.PreservedFiles.Count > 0)
		{
			list.Add("  已保留的损坏文件：");
			list.AddRange(_loadReport.PreservedFiles.Select((string path) => "    · " + path));
		}
		AppendToLog(list);
	}

	private void AppendToLog([ParamCollection] IEnumerable<string> lines)
	{
		if (_data == null)
		{
			return;
		}
		try
		{
			_data.Paths.EnsureCreated();
			File.AppendAllLines(Path.Combine(_data.Paths.LogDirectory, "startup.log"), lines);
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
		}
	}

	internal static void ToggleLauncherFromDock(LauncherWindow? launcher, PixelRect? sourceBounds, Action<PixelRect?> showLauncher)
	{
		ArgumentNullException.ThrowIfNull(showLauncher, "showLauncher");
		if (launcher != null)
		{
			if (launcher.IsVisible)
			{
				launcher.CloseWithTransition();
			}
			else
			{
				showLauncher(sourceBounds);
			}
		}
	}

	private void InitializeInteractionController()
	{
		if (_data != null && _messageWindow != null && _launcher != null && _settings != null)
		{
			_interaction = new InteractionController(_data, _primaryDisplaySession ?? throw new InvalidOperationException("Primary Display must be captured before interaction starts."), _messageWindow.Handle, _application.Dispatcher, CanExecuteInteraction, ExecuteInteraction, ReportInteractionError);
			_launcher.NavigationAvailabilityChanged += OnLauncherNavigationAvailabilityChanged;
			_interaction.SetLauncherAvailability(_launcher.IsVisible, _launcher.CanNavigatePages);
			_settings.InteractionSettingsApply = ApplyInteractionSettings;
			_settings.DesktopIconsRead = _interaction.ReadDesktopIcons;
			_settings.DesktopIconsSet = _interaction.SetDesktopIcons;
			_settings.DataRestored += OnSettingsDataRestored;
			_settings.UserDataDeletionRequested += OnSettingsUserDataDeletionRequested;
			_settings.UninstallRequested += OnSettingsUninstallRequested;
			string text = _interaction.Initialize();
			if (text != null)
			{
				ReportInteractionError(text);
			}
			else
			{
				_settings.ShowInteractionStatus("快捷操作服务已就绪。");
			}
			ApplyDisplayRuntimeSettings(_data.Settings.Current.Display);
			SystemEvents.DisplaySettingsChanged += OnSystemDisplaySettingsChanged;
			SystemEvents.UserPreferenceChanged += OnSystemUserPreferenceChanged;
			SystemEvents.PowerModeChanged += OnBackgroundPowerModeChanged;
			SystemEvents.SessionSwitch += OnBackgroundSessionSwitch;
		}
	}

	private string? ApplyInteractionSettings(ShortcutSettings shortcuts, DisplaySettings display)
	{
		if (_interaction == null)
		{
			return "运行服务未连接，未保存更改。";
		}
		string text = _interaction.Apply(shortcuts, display);
		if (text != null)
		{
			return text;
		}
		ApplyDisplayRuntimeSettings(_data.Settings.Current.Display);
		_interaction.RefreshDisplays();
		return null;
	}

	private void ApplyDisplayRuntimeSettings(DisplaySettings display)
	{
		if (_dockSettings != null && _data != null)
		{
			_dockSettings.Update(_data.Settings.Current.Dock);
		}
		if (_dock != null)
		{
			_dock.SuppressPassiveTriggersWhenFullscreen = display.SuppressTriggersWhenFullscreen;
		}
	}

	private bool CanExecuteInteraction(InputAction action)
	{
		InteractionController? interaction = _interaction;
		if (interaction != null && interaction.CanExecute(action))
		{
			if (action != InputAction.ToggleGameSleep)
			{
				return !_performancePointerInputSuppressed;
			}
			return true;
		}
		return false;
	}

	private void ExecuteInteraction(InputAction action)
	{
		switch (action)
		{
		case InputAction.ToggleLauncher:
		{
			LauncherWindow? launcher = _launcher;
			if (launcher != null && launcher.IsVisible && !_launcher.IsClosingContainer)
			{
				HideLauncher();
			}
			else
			{
				ShowLauncher();
			}
			break;
		}
		case InputAction.HideLauncher:
			HideLauncher();
			break;
		case InputAction.NextPage:
			_launcher?.NavigatePage(1);
			break;
		case InputAction.PreviousPage:
			_launcher?.NavigatePage(-1);
			break;
		case InputAction.ToggleDesktopIcons:
			_interaction?.ToggleDesktopIcons();
			break;
		case InputAction.ToggleDockRevealLock:
			_dock?.ToggleRevealLock();
			_statusBar?.ToggleRevealLock();
			break;
		case InputAction.ToggleGameSleep:
			SetManualGameSleep(!RuntimeManualGameSleep);
			break;
		}
	}

	private void OnLauncherNavigationAvailabilityChanged(object? sender, EventArgs e)
	{
		if (_launcher != null)
		{
			_interaction?.SetLauncherAvailability(_launcher.IsVisible, _launcher.CanNavigatePages);
		}
	}

	private void OnSettingsDataRestored(object? sender, EventArgs e)
	{
		ExitApplication();
	}

	private void ReportInteractionError(string message)
	{
		_settings?.ShowInteractionStatus(message);
		string logFilePath = ((_data == null) ? null : Path.Combine(_data.Paths.LogDirectory, "interaction.log"));
		ReportWriter.Write(message, logFilePath);
	}

	private void OnSystemDisplaySettingsChanged(object? sender, EventArgs e)
	{
		PerformanceCoordinator? performance = _performance;
		if (performance == null || performance.State != PerformanceState.Sleeping)
		{
			BackdropRecovery.RetryBackground("DisplaySettingsChanged");
		}
		QueueDisplayRefresh();
	}

	private void OnBackgroundPowerModeChanged(object sender, PowerModeChangedEventArgs e)
	{
		if (e.Mode == PowerModes.Suspend)
		{
			SetPerformanceSessionSuspended(suspended: true);
		}
		else if (e.Mode == PowerModes.Resume)
		{
			SetPerformanceSessionSuspended(suspended: false);
		}
	}

	private void OnBackgroundSessionSwitch(object sender, SessionSwitchEventArgs e)
	{
		SessionSwitchReason reason = e.Reason;
		if ((reason == SessionSwitchReason.ConsoleDisconnect || reason == SessionSwitchReason.RemoteDisconnect || reason == SessionSwitchReason.SessionLock) ? true : false)
		{
			SetPerformanceSessionSuspended(suspended: true);
			return;
		}
		reason = e.Reason;
		if ((reason == SessionSwitchReason.ConsoleConnect || reason == SessionSwitchReason.RemoteConnect || reason == SessionSwitchReason.SessionUnlock) ? true : false)
		{
			SetPerformanceSessionSuspended(suspended: false);
		}
	}

	private void OnSystemUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
	{
		if (e.Category == UserPreferenceCategory.Desktop)
		{
			BackdropRecovery.RetryBackground("DesktopSettingsChanged");
		}
		UserPreferenceCategory category = e.Category;
		if ((category == UserPreferenceCategory.General || category == UserPreferenceCategory.Window || category == UserPreferenceCategory.VisualStyle) ? true : false)
		{
			QueueDisplayRefresh();
		}
	}

	private void QueueDisplayRefresh()
	{
		if (_application.Dispatcher.HasShutdownStarted || _application.Dispatcher.HasShutdownFinished || Interlocked.Exchange(ref _displayRefreshPending, 1) != 0)
		{
			return;
		}
		_application.Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)delegate
		{
			Interlocked.Exchange(ref _displayRefreshPending, 0);
			if (!_disposed)
			{
				DisplayInfo currentPrimary = DisplayService.Primary();
				bool flag = false;
				PrimaryDisplaySession primaryDisplaySession = _primaryDisplaySession;
				if (primaryDisplaySession != null && !primaryDisplaySession.TryRefreshPrimary(currentPrimary))
				{
					_restoreDockAfterDisplayChange |= _dock?.IsVisible ?? false;
					_restoreLauncherAfterDisplayChange |= _launcher?.IsVisible ?? false;
					_restoreSettingsAfterDisplayChange |= _settings?.IsVisible ?? false;
					_dock?.StopForPrimaryDisplayChange();
					_statusBar?.StopForPrimaryDisplayChange();
					_launcher?.StopForPrimaryDisplayChange();
					foreach (Window window in _application.Windows)
					{
						if (window is GlassWindow glassWindow)
						{
							glassWindow.Hide();
						}
					}
					PerformanceCoordinator? performance = _performance;
					if (performance == null || performance.State != PerformanceState.Sleeping)
					{
						BackdropRecovery.RetryBackground("PrimaryDisplayRebind");
					}
					if (!primaryDisplaySession.TryRebindPrimary(currentPrimary))
					{
						return;
					}
					flag = true;
				}
				_interaction?.RefreshDisplays();
				_performanceFullscreen?.Refresh();
				_dock?.RefreshDisplayPlacement();
				_statusBar?.RefreshDisplayPlacement();
				_launcher?.RefreshDisplayPlacement();
				SettingsWindow? settings = _settings;
				if (settings != null && settings.IsVisible)
				{
					_settings.PositionOnPrimary();
				}
				foreach (Window window2 in _application.Windows)
				{
					if (window2 is GlassWindow glassWindow2)
					{
						glassWindow2.ConstrainCurrentBoundsToPrimary();
					}
				}
				if (flag)
				{
					_dock?.ResumeAfterPrimaryDisplayChange(_restoreDockAfterDisplayChange);
					if (_restoreLauncherAfterDisplayChange)
					{
						ShowLauncher();
					}
					if (_restoreSettingsAfterDisplayChange)
					{
						ShowSettings();
					}
					_restoreDockAfterDisplayChange = (_restoreLauncherAfterDisplayChange = (_restoreSettingsAfterDisplayChange = false));
				}
			}
		});
	}

	private void DisposeInteractionController()
	{
		SystemEvents.DisplaySettingsChanged -= OnSystemDisplaySettingsChanged;
		SystemEvents.UserPreferenceChanged -= OnSystemUserPreferenceChanged;
		SystemEvents.PowerModeChanged -= OnBackgroundPowerModeChanged;
		SystemEvents.SessionSwitch -= OnBackgroundSessionSwitch;
		if (_launcher != null)
		{
			_launcher.NavigationAvailabilityChanged -= OnLauncherNavigationAvailabilityChanged;
		}
		if (_settings != null)
		{
			_settings.DataRestored -= OnSettingsDataRestored;
			_settings.UserDataDeletionRequested -= OnSettingsUserDataDeletionRequested;
			_settings.UninstallRequested -= OnSettingsUninstallRequested;
		}
		_interaction?.Dispose();
		_interaction = null;
	}

	private void OnSettingsUserDataDeletionRequested(object? sender, EventArgs e)
	{
		if (Interlocked.Exchange(ref _maintenanceStarted, 1) == 0)
		{
			string installDirectory = Path.GetDirectoryName(Environment.ProcessPath ?? string.Empty);
			UserDataMaintenanceSequence.Execute(delegate
			{
				_deferredCommandGate.Close();
				_shuttingDown = true;
			}, delegate
			{
				_settings?.PrepareForUserDataMaintenance();
			}, DisposeInteractionController, delegate
			{
				StopTrayRecovery();
				_tray?.Dispose();
				_tray = null;
			}, delegate
			{
				_services?.Dispose();
				_services = null;
			}, delegate
			{
				_messageWindow?.Dispose();
				_messageWindow = null;
			}, delegate
			{
				_userDataLease?.Dispose();
				_userDataLease = null;
			}, () => UserDataDeletionService.ForCurrentUser(installDirectory).DeleteAll(confirmed: true), delegate(Exception ex)
			{
				MessageBox.Show("未能安全停止 GlassDesk 的数据写入，因此没有删除数据。请重新启动后重试。\n\n" + ex.Message, "GlassDesk 数据未删除", MessageBoxButton.OK, MessageBoxImage.Hand);
			}, delegate(UserDataDeletionResult result)
			{
				var (messageBoxText, icon) = FormatDeletionResult(result);
				MessageBox.Show(messageBoxText, "GlassDesk 数据维护", MessageBoxButton.OK, icon);
			}, _application.Shutdown);
		}
	}

	private void OnSettingsUninstallRequested(object? sender, EventArgs e)
	{
		if (_shuttingDown || Volatile.Read(in _maintenanceStarted) != 0)
		{
			return;
		}
		InstallationInfo installationInfo = new InstallationLocator().Inspect(Environment.ProcessPath ?? string.Empty);
		if (installationInfo.State != InstallationState.VerifiedInstalled || string.IsNullOrWhiteSpace(installationInfo.UninstallerPath))
		{
			_settings?.AttachInstallation(installationInfo);
			return;
		}
		try
		{
			using Process process = Process.Start(new ProcessStartInfo(installationInfo.UninstallerPath)
			{
				UseShellExecute = true
			});
			if (process == null)
			{
				_settings?.ShowUninstallLaunchError("系统未能创建卸载程序进程。");
				return;
			}
			_deferredCommandGate.Close();
			_shuttingDown = true;
			_application.Shutdown();
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException || ex is Win32Exception) ? 1 : 0) != 0)
		{
			_settings?.ShowUninstallLaunchError(ex.Message);
		}
	}

	private static (string Message, MessageBoxImage Icon) FormatDeletionResult(UserDataDeletionResult result)
	{
		return result.Status switch
		{
			UserDataDeletionStatus.Deleted => (Message: "GlassDesk 用户数据已删除。程序即将退出；下次启动时会创建一份新的默认数据。", Icon: MessageBoxImage.Asterisk), 
			UserDataDeletionStatus.NotFound => (Message: "没有找到 GlassDesk 用户数据目录。程序即将退出。", Icon: MessageBoxImage.Asterisk), 
			UserDataDeletionStatus.Busy => (Message: result.Error ?? "数据仍被其它 GlassDesk 实例使用；本次没有删除任何数据。请退出其它实例后重试。", Icon: MessageBoxImage.Exclamation), 
			UserDataDeletionStatus.UnsafePath => (Message: "数据目录路径校验未通过，本次没有删除数据。\n\n" + result.Error, Icon: MessageBoxImage.Hand), 
			UserDataDeletionStatus.Residual => (Message: $"数据删除未能完整完成。已删除 {result.DeletedEntries} 个条目；剩余内容已隔离在以下目录，请检查并手动处理：\n\n{result.ResidualDirectory}\n\nGlassDesk 不会在下次启动时自动继续删除。\n\n{result.Error}", Icon: MessageBoxImage.Exclamation), 
			_ => (Message: "数据目录改名隔离失败；原数据保留，没有继续删除。\n\n" + result.Error, Icon: MessageBoxImage.Hand), 
		};
	}

	private void InitializeOnboarding()
	{
		_settings.OnboardingRequested += OnOnboardingRequested;
		if (_data.Settings.Current.OnboardingDismissed == false)
		{
			_application.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(ShowOnboarding));
		}
	}

	private void OnOnboardingRequested(object? sender, EventArgs e)
	{
		ShowOnboarding();
	}

	private void ShowOnboarding()
	{
		if (_disposed || _shuttingDown)
		{
			return;
		}
		PrimaryDisplaySession? primaryDisplaySession = _primaryDisplaySession;
		if (primaryDisplaySession == null || !primaryDisplaySession.IsAvailable || _data == null)
		{
			return;
		}
		if (_onboarding != null)
		{
			_onboarding.Activate();
			return;
		}
		OnboardingWindow window = new OnboardingWindow(_data.Settings)
		{
			Icon = CreateWindowIcon()
		};
		_onboarding = window;
		window.BindPrimaryDisplay(_primaryDisplaySession);
		window.SettingsPageRequested += delegate(object? _, string page)
		{
			ShowSettings();
			_settings?.NavigateToPage(page);
		};
		window.Closed += delegate
		{
			if (_onboarding == window)
			{
				_onboarding = null;
			}
		};
		window.PositionOnPrimary();
		window.Show();
		window.Activate();
	}

	private void DisposeOnboarding()
	{
		if (_settings != null)
		{
			_settings.OnboardingRequested -= OnOnboardingRequested;
		}
		_onboarding?.Close();
		_onboarding = null;
	}

	internal object InspectPerformance()
	{
		return new
		{
			State = RuntimePerformanceState.ToString(),
			Generation = RuntimePerformanceGeneration,
			SuppressionRequested = (_performance?.IsSuppressionRequested ?? false),
			ManualSleep = RuntimeManualGameSleep,
			TrackerPaused = (_services?.Running.IsSuspended ?? false),
			ReconcileCount = (_services?.Running.ReconcileCount ?? 0),
			FullscreenFallback = RuntimeFullscreenFallback,
			DockSleeping = (_dock?.IsPerformanceSleeping ?? false),
			StatusBarCaptureRequested = (_statusBar?.IsPerformanceCaptureRequested ?? false),
			LauncherSleeping = _launcherPerformanceSleeping
		};
	}

	private void InitializePerformanceCoordinator()
	{
		DateTimeOffset utcNow = DateTimeOffset.UtcNow;
		_performance = new PerformanceCoordinator(utcNow);
		_performance.SetAutoSuspend(_data.Settings.Current.Performance.AutoSuspendWhenFullscreen, utcNow);
		_performance.SetDesktopLayerPinned(_data.Settings.Current.Appearance.PinBarsToDesktop, utcNow);
		LauncherWindow? launcher = _launcher;
		_launcherHiddenSince = ((launcher != null && launcher.IsVisible) ? ((DateTimeOffset?)null) : new DateTimeOffset?(utcNow));
		_performanceDeadlineTimer = new DispatcherTimer(DispatcherPriority.Input, _application.Dispatcher);
		_performanceDeadlineTimer.Tick += delegate
		{
			_performanceDeadlineTimer.Stop();
			ReconcilePerformance();
		};
		_dock.PerformanceSurfaceVisibilityChanged += OnPerformanceVisibilityChanged;
		_statusBar.PerformanceSurfaceVisibilityChanged += OnPerformanceVisibilityChanged;
		_dock.IsVisibleChanged += OnPerformanceWindowVisibilityChanged;
		_launcher.IsVisibleChanged += OnPerformanceWindowVisibilityChanged;
		_settings.PerformanceSettingsChanged += OnPerformanceSettingsChanged;
		_performanceFullscreen = new PerformanceFullscreenMonitor(OnPerformanceForegroundObserved);
		ReconcilePerformance();
	}

	private void OnPerformanceForegroundObserved(PerformanceFullscreenObservation observation)
	{
		long sequence = Interlocked.Increment(ref _foregroundObservationSequence);
		if (_application.Dispatcher.HasShutdownStarted)
		{
			return;
		}
		_application.Dispatcher.BeginInvoke(DispatcherPriority.Input, (Action)delegate
		{
			if (!_disposed && !_shuttingDown && _performance != null && sequence == Volatile.Read(in _foregroundObservationSequence))
			{
				bool? isFullscreen = observation.IsFullscreen;
				if (isFullscreen.HasValue)
				{
					bool valueOrDefault = isFullscreen == true;
					_performance.ObserveForeground(valueOrDefault, observation.BelongsToCurrentProcess, DateTimeOffset.UtcNow);
				}
				ReconcilePerformance();
				if (!observation.BelongsToCurrentProcess)
				{
					_dock?.RequestGlassRefresh();
					_statusBar?.RequestGlassRefresh();
					_launcher?.RequestGlassRefresh();
				}
			}
		});
	}

	private void OnPerformanceVisibilityChanged(object? sender, EventArgs e)
	{
		ReconcilePerformance();
	}

	private void OnPerformanceWindowVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender == _launcher)
		{
			object newValue = e.NewValue;
			_launcherHiddenSince = ((newValue is bool && (bool)newValue) ? ((DateTimeOffset?)null) : new DateTimeOffset?(DateTimeOffset.UtcNow));
		}
		ReconcilePerformance();
	}

	private void OnPerformanceSettingsChanged(object? sender, EventArgs e)
	{
		_performance?.SetAutoSuspend(_data.Settings.Current.Performance.AutoSuspendWhenFullscreen, DateTimeOffset.UtcNow);
		_performance?.SetDesktopLayerPinned(_data.Settings.Current.Appearance.PinBarsToDesktop, DateTimeOffset.UtcNow);
		ReconcilePerformance();
	}

	internal void SetManualGameSleep(bool sleeping)
	{
		_application.Dispatcher.VerifyAccess();
		_performance?.SetManualSleep(sleeping, DateTimeOffset.UtcNow);
		ReconcilePerformance();
	}

	private void RequestPerformanceActivation()
	{
		_performanceActivationPending = true;
		_performance?.RequestTemporaryActivation(DateTimeOffset.UtcNow);
		ReconcilePerformance();
	}

	private void SetPerformanceSessionSuspended(bool suspended)
	{
		if (_application.Dispatcher.HasShutdownStarted)
		{
			return;
		}
		_application.Dispatcher.BeginInvoke((Action)delegate
		{
			if (!_disposed && _performance != null)
			{
				_performance.SetSessionSuspended(suspended, DateTimeOffset.UtcNow);
				if (!suspended)
				{
					_performanceFullscreen?.Refresh();
				}
				ReconcilePerformance();
			}
		});
	}

	private void ReconcilePerformance()
	{
		if (_performance == null || _disposed || _shuttingDown)
		{
			return;
		}
		if (_applyingPerformance)
		{
			_performanceApplyPending = true;
			return;
		}
		_applyingPerformance = true;
		try
		{
			do
			{
				_performanceApplyPending = false;
				DateTimeOffset utcNow = DateTimeOffset.UtcNow;
				PerformanceCoordinator? performance = _performance;
				DockWindow? dock = _dock;
				bool dockVisible = (dock != null && dock.IsPerformanceCaptureRequested) || (_statusBar?.IsPerformanceCaptureRequested ?? false);
				LauncherWindow? launcher = _launcher;
				performance.SetVisibleSurfaces(dockVisible, (launcher != null && launcher.IsVisible) || _performanceActivationPending, utcNow);
				PerformanceState state = _performance.State;
				bool flag = state == PerformanceState.Sleeping;
				bool isSuppressionRequested = _performance.IsSuppressionRequested;
				bool isTemporaryActivation = _performance.IsTemporaryActivation;
				_dock?.SetPerformanceSleeping(isSuppressionRequested);
				_statusBar?.SetPerformanceSleeping(isSuppressionRequested);
				_dock?.SetBackgroundCaptureSleeping(flag && !isSuppressionRequested);
				if (flag && isSuppressionRequested)
				{
					LauncherWindow? launcher2 = _launcher;
					if (launcher2 != null && launcher2.IsVisible && !_launcher.IsClosingContainer)
					{
						HideLauncher();
					}
				}
				bool flag2 = _launcher?.IsVisible ?? false;
				_performancePointerInputSuppressed = isSuppressionRequested && !flag2;
				int num;
				if (!flag2 && !isTemporaryActivation)
				{
					if (!isSuppressionRequested)
					{
						DateTimeOffset? launcherHiddenSince = _launcherHiddenSince;
						if (launcherHiddenSince.HasValue)
						{
							DateTimeOffset valueOrDefault = launcherHiddenSince.GetValueOrDefault();
							num = ((utcNow >= valueOrDefault + LauncherWarmRetention) ? 1 : 0);
						}
						else
						{
							num = 0;
						}
					}
					else
					{
						num = 1;
					}
				}
				else
				{
					num = 0;
				}
				bool flag3 = (byte)num != 0;
				int num2;
				if (!flag2)
				{
					DateTimeOffset? launcherHiddenSince = _launcherHiddenSince;
					if (launcherHiddenSince.HasValue)
					{
						DateTimeOffset valueOrDefault2 = launcherHiddenSince.GetValueOrDefault();
						num2 = ((utcNow < valueOrDefault2 + LauncherWarmRetention) ? 1 : 0);
						goto IL_01c0;
					}
				}
				num2 = 0;
				goto IL_01c0;
				IL_01c0:
				bool flag4 = (byte)num2 != 0;
				if (_launcherPerformanceSleeping != flag3)
				{
					_launcherPerformanceSleeping = flag3;
					_launcher?.SetPerformanceSleeping(flag3);
				}
				_launcher?.SetBackgroundWorkSuspended((!flag2 && !isTemporaryActivation && !flag4) || (flag && isSuppressionRequested));
				_services?.Running.SetSuspended(flag || state == PerformanceState.WarmHidden || (isSuppressionRequested && !flag2));
				if (!flag && _libraryDirtyWhileSleeping)
				{
					_libraryDirtyWhileSleeping = false;
					RefreshLibraryForPerformance();
				}
				if (!flag && _launcherLibraryDirty && (flag2 || _performanceActivationPending))
				{
					_launcherLibraryDirty = false;
					_launcher?.RefreshLibrary();
				}
				if (state == PerformanceState.Waking)
				{
					_performance.CompleteWake(utcNow);
				}
				SchedulePerformanceDeadline(utcNow);
			}
			while (_performanceApplyPending);
		}
		finally
		{
			_applyingPerformance = false;
		}
	}

	private void SchedulePerformanceDeadline(DateTimeOffset now)
	{
		_performanceDeadlineTimer.Stop();
		DateTimeOffset? dateTimeOffset = _performance.NextTransitionAt;
		if (!_launcherPerformanceSleeping && !_performance.IsTemporaryActivation)
		{
			DateTimeOffset? launcherHiddenSince = _launcherHiddenSince;
			if (launcherHiddenSince.HasValue)
			{
				DateTimeOffset valueOrDefault = launcherHiddenSince.GetValueOrDefault();
				DateTimeOffset dateTimeOffset2 = valueOrDefault + LauncherWarmRetention;
				if (dateTimeOffset.HasValue)
				{
					DateTimeOffset value = dateTimeOffset2;
					launcherHiddenSince = dateTimeOffset;
					if (!(value < launcherHiddenSince))
					{
						goto IL_0085;
					}
				}
				dateTimeOffset = dateTimeOffset2;
			}
		}
		goto IL_0085;
		IL_0085:
		if (dateTimeOffset.HasValue)
		{
			DateTimeOffset valueOrDefault2 = dateTimeOffset.GetValueOrDefault();
			_performanceDeadlineTimer.Interval = ((valueOrDefault2 > now) ? (valueOrDefault2 - now) : TimeSpan.FromMilliseconds(1L));
			_performanceDeadlineTimer.Start();
		}
	}

	private void RefreshLibraryForPerformance()
	{
		if (_disposed || _shuttingDown)
		{
			return;
		}
		PerformanceCoordinator? performance = _performance;
		if (performance != null && performance.State == PerformanceState.Sleeping)
		{
			_libraryDirtyWhileSleeping = true;
			return;
		}
		_dock?.RefreshTiles();
		LauncherWindow? launcher = _launcher;
		if ((launcher != null && launcher.IsVisible) || _performanceActivationPending)
		{
			_launcher?.RefreshLibrary();
		}
		else
		{
			_launcherLibraryDirty = true;
		}
	}

	private void DisposePerformanceCoordinator()
	{
		_performanceDeadlineTimer?.Stop();
		_performanceDeadlineTimer = null;
		_performanceFullscreen?.Dispose();
		_performanceFullscreen = null;
		if (_dock != null)
		{
			_dock.PerformanceSurfaceVisibilityChanged -= OnPerformanceVisibilityChanged;
			_dock.IsVisibleChanged -= OnPerformanceWindowVisibilityChanged;
		}
		if (_launcher != null)
		{
			_launcher.IsVisibleChanged -= OnPerformanceWindowVisibilityChanged;
		}
		if (_statusBar != null)
		{
			_statusBar.PerformanceSurfaceVisibilityChanged -= OnPerformanceVisibilityChanged;
		}
		if (_settings != null)
		{
			_settings.PerformanceSettingsChanged -= OnPerformanceSettingsChanged;
		}
		_performance = null;
	}

	private void StartTrayRecovery()
	{
		StopTrayRecovery();
		DispatcherTimer dispatcherTimer = new DispatcherTimer(DispatcherPriority.Background, _application.Dispatcher);
		TrayIcon? tray = _tray;
		dispatcherTimer.Interval = TimeSpan.FromSeconds((tray != null && tray.IsVisible) ? 30 : 2);
		_trayRecoveryTimer = dispatcherTimer;
		_trayRecoveryTimer.Tick += OnTrayRecoveryTick;
		_trayRecoveryTimer.Start();
	}

	private void OnTrayRecoveryTick(object? sender, EventArgs e)
	{
		RefreshTrayRegistration();
	}

	private void RefreshTrayRegistration()
	{
		if (!_shuttingDown && !_disposed && _tray != null)
		{
			bool flag = _tray.EnsureVisible();
			if (_trayRecoveryTimer != null)
			{
				_trayRecoveryTimer.Interval = TimeSpan.FromSeconds(flag ? 30 : 2);
			}
		}
	}

	private void StopTrayRecovery()
	{
		if (_trayRecoveryTimer != null)
		{
			_trayRecoveryTimer.Stop();
			_trayRecoveryTimer.Tick -= OnTrayRecoveryTick;
			_trayRecoveryTimer = null;
		}
	}

	private void InitializeUpdates()
	{
		_settings.CheckForUpdatesRequested += async delegate
		{
			await CheckUpdatesAsync(manual: true);
		};
		_settings.CloudDownloadsRequested += delegate
		{
			ShowCloudDownloads();
		};
		_settings.ShowUpdateStatus(ProductVersion.ToString(), "启动时会检查新版本；也可直接点击“网盘下载”，选择网盘转存或下载。");
		if (_options.Sample == AppearanceSample.None && new InstallationLocator().Inspect(Environment.ProcessPath ?? string.Empty).State == InstallationState.VerifiedInstalled)
		{
			_application.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)async delegate
			{
				await CheckUpdatesAsync(manual: false);
			});
		}
	}

	private UpdateClient CreateUpdateClient()
	{
		string text = Path.Combine(AppContext.BaseDirectory, "update-source.json");
		string uriString = "https://api.github.com/repos/everlosttime/glassdesk-updates/releases/latest";
		if (File.Exists(text))
		{
			if (new FileInfo(text).Length > 8192)
			{
				throw new InvalidDataException("更新源配置过大。");
			}
			using JsonDocument jsonDocument = JsonDocument.Parse(File.ReadAllText(text));
			uriString = jsonDocument.RootElement.GetProperty("releasesApi").GetString() ?? throw new InvalidDataException("更新源未配置。");
		}
		return new UpdateClient(_updateHttp, new Uri(uriString));
	}

	private async Task CheckUpdatesAsync(bool manual)
	{
		if (_shuttingDown || _checkingUpdate)
		{
			return;
		}
		_checkingUpdate = true;
		_settings?.ShowUpdateStatus(ProductVersion.ToString(), "正在检查更新…", checking: true);
		try
		{
			UpdateNotice updateNotice = await CreateUpdateClient().CheckAsync(ProductVersion, _updateLifetime.Token);
			if (_shuttingDown || _updateLifetime.IsCancellationRequested)
			{
				return;
			}
			if ((object)updateNotice == null)
			{
				_settings?.ShowUpdateStatus(ProductVersion.ToString(), "当前已是最新正式版本。");
				return;
			}
			_settings?.ShowUpdateStatus(ProductVersion.ToString(), $"发现新版本 {updateNotice.Version}。");
			if (_data.Settings.Current.ShouldShowUpdateNotice(manual, DateTimeOffset.UtcNow))
			{
				ShowCloudDownloads(updateNotice);
			}
		}
		catch (OperationCanceledException)
		{
			if (!_shuttingDown)
			{
				_settings?.ShowUpdateStatus(ProductVersion.ToString(), "检查更新已取消或超时，可重试或直接点击“网盘下载”。");
			}
		}
		catch (Exception ex2)
		{
			if (!_shuttingDown)
			{
				_settings?.ShowUpdateStatus(ProductVersion.ToString(), "无法检查更新，仍可点击“网盘下载”选择下载渠道。" + (manual ? ("\n" + ex2.Message) : string.Empty));
			}
		}
		finally
		{
			_checkingUpdate = false;
		}
	}

	private void ShowCloudDownloads(UpdateNotice? release = null)
	{
		if (_shuttingDown)
		{
			return;
		}
		if (_updateWindow != null && (object)release == null)
		{
			_updateWindow.Activate();
			return;
		}
		try
		{
			CloudDownloadWindow next = new CloudDownloadWindow(ReadCloudDownloadConfiguration(), release?.Version, release?.Notes, null, null, SnoozeUpdateNotice)
			{
				Icon = _settings?.Icon
			};
			next.Closed += delegate
			{
				if (_updateWindow == next)
				{
					_updateWindow = null;
				}
			};
			_updateWindow?.Close();
			_updateWindow = next;
			next.Show();
		}
		catch (Exception ex)
		{
			_settings?.ShowUpdateStatus(ProductVersion.ToString(), "无法显示网盘下载入口：" + ex.Message);
		}
	}

	private bool SnoozeUpdateNotice()
	{
		SettingsStore settings = _data.Settings;
		DateTimeOffset? updateNoticeSnoozedUntilUtc = settings.Current.UpdateNoticeSnoozedUntilUtc;
		try
		{
			settings.Current.UpdateNoticeSnoozedUntilUtc = DateTimeOffset.UtcNow.AddDays(7.0);
			settings.Save();
			return true;
		}
		catch
		{
			settings.Current.UpdateNoticeSnoozedUntilUtc = updateNoticeSnoozedUntilUtc;
			return false;
		}
	}

	private static string ReadCloudDownloadConfiguration()
	{
		string text = Path.Combine(AppContext.BaseDirectory, "update-source.json");
		if (File.Exists(text))
		{
			if (new FileInfo(text).Length > 8192)
			{
				throw new InvalidDataException("网盘下载配置过大。");
			}
			string text2 = File.ReadAllText(text);
			using JsonDocument jsonDocument = JsonDocument.Parse(text2);
			if (jsonDocument.RootElement.TryGetProperty("cloudDownloads", out var _))
			{
				return text2;
			}
		}
		using Stream stream = typeof(AppHost).Assembly.GetManifestResourceStream("GlassDesk.UpdateSourceDefaults.json") ?? throw new InvalidDataException("未找到网盘下载配置。");
		using StreamReader streamReader = new StreamReader(stream);
		return streamReader.ReadToEnd();
	}

	private void DisposeUpdates()
	{
		_updateLifetime.Cancel();
		_updateWindow?.Close();
		_updateHttp.Dispose();
		_updateLifetime.Dispose();
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.1.0.9782' (yours is '9.1.0.7988')
