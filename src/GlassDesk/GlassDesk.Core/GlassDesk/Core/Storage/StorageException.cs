using System;

namespace GlassDesk.Core.Storage;

public sealed class StorageException : Exception
{
	public string Path { get; }

	public StorageException(string message, string path, Exception? innerException = null)
		: base(message, innerException)
	{
		Path = path;
	}
}
