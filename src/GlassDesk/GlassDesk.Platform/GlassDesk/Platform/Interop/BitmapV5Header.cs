namespace GlassDesk.Platform.Interop;

internal struct BitmapV5Header
{
	internal int Size;

	internal int Width;

	internal int Height;

	internal ushort Planes;

	internal ushort BitCount;

	internal int Compression;

	internal int SizeImage;

	internal int XPelsPerMeter;

	internal int YPelsPerMeter;

	internal int ColorsUsed;

	internal int ColorsImportant;

	internal uint RedMask;

	internal uint GreenMask;

	internal uint BlueMask;

	internal uint AlphaMask;

	internal uint CsType;

	internal int Endpoints0;

	internal int Endpoints1;

	internal int Endpoints2;

	internal int Endpoints3;

	internal int Endpoints4;

	internal int Endpoints5;

	internal int Endpoints6;

	internal int Endpoints7;

	internal int Endpoints8;

	internal uint GammaRed;

	internal uint GammaGreen;

	internal uint GammaBlue;

	internal int Intent;

	internal int ProfileData;

	internal int ProfileSize;

	internal int Reserved;
}
