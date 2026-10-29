using System;
using System.Linq;
using GlassDesk.Core.Models;

namespace GlassDesk.AppServices;

public sealed class LauncherWorkspace
{
	private readonly AppServiceHost _host;

	private readonly bool _libraryAvailable;

	public LauncherLayout Layout => _host.Layouts.Launcher;

	public LauncherWorkspace(AppServiceHost host, bool libraryAvailable = true)
	{
		_host = host;
		_libraryAvailable = libraryAvailable;
	}

	public bool Reconcile(int capacity)
	{
		if (!_libraryAvailable)
		{
			return false;
		}
		LauncherLayout launcherLayout = LauncherLayoutRules.Reconcile(Layout, from item in _host.Library.Items
			where item?.IsUsable ?? false
			select item.Id, capacity);
		if (LauncherLayoutRules.Equivalent(Layout, launcherLayout))
		{
			return false;
		}
		return _host.Layouts.CommitLauncher(launcherLayout, _host.Layouts.LauncherVersion);
	}

	public LauncherEditSession BeginEdit()
	{
		return new LauncherEditSession(_host.Layouts.LauncherVersion, _host.Library.Version, LauncherLayoutRules.DeepClone(Layout));
	}

	public LauncherEditSession BeginGroupModeEdit(string? groupId, int capacity)
	{
		return new LauncherEditSession(_host.Layouts.LauncherVersion, _host.Library.Version, LauncherFilterRules.Project(Layout, groupId, capacity))
		{
			IsGroupMode = true,
			FilterGroupId = groupId,
			Capacity = capacity
		};
	}

	public bool Commit(LauncherEditSession session)
	{
		ArgumentNullException.ThrowIfNull(session, "session");
		if (_host.Library.Version != session.LibraryVersion || _host.Layouts.LauncherVersion != session.LayoutVersion)
		{
			return false;
		}
		LauncherLayout draft = (session.IsGroupMode ? LauncherFilterRules.ApplyProjection(Layout, session.Draft, session.FilterGroupId, session.Capacity) : session.Draft);
		return _host.Layouts.CommitLauncher(draft, session.LayoutVersion);
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.1.0.9782' (yours is '9.1.0.7988')
