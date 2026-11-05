using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using GlassDesk.Core.Models;
using GlassDesk.Core.Serialization;

namespace GlassDesk.Core.Storage;

public sealed class BackupService
{
	private const string ManifestFileName = "manifest.json";

	private readonly AppPaths _paths;

	private readonly TimeProvider _timeProvider;

	public BackupService(AppPaths paths, TimeProvider? timeProvider = null)
	{
		_paths = paths;
		_timeProvider = timeProvider ?? TimeProvider.System;
	}

	public TransferResult BackupTo(string destinationDirectory)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory, "destinationDirectory");
		try
		{
			Directory.CreateDirectory(destinationDirectory);
			List<string> list = new List<string>();
			foreach (string item in SourcesToArchive())
			{
				CopyPreservingRelativePath(Path.Combine(_paths.RootDirectory, item), destinationDirectory, item);
				list.Add(item);
			}
			BackupManifest value = new BackupManifest
			{
				ProductVersion = typeof(BackupService).Assembly.GetName().Version?.ToString(),
				CreatedAtUtc = _timeProvider.GetUtcNow(),
				Files = list
			};
			File.WriteAllText(Path.Combine(destinationDirectory, "manifest.json"), JsonSerializer.Serialize(value, GlassDeskJsonContext.Default.BackupManifest));
			return new TransferResult
			{
				Succeeded = true,
				Directory = destinationDirectory,
				Files = list
			};
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
			return new TransferResult
			{
				Succeeded = false,
				Directory = destinationDirectory,
				Error = "备份失败：" + ex.Message
			};
		}
	}

	public TransferResult RestoreFrom(string sourceDirectory)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(sourceDirectory, "sourceDirectory");
		if (!Directory.Exists(sourceDirectory))
		{
			return new TransferResult
			{
				Succeeded = false,
				Directory = sourceDirectory,
				Error = "所选文件夹不存在。"
			};
		}
		string path = Path.Combine(sourceDirectory, "manifest.json");
		if (!File.Exists(path))
		{
			return new TransferResult
			{
				Succeeded = false,
				Directory = sourceDirectory,
				Error = "所选文件夹中没有 GlassDesk 备份清单，无法确认来源。"
			};
		}
		BackupManifest backupManifest;
		try
		{
			backupManifest = JsonSerializer.Deserialize(File.ReadAllText(path), GlassDeskJsonContext.Default.BackupManifest);
		}
		catch (Exception ex) when ((ex is JsonException || ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
			return new TransferResult
			{
				Succeeded = false,
				Directory = sourceDirectory,
				Error = "备份清单无法读取：" + ex.Message
			};
		}
		if (backupManifest == null || !string.Equals(backupManifest.Product, "GlassDesk", StringComparison.Ordinal))
		{
			return new TransferResult
			{
				Succeeded = false,
				Directory = sourceDirectory,
				Error = "该备份不属于本产品。"
			};
		}
		if (backupManifest.SchemaVersion > 1)
		{
			return new TransferResult
			{
				Succeeded = false,
				Directory = sourceDirectory,
				Error = "该备份来自更新的版本，请先升级后再恢复。"
			};
		}
		List<string> list = FindUnreadableDocuments(sourceDirectory);
		if (list.Count > 0)
		{
			return new TransferResult
			{
				Succeeded = false,
				Directory = sourceDirectory,
				Error = "备份中的以下文件无法读取，已放弃恢复（现有数据未改动）：" + string.Join('、', list)
			};
		}
		string safetyCopyDirectory = SnapshotCurrent();
		try
		{
			_paths.EnsureCreated();
			List<string> list2 = new List<string>();
			foreach (string item in RestoreCandidates(sourceDirectory))
			{
				CopyPreservingRelativePath(Path.Combine(sourceDirectory, item), _paths.RootDirectory, item);
				list2.Add(item);
			}
			int repairedIconReferences = RepairManagedIconReferences();
			return new TransferResult
			{
				Succeeded = true,
				Directory = sourceDirectory,
				Files = list2,
				SafetyCopyDirectory = safetyCopyDirectory,
				RepairedIconReferences = repairedIconReferences
			};
		}
		catch (Exception ex2) when ((ex2 is IOException || ex2 is UnauthorizedAccessException) ? true : false)
		{
			return new TransferResult
			{
				Succeeded = false,
				Directory = sourceDirectory,
				SafetyCopyDirectory = safetyCopyDirectory,
				Error = "恢复失败：" + ex2.Message
			};
		}
	}

	private static List<string> FindUnreadableDocuments(string sourceDirectory)
	{
		Dictionary<string, Func<string, bool>> dictionary = new Dictionary<string, Func<string, bool>>(StringComparer.OrdinalIgnoreCase)
		{
			["settings.json"] = (string text) => CanDeserialize(text, GlassDeskJsonContext.Default.GlassDeskSettings),
			["apps.json"] = (string text) => CanDeserialize(text, GlassDeskJsonContext.Default.AppLibraryData),
			["dock-layout.json"] = (string text) => CanDeserialize(text, GlassDeskJsonContext.Default.DockLayout),
			["launcher-layout.json"] = (string text) => CanDeserialize(text, GlassDeskJsonContext.Default.LauncherLayout)
		};
		List<string> list = new List<string>();
		foreach (string item in AppPaths.BackupDocumentRelativePaths())
		{
			string path = Path.Combine(sourceDirectory, item);
			if (File.Exists(path) && (!dictionary.TryGetValue(item, out var value) || !CanRead(path, value)))
			{
				list.Add(item);
			}
		}
		return list;
	}

	private static bool CanRead(string path, Func<string, bool> canDeserialize)
	{
		try
		{
			return canDeserialize(File.ReadAllText(path));
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
			return false;
		}
	}

	private static bool CanDeserialize<T>(string text, JsonTypeInfo<T> typeInfo) where T : class
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		try
		{
			return JsonSerializer.Deserialize(text, typeInfo) != null;
		}
		catch (JsonException)
		{
			return false;
		}
		catch (NotSupportedException)
		{
			return false;
		}
	}

	private string? SnapshotCurrent()
	{
		try
		{
			string text = _timeProvider.GetUtcNow().UtcDateTime.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
			string text2 = Path.Combine(_paths.BackupDirectory, "pre-restore-" + text);
			Directory.CreateDirectory(text2);
			foreach (string item in SourcesToArchive())
			{
				CopyPreservingRelativePath(Path.Combine(_paths.RootDirectory, item), text2, item);
			}
			return text2;
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
			return null;
		}
	}

	private int RepairManagedIconReferences()
	{
		try
		{
			AppLibraryStore appLibraryStore = new AppLibraryStore(_paths.AppLibraryFile);
			appLibraryStore.Load();
			if (appLibraryStore.RepairedIconReferenceCount == 0)
			{
				return 0;
			}
			int repairedIconReferenceCount = appLibraryStore.RepairedIconReferenceCount;
			appLibraryStore.Save();
			return repairedIconReferenceCount;
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException || ex is StorageException) ? true : false)
		{
			return 0;
		}
	}

	private IEnumerable<string> SourcesToArchive()
	{
		foreach (string item in AppPaths.BackupDocumentRelativePaths())
		{
			if (File.Exists(Path.Combine(_paths.RootDirectory, item)))
			{
				yield return item;
			}
		}
		foreach (string item2 in _paths.CustomIconRelativePaths())
		{
			yield return item2;
		}
	}

	private static IEnumerable<string> RestoreCandidates(string sourceDirectory)
	{
		foreach (string item in AppPaths.BackupDocumentRelativePaths())
		{
			if (File.Exists(Path.Combine(sourceDirectory, item)))
			{
				yield return item;
			}
		}
		foreach (string item2 in CustomIconsInArchive(sourceDirectory))
		{
			yield return item2;
		}
	}

	private static IReadOnlyList<string> CustomIconsInArchive(string sourceDirectory)
	{
		string path = Path.Combine(sourceDirectory, "icons");
		if (!Directory.Exists(path))
		{
			return Array.Empty<string>();
		}
		List<string> list = new List<string>();
		list.AddRange(from file in Directory.GetFiles(path, "custom-*")
			select Path.Combine("icons", Path.GetFileName(file)));
		return new _003C_003Ez__ReadOnlyList<string>(list);
	}

	private static void CopyPreservingRelativePath(string source, string destinationDirectory, string relativePath)
	{
		string text = Path.Combine(destinationDirectory, relativePath);
		string directoryName = Path.GetDirectoryName(text);
		if (!string.IsNullOrEmpty(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		File.Copy(source, text, overwrite: true);
	}
}
