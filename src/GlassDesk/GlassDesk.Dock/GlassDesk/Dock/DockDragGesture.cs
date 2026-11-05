namespace GlassDesk.Dock;

public static class DockDragGesture
{
	public static DragEndAction DecideEnd(bool droppedInStrip, bool endedOutside, bool cancelled)
	{
		if (droppedInStrip)
		{
			return DragEndAction.KeepCommitted;
		}
		if (cancelled)
		{
			return DragEndAction.CancelOnly;
		}
		if (!endedOutside)
		{
			return DragEndAction.CancelOnly;
		}
		return DragEndAction.CancelAndUnpin;
	}
}
