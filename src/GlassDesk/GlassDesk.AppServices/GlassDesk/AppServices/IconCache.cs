using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using GlassDesk.Core.Imaging;
using GlassDesk.Core.Models;
using GlassDesk.Core.Storage;
using GlassDesk.Platform.Shell;

namespace GlassDesk.AppServices;

public sealed class IconCache
{
	private const string CacheKeyVersion = "icon-file-resource-v4";

	private readonly AppPaths _paths;

	public int FileCount
	{
		get
		{
			if (!Directory.Exists(_paths.IconCacheDirectory))
			{
				return 0;
			}
			return Directory.GetFiles(_paths.IconCacheDirectory).Count(IsRegenerableCacheFile);
		}
	}

	public IconCache(AppPaths paths)
	{
		ArgumentNullException.ThrowIfNull(paths, "paths");
		_paths = paths;
	}

	public CachedIcon? EnsureFor(string? iconSource)
	{
		if (string.IsNullOrWhiteSpace(iconSource))
		{
			return null;
		}
		string path = PathFor(iconSource);
		if (File.Exists(path))
		{
			var (num, height) = ReadPngSize(path);
			if (num <= 0)
			{
				return null;
			}
			return new CachedIcon(path, num, height, CreatedFile: false);
		}
		ExtractedIcon extractedIcon = IconExtractor.Extract(iconSource);
		if ((object)extractedIcon == null || extractedIcon.VisiblePixelCount == 0)
		{
			return null;
		}
		(byte[], int, int) tuple2 = IconCanvasNormalizer.Normalize(extractedIcon.Bgra, extractedIcon.Width, extractedIcon.Height);
		try
		{
			WriteAtomically(path, PngWriter.EncodeBgra(tuple2.Item1, tuple2.Item2, tuple2.Item3));
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
			return null;
		}
		return new CachedIcon(path, tuple2.Item2, tuple2.Item3, CreatedFile: true);
	}

	public string? ImportCustomIcon(string sourcePath, string itemId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath, "sourcePath");
		ArgumentException.ThrowIfNullOrWhiteSpace(itemId, "itemId");
		ImageFormat? imageFormat = ImageFormatSniffer.Detect(sourcePath);
		if (!imageFormat.HasValue)
		{
			return null;
		}
		string text = ImageFormatSniffer.FileExtensionFor(imageFormat.Value);
		string text2 = Path.Combine(_paths.IconCacheDirectory, "custom-" + Sanitize(itemId) + text);
		try
		{
			Directory.CreateDirectory(_paths.IconCacheDirectory);
			File.Copy(sourcePath, text2, overwrite: true);
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
			return null;
		}
		string[] files = Directory.GetFiles(_paths.IconCacheDirectory, "custom-" + Sanitize(itemId) + ".*");
		foreach (string text3 in files)
		{
			if (!string.Equals(text3, text2, StringComparison.OrdinalIgnoreCase))
			{
				TryDelete(text3);
			}
		}
		return text2;
	}

	public void RemoveCustomIcon(string? itemId)
	{
		if (!string.IsNullOrWhiteSpace(itemId) && Directory.Exists(_paths.IconCacheDirectory))
		{
			string[] files = Directory.GetFiles(_paths.IconCacheDirectory, "custom-" + Sanitize(itemId) + ".*");
			for (int i = 0; i < files.Length; i++)
			{
				TryDelete(files[i]);
			}
		}
	}

	public IconCacheClearResult Clear()
	{
		if (!Directory.Exists(_paths.IconCacheDirectory))
		{
			return new IconCacheClearResult(0, 0);
		}
		int num = 0;
		int num2 = 0;
		string[] files = Directory.GetFiles(_paths.IconCacheDirectory);
		foreach (string path in files)
		{
			if (!IsRegenerableCacheFile(path))
			{
				num2++;
			}
			else if (TryDelete(path))
			{
				num++;
			}
			else
			{
				num2++;
			}
		}
		return new IconCacheClearResult(num, num2);
	}

	private static bool IsRegenerableCacheFile(string path)
	{
		string fileName = Path.GetFileName(path);
		if (fileName.Length != 36 || !fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		for (int i = 0; i < 32; i++)
		{
			if (!Uri.IsHexDigit(fileName[i]))
			{
				return false;
			}
		}
		return true;
	}

	public string PathFor(string iconSource)
	{
		string s = "icon-file-resource-v4\0" + iconSource.Trim().ToLowerInvariant();
		string text = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)).AsSpan(0, 16)).ToLowerInvariant();
		return Path.Combine(_paths.IconCacheDirectory, text + ".png");
	}

	private static void WriteAtomically(string path, byte[] content)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path));
		string text = path + ".tmp";
		using (FileStream fileStream = new FileStream(text, FileMode.Create, FileAccess.Write, FileShare.None))
		{
			fileStream.Write(content);
			fileStream.Flush(flushToDisk: true);
		}
		if (File.Exists(path))
		{
			File.Replace(text, path, null);
		}
		else
		{
			File.Move(text, path);
		}
	}

	private static (int Width, int Height) ReadPngSize(string path)
	{
		Span<byte> buffer = stackalloc byte[24];
		try
		{
			using FileStream fileStream = File.OpenRead(path);
			if (fileStream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false) < buffer.Length)
			{
				return (Width: 0, Height: 0);
			}
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
			return (Width: 0, Height: 0);
		}
		if (!((ReadOnlySpan<byte>)buffer.Slice(0, 8)).SequenceEqual((ReadOnlySpan<byte>)new byte[8] { 137, 80, 78, 71, 13, 10, 26, 10 }))
		{
			return (Width: 0, Height: 0);
		}
		int item = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(16, 4));
		int item2 = BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(20, 4));
		return (Width: item, Height: item2);
	}

	private static string Sanitize(string value)
	{
		StringBuilder stringBuilder = new StringBuilder(value.Length);
		foreach (char c in value)
		{
			StringBuilder stringBuilder2 = stringBuilder;
			bool flag = char.IsAsciiLetterOrDigit(c);
			if (!flag)
			{
				bool flag2 = ((c == '-' || c == '_') ? true : false);
				flag = flag2;
			}
			stringBuilder2.Append(flag ? c : '_');
		}
		return stringBuilder.ToString();
	}

	private static bool TryDelete(string path)
	{
		try
		{
			File.Delete(path);
			return true;
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
			return false;
		}
	}

	public bool ApplyCustomIcon(AppEntry entry, string? customIconPath)
	{
		ArgumentNullException.ThrowIfNull(entry, "entry");
		if (string.IsNullOrWhiteSpace(customIconPath))
		{
			if (entry.CustomIconPath != null)
			{
				entry.CustomIconPath = null;
				RemoveCustomIcon(entry.Id);
				return true;
			}
			return false;
		}
		string text = ImportCustomIcon(customIconPath, entry.Id);
		if (text == null)
		{
			return false;
		}
		entry.CustomIconPath = text;
		return true;
	}
}
