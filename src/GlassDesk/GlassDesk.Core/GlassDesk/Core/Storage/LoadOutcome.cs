using System;

namespace GlassDesk.Core.Storage;

public sealed class LoadOutcome<T> where T : class
{
	public T Value { get; }

	public LoadStatus Status { get; }

	public string? PreservedFile { get; }

	public Exception? Error { get; }

	public bool NeedsUserAttention
	{
		get
		{
			LoadStatus status = Status;
			if ((uint)(status - 2) <= 1u)
			{
				return true;
			}
			return false;
		}
	}

	internal LoadOutcome(T value, LoadStatus status, string? preservedFile, Exception? error)
	{
		Value = value;
		Status = status;
		PreservedFile = preservedFile;
		Error = error;
	}
}
