using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using GlassDesk.Platform.Interop;

namespace GlassDesk.Platform.Shell;

public static class IconExtractor
{
	public const int PreferredSize = 256;

	private static readonly StrategyBasedComWrappers Wrappers = new StrategyBasedComWrappers();

	public static ExtractedIcon? Extract(string? iconSource)
	{
		if (string.IsNullOrWhiteSpace(iconSource))
		{
			return null;
		}
		var (text, num) = SplitIconSource(iconSource);
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		string extension = Path.GetExtension(text);
		if (num.HasValue || extension.Length == 0 || extension.Equals(".ico", StringComparison.OrdinalIgnoreCase))
		{
			ExtractedIcon extractedIcon = ExtractResource(text, num.GetValueOrDefault()) ?? ExtractByIndex(text, num.GetValueOrDefault());
			if ((object)extractedIcon != null && extractedIcon.VisiblePixelCount > 0)
			{
				return extractedIcon;
			}
		}
		return ExtractByShellItem(text, 256) ?? ExtractByIndex(text, 0) ?? ExtractByFileInfo(text);
	}

	private static ExtractedIcon? ExtractResource(string path, int index)
	{
		nint large = 0;
		try
		{
			return (Shell32.SHDefExtractIcon(path, index, 0u, out large, 0, 256u) == 0 && large != 0) ? FromIconHandle(large) : null;
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return null;
		}
		finally
		{
			if (large != 0)
			{
				User32.DestroyIcon(large);
			}
		}
	}

	public static (string Path, int? Index) SplitIconSource(string iconSource)
	{
		string text = iconSource.Trim();
		int num = text.LastIndexOf(',');
		if (num <= 0 || num == text.Length - 1)
		{
			return (Path: text, Index: null);
		}
		if (int.TryParse(text.Substring(num + 1).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			return (Path: text.Substring(0, num).Trim(), Index: result);
		}
		return (Path: text, Index: null);
	}

	private static ExtractedIcon? ExtractByShellItem(string path, int size)
	{
		nint instance = 0;
		IShellItemImageFactory shellItemImageFactory = null;
		try
		{
			if (Shell32.SHCreateItemFromParsingName(path, 0, in ShellComIds.ShellItemImageFactory, out instance) != 0 || instance == 0)
			{
				return null;
			}
			shellItemImageFactory = (IShellItemImageFactory)Wrappers.GetOrCreateObjectForComInstance(instance, CreateObjectFlags.UniqueInstance);
			int flags = 5;
			if (shellItemImageFactory.GetImage(new Size
			{
				Width = size,
				Height = size
			}, flags, out var bitmap) != 0 || bitmap == 0)
			{
				return null;
			}
			try
			{
				return ReadBitmapPixels(bitmap);
			}
			finally
			{
				Gdi32.DeleteObject(bitmap);
			}
		}
		catch (Exception ex) when (!(ex is OutOfMemoryException))
		{
			return null;
		}
		finally
		{
			if (shellItemImageFactory is IDisposable disposable)
			{
				disposable.Dispose();
			}
			if (instance != 0)
			{
				Marshal.Release(instance);
			}
		}
	}

	private static ExtractedIcon? ExtractByIndex(string path, int index)
	{
		nint large = 0;
		nint small = 0;
		try
		{
			return (Shell32.ExtractIconEx(path, index, out large, out small, 1u) == 0 || large == 0) ? null : FromIconHandle(large);
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return null;
		}
		finally
		{
			if (large != 0)
			{
				User32.DestroyIcon(large);
			}
			if (small != 0)
			{
				User32.DestroyIcon(small);
			}
		}
	}

	private static ExtractedIcon? ExtractByFileInfo(string path)
	{
		try
		{
			Shell32.ShellFileInfo info = default;
			uint num = 256u;
			if (!File.Exists(path) && !Directory.Exists(path))
			{
				num |= 0x10;
			}
			if (Shell32.SHGetFileInfo(path, 0u, ref info, (uint)Marshal.SizeOf<Shell32.ShellFileInfo>(), num) == 0 || info.Icon == 0)
			{
				return null;
			}
			try
			{
				return FromIconHandle(info.Icon);
			}
			finally
			{
				User32.DestroyIcon(info.Icon);
			}
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return null;
		}
	}

	private static ExtractedIcon? FromIconHandle(nint icon)
	{
		if (icon == 0 || !User32.GetIconInfo(icon, out var iconInfo))
		{
			return null;
		}
		try
		{
			if (iconInfo.ColorBitmap == 0)
			{
				return null;
			}
			return ReadBitmapPixels(iconInfo.ColorBitmap);
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return null;
		}
		finally
		{
			if (iconInfo.ColorBitmap != 0)
			{
				Gdi32.DeleteObject(iconInfo.ColorBitmap);
			}
			if (iconInfo.MaskBitmap != 0)
			{
				Gdi32.DeleteObject(iconInfo.MaskBitmap);
			}
		}
	}

	private unsafe static ExtractedIcon? ReadBitmapPixels(nint bitmap)
	{
		if (bitmap == 0 || Gdi32.GetObject(bitmap, Marshal.SizeOf<Bitmap>(), out var target) == 0)
		{
			return null;
		}
		if (target.Width <= 0 || target.Height <= 0 || target.Width > 4096 || target.Height > 4096)
		{
			return null;
		}
		int width = target.Width;
		int height = target.Height;
		byte[] array = new byte[width * height * 4];
		BitmapInfoHeader header = new BitmapInfoHeader
		{
			Size = Marshal.SizeOf<BitmapInfoHeader>(),
			Width = width,
			Height = -height,
			Planes = 1,
			BitCount = 32,
			Compression = 0
		};
		BitmapInfo bitmapInfo = new BitmapInfo
		{
			Header = header
		};
		nint dC = Gdi32.GetDC(0);
		if (dC == 0)
		{
			return null;
		}
		try
		{
			fixed (byte* bits = array)
			{
				if (Gdi32.GetDIBits(dC, bitmap, 0u, (uint)height, bits, &bitmapInfo) == 0)
				{
					return null;
				}
			}
		}
		finally
		{
			Gdi32.ReleaseDC(0, dC);
		}
		return new ExtractedIcon(width, height, array);
	}
}
