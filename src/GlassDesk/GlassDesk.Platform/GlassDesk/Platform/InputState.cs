using System;
using GlassDesk.Platform.Interop;

namespace GlassDesk.Platform;

public static class InputState
{
	public static bool IsPrimaryMouseButtonDown()
	{
		try
		{
			return (User32.GetAsyncKeyState(1) & 0x8000) != 0;
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return false;
		}
	}
}
