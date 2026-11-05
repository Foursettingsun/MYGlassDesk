using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using GlassDesk.Platform;
using GlassDesk.Platform.Displays;

namespace GlassDesk.UI.Controls;

public sealed class GlassShadow : IDisposable
{
	private sealed class ShadowDrawing : FrameworkElement
	{
		private GlassWindow? _owner;

		private Rect _panel;

		private double _radius;

		private Size _size;

		private double _scale = 1.0;

		private DrawingGroup? _drawing;

		private bool _compactShape;

		private bool _reducedDetail;

		private LiquidGlassDebugView _view;

		private LiquidGlassFinishingParameters _finishingParameters = LiquidGlassFinishingParameters.Default;

		internal void SetGeometry(GlassWindow owner, Rect panel, double radius, Size size, double scale)
		{
			if (_owner != owner || !(_panel == panel) || _radius != radius || !(_size == size) || _scale != scale)
			{
				_owner = owner;
				_panel = panel;
				_radius = radius;
				_size = size;
				_scale = scale;
				_drawing = null;
				InvalidateVisual();
			}
		}

		internal void SetCompactShape(bool compact)
		{
			if (_compactShape != compact)
			{
				_compactShape = compact;
				_drawing = null;
				InvalidateVisual();
			}
		}

		internal void SetReducedDetail(bool reduced)
		{
			if (_reducedDetail != reduced)
			{
				_reducedDetail = reduced;
				_drawing = null;
				InvalidateVisual();
			}
		}

		internal void SetAppearance(LiquidGlassDebugView view, LiquidGlassFinishingParameters parameters)
		{
			parameters = parameters.Normalize();
			if (_view != view || !(_finishingParameters == parameters))
			{
				_view = view;
				_finishingParameters = parameters;
				_drawing = null;
				InvalidateVisual();
			}
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			base.OnRender(drawingContext);
			if (_panel.Width <= 0.0 || _panel.Height <= 0.0 || _size.Width <= 0.0 || _size.Height <= 0.0)
			{
				return;
			}
			if (_drawing == null)
			{
				_drawing = new DrawingGroup();
				using (DrawingContext drawingContext2 = _drawing.Open())
				{
					CombinedGeometry combinedGeometry = new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(new Rect(_size)), CreatePanel(_panel, _radius));
					combinedGeometry.Freeze();
					drawingContext2.PushClip(combinedGeometry);
					if (LiquidGlassFinishComposer.IncludesBaselineShadow(_view))
					{
						DrawSoftShadow(drawingContext2, 34.0 * _scale, 8.0 * _scale, 0.15, Color.FromRgb(18, 24, 36));
						DrawSoftShadow(drawingContext2, 10.0 * _scale, 2.0 * _scale, 0.1, Color.FromRgb(18, 24, 36));
					}
					var (flag, flag2) = LiquidGlassFinishComposer.ShadowSelection(_view);
					if (flag && _finishingParameters.AmbientShadowOpacity > 0.0)
					{
						DrawSoftShadow(drawingContext2, _finishingParameters.AmbientShadowExtentDip * _scale, _finishingParameters.AmbientShadowOffsetDip * _scale, _finishingParameters.AmbientShadowOpacity, Color.FromRgb(18, 24, 36));
					}
					if (flag2 && _finishingParameters.ContactShadowOpacity > 0.0)
					{
						DrawSoftShadow(drawingContext2, _finishingParameters.ContactShadowExtentDip * _scale, _finishingParameters.ContactShadowOffsetDip * _scale, _finishingParameters.ContactShadowOpacity, Color.FromRgb(12, 17, 27));
					}
					drawingContext2.Pop();
				}
				_drawing.Freeze();
			}
			drawingContext.DrawDrawing(_drawing);
		}

