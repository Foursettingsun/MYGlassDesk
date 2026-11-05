using System.Collections.Generic;

namespace GlassDesk.Core.Models;

public sealed class LauncherGroup
{
	public string Id { get; set; } = string.Empty;

	public string Name { get; set; } = string.Empty;

	public List<string> ItemIds { get; set; } = new List<string>();

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
