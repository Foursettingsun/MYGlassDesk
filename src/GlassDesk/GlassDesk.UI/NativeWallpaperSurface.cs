using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

internal sealed class NativeWallpaperSurface : IDisposable
{
	private struct BitmapInfoHeader
	{
		public uint Size;

		public int Width;

		public int Height;

		public ushort Planes;

		public ushort BitCount;

		public uint Compression;

		public uint SizeImage;

		public int XPelsPerMeter;

		public int YPelsPerMeter;

		public uint ColorsUsed;

		public uint ColorsImportant;
	}

	private struct BitmapInfo
	{
		public BitmapInfoHeader Header;

		public uint Colors;
	}

	private readonly nint _window;

	private readonly int _width;

	private readonly int _height;

	private readonly int _stride;

	private readonly byte[] _pixels;

	private nint _bitmap;

	private nint _bits;

	internal NativeWallpaperSurface(nint window, int width, int height)
	{
		if (window == 0)
		{
			throw new ArgumentException("A wallpaper carrier is required.", "window");
		}
		if (width <= 0 || height <= 0 || (long)width * (long)height > 32000000)
		{
			throw new ArgumentOutOfRangeException("width");
		}
		_window = window;
		_width = width;
		_height = height;
		checked
		{
			_stride = (width * 3 + 3) & -4;
			_pixels = new byte[_stride * height];
		}
		BitmapInfo info = new BitmapInfo
		{
			Header = new BitmapInfoHeader
			{
				Size = (uint)Marshal.SizeOf<BitmapInfoHeader>(),
				Width = width,
				Height = -height,
				Planes = 1,
				BitCount = 24
			}
		};
		_bitmap = CreateDIBSection(0, ref info, 0u, out _bits, 0, 0u);
		if (_bitmap == 0 || _bits == 0)
		{
			int lastWin32Error = Marshal.GetLastWin32Error();
			Dispose();
			throw new Win32Exception(lastWin32Error, "Cannot create the native wallpaper bitmap.");
		}
	}

	internal void Update(byte[] pixels)
	{
		ObjectDisposedException.ThrowIf(_bitmap == 0, this);
		ArgumentNullException.ThrowIfNull(pixels, "pixels");
		if (pixels.Length != checked(_width * _height * 4))
		{
			throw new ArgumentException("Wallpaper geometry does not match the scene.", "pixels");
		}
		for (int i = 0; i < _height; i++)
		{
			int num = i * _width * 4;
			int num2 = i * _stride;
			int num3 = 0;
			while (num3 < _width)
			{
				_pixels[num2] = pixels[num];
				_pixels[num2 + 1] = pixels[num + 1];
				_pixels[num2 + 2] = pixels[num + 2];
				num3++;
				num += 4;
				num2 += 3;
			}
		}
		Marshal.Copy(_pixels, 0, _bits, _pixels.Length);
		SendMessageW(_window, 370u, 0, _bitmap);
		InvalidateRect(_window, 0, erase: false);
		UpdateWindow(_window);
		Marshal.ThrowExceptionForHR(DwmFlush());
	}

	public void Dispose()
	{
		if (_bitmap != 0)
		{
			SendMessageW(_window, 370u, 0, 0);
			DeleteObject(_bitmap);
			_bitmap = (_bits = 0);
		}
	}

	[DllImport("gdi32.dll", SetLastError = true)]
	private static extern nint CreateDIBSection(nint context, ref BitmapInfo info, uint usage, out nint bits, nint section, uint offset);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteObject(nint bitmap);

	[DllImport("user32.dll")]
	private static extern nint SendMessageW(nint window, uint message, nint wParam, nint lParam);

	[DllImport("user32.dll")]
	private static extern bool InvalidateRect(nint window, nint rectangle, bool erase);

	[DllImport("user32.dll")]
	private static extern bool UpdateWindow(nint window);

	[DllImport("dwmapi.dll")]
	private static extern int DwmFlush();
}
