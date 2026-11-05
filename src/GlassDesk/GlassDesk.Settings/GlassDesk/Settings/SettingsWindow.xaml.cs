using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GlassDesk.Core.Input;
using GlassDesk.Core.Models;
using GlassDesk.Core.Models.Settings;
using GlassDesk.Core.Storage;
using GlassDesk.Platform;
using GlassDesk.Platform.Displays;
using GlassDesk.Platform.Installation;
using GlassDesk.Platform.Startup;
using GlassDesk.Settings.Repair;
using GlassDesk.UI.Controls;
using GlassDesk.UI.Settings;
using GlassDesk.UI.Theming;
using Microsoft.Win32;

namespace GlassDesk.Settings;

public partial class SettingsWindow : GlassWindow, IComponentConnector
{
	private bool _userDataDeletionPending;

	private InstallationInfo? _installationInfo;

	private Func<bool?>? _desktopIconsRead;

	private Func<bool, string?>? _desktopIconsSet;

	private GuidedTour? _onboardingGuide;

	private bool _loadingPerformanceControls = true;

	private ImageSource? _alipaySponsorCode;

	private ImageSource? _wechatSponsorCode;

	public static readonly DependencyProperty PlannedPagesProperty = DependencyProperty.Register("PlannedPages", typeof(IReadOnlyList<string>), typeof(SettingsWindow), new PropertyMetadata(Array.Empty<string>()));

	private GlassDeskDataStore? _dataStore;

	private StartupRegistration? _startupRegistration;

	private string _startupExecutablePath = string.Empty;

	private bool _loadingStartupRegistration;

	private bool _loadingDockControls;

	private bool _loadingLauncherControls;

	private bool _loadingAppearanceControls;

	private bool _dockValuesOutOfRange;

	private bool _launcherValuesOutOfRange;

	private readonly FrameworkElement[] _sectionHeadings;

	private bool _syncingCategoryFromScroll;

	private bool _sectionNavigationPending;

	private int _sectionNavigationVersion;

	public Func<ShortcutSettings, DisplaySettings, string?>? InteractionSettingsApply { get; set; }

	public Func<bool?>? DesktopIconsRead
	{
		get
		{
			return _desktopIconsRead;
		}
		set
		{
			_desktopIconsRead = value;
			if (IsInitialized)
			{
				LoadDesktopStatus();
			}
		}
	}

	public Func<bool, string?>? DesktopIconsSet
	{
		get
		{
			return _desktopIconsSet;
		}
		set
		{
			_desktopIconsSet = value;
			if (IsInitialized)
			{
				LoadDesktopStatus();
			}
		}
	}

	protected override bool UseOpaquePanel => true;

	private bool CanResetSelectedPage
	{
		get
		{
			string text = SettingsPageList.SelectedItem as string;
			bool flag = text != null;
			if (flag)
			{
				bool flag2;
				switch (text)
				{
				case "外观":
				case "Dock":
				case "启动台":
				case "快捷操作":
				case "性能":
				case "桌面":
					flag2 = true;
					break;
				default:
					flag2 = false;
					break;
				}
				flag = flag2;
			}
			return flag;
		}
	}

	public IReadOnlyList<string> PlannedPages
	{
		get
		{
			return (IReadOnlyList<string>)GetValue(PlannedPagesProperty);
		}
		private set
		{
			SetValue(PlannedPagesProperty, value);
		}
	}

	public event EventHandler? DataRestored;

	public event EventHandler? UserDataDeletionRequested;

	public event EventHandler? UninstallRequested;

	public event EventHandler? OnboardingRequested;

	public event EventHandler? PerformanceSettingsChanged;

	public event EventHandler? CheckForUpdatesRequested;

	public event EventHandler? CloudDownloadsRequested;

	public event EventHandler<LauncherSettings>? LauncherSettingsChanged;

	public void AttachInstallation(InstallationInfo installationInfo)
	{
		ArgumentNullException.ThrowIfNull(installationInfo, "installationInfo");
		_installationInfo = installationInfo;
		UninstallButton.IsEnabled = installationInfo.State == InstallationState.VerifiedInstalled;
		TextBlock installationStatus = InstallationStatus;
		installationStatus.Text = installationInfo.State switch
		{
			InstallationState.Portable => "当前程序以便携方式运行。退出后可自行删除程序文件夹；用户数据需在“备份与恢复”页单独管理。", 
			InstallationState.VerifiedInstalled => "已核验此安装及卸载程序：" + installationInfo.UninstallerPath + "\n卸载程序不会删除 GlassDesk 用户数据。", 
			_ => "安装登记不完整或与当前程序不匹配，已停用卸载入口。" + Environment.NewLine + installationInfo.Error, 
		};
	}

	public void RefreshInstallationStatus(Func<InstallationInfo> inspect)
	{
		ArgumentNullException.ThrowIfNull(inspect, "inspect");
		AttachInstallation(inspect());
	}

	public void ShowUninstallLaunchError(string message)
	{
		InstallationStatus.Text = "无法启动已核验的卸载程序：" + message;
		Button uninstallButton = UninstallButton;
		InstallationInfo? installationInfo = _installationInfo;
		uninstallButton.IsEnabled = (object)installationInfo != null && installationInfo.State == InstallationState.VerifiedInstalled;
	}

	public void PrepareForUserDataMaintenance()
	{
		SettingsScroll.IsEnabled = false;
		SettingsPageList.IsEnabled = false;
		ResetPageButton.IsEnabled = false;
		_dataStore = null;
	}

	public void ShowInteractionStatus(string message)
	{
		InteractionSettingsStatus.Text = message ?? string.Empty;
	}

	private void LoadAppearanceControls()
	{
		if (_dataStore == null)
		{
			return;
		}
		_loadingAppearanceControls = true;
		try
		{
			AppearanceSettings appearance = _dataStore.Settings.Current.Appearance;
			ComboBox appearanceMaterialCombo = AppearanceMaterialCombo;
			appearanceMaterialCombo.SelectedIndex = appearance.GlassMaterial switch
			{
				GlassMaterialMode.FrostedGlass => 1, 
				GlassMaterialMode.PlainGlass => 2, 
				_ => 0, 
			};
			DesktopLayerModeCombo.SelectedIndex = (appearance.PinBarsToDesktop ? 1 : 0);
			LaunchpadIconStyleCombo.SelectedIndex = ((!appearance.LaunchpadIconFilled) ? 1 : 0);
			DockTransparencySlider.Value = appearance.DockGlassTransparency;
			LauncherTransparencySlider.Value = appearance.LauncherGlassTransparency;
			DockTransparencyValue.Text = appearance.DockGlassTransparency.ToString("P0", CultureInfo.CurrentCulture);
			LauncherTransparencyValue.Text = appearance.LauncherGlassTransparency.ToString("P0", CultureInfo.CurrentCulture);
			DockBuiltinIconBrightnessSlider.Value = appearance.DockBuiltinIconBrightness;
			LauncherTextBrightnessSlider.Value = appearance.LauncherTextBrightness;
			StatusBarTextBrightnessSlider.Value = appearance.StatusBarTextBrightness;
			StatusBarTextBrightnessValue.Text = appearance.StatusBarTextBrightness.ToString("P0", CultureInfo.CurrentCulture);
			DockBuiltinIconBrightnessValue.Text = appearance.DockBuiltinIconBrightness.ToString("P0", CultureInfo.CurrentCulture);
			LauncherTextBrightnessValue.Text = appearance.LauncherTextBrightness.ToString("P0", CultureInfo.CurrentCulture);
			ReduceMotionCheck.IsChecked = appearance.ReduceMotion;
			AppearanceStatus.Text = "设置面板固定使用深色不透明背景；玻璃透明度应用到 Dock 和启动台。";
		}
		finally
		{
			_loadingAppearanceControls = false;
		}
	}

