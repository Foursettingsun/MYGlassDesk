using GlassDesk.Core.Models;

namespace GlassDesk.Core.Contracts;

public interface ILayoutService
{
	void UpdateLayout(DockLayout dock, LauncherLayout launcher);
}
