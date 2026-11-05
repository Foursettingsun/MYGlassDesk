using System;
using System.Collections.Generic;
using System.IO;
using GlassDesk.Core.Models;
using GlassDesk.Platform.Shell;

namespace GlassDesk.AppServices;

public static class ItemKindDetector
{
	private static readonly HashSet<string> ProgramExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".exe", ".com", ".bat", ".cmd", ".ps1" };

	public const string ShortcutExtension = ".lnk";

	public const string InternetShortcutExtension = ".url";

	public static AppItemKind Detect(string path, bool isDirectory)
	{
		if (isDirectory)
		{
			return AppItemKind.Folder;
		}
		string extension = Path.GetExtension(path);
		if (string.Equals(extension, ".lnk", StringComparison.OrdinalIgnoreCase))
		{
			return AppItemKind.Shortcut;
		}
		if (string.Equals(extension, ".url", StringComparison.OrdinalIgnoreCase))
		{
			return AppItemKind.Url;
		}
		if (!ProgramExtensions.Contains(extension))
		{
			return AppItemKind.File;
		}
		return AppItemKind.Program;
	}

	public static string DeriveDisplayName(string path, AppItemKind kind)
	{
		if (((uint)kind <= 2u || kind == AppItemKind.Url) ? true : false)
		{
			return ShellDisplayNames.GetFileName(path, kind != AppItemKind.Folder);
		}
		string fileName = Path.GetFileName(path);
		if (string.IsNullOrWhiteSpace(fileName))
		{
			return path.TrimEnd(new char[2]
			{
				Path.DirectorySeparatorChar,
				Path.AltDirectorySeparatorChar
			});
		}
		return fileName;
	}
}
