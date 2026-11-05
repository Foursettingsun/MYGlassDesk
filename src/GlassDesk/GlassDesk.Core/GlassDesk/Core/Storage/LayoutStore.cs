using System;
using System.Collections.Generic;
using System.Linq;
using GlassDesk.Core.Models;
using GlassDesk.Core.Repair;
using GlassDesk.Core.Serialization;

namespace GlassDesk.Core.Storage;

public sealed class LayoutStore
{
	private readonly JsonFileStore<DockLayout> _dockStore;

	private readonly JsonFileStore<LauncherLayout> _launcherStore;

	private List<string> _lastSavedDockPins = new List<string>();

	private LauncherLayout _lastSavedLauncher = new LauncherLayout();

	public DockLayout Dock { get; private set; } = new DockLayout();

	public LauncherLayout Launcher { get; private set; } = new LauncherLayout();

	public long LauncherVersion { get; private set; }

	public string DockFilePath => _dockStore.Path;

	public string LauncherFilePath => _launcherStore.Path;

	public LayoutStore(string dockLayoutPath, string launcherLayoutPath, TimeProvider? timeProvider = null)
	{
		_dockStore = new JsonFileStore<DockLayout>(dockLayoutPath, GlassDeskJsonContext.Default.DockLayout, timeProvider);
		_launcherStore = new JsonFileStore<LauncherLayout>(launcherLayoutPath, GlassDeskJsonContext.Default.LauncherLayout, timeProvider);
	}

	public LoadOutcome<DockLayout> LoadDock()
	{
		LoadOutcome<DockLayout> loadOutcome = _dockStore.Load(() => new DockLayout());
		Dock = loadOutcome.Value;
		DockLayout dock = Dock;
		if (dock.PinnedItemIds == null)
		{
			List<string> list = (dock.PinnedItemIds = new List<string>());
		}
		_lastSavedDockPins = Dock.PinnedItemIds.ToList();
		return loadOutcome;
	}

	public LoadOutcome<LauncherLayout> LoadLauncher()
	{
		LoadOutcome<LauncherLayout> loadOutcome = _launcherStore.Load(() => new LauncherLayout());
		Launcher = loadOutcome.Value;
		LauncherLayout launcher = Launcher;
		if (launcher.Pages == null)
		{
			List<LauncherPage> list = (launcher.Pages = new List<LauncherPage>());
		}
		_lastSavedLauncher = LauncherLayoutRules.DeepClone(Launcher);
		LauncherVersion++;
		return loadOutcome;
	}

	public void SaveDock()
	{
		try
		{
			_dockStore.Save(Dock);
			_lastSavedDockPins = Dock.PinnedItemIds.ToList();
		}
		catch
		{
			Dock.PinnedItemIds = _lastSavedDockPins.ToList();
			throw;
		}
	}

	public void SaveLauncher()
	{
		try
		{
			_launcherStore.Save(Launcher);
			_lastSavedLauncher = LauncherLayoutRules.DeepClone(Launcher);
			LauncherVersion++;
		}
		catch
		{
			LauncherLayout launcherLayout = LauncherLayoutRules.DeepClone(_lastSavedLauncher);
			Launcher.Pages = launcherLayout.Pages;
			Launcher.ExcludedAppIds = launcherLayout.ExcludedAppIds;
			Launcher.SchemaVersion = launcherLayout.SchemaVersion;
			Launcher.FlatOrder = launcherLayout.FlatOrder;
			Launcher.FilterGroups = launcherLayout.FilterGroups;
			LauncherPagingRepair.CopyStorePaging(this, launcherLayout);
			throw;
		}
	}

	public bool CommitLauncher(LauncherLayout draft, long expectedVersion)
	{
		ArgumentNullException.ThrowIfNull(draft, "draft");
		if (expectedVersion != LauncherVersion)
		{
			return false;
		}
		if (LauncherLayoutRules.Equivalent(Launcher, draft))
		{
			return true;
		}
		LauncherLayout launcherLayout = LauncherLayoutRules.DeepClone(draft);
		Launcher.Pages = launcherLayout.Pages;
		Launcher.ExcludedAppIds = launcherLayout.ExcludedAppIds;
		Launcher.SchemaVersion = launcherLayout.SchemaVersion;
		Launcher.FlatOrder = launcherLayout.FlatOrder;
		Launcher.FilterGroups = launcherLayout.FilterGroups;
		LauncherPagingRepair.CopyStorePaging(this, launcherLayout);
		SaveLauncher();
		return true;
	}

	public bool UnpinFromDock(string? itemId)
	{
		if (string.IsNullOrEmpty(itemId))
		{
			return false;
		}
		return Dock.PinnedItemIds.Remove(itemId);
	}

	public int PurgeItem(string? itemId)
	{
		if (string.IsNullOrEmpty(itemId))
		{
			return 0;
		}
		int num = 0;
		if (Dock.PinnedItemIds.Remove(itemId))
		{
			num++;
		}
		foreach (LauncherPage page in Launcher.Pages)
		{
			foreach (LauncherSlot slot in page.Slots)
			{
				if (slot.Kind == LauncherSlotKind.Group && slot.Group != null)
				{
					num += slot.Group.ItemIds.RemoveAll((string id) => string.Equals(id, itemId, StringComparison.Ordinal));
				}
			}
			num += page.Slots.RemoveAll((LauncherSlot slot) => slot.Kind == LauncherSlotKind.App && string.Equals(slot.ItemId, itemId, StringComparison.Ordinal));
		}
		num += RemoveEmptyGroups();
		LauncherFilterRules.Normalize(Launcher);
		return num;
	}

	public int RemoveEmptyGroups()
	{
		int num = 0;
		foreach (LauncherPage page in Launcher.Pages)
		{
			num += page.Slots.RemoveAll((LauncherSlot slot) => slot.Kind == LauncherSlotKind.Group && (slot.Group == null || slot.Group.ItemIds.Count == 0));
		}
		return num;
	}

	public int RemoveEmptyPages()
	{
		return Launcher.Pages.RemoveAll((LauncherPage page) => page.Slots.Count == 0);
	}
}
