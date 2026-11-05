using System.Collections.Generic;

namespace GlassDesk.Core.Models;

public sealed class LauncherPage
{
	public string Id { get; set; } = string.Empty;

	public List<LauncherSlot> Slots { get; set; } = new List<LauncherSlot>();
}
