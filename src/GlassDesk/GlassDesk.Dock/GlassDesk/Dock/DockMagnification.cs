using System;
using System.Collections.Generic;
using System.Linq;

namespace GlassDesk.Dock;

public static class DockMagnification
{
	public static TileGeometry[] Compute(int count, double iconSize, double spacing, double peak, double? pointerPosition)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(count, "count");
		TileGeometry[] array = new TileGeometry[count];
		ComputeInto(array, iconSize, spacing, peak, pointerPosition);
		return array;
	}

	internal static void ComputeInto(Span<TileGeometry> geometry, double iconSize, double spacing, double peak, double? pointerPosition)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(iconSize, 1.0, "iconSize");
		ArgumentOutOfRangeException.ThrowIfLessThan(spacing, 0.0, "spacing");
		ArgumentOutOfRangeException.ThrowIfLessThan(peak, 1.0, "peak");
		for (int i = 0; i < geometry.Length; i++)
		{
			double num = SlotCenter(i, iconSize, spacing, peak);
			double scale = 1.0;
			if (pointerPosition.HasValue)
			{
				double valueOrDefault = pointerPosition.GetValueOrDefault();
				double distance = Math.Abs(valueOrDefault - num);
				scale = 1.0 + (peak - 1.0) * Falloff(distance, iconSize, spacing);
			}
			geometry[i] = new TileGeometry(num, scale);
		}
	}

	internal static int CompareDrawOrder(TileGeometry[] geometry, int left, int right)
	{
		int num = geometry[right].Scale.CompareTo(geometry[left].Scale);
		if (num == 0)
		{
			return left.CompareTo(right);
		}
		return num;
	}

	public static int[] TopToBottomOrder(TileGeometry[] geometry)
	{
		ArgumentNullException.ThrowIfNull(geometry, "geometry");
		int[] array = Enumerable.Range(0, geometry.Length).ToArray();
		Array.Sort(array, Comparer<int>.Create((int left, int right) => CompareDrawOrder(geometry, left, right)));
		return array;
	}

	public static int HitTest(TileGeometry[] geometry, double iconSize, double pointerPosition)
	{
		ArgumentNullException.ThrowIfNull(geometry, "geometry");
		int[] array = TopToBottomOrder(geometry);
		foreach (int num in array)
		{
			var (num4, num5) = geometry[num];
			double num6 = iconSize * num5 / 2.0;
			if (pointerPosition >= num4 - num6 && pointerPosition < num4 + num6)
			{
				return num;
			}
		}
		return -1;
	}

	public static double EndPadding(double iconSize, double peak)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(iconSize, 1.0, "iconSize");
		ArgumentOutOfRangeException.ThrowIfLessThan(peak, 1.0, "peak");
		return (peak - 1.0) * iconSize / 2.0;
	}

	public static double RequiredLength(int count, double iconSize, double spacing, double peak)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(count, "count");
		if (count == 0)
		{
			return 0.0;
		}
		return (double)count * iconSize + (double)(count - 1) * spacing + 2.0 * EndPadding(iconSize, peak);
	}

	public static double RequiredThickness(double iconSize, double peak, double verticalPadding)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(iconSize, 1.0, "iconSize");
		ArgumentOutOfRangeException.ThrowIfLessThan(peak, 1.0, "peak");
		ArgumentOutOfRangeException.ThrowIfLessThan(verticalPadding, 0.0, "verticalPadding");
		return iconSize * peak + verticalPadding * 2.0;
	}

	public static double SlotCenter(int index, double iconSize, double spacing, double peak)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(index, "index");
		return EndPadding(iconSize, peak) + (double)index * (iconSize + spacing) + iconSize / 2.0;
	}

	private static double Falloff(double distance, double iconSize, double spacing)
	{
		double num = iconSize + spacing;
		if (distance >= num)
		{
			return 0.0;
		}
		double num2 = distance / num;
		return 0.5 * (1.0 + Math.Cos(num2 * Math.PI));
	}
}
