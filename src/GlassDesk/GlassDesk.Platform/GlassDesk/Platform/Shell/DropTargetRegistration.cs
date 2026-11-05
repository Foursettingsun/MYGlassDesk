using System;
using GlassDesk.Platform.Interop;

namespace GlassDesk.Platform.Shell;

public static class DropTargetRegistration
{
	private const string OleDropTargetInterfaceAtom = "OleDropTargetInterface";

	public static bool IsOleDropTargetRegistered(nint window)
	{
		if (window == 0)
		{
			return false;
		}
		try
		{
			return User32.GetProp(window, "OleDropTargetInterface") != 0;
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return false;
		}
	}
}
