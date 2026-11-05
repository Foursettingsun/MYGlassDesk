using System;
using System.Runtime.InteropServices;
using GlassDesk.Platform.Interop;

namespace GlassDesk.Platform.Shell;

public static class ShellExecutor
{
	public static ShellLaunchOutcome Open(string target, string? arguments = null, string? workingDirectory = null)
	{
		if (string.IsNullOrWhiteSpace(target))
		{
			return new ShellLaunchOutcome(Started: false, 2, 0u);
		}
		nint num = Marshal.StringToHGlobalUni(target);
		nint num2 = (string.IsNullOrEmpty(arguments) ? 0 : Marshal.StringToHGlobalUni(arguments));
		nint num3 = (string.IsNullOrWhiteSpace(workingDirectory) ? 0 : Marshal.StringToHGlobalUni(workingDirectory));
		try
		{
			Shell32.ShellExecuteInfo info = new Shell32.ShellExecuteInfo
			{
				Size = Marshal.SizeOf<Shell32.ShellExecuteInfo>(),
				Mask = 1088u,
				File = num,
				Parameters = num2,
				Directory = num3,
				Show = 1
			};
			if (!Shell32.ShellExecuteEx(ref info))
			{
				return new ShellLaunchOutcome(Started: false, Marshal.GetLastWin32Error(), 0u);
			}
			uint processId = ((info.Process != 0) ? Kernel32.GetProcessId(info.Process) : 0u);
			if (info.Process != 0)
			{
				Kernel32.CloseHandle(info.Process);
			}
			return new ShellLaunchOutcome(Started: true, 0, processId);
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return new ShellLaunchOutcome(Started: false, -1, 0u);
		}
		finally
		{
			FreeIfAllocated(num);
			FreeIfAllocated(num2);
			FreeIfAllocated(num3);
		}
	}

	private static void FreeIfAllocated(nint pointer)
	{
		if (pointer != 0)
		{
			Marshal.FreeHGlobal(pointer);
		}
	}
}
