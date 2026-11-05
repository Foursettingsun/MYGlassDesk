using System.Collections.Generic;

namespace GlassDesk.Core.Models;

public sealed class AppLibraryData
{
	public int SchemaVersion { get; set; } = 1;

	public List<AppEntry> Items { get; set; } = new List<AppEntry>();
}
