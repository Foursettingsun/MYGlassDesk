using System;
using System.Collections.Generic;
using System.Linq;

namespace GlassDesk.Dock;

public static class DockPinMerge
{
	public static List<string> Merge(IReadOnlyList<string> pinned, IEnumerable<string> added, IEnumerable<string> duplicates)
	{
		ArgumentNullException.ThrowIfNull(pinned, "pinned");
		ArgumentNullException.ThrowIfNull(added, "added");
		ArgumentNullException.ThrowIfNull(duplicates, "duplicates");
		List<string> list = new List<string>(pinned);
		HashSet<string> hashSet = new HashSet<string>(list, StringComparer.Ordinal);
		foreach (string item in added.Concat(duplicates))
		{
			if (!string.IsNullOrEmpty(item) && hashSet.Add(item))
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static int CountNewlyPinned(IReadOnlyList<string> pinned, IEnumerable<string> added, IEnumerable<string> duplicates)
	{
		return Merge(pinned, added, duplicates).Count - pinned.Count;
	}
}
