using System.Collections.Generic;

namespace GlassDesk.Core.Models;

public sealed class DockLayout
{
	public List<string> PinnedItemIds { get; set; } = new List<string>();
}
