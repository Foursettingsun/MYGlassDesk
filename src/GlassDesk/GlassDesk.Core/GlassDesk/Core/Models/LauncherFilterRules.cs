using System;
using System.Collections.Generic;
using System.Linq;
using GlassDesk.Core.Repair;

namespace GlassDesk.Core.Models;

public static class LauncherFilterRules
{
	private static List<string> OrderedIds(LauncherLayout layout)
	{
		List<string> list = LauncherLayoutRules.AppIds(layout).Distinct(StringComparer.Ordinal).ToList();
		HashSet<string> hashSet = list.ToHashSet(StringComparer.Ordinal);
		return (layout.FlatOrder ?? new List<string>()).Where(hashSet.Contains).Concat(list).Distinct(StringComparer.Ordinal)
			.ToList();
	}

	public static LauncherLayout Project(LauncherLayout source, string? groupId, int capacity)
	{
		return LauncherPagingRepair.Project(source, groupId, capacity);
	}

	public static LauncherLayout PlaceAtSlot(LauncherLayout projection, string id, int finalIndex, int capacity, bool exchange)
	{
		return LauncherPagingRepair.PlaceAtSlot(projection, id, finalIndex, capacity, exchange);
	}

	private static LauncherLayout Pack(IEnumerable<string> ids, int capacity)
	{
		return new LauncherLayout
		{
			SchemaVersion = 2,
			Pages = ids.Chunk(capacity).Select((string[] chunk, int index) => new LauncherPage
			{
				Id = $"flat-{index}",
				Slots = chunk.Select((string id) => new LauncherSlot
				{
					ItemId = id
				}).ToList()
			}).ToList()
		};
	}

	public static LauncherLayout ApplyProjection(LauncherLayout source, LauncherLayout projection, string? groupId, int capacity)
	{
		return LauncherPagingRepair.ApplyProjection(source, projection, groupId, capacity);
	}

	public static LauncherLayout AddGroup(LauncherLayout source, string id, string name, IEnumerable<string> itemIds)
	{
		string name2 = ValidName(name);
		if (string.IsNullOrWhiteSpace(id) || (source.FilterGroups ?? new List<LauncherGroup>()).Any((LauncherGroup group) => group.Id == id))
		{
			throw new ArgumentException("分组标识已存在，请重试。", "id");
		}
		HashSet<string> hashSet = LauncherLayoutRules.AppIds(source).ToHashSet(StringComparer.Ordinal);
		List<string> list = itemIds.Where(hashSet.Contains).Distinct(StringComparer.Ordinal).ToList();
		if (list.Count == 0)
		{
			throw new ArgumentException("请至少选择一个图标。", "itemIds");
		}
		LauncherLayout launcherLayout = LauncherLayoutRules.DeepClone(source);
		launcherLayout.FilterGroups.Add(new LauncherGroup
		{
			Id = id,
			Name = name2,
			ItemIds = list
		});
		return launcherLayout;
	}

	public static LauncherLayout RenameGroup(LauncherLayout source, string id, string name)
	{
		string name2 = ValidName(name);
		LauncherLayout launcherLayout = LauncherLayoutRules.DeepClone(source);
		(launcherLayout.FilterGroups.FirstOrDefault((LauncherGroup group) => group.Id == id) ?? throw new InvalidOperationException("分组已不存在，请重试。")).Name = name2;
		return launcherLayout;
	}

	public static LauncherLayout UpdateGroupMembers(LauncherLayout source, string id, IEnumerable<string> itemIds)
	{
		LauncherLayout launcherLayout = LauncherLayoutRules.DeepClone(source);
		LauncherGroup? launcherGroup = launcherLayout.FilterGroups.FirstOrDefault((LauncherGroup group) => group.Id == id) ?? throw new InvalidOperationException("分组已不存在，请重试。");
		HashSet<string> hashSet = LauncherLayoutRules.AppIds(source).ToHashSet(StringComparer.Ordinal);
		launcherGroup.ItemIds = itemIds.Where(hashSet.Contains).Distinct(StringComparer.Ordinal).ToList();
		return launcherLayout;
	}

	public static LauncherLayout RemoveGroup(LauncherLayout source, string id)
	{
		LauncherLayout launcherLayout = LauncherLayoutRules.DeepClone(source);
		launcherLayout.FilterGroups.RemoveAll((LauncherGroup group) => group.Id == id);
		return launcherLayout;
	}

	private static string ValidName(string name)
	{
		string text = name.Trim();
		int length = text.Length;
		if ((length < 1 || length > 60) ? true : false)
		{
			throw new ArgumentException("分组名称应为 1–60 个字符。", "name");
		}
		return text;
	}

	public static void Normalize(LauncherLayout layout)
	{
		LauncherPagingRepair.NormalizePaging(layout);
		HashSet<string> hashSet = LauncherLayoutRules.AppIds(layout).ToHashSet(StringComparer.Ordinal);
		layout.FlatOrder = (layout.FlatOrder ?? new List<string>()).Where(hashSet.Contains).Distinct(StringComparer.Ordinal).ToList();
		HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
		layout.FilterGroups = (layout.FilterGroups ?? new List<LauncherGroup>()).Where((LauncherGroup group) => !string.IsNullOrWhiteSpace(group.Id) && seen.Add(group.Id)).ToList();
		foreach (LauncherGroup filterGroup in layout.FilterGroups)
		{
			filterGroup.ItemIds = (filterGroup.ItemIds ?? new List<string>()).Where(hashSet.Contains).Distinct(StringComparer.Ordinal).ToList();
		}
	}
}
