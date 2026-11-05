using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using GlassDesk.Core.Models;
using GlassDesk.Core.Models.Settings;
using GlassDesk.Platform;
using GlassDesk.Platform.Displays;
using GlassDesk.UI.Theming;

namespace GlassDesk.UI.Controls;

public abstract class GlassWindow : Window
{
	private CaptureExclusionSession? _captureExclusion;

	private GlassShadow? _shadow;

	private PrimaryDisplaySession? _primaryDisplaySession;

	private const double DefaultChromeInset = 0.0;

	public static readonly DependencyProperty PaletteProperty = DependencyProperty.Register("Palette", typeof(GlassPalette), typeof(GlassWindow), new PropertyMetadata(null));

	public static readonly DependencyProperty PanelCornerRadiusProperty = DependencyProperty.Register("PanelCornerRadius", typeof(CornerRadius), typeof(GlassWindow), new PropertyMetadata(new CornerRadius(36.0)));

	public static readonly DependencyProperty ChromeInsetProperty = DependencyProperty.Register("ChromeInset", typeof(Thickness), typeof(GlassWindow), new PropertyMetadata(new Thickness(0.0)));

	public static readonly DependencyProperty PanelBackgroundBrushProperty = DependencyProperty.Register("PanelBackgroundBrush", typeof(Brush), typeof(GlassWindow), new PropertyMetadata(null));

	public static readonly DependencyProperty PanelBorderBrushProperty = DependencyProperty.Register("PanelBorderBrush", typeof(Brush), typeof(GlassWindow), new PropertyMetadata(null));

	public static readonly DependencyProperty ForegroundBrushProperty = DependencyProperty.Register("ForegroundBrush", typeof(Brush), typeof(GlassWindow), new PropertyMetadata(null));

	public static readonly DependencyProperty DockBuiltinIconBrushProperty = DependencyProperty.Register("DockBuiltinIconBrush", typeof(Brush), typeof(GlassWindow), new PropertyMetadata(null));

	public static readonly DependencyProperty LaunchpadIconFilledProperty = DependencyProperty.Register("LaunchpadIconFilled", typeof(bool), typeof(GlassWindow), new PropertyMetadata(false));

	public static readonly DependencyProperty SecondaryForegroundBrushProperty = DependencyProperty.Register("SecondaryForegroundBrush", typeof(Brush), typeof(GlassWindow), new PropertyMetadata(null));

	public static readonly DependencyProperty AccentBrushProperty = DependencyProperty.Register("AccentBrush", typeof(Brush), typeof(GlassWindow), new PropertyMetadata(null));

	private Geometry? _sharedPanelClip;

	public GlassPalette? Palette
	{
		get
		{
			return (GlassPalette)GetValue(PaletteProperty);
		}
		private set
		{
			SetValue(PaletteProperty, value);
		}
	}

	public CornerRadius PanelCornerRadius
	{
		get
		{
			return (CornerRadius)GetValue(PanelCornerRadiusProperty);
		}
		protected set
		{
			SetValue(PanelCornerRadiusProperty, value);
		}
	}

	protected virtual bool UsesSquirclePanelGeometryCore => true;

	protected virtual bool UseOpaquePanel => false;

	protected virtual bool UseLauncherTextBrightness => false;

	protected virtual bool UseStatusBarTextBrightness => false;

	protected virtual bool SupportsDesktopLayer => false;

	protected bool IsFixedDesktopLayer
	{
		get
		{
			if (SupportsDesktopLayer)
			{
				return ThemeService.Current?.Appearance.PinBarsToDesktop ?? false;
			}
			return false;
		}
	}

	internal bool UsesDesktopOnlyBackdrop => IsFixedDesktopLayer;

	internal bool UsesSquirclePanelGeometry => UsesSquirclePanelGeometryCore;

