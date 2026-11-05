using System;
using System.Windows.Threading;
using GlassDesk.Core.Models.Settings;

namespace GlassDesk.UI.Settings;

public sealed class DockSettingsHost
{
	private readonly Dispatcher _dispatcher;

	public static DockSettingsHost? Current { get; private set; }

	public DockSettings Settings { get; private set; }

	public event EventHandler? Changed;

	public DockSettingsHost(Dispatcher dispatcher)
	{
		ArgumentNullException.ThrowIfNull(dispatcher, "dispatcher");
		_dispatcher = dispatcher;
		Settings = new DockSettings();
	}

	public static DockSettingsHost Initialize(Dispatcher dispatcher)
	{
		Current = new DockSettingsHost(dispatcher);
		return Current;
	}

	public void Update(DockSettings settings)
	{
		ArgumentNullException.ThrowIfNull(settings, "settings");
		Settings = settings;
		NotifyChanged();
	}

	public void Refresh()
	{
		NotifyChanged();
	}

	private void NotifyChanged()
	{
		if (!_dispatcher.HasShutdownStarted && !_dispatcher.HasShutdownFinished)
		{
			if (_dispatcher.CheckAccess())
			{
				Changed?.Invoke(this, EventArgs.Empty);
			}
			else
			{
				_dispatcher.BeginInvoke(new Action(NotifyChanged));
			}
		}
	}
}
