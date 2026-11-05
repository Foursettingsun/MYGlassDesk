using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GlassDesk.Platform;

namespace GlassDesk.App.Tray;

internal static class TrayIconArtwork
{
	public static nint CreateHandle(int size)
	{
		return IconFactory.CreateIconFromBgra(ToStraightBgra(Render(size)), size, size);
	}

	public static BitmapSource RenderBitmap(int size)
	{
		return Render(size);
	}

	private static RenderTargetBitmap Render(int size)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size, "size");
		DrawingImage drawingImage = (Application.Current?.TryFindResource("GlassDeskBrandMark") as DrawingImage) ?? throw new InvalidOperationException("The GlassDesk brand mark resource is unavailable.");
		DrawingVisual drawingVisual = new DrawingVisual();
		RenderOptions.SetBitmapScalingMode(drawingVisual, BitmapScalingMode.HighQuality);
		using (DrawingContext drawingContext = drawingVisual.RenderOpen())
		{
			double num = (double)size / 256.0;
			drawingContext.PushTransform(new ScaleTransform(num, num));
			drawingContext.DrawDrawing(drawingImage.Drawing);
			drawingContext.Pop();
		}
		RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(size, size, 96.0, 96.0, PixelFormats.Pbgra32);
		renderTargetBitmap.Render(drawingVisual);
		renderTargetBitmap.Freeze();
		return renderTargetBitmap;
	}

	private static byte[] ToStraightBgra(BitmapSource source)
	{
		FormatConvertedBitmap formatConvertedBitmap = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0.0);
		formatConvertedBitmap.Freeze();
		int num = formatConvertedBitmap.PixelWidth * 4;
		byte[] array = new byte[num * formatConvertedBitmap.PixelHeight];
		formatConvertedBitmap.CopyPixels(array, num, 0);
		return array;
	}
}
