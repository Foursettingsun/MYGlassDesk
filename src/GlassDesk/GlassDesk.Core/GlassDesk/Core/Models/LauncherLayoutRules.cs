using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using GlassDesk.Core.Repair;

namespace GlassDesk.Core.Models;

public static class LauncherLayoutRules
{
	public static bool Equivalent(LauncherLayout left, LauncherLayout right)
	{
		if (!LauncherPagingRepair.PagingEquivalent(left, right))
		{
			return false;
		}
		if (left.SchemaVersion != right.SchemaVersion)
		{
			return false;
		}
		if (!(left.FlatOrder ?? new List<string>()).SequenceEqual(right.FlatOrder ?? new List<string>(), StringComparer.Ordinal))
		{
			return false;
		}
		List<LauncherGroup> list = left.FilterGroups ?? new List<LauncherGroup>();
		List<LauncherGroup> list2 = right.FilterGroups ?? new List<LauncherGroup>();
		if (list.Count != list2.Count)
		{
			return false;
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].Id != list2[i].Id || list[i].Name != list2[i].Name || !(list[i].ItemIds ?? new List<string>()).SequenceEqual(list2[i].ItemIds ?? new List<string>(), StringComparer.Ordinal))
			{
				return false;
			}
		}
		if (left.SchemaVersion < 2 && !(left.ExcludedAppIds ?? new List<string>()).SequenceEqual(right.ExcludedAppIds ?? new List<string>(), StringComparer.Ordinal))
		{
			return false;
		}
		List<LauncherPage> list3 = left.Pages ?? new List<LauncherPage>();
		List<LauncherPage> list4 = right.Pages ?? new List<LauncherPage>();
		if (list3.Count != list4.Count)
		{
			return false;
		}
		for (int j = 0; j < list3.Count; j++)
		{
			if (list3[j].Slots.Count != list4[j].Slots.Count)
			{
				return false;
			}
		}
		List<LauncherSlot> list5 = (left.Pages ?? new List<LauncherPage>()).SelectMany((LauncherPage page) => page.Slots ?? new List<LauncherSlot>()).ToList();
		List<LauncherSlot> list6 = (right.Pages ?? new List<LauncherPage>()).SelectMany((LauncherPage page) => page.Slots ?? new List<LauncherSlot>()).ToList();
		if (list5.Count != list6.Count)
		{
			return false;
		}
		for (int num = 0; num < list5.Count; num++)
		{
			if (list5[num].Kind != list6[num].Kind || list5[num].ItemId != list6[num].ItemId)
			{
				return false;
			}
			LauncherGroup launcherGroup = list5[num].Group;
			LauncherGroup launcherGroup2 = list6[num].Group;
			if (launcherGroup == null != (launcherGroup2 == null))
			{
				return false;
			}
			if (launcherGroup != null && (launcherGroup.Id != launcherGroup2.Id || launcherGroup.Name != launcherGroup2.Name || !launcherGroup.ItemIds.SequenceEqual(launcherGroup2.ItemIds)))
			{
				return false;
			}
		}
		return true;
	}

	public static LauncherLayout DeepClone(LauncherLayout source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		return LauncherPagingRepair.CopyPaging(new LauncherLayout
		{
			SchemaVersion = source.SchemaVersion,
			ExcludedAppIds = (source.ExcludedAppIds ?? new List<string>()).ToList(),
			FlatOrder = (source.FlatOrder ?? new List<string>()).ToList(),
			FilterGroups = (source.FilterGroups ?? new List<LauncherGroup>()).Select((LauncherGroup group) => new LauncherGroup
			{
				Id = group.Id,
				Name = group.Name,
				ItemIds = (group.ItemIds ?? new List<string>()).ToList()
			}).ToList(),
			Pages = (source.Pages ?? new List<LauncherPage>()).Select((LauncherPage page) => new LauncherPage
			{
				Id = page.Id,
				Slots = (page.Slots ?? new List<LauncherSlot>()).Select((LauncherSlot slot) => new LauncherSlot
				{
					Kind = slot.Kind,
					ItemId = slot.ItemId,
					Group = ((slot.Group == null) ? null : new LauncherGroup
					{
						Id = slot.Group.Id,
						Name = slot.Group.Name,
						ItemIds = (slot.Group.ItemIds ?? new List<string>()).ToList()
					})
				}).ToList()
			}).ToList()
		}, source);
	}

	public static IReadOnlyList<string> AppIds(LauncherLayout layout)
	{
		return (layout.Pages ?? new List<LauncherPage>()).SelectMany((LauncherPage page) => page.Slots ?? new List<LauncherSlot>()).SelectMany((LauncherSlot slot) =>
		{
			if (slot.Kind == LauncherSlotKind.Group)
			{
				return slot.Group?.ItemIds ?? new List<string>();
			}
			if (!string.IsNullOrWhiteSpace(slot.ItemId))
			{
				int num = 1;
				List<string> list = new List<string>(num);
				CollectionsMarshal.SetCount(list, num);
				CollectionsMarshal.AsSpan(list)[0] = slot.ItemId;
				return list;
			}
			return new List<string>();
		}).ToList();
	}

	public static LauncherLayout Reconcile(LauncherLayout source, IEnumerable<string> libraryIds, int capacity)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		ArgumentNullException.ThrowIfNull(libraryIds, "libraryIds");
		ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1, "capacity");
		List<string> source2 = libraryIds.Where((string id) => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal).ToList();
		HashSet<string> valid = source2.ToHashSet(StringComparer.Ordinal);
		List<string> source3 = ((source.SchemaVersion < 2) ? (source.ExcludedAppIds ?? new List<string>()) : new List<string>()).Where((string id) => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal).ToList();
		HashSet<string> hidden = source3.ToHashSet(StringComparer.Ordinal);
		HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
		List<LauncherPage> list = new List<LauncherPage>();
		foreach (LauncherPage item in source.Pages ?? new List<LauncherPage>())
		{
			List<LauncherSlot> list2 = new List<LauncherSlot>();
			foreach (LauncherSlot item2 in item.Slots ?? new List<LauncherSlot>())
			{
				if (item2.Kind == LauncherSlotKind.App)
				{
					string itemId = item2.ItemId;
					if (itemId != null && valid.Contains(itemId) && !hidden.Contains(itemId) && seen.Add(itemId))
					{
						list2.Add(new LauncherSlot
						{
							Kind = LauncherSlotKind.App,
							ItemId = itemId
						});
					}
				}
				else
				{
					if (item2.Kind != LauncherSlotKind.Group)
					{
						continue;
					}
					LauncherGroup launcherGroup = item2.Group;
					if (launcherGroup != null)
					{
						List<string> list3 = (launcherGroup.ItemIds ?? new List<string>()).Where((string id) => valid.Contains(id) && !hidden.Contains(id) && seen.Add(id)).ToList();
						if (list3.Count == 1)
						{
							list2.Add(new LauncherSlot
							{
								ItemId = list3[0]
							});
						}
						else if (list3.Count > 1)
						{
							list2.Add(new LauncherSlot
							{
								Kind = LauncherSlotKind.Group,
								Group = new LauncherGroup
								{
									Id = launcherGroup.Id,
									Name = launcherGroup.Name,
									ItemIds = list3
								}
							});
						}
					}
				}
			}
			list.Add(new LauncherPage
			{
				Id = (string.IsNullOrWhiteSpace(item.Id) ? $"page-{list.Count + 1}" : item.Id),
				Slots = list2
			});
		}
		LauncherLayout launcherLayout = LauncherLayoutEditor.Finish(new LauncherLayout
		{
			Pages = list,
			SchemaVersion = 2
		}, capacity);
		launcherLayout = ((source.SchemaVersion < 2) ? LauncherLayoutEditor.AddShortcuts(launcherLayout, source2.Where((string id) => !hidden.Contains(id) && !seen.Contains(id)), capacity) : launcherLayout);
		launcherLayout.FlatOrder = (source.FlatOrder ?? new List<string>()).ToList();
		launcherLayout.FilterGroups = DeepClone(source).FilterGroups;
		LauncherFilterRules.Normalize(launcherLayout);
		return LauncherPagingRepair.CopyAfterReconcile(launcherLayout, source, capacity);
	}
}