	public Thickness ChromeInset
	{
		get
		{
			return (Thickness)GetValue(ChromeInsetProperty);
		}
		protected set
		{
			SetValue(ChromeInsetProperty, value);
		}
	}

	public Brush? PanelBackgroundBrush
	{
		get
		{
			return (Brush)GetValue(PanelBackgroundBrushProperty);
		}
		private set
		{
			SetValue(PanelBackgroundBrushProperty, value);
		}
	}

	public Brush? PanelBorderBrush
	{
		get
		{
			return (Brush)GetValue(PanelBorderBrushProperty);
		}
		private set
		{
			SetValue(PanelBorderBrushProperty, value);
		}
	}

	public Brush? ForegroundBrush
	{
		get
		{
			return (Brush)GetValue(ForegroundBrushProperty);
		}
		private set
		{
			SetValue(ForegroundBrushProperty, value);
		}
	}

	public Brush? DockBuiltinIconBrush
	{
		get
		{
			return (Brush)GetValue(DockBuiltinIconBrushProperty);
		}
		private set
		{
			SetValue(DockBuiltinIconBrushProperty, value);
		}
	}

	public bool LaunchpadIconFilled
	{
		get
		{
			return (bool)GetValue(LaunchpadIconFilledProperty);
		}
		private set
		{
			SetValue(LaunchpadIconFilledProperty, value);
		}
	}

	public Brush? SecondaryForegroundBrush
	{
		get
		{
			return (Brush)GetValue(SecondaryForegroundBrushProperty);
		}
		private set
		{
			SetValue(SecondaryForegroundBrushProperty, value);
		}
	}

	public Brush? AccentBrush
	{
		get
		{
			return (Brush)GetValue(AccentBrushProperty);
		}
		private set
		{
			SetValue(AccentBrushProperty, value);
		}
	}

	public GlassSurfaceMode SurfaceMode { get; private set; } = GlassSurfaceMode.SelfDrawn;

	public PrimaryDisplaySession? PrimaryDisplaySession
	{
		get
		{
			PrimaryDisplaySession? primaryDisplaySession = _primaryDisplaySession;
			if (primaryDisplaySession == null)
			{
				GlassWindow obj = Owner as GlassWindow;
				if (obj == null)
				{
					return null;
				}
				primaryDisplaySession = obj.PrimaryDisplaySession;
			}
			return primaryDisplaySession;
		}
	}

	protected nint Handle => new WindowInteropHelper(this).Handle;

	protected GlassWindow()
	{
		WindowStyle = WindowStyle.None;
		AllowsTransparency = true;
		Background = Brushes.Transparent;
		UseLayoutRounding = true;
		SnapsToDevicePixels = true;
		DpiChanged += (object _, DpiChangedEventArgs e) =>
		{
			if (e.OriginalSource == this && !e.OldDpi.Equals(e.NewDpi))
			{
				BackdropRecovery.RetryBackground("WindowDpiChanged");
			}
		};
		ResizeMode = ResizeMode.NoResize;
	}

	protected virtual Geometry CreatePanelGeometryCore(Rect bounds, double radius)
	{
		return SquircleGlassGeometry.Create(bounds, radius);
	}

	protected virtual bool ContainsPanelGeometryCore(Rect bounds, double radius, Point point)
	{
		return SquircleGlassGeometry.Contains(bounds, radius, point);
	}

	protected virtual bool TryGetPanelGeometryEdgeCore(Rect bounds, double radius, Point point, out double depth, out Vector inwardNormal)
	{
		return SquircleGlassGeometry.TryGetEdge(bounds, radius, point, out depth, out inwardNormal);
	}

	internal Geometry CreateGlassPanelGeometry(Rect bounds, double radius)
	{
		return CreatePanelGeometryCore(bounds, radius);
	}

	internal bool TryGetGlassPanelEdge(Rect bounds, double radius, Point point, out double depth, out Vector inwardNormal)
	{
		return TryGetPanelGeometryEdgeCore(bounds, radius, point, out depth, out inwardNormal);
	}

