using System;
using System.IO;

namespace GlassDesk.AppServices;

public static class ImageFormatSniffer
{
	private static ReadOnlySpan<byte> PngSignature => new byte[8] { 137, 80, 78, 71, 13, 10, 26, 10 };

	private static ReadOnlySpan<byte> JpegSignature => new byte[3] { 255, 216, 255 };

	private static ReadOnlySpan<byte> BmpSignature => "BM"u8;

	private static ReadOnlySpan<byte> GifSignature => "GIF8"u8;

	private static ReadOnlySpan<byte> IcoSignature => new byte[4] { 0, 0, 1, 0 };

	public static ImageFormat? Detect(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return null;
		}
		Span<byte> buffer = stackalloc byte[16];
		int num;
		try
		{
			using FileStream fileStream = File.OpenRead(path);
			num = fileStream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException) ? true : false)
		{
			return null;
		}
		if (num < 4)
		{
			return null;
		}
		Span<byte> span = buffer.Slice(0, num);
		if (StartsWith(span, PngSignature))
		{
			return ImageFormat.Png;
		}
		if (StartsWith(span, JpegSignature))
		{
			return ImageFormat.Jpeg;
		}
		if (StartsWith(span, BmpSignature))
		{
			return ImageFormat.Bmp;
		}
		if (StartsWith(span, GifSignature))
		{
			return ImageFormat.Gif;
		}
		if (StartsWith(span, IcoSignature))
		{
			return ImageFormat.Ico;
		}
		return null;
	}

	public static bool StartsWith(ReadOnlySpan<byte> data, ReadOnlySpan<byte> prefix)
	{
		if (data.Length >= prefix.Length)
		{
			return data.Slice(0, prefix.Length).SequenceEqual(prefix);
		}
		return false;
	}

	public static string FileExtensionFor(ImageFormat format)
	{
		return format switch
		{
			ImageFormat.Png => ".png", 
			ImageFormat.Jpeg => ".jpg", 
			ImageFormat.Bmp => ".bmp", 
			ImageFormat.Gif => ".gif", 
			ImageFormat.Ico => ".ico", 
			_ => ".bin", 
		};
	}
}
