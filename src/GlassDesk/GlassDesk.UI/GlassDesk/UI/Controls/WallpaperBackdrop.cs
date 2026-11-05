using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GlassDesk.Platform;

namespace GlassDesk.UI.Controls;

internal sealed class WallpaperBackdrop
{
	private sealed record WallpaperImage(BitmapSource Bitmap, int Width, int Height);

	private readonly Func<string, int, string?> _cachedWallpaper;

	private readonly Func<DesktopWallpaperSnapshot?> _readDesktop;

	private DesktopWallpaperSnapshot? _desktop;

	private DateTime _refreshAt;

	private int _revision;

	private readonly Dictionary<string, WallpaperImage?> _images = new Dictionary<string, WallpaperImage>(StringComparer.OrdinalIgnoreCase);

	private (Rect Region, int Width, int Height, int Revision, double Radius) _last;

	internal string SourceKind { get; private set; } = "Unavailable";

	public byte[]? Pixels { get; private set; }

	internal WallpaperBackdrop(Func<string, int, string?>? cachedWallpaper = null, Func<DesktopWallpaperSnapshot?>? readDesktop = null)
	{
		_cachedWallpaper = cachedWallpaper ?? new Func<string, int, string>(new WindowsWallpaperCache().Resolve);
		_readDesktop = readDesktop ?? new Func<DesktopWallpaperSnapshot>(DesktopWallpaperSource.TryRead);
	}

	public bool Update(Rect region, int width, int height, double cornerRadius)
	{
		if (DateTime.UtcNow >= _refreshAt)
		{
			_refreshAt = DateTime.UtcNow.AddSeconds(3.0);
			if (_images.Values.Any((WallpaperImage image) => (object)image == null))
			{
				_images.Clear();
				_last = default;
			}
			DesktopWallpaperSnapshot desktopWallpaperSnapshot = _readDesktop();
			HashCode hashCode = default;
			hashCode.Add(desktopWallpaperSnapshot?.Position);
			hashCode.Add(desktopWallpaperSnapshot?.BackgroundColor);
			foreach (WallpaperMonitor item in desktopWallpaperSnapshot?.Monitors ?? Array.Empty<WallpaperMonitor>())
			{
				hashCode.Add(item);
				hashCode.Add(GetWriteTime(item.Path));
				if (!string.IsNullOrWhiteSpace(item.Path) && !File.Exists(item.Path))
				{
					string text = _cachedWallpaper(item.Path, desktopWallpaperSnapshot.Monitors.Count);
					hashCode.Add(text);
					if (text != null)
					{
						hashCode.Add(FileState(text));
					}
				}
			}
			int num = hashCode.ToHashCode();
			if (num != _revision)
			{
				_images.Clear();
			}
			_revision = num;
			_desktop = desktopWallpaperSnapshot;
		}
		(Rect, int, int, int, double) tuple = (region, width, height, _revision, cornerRadius);
		(Rect, int, int, int, double) tuple2 = tuple;
		(Rect, int, int, int, double) last = _last;
		if (tuple2.Item1 == last.Item1 && tuple2.Item2 == last.Item2 && tuple2.Item3 == last.Item3 && tuple2.Item4 == last.Item4 && tuple2.Item5 == last.Item5)
		{
			return false;
		}
		_last = tuple;
		SourceKind = "Unavailable";
		if ((object)_desktop == null || _desktop.Monitors.Count == 0)
		{
			Pixels = null;
			return true;
		}
		uint backgroundColor = _desktop.BackgroundColor;
		SolidColorBrush brush = new SolidColorBrush(Color.FromRgb((byte)backgroundColor, (byte)(backgroundColor >> 8), (byte)(backgroundColor >> 16)));
		DrawingVisual drawingVisual = new DrawingVisual();
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		using (DrawingContext drawingContext = drawingVisual.RenderOpen())
		{
			drawingContext.PushClip(new RectangleGeometry(new Rect(0.0, 0.0, width, height)));
			drawingContext.DrawRectangle(brush, null, new Rect(0.0, 0.0, width, height));
			drawingContext.PushTransform(new MatrixTransform((double)width / region.Width, 0.0, 0.0, (double)height / region.Height, (0.0 - region.X) * (double)width / region.Width, (0.0 - region.Y) * (double)height / region.Height));
			Rect empty = Rect.Empty;
			foreach (WallpaperMonitor monitor in _desktop.Monitors)
			{
				empty.Union(new Rect(monitor.Bounds.X, monitor.Bounds.Y, monitor.Bounds.Width, monitor.Bounds.Height));
			}
			foreach (WallpaperMonitor monitor2 in _desktop.Monitors)
			{
				Rect rect = new Rect(monitor2.Bounds.X, monitor2.Bounds.Y, monitor2.Bounds.Width, monitor2.Bounds.Height);
				if (!rect.IntersectsWith(region))
				{
					continue;
				}
				WallpaperImage wallpaperImage = Load(monitor2.Path);
				if ((object)wallpaperImage == null && !string.IsNullOrWhiteSpace(monitor2.Path))
				{
					wallpaperImage = LoadCached(monitor2.Path, _desktop.Monitors.Count);
					flag3 |= (object)wallpaperImage != null;
				}
				else
				{
					flag2 |= (object)wallpaperImage != null;
				}
				if ((object)wallpaperImage == null)
				{
					flag |= !string.IsNullOrWhiteSpace(monitor2.Path);
					continue;
				}
				Rect rect2 = ((_desktop.Position == DesktopWallpaperPosition.Span) ? empty : rect);
				drawingContext.PushClip(new RectangleGeometry(rect));
				int width2 = wallpaperImage.Width;
				int height2 = wallpaperImage.Height;
				if (_desktop.Position == DesktopWallpaperPosition.Tile)
				{
					ImageBrush brush2 = new ImageBrush(wallpaperImage.Bitmap)
					{
						TileMode = TileMode.Tile,
						ViewportUnits = BrushMappingMode.Absolute,
						Viewport = new Rect(empty.X, empty.Y, width2, height2),
						Stretch = Stretch.Fill
					};
					drawingContext.DrawRectangle(brush2, null, rect);
				}
				else
				{
					double val = rect2.Width / (double)width2;
					double val2 = rect2.Height / (double)height2;
					double num2 = _desktop.Position switch
					{
						DesktopWallpaperPosition.Center => 1.0, 
						DesktopWallpaperPosition.Fit => Math.Min(val, val2), 
						_ => Math.Max(val, val2), 
					};
					Rect rectangle = ((_desktop.Position == DesktopWallpaperPosition.Stretch) ? rect2 : new Rect(rect2.X + (rect2.Width - (double)width2 * num2) / 2.0, rect2.Y + (rect2.Height - (double)height2 * num2) / 2.0, (double)width2 * num2, (double)height2 * num2));
					drawingContext.DrawImage(wallpaperImage.Bitmap, rectangle);
				}
				drawingContext.Pop();
			}
			drawingContext.Pop();
			drawingContext.Pop();
		}
		if (flag)
		{
			Pixels = null;
			return true;
		}
		RenderOptions.SetBitmapScalingMode(drawingVisual, BitmapScalingMode.HighQuality);
		RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(width, height, 96.0, 96.0, PixelFormats.Pbgra32);
		renderTargetBitmap.Render(drawingVisual);
		Pixels = new byte[width * height * 4];
		renderTargetBitmap.CopyPixels(Pixels, width * 4, 0);
		WallpaperBackdrop wallpaperBackdrop = this;
		string sourceKind;
		if (flag3)
		{
			sourceKind = "DecodedWindowsCache";
		}
		else
		{
			sourceKind = (flag2 ? "DecodedOriginal" : "SolidColor");
		}
		wallpaperBackdrop.SourceKind = sourceKind;
		return true;
	}