		private void DrawSoftShadow(DrawingContext context, double extent, double offset, double opacity, Color color)
		{
			int num = (_reducedDetail ? 16 : 32);
			for (int num2 = num; num2 >= 1; num2--)
			{
				double num3 = extent * (double)num2 / (double)num;
				double num4 = extent * (double)(num2 - 1) / (double)num;
				double opacity2 = opacity * (Math.Exp(-4.5 * num4 * num4 / (extent * extent)) - Math.Exp(-4.5 * num3 * num3 / (extent * extent)));
				Rect panel = _panel;
				panel.Offset(0.0, offset);
				panel.Inflate(num3, num3);
				SolidColorBrush solidColorBrush = new SolidColorBrush(color)
				{
					Opacity = opacity2
				};
				solidColorBrush.Freeze();
				context.DrawGeometry(solidColorBrush, null, CreatePanel(panel, _radius + num3));
			}
		}

		private Geometry CreatePanel(Rect bounds, double radius)
		{
			Geometry geometry;
			if (!_compactShape)
			{
				geometry = _owner?.CreateGlassPanelGeometry(bounds, radius);
				if (geometry == null)
				{
					return SquircleGlassGeometry.Create(bounds, radius);
				}
			}
			else
			{
				geometry = SquircleGlassGeometry.CreateCompact(bounds, radius);
			}
			return geometry;
		}
	}

	private const double Padding = 42.0;

	private readonly GlassWindow _owner;

	private readonly Window _window;

	private readonly ShadowDrawing _drawing = new ShadowDrawing();

	private readonly List<DependencyPropertyDescriptor> _descriptors = new List<DependencyPropertyDescriptor>();

	private HwndSource? _ownerSource;

	private DispatcherOperation? _pendingUpdate;

	private bool _disposed;

	private bool _updating;

	private bool _inputTransparent;

	private CaptureExclusionSession? _captureExclusion;

	private DependencyPropertyDescriptor? _opacityDescriptor;

	private Rect? _transitionPanel;

	private double _transitionRadius;

	private double _transitionStrength = 1.0;

	private int _ownerPixelWidth;

	private int _ownerPixelHeight;

	private int _paddingLeftPixels;

	private int _paddingTopPixels;

	private double _ownerScale;

	private double _shadowScale;

	private Size _shadowSize;

	public nint Handle
	{
		get
		{
			if (!_disposed)
			{
				return new WindowInteropHelper(_window).Handle;
			}
			return 0;
		}
	}

	public bool IsVisible
	{
		get
		{
			if (!_disposed)
			{
				return _window.IsVisible;
			}
			return false;
		}
	}

	public GlassShadow(GlassWindow owner)
	{
		ArgumentNullException.ThrowIfNull(owner, "owner");
		_owner = owner;
		_window = new Window
		{
			Title = "GlassDesk ambient shadow",
			WindowStyle = WindowStyle.None,
			AllowsTransparency = true,
			Background = Brushes.Transparent,
			ResizeMode = ResizeMode.NoResize,
			ShowInTaskbar = false,
			ShowActivated = false,
			Focusable = false,
			IsHitTestVisible = false,
			SizeToContent = SizeToContent.Manual,
			Content = _drawing,
			Width = 1.0,
			Height = 1.0
		};
		_window.SourceInitialized += OnShadowSourceInitialized;
		_window.DpiChanged += OnShadowDpiChanged;
		_owner.SourceInitialized += OnOwnerSourceInitialized;
		_owner.LocationChanged += OnOwnerChanged;
		_owner.SizeChanged += OnOwnerSizeChanged;
		_owner.StateChanged += OnOwnerChanged;
		_owner.IsVisibleChanged += OnOwnerVisibilityChanged;
		_owner.Activated += OnOwnerChanged;
		_owner.Deactivated += OnOwnerChanged;
		_owner.Closed += OnOwnerClosed;
		LiquidGlassDebugState.CompositionChanged += OnCompositionChanged;
		_drawing.SetAppearance(LiquidGlassDebugState.View, LiquidGlassDebugState.FinishingParameters);
		Observe(Window.TopmostProperty);
		_opacityDescriptor = DependencyPropertyDescriptor.FromProperty(UIElement.OpacityProperty, typeof(GlassWindow));
		_opacityDescriptor?.AddValueChanged(_owner, OnOwnerOpacityChanged);
		Observe(GlassWindow.PanelCornerRadiusProperty);
		Observe(GlassWindow.ChromeInsetProperty);
		InstallOwnerHook();
		Update();
	}

	internal void SetTransitionShape(Rect? panelBounds, double radius, double strength = 1.0)
	{
		SetTransitionShape(panelBounds, radius, strength, reducedDetail: false);
	}

	internal void SetTransitionShape(Rect? panelBounds, double radius, double strength, bool reducedDetail)
	{
		if (!_disposed)
		{
			_drawing.SetReducedDetail(reducedDetail);
			_transitionPanel = panelBounds;
			_transitionRadius = (double.IsFinite(radius) ? Math.Max(0.0, radius) : 0.0);
			_transitionStrength = (double.IsFinite(strength) ? Math.Clamp(strength, 0.0, 1.0) : 1.0);
			_window.Opacity = _owner.Opacity * _transitionStrength;
			if (_transitionStrength <= 0.0)
			{
				Hide();
			}
			else if (!_window.IsVisible)
			{
				Update();
			}
			else
			{
				UpdatePanelGeometry();
			}
		}
	}

	public void Update()
	{
		if (_disposed || _updating || _owner.Dispatcher.HasShutdownStarted)
		{
			return;
		}
		_owner.Dispatcher.VerifyAccess();
		_updating = true;
		try
		{
			nint handle = new WindowInteropHelper(_owner).Handle;
			if (_owner.IsVisible && _owner.WindowState != WindowState.Minimized && !(_owner.Opacity <= 0.0) && !(_transitionStrength <= 0.0) && WindowShadowStyles.IsOwnerVisible(handle))
			{
				PixelRect? windowPixelBounds = DisplayService.GetWindowPixelBounds(handle);
				if (windowPixelBounds.HasValue)
				{
					PixelRect valueOrDefault = windowPixelBounds.GetValueOrDefault();
					if (valueOrDefault.Width > 0 && valueOrDefault.Height > 0)
					{
						PrimaryDisplaySession primaryDisplaySession = _owner.PrimaryDisplaySession;
						if (primaryDisplaySession != null && !primaryDisplaySession.IsAvailable)
						{
							Hide();
							return;
						}
						DisplayInfo displayInfo = primaryDisplaySession?.Display ?? DisplayService.Primary();
						if ((object)displayInfo == null || !Contains(displayInfo.Bounds, valueOrDefault))
						{
							Hide();
							return;
						}
						double num = (double)displayInfo.Dpi / 96.0;
						int num2 = (int)Math.Ceiling(42.0 * num);
						PixelRect pixelRect = new PixelRect(valueOrDefault.X - num2, valueOrDefault.Y - num2, valueOrDefault.Width + num2 * 2, valueOrDefault.Height + num2 * 2);
						PixelRect bounds = primaryDisplaySession?.ClipToPrimaryBounds(pixelRect) ?? Clip(pixelRect, displayInfo.Bounds);
						if (bounds.Width <= 0 || bounds.Height <= 0)
						{
							Hide();
							return;
						}
						nint shadow = new WindowInteropHelper(_window).EnsureHandle();
						if (!_inputTransparent || !WindowShadowStyles.PositionBehind(shadow, handle, bounds))
						{
							Hide();
							return;
						}
						_ownerPixelWidth = valueOrDefault.Width;
						_ownerPixelHeight = valueOrDefault.Height;
						_paddingLeftPixels = valueOrDefault.X - bounds.X;
						_paddingTopPixels = valueOrDefault.Y - bounds.Y;
						_ownerScale = num;
						_shadowScale = Math.Max(0.01, (double)displayInfo.Dpi / 96.0);
						_shadowSize = new Size((double)bounds.Width / _shadowScale, (double)bounds.Height / _shadowScale);
						UpdatePanelGeometry();
						_window.Opacity = _owner.Opacity * _transitionStrength;
						if (!_window.IsVisible)
						{
							_window.Show();
							if (!WindowShadowStyles.PositionBehind(shadow, handle, bounds))
							{
								Hide();
							}
						}
						return;
					}
				}
			}
			Hide();
		}
		finally
		{
			_updating = false;
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_owner.Dispatcher.VerifyAccess();
		_disposed = true;
		_pendingUpdate?.Abort();
		_pendingUpdate = null;
		_owner.SourceInitialized -= OnOwnerSourceInitialized;
		_owner.LocationChanged -= OnOwnerChanged;
		_owner.SizeChanged -= OnOwnerSizeChanged;
		_owner.StateChanged -= OnOwnerChanged;
		_owner.IsVisibleChanged -= OnOwnerVisibilityChanged;
		_owner.Activated -= OnOwnerChanged;
		_owner.Deactivated -= OnOwnerChanged;
		_owner.Closed -= OnOwnerClosed;
		LiquidGlassDebugState.CompositionChanged -= OnCompositionChanged;
		foreach (DependencyPropertyDescriptor descriptor in _descriptors)
		{
			descriptor.RemoveValueChanged(_owner, OnOwnerChanged);
		}
		_descriptors.Clear();
		_opacityDescriptor?.RemoveValueChanged(_owner, OnOwnerOpacityChanged);
		_opacityDescriptor = null;
		HwndSource ownerSource = _ownerSource;
		if (ownerSource != null && !ownerSource.IsDisposed)
		{
			_ownerSource.RemoveHook(OnOwnerMessage);
		}
		_ownerSource = null;
		_window.SourceInitialized -= OnShadowSourceInitialized;
		_window.DpiChanged -= OnShadowDpiChanged;
		nint handle = new WindowInteropHelper(_window).Handle;
		_captureExclusion?.Dispose();
		CaptureExclusionSession.Forget(handle);
		_window.Close();
	}

	private static bool Contains(PixelRect bounds, PixelRect rect)
	{
		if (rect.Width > 0 && rect.Height > 0 && rect.X >= bounds.X && rect.Y >= bounds.Y && rect.Right <= bounds.Right)
		{
			return rect.Bottom <= bounds.Bottom;
		}
		return false;
	}

	private static PixelRect Clip(PixelRect rect, PixelRect bounds)
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

	private void Hide()
	{
		if (_window.IsVisible)
		{
			_window.Hide();
		}
		_window.Width = 1.0;
		_window.Height = 1.0;
	}

	private void Observe(DependencyProperty property)
	{
		DependencyPropertyDescriptor dependencyPropertyDescriptor = DependencyPropertyDescriptor.FromProperty(property, typeof(GlassWindow));
		if (dependencyPropertyDescriptor != null)
		{
			dependencyPropertyDescriptor.AddValueChanged(_owner, OnOwnerChanged);
			_descriptors.Add(dependencyPropertyDescriptor);
		}
	}

	private void InstallOwnerHook()
	{
		if (_ownerSource == null && PresentationSource.FromVisual(_owner) is HwndSource hwndSource)
		{
			_ownerSource = hwndSource;
			hwndSource.AddHook(OnOwnerMessage);
		}
	}

	private void QueueUpdate()
	{
		if (_disposed || _owner.Dispatcher.HasShutdownStarted)
		{
			return;
		}
		DispatcherOperation? pendingUpdate = _pendingUpdate;
		if (pendingUpdate == null || pendingUpdate.Status != DispatcherOperationStatus.Pending)
		{
			_pendingUpdate = _owner.Dispatcher.BeginInvoke(DispatcherPriority.Render, (Action)(() =>
			{
				_pendingUpdate = null;
				Update();
			}));
		}
	}

	private nint OnOwnerMessage(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
	{
		bool flag;
		switch (message)
		{
		case 24:
		case 71:
		case 126:
		case 736:
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			QueueUpdate();
		}
		return 0;
	}

	private void OnShadowSourceInitialized(object? sender, EventArgs e)
	{
		nint handle = new WindowInteropHelper(_window).Handle;
		_inputTransparent = WindowShadowStyles.MakeInputTransparent(handle);
	}

	private void OnOwnerSourceInitialized(object? sender, EventArgs e)
	{
		InstallOwnerHook();
		QueueUpdate();
	}

	private void OnShadowDpiChanged(object sender, DpiChangedEventArgs e)
	{
		QueueUpdate();
	}

	private void OnOwnerChanged(object? sender, EventArgs e)
	{
		Update();
	}

	private void OnOwnerSizeChanged(object sender, SizeChangedEventArgs e)
	{
		Update();
	}

	private void OnOwnerVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		Update();
	}

	private void OnOwnerClosed(object? sender, EventArgs e)
	{
		Dispose();
	}

	private void OnCompositionChanged(object? sender, EventArgs e)
	{
		if (!_disposed && !_owner.Dispatcher.HasShutdownStarted)
		{
			if (_owner.Dispatcher.CheckAccess())
			{
				UpdateComposition();
			}
			else
			{
				_owner.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(UpdateComposition));
			}
		}
		void UpdateComposition()
		{
			_drawing.SetAppearance(LiquidGlassDebugState.View, LiquidGlassDebugState.FinishingParameters);
		}
	}

	private void OnOwnerOpacityChanged(object? sender, EventArgs e)
	{
		if (!_disposed)
		{
			if (!_owner.IsVisible || _owner.WindowState == WindowState.Minimized || _owner.Opacity <= 0.0)
			{
				Hide();
			}
			else if (!_window.IsVisible)
			{
				Update();
			}
			else
			{
				_window.Opacity = _owner.Opacity * _transitionStrength;
			}
		}
	}

	private void UpdatePanelGeometry()
	{
		if (!(_ownerScale <= 0.0) && !(_shadowScale <= 0.0) && !(_shadowSize.Width <= 0.0) && !(_shadowSize.Height <= 0.0))
		{
			Thickness chromeInset = _owner.ChromeInset;
			Rect? transitionPanel = _transitionPanel;
			Rect panel;
			double radius;
			if (transitionPanel.HasValue)
			{
				Rect valueOrDefault = transitionPanel.GetValueOrDefault();
				panel = new Rect(((double)_paddingLeftPixels + (chromeInset.Left + valueOrDefault.X) * _ownerScale) / _shadowScale, ((double)_paddingTopPixels + (chromeInset.Top + valueOrDefault.Y) * _ownerScale) / _shadowScale, Math.Max(0.0, valueOrDefault.Width * _ownerScale / _shadowScale), Math.Max(0.0, valueOrDefault.Height * _ownerScale / _shadowScale));
				radius = _transitionRadius * _ownerScale / _shadowScale;
			}
			else
			{
				panel = new Rect(((double)_paddingLeftPixels + chromeInset.Left * _ownerScale) / _shadowScale, ((double)_paddingTopPixels + chromeInset.Top * _ownerScale) / _shadowScale, Math.Max(0.0, (double)_ownerPixelWidth - (chromeInset.Left + chromeInset.Right) * _ownerScale) / _shadowScale, Math.Max(0.0, (double)_ownerPixelHeight - (chromeInset.Top + chromeInset.Bottom) * _ownerScale) / _shadowScale);
				CornerRadius panelCornerRadius = _owner.PanelCornerRadius;
				radius = Math.Max(Math.Max(panelCornerRadius.TopLeft, panelCornerRadius.TopRight), Math.Max(panelCornerRadius.BottomLeft, panelCornerRadius.BottomRight)) * _ownerScale / _shadowScale;
			}
			ShadowDrawing drawing = _drawing;
			transitionPanel = _transitionPanel;
			drawing.SetCompactShape(transitionPanel.HasValue && _owner.UsesSquirclePanelGeometry);
			_drawing.SetGeometry(_owner, panel, radius, _shadowSize, _ownerScale / _shadowScale);
		}
	}
}
