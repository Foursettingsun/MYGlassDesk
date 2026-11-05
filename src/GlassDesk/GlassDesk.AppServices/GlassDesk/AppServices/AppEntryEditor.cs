using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GlassDesk.Core.Models;
using GlassDesk.Core.Storage;

namespace GlassDesk.AppServices;

public sealed class AppEntryEditor(AppLibraryStore library, AppPaths paths, Action changed)
{
	public bool Edit(string id, string displayName, IReadOnlyList<string> aliases, string? iconFile)
	{
		AppEntry appEntry = library.Find(id);
		string text = displayName.Trim();
		bool flag = appEntry == null;
		if (!flag)
		{
			int length = text.Length;
			bool flag2 = ((length < 1 || length > 120) ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			return false;
		}
		List<string> searchAliases = (from value in aliases
			select value.Trim() into value
			where value.Length > 0
			select value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		string text2 = null;
		if (!string.IsNullOrWhiteSpace(iconFile))
		{
			ImageFormat? imageFormat = ImageFormatSniffer.Detect(iconFile);
			if (!imageFormat.HasValue)
			{
				return false;
			}
			string text3 = ImageFormatSniffer.FileExtensionFor(imageFormat.Value);
			text2 = Path.Combine(paths.IconCacheDirectory, "custom-" + id + "-" + Guid.NewGuid().ToString("n") + text3);
			Directory.CreateDirectory(paths.IconCacheDirectory);
			File.Copy(iconFile, text2);
		}
		AppEntry entry = new AppEntry
		{
			Id = appEntry.Id,
			DisplayName = text,
			Kind = appEntry.Kind,
			Target = appEntry.Target,
			Arguments = appEntry.Arguments,
			WorkingDirectory = appEntry.WorkingDirectory,
			AppUserModelId = appEntry.AppUserModelId,
			IconSource = appEntry.IconSource,
			CustomIconPath = (text2 ?? appEntry.CustomIconPath),
			SearchAliases = searchAliases,
			PinyinIndex = null,
			PinyinInitials = null,
			IsTargetMissing = appEntry.IsTargetMissing,
			AddedAtUtc = appEntry.AddedAtUtc
		};
		if (!library.Update(entry))
		{
			if (text2 != null)
			{
				File.Delete(text2);
			}
			return false;
		}
		try
		{
			library.Save();
		}
		catch
		{
			library.Update(appEntry);
			if (text2 != null)
			{
				File.Delete(text2);
			}
			throw;
		}
		if (text2 != null)
		{
			string customIconPath = appEntry.CustomIconPath;
			if (customIconPath != null && File.Exists(customIconPath) && Path.GetFullPath(customIconPath).StartsWith(Path.GetFullPath(paths.IconCacheDirectory) + Path.DirectorySeparatorChar + "custom-", StringComparison.OrdinalIgnoreCase))
			{
				try
				{
					File.Delete(customIconPath);
				}
				catch (IOException)
				{
				}
			}
		}
		changed();
		return true;
	}
}
