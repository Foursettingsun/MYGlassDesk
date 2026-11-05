using System;
using System.Windows.Threading;
using GlassDesk.AppServices;
using GlassDesk.Core.Input;
using GlassDesk.Core.Models.Settings;
using GlassDesk.Core.Storage;
using GlassDesk.Platform.Displays;
using GlassDesk.Platform.Shell;

namespace GlassDesk.App;

internal sealed class InteractionController : IDisposable
{
	private readonly GlassDeskDataStore _data;

	private readonly Dispatcher _dispatcher;

	private readonly Action<InputAction> _execute;

	private readonly Action<string> _reportError;

	private readonly InteractionSettingsService _settings;

	private readonly DesktopIconSession _desktopIcons = new DesktopIconSession();

	private bool _disposed;

	private volatile bool _desktopAvailable;

	private volatile bool _launcherVisible;

	private volatile bool _canNavigatePages;

	public InteractionController(GlassDeskDataStore data, PrimaryDisplaySession primaryDisplay, nint hwnd, Dispatcher dispatcher, Func<InputAction, bool> canExecute, Action<InputAction> execute, Action<string> reportError)
	{
		InteractionController interactionController = this;
		_data = data ?? throw new ArgumentNullException("data");
		if (primaryDisplay == null)
		{
			throw new ArgumentNullException("primaryDisplay");
		}
		_dispatcher = dispatcher ?? throw new ArgumentNullException("dispatcher");
		ArgumentNullException.ThrowIfNull(canExecute, "canExecute");
		_execute = execute ?? throw new ArgumentNullException("execute");
		_reportError = reportError ?? throw new ArgumentNullException("reportError");
		_settings = new InteractionSettingsService(data.Settings, new NativeHotkeyRegistry(hwnd, (int id) => interactionController.CanExecute(interactionController._settings.ResolveAction(id)) && canExecute(interactionController._settings.ResolveAction(id))));
	}

	public string? Initialize()
	{
		if (_disposed)
		{
			return "快捷操作服务已经关闭。";
		}
		_desktopAvailable = _desktopIcons.AreIconsVisible().HasValue;
		GlassDeskSettings glassDeskSettings = SettingsPageEditor.Clone(_data.Settings.Current);
		DisplaySettings display = new DisplaySettings
		{
			DockDisplayId = (glassDeskSettings.Display.DockDisplayId ?? glassDeskSettings.Dock.TargetDisplayId),
			LauncherFollowsPointer = glassDeskSettings.Display.LauncherFollowsPointer,
			SuppressTriggersWhenFullscreen = glassDeskSettings.Display.SuppressTriggersWhenFullscreen
		};
		return ApplyCore(glassDeskSettings.Shortcuts, display, persist: false);
	}

	public string? Apply(ShortcutSettings shortcuts, DisplaySettings display)
	{
		_dispatcher.VerifyAccess();
		if (_disposed)
		{
			return "快捷操作服务已经关闭。";
		}
		return ApplyCore(shortcuts, display, persist: true);
	}

	private string? ApplyCore(ShortcutSettings shortcuts, DisplaySettings display, bool persist)
	{
		shortcuts = SettingsPageEditor.Clone(new GlassDeskSettings
		{
			Shortcuts = shortcuts
		}).Shortcuts;
		shortcuts.HotCornersEnabled = false;
		shortcuts.MouseSideButtonsEnabled = false;
		if (shortcuts.ToggleDesktopIcons.Enabled && !_desktopAvailable)
		{
			return "当前 Windows 桌面图标状态不可读，已拒绝启用相关动作。";
		}
		string text = _settings.Apply(shortcuts, display, persist);
		if (text != null)
		{
			return text;
		}
		RefreshDisplays();
		return null;
	}

	public void SetLauncherAvailability(bool visible, bool canNavigate)
	{
		_launcherVisible = visible;
		_canNavigatePages = canNavigate;
	}

	public bool CanExecute(InputAction action)
	{
		switch (action)
		{
		case InputAction.None:
			return false;
		case InputAction.ToggleLauncher:
			return true;
		case InputAction.HideLauncher:
			return _launcherVisible;
		case InputAction.NextPage:
		case InputAction.PreviousPage:
			return _canNavigatePages;
		case InputAction.ToggleDesktopIcons:
			return _desktopAvailable;
		case InputAction.ToggleDockRevealLock:
			return true;
		case InputAction.ToggleGameSleep:
			return true;
		default:
			return false;
		}
	}

	public void HandleHotkey(int id)
	{
		if (!_disposed)
		{
			InputAction inputAction = _settings.ResolveAction(id);
			if (CanExecute(inputAction))
			{
				_execute(inputAction);
			}
		}
	}

	public bool IsGameSleepHotkey(int id)
	{
		return _settings.ResolveAction(id) == InputAction.ToggleGameSleep;
	}

	public bool? ReadDesktopIcons()
	{
		_dispatcher.VerifyAccess();
		if (_disposed)
		{
			return null;
		}
		bool? result = _desktopIcons.AreIconsVisible();
		_desktopAvailable = result.HasValue;
		return result;
	}

	public string? SetDesktopIcons(bool visible)
	{
		_dispatcher.VerifyAccess();
		if (_disposed || !_desktopAvailable)
		{
			return "当前 Windows 不支持切换桌面图标。";
		}
		if (!_desktopIcons.TrySetVisible(visible, out string error))
		{
			_desktopAvailable = _desktopIcons.AreIconsVisible().HasValue;
			return error ?? _desktopIcons.Status;
		}
		_desktopAvailable = _desktopIcons.AreIconsVisible().HasValue;
		return null;
	}

	public void ToggleDesktopIcons()
	{
		bool? flag = ReadDesktopIcons();
		if (!flag.HasValue)
		{
			_reportError(_desktopIcons.Status);
			return;
		}
		string text = SetDesktopIcons(!flag.Value);
		if (text != null)
		{
			_reportError(text);
		}
	}

	public void RefreshDisplays()
	{
		_desktopAvailable = _desktopIcons.AreIconsVisible().HasValue;
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_settings.Dispose();
			_desktopIcons.Dispose();
		}
	}
}
