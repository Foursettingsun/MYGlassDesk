using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using Microsoft.Win32;

namespace GlassDesk.UI.Controls;

internal sealed class WindowsWallpaperCache
{
	private static readonly Encoding IdentityEncoding = new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true);

	private readonly string _cachePath;

	private readonly Func<(byte[]? Identity, string? Wallpaper)> _readIdentity;

	internal WindowsWallpaperCache()
		: this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Themes", "TranscodedWallpaper"), ReadIdentity)
	{
	}

	internal WindowsWallpaperCache(string cachePath, Func<(byte[]? Identity, string? Wallpaper)> readIdentity)
	{
		_cachePath = cachePath;
		_readIdentity = readIdentity;
	}

	internal string? Resolve(string originalPath, int monitorCount)
	{
		if (monitorCount != 1 || string.IsNullOrWhiteSpace(originalPath))
		{
			return null;
		}
		try
		{
			string text = Normalize(originalPath);
			if (text == null)
			{
				return null;
			}
			(byte[], string) tuple = _readIdentity();
			byte[] item = tuple.Item1;
			string item2 = tuple.Item2;
			bool flag = item == null;
			if (!flag)
			{
				int num = item.Length;
				bool flag2 = ((num < 26 || num > 65536) ? true : false);
				flag = flag2;
			}
			if (flag || (item.Length - 24) % 2 != 0)
			{
				return null;
			}
			string text2 = IdentityEncoding.GetString(item, 24, item.Length - 24);
			int num2 = text2.IndexOf('\0');
			if (num2 <= 0)
			{
				return null;
			}
			string b = Normalize(text2.Substring(0, num2));
			if (!string.Equals(text, b, StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}
			if (!string.IsNullOrEmpty(item2))
			{
				string b2 = Normalize(item2);
				if (!string.Equals(text, b2, StringComparison.OrdinalIgnoreCase) && !string.Equals(Normalize(_cachePath), b2, StringComparison.OrdinalIgnoreCase))
				{
					return null;
				}
			}
			FileInfo fileInfo = new FileInfo(_cachePath);
			object result;
			if (fileInfo.Exists)
			{
				long length = fileInfo.Length;
				if (length > 0 && length <= 33554432)
				{
					result = fileInfo.FullName;
					goto IL_016d;
				}
			}
			result = null;
			goto IL_016d;
			IL_016d:
			return (string?)result;
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is NotSupportedException || ex is SecurityException) ? true : false)
		{
			return null;
		}
	}

	private static string? Normalize(string path)
	{
		if (!Path.IsPathFullyQualified(path))
		{
			return null;
		}
		return Path.GetFullPath(path);
	}

	private static (byte[]? Identity, string? Wallpaper) ReadIdentity()
	{
		using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop", writable: false);
		return (Identity: registryKey?.GetValue("TranscodedImageCache") as byte[], Wallpaper: registryKey?.GetValue("Wallpaper") as string);
	}
}
