using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using GlassDesk.Core.Models;
using GlassDesk.Core.Storage;

namespace GlassDesk.Core.Repair;

public static class LauncherPagingRepair
{
	private sealed class _003C_003Ec__DisplayClass4_0
	{
		public HashSet<string> allowed;

		internal bool _003CMembers_003Eb__0(string id)
		{
			return !allowed.Contains(id);
		}
	}

	private static List<int> Sizes(object owner)
	{
		return (List<int>)owner.GetType().GetProperty("FlatPageSizes").GetValue(owner);
	}

	private static void SetSizes(object owner, List<int> sizes)
	{
		owner.GetType().GetProperty("FlatPageSizes").SetValue(owner, (sizes == null) ? null : new List<int>(sizes));
	}

	private static List<string> Ordered(LauncherLayout layout)
	{
		IReadOnlyList<string> readOnlyList = LauncherLayoutRules.AppIds(layout);
		HashSet<string> hashSet = new HashSet<string>(readOnlyList, StringComparer.Ordinal);
		HashSet<string> hashSet2 = new HashSet<string>(StringComparer.Ordinal);
		List<string> list = new List<string>();
		foreach (string item in layout.FlatOrder ?? new List<string>())
		{
			if (hashSet.Contains(item) && hashSet2.Add(item))
			{
				list.Add(item);
			}
		}
		foreach (string item2 in readOnlyList)
		{
			if (hashSet2.Add(item2))
			{
				list.Add(item2);
			}
		}
		return list;
	}

	private static LauncherGroup FindGroup(LauncherLayout layout, string id)
	{
		if (id == null)
		{
			return null;
		}
		foreach (LauncherGroup item in layout.FilterGroups ?? new List<LauncherGroup>())
		{
			if (item.Id == id)
			{
				return item;
			}
		}
		return null;
	}

	private static List<string> Members(LauncherLayout layout, string groupId)
	{
		_003C_003Ec__DisplayClass4_0 CS_0024_003C_003E8__locals2 = new _003C_003Ec__DisplayClass4_0();
		List<string> list = Ordered(layout);
		if (groupId == null)
		{
			return list;
		}
		CS_0024_003C_003E8__locals2.allowed = new HashSet<string>(FindGroup(layout, groupId)?.ItemIds ?? new List<string>(), StringComparer.Ordinal);
		list.RemoveAll((string id) => !CS_0024_003C_003E8__locals2.allowed.Contains(id));
		return list;
	}