	protected void SetTransitionShadowShape(Rect? panelBounds, double radius, double strength = 1.0)
	{
		_shadow?.SetTransitionShape(panelBounds, radius, strength);
	}

	protected void SetTransitionShadowShape(Rect? panelBounds, double radius, double strength, bool reducedDetail)
	{
		_shadow?.SetTransitionShape(panelBounds, radius, strength, reducedDetail);
	}

	protected void SetSharedPanelClip(Rect bounds, double radius)
	{
		SetSharedPanelGeometry(CreateGlassPanelGeometry(bounds, radius));
	}

	protected void SetSharedPanelGeometry(Geometry geometry)
	{
		_sharedPanelClip = geometry;
		Clip = _sharedPanelClip;
	}

	public void BindPrimaryDisplay(PrimaryDisplaySession session)
	{
		ArgumentNullException.ThrowIfNull(session, "session");
		if (IsLoaded || PresentationSource.FromVisual(this) != null)
		{
			throw new InvalidOperationException("Bind the Primary Monitor before creating the window handle.");
		}
		if (_primaryDisplaySession != null && _primaryDisplaySession != session)
		{
			throw new InvalidOperationException("A GlassDesk window cannot be rebound to another display session.");
		}
		_primaryDisplaySession = session;
	}

	protected PixelRect PlaceWindowWithinPrimaryWorkArea(PixelRect requested)
	{
		PrimaryDisplaySession primaryDisplaySession = PrimaryDisplaySession;
		if (primaryDisplaySession == null || !primaryDisplaySession.IsAvailable)
		{
			return new PixelRect(0, 0, 0, 0);
		}
		DisplayInfo display = primaryDisplaySession.Display;
		PixelRect pixelRect = primaryDisplaySession.ClampWindowToWorkArea(requested);
		double num = 96.0 / (double)Math.Max(1u, display.Dpi);
		Width = (double)pixelRect.Width * num;
		Height = (double)pixelRect.Height * num;
		if (Handle != 0)
		{
			DisplayService.MoveWindowToPixelRect(Handle, pixelRect);
		}
		return pixelRect;
	}

	protected PixelRect CenterOnPrimaryWorkArea()
	{
		PrimaryDisplaySession primaryDisplaySession = PrimaryDisplaySession;
		if (primaryDisplaySession == null || !primaryDisplaySession.IsAvailable)
		{
			return new PixelRect(0, 0, 0, 0);
		}
		DisplayInfo display = primaryDisplaySession.Display;
		double num = (double.IsFinite(Width) ? Width : ActualWidth);
		double num2 = (double.IsFinite(Height) ? Height : ActualHeight);
		if (num <= 0.0 || num2 <= 0.0)
		{
			return new PixelRect(0, 0, 0, 0);
		}
		int num3 = Math.Max(1, display.ToPixels(num));
		int num4 = Math.Max(1, display.ToPixels(num2));
		PixelRect workArea = display.WorkArea;
		PixelRect requested = new PixelRect(workArea.X + (workArea.Width - num3) / 2, workArea.Y + (workArea.Height - num4) / 2, num3, num4);
		PixelRect pixelRect = primaryDisplaySession.ClampWindowToWorkArea(requested);
		WindowStartupLocation = WindowStartupLocation.Manual;
		Width = display.ToDips(pixelRect.Width);
		Height = display.ToDips(pixelRect.Height);
		DisplayService.MoveWindowToPixelRect((Handle != 0) ? Handle : new WindowInteropHelper(this).EnsureHandle(), pixelRect);
		return pixelRect;
	}

