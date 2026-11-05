#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GlassDesk.Core.Models;
using GlassDesk.Platform;
using GlassDesk.Platform.Displays;
using GlassDesk.UI.Repair;
using GlassDesk.UI.Theming;

namespace GlassDesk.UI.Controls;

public sealed class LiquidGlassSurface : FrameworkElement, IDisposable
{
	private sealed record MaterialStamp(Point Origin, Rect CaptureBounds, DpiScale Dpi, GlassMaterialMode Material, bool Dark, double Clarity, bool Launchpad, LiquidGlassSamplingParameters Sampling, LiquidGlassWeightParameters Weights, LiquidGlassLodParameters Lod, LiquidGlassFinishingParameters Finishing, LiquidGlassBrdfParameters Brdf, LiquidGlassDispersionParameters Dispersion, long RecoveryGeneration, bool DesktopOnly);

	private readonly GlassBackgroundLayer _background = new GlassBackgroundLayer();

	private LiquidGlassOptics _optics = new LiquidGlassOptics();

	private AndroidLiquidGlassRenderer _androidGlass = new AndroidLiquidGlassRenderer();

	private readonly GlassFrameWorker _worker = new GlassFrameWorker();

	private readonly DispatcherTimer _timer;

	private TimeSpan _refreshInterval = GlassBackgroundLayer.LiveFrameInterval;

	private bool _renderClockAttached;

	private TimeSpan _lastRenderingTime = TimeSpan.MinValue;

	private long _lastRenderingTicks;

	private long _contentChangedAt;

	private bool _containerAnimationActive;

	private bool _renderFrameQueued;

	private DispatcherOperation? _renderFrameOperation;

	private DispatcherOperation? _refreshOperation;

	private bool _performanceSleeping;

	private long _lastRenderTicks;

	private long _renderCount;

	private bool? _firstVisibleRenderMaterialReady;

	private bool _lastRenderMaterialReady;

	private long _lifecycleGeneration;

	private long _lastCaptureSequence;

	private long _lastCaptureGeneration;

	private long _lastCaptureSourceTicks;

	private long _revealCaptureUntil;

	private long _relocationHandoffUntil;

	private int _renderFrameSignals;

	private readonly TransitionGlassLayer _transitionLayer = new TransitionGlassLayer();

	private readonly TransitionGlassLayer _compatibilityLayer = new TransitionGlassLayer();

	private BitmapSource? _compatibilityBackdrop;

	private Rect _compatibilityBounds;

	private bool _wallpaperPreparationPending;

	private CancellationTokenSource? _wallpaperCancellation;

	private long _handoffStarted;

	private (Rect Panel, double Radius) _compatibilityClipKey;

	private MaterialStamp? _materialStamp;

	private MaterialStamp? _compatibilityStamp;

	private ThemeService? _theme;

	private double _observedBackgroundClarity = 0.5;

	private bool _useFrostedMaterial;

	private double _backgroundClarityOverride = double.NaN;

	private double _transitionBlurRadius;

	private Rect? _panelShape;

	private double _panelRadius;

	private Geometry? _panelGeometry;

	private WriteableBitmap? _frame;

	private Rect _sampledPanelBounds;

	private Rect _sampledGlassPanelBounds;

	private double _sampledGlassPanelRadius;

	private Rect? _transitionCaptureBounds;

	private BitmapSource? _transitionBackdrop;

	private Rect _transitionBackdropBounds;

	private byte[]? _lastSource;

	private (int Width, int Height, Size Size, Rect GlassBounds, double Radius, bool Squircle, GlassBackdropSource Source, double Strength, double Softness, LiquidGlassDebugView DebugView, LiquidGlassSamplingParameters SamplingParameters, LiquidGlassWeightParameters WeightParameters, LiquidGlassLodParameters LodParameters, Color TintColor, double TintOpacity, double TintLuminanceInfluence, LiquidGlassBrdfParameters BrdfParameters, LiquidGlassDispersionParameters DispersionParameters, GlassMaterialMode Material, bool Dark, bool Transition, double BackgroundClarity, bool Launchpad) _frameKey;

	private bool _frameUsesAndroidGlass;

	private readonly Queue<long> _opticalFrameTimestamps = new Queue<long>();

	private double _opticalRefreshRate;

	private double _lastOpticsMilliseconds;

	private double _light = 0.35;

	private double _hover;

	private double _transitionProgress = 1.0;

	private bool _refreshPending;

	private GlassBackdropScene? _backdropScene;

	private GlassBackdropScene? _frameScene;

	private GlassBackdropScene? _transitionBackdropScene;

	private GlassBackdropScene? _lastCaptureScene;

	private long _materialRetentionUntil;

	private static int _nextDiagnosticId;

	private readonly int _diagnosticId = Interlocked.Increment(ref _nextDiagnosticId);

	private string? _diagnosticMaterial;

	private GlassBackdropScene? _compatibilityScene;

	private bool _compatibilityIsDesktopSnapshot;

	private Point? _revealOrigin;

	private PixelRect? _revealHost;

	private int _preparedDesktopSnapshots;

	private double _lastRevealCaptureMilliseconds;

	private double _lastRevealRemapMilliseconds;

	private double _lastRevealFilterMilliseconds;

	private double _maximumSceneQueryMilliseconds;

	private double _maximumUiPreparationMilliseconds;

	private string? _firstVisibleMaterialSource;

	private bool _plainGlassMaterialSuspended;

	private SolidColorBrush? _plainGlassBrush;

	private readonly Action? _cachedRenderFrameAction;

	internal Func<GlassBackdropScene?> ReadBackdropScene { get; set; }

	internal Func<PixelRect, int, int, byte[]?> CaptureRevealBackdrop { get; set; }

	internal string? RevealMaterialInvalidationReason { get; private set; }

	public Func<bool>? IsBackdropActive { get; set; }

	private bool DesktopOnlyBackdrop => (Window.GetWindow(this) as GlassWindow)?.UsesDesktopOnlyBackdrop ?? false;

	public bool UseLaunchpadOptics { get; set; }

	public bool UseLauncherTransparency { get; set; }

	public bool TransparentWhileLoading { get; set; }

	public bool UseFrostedMaterial
	{
		get
		{
			return _useFrostedMaterial;
		}
		set
		{
			if (_useFrostedMaterial != value)
			{
				_useFrostedMaterial = value;
				RefreshMaterialOverride();
			}
		}
	}

	public double BackgroundClarityOverride
	{
		get
		{
			return _backgroundClarityOverride;
		}
		set
		{
			if (!_backgroundClarityOverride.Equals(value))
			{
				_backgroundClarityOverride = value;
				RefreshMaterialOverride();
			}
		}
	}

	private GlassMaterialMode Material
	{
		get
		{
			if (!IsPlainGlass)
			{
				if (!_useFrostedMaterial)
				{
					return ThemeService.Current?.Appearance.GlassMaterial ?? GlassMaterialMode.AndroidLiquidGlass;
				}
				return GlassMaterialMode.FrostedGlass;
			}
			return GlassMaterialMode.PlainGlass;
		}
	}

	private double BackgroundClarity
	{
		get
		{
			double value;
			if (double.IsFinite(_backgroundClarityOverride))
			{
				value = _backgroundClarityOverride;
			}
			else
			{
				value = ((!UseLauncherTransparency) ? (ThemeService.Current?.Appearance.DockGlassTransparency ?? 0.9) : (ThemeService.Current?.Appearance.LauncherGlassTransparency ?? 0.2));
			}
			return AndroidLiquidGlassRenderer.NormalizeBackgroundClarity(value);
		}
	}

	public GlassBackdropSource BackdropSource { get; private set; }

	internal bool IsCurrentRevealMaterialReady
	{
		get
		{
			if (IsPlainGlass)
			{
				return true;
			}
			EnsureBackdropScene(revealing: true);
			ThemeService? current = ThemeService.Current;
			if ((current != null && current.UseOpaquePanels) || BackdropRecovery.RequiresBasicRendering || _background.RequiresBasicBackground)
			{
				return true;
			}
			MaterialStamp materialStamp = ReadMaterialStamp();
			if (_backdropScene != null)
			{
				if (_frame != null)
				{
					GlassBackdropScene? frameScene = _frameScene;
					if (frameScene != null && frameScene.Equals(_backdropScene))
					{
						goto IL_00ba;
					}
				}
				if (_transitionBackdrop != null)
				{
					GlassBackdropScene? transitionBackdropScene = _transitionBackdropScene;
					if (transitionBackdropScene != null && transitionBackdropScene.Equals(_backdropScene))
					{
						goto IL_00ba;
					}
				}
				goto IL_00cb;
			}
			return false;
			IL_00ba:
			if (_materialStamp == materialStamp)
			{
				return true;
			}
			goto IL_00cb;
			IL_00cb:
			if (_compatibilityIsDesktopSnapshot && _compatibilityBackdrop != null)
			{
				GlassBackdropScene? compatibilityScene = _compatibilityScene;
				if (compatibilityScene != null && compatibilityScene.Equals(_backdropScene))
				{
					return _compatibilityStamp == materialStamp;
				}
			}
			return false;
		}
	}

	public Rect? CaptureBoundsInWindow { get; set; }

	public double TransitionProgress
	{
		get
		{
			return _transitionProgress;
		}
		set
		{
			_transitionProgress = (double.IsFinite(value) ? Math.Clamp(value, 0.0, 1.0) : 1.0);
			UpdateTransitionLayer();
			if (_transitionProgress < 1.0 && _refreshInterval > GlassBackgroundLayer.TransitionFrameInterval)
			{
				SetTimerInterval(GlassBackgroundLayer.TransitionFrameInterval);
			}
			InvalidateVisual();
		}
	}

	public double TransitionBlurRadius
	{
		get
		{
			return _transitionBlurRadius;
		}
		set
		{
			_transitionBlurRadius = (double.IsFinite(value) ? Math.Clamp(value, 0.0, 6.0) : 0.0);
		}
	}

	protected override int VisualChildrenCount => 2;

	private bool IsPlainGlass
	{
		get
		{
			ThemeService? current = ThemeService.Current;
			if (current == null)
			{
				return false;
			}
			return current.Appearance.GlassMaterial == GlassMaterialMode.PlainGlass;
		}
	}

	internal event EventHandler? SharedMaterialChanged;

	private void RefreshMaterialOverride()
	{
		if (IsLoaded)
		{
			OnAppearanceChanged(this, EventArgs.Empty);
		}
	}

	public void SetContainerTransitionBounds(Rect? bounds)
	{
		if (!(_transitionCaptureBounds == bounds))
		{
			_transitionCaptureBounds = bounds;
			_worker.Invalidate();
			_lastSource = null;
			SetTimerInterval(GlassBackgroundLayer.LiveFrameInterval);
			RequestBackdropRefresh();
			UpdateTransitionLayer();
			InvalidateVisual();
		}
	}

	public void SetPanelShape(Rect bounds, double radius, Geometry? geometry = null)
	{
		if (!bounds.IsEmpty && !(bounds.Width <= 0.0) && !(bounds.Height <= 0.0))
		{
			_panelShape = bounds;
			_panelRadius = (double.IsFinite(radius) ? Math.Max(0.0, radius) : 0.0);
			_panelGeometry = geometry;
			Clip = geometry ?? ((Window.GetWindow(this) is GlassWindow glassWindow) ? glassWindow.CreateGlassPanelGeometry(bounds, _panelRadius) : SquircleGlassGeometry.Create(bounds, _panelRadius));
			CaptureBoundsInWindow = bounds;
			UpdateTransitionLayer();
			InvalidateVisual();
		}
	}

	public void SetContainerAnimationActive(bool active)
	{
		if (IsPlainGlass)
		{
			_containerAnimationActive = false;
			return;
		}
		_containerAnimationActive = active && !_performanceSleeping;
		if (_containerAnimationActive)
		{
			_background.SetInteractionActive(active: true);
		}
		_background.SetAnimationActive(_containerAnimationActive);
		if (_containerAnimationActive)
		{
			SetTimerInterval(GlassBackgroundLayer.LiveFrameInterval);
		}
	}

	public void SetTransitionFrame(Rect bounds, double radius, Geometry geometry, double progress, double blurRadius)
	{
		if (!bounds.IsEmpty && !(bounds.Width <= 0.0) && !(bounds.Height <= 0.0))
		{
			_transitionProgress = (double.IsFinite(progress) ? Math.Clamp(progress, 0.0, 1.0) : 1.0);
			_transitionBlurRadius = (double.IsFinite(blurRadius) ? Math.Clamp(blurRadius, 0.0, 6.0) : 0.0);
			if (_transitionProgress < 1.0 && _refreshInterval > GlassBackgroundLayer.TransitionFrameInterval)
			{
				SetTimerInterval(GlassBackgroundLayer.TransitionFrameInterval);
			}
			SetPanelShape(bounds, radius, geometry);
		}
	}

