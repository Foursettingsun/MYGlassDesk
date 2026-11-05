using System;
using System.Diagnostics;

namespace GlassDesk.Platform.Shell;

public static class TaskbarSettingsLink
{
	public static bool Open(out string? error)
	{
		try
		{
			Process.Start(new ProcessStartInfo("ms-settings:taskbar")
			{
				UseShellExecute = true
			});
			error = null;
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}
}