	public void ConstrainCurrentBoundsToPrimary()
	{
		PrimaryDisplaySession primaryDisplaySession = PrimaryDisplaySession;
		if (primaryDisplaySession == null || !primaryDisplaySession.IsAvailable || Handle == 0)
		{
			return;
		}
		PixelRect? windowPixelBounds = DisplayService.GetWindowPixelBounds(Handle);
		if (!windowPixelBounds.HasValue)
		{
			return;
		}
		PixelRect valueOrDefault = windowPixelBounds.GetValueOrDefault();
		if (valueOrDefault.Width > 0 && valueOrDefault.Height > 0)
		{
			PixelRect pixelRect = primaryDisplaySession.ClampWindowToWorkArea(valueOrDefault);
			if (!(pixelRect == valueOrDefault))
			{
				double num = 96.0 / (double)Math.Max(1u, primaryDisplaySession.Display.Dpi);
				Width = (double)pixelRect.Width * num;
				Height = (double)pixelRect.Height * num;
				DisplayService.MoveWindowToPixelRect(Handle, pixelRect);
			}
		}
	}

	protected virtual double ResolveCornerRadius(AppearanceSettings appearance)
	{
		ArgumentNullException.ThrowIfNull(appearance, "appearance");
		return Math.Max(40.0, appearance.DockCornerRadius);
	}

	protected override void OnSourceInitialized(EventArgs e)
	{
		base.OnSourceInitialized(e);
		PrimaryDisplaySession primaryDisplaySession = PrimaryDisplaySession;
		if (primaryDisplaySession != null)
		{
			if (!primaryDisplaySession.IsAvailable)
			{
				Hide();
				return;
			}
			PixelRect? windowPixelBounds = DisplayService.GetWindowPixelBounds(Handle);
			if (windowPixelBounds.HasValue)
			{
				PixelRect valueOrDefault = windowPixelBounds.GetValueOrDefault();
				if (valueOrDefault.Width > 0 && valueOrDefault.Height > 0)
				{
					PixelRect pixelRect = primaryDisplaySession.ClampWindowToWorkArea(valueOrDefault);
					if (pixelRect != valueOrDefault)
					{
						DisplayService.MoveWindowToPixelRect(Handle, pixelRect);
					}
				}
			}
		}
		ApplyAppearance();
		InstallHitTestHook();
		ThemeService current = ThemeService.Current;
		if (current != null)
		{
			current.Changed += OnThemeChanged;
		}
	}

	private void InstallHitTestHook()
	{
		if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
		{
			hwndSource.AddHook(OnWindowMessage);
		}
	}