	private void OnAppearanceControlChanged(object sender, RoutedEventArgs e)
	{
		if (!_loadingAppearanceControls && _dataStore != null)
		{
			GlassDeskSettings current = _dataStore.Settings.Current;
			GlassDeskSettings snapshot = SettingsPageEditor.Clone(current);
			AppearanceSettings appearance = current.Appearance;
			appearance.GlassMaterial = AppearanceMaterialCombo.SelectedIndex switch
			{
				1 => GlassMaterialMode.FrostedGlass, 
				2 => GlassMaterialMode.PlainGlass, 
				_ => GlassMaterialMode.AndroidLiquidGlass, 
			};
			current.Appearance.ReduceMotion = ReduceMotionCheck.IsChecked == true;
			current.Appearance.PinBarsToDesktop = DesktopLayerModeCombo.SelectedIndex == 1;
			current.Appearance.LaunchpadIconFilled = LaunchpadIconStyleCombo.SelectedIndex != 1;
			SavePageSnapshot(snapshot, "外观", AppearanceStatus);
			ThemeService current2 = ThemeService.Current;
			if (current2 != null && current2.Appearance != current.Appearance)
			{
				current2.Update(current.Appearance);
			}
		}
	}

	private void OnAppearanceSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		if (_loadingAppearanceControls || !IsInitialized || _dataStore == null)
		{
			return;
		}
		GlassDeskSettings current = _dataStore.Settings.Current;
		GlassDeskSettings snapshot = SettingsPageEditor.Clone(current);
		if (sender == DockTransparencySlider)
		{
			current.Appearance.DockGlassTransparency = DockTransparencySlider.Value;
		}
		else if (sender == LauncherTransparencySlider)
		{
			current.Appearance.LauncherGlassTransparency = LauncherTransparencySlider.Value;
		}
		else if (sender == DockBuiltinIconBrightnessSlider)
		{
			current.Appearance.DockBuiltinIconBrightness = DockBuiltinIconBrightnessSlider.Value;
		}
		else if (sender == LauncherTextBrightnessSlider)
		{
			current.Appearance.LauncherTextBrightness = LauncherTextBrightnessSlider.Value;
		}
		else
		{
			if (sender != StatusBarTextBrightnessSlider)
			{
				return;
			}
			current.Appearance.StatusBarTextBrightness = StatusBarTextBrightnessSlider.Value;
		}
		SavePageSnapshot(snapshot, (sender == StatusBarTextBrightnessSlider) ? "信息栏" : "外观", (sender == StatusBarTextBrightnessSlider) ? InformationBarSettingsStatus : AppearanceStatus);
		ThemeService current2 = ThemeService.Current;
		if (current2 != null && current2.Appearance != current.Appearance)
		{
			current2.Update(current.Appearance);
		}
		DockTransparencyValue.Text = current.Appearance.DockGlassTransparency.ToString("P0", CultureInfo.CurrentCulture);
		LauncherTransparencyValue.Text = current.Appearance.LauncherGlassTransparency.ToString("P0", CultureInfo.CurrentCulture);
		DockBuiltinIconBrightnessValue.Text = current.Appearance.DockBuiltinIconBrightness.ToString("P0", CultureInfo.CurrentCulture);
		LauncherTextBrightnessValue.Text = current.Appearance.LauncherTextBrightness.ToString("P0", CultureInfo.CurrentCulture);
		StatusBarTextBrightnessValue.Text = current.Appearance.StatusBarTextBrightness.ToString("P0", CultureInfo.CurrentCulture);
	}

	private void LoadInteractionControls()
	{
		if (_dataStore == null)
		{
			return;
		}
		try
		{
			GlassDeskSettings current = _dataStore.Settings.Current;
			ToggleLauncherHotkeyBox.Text = HotkeyText.Format(current.Shortcuts.ToggleLauncher);
			NextPageHotkeyBox.Text = HotkeyText.Format(current.Shortcuts.NextPage);
			PreviousPageHotkeyBox.Text = HotkeyText.Format(current.Shortcuts.PreviousPage);
			ToggleDesktopIconsHotkeyBox.Text = HotkeyText.Format(current.Shortcuts.ToggleDesktopIcons);
			ToggleDockRevealLockHotkeyBox.Text = HotkeyText.Format(current.Shortcuts.ToggleDockRevealLock);
			ToggleGameSleepHotkeyBox.Text = HotkeyText.Format(current.Shortcuts.ToggleGameSleep);
			InteractionSettingsStatus.Text = "快捷键录制完成后自动保存并应用。";
		}
		catch (Exception ex)
		{
			InteractionSettingsStatus.Text = "读取快捷操作设置失败：" + ex.Message;
		}
	}

	private void OnApplyInteractionSettingsClick(object sender, RoutedEventArgs e)
	{
		if (_dataStore == null)
		{
			InteractionSettingsStatus.Text = "设置数据尚未加载。";
		}
		else if (InteractionSettingsApply == null)
		{
			InteractionSettingsStatus.Text = "运行服务未连接，未保存更改。";
		}
		else
		{
			if (!TryReadHotkey(ToggleLauncherHotkeyBox, out HotkeyDefinition value) || !TryReadHotkey(NextPageHotkeyBox, out HotkeyDefinition value2) || !TryReadHotkey(PreviousPageHotkeyBox, out HotkeyDefinition value3) || !TryReadHotkey(ToggleDesktopIconsHotkeyBox, out HotkeyDefinition value4) || !TryReadHotkey(ToggleDockRevealLockHotkeyBox, out HotkeyDefinition value5) || !TryReadHotkey(ToggleGameSleepHotkeyBox, out HotkeyDefinition value6))
			{
				return;
			}
			ShortcutSettings shortcutSettings = new ShortcutSettings
			{
				ToggleLauncher = value,
				HideLauncher = new HotkeyDefinition(),
				NextPage = value2,
				PreviousPage = value3,
				ToggleDesktopIcons = value4,
				ToggleDockRevealLock = value5,
				ToggleGameSleep = value6
			};
			DisplaySettings displaySettings = new DisplaySettings
			{
				DockDisplayId = _dataStore.Settings.Current.Display.DockDisplayId,
				LauncherFollowsPointer = _dataStore.Settings.Current.Display.LauncherFollowsPointer,
				SuppressTriggersWhenFullscreen = _dataStore.Settings.Current.Display.SuppressTriggersWhenFullscreen
			};
			GlassDeskSettings glassDeskSettings = SettingsPageEditor.Clone(_dataStore.Settings.Current);
			glassDeskSettings.Shortcuts = shortcutSettings;
			glassDeskSettings.Display = displaySettings;
			string text = SettingsPageEditor.Validate(glassDeskSettings);
			if (text == null)
			{
				try
				{
					string text2 = InteractionSettingsApply(shortcutSettings, displaySettings);
					if (text2 != null)
					{
						LoadInteractionControls();
						InteractionSettingsStatus.Text = "应用失败，已恢复原设置：" + text2;
					}
					else
					{
						LoadInteractionControls();
						InteractionSettingsStatus.Text = "快捷操作设置已应用并保存。";
					}
					return;
				}
				catch (Exception ex)
				{
					LoadInteractionControls();
					InteractionSettingsStatus.Text = "应用失败，已恢复原设置：" + ex.Message;
					return;
				}
			}
			LoadInteractionControls();
			InteractionSettingsStatus.Text = "设置无效：" + text;
		}
	}

	private void OnShortcutRecordingCompleted(object? sender, EventArgs e)
	{
		if ((_dataStore == null || InteractionSettingsApply == null) && sender is ShortcutRecorder shortcutRecorder)
		{
			shortcutRecorder.RestorePreviousBinding();
		}
		OnApplyInteractionSettingsClick(sender ?? this, new RoutedEventArgs());
	}

	public bool TryCaptureRegisteredHotkey(nint packedChord)
	{
		ShortcutRecorder shortcutRecorder = new ShortcutRecorder[6] { ToggleLauncherHotkeyBox, NextPageHotkeyBox, PreviousPageHotkeyBox, ToggleDesktopIconsHotkeyBox, ToggleDockRevealLockHotkeyBox, ToggleGameSleepHotkeyBox }.FirstOrDefault((ShortcutRecorder control) => control.IsRecording && control.IsKeyboardFocusWithin);
		if (shortcutRecorder == null)
		{
			return false;
		}
		uint num = (uint)((IntPtr)packedChord).ToInt64();
		ModifierKeys modifierKeys = ModifierKeys.None;
		if ((num & 1) != 0)
		{
			modifierKeys |= ModifierKeys.Alt;
		}
		if ((num & 2) != 0)
		{
			modifierKeys |= ModifierKeys.Control;
		}
		if ((num & 4) != 0)
		{
			modifierKeys |= ModifierKeys.Shift;
		}
		if ((num & 8) != 0)
		{
			modifierKeys |= ModifierKeys.Windows;
		}
		int num2 = (int)(num >> 16);
		if ((uint)(num2 - 5) <= 1u)
		{
			shortcutRecorder.CaptureMouseButton((num2 == 5) ? 1 : 2, modifierKeys);
		}
		else if ((uint)(num2 - 256) <= 1u)
		{
			shortcutRecorder.CaptureWheel((num2 == 256) ? 120 : (-120), modifierKeys);
		}
		else
		{
			shortcutRecorder.CaptureKey(KeyInterop.KeyFromVirtualKey(num2), modifierKeys);
		}
		return true;
	}

	private bool TryReadHotkey(ShortcutRecorder box, out HotkeyDefinition value)
	{
		if (HotkeyText.TryParse(box.Text, out value, out string error))
		{
			return true;
		}
		InteractionSettingsStatus.Text = AutomationProperties.GetName(box) + "无法识别：" + error;
		box.Focus();
		return false;
	}

	private void LoadDesktopStatus()
	{
		bool? flag;
		try
		{
			flag = DesktopIconsRead?.Invoke();
		}
		catch (Exception ex)
		{
			DesktopIconsToggleButton.IsEnabled = false;
			DesktopSettingsStatus.Text = "读取桌面图标状态失败：" + ex.Message;
			return;
		}
		DesktopIconsToggleButton.IsEnabled = flag.HasValue && DesktopIconsSet != null;
		DesktopSettingsStatus.Text = (flag.HasValue ? ("当前桌面图标：" + (flag.Value ? "显示" : "隐藏") + "。") : "当前环境不支持读取或切换桌面图标。");
	}

	private void OnDesktopIconsToggleClick(object sender, RoutedEventArgs e)
	{
		bool? flag;
		try
		{
			flag = DesktopIconsRead?.Invoke();
		}
		catch (Exception ex)
		{
			DesktopSettingsStatus.Text = "读取桌面图标状态失败：" + ex.Message;
			return;
		}
		if (!flag.HasValue || DesktopIconsSet == null)
		{
			LoadDesktopStatus();
			return;
		}
		string text;
		try
		{
			text = DesktopIconsSet(!flag.Value);
		}
		catch (Exception ex2)
		{
			text = ex2.Message;
		}
		if (text != null)
		{
			DesktopSettingsStatus.Text = "桌面图标设置失败：" + text;
		}
		else
		{
			LoadDesktopStatus();
		}
	}

	private void OnCreateBackupClick(object sender, RoutedEventArgs e)
	{
		if (_dataStore == null)
		{
			BackupStatus.Text = "设置数据尚未加载。";
			return;
		}
		OpenFolderDialog openFolderDialog = new OpenFolderDialog
		{
			Title = "选择 GlassDesk 备份保存文件夹",
			Multiselect = false
		};
		if (openFolderDialog.ShowDialog(this) == true)
		{
			TransferResult transferResult = _dataStore.Backup.BackupTo(openFolderDialog.FolderName);
			BackupStatus.Text = (transferResult.Succeeded ? $"备份已创建：{transferResult.Directory}（{transferResult.Files.Count} 个文件）。" : (transferResult.Error ?? "备份未能完成。"));
		}
	}

	private void OnDeleteUserDataClick(object sender, RoutedEventArgs e)
	{
		if (_userDataDeletionPending || _dataStore == null)
		{
			return;
		}
		if (UserDataDeletionRequested == null)
		{
			BackupStatus.Text = "运行宿主未连接安全维护流程，未执行删除。";
			return;
		}
		string rootDirectory = _dataStore.Paths.RootDirectory;
		if (MessageBox.Show(this, "将删除以下目录中的全部 GlassDesk 用户数据：\n\n" + rootDirectory + "\n\n包括设置、应用库、布局、自定义图标、内部备份和日志。不会卸载程序、删除此目录之外的导入文件或备份，也不会更改登录启动项。建议先使用上方“创建备份”并把备份保存到此目录之外。\n\n确认后 GlassDesk 将停止并退出。要继续吗？", "删除全部 GlassDesk 数据", MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.No) == MessageBoxResult.Yes)
		{
			_userDataDeletionPending = true;
			DeleteUserDataButton.IsEnabled = false;
			PrepareForUserDataMaintenance();
			UserDataDeletionRequested(this, EventArgs.Empty);
		}
	}

	private void OnUninstallClick(object sender, RoutedEventArgs e)
	{
		InstallationInfo? installationInfo = _installationInfo;
		if ((object)installationInfo != null && installationInfo.State == InstallationState.VerifiedInstalled && !_userDataDeletionPending)
		{
			if (UninstallRequested == null)
			{
				ShowUninstallLaunchError("运行宿主未连接卸载流程。");
			}
			else if (MessageBox.Show(this, "将启动已核验的 GlassDesk 卸载程序：\n\n" + _installationInfo.UninstallerPath + "\n\n卸载仅移除已安装的程序文件，不会删除 GlassDesk 用户数据。要继续吗？", "卸载 GlassDesk", MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.No) == MessageBoxResult.Yes)
			{
				UninstallButton.IsEnabled = false;
				UninstallRequested(this, EventArgs.Empty);
			}
		}
	}

	private void OnRestoreBackupClick(object sender, RoutedEventArgs e)
	{
		if (_dataStore == null)
		{
			BackupStatus.Text = "设置数据尚未加载。";
			return;
		}
		OpenFolderDialog openFolderDialog = new OpenFolderDialog
		{
			Title = "选择 GlassDesk 备份文件夹",
			Multiselect = false
		};
		if (openFolderDialog.ShowDialog(this) != true)
		{
			return;
		}
		if (DataRestored == null)
		{
			BackupStatus.Text = "运行宿主未连接安全退出通知，未执行恢复。";
		}
		else if (MessageBox.Show(this, "恢复会用所选备份替换当前 GlassDesk 数据。系统会先保存当前数据副本。恢复后请重新启动 GlassDesk 以载入全部内容。要继续吗？", "恢复 GlassDesk 备份", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
		{
			TransferResult transferResult = _dataStore.Backup.RestoreFrom(openFolderDialog.FolderName);
			if (!transferResult.Succeeded)
			{
				BackupStatus.Text = transferResult.Error ?? "恢复未能完成。";
				return;
			}
			BackupStatus.Text = $"已从备份恢复 {transferResult.Files.Count} 个文件。当前数据的安全副本位于：{transferResult.SafetyCopyDirectory}。请重新启动 GlassDesk。为避免写回恢复前的内存设置，本窗口的设置编辑已停用。";
			SettingsScroll.IsEnabled = false;
			SettingsPageList.IsEnabled = false;
			ResetPageButton.IsEnabled = false;
			_dataStore = null;
			DataRestored?.Invoke(this, EventArgs.Empty);
		}
	}

	private void OnResetPageClick(object sender, RoutedEventArgs e)
	{
		if (_dataStore == null || !CanResetSelectedPage)
		{
			return;
		}
		string text = PlannedPages[SettingsPageList.SelectedIndex];
		GlassDeskSettings current = _dataStore.Settings.Current;
		GlassDeskSettings glassDeskSettings = SettingsPageEditor.Clone(current);
		GlassDeskSettings glassDeskSettings2 = SettingsPageEditor.ResetPage(glassDeskSettings, (text == "桌面") ? "桌面与任务栏" : text);
		string text2 = SettingsPageEditor.Validate(glassDeskSettings2);
		if (text2 != null)
		{
			StatusFor(text).Text = "无法恢复默认：" + text2;
			return;
		}
		if (text == "快捷操作")
		{
			if (InteractionSettingsApply == null)
			{
				InteractionSettingsStatus.Text = "运行服务未连接，未保存更改。";
				return;
			}
			string text3 = InteractionSettingsApply(glassDeskSettings2.Shortcuts, glassDeskSettings2.Display);
			if (text3 != null)
			{
				InteractionSettingsStatus.Text = "恢复默认失败，已恢复原设置：" + text3;
				LoadInteractionControls();
			}
			else
			{
				LoadInteractionControls();
				InteractionSettingsStatus.Text = "本页已恢复默认设置。";
			}
			return;
		}
		try
		{
			SettingsPageEditor.Apply(current, glassDeskSettings2);
			_dataStore.Settings.Save();
		}
		catch (Exception ex)
		{
			SettingsPageEditor.Apply(current, glassDeskSettings);
			ReloadPage(text);
			StatusFor(text).Text = "恢复默认失败，已恢复原设置：" + ex.Message;
			return;
		}
		NotifyPageApplied(text);
		ReloadPage(text);
		if (text == "桌面")
		{
			DesktopSettingsStatus.Text += " 本页应用内配置已恢复默认，未更改 Windows 桌面图标显示状态。";
		}
	}

	private void SavePageSnapshot(GlassDeskSettings snapshot, string page, TextBlock status)
	{
		string text = SettingsPageEditor.Validate(_dataStore.Settings.Current);
		if (text != null)
		{
			SettingsPageEditor.Apply(_dataStore.Settings.Current, snapshot);
			LoadAppearanceControls();
			LoadDockControls();
			status.Text = "设置无效，已恢复原设置：" + text;
			return;
		}
		try
		{
			_dataStore.Settings.Save();
		}
		catch (Exception ex)
		{
			SettingsPageEditor.Apply(_dataStore.Settings.Current, snapshot);
			LoadAppearanceControls();
			LoadDockControls();
			status.Text = "保存失败，已恢复原设置：" + ex.Message;
			return;
		}
		NotifyPageApplied(page);
		status.Text = "已保存并应用。";
	}

	private void NotifyPageApplied(string page)
	{
		GlassDeskSettings current = _dataStore.Settings.Current;
		if ((page == "外观" || page == "信息栏") ? true : false)
		{
			ThemeService.Current?.Update(current.Appearance);
		}
		if (page == "Dock")
		{
			DockSettingsHost.Current?.Update(current.Dock);
			ThemeService.Current?.Update(current.Appearance);
		}
		if (page == "启动台")
		{
			LauncherSettingsChanged?.Invoke(this, current.Launcher);
		}
		if (page == "性能")
		{
			PerformanceSettingsChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	private void ReloadPage(string page)
	{
		switch (page)
		{
		case "外观":
			LoadAppearanceControls();
			ThemeService.Current?.Update(_dataStore.Settings.Current.Appearance);
			break;
		case "Dock":
			LoadDockControls();
			DockSettingsHost.Current?.Update(_dataStore.Settings.Current.Dock);
			break;
		case "启动台":
			LoadLauncherControls();
			LauncherSettingsChanged?.Invoke(this, _dataStore.Settings.Current.Launcher);
			break;
		case "快捷操作":
			LoadInteractionControls();
			break;
		case "性能":
			LoadPerformanceControls();
			break;
		case "桌面":
			LoadDesktopStatus();
			break;
		}
	}

	private TextBlock StatusFor(string page)
	{
		return page switch
		{
			"外观" => AppearanceStatus, 
			"Dock" => DockSettingsStatus, 
			"启动台" => LauncherSettingsStatus, 
			"快捷操作" => InteractionSettingsStatus, 
			"性能" => PerformanceSettingsStatus, 
			_ => DesktopSettingsStatus, 
		};
	}

	private void OnReplayOnboardingClick(object sender, RoutedEventArgs e)
	{
		OnboardingRequested?.Invoke(this, EventArgs.Empty);
	}

	public void NavigateToPage(string page)
	{
		SettingsPageList.SelectedItem = page;
	}

	public GuidedTour BeginOnboardingGuide(string page)
	{
		_onboardingGuide?.Dispose();
		NavigateToPage(page);
		GuidedTourStep[] steps = page switch
		{
			"外观" => new GuidedTourStep[4]
			{
				Step(AppearanceMaterialCombo, "先选喜欢的玻璃质感", "这里切换液态玻璃或磨砂玻璃。你主动选择后才会保存；喜欢当前效果就直接下一步。", GuideInteraction.Selection, "外观"),
				Step(DockTransparencySlider, "让 Dock 更通透，或更清楚", "拖动滑块观察 Dock 的透明度。文字不容易看清时，可以增加不透明程度。每次调整都会即时保存。", GuideInteraction.Value, "外观"),
				Step(LauncherTransparencySlider, "启动台可以单独调整", "这个滑块只控制启动台透明度，不必与 Dock 一样。不确定时保留当前值即可。", GuideInteraction.Value, "外观"),
				Step(DockScaleSlider, "找到舒服的图标大小", "这里是 Dock 的整体大小。试着小幅拖动，观察真实桌面上的变化；教学不会替你移动或删除图标。", GuideInteraction.Value, "Dock")
			}, 
			"Dock" => new GuidedTourStep[3]
			{
				Step(DockDisplayModeCombo, "让 Dock 常驻，或自动隐藏", "按自己的习惯选择显示方式。自动隐藏后可以靠近所在屏幕边缘唤出。", GuideInteraction.Selection, "Dock"),
				Step(DockScaleSlider, "调整整体大小", "轻轻拖动滑块试试。大小变化不会改变你添加的应用或排列顺序。", GuideInteraction.Value, "Dock"),
				Step(ShowLauncherEntryCheckBox, "认识内置快捷入口", "这里控制九宫格启动台入口是否显示。它与自己添加的应用图标是独立的；保留勾选更方便打开启动台。", GuideInteraction.Explore, "Dock")
			}, 
			"信息栏" => new GuidedTourStep[3]
			{
				Step(ShowStatusBarCheckBox, "桌面信息，一眼就看到", "信息栏展示时间及系统状态。由你决定是否启用；教学不会自动改变开关。", GuideInteraction.Toggle, "信息栏"),
				Step(StatusBarEdgeCombo, "放到顺手的屏幕位置", "启用信息栏后，可以在这里选择位置。选项不可用时先看懂说明，再直接下一步也可以。", GuideInteraction.Selection, "信息栏"),
				Step(StatusBarTextBrightnessSlider, "让文字适合你的壁纸", "调整明暗，让浅色或深色壁纸上的文字更清楚。这里只改变信息栏的文字和图标明暗。", GuideInteraction.Value, "信息栏")
			}, 
			"快捷操作" => new GuidedTourStep[3]
			{
				Step(ToggleLauncherHotkeyBox, "记住打开启动台的快捷键", "这里显示当前绑定。需要修改时再点击录制并按下新组合；不录制也能继续，按 Esc 可取消录制。", GuideInteraction.Explore, "快捷操作"),
				Step(ToggleGameSleepHotkeyBox, "给游戏休眠留一个快捷入口", "游戏休眠会暂停部分后台工作。这里可以查看或录制它的快捷键，不会因为教学自动启用休眠。", GuideInteraction.Explore, "快捷操作"),
				Step(AutoSuspendWhenFullscreenCheck, "全屏时自动减少后台工作", "开启后，全屏应用运行时会自动休眠。正常使用不必反复操作；如果不需要，保留当前选项即可。", GuideInteraction.Toggle, "性能")
			}, 
			"性能" => new GuidedTourStep[2]
			{
				Step(AutoSuspendWhenFullscreenCheck, "全屏时自动休眠", "开启后，全屏应用运行时会自动减少背景处理。是否开启由你选择，不会关闭玻璃效果或移除应用。", GuideInteraction.Toggle, "性能"),
				Step(ToggleGameSleepHotkeyBox, "也可以手动切换", "这里能查看游戏休眠的快捷键。日常还可以从托盘菜单手动开关。", GuideInteraction.Explore, "快捷操作")
			}, 
			"备份恢复" => new GuidedTourStep[3]
			{
				Step(CreateBackupButton, "先认识创建备份", "整理完桌面后，可以主动点击这里，选择一个数据目录之外的文件夹保存备份。教学不会自动打开选择器或创建文件。", GuideInteraction.Explore, "备份恢复"),
				Step(BackupInfoText, "记清备份里有什么", "备份包含设置、应用库、Dock 和启动台布局以及自定义图标。恢复会替换当前配置，需要恢复时再操作。", GuideInteraction.Explore, "备份恢复"),
				Step(ReplayOnboardingButton, "以后随时可以重看", "在“关于”里找到这个按钮即可重新开始教学。现在点“完成实操”返回本次教学，继续下一章节。", GuideInteraction.Explore, "关于")
			}, 
			_ => new GuidedTourStep[3]
			{
				Step(SettingsPageList, "从左边找到你要的设置", "外观、Dock、信息栏、启动台都在这里。点击一个分类，右边会滚动到对应位置；设置窗口也可以直接上下滚动。", GuideInteraction.Selection, page),
				Step(ProductVersionText, "了解正在使用的版本", "“关于”中可以查看版本、检查更新和选择网盘下载。教学不会主动下载或安装更新。", GuideInteraction.Explore, "关于"),
				Step(ReplayOnboardingButton, "找回这份教学", "以后想再看一遍，就到 设置 → 关于 → 重新查看新手教学。点“完成实操”即可回到刚才的章节。", GuideInteraction.Explore, "关于")
			}, 
		};
		GuidedTour tour = new GuidedTour(this, SettingsGuideRoot, steps);
		_onboardingGuide = tour;
		tour.Closed += (object? _, EventArgs _) =>
		{
			if (_onboardingGuide == tour)
			{
				_onboardingGuide = null;
			}
		};
		return tour;
		GuidedTourStep Step(FrameworkElement target, string title, string description, GuideInteraction interaction, string category)
		{
			return new GuidedTourStep(target, title, description, interaction, () =>
			{
				NavigateToPage(category);
			});
		}
	}

	private void LoadPerformanceControls()
	{
		_loadingPerformanceControls = true;
		try
		{
			AutoSuspendWhenFullscreenCheck.IsChecked = _dataStore?.Settings.Current.Performance.AutoSuspendWhenFullscreen ?? true;
			AutoSuspendWhenFullscreenCheck.IsEnabled = _dataStore != null;
		}
		finally
		{
			_loadingPerformanceControls = false;
		}
	}

	private void OnPerformanceControlChanged(object sender, RoutedEventArgs e)
	{
		if (!_loadingPerformanceControls && _dataStore != null)
		{
			GlassDeskSettings current = _dataStore.Settings.Current;
			PerformanceSettings performance = current.Performance;
			try
			{
				current.Performance = new PerformanceSettings
				{
					AutoSuspendWhenFullscreen = (AutoSuspendWhenFullscreenCheck.IsChecked == true)
				};
				_dataStore.Settings.Save();
			}
			catch (Exception ex)
			{
				current.Performance = performance;
				LoadPerformanceControls();
				PerformanceSettingsStatus.Text = "保存失败，已恢复原设置：" + ex.Message;
				return;
			}
			PerformanceSettingsChanged?.Invoke(this, EventArgs.Empty);
			PerformanceSettingsStatus.Text = "已保存并应用。窗口化游戏可在托盘手动启用游戏休眠。";
		}
	}

	private void LoadSponsorshipCodes()
	{
		SetSponsorshipCodes(LoadSponsorshipCode("alipay.jpg") ?? LoadSponsorshipCode("alipay.png"), LoadSponsorshipCode("wechat.png"));
	}

	private static BitmapFrame? LoadSponsorshipCode(string fileName)
	{
		try
		{
			using Stream stream = typeof(SettingsWindow).Assembly.GetManifestResourceStream("GlassDesk.Settings.Sponsorship." + fileName);
			if (stream == null)
			{
				return null;
			}
			BitmapFrame bitmapFrame = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
			bitmapFrame.Freeze();
			return bitmapFrame;
		}
		catch (Exception ex) when ((ex is IOException || ex is NotSupportedException || ex is ArgumentException) ? true : false)
		{
			return null;
		}
	}

	public void SetSponsorshipCodes(ImageSource? alipay, ImageSource? wechat)
	{
		_alipaySponsorCode = alipay;
		_wechatSponsorCode = wechat;
	}

	private void OnSponsorClick(object sender, RoutedEventArgs e)
	{
		bool valueOrDefault = SponsorWechatOption.IsChecked == true;
		SponsorshipWindow sponsorshipWindow = new SponsorshipWindow(valueOrDefault, valueOrDefault ? _wechatSponsorCode : _alipaySponsorCode);
		sponsorshipWindow.Owner = this;
		sponsorshipWindow.ShowDialog();
	}

	public void ShowUpdateStatus(string version, string status, bool checking = false)
	{
		ProductVersionText.Text = "GlassDesk " + version;
		UpdateStatusText.Text = status;
		CheckUpdateButton.IsEnabled = !checking && !_userDataDeletionPending;
		CloudDownloadButton.IsEnabled = !_userDataDeletionPending;
	}

	private void OnCheckUpdateClick(object sender, RoutedEventArgs e)
	{
		CheckForUpdatesRequested?.Invoke(this, EventArgs.Empty);
	}

	private void OnCloudDownloadClick(object sender, RoutedEventArgs e)
	{
		CloudDownloadsRequested?.Invoke(this, EventArgs.Empty);
	}

	public SettingsWindow()
	{
		_loadingAppearanceControls = true;
		try
		{
			InitializeComponent();
			LauncherOffsetControls.Configure(this);
			AutostartCheck.IsEnabled = false;
			Title = "GlassDesk - Capture Acceptance 20261007";
		}
		finally
		{
			_loadingAppearanceControls = false;
		}
		_sectionHeadings = new FrameworkElement[10] { AppearanceSectionHeading, DockSectionHeading, InformationBarSectionHeading, LauncherSectionHeading, ShortcutsSectionHeading, PerformanceSectionHeading, DesktopSectionHeading, BackupSectionHeading, SponsorshipSectionHeading, AboutSectionHeading };
		PlannedPages = new _003C_003Ez__ReadOnlyArray<string>(new string[10] { "外观", "Dock", "信息栏", "启动台", "快捷操作", "性能", "桌面", "备份恢复", "赞助支持", "关于" });
		ResetPageButton.IsEnabled = false;
		LoadSponsorshipCodes();
	}

	private void OnSettingsWindowLoaded(object sender, RoutedEventArgs e)
	{
		SyncCategoryFromScroll();
	}

	private void UpdateResetPageButton()
	{
		ResetPageButton.IsEnabled = SettingsPageList.IsEnabled && _dataStore != null && CanResetSelectedPage;
	}

	protected override double ResolveCornerRadius(AppearanceSettings appearance)
	{
		ArgumentNullException.ThrowIfNull(appearance, "appearance");
		return Math.Max(80.0, appearance.LauncherCornerRadius);
	}

	public void AttachDataStore(GlassDeskDataStore dataStore, DataLoadReport? loadReport)
	{
		ArgumentNullException.ThrowIfNull(dataStore, "dataStore");
		_dataStore = dataStore;
		LoadDockControls();
		LoadLauncherControls();
		LoadAppearanceControls();
		LoadInteractionControls();
		LoadPerformanceControls();
		LoadDesktopStatus();
		RefreshDiagnostics(loadReport);
	}

	public void AttachStartupRegistration(StartupRegistration registration, string executablePath)
	{
		ArgumentNullException.ThrowIfNull(registration, "registration");
		ArgumentNullException.ThrowIfNull(executablePath, "executablePath");
		_startupRegistration = registration;
		_startupExecutablePath = executablePath;
		AutostartExecutableText.Text = "当前程序：" + executablePath;
		RefreshStartupRegistration();
	}

	public void RefreshStartupRegistration()
	{
		if (!Dispatcher.CheckAccess())
		{
			Dispatcher.BeginInvoke(new Action(RefreshStartupRegistration));
			return;
		}
		_loadingStartupRegistration = true;
		try
		{
			if (_startupRegistration == null)
			{
				AutostartCheck.IsChecked = false;
				AutostartCheck.IsEnabled = false;
				AutostartStatus.Text = "当前程序的启动注册状态尚不可用。";
				return;
			}
			StartupRegistrationStatus startupRegistrationStatus = _startupRegistration.Query(_startupExecutablePath);
			AutostartCheck.IsChecked = startupRegistrationStatus.State == StartupRegistrationState.Owned;
			CheckBox autostartCheck = AutostartCheck;
			StartupRegistrationState state = startupRegistrationStatus.State;
			bool isEnabled = (uint)state <= 1u;
			autostartCheck.IsEnabled = isEnabled;
			TextBlock autostartStatus = AutostartStatus;
			autostartStatus.Text = startupRegistrationStatus.State switch
			{
				StartupRegistrationState.Missing => "未启用；可通过此开关开启。首次运行默认启用，手动关闭后会保持关闭。", 
				StartupRegistrationState.Owned => "已启用当前用户登录启动；可通过此开关关闭。", 
				StartupRegistrationState.OtherTarget => "同名启动项指向其它位置；为保护其它副本，此处不会覆盖或删除它。", 
				_ => "无法读取或校验当前用户登录项：" + (startupRegistrationStatus.Error ?? "原因未知。"), 
			};
		}
		finally
		{
			_loadingStartupRegistration = false;
		}
	}

	public void PositionOnPrimary()
	{
		CenterOnPrimaryWorkArea();
	}

	public void ShowStartupRegistrationFailure(string message)
	{
		AutostartStatus.Text = "未能自动启用登录启动：" + message + Environment.NewLine + AutostartStatus.Text;
	}

	private void OnAutostartClick(object sender, RoutedEventArgs e)
	{
		if (!_loadingStartupRegistration && _startupRegistration != null)
		{
			bool valueOrDefault = AutostartCheck.IsChecked == true;
			StartupChangeResult startupChangeResult = _startupRegistration.SetEnabled(_startupExecutablePath, valueOrDefault);
			RefreshStartupRegistration();
			if (!startupChangeResult.Succeeded)
			{
				AutostartStatus.Text = "未能更改登录启动项：" + (startupChangeResult.Error ?? "未知错误。") + Environment.NewLine + "当前实际状态：" + AutostartStatus.Text;
			}
		}
	}

	private void LoadDockControls()
	{
		if (_dataStore == null)
		{
			return;
		}
		_loadingDockControls = true;
		try
		{
			DockSettings dock = _dataStore.Settings.Current.Dock;
			DockDisplayModeCombo.SelectedIndex = (dock.AutoHide ? 1 : 0);
			ShowLauncherEntryCheckBox.IsChecked = dock.ShowLauncherEntry;
			ShowStatusBarCheckBox.IsChecked = dock.ShowStatusBar;
			ShowStatusBarNetworkSpeedCheckBox.IsChecked = dock.ShowStatusBarNetworkSpeed;
			ShowThisPcEntryCheckBox.IsChecked = dock.ShowThisPcEntry;
			ShowDesktopIconsEntryCheckBox.IsChecked = dock.ShowDesktopIconsEntry;
			StatusBarEdgeCombo.SelectedIndex = (int)dock.StatusBarEdge;
			StatusBarScaleSlider.Value = dock.StatusBarSizeScale;
			StatusBarScaleReadout.Text = $"{dock.StatusBarSizeScale:P0} · {380.0 * dock.StatusBarSizeScale:0} × {54.0 * dock.StatusBarSizeScale:0} DIP";
			DockScaleSlider.Value = dock.SizeScale;
			DockScaleReadout.Text = $"{dock.SizeScale:P0} · 图标 {48.0 * dock.SizeScale:0} DIP";
			DockPlacementCombo.SelectedIndex = (int)dock.Placement;
			PrimaryDisplaySession primaryDisplaySession = PrimaryDisplaySession;
			DisplayInfo displayInfo;
			if (primaryDisplaySession != null && primaryDisplaySession.IsAvailable)
			{
				displayInfo = primaryDisplaySession.Display;
			}
			else
			{
				displayInfo = ((PrimaryDisplaySession == null) ? DisplayService.Primary() : null);
			}
			double num = (((object)displayInfo == null) ? 10000.0 : (displayInfo.ToDips(displayInfo.WorkArea.Width) - 32.0));
			StatusBarOffsetXSlider.Minimum = Math.Max(-10000.0, Math.Min(0.0, 380.0 * dock.StatusBarSizeScale + 18.0 - (num + 32.0)));
			StatusBarOffsetXSlider.Value = Math.Clamp(dock.StatusBarOffsetX, StatusBarOffsetXSlider.Minimum, StatusBarOffsetXSlider.Maximum);
			StatusBarOffsetXReadout.Text = $"{dock.StatusBarOffsetX:0} DIP";
			double num2 = Math.Min(10000.0, Math.Max(64.0, num));
			int num3 = (dock.ShowLauncherEntry ? 1 : 0) + (dock.ShowThisPcEntry ? 1 : 0) + (dock.ShowDesktopIconsEntry ? 1 : 0);
			double num4 = Math.Min(Math.Max(180.0, 22.0 + 56.0 * (double)num3) * dock.SizeScale, num2);
			DockFrameLengthSlider.Minimum = num4;
			DockFrameLengthSlider.Maximum = num2;
			bool flag = dock.FrameLengthDip == 0.0 || Within(dock.FrameLengthDip, 64.0, 10000.0);
			DockFrameLengthSlider.Value = ((dock.FrameLengthDip > 0.0) ? Math.Clamp(dock.FrameLengthDip, num4, num2) : num4);
			DockFrameLengthReadout.Text = ((dock.FrameLengthDip > 0.0) ? $"固定外框：{DockFrameLengthSlider.Value:0} DIP" : "当前随图标内容自动调整；拖动滑块后固定外框长度。");
			_dockValuesOutOfRange = !Enum.IsDefined(dock.Placement) || !Enum.IsDefined(dock.StatusBarEdge) || !Within(dock.OffsetX, -200.0, 200.0) || !Within(dock.OffsetY, -200.0, 200.0) || !flag;
			DockOffsetXSlider.Value = Coerce(dock.OffsetX, -200.0, 200.0);
			DockOffsetYSlider.Value = Coerce(dock.OffsetY, -200.0, 200.0);
			DockSettingsStatus.Text = (_dockValuesOutOfRange ? "配置文件中有超出支持范围的数值，已显示为最近的可用值；请先恢复当前页默认，避免覆盖原数据。" : "整体大小调整图标与栏高度；外框长度可独立调整。修改后立即保存并应用。");
		}
		finally
		{
			_loadingDockControls = false;
		}
	}

	private static bool Within(double value, double minimum, double maximum)
	{
		if (double.IsFinite(value) && value >= minimum)
		{
			return value <= maximum;
		}
		return false;
	}

	private static double Coerce(double value, double minimum, double maximum)
	{
		if (!double.IsFinite(value))
		{
			return minimum;
		}
		return Math.Clamp(value, minimum, maximum);
	}

	private void LoadLauncherControls()
	{
		if (_dataStore == null)
		{
			LauncherOffsetControls.Reload(this);
			return;
		}
		_loadingLauncherControls = true;
		try
		{
			LauncherSettings launcher = _dataStore.Settings.Current.Launcher;
			int rows = launcher.Rows;
			bool flag = ((rows < 2 || rows > 10) ? true : false);
			bool flag2 = flag;
			if (!flag2)
			{
				int columns = launcher.Columns;
				bool flag3 = ((columns < 2 || columns > 12) ? true : false);
				flag2 = flag3;
			}
			_launcherValuesOutOfRange = flag2;
			LauncherRowsSlider.Value = Math.Clamp(launcher.Rows, 2, 10);
			LauncherColumnsSlider.Value = Math.Clamp(launcher.Columns, 2, 12);
			LauncherFocusSearchCheck.IsChecked = launcher.FocusSearchOnOpen;
			LauncherRememberPageCheck.IsChecked = launcher.RememberLastPage;
			LauncherHighRefreshCheck.IsChecked = launcher.HighRefreshAnimationsEnabled;
			LauncherModeCombo.SelectedIndex = (launcher.GroupModeEnabled ? 1 : 0);
			LauncherSettingsStatus.Text = (_launcherValuesOutOfRange ? "配置文件中的网格数值超出支持范围，已显示为最近的可用值；请先恢复当前页默认，避免覆盖原数据。" : "修改后立即保存并应用到启动台。");
		}
		finally
		{
			_loadingLauncherControls = false;
		}
		LauncherOffsetControls.Reload(this);
	}

	private void OnLauncherControlChanged(object sender, RoutedEventArgs e)
	{
		if (_loadingLauncherControls || _dataStore == null)
		{
			return;
		}
		if (_launcherValuesOutOfRange)
		{
			LoadLauncherControls();
			return;
		}
		GlassDeskSettings current = _dataStore.Settings.Current;
		LauncherSettings launcher = current.Launcher;
		LauncherSettings launcherSettings = new LauncherSettings
		{
			Rows = (int)LauncherRowsSlider.Value,
			Columns = (int)LauncherColumnsSlider.Value,
			WindowWorkAreaRatio = launcher.WindowWorkAreaRatio,
			FullscreenMode = false,
			FocusSearchOnOpen = (LauncherFocusSearchCheck.IsChecked == true),
			RememberLastPage = (LauncherRememberPageCheck.IsChecked == true),
			HighRefreshAnimationsEnabled = (LauncherHighRefreshCheck.IsChecked == true),
			GroupModeEnabled = (LauncherModeCombo.SelectedIndex == 1)
		};
		launcherSettings.OffsetY = launcher.OffsetY;
		GlassDeskSettings glassDeskSettings = SettingsPageEditor.Clone(current);
		glassDeskSettings.Launcher = launcherSettings;
		string text = SettingsPageEditor.Validate(glassDeskSettings);
		if (text != null)
		{
			LoadLauncherControls();
			LauncherSettingsStatus.Text = "设置无效，未保存：" + text;
		}
		else if (launcher.Rows != launcherSettings.Rows || launcher.Columns != launcherSettings.Columns || launcher.FullscreenMode != launcherSettings.FullscreenMode || launcher.FocusSearchOnOpen != launcherSettings.FocusSearchOnOpen || launcher.RememberLastPage != launcherSettings.RememberLastPage || launcher.HighRefreshAnimationsEnabled != launcherSettings.HighRefreshAnimationsEnabled || launcher.GroupModeEnabled != launcherSettings.GroupModeEnabled)
		{
			try
			{
				current.Launcher = launcherSettings;
				_dataStore.Settings.Save();
				LauncherSettingsChanged?.Invoke(this, launcherSettings);
				LauncherSettingsStatus.Text = $"已保存：{launcherSettings.Rows} 行 × {launcherSettings.Columns} 列。";
			}
			catch (Exception ex)
			{
				current.Launcher = launcher;
				LoadLauncherControls();
				LauncherSettingsStatus.Text = "保存失败，已恢复原设置：" + ex.Message;
			}
		}
	}

	private void OnSettingsPageSelected(object sender, SelectionChangedEventArgs e)
	{
		if (IsLoaded)
		{
			UpdateResetPageButton();
			if (!_syncingCategoryFromScroll && SettingsPageList.SelectedIndex >= 0)
			{
				ScrollToSection(_sectionHeadings[SettingsPageList.SelectedIndex]);
			}
		}
	}

	private void OnSettingsScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		if (IsLoaded && !_sectionNavigationPending && e.OriginalSource == SettingsScroll && (e.VerticalChange != 0.0 || e.ExtentHeightChange != 0.0 || e.ViewportHeightChange != 0.0))
		{
			SyncCategoryFromScroll();
		}
	}

	private void SyncCategoryFromScroll()
	{
		int selectedIndex = 0;
		for (int i = 1; i < _sectionHeadings.Length && !(_sectionHeadings[i].TranslatePoint(default, SettingsScroll).Y > 13.0); i++)
		{
			selectedIndex = i;
		}
		if (SettingsScroll.ScrollableHeight > 0.0 && SettingsScroll.VerticalOffset >= SettingsScroll.ScrollableHeight - 0.5)
		{
			selectedIndex = _sectionHeadings.Length - 1;
		}
		_syncingCategoryFromScroll = true;
		try
		{
			SettingsPageList.SelectedIndex = selectedIndex;
		}
		finally
		{
			_syncingCategoryFromScroll = false;
		}
		UpdateResetPageButton();
	}

	private void ScrollToSection(FrameworkElement heading)
	{
		int version = ++_sectionNavigationVersion;
		_sectionNavigationPending = true;
		Dispatcher.BeginInvoke((Action)(() =>
		{
			if (version == _sectionNavigationVersion)
			{
				Point point = heading.TranslatePoint(new Point(0.0, 0.0), SettingsScroll);
				SettingsScroll.ScrollToVerticalOffset(SettingsScroll.VerticalOffset + point.Y - 12.0);
				Dispatcher.BeginInvoke((Action)(() =>
				{
					if (version == _sectionNavigationVersion)
					{
						_sectionNavigationPending = false;
					}
				}), DispatcherPriority.Background);
			}
		}), DispatcherPriority.Loaded);
	}

	private void OnDockControlChanged(object sender, RoutedEventArgs e)
	{
		if (_loadingDockControls || _dataStore == null || DockPlacementCombo.SelectedIndex < 0 || StatusBarEdgeCombo.SelectedIndex < 0 || DockDisplayModeCombo.SelectedIndex < 0)
		{
			return;
		}
		if (_dockValuesOutOfRange)
		{
			LoadDockControls();
			return;
		}
		TextBlock textBlock = ((sender == ShowStatusBarCheckBox || sender == StatusBarEdgeCombo || sender == ShowStatusBarNetworkSpeedCheckBox || sender == StatusBarScaleSlider || sender == StatusBarOffsetXSlider) ? InformationBarSettingsStatus : DockSettingsStatus);
		GlassDeskSettings current = _dataStore.Settings.Current;
		DockSettings dock = current.Dock;
		double dockCornerRadius = current.Appearance.DockCornerRadius;
		DockSettings dockSettings = new DockSettings
		{
			SizeScale = DockScaleSlider.Value,
			ShowLauncherEntry = (ShowLauncherEntryCheckBox.IsChecked == true),
			ShowStatusBar = (ShowStatusBarCheckBox.IsChecked == true),
			ShowStatusBarNetworkSpeed = (ShowStatusBarNetworkSpeedCheckBox.IsChecked == true),
			ShowThisPcEntry = (ShowThisPcEntryCheckBox.IsChecked == true),
			ShowDesktopIconsEntry = (ShowDesktopIconsEntryCheckBox.IsChecked == true),
			LauncherCustomIconPath = dock.LauncherCustomIconPath,
			ThisPcCustomIconPath = dock.ThisPcCustomIconPath,
			DesktopIconsCustomIconPath = dock.DesktopIconsCustomIconPath,
			IconSize = 48.0,
			IconSpacing = 8.0,
			MagnificationPeak = 1.15,
			FrameLengthDip = ((sender == DockFrameLengthSlider) ? DockFrameLengthSlider.Value : dock.FrameLengthDip),
			Placement = (DockPlacement)DockPlacementCombo.SelectedIndex,
			OffsetX = DockOffsetXSlider.Value,
			OffsetY = DockOffsetYSlider.Value,
			StatusBarEdge = (StatusBarEdge)StatusBarEdgeCombo.SelectedIndex,
			StatusBarSizeScale = StatusBarScaleSlider.Value,
			StatusBarOffsetX = ((sender == StatusBarOffsetXSlider) ? StatusBarOffsetXSlider.Value : dock.StatusBarOffsetX),
			StatusBarOffsetY = 0.0,
			AutoHide = (DockDisplayModeCombo.SelectedIndex == 1),
			AutoHideLeaveDelayMs = dock.AutoHideLeaveDelayMs,
			EdgeRevealEnabled = false,
			EdgeHotzoneSize = dock.EdgeHotzoneSize,
			BlockRevealWhenCovered = dock.BlockRevealWhenCovered,
			TargetDisplayId = dock.TargetDisplayId,
			ShowRunningIndicator = dock.ShowRunningIndicator
		};
		GlassDeskSettings glassDeskSettings = SettingsPageEditor.Clone(current);
		glassDeskSettings.Dock = dockSettings;
		glassDeskSettings.Appearance.DockCornerRadius = 40.0;
		string text = SettingsPageEditor.Validate(glassDeskSettings);
		if (text != null)
		{
			LoadDockControls();
			textBlock.Text = "设置无效，未保存：" + text;
			return;
		}
		try
		{
			current.Dock = dockSettings;
			current.Appearance.DockCornerRadius = 40.0;
			_dataStore.Settings.Save();
			DockSettingsHost.Current?.Update(dockSettings);
			if (Math.Abs(dockCornerRadius - current.Appearance.DockCornerRadius) > 0.001)
			{
				ThemeService.Current?.Update(current.Appearance);
			}
			DockFrameLengthReadout.Text = ((dockSettings.FrameLengthDip > 0.0) ? $"固定外框：{DockFrameLengthSlider.Value:0} DIP" : "当前随图标内容自动调整；拖动滑块后固定外框长度。");
			LoadDockControls();
			textBlock.Text = $"已保存；Dock {dockSettings.SizeScale:P0}，信息栏 {dockSettings.StatusBarSizeScale:P0}。";
		}
		catch (Exception ex)
		{
			current.Dock = dock;
			current.Appearance.DockCornerRadius = dockCornerRadius;
			LoadDockControls();
			textBlock.Text = "保存失败，已恢复原设置：" + ex.Message;
		}
	}

	private void OnDockAutoLengthClick(object sender, RoutedEventArgs e)
	{
		if (_dataStore != null && !_dockValuesOutOfRange)
		{
			GlassDeskSettings snapshot = SettingsPageEditor.Clone(_dataStore.Settings.Current);
			_dataStore.Settings.Current.Dock.FrameLengthDip = 0.0;
			SavePageSnapshot(snapshot, "Dock", DockSettingsStatus);
		}
	}

	public void RefreshDiagnostics(DataLoadReport? loadReport = null)
	{
		ThemeService current = ThemeService.Current;
		CultureInfo invariantCulture = CultureInfo.InvariantCulture;
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		IFormatProvider formatProvider = invariantCulture;
		IFormatProvider provider = formatProvider;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(16, 2, stringBuilder2, formatProvider);
		handler.AppendLiteral("Windows：");
		handler.AppendFormatted(WindowsCapabilities.OsVersion);
		handler.AppendLiteral("（Build ");
		handler.AppendFormatted(WindowsCapabilities.BuildNumber);
		handler.AppendLiteral("）");
		stringBuilder3.AppendLine(provider, ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		formatProvider = invariantCulture;
		IFormatProvider provider2 = formatProvider;
		handler = new StringBuilder.AppendInterpolatedStringHandler(7, 1, stringBuilder2, formatProvider);
		handler.AppendLiteral("系统背景材质：");
		handler.AppendFormatted(WindowsCapabilities.SupportsSystemBackdrop ? "受支持（Build 22621 及以上）" : "不支持，使用自绘面板");
		stringBuilder4.AppendLine(provider2, ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		formatProvider = invariantCulture;
		IFormatProvider provider3 = formatProvider;
		handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, stringBuilder2, formatProvider);
		handler.AppendLiteral("窗口圆角：");
		handler.AppendFormatted(WindowsCapabilities.SupportsWindowCornerPreference ? "由 DWM 提供" : "不受支持，使用自绘圆角");
		stringBuilder5.AppendLine(provider3, ref handler);
		if (current != null)
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder6 = stringBuilder2;
			formatProvider = invariantCulture;
			IFormatProvider provider4 = formatProvider;
			handler = new StringBuilder.AppendInterpolatedStringHandler(8, 2, stringBuilder2, formatProvider);
			handler.AppendLiteral("主题：");
			handler.AppendFormatted(current.Appearance.Theme);
			handler.AppendLiteral("（当前 ");
			handler.AppendFormatted(current.IsDark ? "深色" : "浅色");
			handler.AppendLiteral("）");
			stringBuilder6.AppendLine(provider4, ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder7 = stringBuilder2;
			formatProvider = invariantCulture;
			IFormatProvider provider5 = formatProvider;
			handler = new StringBuilder.AppendInterpolatedStringHandler(3, 1, stringBuilder2, formatProvider);
			handler.AppendLiteral("面板：");
			handler.AppendFormatted(current.UseOpaquePanels ? "不透明" : "半透明玻璃");
			stringBuilder7.AppendLine(provider5, ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder8 = stringBuilder2;
			formatProvider = invariantCulture;
			IFormatProvider provider6 = formatProvider;
			handler = new StringBuilder.AppendInterpolatedStringHandler(3, 1, stringBuilder2, formatProvider);
			handler.AppendLiteral("动画：");
			handler.AppendFormatted(current.AnimationsEnabled ? "开启" : "已减少");
			stringBuilder8.AppendLine(provider6, ref handler);
		}
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder9 = stringBuilder2;
		formatProvider = invariantCulture;
		IFormatProvider provider7 = formatProvider;
		handler = new StringBuilder.AppendInterpolatedStringHandler(7, 1, stringBuilder2, formatProvider);
		handler.AppendLiteral("窗口玻璃做法：");
		handler.AppendFormatted((SurfaceMode == GlassSurfaceMode.Acrylic) ? "亚克力背景模糊" : "自绘半透明面板（无背景模糊）");
		stringBuilder9.AppendLine(provider7, ref handler);
		if (_dataStore != null)
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder10 = stringBuilder2;
			formatProvider = invariantCulture;
			IFormatProvider provider8 = formatProvider;
			handler = new StringBuilder.AppendInterpolatedStringHandler(5, 1, stringBuilder2, formatProvider);
			handler.AppendLiteral("数据目录：");
			handler.AppendFormatted(_dataStore.Paths.RootDirectory);
			stringBuilder10.AppendLine(provider8, ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder11 = stringBuilder2;
			formatProvider = invariantCulture;
			IFormatProvider provider9 = formatProvider;
			handler = new StringBuilder.AppendInterpolatedStringHandler(6, 1, stringBuilder2, formatProvider);
			handler.AppendLiteral("应用库条目：");
			handler.AppendFormatted(_dataStore.Library.Items.Count);
			stringBuilder11.AppendLine(provider9, ref handler);
			if (loadReport != null && loadReport.HasNotices)
			{
				stringBuilder.AppendLine();
				stringBuilder.AppendLine("数据加载提示：");
				foreach (string notice in loadReport.Notices)
				{
					stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder12 = stringBuilder2;
					formatProvider = invariantCulture;
					IFormatProvider provider10 = formatProvider;
					handler = new StringBuilder.AppendInterpolatedStringHandler(2, 1, stringBuilder2, formatProvider);
					handler.AppendLiteral("· ");
					handler.AppendFormatted(notice);
					stringBuilder12.AppendLine(provider10, ref handler);
				}
			}
		}
		DiagnosticsText.Text = stringBuilder.ToString().TrimEnd();
	}

	protected override void OnSourceInitialized(EventArgs e)
	{
		base.OnSourceInitialized(e);
		RefreshDiagnostics();
		RefreshStartupRegistration();
	}

	private void OnDragArea(object sender, MouseButtonEventArgs e)
	{
		if (e.ChangedButton == MouseButton.Left && Mouse.LeftButton == MouseButtonState.Pressed)
		{
			e.Handled = true;
			DragMove();
		}
	}

	private void OnCloseClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

}
