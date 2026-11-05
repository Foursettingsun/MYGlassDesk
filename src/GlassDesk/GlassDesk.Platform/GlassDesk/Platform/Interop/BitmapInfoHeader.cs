namespace GlassDesk.Platform.Interop;

internal struct BitmapInfoHeader
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
}
