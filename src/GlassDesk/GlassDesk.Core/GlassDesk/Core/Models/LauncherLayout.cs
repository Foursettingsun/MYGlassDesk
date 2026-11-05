using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GlassDesk.Core.Models;

public sealed class LauncherLayout
{
	public List<LauncherPage> Pages { get; set; } = new List<LauncherPage>();

	public List<string> FlatOrder { get; set; } = new List<string>();

	public List<LauncherGroup> FilterGroups { get; set; } = new List<LauncherGroup>();

	public int SchemaVersion { get; set; }

	[JsonIgnore]
	public List<string> ExcludedAppIds { get; set; } = new List<string>();

	[JsonPropertyName("excludedAppIds")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<string>? LegacyExcludedAppIds
	{
		get
		{
			return null;
		}
		set
		{
			ExcludedAppIds = value ?? new List<string>();
		}
	}

	public List<int> FlatPageSizes
	{
		get
		{
			return field;
		}
		set
		{
			field = value;
		}
	}
}
