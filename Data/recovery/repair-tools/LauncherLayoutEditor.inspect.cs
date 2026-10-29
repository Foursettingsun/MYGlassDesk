using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace GlassDesk.Core.Models;

public static class LauncherLayoutEditor
{
	public static LauncherLayout PlaceAtSlot(LauncherLayout source, string slotId, int finalIndex, int capacity, bool exchange)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1, "capacity");
		LauncherLayout launcherLayout = LauncherLayoutRules.DeepClone(source);
		(LauncherPage Page, int Index) tuple = Find(launcherLayout, slotId);
		LauncherPage item = tuple.Page;
		int item2 = tuple.Index;
		int index = Math.Clamp(finalIndex / capacity, 0, launcherLayout.Pages.Count);
		int num = Math.Max(0, finalIndex % capacity);
		EnsurePage(launcherLayout, index);
		LauncherPage launcherPage = launcherLayout.Pages[index];
		LauncherSlot launcherSlot = item.Slots[item2];
		if (exchange && num < launcherPage.Slots.Count)
		{
			List<LauncherSlot> slots = item.Slots;
			int index2 = item2;
			List<LauncherSlot> slots2 = launcherPage.Slots;
			int index3 = num;
			LauncherSlot value = launcherPage.Slots[num];
			LauncherSlot value2 = launcherSlot;
			slots[index2] = value;
			slots2[index3] = value2;
		}
		else
		{
			item.Slots.RemoveAt(item2);
			launcherPage.Slots.Insert(Math.Min(num, launcherPage.Slots.Count), launcherSlot);
		}
		return Finish(launcherLayout, capacity);
	}

	public static LauncherLayout Move(LauncherLayout source, string slotId, int page, int index, int capacity)
	{
		var (item, num) = Find(source, slotId);
		if (source.Pages.IndexOf(item) == page && index > num)
		{
			index--;
		}
		return PlaceAtSlot(source, slotId, Math.Max(0, page) * capacity + Math.Max(0, index), capacity, exchange: false);
	}

	public static LauncherLayout RemoveShortcut(LauncherLayout source, string slotId, int capacity)
	{
		LauncherLayout launcherLayout = LauncherLayoutRules.DeepClone(source);
		foreach (LauncherPage page in launcherLayout.Pages)
		{
			int num = page.Slots.FindIndex((LauncherSlot slot) => SlotId(slot) == slotId);
			if (num >= 0)
			{
				page.Slots.RemoveAt(num);
				return Finish(launcherLayout, capacity);
			}
			LauncherSlot launcherSlot = page.Slots.FirstOrDefault((LauncherSlot slot) => slot.Group?.ItemIds.Contains<string>(slotId, StringComparer.Ordinal) ?? false);
			LauncherGroup launcherGroup = launcherSlot?.Group;
			if (launcherGroup != null)
			{
				launcherGroup.ItemIds.Remove(slotId);
				CollapseGroup(page, launcherSlot);
				return Finish(launcherLayout, capacity);
			}
		}
		throw new InvalidOperationException("The dragged item no longer exists.");
	}

	public static LauncherLayout CreateGroup(LauncherLayout source, string movingId, string targetId, string groupId, int capacity)
	{
		if (movingId == targetId)
		{
			throw new InvalidOperationException("An item cannot be grouped with itself.");
		}
		LauncherLayout layout = LauncherLayoutRules.DeepClone(source);
		(LauncherPage Page, int Index) tuple = Find(layout, movingId);
		LauncherPage item = tuple.Page;
		int item2 = tuple.Index;
		(LauncherPage Page, int Index) tuple2 = Find(layout, targetId);
		LauncherPage item3 = tuple2.Page;
		int item4 = tuple2.Index;
		LauncherSlot launcherSlot = item.Slots[item2];
		LauncherSlot launcherSlot2 = item3.Slots[item4];
		if (launcherSlot.Kind != LauncherSlotKind.App || launcherSlot2.Kind != LauncherSlotKind.App)
		{
			throw new InvalidOperationException("Both items must be top-level applications.");
		}
		item.Slots.Remove(launcherSlot);
		List<LauncherSlot> slots = item3.Slots;
		int index = item3.Slots.IndexOf(launcherSlot2);
		LauncherSlot obj = new LauncherSlot
		{
			Kind = LauncherSlotKind.Group
		};
		LauncherGroup obj2 = new LauncherGroup
		{
			Id = groupId,
			Name = "新建文件夹"
		};
		int num = 2;
		List<string> list = new List<string>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<string> span = CollectionsMarshal.AsSpan(list);
		span[0] = targetId;
		span[1] = movingId;
		obj2.ItemIds = list;
		obj.Group = obj2;
		slots[index] = obj;
		return Finish(layout, capacity);
	}

	public static LauncherLayout AddToGroup(LauncherLayout source, string itemId, string groupId, int capacity)
	{
		LauncherLayout layout = LauncherLayoutRules.DeepClone(source);
		(LauncherPage Page, int Index) tuple = Find(layout, itemId);
		LauncherPage item = tuple.Page;
		int item2 = tuple.Index;
		LauncherGroup launcherGroup = FindGroup(layout, groupId).Slot.Group;
		if (item.Slots[item2].Kind != LauncherSlotKind.App)
		{
			throw new InvalidOperationException("Only an application can enter a group.");
		}
		item.Slots.RemoveAt(item2);
		if (!launcherGroup.ItemIds.Contains<string>(itemId, StringComparer.Ordinal))
		{
			launcherGroup.ItemIds.Add(itemId);
		}
		return Finish(layout, capacity);
	}

	public static LauncherLayout RemoveFromGroup(LauncherLayout source, string itemId, string groupId, int page, int index, int capacity)
	{
		LauncherLayout launcherLayout = LauncherLayoutRules.DeepClone(source);
		var (page2, launcherSlot) = FindGroup(launcherLayout, groupId);
		if (!launcherSlot.Group.ItemIds.Remove(itemId))
		{
			throw new InvalidOperationException("Item not in group.");
		}
		CollapseGroup(page2, launcherSlot);
		page = Math.Clamp(page, 0, launcherLayout.Pages.Count);
		EnsurePage(launcherLayout, page);
		launcherLayout.Pages[page].Slots.Insert(Math.Clamp(index, 0, launcherLayout.Pages[page].Slots.Count), new LauncherSlot
		{
			ItemId = itemId
		});
		return Finish(launcherLayout, capacity);
	}

	public static LauncherLayout Dissolve(LauncherLayout source, string groupId, int capacity)
	{
		LauncherLayout layout = LauncherLayoutRules.DeepClone(source);
		(LauncherPage Page, LauncherSlot Slot) tuple = FindGroup(layout, groupId);
		LauncherPage item = tuple.Page;
		LauncherSlot item2 = tuple.Slot;
		int index = item.Slots.IndexOf(item2);
		item.Slots.RemoveAt(index);
		item.Slots.InsertRange(index, item2.Group.ItemIds.Select((string id) => new LauncherSlot
		{
			ItemId = id
		}));
		return Finish(layout, capacity);
	}

	public static LauncherLayout MoveGroupMember(LauncherLayout source, string groupId, string itemId, int index)
	{
		LauncherLayout launcherLayout = LauncherLayoutRules.DeepClone(source);
		LauncherGroup launcherGroup = FindGroup(launcherLayout, groupId).Slot.Group;
		if (!launcherGroup.ItemIds.Remove(itemId))
		{
			throw new InvalidOperationException("Item not in group.");
		}
		launcherGroup.ItemIds.Insert(Math.Clamp(index, 0, launcherGroup.ItemIds.Count), itemId);
		return launcherLayout;
	}

	public static LauncherLayout RenameGroup(LauncherLayout source, string groupId, string name)
	{
		string text = name.Trim();
		int length = text.Length;
		if ((length < 1 || length > 60) ? true : false)
		{
			throw new ArgumentException("Group name must be 1–60 characters.", "name");
		}
		LauncherLayout launcherLayout = LauncherLayoutRules.DeepClone(source);
		FindGroup(launcherLayout, groupId).Slot.Group.Name = text;
		return launcherLayout;
	}

	public static LauncherLayout AddShortcuts(LauncherLayout source, IEnumerable<string> ids, int capacity)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1, "capacity");
		LauncherLayout launcherLayout = LauncherLayoutRules.DeepClone(source);
		HashSet<string> hashSet = LauncherLayoutRules.AppIds(source).ToHashSet<string>(StringComparer.Ordinal);
		EnsurePage(launcherLayout, 0);
		foreach (string item in ids.Where((string id) => !string.IsNullOrWhiteSpace(id)))
		{
			if (hashSet.Add(item))
			{
				List<LauncherPage> pages = launcherLayout.Pages;
				if (pages[pages.Count - 1].Slots.Count >= capacity)
				{
					EnsurePage(launcherLayout, launcherLayout.Pages.Count);
				}
				List<LauncherPage> pages2 = launcherLayout.Pages;
				pages2[pages2.Count - 1].Slots.Add(new LauncherSlot
				{
					ItemId = item
				});
			}
		}
		return Finish(launcherLayout, capacity);
	}

	private static string? SlotId(LauncherSlot slot)
	{
		if (slot.Kind != LauncherSlotKind.Group)
		{
			return slot.ItemId;
		}
		return slot.Group?.Id;
	}

	private static (LauncherPage Page, int Index) Find(LauncherLayout layout, string id)
	{
		foreach (LauncherPage page in layout.Pages)
		{
			int num = page.Slots.FindIndex((LauncherSlot slot) => SlotId(slot) == id);
			if (num >= 0)
			{
				return (Page: page, Index: num);
			}
		}
		throw new InvalidOperationException("The dragged item no longer exists.");
	}

	private static (LauncherPage Page, LauncherSlot Slot) FindGroup(LauncherLayout layout, string id)
	{
		var (launcherPage, index) = Find(layout, id);
		if (launcherPage.Slots[index].Group == null)
		{
			throw new InvalidOperationException("Group not found.");
		}
		return (Page: launcherPage, Slot: launcherPage.Slots[index]);
	}

	private static void CollapseGroup(LauncherPage page, LauncherSlot slot)
	{
		if (slot.Group.ItemIds.Count == 0)
		{
			page.Slots.Remove(slot);
		}
		else if (slot.Group.ItemIds.Count == 1)
		{
			page.Slots[page.Slots.IndexOf(slot)] = new LauncherSlot
			{
				ItemId = slot.Group.ItemIds[0]
			};
		}
	}

	private static void EnsurePage(LauncherLayout layout, int index)
	{
		while (layout.Pages.Count <= index)
		{
			layout.Pages.Add(new LauncherPage
			{
				Id = "page-" + Guid.NewGuid().ToString("N")
			});
		}
	}

	internal static LauncherLayout Finish(LauncherLayout layout, int capacity)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1, "capacity");
		for (int i = 0; i < layout.Pages.Count; i++)
		{
			List<LauncherSlot> slots = layout.Pages[i].Slots;
			if (slots.Count > capacity)
			{
				EnsurePage(layout, i + 1);
				layout.Pages[i + 1].Slots.InsertRange(0, slots.Skip(capacity));
				slots.RemoveRange(capacity, slots.Count - capacity);
			}
		}
		while (layout.Pages.Count > 0)
		{
			List<LauncherPage> pages = layout.Pages;
			if (pages[pages.Count - 1].Slots.Count != 0)
			{
				break;
			}
			layout.Pages.RemoveAt(layout.Pages.Count - 1);
		}
		layout.SchemaVersion = 2;
		LauncherFilterRules.Normalize(layout);
		return layout;
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.1.0.9782' (yours is '9.1.0.7988')
