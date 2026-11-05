using System;
using System.IO;
using System.Security;
using Microsoft.Win32;

namespace GlassDesk.Platform;

public static class SystemThemeReader
{
	private const string PersonalizeKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";

	public static bool IsLightTheme()
	{
		int? num = ReadPersonalizeDword("AppsUseLightTheme");
		if (!num.HasValue || num == 1)
		{
			return true;
		}
		return false;
	}

	public static bool IsTransparencyEnabled()
	{
		int? num = ReadPersonalizeDword("EnableTransparency");
		if (!num.HasValue || num == 1)
		{
			return true;
		}
		return false;
	}

	private static int? ReadPersonalizeDword(string name)
	{
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", writable: false);
			object obj = registryKey?.GetValue(name);
			int? result;
			if (obj is int value)
			{
				result = value;
			}
			else
			{
				result = ((!(obj is long num)) ? ((int?)null) : new int?((int)num));
			}
			return result;
		}
		catch (Exception ex) when ((ex is SecurityException || ex is UnauthorizedAccessException || ex is IOException) ? true : false)
		{
			return null;
		}
	}
}
