using System;
using System.Collections.Generic;
using System.Linq;

internal static class PixelComparison
{
	internal static PixelResult Compare(PixelFrame before, PixelFrame source, PixelFrame after, IEnumerable<PixelCandidate> points)
	{
		if (before.Bounds != source.Bounds || before.Bounds != after.Bounds)
		{
			throw new InvalidOperationException("Physical capture bounds changed during the triplet.");
		}
		int num = checked(before.Bounds.Width * before.Bounds.Height * 4);
		if (before.Pixels.Length != num || source.Pixels.Length != num || after.Pixels.Length != num)
		{
			throw new InvalidOperationException("Incomplete physical frame.");
		}
		PixelStats pixelStats = new PixelStats();
		PixelStats pixelStats2 = new PixelStats();
		List<(int, int, bool)> list = new List<(int, int, bool)>();
		foreach (PixelCandidate item in points.DistinctBy((PixelCandidate p) => (X: p.X, Y: p.Y)))
		{
			PixelStats pixelStats3 = (item.Icon ? pixelStats2 : pixelStats);
			pixelStats3.Candidates++;
			if (!item.Clear || !before.Bounds.Contains(item.X, item.Y))
			{
				pixelStats3.Occluded++;
				continue;
			}
			int offset = ((item.Y - before.Bounds.Top) * before.Bounds.Width + item.X - before.Bounds.Left) * 4;
			if (Enumerable.Range(0, 3).Any((int c) => before.Pixels[offset + c] != after.Pixels[offset + c]))
			{
				pixelStats3.Unstable++;
				continue;
			}
			pixelStats3.Valid++;
			bool flag = false;
			for (int num2 = 0; num2 < 3; num2++)
			{
				int num3 = Math.Max(Math.Abs(source.Pixels[offset + num2] - before.Pixels[offset + num2]), Math.Abs(source.Pixels[offset + num2] - after.Pixels[offset + num2]));
				pixelStats3.MaxBgrError[num2] = Math.Max(pixelStats3.MaxBgrError[num2], num3);
				flag |= num3 > 2;
			}
			if (flag)
			{
				pixelStats3.Mismatched++;
				list.Add((item.X, item.Y, item.Icon));
			}
		}
		return new PixelResult(pixelStats, pixelStats2, list);
	}
}
