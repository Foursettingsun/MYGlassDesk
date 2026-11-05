using System;
using System.Collections.Generic;
using System.Linq;
using GlassDesk.Core.Contracts;
using GlassDesk.Core.Models;
using GlassDesk.Core.Storage;

namespace GlassDesk.AppServices;

public sealed class ImportService : IImportService
{
	private readonly AppLibraryStore _library;

	private readonly IconCache _icons;

	private readonly IImportObserver? _observer;

	private readonly LayoutStore? _layouts;

	private readonly Func<int> _launcherCapacity;

	public int IconsWritten { get; private set; }

	public int IconsReused { get; private set; }

	public ImportService(AppLibraryStore library, IconCache icons, IImportObserver? observer = null)
		: this(library, icons, observer, null)
	{
	}

	public ImportService(AppLibraryStore library, IconCache icons, IImportObserver? observer, LayoutStore? layouts, Func<int>? launcherCapacity = null)
	{
		ArgumentNullException.ThrowIfNull(library, "library");
		ArgumentNullException.ThrowIfNull(icons, "icons");
		_library = library;
		_icons = icons;
		_observer = observer;
		_layouts = layouts;
		_launcherCapacity = launcherCapacity ?? ((Func<int>)(() => 35));
	}

	public ImportResult ImportItems(IReadOnlyList<ImportRequest> requests)
	{
		ArgumentNullException.ThrowIfNull(requests, "requests");
		List<AppEntry> list = new List<AppEntry>();
		List<AppEntry> list2 = new List<AppEntry>();
		List<ImportFailure> list3 = new List<ImportFailure>();
		if (requests.Count == 0)
		{
			return new ImportResult();
		}
		IconsWritten = 0;
		IconsReused = 0;
		foreach (ImportRequest request in requests)
		{
			if (request != null)
			{
				try
				{
					ImportOne(request, list, list2, list3);
				}
				catch (Exception ex) when (!(ex is OutOfMemoryException))
				{
					list3.Add(new ImportFailure
					{
						Source = (request.Source ?? string.Empty),
						Reason = ImportFailureReason.Other,
						Message = "处理时出错：" + ex.Message
					});
				}
			}
		}
		if (list.Count > 0)
		{
			try
			{
				_library.Save();
			}
			catch (StorageException)
			{
				foreach (AppEntry item in list)
				{
					_library.Remove(item.Id);
				}
				throw;
			}
		}
		bool flag = false;
		if (_layouts != null && list.Count + list2.Count > 0)
		{
			int capacity = Math.Max(1, _launcherCapacity());
			LauncherLayout source = LauncherLayoutRules.Reconcile(_layouts.Launcher, from item in _library.Items
				where item.IsUsable
				select item.Id, capacity);
			source = LauncherLayoutEditor.AddShortcuts(source, from item in list.Concat(list2)
				where item.IsUsable
				select item.Id, capacity);
			flag = !LauncherLayoutRules.Equivalent(_layouts.Launcher, source);
			if (flag && !_layouts.CommitLauncher(source, _layouts.LauncherVersion))
			{
				throw new StorageException("启动台布局已变化，请重试添加。", _layouts.LauncherFilePath);
			}
		}
		if ((list.Count > 0) | flag)
		{
			_observer?.OnLibraryChanged();
		}
		return new ImportResult
		{
			Added = list,
			Duplicates = list2,
			Failures = list3
		};
	}

	public void ImportOne(ImportRequest request, List<AppEntry> added, List<AppEntry> duplicates, List<ImportFailure> failures)
	{
		ArgumentNullException.ThrowIfNull(request, "request");
		ArgumentNullException.ThrowIfNull(added, "added");
		ArgumentNullException.ThrowIfNull(duplicates, "duplicates");
		ArgumentNullException.ThrowIfNull(failures, "failures");
		var (appEntry, reason, message) = AppEntryFactory.TryCreate(request);
		if (appEntry == null)
		{
			failures.Add(new ImportFailure
			{
				Source = (request.Source ?? string.Empty),
				Reason = reason,
				Message = message
			});
			return;
		}
		AppEntry appEntry2 = _library.FindByKey(AppEntryKey.Compute(appEntry));
		if (appEntry2 != null)
		{
			duplicates.Add(appEntry2);
			return;
		}
		CacheIcon(appEntry);
		if (_library.Add(appEntry) != AddEntryResult.Added)
		{
			duplicates.Add(appEntry);
		}
		else
		{
			added.Add(appEntry);
		}
	}

	public void CacheIcon(AppEntry entry)
	{
		ArgumentNullException.ThrowIfNull(entry, "entry");
		CachedIcon cachedIcon = _icons.EnsureFor(entry.IconSource);
		if ((object)cachedIcon != null)
		{
			if (cachedIcon.CreatedFile)
			{
				IconsWritten++;
			}
			else
			{
				IconsReused++;
			}
		}
	}
}
