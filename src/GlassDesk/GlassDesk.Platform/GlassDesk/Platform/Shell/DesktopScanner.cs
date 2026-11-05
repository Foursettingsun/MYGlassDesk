using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GlassDesk.Platform.Shell;

public static class DesktopScanner
{
	public static DesktopScanResult Scan()
	{
		return Scan(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory));
	}

	public static DesktopScanResult Scan(string? currentUserDesktop, string? commonDesktop)
	{
		Dictionary<string, DesktopItem> dictionary = new Dictionary<string, DesktopItem>(StringComparer.OrdinalIgnoreCase);
		List<string> warnings = new List<string>();
		Collect(commonDesktop, fromCurrentUser: false, dictionary, warnings);
		Collect(currentUserDesktop, fromCurrentUser: true, dictionary, warnings);
		return new DesktopScanResult(dictionary.Values.OrderBy((DesktopItem item) => item.DisplayName, StringComparer.CurrentCulture).ThenBy((DesktopItem item) => item.Path, StringComparer.OrdinalIgnoreCase).ToArray(), warnings);
	}

	private static void Collect(string? root, bool fromCurrentUser, Dictionary<string, DesktopItem> items, List<string> warnings)
	{
		if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
		{
			return;
		}
		try
		{
			string[] fileSystemEntries = Directory.GetFileSystemEntries(root);
			foreach (string text in fileSystemEntries)
			{
				try
				{
					FileAttributes attributes = File.GetAttributes(text);
					if ((attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0)
					{
						continue;
					}
					bool flag = (attributes & FileAttributes.Directory) != 0;
					string path = text;
					bool flag2 = !flag;
					if (flag2)
					{
						bool flag3;
						switch (Path.GetExtension(text).ToLowerInvariant())
						{
						case ".lnk":
						case ".url":
						case ".exe":
						case ".com":
						case ".bat":
						case ".cmd":
						case ".ps1":
							flag3 = true;
							break;
						default:
							flag3 = false;
							break;
						}
						flag2 = flag3;
					}
					string fileName = ShellDisplayNames.GetFileName(path, flag2);
					items[Path.GetFileName(text)] = new DesktopItem(text, fileName, flag, fromCurrentUser);
				}
				catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
				{
					warnings.Add("无法读取 " + text + "：" + ex.Message);
				}
			}
		}
		catch (Exception ex2) when ((ex2 is IOException || ex2 is UnauthorizedAccessException) ? true : false)
		{
			warnings.Add("无法读取桌面 " + root + "：" + ex2.Message);
		}
	}
}
