using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace GlassDesk.Core.Storage;

public static class AtomicFile
{
	public const string BackupSuffix = ".bak";

	public const string TempSuffix = ".tmp";

	public static void WriteAllText(string path, string contents)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path, "path");
		ArgumentNullException.ThrowIfNull(contents, "contents");
		string directoryName = Path.GetDirectoryName(Path.GetFullPath(path));
		if (!string.IsNullOrEmpty(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		string text = path + ".tmp";
		string destinationBackupFileName = path + ".bak";
		try
		{
			using (FileStream fileStream = new FileStream(text, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				using StreamWriter streamWriter = new StreamWriter(fileStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
				streamWriter.Write(contents);
				streamWriter.Flush();
				fileStream.Flush(flushToDisk: true);
			}
			if (File.Exists(path))
			{
				File.Replace(text, path, destinationBackupFileName, ignoreMetadataErrors: true);
			}
			else
			{
				File.Move(text, path);
			}
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException || ex is NotSupportedException) ? true : false)
		{
			TryDelete(text);
			throw new StorageException("无法保存 " + Path.GetFileName(path) + "：" + ex.Message, path, ex);
		}
	}

	public static string? ReadAllTextOrNull(string path)
	{
		if (!File.Exists(path))
		{
			return null;
		}
		try
		{
			return File.ReadAllText(path, Encoding.UTF8);
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
			throw new StorageException("无法读取 " + Path.GetFileName(path) + "：" + ex.Message, path, ex);
		}
	}

	public static string? PreserveCorrupt(string path, DateTimeOffset timestamp)
	{
		if (!File.Exists(path))
		{
			return null;
		}
		string path2 = Path.GetDirectoryName(path) ?? ".";
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
		string extension = Path.GetExtension(path);
		string text = timestamp.UtcDateTime.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
		string text2 = Path.Combine(path2, fileNameWithoutExtension + ".corrupt-" + text + extension);
		try
		{
			string text3 = text2;
			int num = 1;
			while (File.Exists(text3))
			{
				text3 = Path.Combine(path2, $"{fileNameWithoutExtension}.corrupt-{text}-{num}{extension}");
				num++;
			}
			File.Move(path, text3);
			return text3;
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
			return null;
		}
	}

	private static void TryDelete(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
		}
	}
}