	public LiquidGlassSurface()
	{
		ReadBackdropScene = CaptureBackdropScene;
		CaptureRevealBackdrop = CaptureHiddenDesktop;
		_backdropScene = (IsPlainGlass ? null : ReadBackdropScene());
		IsHitTestVisible = false;
		AddVisualChild(_transitionLayer);
		AddVisualChild(_compatibilityLayer);
		RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
		_timer = new DispatcherTimer(DispatcherPriority.Background)
		{
			Interval = TimeSpan.FromMilliseconds(100L)
		};
		_timer.Tick += OnWatchdogFrame;
		Loaded += (object _, RoutedEventArgs _) =>
		{
			_theme = ThemeService.Current;
			if (_theme != null)
			{
				_theme.Changed -= OnAppearanceChanged;
				_theme.Changed += OnAppearanceChanged;
				_observedBackgroundClarity = BackgroundClarity;
			}
			RequestBackdropRefresh(revealing: true);
			if (!IsPlainGlass && !_performanceSleeping && IsVisible)
			{
				Func<bool>? isBackdropActive = IsBackdropActive;
				if (isBackdropActive == null || isBackdropActive())
				{
					_timer.Start();
				}
			}
		};
		Unloaded += (object _, RoutedEventArgs _) =>
		{
			Dispose();
		};
		IsVisibleChanged += (object _, DependencyPropertyChangedEventArgs _) =>
		{
			_firstVisibleRenderMaterialReady = null;
			_firstVisibleMaterialSource = null;
			UpdateRenderClock();
			if (!IsVisible)
			{
				_revealCaptureUntil = 0L;
				_worker.Invalidate();
				_worker.ClearCachedBuffers();
				_lastSource = null;
				_background.Suspend();
				_timer.Stop();
				StopRenderClock();
				CancelQueuedRefresh();
			}
			else
			{
				RequestBackdropRefresh(revealing: true);
			}
		};
		if (IsPlainGlass)
		{
			SuspendPlainGlassMaterial();
		}
		_cachedRenderFrameAction = () =>
		{
			_renderFrameOperation = null;
			_renderFrameQueued = false;
			if (_renderClockAttached && IsLoaded && IsVisible)
			{
				OnFrame(null, EventArgs.Empty);
			}
		};
	}

	public void RequestBackdropRefresh(bool revealing = false)
	{
		if (IsPlainGlass)
		{
			SuspendPlainGlassMaterial();
			InvalidateVisual();
			return;
		}
		_contentChangedAt = Stopwatch.GetTimestamp();
		EnsureBackdropScene(revealing);
		if (_performanceSleeping)
		{
			InvalidateVisual();
			return;
		}
		SetTimerInterval(GlassBackgroundLayer.LiveFrameInterval);
		if (_refreshPending)
		{
			return;
		}
		_refreshPending = true;
		long generation = _lifecycleGeneration;
		_refreshOperation = Dispatcher.BeginInvoke(DispatcherPriority.Render, (Action)(() =>
		{
			_refreshOperation = null;
			_refreshPending = false;
			if (!_performanceSleeping && generation == _lifecycleGeneration && IsLoaded)
			{
				OnFrame(null, EventArgs.Empty);
			}
		}));
		InvalidateVisual();
	}

	public void BeginRelocationHandoff()
	{
		if (!_performanceSleeping && (_frame != null || _transitionBackdrop != null))
		{
			_relocationHandoffUntil = Stopwatch.GetTimestamp() + Stopwatch.Frequency * 3 / 4;
		}
	}

	public void RetireConcealedMaterial()
	{
		RecordMaterialRetirement("Concealed", expected: true);
		_lifecycleGeneration++;
		_worker.Invalidate();
		_worker.ClearCachedBuffers();
		_lastSource = null;
		_lastCaptureSequence = (_lastCaptureGeneration = (_lastCaptureSourceTicks = 0L));
		_lastCaptureScene = null;
		_frame = null;
		_frameScene = null;
		_materialStamp = null;
		_transitionBackdrop = null;
		_transitionBackdropScene = null;
		_compatibilityBackdrop = null;
		_compatibilityStamp = null;
		_compatibilityIsDesktopSnapshot = false;
		_compatibilityScene = null;
		_handoffStarted = (_materialRetentionUntil = (_revealCaptureUntil = (_relocationHandoffUntil = 0L)));
		_wallpaperCancellation?.Cancel();
		_wallpaperCancellation = null;
		_wallpaperPreparationPending = false;
		_background.Suspend();
		_timer.Stop();
		StopRenderClock();
		CancelQueuedRefresh();
		_transitionLayer.SetBackdrop(null);
		_transitionLayer.Opacity = 0.0;
		_compatibilityLayer.SetBackdrop(null);
		_compatibilityLayer.Opacity = 0.0;
		_firstVisibleRenderMaterialReady = null;
		_firstVisibleMaterialSource = null;
		BackdropSource = GlassBackdropSource.Transparent;
		InvalidateVisual();
	}

	public void SetPerformanceSleeping(bool sleeping)
	{
		if (_performanceSleeping == sleeping)
		{
			LauncherRevealRepair.AfterSleep(this, sleeping);
			return;
		}
		_performanceSleeping = sleeping;
		_revealCaptureUntil = 0L;
		_lifecycleGeneration++;
		_worker.Invalidate();
		_worker.ClearCachedBuffers();
		_lastSource = null;
		_lastCaptureSequence = (_lastCaptureGeneration = (_lastCaptureSourceTicks = 0L));
		_lastCaptureScene = null;
		_background.SetPerformanceSleeping(sleeping || IsPlainGlass);
		_transitionLayer.SetPerformanceSleeping(sleeping || IsPlainGlass);
		_compatibilityLayer.SetPerformanceSleeping(sleeping || IsPlainGlass);
		if (sleeping)
		{
			_containerAnimationActive = false;
			_timer.Stop();
			StopRenderClock();
			CancelQueuedRefresh();
			_optics = new LiquidGlassOptics();
			_androidGlass = new AndroidLiquidGlassRenderer();
			_opticalFrameTimestamps.Clear();
			_opticalRefreshRate = 0.0;
			_handoffStarted = (_relocationHandoffUntil = 0L);
			_frame = null;
			_frameScene = null;
			_transitionBackdrop = null;
			_transitionBackdropScene = null;
			_materialStamp = null;
			_compatibilityBackdrop = null;
			_compatibilityStamp = null;
			_compatibilityIsDesktopSnapshot = false;
			_compatibilityScene = null;
			_compatibilityLayer.SetBackdrop(null);
			_compatibilityLayer.Opacity = 0.0;
			_wallpaperPreparationPending = false;
			_wallpaperCancellation?.Cancel();
			_wallpaperCancellation = null;
		}
		else if (!IsPlainGlass && IsLoaded && IsVisible)
		{
			Func<bool>? isBackdropActive = IsBackdropActive;
			if (isBackdropActive == null || isBackdropActive())
			{
				_timer.Start();
				RequestBackdropRefresh(revealing: true);
			}
		}
		InvalidateVisual();
		LauncherRevealRepair.AfterSleep(this, sleeping);
	}

	public object InspectPerformance()
	{
		int renderingTier = RenderCapability.Tier >> 16;
		RenderMode processRenderMode = RenderOptions.ProcessRenderMode;
		RenderMode? renderMode = (PresentationSource.FromVisual(this)?.CompositionTarget as HwndTarget)?.RenderMode;
		int windowRenderMode;
		if (renderMode.HasValue)
		{
			RenderMode valueOrDefault = renderMode.GetValueOrDefault();
			windowRenderMode = (int)valueOrDefault;
		}
		else
		{
			windowRenderMode = -1;
		}
		return new
		{
			renderingTier = renderingTier,
			processRenderMode = (int)processRenderMode,
			windowRenderMode = windowRenderMode,
			shader3Supported = RenderCapability.IsPixelShaderVersionSupported(3, 0),
			gpuLensActive = (_transitionLayer.UsesGpuLens || _compatibilityLayer.UsesGpuLens),
			basicRendering = BackdropRecovery.RequiresBasicRendering,
			remoteSession = SystemParameters.IsRemoteSession,
			sleeping = _performanceSleeping,
			generation = _lifecycleGeneration,
			renderClockAttached = _renderClockAttached,
			watchdogEnabled = _timer.IsEnabled,
			renderQueued = _renderFrameQueued,
			refreshQueued = _refreshPending,
			opticsBusy = _worker.IsBusy,
			lastRenderTicks = _lastRenderTicks,
			renderCount = _renderCount,
			lastRenderMaterialReady = _lastRenderMaterialReady,
			firstVisibleRenderMaterialReady = _firstVisibleRenderMaterialReady,
			firstVisibleMaterialSource = _firstVisibleMaterialSource,
			materialReady = (IsPlainGlass || _frame != null || _transitionBackdrop != null || _compatibilityBackdrop != null),
			compatibilityReady = (_compatibilityBackdrop != null),
			handoffActive = (_handoffStarted != 0),
			compatibilityIsDesktopSnapshot = _compatibilityIsDesktopSnapshot,
			preparedDesktopSnapshots = _preparedDesktopSnapshots,
			revealMaterialInvalidationReason = RevealMaterialInvalidationReason,
			revealCaptureMilliseconds = _lastRevealCaptureMilliseconds,
			revealRemapMilliseconds = _lastRevealRemapMilliseconds,
			revealFilterMilliseconds = _lastRevealFilterMilliseconds,
			maximumSceneQueryMilliseconds = _maximumSceneQueryMilliseconds,
			maximumUiPreparationMilliseconds = _maximumUiPreparationMilliseconds,
			capture = _background.InspectPerformance(),
			source = BackdropSource.ToString(),
			retainedPixelBytes = (long)(_frame?.PixelWidth ?? 0) * (long)(_frame?.PixelHeight ?? 0) * 4 + (long)(_transitionBackdrop?.PixelWidth ?? 0) * (long)(_transitionBackdrop?.PixelHeight ?? 0) * 4 + (long)(_compatibilityBackdrop?.PixelWidth ?? 0) * (long)(_compatibilityBackdrop?.PixelHeight ?? 0) * 4
		};
	}

	private void CancelQueuedRefresh()
	{
		_refreshOperation?.Abort();
		_refreshOperation = null;
		_refreshPending = false;
	}

	public void Dispose()
	{
		if (_theme != null)
		{
			_theme.Changed -= OnAppearanceChanged;
		}
		_theme = null;
		_worker.Invalidate();
		_worker.ClearCachedBuffers();
		StopRenderClock();
		CancelQueuedRefresh();
		_lifecycleGeneration++;
		_revealCaptureUntil = 0L;
		_wallpaperCancellation?.Cancel();
		_wallpaperCancellation = null;
		_timer.Stop();
		_background.Dispose();
		_frame = null;
		_lastSource = null;
		_transitionBackdrop = null;
		_compatibilityBackdrop = null;
		_frameScene = null;
		_transitionBackdropScene = null;
		_transitionLayer.SetBackdrop(null);
		_transitionLayer.Opacity = 0.0;
		_compatibilityLayer.SetBackdrop(null);
		_compatibilityLayer.Opacity = 0.0;
		BackdropSource = GlassBackdropSource.Transparent;
	}

	private void OnAppearanceChanged(object? sender, EventArgs e)
	{
		if (IsPlainGlass)
		{
			SuspendPlainGlassMaterial();
			InvalidateVisual();
			return;
		}
		if (_plainGlassMaterialSuspended)
		{
			_plainGlassMaterialSuspended = false;
			_background.SetPerformanceSleeping(_performanceSleeping);
			_transitionLayer.SetPerformanceSleeping(_performanceSleeping);
			_compatibilityLayer.SetPerformanceSleeping(_performanceSleeping);
		}
		RecordMaterialRetirement("AppearanceChanged", expected: true);
		_lifecycleGeneration++;
		_wallpaperCancellation?.Cancel();
		_wallpaperCancellation = null;
		_wallpaperPreparationPending = false;
		_frame = null;
		_frameScene = null;
		_transitionBackdrop = null;
		_transitionBackdropScene = null;
		_compatibilityBackdrop = null;
		_compatibilityStamp = null;
		_materialStamp = null;
		_handoffStarted = 0L;
		_background.Suspend();
		double backgroundClarity = BackgroundClarity;
		_observedBackgroundClarity = backgroundClarity;
		_worker.Invalidate();
		_lastSource = null;
		SetTimerInterval(GlassBackgroundLayer.LiveFrameInterval);
		RequestBackdropRefresh();
		InvalidateVisual();
	}

