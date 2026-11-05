using System;
using System.IO;
using System.Runtime.InteropServices;
using GlassDesk.Platform.Interop;

namespace GlassDesk.Platform.Shell;

public static class ShellDisplayNames
{
	public static string GetFileName(string path, bool removeExtension = false)
	{
		string text = Read(path) ?? Path.GetFileName(Path.TrimEndingDirectorySeparator(path));
		if (string.IsNullOrWhiteSpace(text))
		{
			return path.TrimEnd(new char[2]
			{
				Path.DirectorySeparatorChar,
				Path.AltDirectorySeparatorChar
			});
		}
		string extension = Path.GetExtension(path);
		if (removeExtension && extension.Length > 0 && text.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
		{
			string text2 = text;
			int length = extension.Length;
			text = text2.Substring(0, text2.Length - length);
		}
		return text;
	}

	private static string? Read(string path)
	{
		nint itemIdList = 0;
		nint name = 0;
		try
		{
			if (Shell32.SHParseDisplayName(path, 0, out itemIdList, 0u, out var _) != 0 || itemIdList == 0)
			{
				return null;
			}
			if (Shell32.SHGetNameFromIDList(itemIdList, 0u, out name) != 0 || name == 0)
			{
				return null;
			}
			string text = Marshal.PtrToStringUni(name);
			return string.IsNullOrWhiteSpace(text) ? null : text;
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException || ex is COMException || ex is ArgumentException) ? true : false)
		{
			return null;
		}
		finally
		{
			if (name != 0)
			{
				Ole32.CoTaskMemFree(name);
			}
			if (itemIdList != 0)
			{
				Shell32.ILFree(itemIdList);
			}
		}
	}
}