	public static LauncherLayout Project(LauncherLayout source, string groupId, int capacity)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1, "capacity");
		List<string> list = Members(source, groupId);
		object obj = ((groupId == null) ? ((object)source) : ((object)FindGroup(source, groupId)));
		List<int> list2 = ((obj == null) ? null : Sizes(obj));
		LauncherLayout launcherLayout = new LauncherLayout
		{
			SchemaVersion = 2
		};
		int offset = 0;
		if (list2 != null)
		{
			foreach (int item in list2)
			{
				if (offset >= list.Count && item != 0)
				{
					break;
				}
				if (item >= 0)
				{
					int num = Math.Min(item, list.Count - offset);
					if (num == 0)
					{
						AddPage(launcherLayout, list, ref offset, 0);
					}
					while (num > 0)
					{
						int num2 = Math.Min(capacity, num);
						AddPage(launcherLayout, list, ref offset, num2);
						num -= num2;
					}
				}
			}
		}
		while (offset < list.Count)
		{
			AddPage(launcherLayout, list, ref offset, Math.Min(capacity, list.Count - offset));
		}
		while (launcherLayout.Pages.Count > 0 && launcherLayout.Pages[launcherLayout.Pages.Count - 1].Slots.Count == 0)
		{
			launcherLayout.Pages.RemoveAt(launcherLayout.Pages.Count - 1);
		}
		return launcherLayout;
	}

	private static void AddPage(LauncherLayout layout, List<string> ids, ref int offset, int count)
	{
		LauncherPage launcherPage = new LauncherPage
		{
			Id = "flat-" + layout.Pages.Count
		};
		for (int i = 0; i < count; i++)
		{
			launcherPage.Slots.Add(new LauncherSlot
			{
				ItemId = ids[offset++]
			});
		}
		layout.Pages.Add(launcherPage);
	}

	public static LauncherLayout PlaceAtSlot(LauncherLayout projection, string id, int finalIndex, int capacity, bool exchange)
	{
		return LauncherLayoutEditor.PlaceAtSlot(projection, id, finalIndex, capacity, exchange);
	}

	public static LauncherLayout ApplyProjection(LauncherLayout source, LauncherLayout projection, string groupId, int capacity)
	{
		List<string> list = Members(source, groupId);
		HashSet<string> hashSet = new HashSet<string>(list, StringComparer.Ordinal);
		List<string> list2 = new List<string>();
		HashSet<string> hashSet2 = new HashSet<string>(StringComparer.Ordinal);
		List<int> list3 = new List<int>();
		foreach (LauncherPage page in projection.Pages)
		{
			if (page.Slots.Count > capacity)
			{
				throw new InvalidOperationException("页面图标数量超出容量，请重试。");
			}
			list3.Add(page.Slots.Count);
			foreach (LauncherSlot slot in page.Slots)
			{
				if (slot.Kind != LauncherSlotKind.App || slot.ItemId == null || !hashSet.Contains(slot.ItemId) || !hashSet2.Add(slot.ItemId))
				{
					throw new InvalidOperationException("平铺整理包含无效的图标，请重试。");
				}
				list2.Add(slot.ItemId);
			}
		}
		LauncherLayout launcherLayout = LauncherLayoutRules.DeepClone(source);
		foreach (string item in list)
		{
			if (!hashSet2.Contains(item))
			{
				launcherLayout = LauncherLayoutEditor.RemoveShortcut(launcherLayout, item, capacity);
			}
		}
		List<string> list4 = Ordered(launcherLayout);
		Queue<string> queue = new Queue<string>(list2);
		launcherLayout.FlatOrder = new List<string>();
		foreach (string item2 in list4)
		{
			launcherLayout.FlatOrder.Add(hashSet.Contains(item2) ? queue.Dequeue() : item2);
		}
		LauncherFilterRules.Normalize(launcherLayout);
		foreach (LauncherGroup filterGroup in launcherLayout.FilterGroups)
		{
			if (!(filterGroup.Id == groupId))
			{
				LauncherGroup launcherGroup = FindGroup(source, filterGroup.Id);
				if (launcherGroup != null)
				{
					SetSizes(filterGroup, AdjustSizes(Sizes(launcherGroup), Members(source, filterGroup.Id), Members(launcherLayout, filterGroup.Id)));
				}
			}
		}
		if (groupId == null)
		{
			SetSizes(launcherLayout, list3);
		}
		else
		{
			SetSizes(FindGroup(launcherLayout, groupId) ?? throw new InvalidOperationException("分组已不存在，请重试。"), list3);
			SetSizes(launcherLayout, AdjustSizes(Sizes(source), Ordered(source), Ordered(launcherLayout)));
		}
		return launcherLayout;
	}

	public static LauncherLayout CopyPaging(LauncherLayout clone, LauncherLayout source)
	{
		SetSizes(clone, Sizes(source));
		foreach (LauncherGroup filterGroup in clone.FilterGroups)
		{
			LauncherGroup launcherGroup = FindGroup(source, filterGroup.Id);
			if (launcherGroup != null)
			{
				SetSizes(filterGroup, Sizes(launcherGroup));
			}
		}
		return clone;
	}

	public static LauncherLayout CopyAfterReconcile(LauncherLayout clone, LauncherLayout source, int capacity)
	{
		CopyPaging(clone, source);
		SetSizes(clone, AdjustSizes(Sizes(source), Ordered(source), Ordered(clone)));
		foreach (LauncherGroup filterGroup in clone.FilterGroups)
		{
			LauncherGroup launcherGroup = FindGroup(source, filterGroup.Id);
			if (launcherGroup != null)
			{
				SetSizes(filterGroup, AdjustSizes(Sizes(launcherGroup), Members(source, filterGroup.Id), Members(clone, filterGroup.Id)));
			}
		}
		return clone;
	}

	private static List<int> AdjustSizes(List<int> sizes, List<string> before, List<string> after)
	{
		if (sizes == null)
		{
			return null;
		}
		HashSet<string> hashSet = new HashSet<string>(after, StringComparer.Ordinal);
		List<int> list = new List<int>();
		int num = 0;
		foreach (int size in sizes)
		{
			int num2 = 0;
			int num3 = Math.Min(before.Count, num + Math.Max(0, Math.Min(size, before.Count)));
			while (num < num3)
			{
				if (hashSet.Contains(before[num++]))
				{
					num2++;
				}
			}
			list.Add(num2);
		}
		return list;
	}

	public static void NormalizePaging(LauncherLayout layout)
	{
		List<string> list = new List<string>(layout.FlatOrder ?? new List<string>());
		if (list.Count == 0)
		{
			return;
		}
		List<string> list2 = Ordered(layout);
		SetSizes(layout, AdjustSizes(Sizes(layout), list, list2));
		foreach (LauncherGroup item in layout.FilterGroups ?? new List<LauncherGroup>())
		{
			HashSet<string> hashSet = new HashSet<string>(item.ItemIds ?? new List<string>(), StringComparer.Ordinal);
			List<string> list3 = new List<string>();
			List<string> list4 = new List<string>();
			foreach (string item2 in list)
			{
				if (hashSet.Contains(item2))
				{
					list3.Add(item2);
				}
			}
			foreach (string item3 in list2)
			{
				if (hashSet.Contains(item3))
				{
					list4.Add(item3);
				}
			}
			SetSizes(item, AdjustSizes(Sizes(item), list3, list4));
		}
	}

	public static bool PagingEquivalent(LauncherLayout left, LauncherLayout right)
	{
		if (!EqualSizes(Sizes(left), Sizes(right)))
		{
			return false;
		}
		foreach (LauncherGroup item in left.FilterGroups ?? new List<LauncherGroup>())
		{
			LauncherGroup launcherGroup = FindGroup(right, item.Id);
			if (launcherGroup != null && !EqualSizes(Sizes(item), Sizes(launcherGroup)))
			{
				return false;
			}
		}
		return true;
	}

	private static bool EqualSizes(List<int> left, List<int> right)
	{
		if (left == null || right == null)
		{
			return left == right;
		}
		if (left.Count != right.Count)
		{
			return false;
		}
		for (int i = 0; i < left.Count; i++)
		{
			if (left[i] != right[i])
			{
				return false;
			}
		}
		return true;
	}

	public static void CopyStorePaging(LayoutStore store, LauncherLayout source)
	{
		SetSizes(store.Launcher, Sizes(source));
	}

	public static JsonTypeInfo ResolveLayoutInfo(Type type, JsonSerializerOptions options)
	{
		return new DefaultJsonTypeInfoResolver().GetTypeInfo(type, options);
	}

	public static JsonTypeInfo ResolveExtraInfo(Type type, JsonSerializerOptions options)
	{
		if (!(type == typeof(List<int>)))
		{
			return null;
		}
		return ResolveLayoutInfo(type, options);
	}
}
