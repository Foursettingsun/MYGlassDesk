using System;
using System.Globalization;
using System.IO;
using GlassDesk.Core.Models;

namespace GlassDesk.Core.Storage;

public static class AppEntryKey
{
	public static string Compute(AppEntry entry)
	{
		ArgumentNullException.ThrowIfNull(entry, "entry");
		string text = NormalizeTarget(entry.Target, entry.Kind);
		string text2 = (entry.Arguments ?? string.Empty).Trim();
		if (text2.Length != 0)
		{
			return text + "\0" + text2;
		}
		return text;
	}

	private static string NormalizeTarget(string target, AppItemKind kind)
	{
		if (string.IsNullOrWhiteSpace(target))
		{
			return string.Empty;
		}
		string text = target.Trim();
		if (kind == AppItemKind.Url)
		{
			if (!Uri.TryCreate(text, UriKind.Absolute, out Uri result))
			{
				return text.ToLowerInvariant();
			}
			return NormalizeUrl(result);
		}
		try
		{
			return Path.GetFullPath(text).TrimEnd(new char[2]
			{
				Path.DirectorySeparatorChar,
				Path.AltDirectorySeparatorChar
			}).ToLowerInvariant();
		}
		catch (Exception ex) when ((ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException) ? true : false)
		{
			return text.ToLowerInvariant();
		}
	}

	private static string NormalizeUrl(Uri uri)
	{
		string value = uri.Scheme.ToLowerInvariant();
		string value2 = uri.Host.ToLowerInvariant();
		string value3 = (uri.IsDefaultPort ? string.Empty : (":" + uri.Port.ToString(CultureInfo.InvariantCulture)));
		return $"{value}://{value2}{value3}{uri.PathAndQuery}{uri.Fragment}";
	}
}
