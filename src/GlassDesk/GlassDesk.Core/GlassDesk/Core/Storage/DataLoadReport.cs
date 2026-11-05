using System.Collections.Generic;

namespace GlassDesk.Core.Storage;

public sealed class DataLoadReport
{
	public LoadStatus SettingsStatus { get; internal set; }

	public LoadStatus LibraryStatus { get; internal set; }

	public List<string> Notices { get; } = new List<string>();

	public List<string> PreservedFiles { get; } = new List<string>();

	public bool HasNotices => Notices.Count > 0;
}
