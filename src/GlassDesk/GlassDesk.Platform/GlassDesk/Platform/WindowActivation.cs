using System;
using GlassDesk.Platform.Interop;

namespace GlassDesk.Platform;

public static class WindowActivation
{
	public static bool TryBringToForeground(nint window)
	{
		if (window == 0)
		{
			return false;
		}
		try
		{
			return User32.SetForegroundWindow(window);
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return false;
		}
	}
}
