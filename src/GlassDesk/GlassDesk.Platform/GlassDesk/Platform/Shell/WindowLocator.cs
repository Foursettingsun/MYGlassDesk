using System;
using System.Collections.Generic;
using System.IO;
using GlassDesk.Platform.Interop;

namespace GlassDesk.Platform.Shell;

public static class WindowLocator
{
	public static IReadOnlyList<WindowCandidateInfo> FindWindowsForAumid(string? aumid)
	{
		if (string.IsNullOrWhiteSpace(aumid))
		{
			return Array.Empty<WindowCandidateInfo>();
		}
		List<WindowCandidateInfo> list = new List<WindowCandidateInfo>();
		foreach (WindowIdentityInfo item in EnumerateTopLevelWindows())
		{
			if (string.Equals(item.EffectiveAumid, aumid, StringComparison.OrdinalIgnoreCase))
			{
				list.Add(new WindowCandidateInfo(item.Handle, item.Title, item.ProcessId, item.IsMinimized));
			}
		}
		return list;
	}

	public static IReadOnlyList<WindowCandidateInfo> FindWindowsForImage(string? imagePath)
	{
		if (string.IsNullOrWhiteSpace(imagePath))
		{
			return Array.Empty<WindowCandidateInfo>();
		}
		string normalized = NormalizePath(imagePath);
		List<WindowCandidateInfo> matches = new List<WindowCandidateInfo>();
		Dictionary<uint, string?> processPaths = new Dictionary<uint, string>();
		try
		{
			User32.EnumWindows((nint window, nint parameter) =>
			{
				if (!IsCandidateWindow(window))
				{
					return 1;
				}
				User32.GetWindowThreadProcessId(window, out var processId);
				if (processId == 0)
				{
					return 1;
				}
				if (!processPaths.TryGetValue(processId, out string value))
				{
					value = TryReadImagePath(processId);
					processPaths[processId] = value;
				}
				if (value != null && string.Equals(value, normalized, StringComparison.OrdinalIgnoreCase))
				{
					matches.Add(new WindowCandidateInfo(window, User32.ReadWindowText(window), processId, User32.IsIconic(window)));
				}
				return 1;
			}, 0);
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return Array.Empty<WindowCandidateInfo>();
		}
		return matches;
	}

	public static IReadOnlyList<WindowIdentityInfo> EnumerateTopLevelWindows()
	{
		List<WindowIdentityInfo> windows = new List<WindowIdentityInfo>();
		Dictionary<uint, string?> imagePaths = new Dictionary<uint, string>();
		Dictionary<uint, string?> processAumids = new Dictionary<uint, string>();
		try
		{
			User32.EnumWindows((nint window, nint parameter) =>
			{
				if (!IsCandidateWindow(window))
				{
					return 1;
				}
				User32.GetWindowThreadProcessId(window, out var processId);
				if (processId == 0)
				{
					return 1;
				}
				if (!imagePaths.TryGetValue(processId, out string value))
				{
					value = TryReadImagePath(processId);
					imagePaths[processId] = value;
				}
				if (!processAumids.TryGetValue(processId, out string value2))
				{
					value2 = WindowIdentity.TryGetProcessAumid(processId);
					processAumids[processId] = value2;
				}
				windows.Add(new WindowIdentityInfo(window, User32.ReadWindowText(window), processId, User32.IsIconic(window), value, WindowIdentity.TryGetWindowAumid(window), value2));
				return 1;
			}, 0);
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return Array.Empty<WindowIdentityInfo>();
		}
		return windows;
	}

	public static bool TryBringToForeground(nint window)
	{
		if (window == 0 || !User32.IsWindow(window))
		{
			return false;
		}
		try
		{
			if (User32.IsIconic(window))
			{
				User32.ShowWindow(window, 9);
			}
			if (User32.SetForegroundWindow(window))
			{
				return true;
			}
			return TryWithAttachedInput(window);
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return false;
		}
	}

	private static bool TryWithAttachedInput(nint window)
	{
		nint foregroundWindow = User32.GetForegroundWindow();
		uint currentThreadId = Kernel32.GetCurrentThreadId();
		uint num = ((foregroundWindow != 0) ? User32.GetWindowThreadProcessId(foregroundWindow, out var _) : 0u);
		if (num == 0 || num == currentThreadId)
		{
			return User32.BringWindowToTop(window);
		}
		if (!User32.AttachThreadInput(currentThreadId, num, attachFlag: true))
		{
			return User32.BringWindowToTop(window);
		}
		try
		{
			User32.BringWindowToTop(window);
			return User32.SetForegroundWindow(window);
		}
		finally
		{
			User32.AttachThreadInput(currentThreadId, num, attachFlag: false);
		}
	}

	private static bool IsCandidateWindow(nint window)
	{
		if (!User32.IsWindowVisible(window))
		{
			return false;
		}
		if (User32.GetWindow(window, 4u) != 0)
		{
			return false;
		}
		if (User32.ReadWindowText(window).Length == 0)
		{
			return false;
		}
		return (User32.GetWindowLong(window, -20) & 0x80) == 0;
	}

	private static string? TryReadImagePath(uint processId)
	{
		nint num = Kernel32.OpenProcess(4096u, inheritHandle: false, processId);
		if (num == 0)
		{
			return null;
		}
		try
		{
			char[] array = new char[1024];
			uint size = (uint)array.Length;
			if (!Kernel32.QueryFullProcessImageName(num, 0u, array, ref size))
			{
				return null;
			}
			return (size == 0) ? null : NormalizePath(new string(array, 0, (int)size));
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return null;
		}
		finally
		{
			Kernel32.CloseHandle(num);
		}
	}

	private static string NormalizePath(string path)
	{
		string text = path.Trim();
		string[] array = new string[3] { "\\??\\", "\\\\?\\", "\\SystemRoot\\" };
		foreach (string text2 in array)
		{
			if (text.StartsWith(text2, StringComparison.OrdinalIgnoreCase))
			{
				text = text.Substring(text2.Length);
				if (text2 == "\\SystemRoot\\")
				{
					text = "C:\\Windows\\" + text;
				}
				break;
			}
		}
		string text3 = Environment.ExpandEnvironmentVariables(text);
		try
		{
			return Path.GetFullPath(text3);
		}
		catch (Exception ex) when ((ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException) ? true : false)
		{
			return text3;
		}
	}
}
