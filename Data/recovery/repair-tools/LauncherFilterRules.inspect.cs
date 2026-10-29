using System;
using System.Collections.Generic;
using System.Linq;

namespace GlassDesk.Core.Models;

public static class LauncherFilterRules
{
	private static List<string> OrderedIds(LauncherLayout layout)
	{
		List<string> list = LauncherLayoutRules.AppIds(layout).Distinct<string>(StringComparer.Ordinal).ToList();
		HashSet<string> hashSet = list.ToHashSet<string>(StringComparer.Ordinal);
		return (layout.FlatOrder ?? new List<string>()).Where(hashSet.Contains).Concat(list).Distinct<string>(StringComparer.Ordinal)
			.ToList();
	}

	public static LauncherLayout Project(LauncherLayout source, string? groupId, int capacity)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1, "capacity");
		IEnumerable<string> enumerable = OrderedIds(source);
		if (groupId != null)
		{
			HashSet<string> hashSet = (source.FilterGroups?.FirstOrDefault((LauncherGroup group) => group.Id == groupId)?.ItemIds ?? new List<string>()).ToHashSet<string>(StringComparer.Ordinal);
			enumerable = enumerable.Where(hashSet.Contains);
		}
		return Pack(enumerable, capacity);
	}

	public static LauncherLayout PlaceAtSlot(LauncherLayout projection, string id, int finalIndex, int capacity, bool exchange)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1, "capacity");
		List<string> list = LauncherLayoutRules.AppIds(projection).ToList();
		int num = list.IndexOf(id);
		if (num < 0)
		{
			throw new InvalidOperationException("拖动的图标已不存在。");
		}
		int num2 = Math.Clamp(finalIndex, 0, list.Count - 1);
		if (exchange)
		{
			List<string> list2 = list;
			int index = num;
			int index2 = num2;
			string value = list[num2];
			string value2 = list[num];
			list2[index] = value;
			list[index2] = value2;
		}
		else
		{
			list.RemoveAt(num);
			list.Insert(num2, id);
		}
		return Pack(list, capacity);
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
		HashSet<string> before = LauncherLayoutRules.AppIds(Project(source, groupId, capacity)).ToHashSet<string>(StringComparer.Ordinal);
		List<string> list = LauncherLayoutRules.AppIds(projection).ToList();
		if (list.Count != list.Distinct<string>(StringComparer.Ordinal).Count() || list.Any((string id) => !before.Contains(id)) || projection.Pages.SelectMany((LauncherPage page) => page.Slots).Any((LauncherSlot slot) => slot.Kind != LauncherSlotKind.App))
		{
			throw new InvalidOperationException("平铺整理包含无效的图标，请重试。");
		}
		LauncherLayout launcherLayout = LauncherLayoutRules.DeepClone(source);
		HashSet<string> remaining = list.ToHashSet<string>(StringComparer.Ordinal);
		foreach (string item in before.Where((string id) => !remaining.Contains(id)))
		{
			launcherLayout = LauncherLayoutEditor.RemoveShortcut(launcherLayout, item, capacity);
		}
		List<string> source2 = OrderedIds(launcherLayout);
		Queue<string> replacements = new Queue<string>(list);
		launcherLayout.FlatOrder = source2.Select((string id) => (!before.Contains(id)) ? id : replacements.Dequeue()).ToList();
		Normalize(launcherLayout);
		return launcherLayout;
	}

	public static LauncherLayout AddGroup(LauncherLayout source, string id, string name, IEnumerable<string> itemIds)
	{
		string name2 = ValidName(name);
		if (string.IsNullOrWhiteSpace(id) || (source.FilterGroups ?? new List<LauncherGroup>()).Any((LauncherGroup group) => group.Id == id))
		{
			throw new ArgumentException("分组标识已存在，请重试。", "id");
		}
		HashSet<string> hashSet = LauncherLayoutRules.AppIds(source).ToHashSet<string>(StringComparer.Ordinal);
		List<string> list = itemIds.Where(hashSet.Contains).Distinct<string>(StringComparer.Ordinal).ToList();
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
		LauncherGroup? obj = launcherLayout.FilterGroups.FirstOrDefault((LauncherGroup group) => group.Id == id) ?? throw new InvalidOperationException("分组已不存在，请重试。");
		HashSet<string> hashSet = LauncherLayoutRules.AppIds(source).ToHashSet<string>(StringComparer.Ordinal);
		obj.ItemIds = itemIds.Where(hashSet.Contains).Distinct<string>(StringComparer.Ordinal).ToList();
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
		HashSet<string> hashSet = LauncherLayoutRules.AppIds(layout).ToHashSet<string>(StringComparer.Ordinal);
		layout.FlatOrder = (layout.FlatOrder ?? new List<string>()).Where(hashSet.Contains).Distinct<string>(StringComparer.Ordinal).ToList();
		HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
		layout.FilterGroups = (layout.FilterGroups ?? new List<LauncherGroup>()).Where((LauncherGroup group) => !string.IsNullOrWhiteSpace(group.Id) && seen.Add(group.Id)).ToList();
		foreach (LauncherGroup filterGroup in layout.FilterGroups)
		{
			filterGroup.ItemIds = (filterGroup.ItemIds ?? new List<string>()).Where(hashSet.Contains).Distinct<string>(StringComparer.Ordinal).ToList();
		}
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.1.0.9782' (yours is '9.1.0.7988')
