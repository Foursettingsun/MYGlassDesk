using System;
using System.Collections.Generic;

namespace GlassDesk.Core.Models;

public sealed class BackupManifest
{
	public string Product { get; set; } = "GlassDesk";

	public int SchemaVersion { get; set; } = 1;

	public string? ProductVersion { get; set; }

	public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

	public List<string> Files { get; set; } = new List<string>();
}
