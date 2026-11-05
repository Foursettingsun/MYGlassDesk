using System;
using System.Runtime.InteropServices;
using GlassDesk.Platform.Displays;

namespace GlassDesk.Platform;

public sealed class DesktopRegionSampler : IDisposable
{
	private struct BitmapInfo
	{
		public uint Size;

		public int Width;

		public int Height;

		public ushort Planes;

		public ushort Bits;

		public uint Compression;

		public uint ImageSize;

		public int XPels;

		public int YPels;

		public uint Colors;

		public uint ImportantColors;
	}

	private nint _sourceDc;

	private nint _outputDc;

	private nint _sourceBitmap;

	private nint _outputBitmap;

	private nint _oldSource;

	private nint _oldOutput;

	private nint _bits;

	private (int RegionWidth, int RegionHeight, int Width, int Height) _size;

	private byte[]? _pixels;

	public bool TryCapture(PixelRect region, int width, int height, out byte[]? pixels)
	{
		return TryCapture(region, width, height, beforeReveal: false, out pixels);
	}

	public bool TryCaptureBeforeReveal(PixelRect region, int width, int height, out byte[]? pixels)
	{
		return TryCapture(region, width, height, beforeReveal: true, out pixels);
	}

	public bool TryCaptureBeforeReveal(PixelRect region, int width, int height, nint emptyOwner, out byte[]? pixels)
	{
		return TryCapture(region, width, height, beforeReveal: true, out pixels, emptyOwner);
	}

	private bool TryCapture(PixelRect region, int width, int height, bool beforeReveal, out byte[]? pixels, nint emptyOwner = 0)
	{
		pixels = null;
		if (region.Width <= 0 || region.Height <= 0 || (long)region.Width * (long)region.Height > 32000000 || width <= 0 || height <= 0 || width > 2048 || height > 2048 || (long)width * (long)height > 500000 || !(beforeReveal ? CaptureExclusionSession.CanCaptureBeforeReveal(region, emptyOwner) : CaptureExclusionSession.CanCaptureRegion(region)))
		{
			return false;
		}
		nint dC = GetDC(0);
		if (dC == 0)
		{
			return false;
		}
		try
		{
			(int, int, int, int) tuple = (region.Width, region.Height, width, height);
			(int, int, int, int) size = _size;
			(int, int, int, int) tuple2 = tuple;
			if (size.Item1 != tuple2.Item1 || size.Item2 != tuple2.Item2 || size.Item3 != tuple2.Item3 || size.Item4 != tuple2.Item4 || _bits == 0)
			{
				Dispose();
				_sourceDc = CreateCompatibleDC(dC);
				_outputDc = CreateCompatibleDC(dC);
				_sourceBitmap = CreateCompatibleBitmap(dC, region.Width, region.Height);
				BitmapInfo info = new BitmapInfo
				{
					Size = (uint)Marshal.SizeOf<BitmapInfo>(),
					Width = width,
					Height = -height,
					Planes = 1,
					Bits = 32
				};
				_outputBitmap = CreateDIBSection(dC, ref info, 0u, out _bits, 0, 0u);
				if (_sourceDc == 0 || _outputDc == 0 || _sourceBitmap == 0 || _outputBitmap == 0 || _bits == 0)
				{
					Dispose();
					return false;
				}
				_oldSource = SelectObject(_sourceDc, _sourceBitmap);
				_oldOutput = SelectObject(_outputDc, _outputBitmap);
				if (_oldSource == 0 || _oldOutput == 0 || _oldSource == -1 || _oldOutput == -1)
				{
					Dispose();
					return false;
				}
				if (SetStretchBltMode(_outputDc, 3) == 0)
				{
					Dispose();
					return false;
				}
				_size = tuple;
				_pixels = new byte[width * height * 4];
			}
			if (!BitBlt(_sourceDc, 0, 0, region.Width, region.Height, dC, region.X, region.Y, 1087111200u) || !StretchBlt(_outputDc, 0, 0, width, height, _sourceDc, 0, 0, region.Width, region.Height, 13369376u))
			{
				return false;
			}
			GdiFlush();
			Marshal.Copy(_bits, _pixels, 0, _pixels.Length);
			for (int i = 3; i < _pixels.Length; i += 4)
			{
				_pixels[i] = byte.MaxValue;
			}
			pixels = _pixels;
			return true;
		}
		finally
		{
			ReleaseDC(0, dC);
		}
	}

	public void Dispose()
	{
		if (_sourceDc != 0 && _oldSource != 0 && _oldSource != -1)
		{
			SelectObject(_sourceDc, _oldSource);
		}
		if (_outputDc != 0 && _oldOutput != 0 && _oldOutput != -1)
		{
			SelectObject(_outputDc, _oldOutput);
		}
		if (_sourceBitmap != 0)
		{
			DeleteObject(_sourceBitmap);
		}
		if (_outputBitmap != 0)
		{
			DeleteObject(_outputBitmap);
		}
		if (_sourceDc != 0)
		{
			DeleteDC(_sourceDc);
		}
		if (_outputDc != 0)
		{
			DeleteDC(_outputDc);
		}
		_sourceDc = (_outputDc = (_sourceBitmap = (_outputBitmap = (_oldSource = (_oldOutput = (_bits = 0))))));
		_pixels = null;
		_size = default;
	}

	[DllImport("user32.dll")]
	private static extern nint GetDC(nint window);

	[DllImport("user32.dll")]
	private static extern int ReleaseDC(nint window, nint dc);

	[DllImport("gdi32.dll")]
	private static extern nint CreateCompatibleDC(nint dc);

	[DllImport("gdi32.dll")]
	private static extern nint CreateCompatibleBitmap(nint dc, int width, int height);

	[DllImport("gdi32.dll")]
	private static extern nint CreateDIBSection(nint dc, ref BitmapInfo info, uint usage, out nint bits, nint section, uint offset);

	[DllImport("gdi32.dll")]
	private static extern nint SelectObject(nint dc, nint obj);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteObject(nint obj);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteDC(nint dc);

	[DllImport("gdi32.dll")]
	private static extern bool BitBlt(nint dst, int x, int y, int width, int height, nint src, int sx, int sy, uint op);

	[DllImport("gdi32.dll")]
	private static extern bool StretchBlt(nint dst, int x, int y, int width, int height, nint src, int sx, int sy, int sw, int sh, uint op);

	[DllImport("gdi32.dll")]
	private static extern int SetStretchBltMode(nint dc, int mode);

	[DllImport("gdi32.dll")]
	private static extern bool GdiFlush();
}