	private async void OnFrame(object? sender, EventArgs e)
	{
		if (_performanceSleeping || IsPlainGlass)
		{
			return;
		}
		long timestamp = Stopwatch.GetTimestamp();
		long lifecycleGeneration = _lifecycleGeneration;
		if (!RenderCapability.IsPixelShaderVersionSupported(3, 0))
		{
			BackdropRecovery.UseBasicRendering();
		}
		EnsureBackdropScene();
		Window window = Window.GetWindow(this);
		if (window is GlassWindow { IsVisible: not false } owner && IsVisible && !(ActualWidth < 1.0) && !(ActualHeight < 1.0))
		{
			Func<bool>? isBackdropActive = IsBackdropActive;
			if (isBackdropActive == null || isBackdropActive())
			{
				_revealCaptureUntil = 0L;
				PrimaryDisplaySession primaryDisplaySession = owner.PrimaryDisplaySession;
				if (primaryDisplaySession != null && !primaryDisplaySession.IsAvailable)
				{
					_worker.Invalidate();
					_worker.ClearCachedBuffers();
					SetTimerInterval(TimeSpan.FromMilliseconds(200L));
					_background.Dispose();
					_frame = null;
					_transitionBackdrop = null;
					_lastSource = null;
					UpdateTransitionLayer();
				}
				else if (ThemeService.Current?.UseOpaquePanels ?? false)
				{
					_worker.Invalidate();
					_worker.ClearCachedBuffers();
					BackdropSource = GlassBackdropSource.Opaque;
					_frame = null;
					_lastSource = null;
					_background.Dispose();
					SetTimerInterval(TimeSpan.FromMilliseconds(200L));
					InvalidateVisual();
				}
				else
				{
					if (_worker.IsBusy)
					{
						return;
					}
					ThemeService? current = ThemeService.Current;
					bool flag = current != null && !current.AnimationsEnabled;
					bool hovering = owner.IsMouseOver && !flag;
					bool interactive = hovering || _hover >= 0.001 || _containerAnimationActive || _transitionProgress < 1.0 || _handoffStarted != 0;
					_background.SetInteractionActive(interactive);
					Rect panelRect = _panelShape ?? new Rect(RenderSize);
					double num = (hovering ? Math.Clamp((Mouse.GetPosition(this).X - panelRect.X) / panelRect.Width, 0.05, 0.95) : 0.35);
					_light += (num - _light) * (flag ? 1.0 : 0.22);
					_hover += ((double)(hovering ? 1 : 0) - _hover) * (flag ? 1.0 : 0.2);
					LiquidGlassSamplingParameters samplingParameters = LiquidGlassDebugState.SamplingParameters;
					LiquidGlassWeightParameters weightParameters = LiquidGlassDebugState.WeightParameters;
					LiquidGlassLodParameters lodParameters = LiquidGlassDebugState.LodParameters;
					LiquidGlassDebugView debugView = LiquidGlassDebugState.View;
					GlassMaterialMode material = Material;
					bool dark = ThemeService.Current?.IsDark ?? true;
					double backgroundClarity = BackgroundClarity;
					GlassMaterialMode glassMaterialMode = material;
					bool flag2 = ((glassMaterialMode == GlassMaterialMode.AndroidLiquidGlass || glassMaterialMode == GlassMaterialMode.FrostedGlass) ? true : false);
					bool useAndroidGlass = flag2 && debugView == LiquidGlassDebugView.None;
					Rect? rect = (useAndroidGlass ? _transitionCaptureBounds : ((Rect?)null));
					bool transitionBackdrop = rect.HasValue;
					LiquidGlassFinishingParameters finishingParameters = LiquidGlassDebugState.FinishingParameters;
					LiquidGlassBrdfParameters brdfParameters = LiquidGlassDebugState.BrdfParameters;
					LiquidGlassDispersionParameters dispersionParameters = LiquidGlassDebugState.DispersionParameters;
					LiquidGlassDebugView liquidGlassDebugView = LiquidGlassFinishComposer.OpticsView(debugView);
					switch (liquidGlassDebugView)
					{
					case LiquidGlassDebugView.None:
					case LiquidGlassDebugView.FinalMixedSampling:
					case LiquidGlassDebugView.LodOff:
					case LiquidGlassDebugView.FaceLodOnly:
					case LiquidGlassDebugView.InnerLodOnly:
					case LiquidGlassDebugView.OuterLodOnly:
					case LiquidGlassDebugView.FinalMixedWithLod:
					case LiquidGlassDebugView.DispersionOff:
					case LiquidGlassDebugView.InnerDispersionOnly:
					case LiquidGlassDebugView.OuterDispersionOnly:
					case LiquidGlassDebugView.FinalWithDispersion:
					case LiquidGlassDebugView.DispersionTimesTenDebug:
						flag2 = true;
						break;
					default:
						flag2 = false;
						break;
					}
					bool flag3 = flag2;
					bool flag4 = (liquidGlassDebugView == LiquidGlassDebugView.OuterOnly) | flag3;
					switch (liquidGlassDebugView)
					{
					case LiquidGlassDebugView.None:
					case LiquidGlassDebugView.FaceLodOnly:
					case LiquidGlassDebugView.InnerLodOnly:
					case LiquidGlassDebugView.OuterLodOnly:
					case LiquidGlassDebugView.FinalMixedWithLod:
					case LiquidGlassDebugView.DispersionOff:
					case LiquidGlassDebugView.InnerDispersionOnly:
					case LiquidGlassDebugView.OuterDispersionOnly:
					case LiquidGlassDebugView.FinalWithDispersion:
					case LiquidGlassDebugView.DispersionTimesTenDebug:
						flag2 = true;
						break;
					default:
						flag2 = false;
						break;
					}
					bool flag5 = flag2;
					double num2 = (flag4 ? samplingParameters.OuterRimCaptureMarginDip : 0.0);
					if (flag5)
					{
						num2 += lodParameters.MaximumActiveFootprintDip;
					}
					if (useAndroidGlass)
					{
						num2 = 48.0;
					}
					Rect? rect2 = rect ?? CaptureBoundsInWindow;
					Point point;
					if (rect2.HasValue)
					{
						Rect valueOrDefault = rect2.GetValueOrDefault();
						point = owner.PointToScreen(new Point(valueOrDefault.X, valueOrDefault.Y));
					}
					else
					{
						point = PointToScreen(default);
					}
					Point panelScreenOrigin = point;
					Point point2;
					if (rect2.HasValue)
					{
						Rect valueOrDefault2 = rect2.GetValueOrDefault();
						point2 = owner.PointToScreen(new Point(valueOrDefault2.Right, valueOrDefault2.Bottom));
					}
					else
					{
						point2 = PointToScreen(new Point(ActualWidth, ActualHeight));
					}
					Point point3 = point2;
					PrimaryDisplaySession primaryDisplaySession2 = owner.PrimaryDisplaySession;
					DisplayInfo displayInfo = ((primaryDisplaySession2 != null && primaryDisplaySession2.IsAvailable) ? primaryDisplaySession2.Display : DisplayService.Primary());
					if ((object)displayInfo == null)
					{
						SetTimerInterval(TimeSpan.FromMilliseconds(200L));
						_background.Dispose();
						_frame = null;
						_lastSource = null;
						return;
					}
					double scaleFactor = displayInfo.ScaleFactor;
					int num3 = (int)Math.Floor(panelScreenOrigin.X - num2 * scaleFactor);
					int num4 = (int)Math.Floor(panelScreenOrigin.Y - num2 * scaleFactor);
					int num5 = (int)Math.Ceiling(point3.X + num2 * scaleFactor);
					int num6 = (int)Math.Ceiling(point3.Y + num2 * scaleFactor);
					PixelRect pixelRect = new PixelRect(num3, num4, Math.Max(1, num5 - num3), Math.Max(1, num6 - num4));
					PrimaryDisplaySession primaryDisplaySession3 = owner.PrimaryDisplaySession;
					PixelRect pixelRect2 = ((primaryDisplaySession3 != null && primaryDisplaySession3.IsAvailable) ? primaryDisplaySession3.ClipToPrimaryBounds(pixelRect) : ClipRegion(pixelRect, displayInfo.Bounds));
					if (pixelRect2.Width > 0 && pixelRect2.Height > 0)
					{
						bool launchpadOptics = UseLaunchpadOptics;
						double num7 = (launchpadOptics ? (320000.0 + 680000.0 * Math.Clamp((backgroundClarity - 0.5) * 2.0, 0.0, 1.0)) : ((double)((!transitionBackdrop && _transitionProgress < 1.0) ? 100000 : 320000)));
						double num8 = Math.Min(1.0, Math.Min(2048.0 / (double)Math.Max(pixelRect.Width, pixelRect.Height), Math.Sqrt(num7 / ((double)pixelRect.Width * (double)pixelRect.Height))));
						int width = Math.Max(1, (int)Math.Floor((double)pixelRect.Width * num8));
						int height = Math.Max(1, (int)Math.Floor((double)pixelRect.Height * num8));
						Rect? panelShape = _panelShape;
						double radius = ((!panelShape.HasValue) ? owner.PanelCornerRadius.TopLeft : _panelRadius);
						bool squircle = owner.UsesSquirclePanelGeometry;
						GlassCaptureGeometry glassCaptureGeometry = GlassCaptureGeometry.Create(pixelRect, pixelRect2, panelScreenOrigin, panelRect, scaleFactor, num2 > 0.0);
						if (transitionBackdrop)
						{
							glassCaptureGeometry = GlassCaptureGeometry.Create(pixelRect, pixelRect2, owner.PointToScreen(panelRect.Location), panelRect, scaleFactor, expanded: true);
						}
						Size sampleSizeDip = glassCaptureGeometry.SampleSizeDip;
						Rect glassBoundsDip = glassCaptureGeometry.GlassBoundsDip;
						Rect sampledFrameBounds = glassCaptureGeometry.FrameBoundsDip;
						using GlassBackgroundFrame capturedBackground = _background.Capture(pixelRect2, Math.Max(1, (int)Math.Floor((double)pixelRect2.Width * num8)), Math.Max(1, (int)Math.Floor((double)pixelRect2.Height * num8)), radius, owner.SurfaceMode == GlassSurfaceMode.OpticalGlass, DesktopOnlyBackdrop);
						if (_background.MustDiscardUnverifiedMaterial)
						{
							if (_frame != null || _transitionBackdrop != null || _compatibilityIsDesktopSnapshot)
							{
								RecordMaterialRetirement("UnverifiedBackground");
								_frame = null;
								_frameScene = null;
								_materialStamp = null;
								_transitionBackdrop = null;
								_transitionBackdropScene = null;
								_compatibilityBackdrop = null;
								_compatibilityStamp = null;
								_compatibilityIsDesktopSnapshot = false;
								_compatibilityScene = null;
								_handoffStarted = (_materialRetentionUntil = 0L);
							}
							_worker.Invalidate();
							_lastSource = null;
							BackdropSource = GlassBackdropSource.Transparent;
							UpdateTransitionLayer();
							InvalidateVisual();
							return;
						}
						GlassBackdropScene capturedScene = _backdropScene;
						MaterialStamp capturedStamp = ReadMaterialStamp();
						long capturedSourceTicks = _background.LastCaptureTiming?.SourceTicks ?? 0;
						using GlassBackgroundFrame background = ((capturedBackground == null) ? null : _background.ExtendCapturedFrame(capturedBackground, pixelRect2, pixelRect, width, height));
						byte[] source = background?.Pixels;
						_maximumUiPreparationMilliseconds = Math.Max(_maximumUiPreparationMilliseconds, Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds);
						GlassBackdropSource glassBackdropSource = (BackdropSource = background?.Source ?? GlassBackdropSource.Transparent);
						if (glassBackdropSource == GlassBackdropSource.Basic)
						{
							RecordMaterialRetirement("CaptureDeadlineExpired");
							_worker.Invalidate();
							_frame = null;
							_transitionBackdrop = null;
							_lastSource = null;
							UpdateTransitionLayer();
							InvalidateVisual();
							return;
						}
						double strength = 0.6 + 0.4 * _transitionProgress;
						(int width, int height, Size sampleSizeDip, Rect, double, bool squircle, GlassBackdropSource kind, double strength, double _transitionBlurRadius, LiquidGlassDebugView debugView, LiquidGlassSamplingParameters samplingParameters, LiquidGlassWeightParameters weightParameters, LiquidGlassLodParameters lodParameters, Color TintColor, double TintOpacity, double TintLuminanceInfluence, LiquidGlassBrdfParameters brdfParameters, LiquidGlassDispersionParameters dispersionParameters, GlassMaterialMode material, bool dark, bool transitionBackdrop, double backgroundClarity, bool launchpadOptics) key = (width: width, height: height, sampleSizeDip: sampleSizeDip, transitionBackdrop ? Rect.Empty : glassBoundsDip, transitionBackdrop ? 0.0 : radius, squircle: squircle, kind: glassBackdropSource, strength: strength, _transitionBlurRadius: _transitionBlurRadius, debugView: debugView, samplingParameters: samplingParameters, weightParameters: weightParameters, lodParameters: lodParameters, TintColor: finishingParameters.TintColor, TintOpacity: finishingParameters.TintOpacity, TintLuminanceInfluence: finishingParameters.TintLuminanceInfluence, brdfParameters: brdfParameters, dispersionParameters: dispersionParameters, material: material, dark: dark, transitionBackdrop: transitionBackdrop, backgroundClarity: backgroundClarity, launchpadOptics: launchpadOptics);
						if (source == null)
						{
							_lastSource = null;
						}
						else
						{
							if (!(transitionBackdrop ? (_transitionBackdrop == null) : (_frame == null)))
							{
								(int, int, Size, Rect, double, bool, GlassBackdropSource, double, double, LiquidGlassDebugView, LiquidGlassSamplingParameters, LiquidGlassWeightParameters, LiquidGlassLodParameters, Color, double, double, LiquidGlassBrdfParameters, LiquidGlassDispersionParameters, GlassMaterialMode, bool, bool, double, bool) frameKey = _frameKey;
								(int, int, Size, Rect, double, bool, GlassBackdropSource, double, double, LiquidGlassDebugView, LiquidGlassSamplingParameters, LiquidGlassWeightParameters, LiquidGlassLodParameters, Color, double, double, LiquidGlassBrdfParameters, LiquidGlassDispersionParameters, GlassMaterialMode, bool, bool, double, bool) tuple = key;
								if (frameKey.Item1 == tuple.Item1 && frameKey.Item2 == tuple.Item2 && !(frameKey.Item3 != tuple.Item3) && !(frameKey.Item4 != tuple.Item4) && frameKey.Item5 == tuple.Item5 && frameKey.Item6 == tuple.Item6 && frameKey.Item7 == tuple.Item7 && frameKey.Item8 == tuple.Item8 && frameKey.Item9 == tuple.Item9 && frameKey.Item10 == tuple.Item10 && !(frameKey.Item11 != tuple.Item11) && !(frameKey.Item12 != tuple.Item12) && !(frameKey.Item13 != tuple.Item13) && !(frameKey.Item14 != tuple.Item14) && frameKey.Item15 == tuple.Item15 && frameKey.Item16 == tuple.Item16 && !(frameKey.Item17 != tuple.Item17) && !(frameKey.Item18 != tuple.Item18) && frameKey.Item19 == tuple.Item19 && frameKey.Item20 == tuple.Item20 && frameKey.Item21 == tuple.Item21 && frameKey.Item22 == tuple.Item22 && frameKey.Item23 == tuple.Item23 && !((!background.IsCaptured) ? (_lastSource == null || source != _lastSource) : (background.Sequence != _lastCaptureSequence || background.CaptureGeneration != _lastCaptureGeneration)))
								{
									if (background.IsCaptured && capturedSourceTicks > _lastCaptureSourceTicks && CanCommitCapturedScene(capturedScene, capturedStamp, capturedSourceTicks))
									{
										if (transitionBackdrop)
										{
											_transitionBackdropScene = capturedScene;
										}
										else
										{
											_frameScene = capturedScene;
										}
										_lastCaptureSourceTicks = capturedSourceTicks;
										_lastCaptureScene = capturedScene;
										if (!_compatibilityIsDesktopSnapshot || (_compatibilityScene?.Equals(_backdropScene) ?? false))
										{
											_materialRetentionUntil = 0L;
										}
									}
									goto IL_18a0;
								}
							}
							_contentChangedAt = Stopwatch.GetTimestamp();
							bool flag6 = !useAndroidGlass && glassBackdropSource == GlassBackdropSource.Wallpaper && (debugView == LiquidGlassDebugView.None || LiquidGlassFinishComposer.IsComparisonView(debugView));
							double[] wallpaperWeights = (flag6 ? CaptureWallpaperShoulderWeights(owner, width, height, sampleSizeDip, glassBoundsDip, radius) : null);
							LiquidGlassOptics optics = _optics;
							AndroidLiquidGlassRenderer androidGlass = _androidGlass;
							GlassFrameResult glassFrameResult = ((!transitionBackdrop) ? (await _worker.RenderAsync(() => LiquidGlassFrameComposer.Compose(optics, source, width, height, sampleSizeDip, radius, squircle, strength, _transitionBlurRadius, debugView, samplingParameters, weightParameters, glassBoundsDip, lodParameters, finishingParameters, brdfParameters, dispersionParameters, (wallpaperWeights != null) ? ((Action<byte[]>)((byte[] pixels2) =>
							{
								ApplyWallpaperShoulder(pixels2, wallpaperWeights);
							})) : null, material, dark, androidGlass, 0.16, backgroundClarity, launchpadOptics))) : (await _worker.RenderBufferedAsync(source.Length, (byte[] pixels2, byte[] scratch) =>
							{
								RenderMaterialBackdropInto(material, source, pixels2, scratch, width, height, sampleSizeDip, dark, strength, backgroundClarity, launchpadOptics);
							})));
							using (GlassFrameResult glassFrameResult2 = glassFrameResult)
							{
							EnsureBackdropScene();
							if (glassFrameResult2 == null || !_worker.IsCurrent(glassFrameResult2) || _performanceSleeping || lifecycleGeneration != _lifecycleGeneration || !owner.IsVisible || !IsVisible)
							{
								return;
							}
							Func<bool>? isBackdropActive2 = IsBackdropActive;
							if (isBackdropActive2 != null && !isBackdropActive2())
							{
								return;
							}
							primaryDisplaySession = owner.PrimaryDisplaySession;
							if ((primaryDisplaySession != null && !primaryDisplaySession.IsAvailable) || (ThemeService.Current?.UseOpaquePanels ?? false) || backgroundClarity != BackgroundClarity || capturedScene == null || !CanCommitCapturedScene(capturedScene, capturedStamp, capturedSourceTicks))
							{
								return;
							}
							byte[] pixels = glassFrameResult2.Pixels;
							if (_compatibilityBackdrop != null && _handoffStarted == 0L)
							{
								_handoffStarted = Stopwatch.GetTimestamp();
							}
							if (transitionBackdrop)
							{
								if (!capturedScene.Equals(_frameScene))
								{
									_frame = null;
									_frameScene = null;
								}
								if (!TryUpdateBackdropBitmap(_transitionBackdrop, pixels, width, height, out WriteableBitmap bitmap))
								{
									return;
								}
								_transitionBackdrop = bitmap;
								_transitionBackdropScene = capturedScene;
								if (_transitionBackdropBounds != sampledFrameBounds)
								{
									InvalidateArrange();
								}
								_transitionBackdropBounds = sampledFrameBounds;
							}
							else
							{
								if (!capturedScene.Equals(_transitionBackdropScene))
								{
									_transitionBackdrop = null;
									_transitionBackdropScene = null;
								}
								if (!TryUpdateBackdropBitmap(_frame, pixels, width, height, out WriteableBitmap bitmap2))
								{
									return;
								}
								_frame = bitmap2;
								_frameScene = capturedScene;
								_sampledGlassPanelBounds = panelRect;
								_sampledGlassPanelRadius = radius;
								_sampledPanelBounds = sampledFrameBounds;
							}
							if (_compatibilityIsDesktopSnapshot && !capturedScene.Equals(_compatibilityScene))
							{
								_compatibilityBackdrop = null;
								_compatibilityStamp = null;
								_compatibilityIsDesktopSnapshot = false;
								_compatibilityScene = null;
								_handoffStarted = 0L;
							}
							_lastOpticsMilliseconds = glassFrameResult2.Milliseconds;
							if (!_compatibilityIsDesktopSnapshot || (_compatibilityScene?.Equals(_backdropScene) ?? false))
							{
								_materialRetentionUntil = (_relocationHandoffUntil = 0L);
							}
							_frameUsesAndroidGlass = useAndroidGlass;
							_materialStamp = ReadMaterialStamp();
							_opticalFrameTimestamps.Enqueue(Stopwatch.GetTimestamp());
							_lastSource = (background.IsCaptured ? null : source);
							_frameKey = key;
							_lastCaptureSequence = background.Sequence;
							_lastCaptureGeneration = background.CaptureGeneration;
							_lastCaptureSourceTicks = capturedSourceTicks;
							_lastCaptureScene = capturedScene;
							}
						}
						goto IL_18a0;
						IL_18a0:
						if (!transitionBackdrop && _frame != null)
						{
							_sampledPanelBounds = sampledFrameBounds;
							_sampledGlassPanelBounds = panelRect;
							_sampledGlassPanelRadius = radius;
						}
						bool flag7 = source != null && _contentChangedAt != 0L && Stopwatch.GetElapsedTime(_contentChangedAt) >= TimeSpan.FromMilliseconds(500L) && !hovering && _hover < 0.001 && !_containerAnimationActive && _transitionProgress >= 1.0 && _handoffStarted == 0L && _relocationHandoffUntil == 0;
						RefreshOpticalRefreshRate();
						LiquidGlassSurface liquidGlassSurface = this;
						TimeSpan timerInterval;
						if (flag7)
						{
							timerInterval = TimeSpan.FromMilliseconds(100L);
						}
						else
						{
							timerInterval = ((!interactive && source != null) ? TimeSpan.FromMilliseconds(33.333333333333336) : GlassBackgroundLayer.LiveFrameInterval);
						}
						liquidGlassSurface.SetTimerInterval(timerInterval);
						if (!flag7)
						{
							UpdateTransitionLayer();
							InvalidateVisual();
						}
						return;
					}
					SetTimerInterval(TimeSpan.FromMilliseconds(200L));
					_background.Dispose();
					_frame = null;
					_lastSource = null;
				}
				return;
			}
		}
		_worker.Invalidate();
		_worker.ClearCachedBuffers();
		SetTimerInterval(TimeSpan.FromMilliseconds(200L));
		_lastSource = null;
		if (_revealCaptureUntil > Stopwatch.GetTimestamp())
		{
			_timer.Interval = TimeSpan.FromMilliseconds(100L);
			_timer.Start();
		}
		else
		{
			_revealCaptureUntil = 0L;
			_background.Suspend();
			_timer.Stop();
		}
		StopRenderClock();
	}

