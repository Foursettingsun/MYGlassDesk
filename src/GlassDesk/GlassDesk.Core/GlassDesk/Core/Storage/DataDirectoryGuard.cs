using System;
using System.IO;

namespace GlassDesk.Core.Storage;

public static class DataDirectoryGuard
{
	public static bool Overlaps(string candidateDirectory, string protectedDirectory)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(candidateDirectory, "candidateDirectory");
		ArgumentException.ThrowIfNullOrWhiteSpace(protectedDirectory, "protectedDirectory");
		string text = Normalize(candidateDirectory);
		string text2 = Normalize(protectedDirectory);
		StringComparison stringComparison = (OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
		if (!string.Equals(text, text2, stringComparison) && !IsWithin(text, text2, stringComparison))
		{
			return IsWithin(text2, text, stringComparison);
		}
		return true;
	}

	private static string Normalize(string path)
	{
		string fullPath = Path.GetFullPath(path);
		string pathRoot = Path.GetPathRoot(fullPath);
		if (fullPath.Length <= (pathRoot?.Length ?? 0))
		{
			return fullPath;
		}
		return Path.TrimEndingDirectorySeparator(fullPath);
	}

	private static bool IsWithin(string candidate, string parent, StringComparison comparison)
	{
		string value = (Path.EndsInDirectorySeparator(parent) ? parent : (parent + Path.DirectorySeparatorChar));
		return candidate.StartsWith(value, comparison);
	}
}
