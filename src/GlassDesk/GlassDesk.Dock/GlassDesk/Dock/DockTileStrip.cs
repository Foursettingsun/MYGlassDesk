using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using GlassDesk.Platform;
using GlassDesk.Platform.Displays;
using GlassDesk.UI.Theming;

namespace GlassDesk.Dock;

public sealed class DockTileStrip : Border
{
	private const string TileDragFormat = "GlassDesk-TileDrag";

	private const double UnpinDragDistance = 80.0;

	private readonly ScrollViewer _scroller;

	private readonly Canvas _canvas;

	private readonly Border _leadingFade;

	private readonly Border _trailingFade;

	private readonly List<DockTileModel> _models = new List<DockTileModel>();

	private readonly List<Border> _tiles = new List<Border>();

	private readonly List<RadialGradientBrush> _pressGlows = new List<RadialGradientBrush>();

	private readonly DockRenderLoop _animationLoop;

	private bool _pointerActive;

	private double _pointerPosition = double.NaN;

	private int _hoveredTileIndex = -1;

	private int _pressedTileIndex = -1;

	private double[] _pressScale = Array.Empty<double>();

	private double[] _pressVelocity = Array.Empty<double>();

	private double[] _pressGlow = Array.Empty<double>();

	private double _iconSize = 48.0;

	private double _spacing = 8.0;

	private double _peak = 1.15;

	private double _verticalPadding = 8.0;

	private double _maxContentWidth;

	private DockReorderModel? _reorder;

	private string? _pressedEntryId;

	private Point _pressPosition;

	private bool _dragActive;

	private int _dragStartIndex = -1;

	private DispatcherTimer? _pressTimer;

	private Border? _pressTile;

	private Point _pressGrabPoint;

	private bool _longPressDrag;

	private Border? _dragGhost;

	private TranslateTransform? _dragGhostMotion;

	private string? _dragGhostEntryId;

	private readonly Canvas _dragOverlay;

	private Canvas? _ghostLayer;

	private Point _ghostGrab;

	private bool _droppedInStrip;

	private DragCancelReason _dragCancelReason;

	private TileGeometry[] _paintedGeometry = Array.Empty<TileGeometry>();

	private TileGeometry[] _targetGeometry = Array.Empty<TileGeometry>();

	private int[] _paintOrder = Array.Empty<int>();

	private readonly Comparer<int> _paintOrderComparer;

	private readonly Rectangle _groupDivider = new Rectangle
	{
		Width = 1.0,
		Fill = Brushes.Gray,
		Opacity = 0.5,
		Visibility = Visibility.Collapsed,
		IsHitTestVisible = false
	};

	internal bool IsDragging => _dragActive;

	internal Canvas? DragOverlay { get; set; }

	public DragCancelReason LastDragCancelReason => _dragCancelReason;

	public IReadOnlyList<string> ReorderCandidateIds => (from model in _models.Take(AppTileCount)
		select model.EntryId).ToList();

	private int AppTileCount { get; set; }

	public double RequiredLength => DockMagnification.RequiredLength(_models.Count, _iconSize, _spacing, _peak);

	public double RequiredThickness => DockMagnification.RequiredThickness(_iconSize, _peak, _verticalPadding);

	public bool CanScroll
	{
		get
		{
			if (_maxContentWidth > 0.0)
			{
				return RequiredLength > _maxContentWidth;
			}
			return false;
		}
	}

	public double HorizontalOffset => _scroller.HorizontalOffset;

	public double ViewportWidth => _scroller.ViewportWidth;

	public IReadOnlyList<TileGeometry> PaintedGeometry => _paintedGeometry.ToArray();

	public IReadOnlyList<PaintedTileState> PaintedTiles
	{
		get
		{
			List<PaintedTileState> list = new List<PaintedTileState>(_tiles.Count);
			for (int i = 0; i < _tiles.Count; i++)
			{
				Border border = _tiles[i];
				double scale = ((border.RenderTransform is ScaleTransform scaleTransform) ? scaleTransform.ScaleX : 1.0);
				string entryId = ((i < _models.Count) ? _models[i].EntryId : string.Empty);
				list.Add(new PaintedTileState(entryId, scale, Panel.GetZIndex(border), Canvas.GetLeft(border)));
			}
			return list;
		}
	}

	public event EventHandler<string>? TileClicked;

	public event EventHandler<string>? TileContextMenuRequested;

	public event EventHandler? ReorderFinished;

	public event EventHandler<string>? UnpinRequested;

	public void SetReorderModel(DockReorderModel? reorder)
	{
		_reorder = reorder;
		AllowDrop = reorder != null;
	}

