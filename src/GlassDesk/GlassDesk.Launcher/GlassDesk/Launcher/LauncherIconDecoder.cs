using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace GlassDesk.Launcher;

internal static class LauncherIconDecoder
{
	internal static BitmapImage DecodeFile(string path, int maximumPixels)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumPixels, "maximumPixels");
		int pixelWidth;
		int pixelHeight;
		using (FileStream bitmapStream = File.OpenRead(path))
		{
			BitmapFrame bitmapFrame = BitmapDecoder.Create(bitmapStream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None).Frames[0];
			pixelWidth = bitmapFrame.PixelWidth;
			pixelHeight = bitmapFrame.PixelHeight;
		}
		BitmapImage bitmapImage = new BitmapImage();
		bitmapImage.BeginInit();
		bitmapImage.UriSource = new Uri(path, UriKind.Absolute);
		bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
		bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
		if (Math.Max(pixelWidth, pixelHeight) > maximumPixels)
		{
			if (pixelWidth >= pixelHeight)
			{
				bitmapImage.DecodePixelWidth = maximumPixels;
			}
			else
			{
				bitmapImage.DecodePixelHeight = maximumPixels;
			}
		}
		bitmapImage.EndInit();
		bitmapImage.Freeze();
		return bitmapImage;
	}
}
