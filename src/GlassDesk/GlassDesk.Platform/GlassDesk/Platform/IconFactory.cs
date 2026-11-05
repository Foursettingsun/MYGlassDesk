using System;
using System.Runtime.InteropServices;
using GlassDesk.Platform.Interop;

namespace GlassDesk.Platform;

public static class IconFactory
{
	public static nint CreateIconFromBgra(byte[] bgra, int width, int height)
	{
		ArgumentNullException.ThrowIfNull(bgra, "bgra");
		ArgumentOutOfRangeException.ThrowIfLessThan(width, 1, "width");
		ArgumentOutOfRangeException.ThrowIfLessThan(height, 1, "height");
		int num = width * height * 4;
		if (bgra.Length < num)
		{
			throw new ArgumentException($"像素数据长度不足：需要 {num} 字节，实际 {bgra.Length} 字节。", "bgra");
		}
		nint num2 = 0;
		nint num3 = 0;
		nint dC = Gdi32.GetDC(0);
		try
		{
			BitmapV5Header header = new BitmapV5Header
			{
				Size = Marshal.SizeOf<BitmapV5Header>(),
				Width = width,
				Height = -height,
				Planes = 1,
				BitCount = 32,
				Compression = 3,
				SizeImage = num,
				RedMask = 16711680u,
				GreenMask = 65280u,
				BlueMask = 255u,
				AlphaMask = 4278190080u
			};
			num2 = Gdi32.CreateDibSection(dC, ref header, 0, out var bits, 0, 0);
			if (num2 == 0 || bits == 0)
			{
				return 0;
			}
			Marshal.Copy(bgra, 0, bits, num);
			int num4 = (width + 31) / 32 * 4;
			num3 = Gdi32.CreateBitmap(width, height, 1u, 1u, new byte[num4 * height]);
			if (num3 == 0)
			{
				return 0;
			}
			IconInfo iconInfo = new IconInfo
			{
				IsIcon = 1,
				MaskBitmap = num3,
				ColorBitmap = num2
			};
			return Gdi32.CreateIconIndirect(ref iconInfo);
		}
		finally
		{
			if (dC != 0)
			{
				Gdi32.ReleaseDC(0, dC);
			}
			if (num2 != 0)
			{
				Gdi32.DeleteObject(num2);
			}
			if (num3 != 0)
			{
				Gdi32.DeleteObject(num3);
			}
		}
	}

	public static void DestroyIcon(nint icon)
	{
		if (icon != 0)
		{
			User32.DestroyIcon(icon);
		}
	}
}