	private nint OnWindowMessage(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
	{
		if (message == 70 && IsFixedDesktopLayer)
		{
			WindowStyles.ConstrainToDesktopLayer(lParam);
		}
		if (message != 132)
		{
			return 0;
		}
		short screenX = (short)lParam;
		short screenY = (short)((long)lParam >> 16);
		if (!IsInsidePanel(hwnd, screenX, screenY))
		{
			handled = true;
			return -1;
		}
		return 0;
	}

	private bool IsInsidePanel(nint hwnd, int screenX, int screenY)
	{
		PrimaryDisplaySession primaryDisplaySession = PrimaryDisplaySession;
		if (primaryDisplaySession != null && !primaryDisplaySession.IsAvailable)
		{
			return false;
		}
		PixelRect? windowPixelBounds = DisplayService.GetWindowPixelBounds(hwnd);
		if (windowPixelBounds.HasValue)
		{
			PixelRect valueOrDefault = windowPixelBounds.GetValueOrDefault();
			PrimaryDisplaySession primaryDisplaySession2 = PrimaryDisplaySession;
			uint num = ((primaryDisplaySession2 != null && primaryDisplaySession2.IsAvailable) ? primaryDisplaySession2.Display.Dpi : DisplayService.DpiForWindow(hwnd));
			double num2 = ((num == 0) ? 1.0 : ((double)num / 96.0));
			double val = (double)valueOrDefault.Width / num2 - ChromeInset.Left - ChromeInset.Right;
			double val2 = (double)valueOrDefault.Height / num2 - ChromeInset.Top - ChromeInset.Bottom;
			double x = (double)(screenX - valueOrDefault.X) / num2 - ChromeInset.Left;
			double y = (double)(screenY - valueOrDefault.Y) / num2 - ChromeInset.Top;
			double radius = Math.Max(Math.Max(PanelCornerRadius.TopLeft, PanelCornerRadius.TopRight), Math.Max(PanelCornerRadius.BottomLeft, PanelCornerRadius.BottomRight));
			return ContainsPanelGeometryCore(new Rect(0.0, 0.0, Math.Max(0.0, val), Math.Max(0.0, val2)), radius, new Point(x, y));
		}
		return true;
	}

	protected override void OnClosed(EventArgs e)
	{
		_shadow?.Dispose();
		_shadow = null;
		_captureExclusion?.Dispose();
		CaptureExclusionSession.Forget(Handle);
		_captureExclusion = null;
		ThemeService current = ThemeService.Current;
		if (current != null)
		{
			current.Changed -= OnThemeChanged;
		}
		base.OnClosed(e);
	}

	protected void ApplyAppearance()
	{
		ThemeService current = ThemeService.Current;
		AppearanceSettings appearanceSettings = current?.Appearance;
		if (SupportsDesktopLayer)
		{
			Topmost = !IsFixedDesktopLayer;
			if (IsFixedDesktopLayer)
			{
				WindowStyles.SendToDesktopLayer(Handle);
			}
		}
		GlassPalette glassPalette = current?.Palette ?? Palette ?? GlassPalette.Create(isDark: true, GlassPalette.DefaultAccent, opaquePanels: false);
		if (UseOpaquePanel)
		{
			glassPalette = GlassPalette.Create(isDark: true, glassPalette.Accent, opaquePanels: true);
		}
		Palette = glassPalette;
		if (appearanceSettings != null)
		{
			PanelCornerRadius = new CornerRadius(ResolveCornerRadius(appearanceSettings));
		}
		SurfaceMode = (UseOpaquePanel ? GlassSurfaceMode.SelfDrawn : ResolveSurface(current, appearanceSettings));
		ApplySurface(glassPalette);
		ApplyPaletteBrushes(glassPalette);
		UpdateGlassClip();
		if (!UseOpaquePanel && appearanceSettings != null && appearanceSettings.GlassMaterial == GlassMaterialMode.PlainGlass)
		{
			_shadow?.Dispose();
			_shadow = null;
		}
		else if (_shadow == null && Handle != 0)
		{
			_shadow = new GlassShadow(this);
		}
		_shadow?.Update();
	}

	private void UpdateGlassClip()
	{
		if (_sharedPanelClip != null)
		{
			Clip = _sharedPanelClip;
			return;
		}
		double actualWidth = ActualWidth;
		double actualHeight = ActualHeight;
		if (actualWidth <= 0.0 || actualHeight <= 0.0)
		{
			Clip = null;
			return;
		}
		double num = Math.Max(0.0, actualWidth - ChromeInset.Left - ChromeInset.Right);
		double num2 = Math.Max(0.0, actualHeight - ChromeInset.Top - ChromeInset.Bottom);
		if (num <= 0.0 || num2 <= 0.0)
		{
			Clip = null;
			return;
		}
		Rect rect = new Rect(ChromeInset.Left, ChromeInset.Top, num, num2);
		double val = Math.Max(Math.Max(PanelCornerRadius.TopLeft, PanelCornerRadius.TopRight), Math.Max(PanelCornerRadius.BottomLeft, PanelCornerRadius.BottomRight));
		val = Math.Min(val, Math.Min(num, num2) / 2.0);
		CombinedGeometry geometry = new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(new Rect(0.0, 0.0, actualWidth, actualHeight)), new RectangleGeometry(rect));
		Geometry geometry2 = CreatePanelGeometryCore(rect, val);
		CombinedGeometry combinedGeometry = new CombinedGeometry(GeometryCombineMode.Union, geometry, geometry2);
		combinedGeometry.Freeze();
		Clip = combinedGeometry;
	}

	protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
	{
		base.OnRenderSizeChanged(sizeInfo);
		UpdateGlassClip();
	}

	private static GlassSurfaceMode ResolveSurface(ThemeService? theme, AppearanceSettings? appearance)
	{
		if ((appearance != null && appearance.GlassMaterial == GlassMaterialMode.PlainGlass) || (appearance != null && !appearance.UseSystemBackdrop) || (theme != null && theme.UseOpaquePanels))
		{
			return GlassSurfaceMode.SelfDrawn;
		}
		return GlassSurfaceMode.OpticalGlass;
	}

	private void ApplySurface(GlassPalette palette)
	{
		nint handle = Handle;
		if (handle == 0)
		{
			return;
		}
		if (SurfaceMode == GlassSurfaceMode.Acrylic)
		{
			if (!WindowAcrylic.TryApply(handle, ResolveAcrylicTintAlpha(palette), palette.AcrylicTint.R, palette.AcrylicTint.G, palette.AcrylicTint.B))
			{
				SurfaceMode = GlassSurfaceMode.SelfDrawn;
			}
		}
		else
		{
			WindowAcrylic.TryRemove(handle);
		}
	}

	protected virtual byte ResolveAcrylicTintAlpha(GlassPalette palette)
	{
		return palette.AcrylicTintAlpha;
	}

	private void ApplyPaletteBrushes(GlassPalette palette)
	{
		if (UseOpaquePanel)
		{
			Color fallbackPanelBackground = palette.FallbackPanelBackground;
			SolidColorBrush solidColorBrush = new SolidColorBrush(Color.FromRgb(fallbackPanelBackground.R, fallbackPanelBackground.G, fallbackPanelBackground.B));
			solidColorBrush.Freeze();
			PanelBackgroundBrush = solidColorBrush;
		}
		else
		{
			PanelBackgroundBrush = palette.ResolvePanelBrush(SurfaceMode == GlassSurfaceMode.Acrylic);
		}
		PanelBorderBrush = palette.PanelBorderBrush;
		AppearanceSettings appearanceSettings = ThemeService.Current?.Appearance;
		DockBuiltinIconBrush = MonochromeBrush(appearanceSettings?.DockBuiltinIconBrightness ?? 1.0);
		LaunchpadIconFilled = appearanceSettings?.LaunchpadIconFilled ?? false;
		SolidColorBrush solidColorBrush2 = (SolidColorBrush)(ForegroundBrush = (UseStatusBarTextBrightness ? MonochromeBrush(appearanceSettings?.StatusBarTextBrightness ?? 1.0) : (UseLauncherTextBrightness ? MonochromeBrush(appearanceSettings?.LauncherTextBrightness ?? 1.0) : palette.ForegroundBrush)));
		SecondaryForegroundBrush = ((UseLauncherTextBrightness || UseStatusBarTextBrightness) ? solidColorBrush2 : palette.SecondaryForegroundBrush);
		AccentBrush = palette.AccentBrush;
		Foreground = solidColorBrush2;
	}

	private static SolidColorBrush MonochromeBrush(double brightness)
	{
		byte b = (byte)Math.Round(255.0 * (double.IsFinite(brightness) ? Math.Clamp(brightness, 0.0, 1.0) : 1.0));
		byte b2 = b;
		SolidColorBrush solidColorBrush = new SolidColorBrush(Color.FromRgb(b, b2, b2));
		solidColorBrush.Freeze();
		return solidColorBrush;
	}

	private void OnThemeChanged(object? sender, EventArgs e)
	{
		if (!Dispatcher.HasShutdownStarted && !Dispatcher.HasShutdownFinished)
		{
			ApplyAppearance();
		}
	}
}
