using System;
using System.Runtime.InteropServices;
using GlassDesk.Platform.Displays;

namespace GlassDesk.Platform.Shell;

public static class ForegroundCoverage
{
	private struct Rect
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	public static bool IsCovered(PixelRect target)
	{
		if (target.Width <= 0 || target.Height <= 0)
		{
			return false;
		}
		nint foregroundWindow = GetForegroundWindow();
		if (foregroundWindow == 0 || !IsWindowVisible(foregroundWindow) || IsIconic(foregroundWindow))
		{
			return false;
		}
		if (GetWindowThreadProcessId(foregroundWindow, out var pid) == 0)
		{
			return false;
		}
		if (pid == Environment.ProcessId)
		{
			return false;
		}
		char[] array = new char[256];
		int className = GetClassName(foregroundWindow, array, array.Length);
		if (className == 0)
		{
			return false;
		}
		bool flag;
		switch (new string(array, 0, className))
		{
		case "Progman":
		case "WorkerW":
		case "Shell_TrayWnd":
		case "Shell_SecondaryTrayWnd":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			return false;
		}
		if (DwmGetWindowAttribute(foregroundWindow, 14, out int value, 4) == 0 && value != 0)
		{
			return false;
		}
		if (DwmGetWindowAttribute(foregroundWindow, 9, out Rect rect, Marshal.SizeOf<Rect>()) != 0 && !GetWindowRect(foregroundWindow, out rect))
		{
			return false;
		}
		if (rect.Left < (long)target.X + (long)target.Width && rect.Right > target.X && rect.Top < (long)target.Y + (long)target.Height)
		{
			return rect.Bottom > target.Y;
		}
		return false;
	}

	[DllImport("user32.dll")]
	private static extern nint GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern bool IsWindowVisible(nint hwnd);

	[DllImport("user32.dll")]
	private static extern bool IsIconic(nint hwnd);

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(nint hwnd, out uint pid);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int GetClassName(nint hwnd, [Out] char[] name, int size);

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(nint hwnd, out Rect rect);

	[DllImport("dwmapi.dll")]
	private static extern int DwmGetWindowAttribute(nint hwnd, int attribute, out Rect rect, int size);

	[DllImport("dwmapi.dll")]
	private static extern int DwmGetWindowAttribute(nint hwnd, int attribute, out int value, int size);
}
