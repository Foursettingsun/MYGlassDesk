using System;

namespace GlassDesk.Core.Imaging;

public static class IconCanvasNormalizer
{
	private const byte MinimumContentAlpha = 16;

	private const long MinimumProjectionAlpha = 510L;

	private const int PaddingDivisor = 14;

	private const int EdgeComponentAreaDivisor = 8;

	private const int EdgeCoverageDivisor = 20;

	public static (byte[] Bgra, int Width, int Height) Normalize(byte[] bgra, int width, int height)
	{
		ArgumentNullException.ThrowIfNull(bgra, "bgra");
		ArgumentOutOfRangeException.ThrowIfLessThan(width, 1, "width");
		ArgumentOutOfRangeException.ThrowIfLessThan(height, 1, "height");
		int num2;
		bool[] array;
		checked
		{
			int num = width * height * 4;
			if (bgra.Length != num)
			{
				throw new ArgumentException($"像素数据长度应为 {num} 字节，实际 {bgra.Length} 字节。", "bgra");
			}
			num2 = width * height;
			array = new bool[num2];
		}
		for (int i = 0; i < num2; i++)
		{
			array[i] = bgra[i * 4 + 3] >= 16;
		}
		bool[] array2 = FindThinFrameComponents(array, width, height);
		long[] array3 = new long[height];
		long[] array4 = new long[width];
		for (int j = 0; j < num2; j++)
		{
			if (array[j] && !array2[j])
			{
				byte b = bgra[j * 4 + 3];
				int num3 = j % width;
				int num4 = j / width;
				array3[num4] += b;
				array4[num3] += b;
			}
		}
		int num5 = Array.FindIndex(array3, (long mass) => mass >= 510);
		int num6 = Array.FindLastIndex(array3, (long mass) => mass >= 510);
		int num7 = Array.FindIndex(array4, (long mass) => mass >= 510);
		int num8 = Array.FindLastIndex(array4, (long mass) => mass >= 510);
		if (num5 < 0 || num6 < num5 || num7 < 0 || num8 < num7)
		{
			return (Bgra: bgra, Width: width, Height: height);
		}
		int num9 = num8 - num7 + 1;
		int num10 = num6 - num5 + 1;
		int num11 = Math.Max(1, (num9 + 14 - 1) / 14);
		int num12 = Math.Max(1, (num10 + 14 - 1) / 14);
		num7 = Math.Max(0, num7 - num11);
		num8 = Math.Min(width - 1, num8 + num11);
		num5 = Math.Max(0, num5 - num12);
		num6 = Math.Min(height - 1, num6 + num12);
		int num13 = num8 - num7 + 1;
		int num14 = num6 - num5 + 1;
		if (num13 == width && num14 == height)
		{
			return (Bgra: bgra, Width: width, Height: height);
		}
		byte[] array5 = new byte[checked(num13 * num14 * 4)];
		int num15 = num13 * 4;
		for (int num16 = 0; num16 < num14; num16++)
		{
			int srcOffset = ((num5 + num16) * width + num7) * 4;
			Buffer.BlockCopy(bgra, srcOffset, array5, num16 * num15, num15);
		}
		return (Bgra: array5, Width: num13, Height: num14);
	}

	private static bool[] FindThinFrameComponents(bool[] visible, int width, int height)
	{
		int pixelCount = visible.Length;
		bool[] visited = new bool[pixelCount];
		bool[] ignored = new bool[pixelCount];
		int[] queue = new int[pixelCount];
		for (int i = 0; i < width; i++)
		{
			InspectEdgeComponent(i);
			if (height > 1)
			{
				InspectEdgeComponent((height - 1) * width + i);
			}
		}
		for (int j = 1; j < height - 1; j++)
		{
			InspectEdgeComponent(j * width);
			if (width > 1)
			{
				InspectEdgeComponent(j * width + width - 1);
			}
		}
		return ignored;
		void InspectEdgeComponent(int start)
		{
			if (visible[start] && !visited[start])
			{
				int num = 0;
				int num2 = 0;
				queue[num2++] = start;
				visited[start] = true;
				bool flag = false;
				bool flag2 = false;
				bool flag3 = false;
				bool flag4 = false;
				int num3 = 0;
				int num4 = 0;
				int num5 = 0;
				int num6 = 0;
				while (num < num2)
				{
					int num7 = queue[num++];
					int num8 = num7 % width;
					int num9 = num7 / width;
					if (num9 == 0)
					{
						flag = true;
						num3++;
					}
					if (num9 == height - 1)
					{
						flag2 = true;
						num4++;
					}
					if (num8 == 0)
					{
						flag3 = true;
						num5++;
					}
					if (num8 == width - 1)
					{
						flag4 = true;
						num6++;
					}
					for (int k = -1; k <= 1; k++)
					{
						for (int l = -1; l <= 1; l++)
						{
							if (l != 0 || k != 0)
							{
								int num10 = num8 + l;
								int num11 = num9 + k;
								if (num10 >= 0 && num10 < width && num11 >= 0 && num11 < height)
								{
									int num12 = num11 * width + num10;
									if (visible[num12] && !visited[num12])
									{
										visited[num12] = true;
										queue[num2++] = num12;
									}
								}
							}
						}
					}
				}
				int num13 = Math.Max(2, width / 20);
				int num14 = Math.Max(2, height / 20);
				if ((flag & flag2 & flag3 & flag4) && num2 <= pixelCount / 8 && num3 >= num13 && num4 >= num13 && num5 >= num14 && num6 >= num14)
				{
					for (int m = 0; m < num2; m++)
					{
						ignored[queue[m]] = true;
					}
				}
			}
		}
	}
}
