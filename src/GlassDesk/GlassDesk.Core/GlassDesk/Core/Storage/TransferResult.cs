using System;
using System.Collections.Generic;

namespace GlassDesk.Core.Storage;

public sealed class TransferResult
{
	public bool Succeeded { get; init; }

	public string Directory { get; init; } = string.Empty;

	public IReadOnlyList<string> Files { get; init; } = Array.Empty<string>();

	public string? SafetyCopyDirectory { get; init; }

	public int RepairedIconReferences { get; init; }

	public string? Error { get; init; }
}
