using System;
using System.Collections.Generic;

namespace GlassDesk.Dock;

public static class DockLayoutPolicy
{
	public const string EntryTilePrefix = "entry:";

	public static bool IsEntryTile(string? itemId)
	{
		return itemId?.StartsWith("entry:", StringComparison.Ordinal) ?? false;
	}

	public static List<string> Sanitize(IEnumerable<string> pinned, Func<string, bool> isLibraryEntry)
	{
		ArgumentNullException.ThrowIfNull(pinned, "pinned");
		ArgumentNullException.ThrowIfNull(isLibraryEntry, "isLibraryEntry");
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		foreach (string item in pinned)
		{
			if (!string.IsNullOrWhiteSpace(item) && !IsEntryTile(item) && isLibraryEntry(item) && hashSet.Add(item))
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static bool SameOrder(IReadOnlyList<string> left, IReadOnlyList<string> right)
	{
		ArgumentNullException.ThrowIfNull(left, "left");
		ArgumentNullException.ThrowIfNull(right, "right");
		if (left.Count != right.Count)
		{
			return false;
		}
		for (int i = 0; i < left.Count; i++)
		{
			if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
			{
				return false;
			}
		}
		return true;
	}
}
