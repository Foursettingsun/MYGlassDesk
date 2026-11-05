using System;
using GlassDesk.Platform.Interop;

namespace GlassDesk.Platform;

public sealed class TrayIcon : IDisposable
{
	public const int CallbackMessage = 2048;

	private readonly nint _ownerWindow;

	private readonly int _id;

	private nint _icon;

	private string _tooltip = string.Empty;

	private bool _added;

	private bool _requestedVisible;

	private bool _disposed;

	public static int TaskbarCreatedMessage { get; } = (int)User32.RegisterWindowMessage("TaskbarCreated");

	public bool IsVisible => _added;

	public TrayIcon(nint ownerWindow, int id = 1)
	{
		if (ownerWindow == 0)
		{
			throw new ArgumentException("托盘图标需要一个有效的窗口句柄。", "ownerWindow");
		}
		_ownerWindow = ownerWindow;
		_id = id;
	}

	public bool Show(nint icon, string tooltip)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		TakeOwnership(icon);
		_tooltip = tooltip;
		_requestedVisible = true;
		return EnsureVisible();
	}

	public bool EnsureVisible()
	{
		if (_disposed || !_requestedVisible || _icon == 0)
		{
			return false;
		}
		Shell32.NotifyIconData data = CreateData();
		if (Shell32.Shell_NotifyIcon(1, ref data))
		{
			return _added = true;
		}
		_added = Shell32.Shell_NotifyIcon(0, ref data);
		if (_added)
		{
			Shell32.NotifyIconData data2 = CreateData();
			data2.VersionOrTimeout = 3;
			Shell32.Shell_NotifyIcon(4, ref data2);
		}
		return _added;
	}

	public bool Update(nint icon, string tooltip)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		return Show(icon, tooltip);
	}

	public void Hide()
	{
		_requestedVisible = false;
		if (_added && !_disposed)
		{
			Shell32.NotifyIconData data = CreateData();
			Shell32.Shell_NotifyIcon(2, ref data);
			_added = false;
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			Hide();
			if (_icon != 0)
			{
				IconFactory.DestroyIcon(_icon);
				_icon = 0;
			}
			_disposed = true;
		}
	}

	public static TrayEvent Classify(int lParam)
	{
		return (lParam & 0xFFFF) switch
		{
			514 => TrayEvent.Activate, 
			517 => TrayEvent.ContextMenu, 
			515 => TrayEvent.DoubleClick, 
			_ => TrayEvent.None, 
		};
	}

	private void TakeOwnership(nint icon)
	{
		if (_icon != icon)
		{
			if (_icon != 0)
			{
				IconFactory.DestroyIcon(_icon);
			}
			_icon = icon;
		}
	}

	private unsafe Shell32.NotifyIconData CreateData()
	{
		Shell32.NotifyIconData result = new Shell32.NotifyIconData
		{
			Size = sizeof(Shell32.NotifyIconData),
			WindowHandle = _ownerWindow,
			Id = _id,
			Flags = 7,
			CallbackMessage = 2048,
			IconHandle = _icon
		};
		Shell32.NotifyIconData.SetText(result.Tip, 128, _tooltip);
		return result;
	}
}