	internal static bool TryUpdateBackdropBitmap(BitmapSource? current, byte[] pixels, int width, int height, out WriteableBitmap bitmap)
	{
		if (width <= 0 || height <= 0 || pixels.Length != (long)width * (long)height * 4)
		{
			throw new ArgumentException("Invalid bitmap submission extent.");
		}
		bitmap = ((current is WriteableBitmap { IsFrozen: false } writeableBitmap && writeableBitmap.PixelWidth == width && writeableBitmap.PixelHeight == height) ? writeableBitmap : new WriteableBitmap(width, height, 96.0, 96.0, PixelFormats.Bgra32, null));
		if (!bitmap.TryLock(new Duration(TimeSpan.Zero)))
		{
			return false;
		}
		try
		{
			int num = checked(width * 4);
			if (bitmap.BackBufferStride == num)
			{
				Marshal.Copy(pixels, 0, bitmap.BackBuffer, pixels.Length);
			}
			else
			{
				for (int i = 0; i < height; i++)
				{
					Marshal.Copy(pixels, i * num, bitmap.BackBuffer + i * bitmap.BackBufferStride, num);
				}
			}
			bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
			return true;
		}
		finally
		{
			bitmap.Unlock();
		}
	}

	private static double[] CaptureWallpaperShoulderWeights(GlassWindow owner, int width, int height, Size sampleSizeDip, Rect glassBoundsDip, double radius)
	{
		double[] array = new double[width * height];
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				owner.TryGetGlassPanelEdge(glassBoundsDip, radius, new Point(((double)j + 0.5) * sampleSizeDip.Width / (double)width, ((double)i + 0.5) * sampleSizeDip.Height / (double)height), out var depth, out var _);
				array[i * width + j] = Math.Clamp(1.0 - depth / 12.0, 0.0, 1.0);
			}
		}
		return array;
	}

	private static void ApplyWallpaperShoulder(byte[] pixels, double[] weights)
	{
		for (int i = 0; i < weights.Length; i++)
		{
			double num = weights[i];
			int num2 = i * 4 + 3;
			pixels[num2] = (byte)((double)(int)pixels[num2] * 0.32 * num * num);
		}
	}

	protected override void OnRender(DrawingContext drawingContext)
	{
		LiquidGlassSurface liquidGlassSurface = this;
		if (IsPlainGlass)
		{
			RenderPlainGlass(drawingContext);
			return;
		}
		EnsureBackdropScene();
		UpdateTransitionLayer();
		SharedMaterialChanged?.Invoke(this, EventArgs.Empty);
		DrawingContext dc = drawingContext;
		base.OnRender(dc);
		if (ActualWidth < 1.0 || ActualHeight < 1.0)
		{
			return;
		}
		Rect rect = _panelShape ?? new Rect(0.0, 0.0, ActualWidth, ActualHeight);
		GlassWindow glass = Window.GetWindow(this) as GlassWindow;
		Rect? panelShape = _panelShape;
		double radius = (panelShape.HasValue ? _panelRadius : (glass?.PanelCornerRadius.TopLeft ?? 40.0));
		_lastRenderTicks = Stopwatch.GetTimestamp();
		_renderCount++;
		ThemeService? current = ThemeService.Current;
		_lastRenderMaterialReady = (current != null && current.UseOpaquePanels) || _background.RequiresBasicBackground || (_frame != null && _sampledGlassPanelBounds == rect && _sampledGlassPanelRadius == radius) || (_transitionBackdrop != null && _transitionBackdropBounds.Contains(rect)) || (_compatibilityBackdrop != null && _compatibilityLayer.Opacity > 0.0 && _compatibilityLayer.RenderSize.Width > 0.0 && _compatibilityLayer.RenderSize.Height > 0.0);
		int num;
		if (TransparentWhileLoading)
		{
			ThemeService? current2 = ThemeService.Current;
			if (current2 == null || !current2.UseOpaquePanels)
			{
				num = (((_frame == null || !(_sampledGlassPanelBounds == rect) || _sampledGlassPanelRadius != radius) && (_transitionBackdrop == null || !_transitionBackdropBounds.Contains(rect)) && (!_compatibilityIsDesktopSnapshot || _compatibilityBackdrop == null || !(_compatibilityLayer.Opacity > 0.0))) ? 1 : 0);
				goto IL_0305;
			}
		}
		num = 0;
		goto IL_0305;
		IL_051b:
		bool flag;
		if (_firstVisibleMaterialSource == null)
		{
			string firstVisibleMaterialSource;
			if (!flag)
			{
				if (!_compatibilityIsDesktopSnapshot || _compatibilityBackdrop == null || !(_compatibilityLayer.Opacity > 0.0))
				{
					if (_frame == null && _transitionBackdrop == null)
					{
						ThemeService? current3 = ThemeService.Current;
						firstVisibleMaterialSource = ((current3 != null && current3.UseOpaquePanels) ? "OpaquePreference" : "AmbientCompatibility");
					}
					else
					{
						firstVisibleMaterialSource = "CompletedLiveMaterial";
					}
				}
				else
				{
					firstVisibleMaterialSource = "CurrentDesktopSnapshot";
				}
			}
			else
			{
				firstVisibleMaterialSource = "TransparentPending";
			}
			_firstVisibleMaterialSource = firstVisibleMaterialSource;
		}
		if (flag)
		{
			return;
		}
		double num2 = Math.Clamp(_transitionProgress, 0.0, 1.0);
		double highlightScale = 0.48 + 0.52 * num2;
		LiquidGlassDebugView view = LiquidGlassDebugState.View;
		bool flag2 = ThemeService.Current?.IsDark ?? true;
		if (view != LiquidGlassDebugView.None && (!LiquidGlassFinishComposer.IsComparisonView(view) || LiquidGlassFinishComposer.IsBrdfComponentView(view)))
		{
			if (_frame != null)
			{
				dc.DrawImage(_frame, _sampledPanelBounds);
			}
			Pen pen = new Pen(new SolidColorBrush(Color.FromArgb(235, byte.MaxValue, byte.MaxValue, byte.MaxValue)), 1.0);
			dc.DrawGeometry(null, pen, CreatePanel(rect, radius));
			if (LiquidGlassDebugState.ShowPerformanceOverlay)
			{
				DrawMetricsOverlay(dc, rect);
			}
			return;
		}
		dc.PushClip(CreatePanel(rect, radius));
		ThemeService? current4 = ThemeService.Current;
		if ((current4 != null && current4.UseOpaquePanels) || BackdropRecovery.RequiresBasicRendering)
		{
			dc.DrawRectangle(new SolidColorBrush(flag2 ? Color.FromRgb(28, 29, 34) : Color.FromRgb(246, 247, 250)), null, rect);
			if (_background.RequiresBasicBackground)
			{
				DrawCompatibilityNotice(dc, rect, "基础背景 · 正在恢复实时效果", flag2);
			}
			dc.Pop();
			return;
		}
		bool flag3 = view == LiquidGlassDebugView.None;
		if (flag3)
		{
			bool flag4 = _frameUsesAndroidGlass;
			if (!flag4)
			{
				bool flag5 = _frame == null;
				if (flag5)
				{
					GlassMaterialMode material = Material;
					bool flag6 = ((material == GlassMaterialMode.AndroidLiquidGlass || material == GlassMaterialMode.FrostedGlass) ? true : false);
					flag5 = flag6;
				}
				flag4 = flag5;
			}
			flag3 = flag4;
		}
		bool flag7 = flag3;
		bool flag8 = _frame != null && _sampledGlassPanelBounds == rect && _sampledGlassPanelRadius == radius;
		panelShape = _transitionCaptureBounds;
		bool flag9 = panelShape.HasValue || !flag8;
		bool flag10 = flag7 && _transitionBackdrop != null && _transitionBackdropBounds.Contains(rect);
		if ((flag7 & flag9) && !flag10)
		{
			byte b = (byte)(flag2 ? 18 : 250);
			dc.DrawRectangle(new SolidColorBrush(Color.FromArgb((byte)Math.Round(40.800000000000004 * AndroidLiquidGlassRenderer.BodyTintScale(BackgroundClarity, UseLaunchpadOptics)), b, b, b)), null, rect);
		}
		if (flag8)
		{
			if (flag7)
			{
				panelShape = _transitionCaptureBounds;
				if (panelShape.HasValue)
				{
					goto IL_0911;
				}
			}
			dc.DrawImage(_frame, _sampledPanelBounds);
		}
		goto IL_0911;
		IL_0305:
		flag = (byte)num != 0;
		if (flag)
		{
			_lastRenderMaterialReady = false;
		}
		flag3 = _firstVisibleRenderMaterialReady == true;
		if (!_firstVisibleRenderMaterialReady.HasValue)
		{
			flag3 = _lastRenderMaterialReady;
			_firstVisibleRenderMaterialReady = flag3;
		}
		string text;
		if (!flag)
		{
			ThemeService? current5 = ThemeService.Current;
			if (current5 != null && current5.UseOpaquePanels)
			{
				text = "Opaque";
			}
			else if (BackdropRecovery.RequiresBasicRendering || _background.RequiresBasicBackground)
			{
				text = "Basic";
			}
			else if ((_frame != null && _sampledGlassPanelBounds == rect && _sampledGlassPanelRadius == radius) || (_transitionBackdrop != null && _transitionBackdropBounds.Contains(rect)))
			{
				text = "Live";
			}
			else
			{
				text = ((_compatibilityIsDesktopSnapshot && _compatibilityBackdrop != null) ? "DesktopSnapshot" : "Ambient");
			}
		}
		else
		{
			text = "TransparentPending";
		}
		string text2 = text;
		int num3;
		if (_diagnosticMaterial != text2)
		{
			flag3 = _diagnosticMaterial == "Live";
			if (flag3)
			{
				bool flag4;
				switch (text2)
				{
				case "Ambient":
				case "Basic":
				case "TransparentPending":
					flag4 = true;
					break;
				default:
					flag4 = false;
					break;
				}
				flag3 = flag4;
			}
			if (flag3)
			{
				Window window = Window.GetWindow(this);
				if (window != null && window.IsVisible && IsVisible)
				{
					num3 = ((IsBackdropActive?.Invoke() ?? true) ? 1 : 0);
					goto IL_04d8;
				}
			}
			num3 = 0;
			goto IL_04d8;
		}
		goto IL_051b;
		IL_04d8:
		bool flag11 = (byte)num3 != 0;
		RecordSurfaceTrace(flag11 ? "MaterialLoss" : "MaterialState", (_diagnosticMaterial ?? "Initial") + "To" + text2, text2);
		_diagnosticMaterial = text2;
		goto IL_051b;
		IL_0911:
		if (flag7)
		{
			if (!flag8 && !flag10)
			{
				DrawWaitingRim(dc, CreatePanel(rect, radius), highlightScale);
			}
			if (LiquidGlassDebugState.ShowPerformanceOverlay)
			{
				DrawMetricsOverlay(dc, rect);
			}
			if (BackdropSource == GlassBackdropSource.CompatibleScene)
			{
				DrawCompatibilityNotice(dc, rect, "兼容场景玻璃 · 壁纸为静态", flag2);
			}
			else if (BackdropSource == GlassBackdropSource.Wallpaper)
			{
				DrawCompatibilityNotice(dc, rect, "壁纸玻璃 · 不反映后方窗口", flag2);
			}
			dc.Pop();
			return;
		}
		LinearGradientBrush brush = new LinearGradientBrush(new GradientStopCollection
		{
			new GradientStop(Color.FromArgb(ScaleAlpha(5.0, highlightScale), byte.MaxValue, byte.MaxValue, byte.MaxValue), 0.0),
			new GradientStop(Colors.Transparent, 0.35),
			new GradientStop(Color.FromArgb(ScaleAlpha(flag2 ? 6 : 3, highlightScale), 12, 18, 30), 1.0)
		}, new Point(0.0, 0.0), new Point(0.0, 1.0));
		dc.DrawRectangle(brush, null, rect);
		RadialGradientBrush brush2 = new RadialGradientBrush
		{
			Center = new Point(_light, -0.12),
			GradientOrigin = new Point(_light, -0.12),
			RadiusX = 0.38,
			RadiusY = 0.58,
			GradientStops = new GradientStopCollection
			{
				new GradientStop(Color.FromArgb(ScaleAlpha(7.0 + _hover * 4.0, highlightScale), byte.MaxValue, byte.MaxValue, byte.MaxValue), 0.0),
				new GradientStop(Colors.Transparent, 1.0)
			}
		};
		dc.DrawRectangle(brush2, null, rect);
		LinearGradientBrush brush3 = new LinearGradientBrush(new GradientStopCollection
		{
			new GradientStop(Color.FromArgb(ScaleAlpha(70.0, highlightScale), byte.MaxValue, byte.MaxValue, byte.MaxValue), 0.0),
			new GradientStop(Color.FromArgb(ScaleAlpha(16.0, highlightScale), byte.MaxValue, byte.MaxValue, byte.MaxValue), 0.24),
			new GradientStop(Color.FromArgb(ScaleAlpha(24.0, highlightScale), 18, 28, 44), 0.58),
			new GradientStop(Color.FromArgb(ScaleAlpha(42.0, highlightScale), byte.MaxValue, byte.MaxValue, byte.MaxValue), 1.0)
		}, new Point(_light * 0.5, 0.0), new Point(1.0 - _light * 0.5, 1.0));
		Rect rimBounds = rect;
		rimBounds.Inflate(-0.55, -0.55);
		if (rimBounds.Width > 0.0 && rimBounds.Height > 0.0)
		{
			dc.DrawGeometry(null, new Pen(brush3, 0.8 * highlightScale), CreatePanel(rimBounds, Math.Max(0.0, radius - 0.55)));
			DrawGlint(new Point(0.12 + _light * 0.35, 0.015), 0.4, 0.23, 198, 1.15);
			DrawGlint(new Point(0.015, 0.27), 0.1, 0.33, 104, 1.0);
			DrawGlint(new Point(0.96, 0.8), 0.17, 0.3, 118, 0.85);
		}
		Rect bounds = rect;
		bounds.Inflate(-2.05, -2.05);
		if (bounds.Width > 0.0 && bounds.Height > 0.0)
		{
			dc.DrawGeometry(null, new Pen(brush2, 2.4 * highlightScale), CreatePanel(bounds, Math.Max(0.0, radius - 2.05)));
			LinearGradientBrush brush4 = new LinearGradientBrush(Color.FromArgb(ScaleAlpha(16.0, highlightScale), 20, 32, 48), Color.FromArgb(ScaleAlpha(18.0, highlightScale), byte.MaxValue, byte.MaxValue, byte.MaxValue), 90.0);
			dc.DrawGeometry(null, new Pen(brush4, 0.75), CreatePanel(bounds, Math.Max(0.0, radius - 2.05)));
		}
		if (LiquidGlassDebugState.ShowPerformanceOverlay)
		{
			DrawMetricsOverlay(dc, rect);
		}
		dc.Pop();
		Geometry CreatePanel(Rect rect2, double cornerRadius)
		{
			Rect? panelShape2 = _panelShape;
			if (panelShape2.HasValue && rect2 == rect && cornerRadius == radius)
			{
				return _panelGeometry ?? (_panelGeometry = ((glass == null) ? SquircleGlassGeometry.Create(rect2, cornerRadius) : glass.CreateGlassPanelGeometry(rect2, cornerRadius)));
			}
			if (glass != null)
			{
				return glass.CreateGlassPanelGeometry(rect2, cornerRadius);
			}
			return SquircleGlassGeometry.Create(rect2, cornerRadius);
		}
		void DrawGlint(Point center, double rx, double ry, byte alpha, double thickness)
		{
			RadialGradientBrush brush5 = new RadialGradientBrush
			{
				Center = center,
				GradientOrigin = center,
				RadiusX = rx,
				RadiusY = ry,
				GradientStops = new GradientStopCollection
				{
					new GradientStop(Color.FromArgb(ScaleAlpha((int)alpha, highlightScale), byte.MaxValue, byte.MaxValue, byte.MaxValue), 0.0),
					new GradientStop(Color.FromArgb(ScaleAlpha((double)(int)alpha * 0.4, highlightScale), 235, 245, byte.MaxValue), 0.45),
					new GradientStop(Colors.Transparent, 1.0)
				}
			};
			dc.DrawGeometry(null, new Pen(brush5, thickness), CreatePanel(rimBounds, Math.Max(0.0, radius - 0.55)));
		}
	}

	private void DrawCompatibilityNotice(DrawingContext dc, Rect rect, string text, bool dark)
	{
		if (!(rect.Height < 120.0) && !(rect.Width < 300.0))
		{
			FormattedText formattedText = new FormattedText(text, CultureInfo.GetCultureInfo("zh-CN"), FlowDirection.LeftToRight, new Typeface("Microsoft YaHei UI"), 11.0, dark ? Brushes.WhiteSmoke : Brushes.DimGray, VisualTreeHelper.GetDpi(this).PixelsPerDip);
			dc.DrawText(formattedText, new Point(Math.Max(rect.Left + 12.0, rect.Right - formattedText.Width - 16.0), rect.Bottom - formattedText.Height - 12.0));
		}
	}

	protected override Visual GetVisualChild(int index)
	{
		return index switch
		{
			0 => (Visual)_transitionLayer, 
			1 => _compatibilityLayer, 
			_ => throw new ArgumentOutOfRangeException("index"), 
		};
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		_transitionLayer.Measure(availableSize);
		_compatibilityLayer.Measure(availableSize);
		return base.MeasureOverride(availableSize);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		UpdateTransitionLayer(finalSize);
		_transitionLayer.Arrange((_transitionBackdropBounds.Width > 0.0) ? _transitionBackdropBounds : new Rect(finalSize));
		_compatibilityLayer.Arrange(new Rect(finalSize));
		return finalSize;
	}

	private void UpdateTransitionLayer(Size? layoutSize = null)
	{
		if (IsPlainGlass)
		{
			_transitionLayer.SetBackdrop(null);
			_transitionLayer.Opacity = 0.0;
			_compatibilityLayer.SetBackdrop(null);
			_compatibilityLayer.Opacity = 0.0;
			return;
		}
		Size size = layoutSize ?? RenderSize;
		Rect rect = _panelShape ?? new Rect(size);
		Rect? panelShape = _panelShape;
		double num = (panelShape.HasValue ? _panelRadius : ((Window.GetWindow(this) as GlassWindow)?.PanelCornerRadius.TopLeft ?? 40.0));
		bool flag = _frame != null && _sampledGlassPanelBounds == rect && _sampledGlassPanelRadius == num;
		panelShape = _transitionCaptureBounds;
		int num2;
		if ((panelShape.HasValue || !flag) && !_background.RequiresBasicBackground && _transitionBackdrop != null && _transitionBackdropBounds.Contains(rect) && LiquidGlassDebugState.View == LiquidGlassDebugView.None)
		{
			ThemeService? current = ThemeService.Current;
			num2 = ((current == null || !current.UseOpaquePanels) ? 1 : 0);
		}
		else
		{
			num2 = 0;
		}
		bool flag2 = (byte)num2 != 0;
		if (flag2)
		{
			GlassMaterialMode material = Material;
			bool flag3 = ((material == GlassMaterialMode.AndroidLiquidGlass || material == GlassMaterialMode.FrostedGlass) ? true : false);
			flag2 = flag3;
		}
		bool flag4 = flag2;
		_transitionLayer.SetBackdrop(flag4 ? _transitionBackdrop : null);
		_transitionLayer.Opacity = (flag4 ? 1 : 0);
		bool frosted = Material == GlassMaterialMode.FrostedGlass;
		if (flag4)
		{
			_transitionLayer.SetPanel(rect, _transitionBackdropBounds, num, 0.6 + 0.4 * _transitionProgress, UseLaunchpadOptics, frosted);
		}
		bool flag5 = !_background.RequiresBasicBackground && (flag4 | flag);
		ThemeService? current2 = ThemeService.Current;
		double num3;
		if ((current2 == null || !current2.UseOpaquePanels) && !BackdropRecovery.RequiresBasicRendering && LiquidGlassDebugState.View == LiquidGlassDebugView.None && (!flag5 || _handoffStarted != 0) && size.Width > 0.0 && size.Height > 0.0)
		{
			PrepareCompatibleMaterial(size);
			num3 = ((!flag5 || _handoffStarted == 0L) ? 1.0 : Math.Clamp(1.0 - Stopwatch.GetElapsedTime(_handoffStarted).TotalMilliseconds / 100.0, 0.0, 1.0));
			if (_background.UsesTranslucentCaptureFallback && !_compatibilityIsDesktopSnapshot && (UseLaunchpadOptics || IsBackdropActive == null))
			{
				num3 *= 0.18;
			}
			_compatibilityLayer.SetBackdrop(_compatibilityBackdrop);
			if (_compatibilityLayer.Clip != null)
			{
				(Rect, double) compatibilityClipKey = _compatibilityClipKey;
				Rect rect2 = rect;
				double num4 = num;
				if (!(compatibilityClipKey.Item1 != rect2) && compatibilityClipKey.Item2 == num4)
				{
					goto IL_0408;
				}
			}
			_compatibilityClipKey = (Panel: rect, Radius: num);
			_compatibilityLayer.Clip = _panelGeometry ?? ((Window.GetWindow(this) is GlassWindow glassWindow) ? glassWindow.CreateGlassPanelGeometry(rect, num) : SquircleGlassGeometry.Create(rect, num));
			goto IL_0408;
		}
		_compatibilityLayer.Opacity = 0.0;
		_compatibilityLayer.SetBackdrop(null);
		return;
		IL_0408:
		_compatibilityLayer.SetPanel(rect, _compatibilityBounds, num, 0.6 + 0.4 * _transitionProgress, UseLaunchpadOptics, frosted);
		_compatibilityLayer.Opacity = num3;
		if (num3 == 0.0)
		{
			_handoffStarted = 0L;
			_compatibilityBackdrop = null;
			_compatibilityStamp = null;
			_compatibilityIsDesktopSnapshot = false;
			_compatibilityScene = null;
			_compatibilityLayer.SetBackdrop(null);
		}
	}

	private static void RenderMaterialBackdropInto(GlassMaterialMode material, byte[] source, byte[] pixels, byte[] scratch, int width, int height, Size size, bool dark, double strength, double clarity, bool launchpad)
	{
		if (material == GlassMaterialMode.FrostedGlass)
		{
			AndroidLiquidGlassRenderer.RenderFrostedBackdropInto(source, pixels, scratch, width, height, size, dark, strength, clarity);
		}
		else
		{
			AndroidLiquidGlassRenderer.RenderTransitionBackdropInto(source, pixels, scratch, width, height, size, dark, strength, 0.16, clarity, launchpad);
		}
	}

	private MaterialStamp ReadMaterialStamp()
	{
		Point origin = default;
		DpiScale dpi = VisualTreeHelper.GetDpi(this);
		Point? revealOrigin = _revealOrigin;
		if (revealOrigin.HasValue)
		{
			Point valueOrDefault = revealOrigin.GetValueOrDefault();
			Window window = Window.GetWindow(this);
			if (window != null && !window.IsVisible)
			{
				origin = valueOrDefault;
				goto IL_00a2;
			}
		}
		if (PresentationSource.FromVisual(this) != null)
		{
			origin = PointToScreen(default);
		}
		else
		{
			Window window2 = Window.GetWindow(this);
			if (window2 != null && PresentationSource.FromVisual(window2) != null)
			{
				origin = window2.PointToScreen(default);
				dpi = VisualTreeHelper.GetDpi(window2);
			}
		}
		goto IL_00a2;
		IL_00a2:
		return new MaterialStamp(origin, _transitionCaptureBounds ?? CaptureBoundsInWindow ?? new Rect(RenderSize), dpi, Material, ThemeService.Current?.IsDark ?? true, BackgroundClarity, UseLaunchpadOptics, LiquidGlassDebugState.SamplingParameters, LiquidGlassDebugState.WeightParameters, LiquidGlassDebugState.LodParameters, LiquidGlassDebugState.FinishingParameters, LiquidGlassDebugState.BrdfParameters, LiquidGlassDebugState.DispersionParameters, BackdropRecovery.Generation, DesktopOnlyBackdrop);
	}

	private static bool MatchesExceptOrigin(MaterialStamp left, MaterialStamp right)
	{
		return left with
		{
			Origin = right.Origin
		} == right;
	}

	private void PrepareCompatibleMaterial(Size size)
	{
		MaterialStamp materialStamp = ReadMaterialStamp();
		Rect rect = new Rect(size);
		if (_compatibilityBackdrop != null && _compatibilityStamp == materialStamp && _compatibilityBounds == rect)
		{
			return;
		}
		if (_compatibilityIsDesktopSnapshot)
		{
			RevealMaterialInvalidationReason = $"origin {_compatibilityStamp?.Origin} => {materialStamp.Origin}; dpi {_compatibilityStamp?.Dpi.DpiScaleX} => {materialStamp.Dpi.DpiScaleX}; bounds {_compatibilityBounds} => {rect}; capture {_compatibilityStamp?.CaptureBounds} => {materialStamp.CaptureBounds}";
		}
		_compatibilityStamp = materialStamp;
		_compatibilityBounds = rect;
		if (TransparentWhileLoading)
		{
			_compatibilityBackdrop = null;
			_compatibilityIsDesktopSnapshot = false;
			_compatibilityScene = null;
			_handoffStarted = 0L;
			return;
		}
		byte[] array = new byte[3072];
		for (int i = 0; i < 24; i++)
		{
			for (int j = 0; j < 32; j++)
			{
				int num = (i * 32 + j) * 4;
				byte b = (byte)(12 * j / 32 + 14 * i / 24);
				array[num] = (byte)((materialStamp.Dark ? 51 : 208) + b);
				array[num + 1] = (byte)((materialStamp.Dark ? 43 : 205) + b);
				array[num + 2] = (byte)((materialStamp.Dark ? 37 : 200) + b);
				array[num + 3] = ((UseLaunchpadOptics || IsBackdropActive == null) ? byte.MaxValue : ((byte)Math.Round(40.800000000000004)));
			}
		}
		byte[] pixels = new byte[array.Length];
		byte[] scratch = new byte[array.Length];
		RenderMaterialBackdropInto(materialStamp.Material, array, pixels, scratch, 32, 24, rect.Size, materialStamp.Dark, 1.0, materialStamp.Clarity, materialStamp.Launchpad);
		BitmapSource bitmapSource = BitmapSource.Create(32, 24, 96.0, 96.0, PixelFormats.Bgra32, null, pixels, 128);
		bitmapSource.Freeze();
		_compatibilityBackdrop = bitmapSource;
		_compatibilityIsDesktopSnapshot = false;
		_compatibilityScene = null;
		_handoffStarted = 0L;
		if (!UseLaunchpadOptics && IsBackdropActive == null && !_performanceSleeping && !_wallpaperPreparationPending && PresentationSource.FromVisual(this) != null)
		{
			PrepareWallpaperMaterial(materialStamp, rect);
		}
	}

	public void PrepareRevealMaterial(PixelRect host, PixelRect panel, double dpi)
	{
		if (IsPlainGlass)
		{
			LauncherRevealRepair.AfterPrepare(this);
			return;
		}
		_lastRevealCaptureMilliseconds = (_lastRevealRemapMilliseconds = (_lastRevealFilterMilliseconds = 0.0));
		Window window = Window.GetWindow(this);
		if (window != null)
		{
			if (window.IsVisible)
			{
				Func<bool>? isBackdropActive = IsBackdropActive;
				if (isBackdropActive == null || isBackdropActive())
				{
					goto IL_00ac;
				}
			}
			ThemeService? current = ThemeService.Current;
			if ((current == null || !current.UseOpaquePanels) && !BackdropRecovery.RequiresBasicRendering && host.Width > 0 && host.Height > 0)
			{
				Size size = new Size((double)host.Width * 96.0 / dpi, (double)host.Height * 96.0 / dpi);
				_revealOrigin = new Point(host.X, host.Y);
				_revealHost = host;
				Measure(size);
				Arrange(new Rect(size));
				EnsureBackdropScene(revealing: true);
				PixelRect region;
				using (GlassBackgroundFrame glassBackgroundFrame = PrimeRevealCapture(host, dpi, out region))
				{
					if (_frame == null && _transitionBackdrop == null)
					{
						if (!_compatibilityIsDesktopSnapshot)
						{
							goto IL_0197;
						}
						GlassBackdropScene? compatibilityScene = _compatibilityScene;
						if (compatibilityScene == null || !compatibilityScene.Equals(_backdropScene) || !(_compatibilityStamp == ReadMaterialStamp()))
						{
							goto IL_0197;
						}
					}
					goto end_IL_0133;
					IL_0197:
					PixelRect arg = new PixelRect(Math.Max(host.X, panel.X), Math.Max(host.Y, panel.Y), Math.Min(host.Right, panel.Right) - Math.Max(host.X, panel.X), Math.Min(host.Bottom, panel.Bottom) - Math.Max(host.Y, panel.Y));
					if (arg.Width > 0 && arg.Height > 0)
					{
						double num = Math.Min(1.0, Math.Min(2048.0 / (double)Math.Max(host.Width, host.Height), Math.Sqrt(320000.0 / ((double)host.Width * (double)host.Height))));
						int num2 = Math.Max(1, (int)Math.Floor((double)host.Width * num));
						int num3 = Math.Max(1, (int)Math.Floor((double)host.Height * num));
						int num4 = Math.Max(1, (int)Math.Floor((double)arg.Width * num));
						int num5 = Math.Max(1, (int)Math.Floor((double)arg.Height * num));
						GlassBackdropScene backdropScene = _backdropScene;
						long timestamp = Stopwatch.GetTimestamp();
						byte[] array = (DesktopOnlyBackdrop ? null : CaptureRevealBackdrop(arg, num4, num5));
						_lastRevealCaptureMilliseconds = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
						long timestamp2 = Stopwatch.GetTimestamp();
						bool flag = false;
						bool flag2 = (array == null || array.Length != num4 * num5 * 4 || IsSolidBlack(array)) && !UseLaunchpadOptics && (glassBackgroundFrame?.IsCaptured ?? false);
						if (flag2)
						{
							GlassBackdropSource source = glassBackgroundFrame.Source;
							bool flag3 = ((source == GlassBackdropSource.LiveDesktop || source == GlassBackdropSource.CompatibleScene) ? true : false);
							flag2 = flag3;
						}
						if (flag2)
						{
							GlassBackgroundLayer.CaptureTiming lastCaptureTiming = _background.LastCaptureTiming;
							if ((object)lastCaptureTiming != null)
							{
								long num6 = Stopwatch.GetTimestamp() - lastCaptureTiming.SourceTicks;
								if (num6 >= 0 && num6 <= Stopwatch.Frequency * 120 / 1000)
								{
									flag = true;
									array = new byte[num4 * num5 * 4];
									for (int i = 0; i < num5; i++)
									{
										int num7 = Math.Clamp((int)(((double)arg.Y + ((double)i + 0.5) * (double)arg.Height / (double)num5 - (double)region.Y) * (double)glassBackgroundFrame.Height / (double)region.Height), 0, glassBackgroundFrame.Height - 1);
										for (int j = 0; j < num4; j++)
										{
											int num8 = Math.Clamp((int)(((double)arg.X + ((double)j + 0.5) * (double)arg.Width / (double)num4 - (double)region.X) * (double)glassBackgroundFrame.Width / (double)region.Width), 0, glassBackgroundFrame.Width - 1);
											glassBackgroundFrame.Pixels.AsSpan((num7 * glassBackgroundFrame.Width + num8) * 4, 4).CopyTo(array.AsSpan((i * num4 + j) * 4, 4));
										}
									}
								}
							}
						}
						if (array != null && array.Length == num4 * num5 * 4 && (flag || !IsSolidBlack(array)) && backdropScene != null && backdropScene.Equals(ReadBackdropScene()))
						{
							byte[] array2 = new byte[num2 * num3 * 4];
							for (int k = 0; k < num3; k++)
							{
								int num9 = Math.Clamp((int)(((double)host.Y + ((double)k + 0.5) * (double)host.Height / (double)num3 - (double)arg.Y) * (double)num5 / (double)arg.Height), 0, num5 - 1);
								for (int l = 0; l < num2; l++)
								{
									int num10 = Math.Clamp((int)(((double)host.X + ((double)l + 0.5) * (double)host.Width / (double)num2 - (double)arg.X) * (double)num4 / (double)arg.Width), 0, num4 - 1);
									array.AsSpan((num9 * num4 + num10) * 4, 4).CopyTo(array2.AsSpan((k * num2 + l) * 4, 4));
								}
							}
							MaterialStamp materialStamp = ReadMaterialStamp();
							_lastRevealRemapMilliseconds = Stopwatch.GetElapsedTime(timestamp2).TotalMilliseconds;
							byte[] pixels = new byte[array2.Length];
							byte[] scratch = new byte[array2.Length];
							long timestamp3 = Stopwatch.GetTimestamp();
							RenderMaterialBackdropInto(materialStamp.Material, array2, pixels, scratch, num2, num3, size, materialStamp.Dark, 1.0, materialStamp.Clarity, materialStamp.Launchpad);
							_lastRevealFilterMilliseconds = Stopwatch.GetElapsedTime(timestamp3).TotalMilliseconds;
							BitmapSource bitmapSource = BitmapSource.Create(num2, num3, 96.0, 96.0, PixelFormats.Bgra32, null, pixels, num2 * 4);
							bitmapSource.Freeze();
							_wallpaperCancellation?.Cancel();
							_compatibilityBackdrop = bitmapSource;
							_compatibilityStamp = materialStamp;
							_compatibilityBounds = new Rect(size);
							_compatibilityScene = backdropScene;
							_compatibilityIsDesktopSnapshot = true;
							_handoffStarted = 0L;
							_preparedDesktopSnapshots++;
							RevealMaterialInvalidationReason = null;
							UpdateTransitionLayer(size);
							InvalidateVisual();
						}
					}
					end_IL_0133:;
				}
				LauncherRevealRepair.AfterPrepare(this);
				return;
			}
		}
		goto IL_00ac;
		IL_00ac:
		LauncherRevealRepair.AfterPrepare(this);
	}

	private byte[]? CaptureHiddenDesktop(PixelRect region, int width, int height)
	{
		using DesktopRegionSampler desktopRegionSampler = new DesktopRegionSampler();
		Window window = Window.GetWindow(this);
		nint num;
		if (window != null && window.IsVisible)
		{
			Func<bool>? isBackdropActive = IsBackdropActive;
			if (isBackdropActive != null && !isBackdropActive())
			{
				num = new WindowInteropHelper(window).Handle;
				goto IL_0050;
			}
		}
		num = 0;
		goto IL_0050;
		IL_0050:
		nint emptyOwner = num;
		byte[] pixels;
		return desktopRegionSampler.TryCaptureBeforeReveal(region, width, height, emptyOwner, out pixels) ? pixels : null;
	}

	private GlassBackgroundFrame? PrimeRevealCapture(PixelRect host, double dpi, out PixelRect region)
	{
		region = default;
		if (_performanceSleeping || !(Window.GetWindow(this) is GlassWindow { SurfaceMode: GlassSurfaceMode.OpticalGlass } glassWindow))
		{
			return null;
		}
		double num = dpi / 96.0;
		Rect rect = _transitionCaptureBounds ?? new Rect(0.0, 0.0, (double)host.Width / num, (double)host.Height / num);
		int num2 = (int)Math.Floor((double)host.X + (rect.Left - 48.0) * num);
		int num3 = (int)Math.Floor((double)host.Y + (rect.Top - 48.0) * num);
		int num4 = (int)Math.Ceiling((double)host.X + (rect.Right + 48.0) * num);
		int num5 = (int)Math.Ceiling((double)host.Y + (rect.Bottom + 48.0) * num);
		PixelRect rect2 = new PixelRect(num2, num3, num4 - num2, num5 - num3);
		DisplayInfo displayInfo = glassWindow.PrimaryDisplaySession?.Display ?? DisplayService.Primary();
		if ((object)displayInfo == null)
		{
			return null;
		}
		region = ClipRegion(rect2, displayInfo.Bounds);
		if (region.Width <= 0 || region.Height <= 0)
		{
			return null;
		}
		double num6 = (UseLaunchpadOptics ? (320000.0 + 680000.0 * Math.Clamp((BackgroundClarity - 0.5) * 2.0, 0.0, 1.0)) : 320000.0);
		double num7 = Math.Min(1.0, Math.Min(2048.0 / (double)Math.Max(rect2.Width, rect2.Height), Math.Sqrt(num6 / ((double)rect2.Width * (double)rect2.Height))));
		GlassBackgroundFrame? result = _background.Capture(region, Math.Max(1, (int)Math.Floor((double)region.Width * num7)), Math.Max(1, (int)Math.Floor((double)region.Height * num7)), _panelRadius, allowDesktop: true, DesktopOnlyBackdrop);
		_revealCaptureUntil = Stopwatch.GetTimestamp() + Stopwatch.Frequency;
		_timer.Interval = TimeSpan.FromMilliseconds(100L);
		_timer.Start();
		return result;
	}

	private void PrepareWallpaperMaterial(MaterialStamp stamp, Rect bounds)
	{
		_wallpaperPreparationPending = true;
		CancellationTokenSource cancellation = new CancellationTokenSource();
		_wallpaperCancellation = cancellation;
		CancellationToken token = cancellation.Token;
		long generation = _lifecycleGeneration;
		GlassBackdropScene scene = _backdropScene;
		Rect region = new Rect(stamp.Origin.X, stamp.Origin.Y, bounds.Width * stamp.Dpi.DpiScaleX, bounds.Height * stamp.Dpi.DpiScaleY);
		double num = Math.Min(1.0, Math.Min(2048.0 / Math.Max(region.Width, region.Height), Math.Sqrt(320000.0 / (region.Width * region.Height))));
		int width = Math.Max(1, (int)Math.Floor(region.Width * num));
		int height = Math.Max(1, (int)Math.Floor(region.Height * num));
		Thread thread = new Thread(() =>
		{
			BitmapSource result = null;
			try
			{
				WallpaperBackdrop wallpaperBackdrop = new WallpaperBackdrop();
				if (!token.IsCancellationRequested)
				{
					wallpaperBackdrop.Update(region, width, height, 0.0);
				}
				byte[] pixels = wallpaperBackdrop.Pixels;
				if (!token.IsCancellationRequested && pixels != null && !IsSolidBlack(pixels))
				{
					byte[] pixels2 = new byte[pixels.Length];
					byte[] scratch = new byte[pixels.Length];
					RenderMaterialBackdropInto(stamp.Material, pixels, pixels2, scratch, width, height, bounds.Size, stamp.Dark, 1.0, stamp.Clarity, stamp.Launchpad);
					result = BitmapSource.Create(width, height, 96.0, 96.0, PixelFormats.Bgra32, null, pixels2, width * 4);
					result.Freeze();
				}
			}
			catch (Exception ex) when ((ex is IOException || ex is InvalidOperationException || ex is COMException || ex is ArgumentException || ex is NotSupportedException) ? true : false)
			{
				Trace.WriteLine("Compatible first material: " + ex.Message);
			}
			if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
			{
				cancellation.Dispose();
			}
			else
			{
				Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(() =>
				{
					if (_wallpaperCancellation == cancellation)
					{
						_wallpaperCancellation = null;
					}
					cancellation.Dispose();
					if (generation == _lifecycleGeneration)
					{
						_wallpaperPreparationPending = false;
						if (!token.IsCancellationRequested && !_compatibilityIsDesktopSnapshot && !_performanceSleeping && result != null && !(stamp != ReadMaterialStamp()) && scene != null && scene.Equals(_backdropScene) && _handoffStarted == 0L && _frame == null && _transitionBackdrop == null)
						{
							_compatibilityBackdrop = result;
							UpdateTransitionLayer();
							InvalidateVisual();
						}
					}
				}));
			}
		});
		thread.IsBackground = true;
		thread.Name = "GlassDesk first wallpaper material";
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
	}

	private static bool IsSolidBlack(byte[] pixels)
	{
		for (int i = 0; i < pixels.Length; i += 4)
		{
			if (pixels[i] != 0 || pixels[i + 1] != 0 || pixels[i + 2] != 0)
			{
				return false;
			}
		}
		return true;
	}

	private bool CanCommitCapturedScene(GlassBackdropScene? capturedScene, MaterialStamp? capturedStamp, long sourceTicks)
	{
		if (capturedScene == null || capturedStamp != ReadMaterialStamp())
		{
			return false;
		}
		if (sourceTicks > 0 && sourceTicks <= _lastCaptureSourceTicks && !capturedScene.Equals(_lastCaptureScene))
		{
			return false;
		}
		if (capturedScene.Equals(_backdropScene))
		{
			return true;
		}
		long num = Stopwatch.GetTimestamp() - sourceTicks;
		GlassBackdropScene? backdropScene = _backdropScene;
		if (backdropScene != null && backdropScene.CanHandoffMaterialFrom(capturedScene) && sourceTicks > 0 && num >= 0)
		{
			return num <= Stopwatch.Frequency * 120 / 1000;
		}
		return false;
	}

	private void EnsureBackdropScene(bool revealing = false)
	{
		LiquidGlassSurface liquidGlassSurface = this;
		bool revealing2 = revealing;
		if (IsPlainGlass)
		{
			LauncherRevealRepair.AfterEnsure(this);
			return;
		}
		long timestamp = Stopwatch.GetTimestamp();
		GlassBackdropScene scene = ReadBackdropScene();
		MaterialStamp materialStamp = ReadMaterialStamp();
		long timestamp2 = Stopwatch.GetTimestamp();
		bool relocationHandoff = _relocationHandoffUntil > timestamp2 && (object)_materialStamp != null && MatchesExceptOrigin(_materialStamp, materialStamp);
		_maximumSceneQueryMilliseconds = Math.Max(_maximumSceneQueryMilliseconds, Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds);
		bool flag = false;
		int num;
		if (_compatibilityIsDesktopSnapshot && (scene == null || !scene.Equals(_compatibilityScene)))
		{
			if (!UseLaunchpadOptics && !revealing2 && scene != null && materialStamp == _compatibilityStamp)
			{
				Window window = Window.GetWindow(this);
				if (window != null && window.IsVisible && IsVisible)
				{
					Func<bool>? isBackdropActive = IsBackdropActive;
					if (isBackdropActive == null || isBackdropActive())
					{
						num = (scene.CanHandoffMaterialFrom(_compatibilityScene) ? 1 : 0);
						if (num != 0 && _materialRetentionUntil == 0L)
						{
							_materialRetentionUntil = Stopwatch.GetTimestamp() + Stopwatch.Frequency / 10;
						}
						goto IL_017a;
					}
				}
			}
			num = 0;
			goto IL_017a;
		}
		goto IL_01e2;
		IL_0321:
		int num2;
		bool flag2 = (byte)num2 != 0;
		if (scene == null || !scene.Equals(_backdropScene))
		{
			_backdropScene = scene;
			if (!flag2)
			{
				_worker.Invalidate();
				_revealCaptureUntil = 0L;
				_background.Suspend();
			}
			_lastSource = null;
		}
		bool flag3 = false;
		if (_frame != null && !Reusable(_frameScene))
		{
			RecordMaterialRetirement((scene == null) ? "SceneUnknown" : "SceneHandoffExpired");
			_frame = null;
			_frameScene = null;
			flag3 = true;
		}
		if (_transitionBackdrop != null && !Reusable(_transitionBackdropScene))
		{
			RecordMaterialRetirement((scene == null) ? "SceneUnknown" : "SceneHandoffExpired");
			_transitionBackdrop = null;
			_transitionBackdropScene = null;
			flag3 = true;
		}
		if (!flag3 && !flag)
		{
			LauncherRevealRepair.AfterEnsure(this);
			return;
		}
		_compatibilityBackdrop = null;
		_compatibilityStamp = null;
		_handoffStarted = 0L;
		if (!flag2)
		{
			_worker.Invalidate();
		}
		_lastSource = null;
		if (_frame == null && _transitionBackdrop == null)
		{
			BackdropSource = GlassBackdropSource.Transparent;
		}
		UpdateTransitionLayer();
		InvalidateVisual();
		LauncherRevealRepair.AfterEnsure(this);
		return;
		IL_01e2:
		if ((_frame != null || _transitionBackdrop != null) && (object)_materialStamp != null && _materialStamp != materialStamp && !relocationHandoff)
		{
			RecordMaterialRetirement("MaterialStampChanged");
			_frame = null;
			_frameScene = null;
			_transitionBackdrop = null;
			_transitionBackdropScene = null;
			_worker.Invalidate();
			_lastSource = null;
			_handoffStarted = 0L;
			_relocationHandoffUntil = 0L;
		}
		if (_frame != null && _frameScene == null)
		{
			_frameScene = _backdropScene;
		}
		if (_transitionBackdrop != null && _transitionBackdropScene == null)
		{
			_transitionBackdropScene = _backdropScene;
		}
		if (!revealing2)
		{
			Window window2 = Window.GetWindow(this);
			if (window2 != null && window2.IsVisible && IsVisible)
			{
				Func<bool>? isBackdropActive2 = IsBackdropActive;
				if (isBackdropActive2 == null || isBackdropActive2())
				{
					num2 = ((scene?.CanHandoffMaterialFrom(_backdropScene) ?? false) ? 1 : 0);
					goto IL_0321;
				}
			}
		}
		num2 = 0;
		goto IL_0321;
		IL_017a:
		if (num == 0 || Stopwatch.GetTimestamp() >= _materialRetentionUntil)
		{
			RecordMaterialRetirement((scene == null) ? "SceneUnknown" : "SnapshotHandoffExpired");
			RevealMaterialInvalidationReason = "desktop scene changed";
			_compatibilityBackdrop = null;
			_compatibilityStamp = null;
			_compatibilityIsDesktopSnapshot = false;
			_compatibilityScene = null;
			_handoffStarted = 0L;
			flag = true;
		}
		goto IL_01e2;
		bool Reusable(GlassBackdropScene? materialScene)
		{
			if (relocationHandoff)
			{
				return true;
			}
			if (scene == null)
			{
				return false;
			}
			if (scene.Equals(materialScene))
			{
				return true;
			}
			Window window3 = Window.GetWindow(this);
			int num3;
			if (window3 != null && window3.IsVisible && IsVisible)
			{
				Func<bool>? isBackdropActive3 = IsBackdropActive;
				if (isBackdropActive3 == null || isBackdropActive3())
				{
					num3 = (scene.CanHandoffMaterialFrom(materialScene) ? 1 : 0);
					goto IL_0084;
				}
			}
			num3 = 0;
			goto IL_0084;
			IL_0084:
			bool flag4 = (byte)num3 != 0;
			if (revealing2 || (!scene.CanRetainMaterialFrom(materialScene) && !flag4))
			{
				return false;
			}
			long timestamp3 = Stopwatch.GetTimestamp();
			if (_materialRetentionUntil == 0L)
			{
				_materialRetentionUntil = timestamp3 + Stopwatch.Frequency / 10;
			}
			return timestamp3 < _materialRetentionUntil;
		}
	}

	private void RecordMaterialRetirement(string reason, bool expected = false)
	{
		if (_frame != null || _transitionBackdrop != null || (_compatibilityIsDesktopSnapshot && _compatibilityBackdrop != null))
		{
			Window window = Window.GetWindow(this);
			bool flag = window != null && window.IsVisible && IsVisible;
			bool flag2 = IsBackdropActive?.Invoke() ?? true;
			RecordSurfaceTrace((!expected & flag & flag2) ? "MaterialLoss" : "MaterialState", reason, _diagnosticMaterial);
		}
	}

	private void RecordSurfaceTrace(string stage, string reason, string? source)
	{
		GlassBackgroundLayer.CaptureTiming lastCaptureTiming = _background.LastCaptureTiming;
		long timestamp = Stopwatch.GetTimestamp();
		long sourceTicks = lastCaptureTiming?.SourceTicks ?? 0;
		long endTicks = 0L;
		long barrierTicks = 0L;
		long generation = BackdropRecovery.Generation;
		long windowHandle = 0L;
		int diagnosticId = _diagnosticId;
		string surface = (UseLaunchpadOptics ? "Launcher" : "Dock");
		Window window = Window.GetWindow(this);
		BackdropRecovery.Record(new CaptureTrace(stage, reason, timestamp, sourceTicks, endTicks, barrierTicks, generation, 0, windowHandle, 0u, null, 0, diagnosticId, surface, source, window != null && window.IsVisible && IsVisible, IsBackdropActive?.Invoke() ?? true, _performanceSleeping, _backdropScene?.GetHashCode() ?? 0, (int)Math.Max(0.0, ActualWidth), (int)Math.Max(0.0, ActualHeight)));
	}

	private GlassBackdropScene? CaptureBackdropScene()
	{
		if (DesktopOnlyBackdrop)
		{
			return new GlassBackdropScene();
		}
		if (!(Window.GetWindow(this) is GlassWindow glassWindow))
		{
			return GlassBackdropScene.Capture();
		}
		PixelRect? pixelRect;
		if (!glassWindow.IsVisible)
		{
			PixelRect? revealHost = _revealHost;
			if (revealHost.HasValue)
			{
				PixelRect valueOrDefault = revealHost.GetValueOrDefault();
				pixelRect = valueOrDefault;
				goto IL_0075;
			}
		}
		pixelRect = DisplayService.GetWindowPixelBounds(new WindowInteropHelper(glassWindow).Handle);
		goto IL_0075;
		IL_0075:
		PixelRect? pixelRect2 = pixelRect;
		if (pixelRect2.HasValue)
		{
			PixelRect valueOrDefault2 = pixelRect2.GetValueOrDefault();
			if (valueOrDefault2.Width > 0 && valueOrDefault2.Height > 0)
			{
				DpiScale dpi = VisualTreeHelper.GetDpi(glassWindow);
				int num = (int)Math.Ceiling(64.0 * dpi.DpiScaleX);
				int num2 = (int)Math.Ceiling(64.0 * dpi.DpiScaleY);
				PixelRect pixelRect3 = new PixelRect(valueOrDefault2.X - num, valueOrDefault2.Y - num2, valueOrDefault2.Width + 2 * num, valueOrDefault2.Height + 2 * num2);
				PrimaryDisplaySession primaryDisplaySession = glassWindow.PrimaryDisplaySession;
				if (primaryDisplaySession != null && primaryDisplaySession.IsAvailable)
				{
					pixelRect3 = primaryDisplaySession.ClipToPrimaryBounds(pixelRect3);
				}
				return GlassBackdropScene.Capture(pixelRect3);
			}
		}
		return GlassBackdropScene.Capture();
	}

	private static void DrawWaitingRim(DrawingContext dc, Geometry panel, double strength)
	{
		LinearGradientBrush brush = new LinearGradientBrush(new GradientStopCollection
		{
			new GradientStop(Color.FromArgb(ScaleAlpha(100.0, strength), byte.MaxValue, byte.MaxValue, byte.MaxValue), 0.0),
			new GradientStop(Color.FromArgb(ScaleAlpha(20.0, strength), byte.MaxValue, byte.MaxValue, byte.MaxValue), 0.4),
			new GradientStop(Color.FromArgb(ScaleAlpha(75.0, strength), 12, 17, 27), 1.0)
		}, new Point(0.0, 0.0), new Point(1.0, 1.0));
		dc.DrawGeometry(null, new Pen(brush, 1.5 * strength), panel);
	}

	private void SetTimerInterval(TimeSpan interval)
	{
		if (IsPlainGlass)
		{
			return;
		}
		_refreshInterval = ((_handoffStarted != 0L) ? GlassBackgroundLayer.LiveFrameInterval : interval);
		TimeSpan timeSpan = ((interval <= GlassBackgroundLayer.LiveFrameInterval) ? TimeSpan.FromMilliseconds(100L) : interval);
		if (_timer.Interval != timeSpan)
		{
			_timer.Interval = timeSpan;
		}
		UpdateRenderClock();
		if (!_performanceSleeping && IsLoaded && IsVisible)
		{
			Func<bool>? isBackdropActive = IsBackdropActive;
			if (isBackdropActive == null || isBackdropActive())
			{
				_timer.Start();
			}
		}
	}

	private void UpdateRenderClock()
	{
		int num;
		if (!IsPlainGlass && !_performanceSleeping && IsLoaded && IsVisible)
		{
			Func<bool>? isBackdropActive = IsBackdropActive;
			if (isBackdropActive == null || isBackdropActive())
			{
				num = ((_refreshInterval <= GlassBackgroundLayer.LiveFrameInterval) ? 1 : 0);
				goto IL_005f;
			}
		}
		num = 0;
		goto IL_005f;
		IL_005f:
		bool flag = (byte)num != 0;
		if (flag && !_renderClockAttached)
		{
			_lastRenderingTime = TimeSpan.MinValue;
			_lastRenderingTicks = 0L;
			CompositionTarget.Rendering += OnRenderingFrame;
			_renderClockAttached = true;
		}
		else if (!flag)
		{
			StopRenderClock();
		}
	}

	private void StopRenderClock()
	{
		if (_renderClockAttached)
		{
			CompositionTarget.Rendering -= OnRenderingFrame;
		}
		_renderClockAttached = false;
		_renderFrameOperation?.Abort();
		_renderFrameOperation = null;
		_renderFrameQueued = false;
		_lastRenderingTicks = 0L;
	}

	private void OnRenderingFrame(object? sender, EventArgs e)
	{
		if (_performanceSleeping || IsPlainGlass)
		{
			return;
		}
		if (e is RenderingEventArgs e2)
		{
			if (e2.RenderingTime == _lastRenderingTime)
			{
				return;
			}
			_lastRenderingTime = e2.RenderingTime;
		}
		_lastRenderingTicks = Stopwatch.GetTimestamp();
		if (_renderFrameQueued)
		{
			DispatcherOperation renderFrameOperation = _renderFrameOperation;
			if (renderFrameOperation != null && renderFrameOperation.Status == DispatcherOperationStatus.Pending && renderFrameOperation.Priority == DispatcherPriority.Background)
			{
				_renderFrameOperation.Priority = DispatcherPriority.Normal;
			}
		}
		else if (!_worker.IsBusy)
		{
			_renderFrameQueued = true;
			DispatcherPriority priority = ((++_renderFrameSignals % 8 == 0) ? DispatcherPriority.Background : DispatcherPriority.Normal);
			_renderFrameOperation = Dispatcher.BeginInvoke(priority, _cachedRenderFrameAction);
		}
	}

	private void OnWatchdogFrame(object? sender, EventArgs e)
	{
		if (!_performanceSleeping && !IsPlainGlass && (!_renderClockAttached || _lastRenderingTicks == 0L || !(Stopwatch.GetElapsedTime(_lastRenderingTicks) < TimeSpan.FromMilliseconds(100L))))
		{
			OnFrame(sender, e);
		}
	}

	private static PixelRect ClipRegion(PixelRect rect, PixelRect bounds)
	{
		int num = Math.Max(rect.X, bounds.X);
		int num2 = Math.Max(rect.Y, bounds.Y);
		int num3 = Math.Min(rect.Right, bounds.Right);
		int num4 = Math.Min(rect.Bottom, bounds.Bottom);
		if (num3 > num && num4 > num2)
		{
			return new PixelRect(num, num2, num3 - num, num4 - num2);
		}
		return new PixelRect(Math.Clamp(num, bounds.X, bounds.Right), Math.Clamp(num2, bounds.Y, bounds.Bottom), 0, 0);
	}

	private static byte ScaleAlpha(double alpha, double scale)
	{
		return (byte)Math.Clamp((int)Math.Round(alpha * scale), 0, 255);
	}

	private void RefreshOpticalRefreshRate()
	{
		long num = Stopwatch.GetTimestamp() - Stopwatch.Frequency;
		long result;
		while (_opticalFrameTimestamps.TryPeek(out result) && result < num)
		{
			_opticalFrameTimestamps.Dequeue();
		}
		_opticalRefreshRate = _opticalFrameTimestamps.Count;
	}

	private void DrawMetricsOverlay(DrawingContext drawingContext, Rect bounds)
	{
		FormattedText formattedText = new FormattedText($"折射刷新 {_opticalRefreshRate:0} fps · CPU {_lastOpticsMilliseconds:0.00} ms", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 9.0, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
		Rect rectangle = new Rect(bounds.Left + 6.0, bounds.Bottom - formattedText.Height - 8.0, formattedText.Width + 12.0, formattedText.Height + 4.0);
		drawingContext.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(215, 12, 18, 28)), null, rectangle, 3.0, 3.0);
		drawingContext.DrawText(formattedText, new Point(rectangle.Left + 6.0, rectangle.Top + 2.0));
	}

	private void SuspendPlainGlassMaterial()
	{
		if (!_plainGlassMaterialSuspended)
		{
			_plainGlassMaterialSuspended = true;
			RetireConcealedMaterial();
			_containerAnimationActive = false;
			_background.SetPerformanceSleeping(sleeping: true);
			_transitionLayer.SetPerformanceSleeping(sleeping: true);
			_compatibilityLayer.SetPerformanceSleeping(sleeping: true);
			_optics = new LiquidGlassOptics();
			_androidGlass = new AndroidLiquidGlassRenderer();
			_opticalFrameTimestamps.Clear();
			_opticalRefreshRate = 0.0;
			_backdropScene = null;
		}
	}

	internal static Color PlainGlassColor(bool launcher)
	{
		ThemeService current = ThemeService.Current;
		double num = ((!launcher) ? (current?.Appearance.DockGlassTransparency ?? 0.9) : (current?.Appearance.LauncherGlassTransparency ?? 0.2));
		if (!double.IsFinite(num))
		{
			num = (launcher ? 0.2 : 0.9);
		}
		byte a = ((current != null && current.UseOpaquePanels) ? byte.MaxValue : ((byte)Math.Round(255.0 * (1.0 - Math.Clamp(num, 0.0, 1.0)))));
		if (current != null && !current.IsDark)
		{
			return Color.FromArgb(a, 246, 247, 250);
		}
		return Color.FromArgb(a, 28, 29, 34);
	}

	private void RenderPlainGlass(DrawingContext dc)
	{
		SuspendPlainGlassMaterial();
		base.OnRender(dc);
		if (!(ActualWidth < 1.0) && !(ActualHeight < 1.0))
		{
			Rect bounds = _panelShape ?? new Rect(RenderSize);
			GlassWindow glassWindow = Window.GetWindow(this) as GlassWindow;
			Rect? panelShape = _panelShape;
			double radius = (panelShape.HasValue ? _panelRadius : (glassWindow?.PanelCornerRadius.TopLeft ?? 40.0));
			Color color = PlainGlassColor(UseLauncherTransparency);
			SolidColorBrush? plainGlassBrush = _plainGlassBrush;
			if (plainGlassBrush == null || plainGlassBrush.Color != color)
			{
				_plainGlassBrush = new SolidColorBrush(color);
				_plainGlassBrush.Freeze();
			}
			Geometry geometry = _panelGeometry ?? glassWindow?.CreateGlassPanelGeometry(bounds, radius) ?? SquircleGlassGeometry.Create(bounds, radius);
			dc.DrawGeometry(_plainGlassBrush, null, geometry);
			_lastRenderTicks = Stopwatch.GetTimestamp();
			_renderCount++;
			_lastRenderMaterialReady = true;
			bool valueOrDefault = _firstVisibleRenderMaterialReady == true;
			if (!_firstVisibleRenderMaterialReady.HasValue)
			{
				valueOrDefault = true;
				_firstVisibleRenderMaterialReady = valueOrDefault;
			}
			if (_firstVisibleMaterialSource == null)
			{
				_firstVisibleMaterialSource = "PlainGlass";
			}
			SharedMaterialChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	internal void DrawSharedMaterial(DrawingContext dc, Rect region)
	{
		if (IsPlainGlass)
		{
			return;
		}
		BitmapSource bitmapSource = null;
		Rect rectangle = default;
		if (_transitionBackdrop != null)
		{
			GlassBackdropScene? transitionBackdropScene = _transitionBackdropScene;
			if (transitionBackdropScene != null && transitionBackdropScene.Equals(_backdropScene))
			{
				bitmapSource = _transitionBackdrop;
				rectangle = _transitionBackdropBounds;
				goto IL_00d4;
			}
		}
		if (_frame != null)
		{
			GlassBackdropScene? frameScene = _frameScene;
			if (frameScene != null && frameScene.Equals(_backdropScene))
			{
				bitmapSource = _frame;
				rectangle = _sampledPanelBounds;
				goto IL_00d4;
			}
		}
		if (_compatibilityBackdrop != null)
		{
			GlassBackdropScene? compatibilityScene = _compatibilityScene;
			if (compatibilityScene != null && compatibilityScene.Equals(_backdropScene))
			{
				bitmapSource = _compatibilityBackdrop;
				rectangle = _compatibilityBounds;
			}
		}
		goto IL_00d4;
		IL_00d4:
		if (bitmapSource != null && !(rectangle.Width <= 0.0) && !(rectangle.Height <= 0.0))
		{
			rectangle.Offset(0.0 - region.X, 0.0 - region.Y);
			dc.DrawImage(bitmapSource, rectangle);
		}
	}
}
