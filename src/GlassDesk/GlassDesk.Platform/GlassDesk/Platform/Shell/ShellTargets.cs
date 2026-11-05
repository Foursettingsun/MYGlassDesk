using System;
using GlassDesk.Platform.Interop;

namespace GlassDesk.Platform.Shell;

public static class ShellTargets
{
	public static bool? CanResolve(string? target)
	{
		if (string.IsNullOrWhiteSpace(target))
		{
			return false;
		}
		nint itemIdList = 0;
		try
		{
			uint attributesOut;
			return Shell32.SHParseDisplayName(target, 0, out itemIdList, 0u, out attributesOut) == 0 && itemIdList != 0;
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return null;
		}
		finally
		{
			if (itemIdList != 0)
			{
				Shell32.ILFree(itemIdList);
			}
		}
	}
}
