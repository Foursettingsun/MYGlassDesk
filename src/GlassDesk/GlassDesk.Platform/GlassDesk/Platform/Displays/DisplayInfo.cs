using System;

namespace GlassDesk.Platform.Displays;

public sealed record DisplayInfo(string DeviceName, bool IsPrimary, PixelRect Bounds, PixelRect WorkArea, uint Dpi)
{
	public double ScaleFactor => (double)Dpi / 96.0;

	public double ToDips(int physicalPixels)
	{
		return (double)physicalPixels * 96.0 / (double)Dpi;
	}

	public int ToPixels(double dips)
	{
		return (int)Math.Round(dips * (double)Dpi / 96.0);
	}

	public override string ToString()
	{
		return $"{DeviceName}{(IsPrimary ? "（主屏）" : string.Empty)} 边界={Bounds} 工作区={WorkArea} DPI={Dpi}（{ScaleFactor:P0}）";
	}
}
