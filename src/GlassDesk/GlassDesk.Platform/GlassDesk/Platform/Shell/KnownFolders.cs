using System;
using System.Runtime.InteropServices;
using GlassDesk.Platform.Interop;

namespace GlassDesk.Platform.Shell;

public static class KnownFolders
{
	public static string? CommonPrograms => Resolve(Shell32.FolderIdCommonPrograms);

	public static string? Programs => Resolve(Shell32.FolderIdPrograms);

	private static string? Resolve(Guid folderId)
	{
		nint path = 0;
		try
		{
			if (Shell32.SHGetKnownFolderPath(in folderId, 0u, 0, out path) != 0 || path == 0)
			{
				return null;
			}
			string text = Marshal.PtrToStringUni(path);
			return string.IsNullOrWhiteSpace(text) ? null : text;
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return null;
		}
		finally
		{
			if (path != 0)
			{
				Ole32.CoTaskMemFree(path);
			}
		}
	}
}
