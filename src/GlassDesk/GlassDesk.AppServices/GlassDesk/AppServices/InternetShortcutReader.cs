using System;
using System.IO;
using System.Text;

namespace GlassDesk.AppServices;

public static class InternetShortcutReader
{
	private const string UrlKey = "URL=";

	public static string? ReadUrl(string path)
	{
		string[] array;
		try
		{
			array = File.ReadAllLines(path);
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
			return null;
		}
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			string text = array2[i].Trim();
			if (!text.StartsWith(';') && !text.StartsWith('#') && text.StartsWith("URL=", StringComparison.OrdinalIgnoreCase))
			{
				string text2 = text.Substring("URL=".Length).Trim();
				if (!IsSupportedUrl(text2))
				{
					return null;
				}
				return text2;
			}
		}
		return null;
	}

	public static bool IsSupportedUrl(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}
		if (Uri.TryCreate(value.Trim(), UriKind.Absolute, out Uri result))
		{
			if (!(result.Scheme == Uri.UriSchemeHttp))
			{
				return result.Scheme == Uri.UriSchemeHttps;
			}
			return true;
		}
		return false;
	}

	public static string Compose(string url)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(url, "url");
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("[InternetShortcut]");
		stringBuilder.Append("URL=").AppendLine(url.Trim());
		return stringBuilder.ToString();
	}
}