	private WallpaperImage? LoadCached(string originalPath, int monitorCount)
	{
		string text = _cachedWallpaper(originalPath, monitorCount);
		if (text == null)
		{
			return null;
		}
		(long, DateTime) tuple = FileState(text);
		var (num, _) = tuple;
		if ((num <= 0 || num > 33554432) ? true : false)
		{
			return null;
		}
		WallpaperImage result = Load(text);
		(long, DateTime) tuple3 = tuple;
		(long, DateTime) tuple4 = FileState(text);
		if (tuple3.Item1 != tuple4.Item1 || tuple3.Item2 != tuple4.Item2 || !string.Equals(text, _cachedWallpaper(originalPath, monitorCount), StringComparison.OrdinalIgnoreCase))
		{
			_images.Remove(text);
			return null;
		}
		return result;
	}

	private static (long Length, DateTime Modified) FileState(string path)
	{
		try
		{
			FileInfo fileInfo = new FileInfo(path);
			return fileInfo.Exists ? (Length: fileInfo.Length, Modified: fileInfo.LastWriteTimeUtc) : default((long, DateTime));
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is SecurityException) ? true : false)
		{
			return default;
		}
	}

	private static DateTime GetWriteTime(string path)
	{
		try
		{
			return File.GetLastWriteTimeUtc(path);
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException) ? true : false)
		{
			return default;
		}
	}

	private WallpaperImage? Load(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return null;
		}
		if (_images.TryGetValue(path, out WallpaperImage value))
		{
			return value;
		}
		WallpaperImage wallpaperImage = null;
		try
		{
			using FileStream fileStream = File.OpenRead(path);
			BitmapFrame bitmapFrame = BitmapDecoder.Create(fileStream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None).Frames[0];
			int pixelWidth = bitmapFrame.PixelWidth;
			int pixelHeight = bitmapFrame.PixelHeight;
			if (pixelWidth <= 0 || pixelHeight <= 0 || (long)pixelWidth * (long)pixelHeight > 64000000)
			{
				throw new NotSupportedException("Wallpaper dimensions exceed the decode budget.");
			}
			fileStream.Position = 0L;
			BitmapImage bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.StreamSource = fileStream;
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.DecodePixelWidth = Math.Max(1, (int)Math.Round((double)pixelWidth * Math.Min(1.0, 2048.0 / (double)Math.Max(pixelWidth, pixelHeight))));
			bitmapImage.EndInit();
			bitmapImage.Freeze();
			wallpaperImage = new WallpaperImage(bitmapImage, pixelWidth, pixelHeight);
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException || ex is NotSupportedException || ex is COMException || ex is ArgumentException || ex is FileFormatException) ? true : false)
		{
		}
		_images[path] = wallpaperImage;
		return wallpaperImage;
	}
}
