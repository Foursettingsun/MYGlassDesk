using System;
using System.Collections.Generic;
using System.IO;
using GlassDesk.Core.Models;
using GlassDesk.Core.Storage;

namespace GlassDesk.AppServices;

public sealed class LibraryMaintenance
{
	private readonly AppLibraryStore _library;

	private readonly IconCache _icons;

	private readonly IImportObserver? _observer;

	public LibraryMaintenance(AppLibraryStore library, IconCache icons, IImportObserver? observer = null)
	{
		ArgumentNullException.ThrowIfNull(library, "library");
		ArgumentNullException.ThrowIfNull(icons, "icons");
		_library = library;
		_icons = icons;
		_observer = observer;
	}

	public MissingTargetReport RefreshMissingTargets(bool persist = true)
	{
		List<AppEntry> list = new List<AppEntry>();
		List<AppEntry> list2 = new List<AppEntry>();
		int num = 0;
		int num2 = 0;
		foreach (AppEntry item in _library.Items)
		{
			if (item == null || !item.IsUsable)
			{
				continue;
			}
			num2++;
			if (item.Kind == AppItemKind.Url)
			{
				continue;
			}
			bool flag = LaunchService.IsTargetGone(item);
			if (flag)
			{
				num++;
			}
			if (flag != item.IsTargetMissing)
			{
				item.IsTargetMissing = flag;
				if (flag)
				{
					list.Add(item);
				}
				else
				{
					list2.Add(item);
				}
			}
		}
		bool flag2 = list.Count > 0 || list2.Count > 0;
		if (flag2 & persist)
		{
			_library.Save();
			_observer?.OnLibraryChanged();
		}
		return new MissingTargetReport(num2, num, list, list2, flag2);
	}

	public RelocationOutcome Relocate(string itemId, string newPath)
	{
		AppEntry appEntry = _library.Find(itemId);
		if (appEntry == null)
		{
			return new RelocationOutcome(RelocateResult.UnknownItem, "应用库里没有这一条记录。");
		}
		if (appEntry.Kind == AppItemKind.Url)
		{
			return new RelocationOutcome(RelocateResult.NotRelocatable, "网址不需要定位到文件。");
		}
		if (string.IsNullOrWhiteSpace(newPath))
		{
			return new RelocationOutcome(RelocateResult.TargetNotFound, "没有给出新位置。");
		}
		string fullPath;
		try
		{
			fullPath = Path.GetFullPath(newPath.Trim());
		}
		catch (Exception ex) when ((ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException) ? true : false)
		{
			return new RelocationOutcome(RelocateResult.TargetNotFound, "新位置无法解析：" + newPath);
		}
		if (!ExistsQuietly(fullPath))
		{
			return new RelocationOutcome(RelocateResult.TargetNotFound, "新位置不存在：" + fullPath);
		}
		AppEntry entry = CloneWithTarget(appEntry, fullPath);
		AppEntry appEntry2 = _library.FindByKey(AppEntryKey.Compute(entry));
		if (appEntry2 != null && !string.Equals(appEntry2.Id, appEntry.Id, StringComparison.Ordinal))
		{
			return new RelocationOutcome(RelocateResult.ConflictsWithExisting, "已经有另一条记录指向这个位置：" + appEntry2.DisplayName, appEntry2.Id);
		}
		string target = appEntry.Target;
		AppItemKind kind = appEntry.Kind;
		string iconSource = appEntry.IconSource;
		bool isTargetMissing = appEntry.IsTargetMissing;
		appEntry.Target = fullPath;
		appEntry.Kind = ItemKindDetector.Detect(fullPath, Directory.Exists(fullPath));
		appEntry.IsTargetMissing = false;
		if (string.IsNullOrWhiteSpace(iconSource) || string.Equals(iconSource, target, StringComparison.OrdinalIgnoreCase))
		{
			appEntry.IconSource = fullPath;
		}
		if (!_library.Update(appEntry))
		{
			appEntry.Target = target;
			appEntry.Kind = kind;
			appEntry.IconSource = iconSource;
			appEntry.IsTargetMissing = isTargetMissing;
			return new RelocationOutcome(RelocateResult.UnknownItem, "应用库索引里找不到这一条记录。");
		}
		try
		{
			_library.Save();
		}
		catch (StorageException ex2)
		{
			appEntry.Target = target;
			appEntry.Kind = kind;
			appEntry.IconSource = iconSource;
			appEntry.IsTargetMissing = isTargetMissing;
			_library.Update(appEntry);
			return new RelocationOutcome(RelocateResult.SaveFailed, "重新定位未能保存，原位置已恢复：" + ex2.Message);
		}
		CacheIcon(appEntry);
		_observer?.OnLibraryChanged();
		return new RelocationOutcome(RelocateResult.Relocated, "已重新定位到：" + fullPath);
	}

	public bool SetCustomIcon(string itemId, string? iconFilePath)
	{
		AppEntry appEntry = _library.Find(itemId);
		if (appEntry == null)
		{
			return false;
		}
		if (!_icons.ApplyCustomIcon(appEntry, iconFilePath))
		{
			return false;
		}
		_library.Update(appEntry);
		_library.Save();
		_observer?.OnLibraryChanged();
		return true;
	}

	private void CacheIcon(AppEntry entry)
	{
		_icons.EnsureFor(entry.IconSource);
	}

	private static AppEntry CloneWithTarget(AppEntry entry, string target)
	{
		return new AppEntry
		{
			Id = entry.Id,
			Target = target,
			Kind = ItemKindDetector.Detect(target, Directory.Exists(target)),
			Arguments = entry.Arguments
		};
	}

	private static bool ExistsQuietly(string path)
	{
		try
		{
			return File.Exists(path) || Directory.Exists(path);
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
			return false;
		}
	}
}
