using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace GlassDesk.Core.Storage;

public sealed class JsonFileStore<T> where T : class
{
	private readonly JsonTypeInfo<T> _typeInfo;

	private readonly TimeProvider _timeProvider;

	public string Path { get; }

	public JsonFileStore(string path, JsonTypeInfo<T> typeInfo, TimeProvider? timeProvider = null)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path, "path");
		ArgumentNullException.ThrowIfNull(typeInfo, "typeInfo");
		Path = path;
		_typeInfo = typeInfo;
		_timeProvider = timeProvider ?? TimeProvider.System;
	}

	public LoadOutcome<T> Load(Func<T> createDefault)
	{
		ArgumentNullException.ThrowIfNull(createDefault, "createDefault");
		string text = TryRead(Path);
		if (text != null)
		{
			if (TryDeserialize(text, out T value))
			{
				return new LoadOutcome<T>(value, LoadStatus.Loaded, null, null);
			}
			string preservedFile = AtomicFile.PreserveCorrupt(Path, _timeProvider.GetUtcNow());
			return RecoverOrFail(createDefault, preservedFile);
		}
		if (File.Exists(Path + ".bak"))
		{
			return RecoverOrFail(createDefault, null);
		}
		return new LoadOutcome<T>(createDefault(), LoadStatus.FirstRun, null, null);
	}

	public void Save(T value)
	{
		ArgumentNullException.ThrowIfNull(value, "value");
		string contents = JsonSerializer.Serialize(value, _typeInfo);
		AtomicFile.WriteAllText(Path, contents);
	}

	private LoadOutcome<T> RecoverOrFail(Func<T> createDefault, string? preservedFile)
	{
		string text = TryRead(Path + ".bak");
		if (text != null && TryDeserialize(text, out T value))
		{
			return new LoadOutcome<T>(value, LoadStatus.RecoveredFromBackup, preservedFile, null);
		}
		if (preservedFile == null && !File.Exists(Path))
		{
			return new LoadOutcome<T>(createDefault(), LoadStatus.FirstRun, null, null);
		}
		return new LoadOutcome<T>(createDefault(), LoadStatus.Unrecoverable, preservedFile, new JsonException("配置文件与其备份均无法解析。"));
	}

	private static string? TryRead(string path)
	{
		try
		{
			return File.Exists(path) ? File.ReadAllText(path) : null;
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
			return null;
		}
	}

	private bool TryDeserialize(string raw, out T? value)
	{
		value = null;
		if (string.IsNullOrWhiteSpace(raw))
		{
			return false;
		}
		try
		{
			value = JsonSerializer.Deserialize(raw, _typeInfo);
			return value != null;
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
}
