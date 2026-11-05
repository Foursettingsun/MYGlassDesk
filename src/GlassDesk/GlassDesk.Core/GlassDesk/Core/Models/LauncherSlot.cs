namespace GlassDesk.Core.Models;

public sealed class LauncherSlot
{
	public LauncherSlotKind Kind { get; set; }

	public string? ItemId { get; set; }

	public LauncherGroup? Group { get; set; }
}
