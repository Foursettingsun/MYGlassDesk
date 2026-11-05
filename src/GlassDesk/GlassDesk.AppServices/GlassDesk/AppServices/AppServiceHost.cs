using System;
using System.Collections.Generic;
using System.Linq;
using GlassDesk.Core.Contracts;
using GlassDesk.Core.Storage;
using GlassDesk.Platform.Shell;

namespace GlassDesk.AppServices;

public sealed class AppServiceHost : IDisposable, IImportObserver
{
	private readonly GlassDeskDataStore _data;

	private readonly IImportObserver? _externalObserver;

	public IconCache Icons { get; }

	public ImportService Import { get; }

	public LaunchService Launch { get; }

	public LibraryMaintenance Maintenance { get; }

	public SearchService Search { get; }

	public AppEntryEditor Editor { get; }

	public DockIconCustomization DockIcons { get; }

	public RunningAppTracker Running { get; }

	public LayoutStore Layouts => _data.Layouts;

	public AppLibraryStore Library => _data.Library;

	public AppPaths Paths => _data.Paths;

	public IImportService ImportContract => Import;

	public ILaunchService LaunchContract => Launch;

	public event EventHandler? LibraryChanged;

	public AppServiceHost(GlassDeskDataStore data, IImportObserver? observer = null)
	{
		ArgumentNullException.ThrowIfNull(data, "data");
		_data = data;
		_externalObserver = observer;
		data.Paths.EnsureCreated();
		Icons = new IconCache(data.Paths);
		Import = new ImportService(data.Library, Icons, this, data.Layouts, () => Math.Clamp(data.Settings.Current.Launcher.Rows, 2, 10) * Math.Clamp(data.Settings.Current.Launcher.Columns, 2, 12));
		Launch = new LaunchService(data.Library);
		Maintenance = new LibraryMaintenance(data.Library, Icons, this);
		Running = new RunningAppTracker(data.Library);
		Search = new SearchService(() => data.Library.Items, () => data.Layouts.Launcher);
		Editor = new AppEntryEditor(data.Library, data.Paths, OnLibraryChanged);
		DockIcons = new DockIconCustomization(data, OnLibraryChanged);
	}

	public void OnLibraryChanged()
	{
		_externalObserver?.OnLibraryChanged();
		LibraryChanged?.Invoke(this, EventArgs.Empty);
	}

	public static StartMenuScanResult ScanStartMenu()
	{
		return StartMenuScanner.Scan();
	}

	public ImportResult ImportStartMenuShortcuts(IEnumerable<StartMenuShortcut> shortcuts)
	{
		ArgumentNullException.ThrowIfNull(shortcuts, "shortcuts");
		List<ImportRequest> requests = shortcuts.Select((StartMenuShortcut shortcut) => new ImportRequest
		{
			Source = shortcut.Path,
			DisplayName = shortcut.Name
		}).ToList();
		return Import.ImportItems(requests);
	}

	public void Dispose()
	{
		Running.Dispose();
	}
}
