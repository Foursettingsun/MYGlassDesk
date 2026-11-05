using System;
using System.Collections.Generic;
using GlassDesk.Core.Models;

namespace GlassDesk.Core.Contracts;

public sealed class ImportResult
{
	public IReadOnlyList<AppEntry> Added { get; init; } = Array.Empty<AppEntry>();

	public IReadOnlyList<AppEntry> Duplicates { get; init; } = Array.Empty<AppEntry>();

	public IReadOnlyList<ImportFailure> Failures { get; init; } = Array.Empty<ImportFailure>();
}
