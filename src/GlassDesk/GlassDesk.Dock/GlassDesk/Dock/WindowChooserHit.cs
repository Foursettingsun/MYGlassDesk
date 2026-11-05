using System;
using System.Windows;
using System.Windows.Controls;

namespace GlassDesk.Dock;

public static class WindowChooserHit
{
	public static nint Resolve(ItemsControl items, object? originalSource)
	{
		ArgumentNullException.ThrowIfNull(items, "items");
		if (!(originalSource is DependencyObject dependencyObject))
		{
			return 0;
		}
		if (dependencyObject is ListBoxItem item && TryRead(item, out var handle))
		{
			return handle;
		}
		if (ItemsControl.ContainerFromElement(items, dependencyObject) is ListBoxItem item2 && TryRead(item2, out var handle2))
		{
			return handle2;
		}
		return 0;
	}

	private static bool TryRead(ListBoxItem item, out nint handle)
	{
		if (item.Tag is nint num && num != 0)
		{
			handle = num;
			return true;
		}
		handle = 0;
		return false;
	}
}
