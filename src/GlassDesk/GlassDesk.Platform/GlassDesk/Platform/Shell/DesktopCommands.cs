using System;

namespace GlassDesk.Platform.Shell;

public static class DesktopCommands
{
	public static int ShowDesktop()
	{
		int num = 0;
		uint processId = (uint)Environment.ProcessId;
		foreach (WindowIdentityInfo item in WindowLocator.EnumerateTopLevelWindows())
		{
			if (item.ProcessId != processId && !item.IsMinimized && WindowControl.Minimize(item.Handle))
			{
				num++;
			}
		}
		return num;
	}
}
