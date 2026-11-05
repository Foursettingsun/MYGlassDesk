using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GlassDesk.Core.Models;
using GlassDesk.Platform.Shell;

namespace GlassDesk.AppServices;

public static class WindowMatcher
{
	public static IReadOnlyList<WindowIdentityInfo> MatchWindows(AppEntry entry, IReadOnlyList<WindowIdentityInfo> snapshot, string? aumid)
	{
		ArgumentNullException.ThrowIfNull(entry, "entry");
		ArgumentNullException.ThrowIfNull(snapshot, "snapshot");
		if (!string.IsNullOrWhiteSpace(aumid))
		{
			return MatchByIdentity(snapshot, aumid);
		}
		if (CanMatchByImage(entry))
		{
			string target = NormalizePath(entry.Target);
			return snapshot.Where((WindowIdentityInfo window) => string.Equals(window.ImagePath, target, StringComparison.OrdinalIgnoreCase)).ToList();
		}
		return Array.Empty<WindowIdentityInfo>();
	}

	public static IReadOnlyList<WindowIdentityInfo> FindWindows(AppEntry entry, IDictionary<string, string?>? identityCache = null)
	{
		ArgumentNullException.ThrowIfNull(entry, "entry");
		return MatchWindows(entry, WindowLocator.EnumerateTopLevelWindows(), ResolveAumid(entry, identityCache));
	}

	public static string? ResolveAumid(AppEntry entry, IDictionary<string, string?>? identityCache = null)
	{
		ArgumentNullException.ThrowIfNull(entry, "entry");
		if (!string.IsNullOrWhiteSpace(entry.AppUserModelId))
		{
			return entry.AppUserModelId;
		}
		if (identityCache != null && identityCache.TryGetValue(entry.Id, out string value))
		{
			return value;
		}
		string text = ((!entry.Target.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)) ? LaunchTargetIdentity.ReadManagedShellLauncher(entry.Target) : ShortcutResolver.Resolve(entry.Target)?.Aumid);
		if (identityCache != null)
		{
			identityCache[entry.Id] = text;
		}
		return text;
	}

	private static List<WindowIdentityInfo> MatchByIdentity(IReadOnlyList<WindowIdentityInfo> windows, string aumid)
	{
		List<WindowIdentityInfo> list = new List<WindowIdentityInfo>();
		foreach (WindowIdentityInfo window in windows)
		{
			if (string.Equals(window.EffectiveAumid, aumid, StringComparison.OrdinalIgnoreCase))
			{
				list.Add(window);
			}
		}
		return list;
	}

	public static bool CanMatchByImage(AppEntry entry)
	{
		return entry.Target.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
	}

	private static string NormalizePath(string path)
	{
		string text = path.Trim();
		try
		{
			return Path.GetFullPath(text);
		}
		catch (Exception ex) when ((ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException) ? true : false)
		{
			return text;
		}
	}
}