	public DockTileStrip()
	{
		_paintOrderComparer = Comparer<int>.Create((int left, int right) => DockMagnification.CompareDrawOrder(_paintedGeometry, left, right));
		Background = Brushes.Transparent;
		ClipToBounds = true;
		_canvas = new Canvas
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			Background = Brushes.Transparent
		};
		_scroller = new ScrollViewer
		{
			Content = _canvas,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
			VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
			Background = Brushes.Transparent,
			Focusable = false
		};
		_scroller.ScrollChanged += (object _, ScrollChangedEventArgs _) =>
		{
			UpdateFades();
		};
		_leadingFade = CreateFade(0.0);
		_trailingFade = CreateFade(1.0);
		AddHandler(DragDrop.QueryContinueDragEvent, new QueryContinueDragEventHandler(OnQueryContinueDrag));
		AddHandler(Mouse.PreviewMouseUpEvent, new MouseButtonEventHandler(OnPreviewMouseUp), handledEventsToo: true);
		AddHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), handledEventsToo: true);
		_dragOverlay = new Canvas
		{
			Background = Brushes.Transparent,
			IsHitTestVisible = false
		};
		Grid grid = new Grid
		{
			Background = Brushes.Transparent
		};
		grid.Children.Add(_scroller);
		grid.Children.Add(_leadingFade);
		grid.Children.Add(_trailingFade);
		grid.Children.Add(_dragOverlay);
		Child = grid;
		_animationLoop = new DockRenderLoop(OnFrame);
		Unloaded += (object _, RoutedEventArgs _) =>
		{
			ResetHover();
		};
		IsVisibleChanged += (object _, DependencyPropertyChangedEventArgs e) =>
		{
			object newValue = e.NewValue;
			if (newValue is bool && !(bool)newValue)
			{
				ResetHover();
			}
		};
	}

	public void SetTiles(IReadOnlyList<DockTileModel> tiles)
	{
		ArgumentNullException.ThrowIfNull(tiles, "tiles");
		_models.Clear();
		_models.AddRange(tiles);
		AppTileCount = 0;
		using (List<DockTileModel>.Enumerator enumerator = _models.GetEnumerator())
		{
			while (enumerator.MoveNext() && !DockLayoutPolicy.IsEntryTile(enumerator.Current.EntryId))
			{
				AppTileCount++;
			}
		}
		_canvas.Children.Clear();
		_tiles.Clear();
		_pressGlows.Clear();
		for (int i = 0; i < _models.Count; i++)
		{
			bool draggable = i < AppTileCount;
			Border border = CreateTile(_models[i], draggable);
			_tiles.Add(border);
			_canvas.Children.Add(border);
		}
		_canvas.Children.Add(_groupDivider);
		_animationLoop.Stop();
		_pointerActive = false;
		_pointerPosition = double.NaN;
		_hoveredTileIndex = -1;
		_pressedTileIndex = -1;
		_pressScale = Enumerable.Repeat(1.0, _tiles.Count).ToArray();
		_pressVelocity = new double[_tiles.Count];
		_pressGlow = new double[_tiles.Count];
		UpdateTileLayout();
		PaintFrame(null);
	}

	public void SetMetrics(double iconSize, double spacing, double peak, double verticalPadding, double maxContentWidth)
	{
		_iconSize = Math.Max(1.0, iconSize);
		_spacing = Math.Max(0.0, spacing);
		_peak = Math.Max(1.0, peak);
		_verticalPadding = Math.Max(0.0, verticalPadding);
		_maxContentWidth = Math.Max(0.0, maxContentWidth);
		MaxWidth = ((_maxContentWidth > 0.0) ? _maxContentWidth : double.PositiveInfinity);
		UpdateTileLayout();
		PaintFrame(CurrentPointerOrNull());
	}

	public void NotifyPointerLeft()
	{
		_pointerActive = false;
		_hoveredTileIndex = -1;
		EnsureAnimating();
	}

	private Border CreateTile(DockTileModel model, bool draggable)
	{
		ImageSource icon = model.Icon;
		FrameworkElement element;
		if (icon != null)
		{
			Image image = new Image
			{
				Source = icon,
				Stretch = Stretch.Uniform,
				HorizontalAlignment = HorizontalAlignment.Center,
				Opacity = (model.IsMissing ? 0.4 : 1.0)
			};
			RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
			Bind(image, FrameworkElement.WidthProperty, model, "IconSize");
			Bind(image, FrameworkElement.HeightProperty, model, "IconSize");
			element = image;
		}
		else
		{
			element = new TextBlock
			{
				Text = (model.Glyph ?? "?"),
				Opacity = (model.IsMissing ? 0.4 : 1.0),
				FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			Bind(element, TextBlock.FontSizeProperty, model, "IconSize");
		}
		AutomationProperties.SetName(element, model.DisplayName);
		AutomationProperties.SetAutomationId(element, "dock-tile:" + model.EntryId);
		Border element2 = new Border
		{
			Height = 5.0,
			CornerRadius = new CornerRadius(2.5),
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0.0, 2.0, 0.0, 0.0)
		};
		Bind(element2, FrameworkElement.WidthProperty, model, "IndicatorWidth");
		Bind(element2, Border.BackgroundProperty, model, "IndicatorBrush");
		Bind(element2, UIElement.VisibilityProperty, model, "RunningVisibility");
		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(element);
		stackPanel.Children.Add(element2);
		RadialGradientBrush radialGradientBrush = new RadialGradientBrush
		{
			Center = new Point(0.5, 0.48),
			GradientOrigin = new Point(0.5, 0.48),
			RadiusX = 0.72,
			RadiusY = 0.72,
			Opacity = 0.0,
			GradientStops = new GradientStopCollection
			{
				new GradientStop(Color.FromArgb(75, byte.MaxValue, byte.MaxValue, byte.MaxValue), 0.0),
				new GradientStop(Color.FromArgb(24, byte.MaxValue, byte.MaxValue, byte.MaxValue), 0.55),
				new GradientStop(Colors.Transparent, 1.0)
			}
		};
		_pressGlows.Add(radialGradientBrush);
		Border border = new Border
		{
			Child = stackPanel,
			RenderTransform = new ScaleTransform(1.0, 1.0),
			RenderTransformOrigin = new Point(0.5, 0.5),
			Cursor = Cursors.Hand,
			Background = radialGradientBrush,
			ToolTip = model.ToolTipText,
			DataContext = model
		};
		if (draggable)
		{
			border.MouseLeftButtonDown += OnTileMouseDown;
			border.MouseMove += OnTileMouseMove;
		}
		border.MouseLeftButtonUp += OnTileMouseUp;
		border.MouseRightButtonUp += OnTileContextMenu;
		return border;
	}

	private void OnTileMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is Border { DataContext: var dataContext } border))
		{
			return;
		}
		DockTileModel model = dataContext as DockTileModel;
		if (model == null)
		{
			return;
		}
		_pressedEntryId = model.EntryId;
		_pressPosition = e.GetPosition(this);
		_pressedTileIndex = _models.FindIndex((DockTileModel item) => item.EntryId == model.EntryId);
		EnsureAnimating();
		_pressTile = border;
		_pressGrabPoint = e.GetPosition(border);
		_pressTimer?.Stop();
		_pressTimer = new DispatcherTimer(DispatcherPriority.Input, Dispatcher)
		{
			Interval = TimeSpan.FromMilliseconds(350L)
		};
		_pressTimer.Tick += (object? _, EventArgs _) =>
		{
			_pressTimer?.Stop();
			_pressTimer = null;
			if (_pressedEntryId != null && _pressTile != null && Mouse.LeftButton == MouseButtonState.Pressed)
			{
				StartLongPressDrag(_pressTile);
			}
		};
		_pressTimer.Start();
	}

	private void OnTileMouseMove(object sender, MouseEventArgs e)
	{
		if (_longPressDrag)
		{
			UpdateLongPressDrag();
			return;
		}
		if (_pressTimer != null && _pressedEntryId != null && (e.GetPosition(this) - _pressPosition).Length >= 4.0)
		{
			_pressTimer.Stop();
			_pressTimer = null;
			_pressTile = null;
		}
		if (_pressedEntryId == null || _dragActive || e.LeftButton != MouseButtonState.Pressed || _reorder == null || (e.GetPosition(this) - _pressPosition).Length < 4.0)
		{
			return;
		}
		_dragActive = true;
		_pressedTileIndex = -1;
		EnsureAnimating();
		_droppedInStrip = false;
		_dragCancelReason = DragCancelReason.None;
		string pressedEntryId = _pressedEntryId;
		string[] array = ReorderCandidateIds.ToArray();
		_dragStartIndex = Array.IndexOf(array, pressedEntryId);
		_reorder.Begin(array);
		DataObject dataObject = new DataObject("GlassDesk-TileDrag", pressedEntryId);
		dataObject.SetData(DataFormats.Serializable, pressedEntryId);
		try
		{
			DragDrop.DoDragDrop(this, dataObject, DragDropEffects.Move);
			bool endedOutside = DragEndedOutside();
			bool cancelled = ResolveCancelReason();
			DragEndAction dragEndAction = DockDragGesture.DecideEnd(_droppedInStrip, endedOutside, cancelled);
			bool flag = (uint)(dragEndAction - 1) <= 1u;
			if (flag && _reorder.IsActive)
			{
				_reorder.Cancel();
			}
			if (dragEndAction == DragEndAction.CancelAndUnpin && !string.IsNullOrEmpty(pressedEntryId))
			{
				UnpinRequested?.Invoke(this, pressedEntryId);
			}
			if (dragEndAction != DragEndAction.KeepCommitted || !_reorder.IsActive)
			{
				ReorderFinished?.Invoke(this, EventArgs.Empty);
			}
		}
		finally
		{
			_dragActive = false;
			_droppedInStrip = false;
			_pressedEntryId = null;
			_pressTile = null;
			_dragStartIndex = -1;
		}
	}

	private void StartLongPressDrag(Border tile)
	{
		if (_reorder != null && _pressedEntryId != null && !_dragActive)
		{
			_longPressDrag = true;
			_dragActive = true;
			_pressedTileIndex = -1;
			_droppedInStrip = false;
			_dragCancelReason = DragCancelReason.None;
			string value = (_dragGhostEntryId = _pressedEntryId);
			string[] array = ReorderCandidateIds.ToArray();
			_dragStartIndex = Array.IndexOf(array, value);
			_reorder.Begin(array);
			Canvas canvas = (_ghostLayer = DragOverlay ?? _dragOverlay);
			_dragGhost = CreateGhost(tile);
			canvas.Children.Add(_dragGhost);
			Point point = tile.TranslatePoint(new Point(0.0, 0.0), canvas);
			_ghostGrab = new Point(_pressGrabPoint.X, _pressGrabPoint.Y);
			_dragGhostMotion = new TranslateTransform(point.X, point.Y);
			TransformGroup transformGroup = new TransformGroup();
			transformGroup.Children.Add(new ScaleTransform(1.12, 1.12));
			transformGroup.Children.Add(_dragGhostMotion);
			_dragGhost.RenderTransform = transformGroup;
			_dragGhost.RenderTransformOrigin = new Point(0.5, 0.5);
			tile.Opacity = 0.0;
			tile.IsHitTestVisible = false;
			Mouse.Capture(this, CaptureMode.SubTree);
			_animationLoop.Start();
		}
	}

	private void UpdateLongPressDrag()
	{
		if (_dragGhost != null && _dragGhostMotion != null)
		{
			Canvas canvas = _ghostLayer ?? _dragOverlay;
			double num = ((_dragGhost.Width > 0.0) ? _dragGhost.Width : _iconSize);
			double num2 = ((_dragGhost.Height > 0.0) ? _dragGhost.Height : _iconSize);
			Point position = Mouse.GetPosition(canvas);
			double value = position.X - _ghostGrab.X;
			double value2 = position.Y - _ghostGrab.Y;
			value = Math.Clamp(value, 0.0, Math.Max(0.0, canvas.ActualWidth - num));
			value2 = Math.Clamp(value2, 0.0, Math.Max(0.0, canvas.ActualHeight - num2));
			_dragGhostMotion.X = value;
			_dragGhostMotion.Y = value2;
			DockReorderModel? reorder = _reorder;
			if (reorder != null && reorder.IsActive && _dragGhostEntryId != null)
			{
				Point point = new Point(value + num / 2.0, value2 + num2 / 2.0);
				double num3 = PointFromScreen(canvas.PointToScreen(point)).X + _scroller.HorizontalOffset;
				double num4 = DockMagnification.SlotCenter(0, _iconSize, _spacing, _peak);
				int targetIndex = (int)Math.Round((num3 - num4) / (_iconSize + _spacing));
				_reorder.PreviewMove(_dragGhostEntryId, targetIndex);
				ApplyPreviewOrder();
			}
		}
	}

	private void ApplyPreviewOrder()
	{
		if (_reorder == null || AppTileCount < 2)
		{
			return;
		}
		IReadOnlyList<string> order = _reorder.Order;
		if (order.Count != AppTileCount)
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < AppTileCount; i++)
		{
			if (!string.Equals(_models[i].EntryId, order[i], StringComparison.Ordinal))
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return;
		}
		string pressedId = ((_pressedTileIndex >= 0 && _pressedTileIndex < _models.Count) ? _models[_pressedTileIndex].EntryId : null);
		Dictionary<string, (DockTileModel, Border, TileGeometry, RadialGradientBrush, double, double, double)> dictionary = new Dictionary<string, (DockTileModel, Border, TileGeometry, RadialGradientBrush, double, double, double)>(AppTileCount, StringComparer.Ordinal);
		for (int j = 0; j < AppTileCount; j++)
		{
			dictionary[_models[j].EntryId] = (_models[j], _tiles[j], _paintedGeometry[j], _pressGlows[j], _pressScale[j], _pressVelocity[j], _pressGlow[j]);
		}
		for (int k = 0; k < AppTileCount; k++)
		{
			if (!dictionary.TryGetValue(order[k], out var value))
			{
				return;
			}
			_models[k] = value.Item1;
			_tiles[k] = value.Item2;
			_paintedGeometry[k] = value.Item3;
			_pressGlows[k] = value.Item4;
			_pressScale[k] = value.Item5;
			_pressVelocity[k] = value.Item6;
			_pressGlow[k] = value.Item7;
		}
		_pressedTileIndex = ((pressedId == null) ? (-1) : _models.FindIndex((DockTileModel model) => model.EntryId == pressedId));
		ThemeService? current = ThemeService.Current;
		if (current != null && !current.AnimationsEnabled)
		{
			PaintFrame(CurrentPointerOrNull());
			return;
		}
		PaintGeometry(_paintedGeometry);
		if (IsVisible)
		{
			_animationLoop.Start();
		}
	}

	private void EndLongPressDrag(bool commit)
	{
		if (!_longPressDrag)
		{
			return;
		}
		string dragGhostEntryId = _dragGhostEntryId;
		bool flag = _reorder?.IsActive ?? false;
		if (_pressTile != null)
		{
			_pressTile.Opacity = 1.0;
			_pressTile.IsHitTestVisible = true;
		}
		if (_dragGhost != null)
		{
			(_ghostLayer ?? _dragOverlay).Children.Remove(_dragGhost);
			_dragGhost = null;
		}
		_ghostLayer = null;
		_dragGhostMotion = null;
		_dragGhostEntryId = null;
		_longPressDrag = false;
		_dragActive = false;
		_droppedInStrip = false;
		_pressedEntryId = null;
		_pressTile = null;
		_dragStartIndex = -1;
		_pressedTileIndex = -1;
		if (Mouse.Captured == this)
		{
			Mouse.Capture(null);
		}
		if ((commit & flag) && dragGhostEntryId != null && _reorder != null)
		{
			if (_reorder.Commit() == ReorderOutcome.SaveFailed)
			{
				MessageBox.Show("排序没能保存，可能是数据文件暂时写不了。顺序已恢复原样。", "GlassDesk", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
			ApplyPreviewOrder();
			ReorderFinished?.Invoke(this, EventArgs.Empty);
		}
		else if (flag && _reorder != null)
		{
			_reorder.Cancel();
			ApplyPreviewOrder();
			ReorderFinished?.Invoke(this, EventArgs.Empty);
		}
	}

	private Border CreateGhost(Border source)
	{
		if (!(source.DataContext is DockTileModel dockTileModel))
		{
			return new Border();
		}
		ImageSource icon = dockTileModel.Icon;
		FrameworkElement child = ((icon == null) ? ((FrameworkElement)new TextBlock
		{
			Text = (dockTileModel.Glyph ?? "?"),
			FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			FontSize = dockTileModel.IconSize
		}) : ((FrameworkElement)new Image
		{
			Source = icon,
			Stretch = Stretch.Uniform,
			HorizontalAlignment = HorizontalAlignment.Center,
			Width = dockTileModel.IconSize,
			Height = dockTileModel.IconSize
		}));
		return new Border
		{
			Width = ((source.ActualWidth > 0.0) ? source.ActualWidth : _iconSize),
			Height = ((source.ActualHeight > 0.0) ? source.ActualHeight : (_iconSize + 12.0)),
			Background = Brushes.Transparent,
			Child = child,
			IsHitTestVisible = false,
			Effect = new DropShadowEffect
			{
				BlurRadius = 14.0,
				ShadowDepth = 5.0,
				Opacity = 0.28
			}
		};
	}

	private void OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (_longPressDrag && e.ChangedButton == MouseButton.Left)
		{
			e.Handled = true;
			UpdateLongPressDrag();
			EndLongPressDrag(commit: true);
		}
		_pressTimer?.Stop();
		_pressTimer = null;
		_pressedEntryId = null;
		_pressedTileIndex = -1;
	}

	private void OnPreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (_longPressDrag && e.Key == Key.Escape)
		{
			e.Handled = true;
			EndLongPressDrag(commit: false);
		}
	}

	private bool ResolveCancelReason()
	{
		if (_dragCancelReason != DragCancelReason.None)
		{
			return true;
		}
		if (InputState.IsPrimaryMouseButtonDown())
		{
			_dragCancelReason = DragCancelReason.PrimaryButtonStillDown;
			return true;
		}
		return false;
	}

	private void OnQueryContinueDrag(object sender, QueryContinueDragEventArgs e)
	{
		if (e.EscapePressed)
		{
			_dragCancelReason = DragCancelReason.Escape;
		}
	}

	private bool DragEndedOutside()
	{
		(int, int)? cursorPixelPoint = DisplayService.GetCursorPixelPoint();
		if (cursorPixelPoint.HasValue)
		{
			(int, int) valueOrDefault = cursorPixelPoint.GetValueOrDefault();
			Point point;
			try
			{
				point = PointFromScreen(new Point(valueOrDefault.Item1, valueOrDefault.Item2));
			}
			catch (Exception ex) when ((ex is InvalidOperationException || ex is ArgumentException) ? true : false)
			{
				return false;
			}
			if (double.IsNaN(point.X) || double.IsNaN(point.Y))
			{
				return false;
			}
			if (!(point.X < -80.0) && !(point.X > ActualWidth + 80.0) && !(point.Y < -80.0))
			{
				return point.Y > ActualHeight + 80.0;
			}
			return true;
		}
		return false;
	}

	protected override void OnDragOver(DragEventArgs e)
	{
		base.OnDragOver(e);
		if (IsInternalTileDrag(e.Data, out string entryId) && _reorder != null)
		{
			e.Effects = DragDropEffects.Move;
			e.Handled = true;
			PreviewReorderAtEventPoint(e, entryId);
		}
	}

	protected override void OnDrop(DragEventArgs e)
	{
		base.OnDrop(e);
		if (IsInternalTileDrag(e.Data, out string entryId) && _reorder != null)
		{
			e.Handled = true;
			_droppedInStrip = true;
			PreviewReorderAtEventPoint(e, entryId);
			if (_reorder.Commit() == ReorderOutcome.SaveFailed)
			{
				MessageBox.Show("排序没能保存，可能是数据文件暂时写不了。顺序已恢复原样。", "GlassDesk", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
			ReorderFinished?.Invoke(this, EventArgs.Empty);
		}
	}

	private void PreviewReorderAtEventPoint(DragEventArgs e, string entryId)
	{
		if (_dragStartIndex < 0)
		{
			return;
		}
		DockReorderModel? reorder = _reorder;
		if (reorder != null && reorder.IsActive)
		{
			double x = e.GetPosition(_scroller).X;
			if (!double.IsNaN(x))
			{
				int num = IndexForInsert(ToContentX(x));
				int targetIndex = ((num > _dragStartIndex) ? (num - 1) : num);
				_reorder.PreviewMove(entryId, targetIndex);
				ApplyPreviewOrder();
			}
		}
	}

	private bool IsInternalTileDrag(IDataObject data, out string entryId)
	{
		if (_dragActive && data.GetDataPresent("GlassDesk-TileDrag"))
		{
			string text = data.GetData("GlassDesk-TileDrag") as string;
			if (!string.IsNullOrEmpty(text))
			{
				entryId = text;
				return true;
			}
		}
		entryId = string.Empty;
		return false;
	}

	private int IndexForInsert(double contentPosition)
	{
		if (AppTileCount == 0)
		{
			return 0;
		}
		TileGeometry[] array = DockMagnification.Compute(_models.Count, _iconSize, _spacing, _peak, null);
		for (int i = 0; i < AppTileCount; i++)
		{
			if (contentPosition < array[i].Center)
			{
				return i;
			}
		}
		return AppTileCount;
	}

	private void OnTileMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (_longPressDrag || _dragActive || e.Handled)
		{
			return;
		}
		_pressedTileIndex = -1;
		EnsureAnimating();
		double? num = PointerViewportPosition();
		if (num.HasValue)
		{
			double valueOrDefault = num.GetValueOrDefault();
			int num2 = ResolveHitAtViewportX(valueOrDefault);
			if (num2 >= 0 && num2 < _models.Count)
			{
				TileClicked?.Invoke(this, _models[num2].EntryId);
			}
		}
	}

	private void OnTileContextMenu(object sender, MouseButtonEventArgs e)
	{
		if (sender is Border { DataContext: DockTileModel dataContext })
		{
			string entryId = dataContext.EntryId;
			if (!entryId.StartsWith("entry:", StringComparison.Ordinal))
			{
				TileContextMenuRequested?.Invoke(this, entryId);
			}
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		if (_longPressDrag)
		{
			UpdateLongPressDrag();
			return;
		}
		_pointerActive = true;
		double? num = PointerContentPosition();
		if (num.HasValue)
		{
			double valueOrDefault = num.GetValueOrDefault();
			_pointerPosition = valueOrDefault;
		}
		EnsureAnimating();
	}

	protected override void OnMouseLeave(MouseEventArgs e)
	{
		base.OnMouseLeave(e);
		NotifyPointerLeft();
	}

	protected override void OnLostMouseCapture(MouseEventArgs e)
	{
		base.OnLostMouseCapture(e);
		if (_longPressDrag && !IsMouseCaptureWithin)
		{
			EndLongPressDrag(commit: false);
		}
	}

	protected override void OnMouseWheel(MouseWheelEventArgs e)
	{
		base.OnMouseWheel(e);
		if (CanScroll)
		{
			_scroller.ScrollToHorizontalOffset(_scroller.HorizontalOffset - (double)e.Delta);
			e.Handled = true;
		}
	}

	private double? PointerViewportPosition()
	{
		Point position = Mouse.GetPosition(this);
		if (!double.IsNaN(position.X))
		{
			return position.X;
		}
		return null;
	}

	private double? PointerContentPosition()
	{
		Point position = Mouse.GetPosition(_canvas);
		if (!double.IsNaN(position.X))
		{
			return position.X;
		}
		return null;
	}

	public double ToContentX(double viewportX)
	{
		return viewportX + _scroller.HorizontalOffset;
	}

	public int ResolveHitAtViewportX(double viewportX)
	{
		return ResolveHit(ToContentX(viewportX));
	}

	public void ScrollToHorizontalOffset(double offset)
	{
		_scroller.ScrollToHorizontalOffset(offset);
	}

	public void ScrollToEnd()
	{
		_scroller.ScrollToRightEnd();
	}

	private void EnsureAnimating()
	{
		if (IsVisible)
		{
			_animationLoop.Start();
		}
		else
		{
			PaintFrame(CurrentPointerOrNull());
		}
	}

	private bool OnFrame(double deltaSeconds)
	{
		if (_longPressDrag)
		{
			if (Keyboard.IsKeyDown(Key.Escape))
			{
				_dragCancelReason = DragCancelReason.Escape;
				EndLongPressDrag(commit: false);
			}
			else if (Mouse.LeftButton != MouseButtonState.Pressed)
			{
				EndLongPressDrag(commit: true);
			}
			else
			{
				UpdateLongPressDrag();
			}
		}
		double? pointerPosition = CurrentPointerOrNull();
		int hoveredTileIndex;
		if (pointerPosition.HasValue)
		{
			double valueOrDefault = pointerPosition.GetValueOrDefault();
			hoveredTileIndex = FindHoveredTile(valueOrDefault);
		}
		else
		{
			hoveredTileIndex = -1;
		}
		_hoveredTileIndex = hoveredTileIndex;
		ThemeService? current = ThemeService.Current;
		if (current != null && !current.AnimationsEnabled)
		{
			SnapPressFeedback();
			PaintFrame(pointerPosition);
			return !_longPressDrag;
		}
		if (AnimateFrame(pointerPosition, deltaSeconds))
		{
			return !_longPressDrag;
		}
		return false;
	}

	private void ResetHover()
	{
		_animationLoop.Stop();
		_pointerActive = false;
		_pointerPosition = double.NaN;
		_hoveredTileIndex = -1;
		PaintFrame(null);
		if (_longPressDrag)
		{
			EndLongPressDrag(commit: false);
		}
		_pressTimer?.Stop();
		_pressTimer = null;
		_pressTile = null;
		_pressedEntryId = null;
	}

	public void PaintFrame(double? pointerPosition)
	{
		DockMagnification.ComputeInto(_paintedGeometry, _iconSize, _spacing, _peak, pointerPosition);
		ApplyHoveredPeak(_paintedGeometry, pointerPosition);
		PaintGeometry(_paintedGeometry);
	}

	private bool AnimateFrame(double? pointerPosition, double deltaSeconds)
	{
		TileGeometry[] targetGeometry = _targetGeometry;
		DockMagnification.ComputeInto(targetGeometry, _iconSize, _spacing, _peak, pointerPosition);
		ApplyHoveredPeak(targetGeometry, pointerPosition);
		TileGeometry[] paintedGeometry = _paintedGeometry;
		bool result = AdvancePressFeedback(deltaSeconds);
		for (int i = 0; i < targetGeometry.Length; i++)
		{
			double scale = paintedGeometry[i].Scale;
			double scale2 = targetGeometry[i].Scale;
			double num = DockHoverScale.Advance(scale, scale2, deltaSeconds);
			double num2 = DockHoverScale.Advance(paintedGeometry[i].Center, targetGeometry[i].Center, deltaSeconds);
			if (num != scale2 || num2 != targetGeometry[i].Center)
			{
				result = false;
			}
			paintedGeometry[i] = new TileGeometry(num2, num);
		}
		PaintGeometry(paintedGeometry);
		return result;
	}

	private bool AdvancePressFeedback(double deltaSeconds)
	{
		if (_pressScale.Length != _tiles.Count)
		{
			return true;
		}
		double num = Math.Clamp(deltaSeconds, 0.0, 0.05);
		bool result = true;
		for (int i = 0; i < _pressScale.Length; i++)
		{
			double num2 = ((i == _pressedTileIndex) ? 0.94 : 1.0);
			double num3 = _pressScale[i] - num2;
			double num4 = _pressVelocity[i];
			double num5 = 22.0 * Math.Sqrt(0.5375999999999999);
			double num6 = Math.Exp(-14.96 * num);
			double num7 = Math.Sin(num5 * num);
			double num8 = Math.Cos(num5 * num);
			double num9 = (num4 + 14.96 * num3) / num5;
			double num10 = num3 * num8 + num9 * num7;
			double num11 = num2 + num6 * num10;
			double num12 = num6 * (num5 * ((0.0 - num3) * num7 + num9 * num8) - 14.96 * num10);
			if (Math.Abs(num11 - num2) < 0.0005 && Math.Abs(num12) < 0.01)
			{
				num11 = num2;
				num12 = 0.0;
			}
			else
			{
				result = false;
			}
			_pressScale[i] = Math.Clamp(num11, 0.9, 1.04);
			_pressVelocity[i] = num12;
			double num13 = ((i == _pressedTileIndex) ? 1.0 : 0.0);
			_pressGlow[i] += (num13 - _pressGlow[i]) * (1.0 - Math.Exp(-18.0 * num));
			if (Math.Abs(_pressGlow[i] - num13) < 0.005)
			{
				_pressGlow[i] = num13;
			}
			else
			{
				result = false;
			}
			if (i < _pressGlows.Count)
			{
				_pressGlows[i].Opacity = 0.46 * _pressGlow[i];
			}
		}
		return result;
	}

	private void SnapPressFeedback()
	{
		for (int i = 0; i < _pressScale.Length; i++)
		{
			_pressScale[i] = ((i == _pressedTileIndex) ? 0.94 : 1.0);
			_pressVelocity[i] = 0.0;
			_pressGlow[i] = 0.0;
			if (i < _pressGlows.Count)
			{
				_pressGlows[i].Opacity = 0.0;
			}
		}
	}

	private void ApplyHoveredPeak(TileGeometry[] geometry, double? pointerPosition)
	{
		if (pointerPosition.HasValue && _hoveredTileIndex >= 0 && _hoveredTileIndex < geometry.Length)
		{
			geometry[_hoveredTileIndex] = new TileGeometry(geometry[_hoveredTileIndex].Center, _peak);
		}
	}

	private int FindHoveredTile(double contentPosition)
	{
		int result = -1;
		double num = double.PositiveInfinity;
		double num2 = _iconSize * _peak / 2.0;
		for (int i = 0; i < _models.Count; i++)
		{
			double num3 = DockMagnification.SlotCenter(i, _iconSize, _spacing, _peak);
			double num4 = Math.Abs(contentPosition - num3);
			if (num4 <= num2 && num4 < num)
			{
				result = i;
				num = num4;
			}
		}
		return result;
	}

	private void UpdateTileLayout()
	{
		int count = _models.Count;
		if (_paintedGeometry.Length != count)
		{
			_paintedGeometry = new TileGeometry[count];
			_targetGeometry = new TileGeometry[count];
			_paintOrder = Enumerable.Range(0, count).ToArray();
		}
		DockMagnification.ComputeInto(_paintedGeometry, _iconSize, _spacing, _peak, null);
		_canvas.Width = DockMagnification.RequiredLength(count, _iconSize, _spacing, _peak);
		_canvas.Height = RequiredThickness;
		double length = (_canvas.Height - _iconSize) / 2.0;
		for (int i = 0; i < count; i++)
		{
			Canvas.SetLeft(_tiles[i], _paintedGeometry[i].Center - _iconSize / 2.0);
			Canvas.SetTop(_tiles[i], length);
		}
		UpdateGroupDivider(_paintedGeometry);
		UpdateFades();
	}

	private void PaintGeometry(TileGeometry[] geometry)
	{
		int[] paintOrder = _paintOrder;
		if (!IsPaintOrderCurrent(paintOrder))
		{
			Array.Sort(paintOrder, _paintOrderComparer);
		}
		for (int i = 0; i < paintOrder.Length; i++)
		{
			int num = paintOrder[i];
			Border border = _tiles[num];
			double num2 = geometry[num].Scale * ((num < _pressScale.Length) ? _pressScale[num] : 1.0);
			double num3 = geometry[num].Center - _iconSize / 2.0;
			if (Canvas.GetLeft(border) != num3)
			{
				Canvas.SetLeft(border, num3);
			}
			int num4 = paintOrder.Length - i;
			if (Panel.GetZIndex(border) != num4)
			{
				Panel.SetZIndex(border, num4);
			}
			if (border.RenderTransform is ScaleTransform scaleTransform)
			{
				if (scaleTransform.ScaleX != num2)
				{
					scaleTransform.ScaleX = num2;
				}
				if (scaleTransform.ScaleY != num2)
				{
					scaleTransform.ScaleY = num2;
				}
			}
		}
	}

	private bool IsPaintOrderCurrent(int[] order)
	{
		for (int i = 1; i < order.Length; i++)
		{
			if (_paintOrderComparer.Compare(order[i - 1], order[i]) > 0)
			{
				return false;
			}
		}
		return true;
	}

	private void UpdateGroupDivider(TileGeometry[] geometry)
	{
		if (AppTileCount == 0 || AppTileCount >= geometry.Length)
		{
			_groupDivider.Visibility = Visibility.Collapsed;
			return;
		}
		double center = geometry[AppTileCount - 1].Center;
		double center2 = geometry[AppTileCount].Center;
		Canvas.SetLeft(_groupDivider, (center + center2) / 2.0);
		Canvas.SetTop(_groupDivider, _iconSize * 0.15);
		_groupDivider.Height = _iconSize * 0.7;
		_groupDivider.Visibility = Visibility.Visible;
		Panel.SetZIndex(_groupDivider, -1);
	}

	public int ResolveHit(double pointerPosition)
	{
		if (_paintedGeometry.Length != 0)
		{
			return DockMagnification.HitTest(_paintedGeometry, _iconSize, pointerPosition);
		}
		return -1;
	}

	private double? CurrentPointerOrNull()
	{
		if (!_pointerActive || double.IsNaN(_pointerPosition))
		{
			return null;
		}
		return _pointerPosition;
	}

	private void UpdateFades()
	{
		bool canScroll = CanScroll;
		_leadingFade.Visibility = ((!canScroll || !(_scroller.HorizontalOffset > 1.0)) ? Visibility.Collapsed : Visibility.Visible);
		double num = Math.Max(0.0, _scroller.ExtentWidth - _scroller.ViewportWidth);
		_trailingFade.Visibility = ((!canScroll || !(_scroller.HorizontalOffset < num - 1.0)) ? Visibility.Collapsed : Visibility.Visible);
	}

	private static Border CreateFade(double direction)
	{
		LinearGradientBrush background = new LinearGradientBrush
		{
			StartPoint = new Point(direction, 0.5),
			EndPoint = new Point(1.0 - direction, 0.5),
			GradientStops = 
			{
				new GradientStop(Color.FromArgb(136, 0, 0, 0), 0.0),
				new GradientStop(Color.FromArgb(0, 0, 0, 0), 1.0)
			}
		};
		return new Border
		{
			Background = background,
			Width = 28.0,
			HorizontalAlignment = ((!(direction < 0.5)) ? HorizontalAlignment.Right : HorizontalAlignment.Left),
			Visibility = Visibility.Collapsed,
			IsHitTestVisible = false
		};
	}

	private static void Bind(FrameworkElement element, DependencyProperty property, object source, string path)
	{
		element.SetBinding(property, new Binding
		{
			Source = source,
			Path = new PropertyPath(path)
		});
	}
}
