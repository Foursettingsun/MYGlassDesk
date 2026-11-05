#define TRACE
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GlassDesk.UI.CaptureCompatibility;

internal sealed class LiveWallpaperSurface : IDisposable
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

		public uint ClrUsed;

		public uint ClrImportant;
	}

	private struct BitmapInfo
	{
		public BitmapInfoHeader Header;

		public uint Colors;
	}

	private const uint RenderFullContent = 2u;

	private const uint SourceCopy = 13369376u;

	private const int HalfTone = 3;

	private readonly int _sourceWidth;

	private readonly int _sourceHeight;

	private readonly int _targetWidth;

	private readonly int _targetHeight;

	private nint _sourceDc;

	private nint _sourceBitmap;

	private nint _sourceOld;

	private nint _sourceBits;

	private nint _targetDc;

	private nint _targetBitmap;

	private nint _targetOld;

	private nint _targetBits;

	private bool _disposed;

	internal LiveWallpaperSurface(int sourceWidth, int sourceHeight, int targetWidth, int targetHeight)
	{
		if (sourceWidth <= 0 || sourceHeight <= 0 || (long)sourceWidth * (long)sourceHeight > 64000000)
		{
			throw new ArgumentOutOfRangeException("sourceWidth");
		}
		if (targetWidth <= 0 || targetHeight <= 0 || (long)targetWidth * (long)targetHeight > 32000000)
		{
			throw new ArgumentOutOfRangeException("targetWidth");
		}
		_sourceWidth = sourceWidth;
		_sourceHeight = sourceHeight;
		_targetWidth = targetWidth;
		_targetHeight = targetHeight;
		try
		{
			_sourceDc = CreateCompatibleDC(0);
			_targetDc = CreateCompatibleDC(0);
			_sourceBitmap = CreateDib(_sourceDc, sourceWidth, sourceHeight, out _sourceBits);
			_targetBitmap = CreateDib(_targetDc, targetWidth, targetHeight, out _targetBits);
			if (_sourceDc == 0 || _targetDc == 0 || _sourceBitmap == 0 || _targetBitmap == 0 || _sourceBits == 0 || _targetBits == 0)
			{
				throw new Win32Exception(Marshal.GetLastWin32Error(), "Live wallpaper buffers are unavailable.");
			}
			_sourceOld = SelectObject(_sourceDc, _sourceBitmap);
			_targetOld = SelectObject(_targetDc, _targetBitmap);
			if (_sourceOld == 0 || _targetOld == 0)
			{
				throw new Win32Exception(Marshal.GetLastWin32Error(), "Live wallpaper buffer selection failed.");
			}
			if (SetStretchBltMode(_targetDc, 3) == 0)
			{
				throw new Win32Exception(Marshal.GetLastWin32Error(), "Live wallpaper scaling mode failed.");
			}
		}
		catch
		{
			Dispose();
			throw;
		}
	}

	internal bool TryDraw(nint window, int regionX, int regionY, int regionWidth, int regionHeight, int windowLeft, int windowTop, int windowRight, int windowBottom, byte[] target)
	{
		if (_disposed || window == 0 || target.Length < (long)_targetWidth * (long)_targetHeight * 4)
		{
			return false;
		}
		int num = Math.Max(regionX, windowLeft);
		int num2 = Math.Max(regionY, windowTop);
		int num3 = Math.Min(regionX + regionWidth, windowRight);
		int num4 = Math.Min(regionY + regionHeight, windowBottom);
		if (num3 <= num || num4 <= num2)
		{
			return false;
		}
		try
		{
			Marshal.ThrowExceptionForHR(DwmFlush());
			if (!PrintWindow(window, _sourceDc, 2u))
			{
				return false;
			}
			double num5 = (double)_targetWidth / (double)regionWidth;
			double num6 = (double)_targetHeight / (double)regionHeight;
			int num7 = (int)Math.Floor((double)(num - regionX) * num5);
			int num8 = (int)Math.Floor((double)(num2 - regionY) * num6);
			int num9 = (int)Math.Ceiling((double)(num3 - regionX) * num5);
			int num10 = (int)Math.Ceiling((double)(num4 - regionY) * num6);
			Marshal.Copy(target, 0, _targetBits, _targetWidth * _targetHeight * 4);
			if (!StretchBlt(_targetDc, num7, num8, Math.Max(1, num9 - num7), Math.Max(1, num10 - num8), _sourceDc, num - windowLeft, num2 - windowTop, num3 - num, num4 - num2, 13369376u))
			{
				return false;
			}
			GdiFlush();
			Marshal.Copy(_targetBits, target, 0, _targetWidth * _targetHeight * 4);
			for (int i = 3; i < target.Length; i += 4)
			{
				target[i] = byte.MaxValue;
			}
			return true;
		}
		catch (Exception ex)
		{
			Trace.WriteLine("Live wallpaper frame failed: " + ex.Message);
			return false;
		}
	}

	private static nint CreateDib(nint dc, int width, int height, out nint bits)
	{
		BitmapInfoHeader header = new BitmapInfoHeader
		{
			Size = (uint)Marshal.SizeOf<BitmapInfoHeader>(),
			Width = width,
			Height = -height,
			Planes = 1,
			BitCount = 32,
			Compression = 0u
		};
		BitmapInfo info = new BitmapInfo
		{
			Header = header
		};
		return CreateDIBSection(dc, ref info, 0u, out bits, 0, 0u);
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			if (_sourceDc != 0 && _sourceOld != 0)
			{
				SelectObject(_sourceDc, _sourceOld);
			}
			if (_targetDc != 0 && _targetOld != 0)
			{
				SelectObject(_targetDc, _targetOld);
			}
			if (_sourceBitmap != 0)
			{
				DeleteObject(_sourceBitmap);
			}
			if (_targetBitmap != 0)
			{
				DeleteObject(_targetBitmap);
			}
			if (_sourceDc != 0)
			{
				DeleteDC(_sourceDc);
			}
			if (_targetDc != 0)
			{
				DeleteDC(_targetDc);
			}
			_sourceDc = (_targetDc = (_sourceBitmap = (_targetBitmap = (_sourceOld = (_targetOld = (_sourceBits = (_targetBits = 0)))))));
		}
	}

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool PrintWindow(nint window, nint dc, uint flags);

	[DllImport("gdi32.dll", SetLastError = true)]
	private static extern nint CreateCompatibleDC(nint dc);

	[DllImport("gdi32.dll", SetLastError = true)]
	private static extern nint CreateDIBSection(nint dc, ref BitmapInfo info, uint usage, out nint bits, nint section, uint offset);

	[DllImport("gdi32.dll", SetLastError = true)]
	private static extern nint SelectObject(nint dc, nint handle);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteObject(nint handle);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteDC(nint dc);

	[DllImport("gdi32.dll")]
	private static extern int SetStretchBltMode(nint dc, int mode);

	[DllImport("gdi32.dll")]
	private static extern bool StretchBlt(nint destination, int x, int y, int width, int height, nint source, int sourceX, int sourceY, int sourceWidth, int sourceHeight, uint operation);

	[DllImport("gdi32.dll")]
	private static extern bool GdiFlush();

	[DllImport("dwmapi.dll")]
	private static extern int DwmFlush();
}
