using System;
using System.Collections.Generic;
using System.IO;
using GlassDesk.Core.Models;
using GlassDesk.Core.Serialization;

namespace GlassDesk.Core.Storage;

public sealed class AppLibraryStore
{
	private readonly JsonFileStore<AppLibraryData> _store;

	private readonly Dictionary<string, AppEntry> _byId = new Dictionary<string, AppEntry>(StringComparer.Ordinal);

	private readonly Dictionary<string, AppEntry> _byKey = new Dictionary<string, AppEntry>(StringComparer.Ordinal);

	private readonly Dictionary<AppEntry, string> _keyByEntry = new Dictionary<AppEntry, string>(ReferenceEqualityComparer.Instance);

	public string FilePath => _store.Path;

	public long Version { get; private set; }

	public IReadOnlyList<AppEntry> Items => Data.Items;

	public int AnomalousEntryCount { get; private set; }

	public int RepairedIconReferenceCount { get; private set; }

	private AppLibraryData Data { get; set; } = new AppLibraryData();

	public AppLibraryStore(string path, TimeProvider? timeProvider = null)
	{
		_store = new JsonFileStore<AppLibraryData>(path, GlassDeskJsonContext.Default.AppLibraryData, timeProvider);
	}

	public LoadOutcome<AppLibraryData> Load()
	{
		LoadOutcome<AppLibraryData> loadOutcome = _store.Load(() => new AppLibraryData());
		Data = loadOutcome.Value;
		AppLibraryData data = Data;
		if (data.Items == null)
		{
			List<AppEntry> list = (data.Items = new List<AppEntry>());
		}
		RebuildIndexes();
		RepairManagedIconReferences();
		Version++;
		return loadOutcome;
	}

	public void Save()
	{
		_store.Save(Data);
	}

	public AppEntry? Find(string? id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		return _byId.GetValueOrDefault(id);
	}

	public AppEntry? FindByKey(string key)
	{
		return _byKey.GetValueOrDefault(key);
	}

	public AddEntryResult Add(AppEntry entry)
	{
		ArgumentNullException.ThrowIfNull(entry, "entry");
		if (!entry.HasLaunchTarget)
		{
			return AddEntryResult.Rejected;
		}
		if (string.IsNullOrWhiteSpace(entry.Id))
		{
			entry.Id = NewId();
		}
		if (_byId.ContainsKey(entry.Id))
		{
			return AddEntryResult.Duplicate;
		}
		string key = AppEntryKey.Compute(entry);
		if (_byKey.ContainsKey(key))
		{
			return AddEntryResult.Duplicate;
		}
		Data.Items.Add(entry);
		Index(entry, key);
		Version++;
		return AddEntryResult.Added;
	}

	public bool Remove(string? id)
	{
		AppEntry appEntry = Find(id);
		if (appEntry == null)
		{
			return false;
		}
		Data.Items.Remove(appEntry);
		_byId.Remove(appEntry.Id);
		Deindex(appEntry);
		Version++;
		return true;
	}

	public bool Update(AppEntry entry)
	{
		ArgumentNullException.ThrowIfNull(entry, "entry");
		AppEntry appEntry = Find(entry.Id);
		if (appEntry == null)
		{
			return false;
		}
		int num = Data.Items.IndexOf(appEntry);
		if (num < 0)
		{
			return false;
		}
		string key = AppEntryKey.Compute(entry);
		if (_byKey.TryGetValue(key, out AppEntry value) && value != appEntry && value != entry)
		{
			return false;
		}
		Deindex(appEntry);
		Data.Items[num] = entry;
		_byId[entry.Id] = entry;
		Index(entry, key);
		Version++;
		return true;
	}

	public static string NewId()
	{
		return Guid.NewGuid().ToString("n");
	}

	private void Index(AppEntry entry, string key)
	{
		_byId[entry.Id] = entry;
		_byKey[key] = entry;
		_keyByEntry[entry] = key;
	}

	private void Deindex(AppEntry entry)
	{
		_byId.Remove(entry.Id);
		if (_keyByEntry.Remove(entry, out string value))
		{
			_byKey.Remove(value);
		}
	}

	private void RebuildIndexes()
	{
		_byId.Clear();
		_byKey.Clear();
		_keyByEntry.Clear();
		AnomalousEntryCount = 0;
		foreach (AppEntry item in Data.Items)
		{
			if (item == null || !item.IsUsable)
			{
				AnomalousEntryCount++;
				continue;
			}
			string text = AppEntryKey.Compute(item);
			if (_byKey.TryAdd(text, item))
			{
				_keyByEntry[item] = text;
			}
			_byId[item.Id] = item;
		}
	}

	private void RepairManagedIconReferences()
	{
		RepairedIconReferenceCount = 0;
		string directoryName = Path.GetDirectoryName(Path.GetFullPath(_store.Path));
		if (string.IsNullOrEmpty(directoryName))
		{
			return;
		}
		foreach (AppEntry item in Data.Items)
		{
			if (item != null && AppPaths.IsManagedCustomIconPath(item.CustomIconPath))
			{
				string customIconPath = item.CustomIconPath;
				string text = AppPaths.ManagedCustomIconPath(directoryName, Path.GetFileName(customIconPath));
				if (!string.Equals(text, customIconPath, StringComparison.OrdinalIgnoreCase) && File.Exists(text))
				{
					item.CustomIconPath = text;
					RepairedIconReferenceCount++;
				}
			}
		}
	}
}
