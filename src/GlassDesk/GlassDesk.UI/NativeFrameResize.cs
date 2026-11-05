using System;
using System.Runtime.InteropServices;

internal sealed class NativeFrameResize : IDisposable
{
	private struct Info
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

		public uint Important;
	}

	private nint _sourceDc;

	private nint _destDc;

	private nint _sourceBitmap;

	private nint _destBitmap;

	private nint _sourceOld;

	private nint _destOld;

	private nint _sourceBits;

	private nint _destBits;

	private (int, int, int, int) _size;

	private PixelBounds _crop;

	private byte[]? _lastPixels;

	private bool _sampleReady;

	internal byte[] Resize(CapturePixels frame, int width, int height)
	{
		return Resize(frame, width, height, new PixelBounds(0, 0, frame.Width, frame.Height));
	}

	internal byte[] Resize(CapturePixels frame, int width, int height, PixelBounds crop)
	{
		return ResizeCore(frame, width, height, crop, null);
	}

	internal void ResizeInto(CapturePixels frame, int width, int height, PixelBounds crop, byte[] destination)
	{
		if (destination.Length != (long)width * (long)height * 4)
		{
			throw new ArgumentException("Invalid destination sample extent.");
		}
		ResizeCore(frame, width, height, crop, destination);
	}

	private unsafe byte[] ResizeCore(CapturePixels frame, int width, int height, PixelBounds crop, byte[]? destination)
	{
		if (frame.Width <= 0 || frame.Height <= 0 || width <= 0 || height <= 0 || frame.Pixels.Length != (long)frame.Width * (long)frame.Height * 4)
		{
			throw new ArgumentException("Invalid sample resize extent.");
		}
		if (crop.Left < 0 || crop.Top < 0 || crop.Right > frame.Width || crop.Bottom > frame.Height || crop.Width <= 0 || crop.Height <= 0)
		{
			throw new ArgumentOutOfRangeException("crop");
		}
		(int, int, int, int) tuple = (frame.Width, frame.Height, width, height);
		(int, int, int, int) size = _size;
		(int, int, int, int) tuple2 = tuple;
		if (size.Item1 != tuple2.Item1 || size.Item2 != tuple2.Item2 || size.Item3 != tuple2.Item3 || size.Item4 != tuple2.Item4)
		{
			Dispose();
			_sourceDc = CreateCompatibleDC(0);
			_destDc = CreateCompatibleDC(0);
			Info info = new Info
			{
				Size = 40u,
				Width = frame.Width,
				Height = -frame.Height,
				Planes = 1,
				Bits = 32
			};
			Info info2 = new Info
			{
				Size = 40u,
				Width = width,
				Height = -height,
				Planes = 1,
				Bits = 32
			};
			_sourceBitmap = CreateDIBSection(_sourceDc, ref info, 0u, out _sourceBits, 0, 0u);
			_destBitmap = CreateDIBSection(_destDc, ref info2, 0u, out _destBits, 0, 0u);
			if (_sourceDc == 0 || _destDc == 0 || _sourceBitmap == 0 || _destBitmap == 0 || _sourceBits == 0 || _destBits == 0)
			{
				throw new InvalidOperationException("Sample resize allocation failed.");
			}
			_sourceOld = SelectObject(_sourceDc, _sourceBitmap);
			_destOld = SelectObject(_destDc, _destBitmap);
			if (SetStretchBltMode(_destDc, 3) == 0)
			{
				throw new InvalidOperationException("Sample resize mode failed.");
			}
			_size = tuple;
		}
		bool flag = _sampleReady && _crop == crop && ((ReadOnlySpan<byte>)frame.Pixels.AsSpan()).SequenceEqual(new ReadOnlySpan<byte>((void*)_sourceBits, frame.Pixels.Length));
		if (flag && destination == null && _lastPixels != null)
		{
			return _lastPixels;
		}
		if (!flag)
		{
			Marshal.Copy(frame.Pixels, 0, _sourceBits, frame.Pixels.Length);
			if (!StretchBlt(_destDc, 0, 0, width, height, _sourceDc, crop.Left, crop.Top, crop.Width, crop.Height, 13369376u))
			{
				throw new InvalidOperationException("Sample resize failed.");
			}
			GdiFlush();
			_sampleReady = true;
			_lastPixels = null;
		}
		byte[] array = destination ?? new byte[checked(width * height * 4)];
		Marshal.Copy(_destBits, array, 0, array.Length);
		for (int i = 3; i < array.Length; i += 4)
		{
			array[i] = byte.MaxValue;
		}
		_crop = crop;
		if (destination != null)
		{
			return array;
		}
		return _lastPixels = array;
	}

	public void Dispose()
	{
		if (_sourceOld != 0)
		{
			SelectObject(_sourceDc, _sourceOld);
		}
		if (_destOld != 0)
		{
			SelectObject(_destDc, _destOld);
		}
		if (_sourceBitmap != 0)
		{
			DeleteObject(_sourceBitmap);
		}
		if (_destBitmap != 0)
		{
			DeleteObject(_destBitmap);
		}
		if (_sourceDc != 0)
		{
			DeleteDC(_sourceDc);
		}
		if (_destDc != 0)
		{
			DeleteDC(_destDc);
		}
		_sourceDc = (_destDc = (_sourceBitmap = (_destBitmap = (_sourceOld = (_destOld = (_sourceBits = (_destBits = 0)))))));
		_size = default;
		_lastPixels = null;
		_crop = default;
		_sampleReady = false;
	}

	[DllImport("gdi32.dll")]
	private static extern nint CreateCompatibleDC(nint dc);

	[DllImport("gdi32.dll")]
	private static extern nint CreateDIBSection(nint dc, ref Info info, uint usage, out nint bits, nint section, uint offset);

	[DllImport("gdi32.dll")]
	private static extern nint SelectObject(nint dc, nint value);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteObject(nint value);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteDC(nint dc);

	[DllImport("gdi32.dll")]
	private static extern int SetStretchBltMode(nint dc, int mode);

	[DllImport("gdi32.dll")]
	private static extern bool StretchBlt(nint destination, int x, int y, int width, int height, nint source, int sx, int sy, int sw, int sh, uint operation);

	[DllImport("gdi32.dll")]
	private static extern bool GdiFlush();
}
