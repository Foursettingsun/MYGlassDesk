using System;
using System.Windows.Threading;
using GlassDesk.Core.Models;
using GlassDesk.Core.Models.Settings;
using GlassDesk.Platform;
using Microsoft.Win32;

namespace GlassDesk.UI.Theming;

public sealed class ThemeService : IDisposable
{
	private readonly Dispatcher _dispatcher;

	private bool _disposed;

	public static ThemeService? Current { get; private set; }

	public AppearanceSettings Appearance { get; private set; }

	public bool IsDark { get; private set; }

	public GlassPalette Palette { get; private set; }

	public bool UseOpaquePanels { get; private set; }

	public bool AnimationsEnabled => !Appearance.ReduceMotion;

	public event EventHandler? Changed;

	public ThemeService(Dispatcher dispatcher)
	{
		ArgumentNullException.ThrowIfNull(dispatcher, "dispatcher");
		_dispatcher = dispatcher;
		Appearance = new AppearanceSettings();
		Palette = ResolvePalette(Appearance);
		SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
	}

	public static ThemeService Initialize(Dispatcher dispatcher)
	{
		Current?.Dispose();
		Current = new ThemeService(dispatcher);
		return Current;
	}

	public void Update(AppearanceSettings appearance)
	{
		ArgumentNullException.ThrowIfNull(appearance, "appearance");
		Appearance = appearance;
		Refresh();
	}

	public void Refresh()
	{
		IsDark = Appearance.Theme switch
		{
			ThemeMode.Dark => true, 
			ThemeMode.Light => false, 
			_ => !SystemThemeReader.IsLightTheme(), 
		};
		UseOpaquePanels = Appearance.ReduceTransparency || !SystemThemeReader.IsTransparencyEnabled();
		Palette = GlassPalette.Create(IsDark, GlassPalette.ParseAccent(Appearance.AccentColor), UseOpaquePanels);
		Changed?.Invoke(this, EventArgs.Empty);
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
			_disposed = true;
			if (Current == this)
			{
				Current = null;
			}
		}
	}

	private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
	{
		UserPreferenceCategory category = e.Category;
		bool flag = ((category == UserPreferenceCategory.Color || category == UserPreferenceCategory.General || category == UserPreferenceCategory.VisualStyle) ? true : false);
		if (!flag || _dispatcher.HasShutdownStarted || _dispatcher.HasShutdownFinished)
		{
			return;
		}
		_dispatcher.BeginInvoke((Action)(() =>
		{
			if (!_disposed)
			{
				GlassPalette glassPalette = ResolvePalette(Appearance);
				if (Palette.IsDark != glassPalette.IsDark || !(Palette.Accent == glassPalette.Accent) || !(Palette.FallbackPanelBackground == glassPalette.FallbackPanelBackground))
				{
					Refresh();
				}
			}
		}));
	}

	private static GlassPalette ResolvePalette(AppearanceSettings appearance)
	{
		return GlassPalette.Create(appearance.Theme switch
		{
			ThemeMode.Dark => true, 
			ThemeMode.Light => false, 
			_ => !SystemThemeReader.IsLightTheme(), 
		}, opaquePanels: appearance.ReduceTransparency || !SystemThemeReader.IsTransparencyEnabled(), accent: GlassPalette.ParseAccent(appearance.AccentColor));
	}

	public static bool SystemPrefersDark()
	{
		return !SystemThemeReader.IsLightTheme();
	}

	public string Describe()
	{
		return $"主题={Appearance.Theme}（实际 {(IsDark ? "深色" : "浅色")}），不透明面板={(UseOpaquePanels ? "是" : "否")}，动画={(AnimationsEnabled ? "开" : "关")}，{WindowsCapabilities.Describe()}";
	}
}
