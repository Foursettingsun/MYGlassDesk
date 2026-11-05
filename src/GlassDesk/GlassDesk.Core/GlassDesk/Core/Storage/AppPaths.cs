using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GlassDesk.Core.Storage;

public sealed class AppPaths
{
	public const string ProductFolderName = "GlassDesk";

	public const string IconCacheSubdirectory = "icons";

	public const string ManagedCustomIconPrefix = "custom-";

	public string RootDirectory { get; }

	public string SettingsFile => Path.Combine(RootDirectory, "settings.json");

	public string AppLibraryFile => Path.Combine(RootDirectory, "apps.json");

	public string DockLayoutFile => Path.Combine(RootDirectory, "dock-layout.json");

	public string LauncherLayoutFile => Path.Combine(RootDirectory, "launcher-layout.json");

	public string IconCacheDirectory => Path.Combine(RootDirectory, "icons");

	public string BackupDirectory => Path.Combine(RootDirectory, "backup");

	public string LogDirectory => Path.Combine(RootDirectory, "logs");

	public string RecoveryDirectory => Path.Combine(RootDirectory, "recovery");

	public AppPaths(string rootDirectory)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory, "rootDirectory");
		RootDirectory = Path.GetFullPath(rootDirectory);
	}

	public static bool IsManagedCustomIconPath(string? path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return false;
		}
		return Path.GetFileName(path).StartsWith("custom-", StringComparison.OrdinalIgnoreCase);
	}

	public static string ManagedCustomIconPath(string dataRoot, string fileName)
	{
		return Path.Combine(dataRoot, "icons", fileName);
	}

	public static AppPaths ForCurrentUser()
	{
		return new AppPaths(Path.Combine(AppContext.BaseDirectory, "Data"));
	}

	public void EnsureCreated()
	{
		Directory.CreateDirectory(RootDirectory);
		Directory.CreateDirectory(IconCacheDirectory);
		Directory.CreateDirectory(BackupDirectory);
		Directory.CreateDirectory(LogDirectory);
		Directory.CreateDirectory(RecoveryDirectory);
	}

	public static IReadOnlyList<string> BackupDocumentRelativePaths()
	{
		return new _003C_003Ez__ReadOnlyArray<string>(new string[4] { "settings.json", "apps.json", "dock-layout.json", "launcher-layout.json" });
	}

	public IReadOnlyList<string> CustomIconRelativePaths()
	{
		string iconCacheDirectory = IconCacheDirectory;
		if (!Directory.Exists(iconCacheDirectory))
		{
			return Array.Empty<string>();
		}
		List<string> list = new List<string>();
		list.AddRange(from file in Directory.GetFiles(iconCacheDirectory, "custom-*")
			select Path.GetRelativePath(RootDirectory, file));
		return new _003C_003Ez__ReadOnlyList<string>(list);
	}
}
