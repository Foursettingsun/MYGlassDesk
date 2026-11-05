using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GlassDesk.Platform.Shell;

public static class StartMenuScanner
{
	private const string ShortcutExtension = ".lnk";

	public static StartMenuScanResult Scan()
	{
		List<string> roots = new List<string>();
		List<StartMenuShortcut> list = new List<StartMenuShortcut>();
		Collect(KnownFolders.CommonPrograms, fromCurrentUser: false, roots, list);
		Collect(KnownFolders.Programs, fromCurrentUser: true, roots, list);
		int count = list.Count;
		Dictionary<string, StartMenuShortcut> dictionary = new Dictionary<string, StartMenuShortcut>(StringComparer.OrdinalIgnoreCase);
		foreach (StartMenuShortcut item in list)
		{
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item.Path);
			string key = ((item.Group.Length == 0) ? fileNameWithoutExtension : (item.Group + "\\" + fileNameWithoutExtension));
			dictionary[key] = item;
		}
		List<StartMenuShortcut> list2 = dictionary.Values.OrderBy((StartMenuShortcut item) => item.Group, StringComparer.CurrentCulture).ThenBy((StartMenuShortcut item) => item.Name, StringComparer.CurrentCulture).ToList();
		return new StartMenuScanResult(list2, count, count - list2.Count, roots);
	}

	private static void Collect(string? root, bool fromCurrentUser, List<string> roots, List<StartMenuShortcut> into)
	{
		if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
		{
			return;
		}
		roots.Add(root);
		IEnumerable<string> enumerable;
		try
		{
			enumerable = EnumerateShortcuts(root);
		}
		catch (Exception ex) when ((ex is UnauthorizedAccessException || ex is IOException) ? true : false)
		{
			return;
		}
		foreach (string item in enumerable)
		{
			string fileName = ShellDisplayNames.GetFileName(item, removeExtension: true);
			if (!string.IsNullOrWhiteSpace(fileName))
			{
				string text = DescribeGroup(root, item);
				into.Add(new StartMenuShortcut(item, fileName, text, fromCurrentUser));
			}
		}
	}

	private static IEnumerable<string> EnumerateShortcuts(string root)
	{
		Stack<string> pending = new Stack<string>(new _003C_003Ez__ReadOnlySingleElementList<string>(root));
		while (pending.Count > 0)
		{
			string directory = pending.Pop();
			string[] files;
			try
			{
				files = Directory.GetFiles(directory, "*.lnk");
			}
			catch (Exception ex) when ((ex is UnauthorizedAccessException || ex is IOException) ? true : false)
			{
				continue;
			}
			string[] array = files;
			for (int i = 0; i < array.Length; i++)
			{
				yield return array[i];
			}
			string[] directories;
			try
			{
				directories = Directory.GetDirectories(directory);
			}
			catch (Exception ex2) when ((ex2 is UnauthorizedAccessException || ex2 is IOException) ? true : false)
			{
				continue;
			}
			string[] array2 = directories;
			foreach (string text in array2)
			{
				if ((File.GetAttributes(text) & FileAttributes.ReparsePoint) == 0)
				{
					pending.Push(text);
				}
			}
		}
	}

	private static string DescribeGroup(string root, string file)
	{
		string relativePath = Path.GetRelativePath(root, file);
		int num = relativePath.LastIndexOf(Path.DirectorySeparatorChar);
		if (num <= 0)
		{
			return string.Empty;
		}
		return relativePath.Substring(0, num).Replace(Path.DirectorySeparatorChar, '/');
	}
}
