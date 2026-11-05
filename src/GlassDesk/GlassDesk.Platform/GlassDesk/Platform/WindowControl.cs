using System;
using GlassDesk.Platform.Interop;

namespace GlassDesk.Platform;

public static class WindowControl
{
	public static bool IsAlive(nint window)
	{
		try
		{
			return window != 0 && User32.IsWindow(window);
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return false;
		}
	}

	public static bool IsMinimized(nint window)
	{
		try
		{
			return window != 0 && User32.IsIconic(window);
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return false;
		}
	}

	public static nint GetForegroundWindow()
	{
		try
		{
			return User32.GetForegroundWindow();
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return 0;
		}
	}

	public static bool MinimizeViaSysCommand(nint window)
	{
		try
		{
			return window != 0 && User32.PostMessage(window, 274u, 61472u, 0);
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return false;
		}
	}

	public static string GetTitle(nint window)
	{
		try
		{
			return (window == 0) ? string.Empty : User32.ReadWindowText(window);
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return string.Empty;
		}
	}

	public static bool Close(nint window)
	{
		try
		{
			return window != 0 && User32.PostMessage(window, 16u, 0u, 0);
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return false;
		}
	}

	public static bool Minimize(nint window)
	{
		return ShowWith(window, 6);
	}

	public static bool Restore(nint window)
	{
		return ShowWith(window, 9);
	}

	public static bool Show(nint window)
	{
		return ShowWith(window, 5);
	}

	public static bool Hide(nint window)
	{
		return ShowWith(window, 0);
	}

	private static bool ShowWith(nint window, int command)
	{
		try
		{
			return window != 0 && User32.ShowWindow(window, command);
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return false;
		}
	}
}
