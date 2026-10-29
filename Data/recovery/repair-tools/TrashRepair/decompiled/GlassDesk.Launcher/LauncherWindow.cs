#define TRACE
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using GlassDesk.AppServices;
using GlassDesk.Core.Contracts;
using GlassDesk.Core.Models;
using GlassDesk.Core.Models.Settings;
using GlassDesk.Core.Storage;
using GlassDesk.Platform;
using GlassDesk.Platform.Displays;
using GlassDesk.Platform.Shell;
using GlassDesk.UI.Controls;
using GlassDesk.UI.Repair;
using GlassDesk.UI.Theming;
using Microsoft.Win32;

namespace GlassDesk.Launcher;

public class LauncherWindow : GlassWindow, IComponentConnector
{
	private readonly record struct PreparationTiming(string stage, double milliseconds);

	private sealed class AnimationTrace(string directory, bool opening, bool highRefresh, double requestPreparationMilliseconds, double revealPreparationMilliseconds, PreparationTiming[] preparationStages)
	{
		private readonly record struct FrameTiming(double sinceStartMilliseconds, double gapMilliseconds, double updateMilliseconds);

		private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
		{
			WriteIndented = true
		};

		private readonly DateTimeOffset _timestamp = DateTimeOffset.UtcNow;

		private readonly long _started = Stopwatch.GetTimestamp();

		private readonly int _uiThread = Environment.CurrentManagedThreadId;

		private readonly List<FrameTiming> _samples = new List<FrameTiming>(128);

		private long _previous;

		private bool _truncated;

		private static Dictionary<string, object> SelectNumericMetrics(JsonElement value)
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			foreach (JsonProperty item in value.EnumerateObject())
			{
				if (item.Value.ValueKind == JsonValueKind.Object)
				{
					dictionary[item.Name] = SelectNumericMetrics(item.Value);
					continue;
				}
				JsonValueKind valueKind = item.Value.ValueKind;
				if (valueKind - 4 <= JsonValueKind.Array)
				{
					dictionary[item.Name] = item.Value.Clone();
				}
			}
			return dictionary;
		}

		internal void Record(long start)
		{
			if (_samples.Count >= 4096)
			{
				_truncated = true;
				return;
			}
			_samples.Add(new FrameTiming(Stopwatch.GetElapsedTime(_started, start).TotalMilliseconds, (_previous == 0L) ? 0.0 : Stopwatch.GetElapsedTime(_previous, start).TotalMilliseconds, Stopwatch.GetElapsedTime(start).TotalMilliseconds));
			_previous = start;
		}

		internal void Finish(object surfaceMetrics)
		{
			FrameTiming[] samples = _samples.ToArray();
			double elapsed = Stopwatch.GetElapsedTime(_started).TotalMilliseconds;
			Task.Run(delegate
			{
				try
				{
					double[] array = (from s in samples
						where s.gapMilliseconds > 0.0
						select s.gapMilliseconds).ToArray();
					var value = new
					{
						schemaVersion = 1,
						timestampUtc = _timestamp,
						direction = (opening ? "open" : "close"),
						experimentalHighRefreshCapture = highRefresh,
						note = "UI rendering callbacks and CPU stage durations; not displayed GPU FPS. No screenshots or application titles.",
						uiThreadId = _uiThread,
						writerThreadId = Environment.CurrentManagedThreadId,
						requestPreparationMilliseconds = requestPreparationMilliseconds,
						revealPreparationMilliseconds = revealPreparationMilliseconds,
						preparationStages = preparationStages,
						animationMilliseconds = elapsed,
						frameSamples = samples,
						samplesTruncated = _truncated,
						averageGapMilliseconds = ((array.Length == 0) ? 0.0 : array.Average()),
						maximumGapMilliseconds = ((array.Length == 0) ? 0.0 : array.Max()),
						gapsOver50Milliseconds = array.Count((double gap) => gap > 50.0),
						surfaceMetrics = SelectNumericMetrics(JsonSerializer.SerializeToElement(surfaceMetrics))
					};
					Directory.CreateDirectory(directory);
					string text = System.IO.Path.Combine(directory, $"{_timestamp:yyyyMMddTHHmmssfff}-{Guid.NewGuid():N}.json");
					string text2 = text + ".tmp";
					File.WriteAllText(text2, JsonSerializer.Serialize(value, JsonOptions));
					File.Move(text2, text);
				}
				catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException) ? 1 : 0) != 0)
				{
					Trace.WriteLine("Animation timing log unavailable: " + ex.GetType().Name);
				}
			});
		}
	}

	private sealed class GlassPromptWindow : GlassWindow
	{
		public bool UseMenuBackground { get; init; }

		protected override bool UseOpaquePanel => UseMenuBackground;

		protected override double ResolveCornerRadius(AppearanceSettings appearance)
		{
			return 40.0;
		}
	}

	private sealed record FilterTileMotion(FrameworkElement Tile, Transform OriginalTransform, double OriginalOpacity, TranslateTransform Offset, double StartOpacity, double StartOffset, int Row);

	private sealed class IconOffsetMotion(TranslateTransform transform)
	{
		public readonly TranslateTransform Transform = transform;

		public Point Start;

		public Point Target;

		public long Started;

		public bool Active;
	}

	private static class IconDragNative
	{
		internal struct Point
		{
			public int X;

			public int Y;
		}

		[DllImport("user32.dll")]
		internal static extern nint WindowFromPoint(Point point);

		[DllImport("user32.dll")]
		internal static extern nint GetAncestor(nint window, uint flags);
	}

	private readonly record struct TilesRenderKey(long LibraryVersion, long LayoutVersion, string Query, int Page, int Rows, int Columns, double PanelWidth, double PanelHeight, string? OpenGroupId, int IconPixels, bool GroupMode, string? FilterGroupId);

	private readonly bool _experimentalHighRefreshCapture = Environment.GetEnvironmentVariable("GLASSDESK_EXPERIMENTAL_120HZ") == "1";

	private readonly string? _animationTraceDirectory = Environment.GetEnvironmentVariable("GLASSDESK_ANIMATION_TRACE_DIR");

	private AnimationTrace? _animationTrace;

	private long _showRequestedAt;

	private double _lastRevealPreparationMilliseconds;

	private List<PreparationTiming>? _preparationTimings;

	private bool _backgroundWorkSuspended;

	private long _iconRequestGeneration;

	private bool _iconsNeedReapply;

	private PixelRect _transformSourceBounds;

	private PixelRect _transformDestinationBounds;

	private double _transformDipPerPixel = 1.0;

	private double _transformSourceRadiusPixels;

	private double _transformDestinationRadiusPixels;

	private double _transformEndTimeline;

	private long _transformStartedAt;

	private ContainerMorphMotion? _containerMotion;

	private bool _transformActive;

	private bool _hasTransformPath;

	private bool _preserveExpandedHostOnHide;

	private readonly MatrixTransform _glyphRenderTransform = new MatrixTransform();

	private TimeSpan _lastTransformRenderingTime = TimeSpan.MinValue;

	private LauncherEditSession? _dragEdit;

	private string? _pressedId;

	private Point _lastDragPoint;

	private DispatcherTimer? _pressTimer;

	private Button? _pressedTile;

	private Point _grabPoint;

	private readonly LauncherEdgePager _edgePager = new LauncherEdgePager(TimeSpan.FromMilliseconds(600L));

	private bool _dropCommitted;

	private Button? _dropTarget;

	private Brush? _dropTargetOriginalBorderBrush;

	private Thickness _dropTargetOriginalBorderThickness;

	private string? _activeFilterGroupId;

	private LauncherLayout? _flatProjection;

	private (long Version, int Capacity, string? GroupId) _flatProjectionKey;

	private (long Version, bool Enabled)? _filterButtonsKey;

	private bool _filterTransitionActive;

	private bool _filterTransitionClosing;

	private string? _filterTransitionTarget;

	private long _filterTransitionStarted;

	private readonly List<FilterTileMotion> _filterTileMotions = new List<FilterTileMotion>();

	private readonly FolderHoverIntent _folderIntent = new FolderHoverIntent();

	private readonly FolderSpring _fusionSpring = new FolderSpring();

	private LauncherAppTileButton? _folderHoverTile;

	private Rect _folderHoverBounds;

	private SharedGlassLens? _fusionLens;

	private Point _fusionA;

	private Point _ghostIconCenterOffset;

	private string? _dropLandingId;

	private string? _dragSourceGroupId;

	private LauncherLayout? _memberOrigin;

	private int _memberPreviewIndex = -1;

	private bool _memberHasLeft;

	private readonly FolderSpring _folderOpen = new FolderSpring();

	private Rect _folderOrigin;

	private Rect _folderDestination;

	private string? _displayedFolderId;

	private string? _folderItemsKey;

	private bool _folderClosing;

	private bool _folderFramesAttached;

	private long _folderFrameTicks;

	private TimeSpan _folderRenderingTime = TimeSpan.MinValue;

	private LauncherLayout? _dragOrigin;

	private int _dragPageCount;

	private string? _dragIconId;

	private Button? _dragGhost;

	private bool _suppressTileClick;

	private int _previewIndex = -1;

	private bool _previewExchange;

	private LauncherLayout? _layoutBeforePlacement;

	private int _placementCandidateIndex = -1;

	private long _placementCandidateStarted;

	private DateTimeOffset _pagePreviewAfter;

	private readonly Dictionary<string, Button> _dragTiles = new Dictionary<string, Button>(StringComparer.Ordinal);

	private (Point Point, bool Exchange)? _pendingIconPreview;

	private bool _iconPreviewQueued;

	private bool _trackDragPointer;

	private long _lastOlePointerSampleTicks;

	private TimeSpan? _lastIconRenderingTime;

	private TranslateTransform? _dragGhostMotion;

	private long _dragPreviewCommits;

	private double _lastDragPreviewMilliseconds;

	private readonly Dictionary<Button, Point> _dragTileOffsets = new Dictionary<Button, Point>();

	private readonly Dictionary<Button, IconOffsetMotion> _iconOffsetMotions = new Dictionary<Button, IconOffsetMotion>();

	private DispatcherTimer? _noticeTimer;

	private bool _logicalIconDrag;

	private bool _capturedIconDrag;

	private bool _oleIconDrag;

	private bool _oleTransferQueued;

	private bool _resumeCaptureFromOle;

	private long _iconDragSession;

	private Cursor? _cursorBeforeIconDrag;

	private GuidedTour? _onboardingGuide;

	private TilesRenderKey? _lastTilesRenderKey;

	private TilesRenderKey? _retainedTileKey;

	private readonly Dictionary<int, LauncherAppTileButton[]> _retainedTilePages = new Dictionary<int, LauncherAppTileButton[]>();

	private readonly LauncherIconLoader _iconLoader = new LauncherIconLoader();

	private LauncherSettings _settings = new LauncherSettings();

	private readonly LauncherViewState _view = new LauncherViewState(5, 7);

	private AppServiceHost? _services;

	private LauncherWorkspace? _workspace;

	private string? _notice;

	private bool _imeComposing;

	private bool _preparedForShow;

	private int _openContextMenuCount;

	private int _keepOpenWhileModalCount;

	private PixelRect _panelPixelBounds;

	private PixelRect _transformHostBounds;

	private double _panelDipPerPixel = 1.0;

	private Rect? _transitionPanelBoundsDip;

	private double _transitionPanelRadiusDip;

	internal Border ContainerTransformClipRoot;

	internal Grid LauncherGuideRoot;

	internal LiquidGlassSurface GlassSurface;

	internal Grid LauncherPanelRoot;

	internal Grid LauncherContent;

	internal ScaleTransform ContentScaleTransform;

	internal DockPanel SearchHeader;

	internal Button AddAppButton;

	internal TextBox SearchBox;

	internal ScrollViewer FilterGroupBar;

	internal StackPanel FilterGroupButtons;

	internal Grid PageViewport;

	internal ScrollViewer OutgoingPageScroll;

	internal UniformGrid OutgoingPageGrid;

	internal TranslateTransform OutgoingPageMotion;

	internal ScrollViewer PageScroll;

	internal UniformGrid TileGrid;

	internal TranslateTransform IncomingPageMotion;

	internal Grid GroupPanel;

	internal Border FolderDismissArea;

	internal Canvas FolderCanvas;

	internal System.Windows.Shapes.Path FolderShadow;

	internal Grid FolderCard;

	internal SharedGlassLens FolderGlass;

	internal DockPanel GroupContent;

	internal TextBlock GroupTitle;

	internal TextBlock FolderHint;

	internal ScrollViewer GroupScroll;

	internal UniformGrid GroupItems;

	internal PhoneBorder TrashDropTarget;

	internal StackPanel PageControls;

	internal Button PreviousPage;

	internal TextBlock PageText;

	internal Button NextPage;

	internal TextBlock StatusText;

	internal LaunchpadGlyph CollapsedGlyph;

	internal Canvas DragOverlay;

	private bool _contentLoaded;

	private bool UseHighRefreshCapture
	{
		get
		{
			if (!_settings.HighRefreshAnimationsEnabled)
			{
				return _experimentalHighRefreshCapture;
			}
			return true;
		}
	}

	public Func<PixelRect?>? ContainerAnchorProvider { get; set; }

	public bool IsClosingContainer
	{
		get
		{
			if (_transformActive)
			{
				return _transformEndTimeline == 0.0;
			}
			return false;
		}
	}

	private DisplayInfo? TransformDisplay
	{
		get
		{
			PrimaryDisplaySession primaryDisplaySession = base.PrimaryDisplaySession;
			if (primaryDisplaySession == null)
			{
				return DisplayService.Primary();
			}
			if (!primaryDisplaySession.IsAvailable)
			{
				return null;
			}
			return primaryDisplaySession.Display;
		}
	}

	private LauncherLayout BrowseLayout
	{
		get
		{
			if (_dragEdit != null)
			{
				return _dragEdit.Draft;
			}
			if (_workspace == null || _services == null)
			{
				return new LauncherLayout();
			}
			if (!_settings.GroupModeEnabled)
			{
				return _workspace.Layout;
			}
			(long, int, string) tuple = (_services.Layouts.LauncherVersion, _view.Capacity, _activeFilterGroupId);
			if (_flatProjection != null)
			{
				(long, int, string) flatProjectionKey = _flatProjectionKey;
				(long, int, string) tuple2 = tuple;
				if (flatProjectionKey.Item1 == tuple2.Item1 && flatProjectionKey.Item2 == tuple2.Item2 && !(flatProjectionKey.Item3 != tuple2.Item3))
				{
					goto IL_00d9;
				}
			}
			_flatProjection = LauncherFilterRules.Project(_workspace.Layout, _activeFilterGroupId, _view.Capacity);
			_flatProjectionKey = tuple;
			goto IL_00d9;
			IL_00d9:
			return _flatProjection;
		}
	}

	private double AppIconSize => Math.Clamp(Math.Floor(((LauncherPanelRoot.Height - 160.0 - (double)((FilterGroupBar.Visibility == Visibility.Visible) ? 44 : 0)) / (double)_view.Rows - 44.0) / 4.0) * 4.0, 32.0, 64.0);

	protected override bool UseLauncherTextBrightness => true;

	protected override bool UsesSquirclePanelGeometryCore => true;

	public bool CanNavigatePages
	{
		get
		{
			if (base.IsVisible && !_imeComposing && !_transformActive && _keepOpenWhileModalCount == 0 && !_filterTransitionActive && _view.OpenGroupId == null)
			{
				return _displayedFolderId == null;
			}
			return false;
		}
	}

	public event EventHandler? InternalDragStarted;

	public event EventHandler? InternalDragFinished;

	public event EventHandler? NavigationAvailabilityChanged;

	private void MarkPreparationTiming(string stage, ref long started)
	{
		long timestamp = Stopwatch.GetTimestamp();
		_preparationTimings?.Add(new PreparationTiming(stage, Stopwatch.GetElapsedTime(started, timestamp).TotalMilliseconds));
		started = timestamp;
	}

	private void BeginAnimationTrace(bool opening)
	{
		if (!string.IsNullOrWhiteSpace(_animationTraceDirectory))
		{
			_animationTrace = new AnimationTrace(_animationTraceDirectory, opening, UseHighRefreshCapture, (opening && _showRequestedAt != 0L) ? Stopwatch.GetElapsedTime(_showRequestedAt).TotalMilliseconds : 0.0, opening ? _lastRevealPreparationMilliseconds : 0.0, (!opening) ? Array.Empty<PreparationTiming>() : (_preparationTimings?.ToArray() ?? Array.Empty<PreparationTiming>()));
		}
	}

	private void FinishAnimationTrace()
	{
		AnimationTrace? animationTrace = _animationTrace;
		_animationTrace = null;
		animationTrace?.Finish(GlassSurface.InspectPerformance());
	}

	public void RequestGlassRefresh()
	{
		if (base.IsVisible)
		{
			GlassSurface.RequestBackdropRefresh();
		}
	}

	public void SetPerformanceSleeping(bool sleeping)
	{
		GlassSurface.SetPerformanceSleeping(sleeping);
		if (sleeping && !base.IsVisible && base.Handle != 0)
		{
			CancelDrag();
			StopPageSlide();
			_iconRequestGeneration++;
			_retainedTilePages.Clear();
			_retainedTileKey = null;
			_lastTilesRenderKey = null;
			TileGrid.Children.Clear();
			GroupItems.Children.Clear();
			_iconsNeedReapply = true;
			_transformHostBounds = default(PixelRect);
			_hasTransformPath = false;
			_containerMotion = null;
			base.Width = 1.0;
			base.Height = 1.0;
		}
	}

	public void SetBackgroundWorkSuspended(bool suspended)
	{
		if (_backgroundWorkSuspended == suspended)
		{
			return;
		}
		_backgroundWorkSuspended = suspended;
		_iconRequestGeneration++;
		_iconLoader.SetSuspended(suspended);
		if (!suspended && _iconsNeedReapply)
		{
			_iconsNeedReapply = false;
			_lastTilesRenderKey = null;
			_retainedTilePages.Clear();
			if (base.IsVisible && _services != null)
			{
				RenderTiles();
			}
		}
		if (!suspended)
		{
			PreloadIcons();
		}
	}

	public void StopForPrimaryDisplayChange()
	{
		FinishIconDrag();
		AbortContainerTransform();
		Hide();
	}

	public void PrewarmNextRevealMaterial()
	{
		if (base.IsVisible || _transformActive)
		{
			return;
		}
		PixelRect panelPixelBounds = _panelPixelBounds;
		if (panelPixelBounds.Width <= 0 || panelPixelBounds.Height <= 0)
		{
			return;
		}
		DisplayInfo transformDisplay = TransformDisplay;
		PixelRect? pixelRect = ContainerAnchorProvider?.Invoke();
		if ((object)transformDisplay != null && pixelRect.HasValue)
		{
			panelPixelBounds = pixelRect.GetValueOrDefault();
			if (panelPixelBounds.Width > 0 && panelPixelBounds.Height > 0 && transformDisplay.Bounds.Contains(pixelRect.Value.X + pixelRect.Value.Width / 2, pixelRect.Value.Y + pixelRect.Value.Height / 2))
			{
				new WindowInteropHelper(this).EnsureHandle();
				PositionOnDisplay(transformDisplay);
				_transformSourceBounds = pixelRect.Value;
				_transformDestinationBounds = _panelPixelBounds;
				_transformSourceRadiusPixels = (double)Math.Min(_transformSourceBounds.Width, _transformSourceBounds.Height) / 3.0;
				_transformDestinationRadiusPixels = base.PanelCornerRadius.TopLeft * (double)transformDisplay.Dpi / 96.0;
				_transformDipPerPixel = 96.0 / (double)Math.Max(1u, transformDisplay.Dpi);
				_panelDipPerPixel = _transformDipPerPixel;
				ExpandTransformHost();
				PrepareTransitionBackdrop();
				PrepareRevealMaterial();
			}
		}
	}

	public void ShowWithTransition(PixelRect? sourceBounds)
	{
		_showRequestedAt = Stopwatch.GetTimestamp();
		_lastRevealPreparationMilliseconds = 0.0;
		_preparationTimings = (string.IsNullOrWhiteSpace(_animationTraceDirectory) ? null : new List<PreparationTiming>(8));
		long started = _showRequestedAt;
		sourceBounds = ContainerAnchorProvider?.Invoke() ?? sourceBounds;
		PrimaryDisplaySession primaryDisplaySession = base.PrimaryDisplaySession;
		if (primaryDisplaySession != null && !primaryDisplaySession.IsAvailable)
		{
			return;
		}
		DisplayInfo transformDisplay = TransformDisplay;
		if (sourceBounds.HasValue)
		{
			PixelRect valueOrDefault = sourceBounds.GetValueOrDefault();
			if ((object)transformDisplay != null && !transformDisplay.Bounds.Contains(valueOrDefault.X + valueOrDefault.Width / 2, valueOrDefault.Y + valueOrDefault.Height / 2))
			{
				sourceBounds = null;
			}
		}
		MarkPreparationTiming("resolve-anchor-and-display", ref started);
		if (base.IsVisible)
		{
			if (_transformActive)
			{
				if (_transformEndTimeline < 1.0)
				{
					LauncherRevealRepair.BeginWhenReady(this, 1.0);
				}
				return;
			}
			PositionOnWorkingArea();
			PrepareToShow();
			if (!_hasTransformPath)
			{
				return;
			}
			if (sourceBounds.HasValue)
			{
				PixelRect valueOrDefault2 = sourceBounds.GetValueOrDefault();
				if (valueOrDefault2.Width > 0 && valueOrDefault2.Height > 0)
				{
					_transformSourceBounds = valueOrDefault2;
					_transformSourceRadiusPixels = (double)Math.Min(valueOrDefault2.Width, valueOrDefault2.Height) / 3.0;
				}
			}
			PixelRect? pixelRect = ((_panelPixelBounds.Width > 0 && _panelPixelBounds.Height > 0) ? new PixelRect?(_panelPixelBounds) : DisplayService.GetWindowPixelBounds(base.Handle));
			if (pixelRect.HasValue)
			{
				PixelRect valueOrDefault3 = pixelRect.GetValueOrDefault();
				if (valueOrDefault3.Width > 0 && valueOrDefault3.Height > 0)
				{
					_transformDestinationBounds = pixelRect.Value;
					DisplayInfo transformDisplay2 = TransformDisplay;
					_transformDestinationRadiusPixels = base.PanelCornerRadius.TopLeft * (double)(transformDisplay2?.Dpi ?? 96) / 96.0;
					_transformDipPerPixel = 96.0 / (double)Math.Max(1u, transformDisplay2?.Dpi ?? 96);
				}
			}
			return;
		}
		PositionOnWorkingArea();
		if (sourceBounds.HasValue)
		{
			PixelRect valueOrDefault3 = sourceBounds.GetValueOrDefault();
			if (valueOrDefault3.Width > 0 && valueOrDefault3.Height > 0)
			{
				PositionOnDisplay(TransformDisplay);
			}
		}
		MarkPreparationTiming("initial-placement", ref started);
		PrepareToShow();
		MarkPreparationTiming("library-and-tiles", ref started);
		nint window = new WindowInteropHelper(this).EnsureHandle();
		if (sourceBounds.HasValue)
		{
			PixelRect valueOrDefault3 = sourceBounds.GetValueOrDefault();
			if (valueOrDefault3.Width > 0 && valueOrDefault3.Height > 0)
			{
				PositionOnDisplay(TransformDisplay);
				goto IL_034b;
			}
		}
		PositionOnWorkingArea();
		goto IL_034b;
		IL_04e8:
		StopContainerTransform();
		int num;
		PixelRect? pixelRect2;
		if (num == 0)
		{
			_hasTransformPath = false;
			_containerMotion = null;
			if (pixelRect2.HasValue)
			{
				PixelRect valueOrDefault4 = pixelRect2.GetValueOrDefault();
				SetPixelBounds(valueOrDefault4);
			}
			SetFinalVisualState();
			PrepareRevealMaterial();
			LauncherRevealRepair.ShowPrepared(this);
			return;
		}
		_transformSourceBounds = sourceBounds.Value;
		_transformDestinationBounds = pixelRect2.Value;
		_transformSourceRadiusPixels = (double)Math.Min(_transformSourceBounds.Width, _transformSourceBounds.Height) / 3.0;
		DisplayInfo displayInfo;
		_transformDestinationRadiusPixels = base.PanelCornerRadius.TopLeft * (double)displayInfo.Dpi / 96.0;
		_transformDipPerPixel = 96.0 / (double)Math.Max(1u, displayInfo.Dpi);
		_panelDipPerPixel = _transformDipPerPixel;
		_hasTransformPath = true;
		_containerMotion = new ContainerMorphMotion(_transformSourceBounds, _transformDestinationBounds, _transformSourceRadiusPixels, _transformDestinationRadiusPixels);
		ExpandTransformHost();
		PrepareTransitionBackdrop();
		LauncherContent.IsHitTestVisible = false;
		ApplyContainerTransformFrame(_containerMotion.CurrentFrame);
		MarkPreparationTiming("prepare-morph-geometry", ref started);
		PrepareRevealMaterial();
		MarkPreparationTiming("prepare-reveal-material", ref started);
		LauncherRevealRepair.ShowPrepared(this);
		MarkPreparationTiming("show-window", ref started);
		LauncherRevealRepair.BeginWhenReady(this, 1.0);
		return;
		IL_03ea:
		object obj;
		displayInfo = (DisplayInfo)obj;
		object obj2;
		if (sourceBounds.HasValue)
		{
			PixelRect valueOrDefault5 = sourceBounds.GetValueOrDefault();
			if ((object)transformDisplay != null && transformDisplay.Bounds.Contains(valueOrDefault5.X + valueOrDefault5.Width / 2, valueOrDefault5.Y + valueOrDefault5.Height / 2))
			{
				obj2 = transformDisplay;
				goto IL_0444;
			}
		}
		obj2 = null;
		goto IL_0444;
		IL_0444:
		DisplayInfo displayInfo2 = (DisplayInfo)obj2;
		ThemeService? current = ThemeService.Current;
		if ((current == null || current.AnimationsEnabled) && sourceBounds.HasValue)
		{
			PixelRect valueOrDefault3 = sourceBounds.GetValueOrDefault();
			if (valueOrDefault3.Width > 0 && valueOrDefault3.Height > 0 && pixelRect2.HasValue)
			{
				valueOrDefault3 = pixelRect2.GetValueOrDefault();
				if (valueOrDefault3.Width > 0 && valueOrDefault3.Height > 0 && (object)displayInfo != null && (object)displayInfo2 != null)
				{
					num = (string.Equals(displayInfo.DeviceName, displayInfo2.DeviceName, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
					goto IL_04e8;
				}
			}
		}
		num = 0;
		goto IL_04e8;
		IL_034b:
		MarkPreparationTiming("native-host-placement", ref started);
		pixelRect2 = ((_panelPixelBounds.Width > 0 && _panelPixelBounds.Height > 0) ? new PixelRect?(_panelPixelBounds) : DisplayService.GetWindowPixelBounds(window));
		if (pixelRect2.HasValue)
		{
			PixelRect valueOrDefault6 = pixelRect2.GetValueOrDefault();
			if ((object)transformDisplay != null && transformDisplay.Bounds.Contains(valueOrDefault6.X + valueOrDefault6.Width / 2, valueOrDefault6.Y + valueOrDefault6.Height / 2))
			{
				obj = transformDisplay;
				goto IL_03ea;
			}
		}
		obj = null;
		goto IL_03ea;
	}

	public void CloseWithTransition()
	{
		if (!base.IsVisible)
		{
			return;
		}
		StopFilterTransition();
		StopPageSlide();
		if (_transformActive && _transformEndTimeline <= 0.0)
		{
			return;
		}
		if (_hasTransformPath)
		{
			ThemeService? current = ThemeService.Current;
			if (current == null || current.AnimationsEnabled)
			{
				RefreshContainerAnchor();
				ExpandTransformHost();
				LauncherContent.IsHitTestVisible = false;
				BeginContainerTransform(0.0);
				return;
			}
		}
		StopContainerTransform();
		Hide();
		SetFinalVisualState();
	}

	private void BeginContainerTransform(double targetTimeline)
	{
		if (_hasTransformPath && _containerMotion != null)
		{
			StopPageSlide();
			PrepareTransitionBackdrop();
			AdvanceContainerMotion();
			StopContainerTransform();
			_transformEndTimeline = Math.Clamp(targetTimeline, 0.0, 1.0);
			_containerMotion.Retarget(_transformEndTimeline >= 1.0, _transformSourceBounds);
			ApplyContainerTransformFrame(_containerMotion.CurrentFrame);
			if (_containerMotion.IsSettled)
			{
				CompleteContainerTransform(_transformEndTimeline);
				return;
			}
			_transformStartedAt = Stopwatch.GetTimestamp();
			_transformActive = true;
			GlassSurface.SetContainerAnimationActive(UseHighRefreshCapture);
			BeginAnimationTrace(_transformEndTimeline >= 1.0);
			_lastTransformRenderingTime = TimeSpan.MinValue;
			CompositionTarget.Rendering += OnContainerTransformRendering;
			this.NavigationAvailabilityChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	private void OnContainerTransformRendering(object? sender, EventArgs e)
	{
		if (!_transformActive)
		{
			return;
		}
		if (e is RenderingEventArgs e2)
		{
			if (e2.RenderingTime == _lastTransformRenderingTime)
			{
				return;
			}
			_lastTransformRenderingTime = e2.RenderingTime;
		}
		long timestamp = Stopwatch.GetTimestamp();
		RefreshContainerAnchor();
		AdvanceContainerMotion();
		_animationTrace?.Record(timestamp);
		ContainerMorphMotion? containerMotion = _containerMotion;
		if (containerMotion != null && containerMotion.IsSettled)
		{
			CompleteContainerTransform(_transformEndTimeline);
		}
	}

	private void AdvanceContainerMotion()
	{
		if (_transformActive && _containerMotion != null)
		{
			long timestamp = Stopwatch.GetTimestamp();
			_containerMotion.Advance(Stopwatch.GetElapsedTime(_transformStartedAt, timestamp).TotalSeconds);
			_transformStartedAt = timestamp;
			ApplyContainerTransformFrame(_containerMotion.CurrentFrame);
		}
	}

	private void ApplyContainerTransformFrame(ContainerMorphFrame frame)
	{
		Rect rect = new Rect((frame.Bounds.X - (double)_transformHostBounds.X) * _transformDipPerPixel, (frame.Bounds.Y - (double)_transformHostBounds.Y) * _transformDipPerPixel, frame.Bounds.Width * _transformDipPerPixel, frame.Bounds.Height * _transformDipPerPixel);
		double num = frame.CornerRadius * _transformDipPerPixel;
		_transitionPanelBoundsDip = rect;
		_transitionPanelRadiusDip = num;
		Geometry geometry = SquircleGlassGeometry.CreateCompact(rect, num);
		ContainerTransformClipRoot.Clip = geometry;
		SetSharedPanelGeometry(geometry);
		GlassSurface.SetTransitionFrame(rect, num, geometry, frame.GlassStrength, frame.BlurRadius);
		SetTransitionShadowShape(rect, num, 0.35 + 0.65 * frame.GlassStrength, reducedDetail: true);
		base.Opacity = 1.0;
		ApplyContentReveal(frame.ContentOpacity);
		double num2 = Math.Min(64.0, Math.Min(rect.Width, rect.Height));
		_glyphRenderTransform.Matrix = new Matrix(num2 / 48.0, 0.0, 0.0, num2 / 48.0, rect.X + (rect.Width - num2) / 2.0, rect.Y + (rect.Height - num2) / 2.0);
		CollapsedGlyph.RenderTransform = _glyphRenderTransform;
		CollapsedGlyph.Opacity = frame.GlyphOpacity;
	}

	private void CompleteContainerTransform(double timeline)
	{
		StopContainerTransform();
		if (_containerMotion != null)
		{
			_containerMotion.Retarget(timeline >= 1.0, _transformSourceBounds);
			_containerMotion.SnapToTarget();
			ApplyContainerTransformFrame(_containerMotion.CurrentFrame);
		}
		if (timeline >= 1.0)
		{
			ClearContainerTransformShape(RenderCapability.IsPixelShaderVersionSupported(3, 0));
			LauncherContent.IsHitTestVisible = true;
			this.NavigationAvailabilityChanged?.Invoke(this, EventArgs.Empty);
		}
		else
		{
			_preserveExpandedHostOnHide = true;
			Hide();
			SetFinalVisualState(collapseHost: false);
			_hasTransformPath = false;
			_containerMotion = null;
		}
	}

	private void SetPixelBounds(PixelRect bounds)
	{
		DisplayInfo transformDisplay = TransformDisplay;
		if ((object)transformDisplay != null)
		{
			PrimaryDisplaySession primaryDisplaySession = base.PrimaryDisplaySession;
			if (primaryDisplaySession != null)
			{
				bounds = primaryDisplaySession.ClampWindowToWorkArea(bounds);
			}
			double num = 96.0 / (double)Math.Max(1u, transformDisplay.Dpi);
			base.Width = (double)bounds.Width * num;
			base.Height = (double)bounds.Height * num;
			DisplayService.MoveWindowToPixelRect(base.Handle, bounds);
			_transformHostBounds = bounds;
		}
	}

	private void SetFinalVisualState(bool collapseHost = true)
	{
		if (collapseHost)
		{
			CollapseTransformHost();
		}
		ClearContainerTransformShape();
		base.Opacity = 1.0;
		base.PanelCornerRadius = new CornerRadius(Math.Max(80.0, ThemeService.Current?.Appearance.LauncherCornerRadius ?? 80.0));
		GlassSurface.TransitionProgress = 1.0;
		GlassSurface.TransitionBlurRadius = 0.0;
		CollapsedGlyph.Opacity = 0.0;
		LauncherContent.Opacity = 1.0;
		LauncherContent.IsHitTestVisible = true;
		ScaleTransform contentScaleTransform = ContentScaleTransform;
		double scaleX = (ContentScaleTransform.ScaleY = 1.0);
		contentScaleTransform.ScaleX = scaleX;
		ApplyContentReveal(1.0);
	}

	private void StopContainerTransform()
	{
		GlassSurface.SetContainerAnimationActive(active: false);
		FinishAnimationTrace();
		if (_transformActive)
		{
			CompositionTarget.Rendering -= OnContainerTransformRendering;
		}
		_transformActive = false;
	}

	private void AbortContainerTransform()
	{
		StopContainerTransform();
		if (_preserveExpandedHostOnHide)
		{
			_preserveExpandedHostOnHide = false;
			SetPanelRootLayout(_panelPixelBounds, _transformHostBounds, _panelDipPerPixel);
		}
		else
		{
			CollapseTransformHost();
		}
		_hasTransformPath = false;
		_containerMotion = null;
		ClearContainerTransformShape();
		base.Opacity = 1.0;
		GlassSurface.TransitionProgress = 1.0;
		GlassSurface.TransitionBlurRadius = 0.0;
		ApplyContentReveal(1.0);
		LauncherContent.IsHitTestVisible = true;
	}

	private void ClearContainerTransformShape(bool keepBackdrop = false)
	{
		if (!keepBackdrop)
		{
			GlassSurface.SetContainerTransitionBounds(null);
		}
		if (_panelPixelBounds.Width > 0 && _transformHostBounds.Width > 0)
		{
			SetPanelRootLayout(_panelPixelBounds, _transformHostBounds, _panelDipPerPixel);
		}
		CollapsedGlyph.Opacity = 0.0;
	}

	private void PrepareTransitionBackdrop()
	{
		GlassSurface.SetContainerTransitionBounds(new Rect(0.0, 0.0, (double)_transformHostBounds.Width * _transformDipPerPixel, (double)_transformHostBounds.Height * _transformDipPerPixel));
	}

	private void PrepareRevealMaterial()
	{
		long timestamp = Stopwatch.GetTimestamp();
		try
		{
			GlassSurface.PrepareRevealMaterial(_transformHostBounds, _panelPixelBounds, Math.Max(96u, TransformDisplay?.Dpi ?? 96));
		}
		finally
		{
			_lastRevealPreparationMilliseconds = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
		}
	}

	private void RefreshContainerAnchor()
	{
		PixelRect? pixelRect = ContainerAnchorProvider?.Invoke();
		if (!pixelRect.HasValue)
		{
			return;
		}
		PixelRect valueOrDefault = pixelRect.GetValueOrDefault();
		if (valueOrDefault.Width <= 0 || valueOrDefault.Height <= 0 || valueOrDefault == _transformSourceBounds)
		{
			return;
		}
		DisplayInfo transformDisplay = TransformDisplay;
		if ((object)transformDisplay != null && transformDisplay.Bounds.Contains(valueOrDefault.X + valueOrDefault.Width / 2, valueOrDefault.Y + valueOrDefault.Height / 2) && transformDisplay.Bounds.Contains(_transformDestinationBounds.X + _transformDestinationBounds.Width / 2, _transformDestinationBounds.Y + _transformDestinationBounds.Height / 2))
		{
			_transformSourceBounds = valueOrDefault;
			_transformSourceRadiusPixels = (double)Math.Min(valueOrDefault.Width, valueOrDefault.Height) / 3.0;
			_containerMotion?.Retarget(_transformEndTimeline >= 1.0, valueOrDefault);
			if (_transformActive)
			{
				ExpandTransformHost();
			}
		}
	}

	private void ExpandTransformHost()
	{
		PixelRect pixelRect = Union(_transformSourceBounds, _transformDestinationBounds);
		int num = (int)Math.Ceiling((double)_transformDestinationBounds.Width * 0.04);
		int num2 = (int)Math.Ceiling((double)_transformDestinationBounds.Height * 0.04);
		PixelRect pixelRect2 = new PixelRect(pixelRect.X - num, pixelRect.Y - num2, pixelRect.Width + num * 2, pixelRect.Height + num2 * 2);
		PrimaryDisplaySession primaryDisplaySession = base.PrimaryDisplaySession;
		if (primaryDisplaySession != null)
		{
			pixelRect2 = primaryDisplaySession.ClampWindowToWorkArea(pixelRect2);
		}
		PixelRect pixelRect3 = ((_transformHostBounds.Width > 0 && ContainsPixelRect(_transformHostBounds, pixelRect2)) ? _transformHostBounds : pixelRect2);
		if (!(_transformHostBounds == pixelRect3))
		{
			SetPixelBounds(pixelRect3);
			_transformHostBounds = pixelRect3;
			SetPanelRootLayout(_transformDestinationBounds, pixelRect3, _transformDipPerPixel);
		}
	}

	private void CollapseTransformHost()
	{
		PixelRect pixelRect = ((_panelPixelBounds.Width > 0) ? _panelPixelBounds : _transformDestinationBounds);
		if (pixelRect.Width > 0 && pixelRect.Height > 0)
		{
			if (_transformHostBounds != pixelRect)
			{
				SetPixelBounds(pixelRect);
			}
			_transformHostBounds = pixelRect;
			double dipPerPixel = ((_transformDestinationBounds == pixelRect && _transformDestinationBounds.Width > 0) ? _transformDipPerPixel : _panelDipPerPixel);
			SetPanelRootLayout(pixelRect, pixelRect, dipPerPixel);
		}
	}

	private static PixelRect Union(PixelRect left, PixelRect right)
	{
		int num = Math.Min(left.X, right.X);
		int num2 = Math.Min(left.Y, right.Y);
		int num3 = Math.Max(left.Right, right.Right);
		int num4 = Math.Max(left.Bottom, right.Bottom);
		return new PixelRect(num, num2, num3 - num, num4 - num2);
	}

	private void OnAddClick(object sender, RoutedEventArgs e)
	{
		ContextMenu contextMenu = CreateTrackedContextMenu();
		contextMenu.PlacementTarget = sender as Button;
		AddMenu(contextMenu, "从桌面批量添加…", AddFromDesktop);
		AddMenu(contextMenu, "文件、程序或快捷方式…", delegate
		{
			OpenFileDialog picker = CreateImportFilePicker();
			if (ShowModalDialog(() => picker.ShowDialog(this)))
			{
				ImportPaths(picker.FileNames);
			}
		});
		AddMenu(contextMenu, "文件夹…", delegate
		{
			OpenFolderDialog picker = new OpenFolderDialog();
			if (ShowModalDialog(() => picker.ShowDialog(this)))
			{
				ImportPaths(new global::_003C_003Ez__ReadOnlySingleElementList<string>(picker.FolderName));
			}
		});
		AddMenu(contextMenu, "网站…", AddWebsite);
		if (_settings.GroupModeEnabled)
		{
			contextMenu.Items.Add(new Separator());
			AddMenu(contextMenu, "添加分组…", AddFilterGroup);
		}
		contextMenu.IsOpen = true;
	}

	private static OpenFileDialog CreateImportFilePicker()
	{
		return new OpenFileDialog
		{
			Multiselect = true,
			DereferenceLinks = false,
			Filter = "所有文件|*.*"
		};
	}

	private void AddFromDesktop()
	{
		if (_services != null)
		{
			DesktopImportWindow desktopImportWindow = new DesktopImportWindow(DesktopScanner.Scan())
			{
				Owner = this
			};
			if (ShowModalDialog(desktopImportWindow.ShowDialog))
			{
				ImportPaths(desktopImportWindow.SelectedPaths);
			}
		}
	}

	private void ImportPaths(IReadOnlyList<string> paths)
	{
		if (_services == null)
		{
			return;
		}
		try
		{
			List<ImportRequest> requests = paths.Select((string path) => new ImportRequest
			{
				Source = path
			}).ToList();
			HashSet<string> before = LauncherLayoutRules.AppIds(_workspace?.Layout ?? new LauncherLayout()).ToHashSet<string>(StringComparer.Ordinal);
			ImportResult result = _services.Import.ImportItems(requests);
			List<string> list = (from item in result.Added.Concat(result.Duplicates)
				select item.Id).Distinct().ToList();
			int num = list.Count((string id) => !before.Contains(id) && _workspace != null && LauncherLayoutRules.AppIds(_workspace.Layout).Contains(id));
			string summary = $"已添加到启动台 {num} 项，已有 {list.Count - num}，失败 {result.Failures.Count}。";
			if (result.Failures.Count > 0)
			{
				summary = summary + " " + result.Failures[0].Message;
			}
			ShowNotice(summary, result.Failures.Count == 0);
			if (result.Failures.Count > 0)
			{
				ShowModalDialog(delegate
				{
					MessageBox.Show(this, summary + Environment.NewLine + string.Join(Environment.NewLine, result.Failures.Select((ImportFailure failure) => failure.Source + "：" + failure.Message)), "导入结果", MessageBoxButton.OK, MessageBoxImage.Exclamation);
					return true;
				});
			}
			RefreshLibrary();
		}
		catch (StorageException ex)
		{
			ShowNotice("应用库写入失败；请检查数据目录并重试：" + ex.Message);
		}
	}

	private void AddWebsite()
	{
		string[] array = AskFields("添加网站", ("网站地址", "https://"), ("显示名称", ""));
		if (array == null || _services == null)
		{
			return;
		}
		try
		{
			HashSet<string> before = LauncherLayoutRules.AppIds(_workspace?.Layout ?? new LauncherLayout()).ToHashSet<string>(StringComparer.Ordinal);
			ImportResult importResult = _services.Import.ImportItems(new global::_003C_003Ez__ReadOnlySingleElementList<ImportRequest>(new ImportRequest
			{
				Source = array[0],
				DisplayName = array[1],
				IsUrl = true
			}));
			ShowNotice((importResult.Failures.Count > 0) ? importResult.Failures[0].Message : (importResult.Added.Concat(importResult.Duplicates).Any((AppEntry item) => !before.Contains(item.Id)) ? "网站已添加。" : "此网站已在启动台中。"), importResult.Failures.Count == 0);
			RefreshLibrary();
		}
		catch (StorageException ex)
		{
			ShowNotice("网站记录未能保存：" + ex.Message);
		}
	}

	private void RenameGroup(string groupId)
	{
		LauncherGroup launcherGroup = _workspace?.Layout.Pages.SelectMany((LauncherPage page) => page.Slots).SingleOrDefault((LauncherSlot slot) => slot.Group?.Id == groupId)?.Group;
		if (launcherGroup == null)
		{
			return;
		}
		string[] values = AskFields("重命名文件夹", ("名称", launcherGroup.Name));
		if (values != null)
		{
			CommitEdit((LauncherLayout layout) => LauncherLayoutEditor.RenameGroup(layout, groupId, values[0]));
		}
	}

	private void EditEntry(string id)
	{
		AppEntry appEntry = _services?.Library.Find(id);
		if (appEntry == null || _services == null)
		{
			return;
		}
		string[] array = AskFields("编辑应用", ("名称", appEntry.DisplayName), ("搜索别名（逗号分隔）", string.Join(", ", appEntry.SearchAliases)), ("自定义图标路径（留空保持现状）", ""));
		if (array == null)
		{
			return;
		}
		try
		{
			string[] aliases = array[1].Split(new char[2] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (!_services.Editor.Edit(id, array[0], aliases, string.IsNullOrWhiteSpace(array[2]) ? null : array[2]))
			{
				ShowNotice("编辑内容无效；名称应为 1–120 字，图标须为可读取的图片文件。");
				return;
			}
			ShowNotice("应用已更新。", autoDismiss: true);
			RefreshLibrary();
		}
		catch (Exception ex) when (((ex is StorageException || ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			ShowNotice("编辑未能保存，原记录已恢复：" + ex.Message);
		}
	}

	private void RelocateEntry(string id)
	{
		AppEntry appEntry = _services?.Library.Find(id);
		if (appEntry == null || _services == null || appEntry.Kind == AppItemKind.Url)
		{
			return;
		}
		string text = null;
		if (appEntry.Kind == AppItemKind.Folder)
		{
			OpenFolderDialog folder = new OpenFolderDialog();
			if (ShowModalDialog(() => folder.ShowDialog(this)))
			{
				text = folder.FolderName;
			}
		}
		else
		{
			OpenFileDialog picker = new OpenFileDialog
			{
				Filter = "所有文件|*.*"
			};
			if (ShowModalDialog(() => picker.ShowDialog(this)))
			{
				text = picker.FileName;
			}
		}
		if (text != null)
		{
			RelocationOutcome relocationOutcome = _services.Maintenance.Relocate(id, text);
			ShowNotice(relocationOutcome.Message, relocationOutcome.Result == RelocateResult.Relocated);
			RefreshLibrary();
		}
	}

	private string[]? AskFields(string title, params (string Label, string Value)[] fields)
	{
		return AskFields(title, useMenuBackground: false, fields);
	}

	private string[]? AskFields(string title, bool useMenuBackground, params (string Label, string Value)[] fields)
	{
		GlassPromptWindow dialog = new GlassPromptWindow
		{
			UseMenuBackground = useMenuBackground,
			Title = title,
			Owner = this,
			WindowStartupLocation = WindowStartupLocation.CenterOwner,
			Width = 430.0,
			SizeToContent = SizeToContent.Height,
			MinHeight = 150.0,
			ShowInTaskbar = false
		};
		StackPanel stackPanel = new StackPanel
		{
			Margin = new Thickness(18.0)
		};
		List<TextBox> inputs = new List<TextBox>();
		for (int i = 0; i < fields.Length; i++)
		{
			(string, string) tuple = fields[i];
			stackPanel.Children.Add(new TextBlock
			{
				Text = tuple.Item1,
				Margin = new Thickness(0.0, 6.0, 0.0, 3.0)
			});
			TextBox box = new TextBox
			{
				Text = tuple.Item2,
				MinHeight = 28.0
			};
			AutomationProperties.SetName(box, tuple.Item1);
			if (tuple.Item1.Contains("图标", StringComparison.Ordinal))
			{
				DockPanel dockPanel = new DockPanel();
				Button button = new Button
				{
					Content = "选择…",
					MinWidth = 72.0,
					Margin = new Thickness(6.0, 0.0, 0.0, 0.0)
				};
				button.Click += delegate
				{
					OpenFileDialog picker = new OpenFileDialog
					{
						Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.ico|所有文件|*.*"
					};
					if (ShowModalDialog(() => picker.ShowDialog(dialog)))
					{
						box.Text = picker.FileName;
					}
				};
				DockPanel.SetDock(button, Dock.Right);
				dockPanel.Children.Add(button);
				dockPanel.Children.Add(box);
				stackPanel.Children.Add(dockPanel);
			}
			else
			{
				stackPanel.Children.Add(box);
			}
			inputs.Add(box);
		}
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness(0.0, 14.0, 0.0, 0.0)
		};
		Button element = new Button
		{
			Content = "取消",
			MinWidth = 70.0,
			IsCancel = true,
			Margin = new Thickness(0.0, 0.0, 8.0, 0.0)
		};
		Button button2 = new Button
		{
			Content = "保存",
			MinWidth = 70.0,
			IsDefault = true
		};
		button2.Click += delegate
		{
			dialog.DialogResult = true;
		};
		stackPanel2.Children.Add(element);
		stackPanel2.Children.Add(button2);
		stackPanel.Children.Add(stackPanel2);
		SetPromptContent(dialog, stackPanel);
		dialog.Loaded += delegate
		{
			inputs[0].Focus();
		};
		if (!ShowModalDialog(dialog.ShowDialog))
		{
			return null;
		}
		return inputs.Select((TextBox textBox) => textBox.Text).ToArray();
	}

	private void SetPromptContent(GlassPromptWindow dialog, UIElement content)
	{
		if (dialog.UseMenuBackground)
		{
			PhoneBorder phoneBorder = new PhoneBorder
			{
				Background = (Brush)FindResource("LiquidControlPopup"),
				BorderBrush = (Brush)FindResource("LiquidControlEdge"),
				BorderThickness = new Thickness(1.0),
				Child = content
			};
			phoneBorder.SetBinding(Border.CornerRadiusProperty, new Binding("PanelCornerRadius")
			{
				Source = dialog
			});
			dialog.Content = phoneBorder;
		}
		else
		{
			Grid grid = new Grid();
			grid.Children.Add(new LiquidGlassSurface
			{
				UseLauncherTransparency = true,
				TransparentWhileLoading = true
			});
			grid.Children.Add(content);
			dialog.Content = grid;
		}
	}

	private void OnTileMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (_view.IsSearching || _logicalIconDrag || _oleIconDrag || !(sender is Button button))
		{
			return;
		}
		_pressedId = button.Tag as string;
		_pressedTile = button;
		_grabPoint = e.GetPosition(button);
		_pressTimer?.Stop();
		_pressTimer = new DispatcherTimer(DispatcherPriority.Input, base.Dispatcher)
		{
			Interval = TimeSpan.FromMilliseconds(350L)
		};
		_pressTimer.Tick += delegate
		{
			_pressTimer?.Stop();
			_pressTimer = null;
			Button pressedTile = _pressedTile;
			if (pressedTile != null && _pressedId != null && Mouse.LeftButton == MouseButtonState.Pressed)
			{
				StartIconDrag(pressedTile);
			}
		};
		_pressTimer.Start();
	}

	private void OnTileMouseMove(object sender, MouseEventArgs e)
	{
		if (!_view.IsSearching && _pressedId != null && _workspace != null)
		{
			_ = e.LeftButton;
			_ = 1;
		}
	}

	private void StartIconDrag(Button tile)
	{
		if (tile.Tag is string && _workspace != null && !_view.IsSearching)
		{
			PrepareIconDrag(tile, _grabPoint);
			_trackDragPointer = true;
			BeginCapturedIconDrag();
		}
	}

	private void RunIconOleTransfer(bool returnToCapture = true)
	{
		string dragIconId = _dragIconId;
		if (dragIconId == null || !_logicalIconDrag)
		{
			return;
		}
		long iconDragSession = _iconDragSession;
		_resumeCaptureFromOle = false;
		_oleIconDrag = true;
		ReleaseIconMouseCapture();
		QueryContinueDragEventHandler value = delegate(object _, QueryContinueDragEventArgs e)
		{
			if (_dragEdit == null)
			{
				e.Action = DragAction.Cancel;
				e.Handled = true;
			}
			else if (returnToCapture && !e.EscapePressed && (e.KeyStates & DragDropKeyStates.LeftMouseButton) != DragDropKeyStates.None && PointerIsOverLauncher())
			{
				_resumeCaptureFromOle = true;
				e.Action = DragAction.Cancel;
				e.Handled = true;
			}
		};
		GiveFeedbackEventHandler value2 = delegate(object _, GiveFeedbackEventArgs e)
		{
			UpdatePointerFromOleFeedback();
			e.UseDefaultCursors = false;
			Mouse.SetCursor(Cursors.Hand);
			e.Handled = true;
		};
		base.QueryContinueDrag += value;
		base.GiveFeedback += value2;
		try
		{
			DragDrop.DoDragDrop(this, new DataObject("GlassDesk.Launcher.Slot", dragIconId), DragDropEffects.Copy | DragDropEffects.Move);
		}
		finally
		{
			base.QueryContinueDrag -= value;
			base.GiveFeedback -= value2;
			_oleIconDrag = false;
			if (_logicalIconDrag && iconDragSession == _iconDragSession && (!_resumeCaptureFromOle || !base.IsVisible || !CaptureIconMouse()))
			{
				EndCapturedIconDrag();
			}
		}
	}

	private void OnTileMouseUp(object sender, MouseButtonEventArgs e)
	{
		_pressTimer?.Stop();
		_pressTimer = null;
		_pressedTile = null;
		_pressedId = null;
		if (_suppressTileClick)
		{
			e.Handled = true;
		}
	}

	private void CancelDrag()
	{
		_pressTimer?.Stop();
		_pressTimer = null;
		_pressedTile = null;
		if (_logicalIconDrag)
		{
			_dropCommitted = false;
			EndCapturedIconDrag();
			return;
		}
		if (_dragOrigin != null)
		{
			_dropCommitted = false;
			FinishIconDrag();
			return;
		}
		ClearDropTarget();
		_pressedId = null;
		_dragEdit = null;
		_edgePager.Reset();
		TrashDropTarget.Visibility = Visibility.Collapsed;
	}

	private void UpdateDragEdge()
	{
		if (_dragEdit == null)
		{
			CancelDrag();
			return;
		}
		Point point = TileGrid.TranslatePoint(new Point(_lastDragPoint.X - IncomingPageMotion.X, _lastDragPoint.Y), LauncherPanelRoot);
		double x = TileGrid.TranslatePoint(default(Point), LauncherPanelRoot).X;
		double y = PageViewport.TranslatePoint(default(Point), LauncherPanelRoot).Y;
		int num = ((point.X >= 0.0 && point.X <= LauncherPanelRoot.ActualWidth && point.Y >= y && point.Y < y + PageViewport.ActualHeight) ? ((point.X < x + 42.0) ? (-1) : ((point.X > x + TileGrid.ActualWidth - 42.0) ? 1 : 0)) : 0);
		if (_edgePager.Tick(num, DateTimeOffset.UtcNow) && num != 0)
		{
			NavigateDragPage(num);
		}
	}

	private void OnExternalDragOver(object sender, DragEventArgs e)
	{
		if (e.Data.GetDataPresent("GlassDesk.Launcher.Slot"))
		{
			e.Effects = ((!_view.IsSearching) ? DragDropEffects.Move : DragDropEffects.None);
			_lastDragPoint = e.GetPosition(TileGrid);
			_lastDragPoint.X += IncomingPageMotion.X;
			if (_dragOrigin != null)
			{
				QueueIconPreview(_lastDragPoint, (e.KeyStates & DragDropKeyStates.AltKey) != 0);
				e.Handled = true;
				return;
			}
			Button button = FindButton(e.OriginalSource as DependencyObject);
			string text = e.Data.GetData("GlassDesk.Launcher.Slot") as string;
			if (button != null && button.Tag is string text2 && text2 != text && !_view.IsSearching)
			{
				bool flag = e.GetPosition(button).X >= button.ActualWidth / 2.0;
				SetDropTarget(button, new Thickness((!flag) ? 3 : 0, 0.0, flag ? 3 : 0, 0.0));
			}
			else
			{
				ClearDropTarget();
			}
		}
		else
		{
			e.Effects = (e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None);
		}
		e.Handled = true;
	}

	private void OnLauncherDragLeave(object sender, DragEventArgs e)
	{
		Point position = e.GetPosition(this);
		if (position.X < 0.0 || position.Y < 0.0 || position.X >= base.ActualWidth || position.Y >= base.ActualHeight)
		{
			_edgePager.Reset();
		}
	}

	private void OnExternalDrop(object sender, DragEventArgs e)
	{
		if (e.Data.GetDataPresent(DataFormats.FileDrop))
		{
			if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
			{
				ImportPaths(paths);
			}
			e.Handled = true;
		}
		else
		{
			if (_dragEdit == null || _workspace == null || _view.IsSearching)
			{
				return;
			}
			object data = e.Data.GetData("GlassDesk.Launcher.Slot");
			string source = data as string;
			if (source == null)
			{
				return;
			}
			try
			{
				if (_dragOrigin != null)
				{
					Point position = e.GetPosition(TileGrid);
					position.X += IncomingPageMotion.X;
					bool exchange = (e.KeyStates & DragDropKeyStates.AltKey) != 0;
					if (!ApplyFolderDrop(position, exchange))
					{
						PlaceIconAtPointer(position, exchange, snap: true);
					}
					_dropCommitted = _workspace.Commit(_dragEdit);
					if (!_dropCommitted)
					{
						ShowNotice("拖拽期间布局或应用库已变化，请重试。");
					}
					e.Effects = (_dropCommitted ? DragDropEffects.Move : DragDropEffects.None);
					return;
				}
				Button button = FindButton(e.OriginalSource as DependencyObject);
				string target = button?.Tag as string;
				if (button == null || target == null || target == source)
				{
					return;
				}
				List<LauncherSlot> source2 = CurrentSlots();
				LauncherSlot launcherSlot = source2.FirstOrDefault((LauncherSlot slot) => slot.ItemId == target || slot.Group?.Id == target);
				if (launcherSlot == null || !source2.Any((LauncherSlot slot) => slot.Kind == LauncherSlotKind.App && slot.ItemId == source))
				{
					return;
				}
				int num = VisiblePageSlots().ToList().IndexOf(launcherSlot);
				if (num >= 0)
				{
					bool flag = e.GetPosition(button).X >= button.ActualWidth / 2.0;
					int index = num + (flag ? 1 : 0);
					_dragEdit.Draft = LauncherLayoutEditor.Move(_dragEdit.Draft, source, _view.Page, index, _view.Capacity);
					_dropCommitted = _workspace.Commit(_dragEdit);
					if (!_dropCommitted)
					{
						ShowNotice("拖拽期间布局或应用库已变化，请重试。");
					}
				}
			}
			catch (Exception ex) when (((ex is StorageException || ex is InvalidOperationException || ex is ArgumentException) ? 1 : 0) != 0)
			{
				ShowNotice("拖拽未保存，原布局已恢复：" + ex.Message);
			}
			finally
			{
				e.Handled = true;
			}
		}
	}

	private void OnTrashDragOver(object sender, DragEventArgs e)
	{
		bool flag = _dragEdit != null && !_view.IsSearching && _workspace != null && e.Data.GetData("GlassDesk.Launcher.Slot") is string id && ContainsLayoutItem(id);
		e.Effects = (flag ? DragDropEffects.Move : DragDropEffects.None);
		e.Handled = true;
	}

	private void OnTrashDrop(object sender, DragEventArgs e)
	{
		e.Handled = true;
		if (_dragEdit == null || _workspace == null || _view.IsSearching || !(e.Data.GetData("GlassDesk.Launcher.Slot") is string text) || !ContainsLayoutItem(text))
		{
			e.Effects = DragDropEffects.None;
			return;
		}
		e.Effects = DragDropEffects.Move;
		try
		{
			_dragEdit.Draft = LauncherLayoutEditor.RemoveShortcut(_dragEdit.Draft, text, _view.Capacity);
			_dropCommitted = _workspace.Commit(_dragEdit);
			if (!_dropCommitted)
			{
				ShowNotice("拖拽期间布局或应用库已变化，请重试。");
			}
			else
			{
				ShowNotice("已从启动台移除，需要时可重新添加。", autoDismiss: true);
			}
		}
		catch (Exception ex) when (((ex is StorageException || ex is InvalidOperationException || ex is ArgumentException) ? 1 : 0) != 0)
		{
			ShowNotice("未能从启动台移除，原布局已恢复：" + ex.Message);
		}
	}

	private bool ContainsLayoutItem(string id)
	{
		return _workspace?.Layout.Pages.SelectMany((LauncherPage page) => page.Slots).Any((LauncherSlot slot) => slot.ItemId == id || slot.Group?.Id == id || (slot.Group?.ItemIds.Contains<string>(id, StringComparer.Ordinal) ?? false)) ?? false;
	}

	private void SetDropTarget(Button target, Thickness indicatorThickness)
	{
		if (_dropTarget != target)
		{
			ClearDropTarget();
			_dropTarget = target;
			_dropTargetOriginalBorderBrush = target.BorderBrush;
			_dropTargetOriginalBorderThickness = target.BorderThickness;
		}
		target.BorderBrush = Brushes.DodgerBlue;
		target.BorderThickness = indicatorThickness;
	}

	private void ClearDropTarget()
	{
		if (_dropTarget != null)
		{
			_dropTarget.BorderBrush = _dropTargetOriginalBorderBrush ?? Brushes.Transparent;
			_dropTarget.BorderThickness = _dropTargetOriginalBorderThickness;
			_dropTarget = null;
			_dropTargetOriginalBorderBrush = null;
		}
	}

	private static Button? FindButton(DependencyObject? node)
	{
		while (node != null)
		{
			if (node is Button result)
			{
				return result;
			}
			node = VisualTreeHelper.GetParent(node);
		}
		return null;
	}

	private void RenderFilterGroups()
	{
		if (_workspace == null || _services == null)
		{
			return;
		}
		List<LauncherGroup> filterGroups = _workspace.Layout.FilterGroups;
		if (!_settings.GroupModeEnabled || !filterGroups.Any((LauncherGroup group) => group.Id == _activeFilterGroupId))
		{
			_activeFilterGroupId = null;
		}
		FilterGroupBar.Visibility = ((!_settings.GroupModeEnabled || filterGroups.Count <= 0) ? Visibility.Collapsed : Visibility.Visible);
		(long, bool) tuple = (_services.Layouts.LauncherVersion, _settings.GroupModeEnabled);
		(long, bool)? filterButtonsKey = _filterButtonsKey;
		(long, bool) tuple2 = tuple;
		bool hasValue = filterButtonsKey.HasValue;
		if (!hasValue)
		{
			goto IL_00e3;
		}
		if (hasValue)
		{
			(long, bool) valueOrDefault = filterButtonsKey.GetValueOrDefault();
			if (valueOrDefault.Item1 != tuple2.Item1 || valueOrDefault.Item2 != tuple2.Item2)
			{
				goto IL_00e3;
			}
		}
		goto IL_022d;
		IL_00e3:
		FilterGroupButtons.Children.Clear();
		foreach (LauncherGroup item in filterGroups)
		{
			string id = item.Id;
			ToggleButton toggleButton = new ToggleButton
			{
				Content = item.Name,
				Tag = id,
				ToolTip = item.Name,
				Style = (Style)FindResource("FilterGroupChip")
			};
			AutomationProperties.SetName(toggleButton, "分组：" + item.Name);
			toggleButton.Click += delegate
			{
				ToggleFilterGroup(id);
			};
			ContextMenu contextMenu = CreateTrackedContextMenu();
			AddMenu(contextMenu, "重命名", delegate
			{
				RenameFilterGroup(id);
			});
			AddMenu(contextMenu, "编辑图标…", delegate
			{
				EditFilterGroup(id);
			});
			AddMenu(contextMenu, "删除分组", delegate
			{
				CommitFilterEdit((LauncherLayout layout) => LauncherFilterRules.RemoveGroup(layout, id));
			});
			toggleButton.ContextMenu = contextMenu;
			FilterGroupButtons.Children.Add(toggleButton);
		}
		_filterButtonsKey = tuple;
		goto IL_022d;
		IL_022d:
		RefreshFilterSelection();
	}

	private void RefreshFilterSelection()
	{
		string objB = (_filterTransitionActive ? _filterTransitionTarget : _activeFilterGroupId);
		foreach (ToggleButton item in FilterGroupButtons.Children.OfType<ToggleButton>())
		{
			item.IsChecked = object.Equals(item.Tag, objB);
		}
	}

	private void ToggleFilterGroup(string id)
	{
		if (_settings.GroupModeEnabled)
		{
			LauncherWorkspace? workspace = _workspace;
			if (workspace != null && workspace.Layout.FilterGroups.Any((LauncherGroup group) => group.Id == id))
			{
				CancelDrag();
				ResetFolderPresentation();
				string text = (_filterTransitionActive ? _filterTransitionTarget : _activeFilterGroupId);
				SwitchFilterGroup((text == id) ? null : id);
			}
		}
	}

	private void CommitFilterEdit(Func<LauncherLayout, LauncherLayout> transform)
	{
		if (_workspace == null)
		{
			return;
		}
		CancelDrag();
		LauncherEditSession launcherEditSession = _workspace.BeginEdit();
		try
		{
			launcherEditSession.Draft = transform(launcherEditSession.Draft);
			if (!_workspace.Commit(launcherEditSession))
			{
				ShowNotice("应用库或布局已变化，请重试分组操作。");
			}
			else
			{
				_notice = null;
			}
		}
		catch (Exception ex) when (((ex is StorageException || ex is InvalidOperationException || ex is ArgumentException) ? 1 : 0) != 0)
		{
			ShowNotice("分组未保存，原有数据仍保留：" + ex.Message);
		}
		RenderTiles();
	}

	private void RenameFilterGroup(string id)
	{
		LauncherGroup launcherGroup = _workspace?.Layout.FilterGroups.FirstOrDefault((LauncherGroup group) => group.Id == id);
		if (launcherGroup == null)
		{
			return;
		}
		string[] values = AskFields("重命名分组", true, ("分组名称（1–60 个字符）", launcherGroup.Name));
		if (values != null)
		{
			CommitFilterEdit((LauncherLayout layout) => LauncherFilterRules.RenameGroup(layout, id, values[0]));
		}
	}

	private void AddFilterGroup()
	{
		if (_workspace == null || _services == null)
		{
			return;
		}
		string[] array = AskFields("添加分组", true, ("分组名称（1–60 个字符）", ""));
		if (array == null)
		{
			return;
		}
		string name = array[0].Trim();
		int length = name.Length;
		if ((length < 1 || length > 60) ? true : false)
		{
			ShowNotice("分组名称应为 1–60 个字符。");
			return;
		}
		List<string> list = LauncherLayoutRules.AppIds(_workspace.Layout).Distinct<string>(StringComparer.Ordinal).ToList();
		if (list.Count == 0)
		{
			ShowNotice("请先向启动台添加图标，再创建分组。");
			return;
		}
		List<string> selection = SelectFilterMembers(name, list);
		if (selection != null)
		{
			CommitFilterEdit((LauncherLayout layout) => LauncherFilterRules.AddGroup(layout, "filter-" + Guid.NewGuid().ToString("N"), name, selection));
		}
	}

	private void EditFilterGroup(string id)
	{
		LauncherGroup launcherGroup = _workspace?.Layout.FilterGroups.FirstOrDefault((LauncherGroup group) => group.Id == id);
		if (launcherGroup == null || _services == null)
		{
			return;
		}
		(long, long) tuple = (_services.Layouts.LauncherVersion, _services.Library.Version);
		List<string> ids = LauncherLayoutRules.AppIds(_workspace.Layout).Distinct<string>(StringComparer.Ordinal).ToList();
		List<string> selection = SelectFilterMembers(launcherGroup.Name, ids, launcherGroup.ItemIds);
		if (selection == null)
		{
			return;
		}
		(long, long) tuple2 = tuple;
		long launcherVersion = _services.Layouts.LauncherVersion;
		long version = _services.Library.Version;
		if (tuple2.Item1 != launcherVersion || tuple2.Item2 != version)
		{
			ShowNotice("编辑期间应用库或分组已变化，请重新打开后再试。");
			return;
		}
		CommitFilterEdit((LauncherLayout layout) => LauncherFilterRules.UpdateGroupMembers(layout, id, selection));
	}

	private List<string>? SelectFilterMembers(string name, IReadOnlyList<string> ids, IReadOnlyCollection<string>? currentMembers = null)
	{
		bool editing = currentMembers != null;
		GlassPromptWindow dialog = new GlassPromptWindow
		{
			UseMenuBackground = true,
			Owner = this,
			Title = (editing ? "编辑分组图标" : "选择分组图标"),
			Width = 680.0,
			Height = 550.0,
			WindowStartupLocation = WindowStartupLocation.CenterOwner,
			ShowInTaskbar = false
		};
		Grid grid = new Grid
		{
			Margin = new Thickness(24.0)
		};
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star)
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		TextBlock element = new TextBlock
		{
			Text = name,
			FontSize = 20.0,
			FontWeight = FontWeights.SemiBold,
			TextTrimming = TextTrimming.CharacterEllipsis
		};
		grid.Children.Add(element);
		TextBox search = new TextBox
		{
			MinHeight = 32.0,
			Padding = new Thickness(8.0, 4.0, 8.0, 4.0),
			Background = Brushes.Transparent,
			BorderThickness = new Thickness(0.0),
			ToolTip = "筛选已添加到启动台的图标"
		};
		search.SetBinding(Control.ForegroundProperty, new Binding("ForegroundBrush")
		{
			Source = dialog
		});
		search.SetBinding(TextBoxBase.CaretBrushProperty, new Binding("ForegroundBrush")
		{
			Source = dialog
		});
		PhoneBorder phoneBorder = new PhoneBorder
		{
			Child = search,
			CornerRadius = new CornerRadius(16.0),
			Background = new SolidColorBrush(Color.FromArgb(16, byte.MaxValue, byte.MaxValue, byte.MaxValue)),
			BorderThickness = new Thickness(1.0),
			Margin = new Thickness(0.0, 14.0, 0.0, 12.0)
		};
		phoneBorder.SetBinding(Border.BorderBrushProperty, new Binding("PanelBorderBrush")
		{
			Source = dialog
		});
		AutomationProperties.SetName(search, "筛选可选图标");
		Grid.SetRow(phoneBorder, 1);
		grid.Children.Add(phoneBorder);
		WrapPanel wrapPanel = new WrapPanel();
		ScrollViewer element2 = new ScrollViewer
		{
			Content = wrapPanel,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
		};
		Grid.SetRow(element2, 2);
		grid.Children.Add(element2);
		HashSet<string> selected = ((IEnumerable<string>)(((object)currentMembers) ?? ((object)Array.Empty<string>()))).Intersect<string>(ids, StringComparer.Ordinal).ToHashSet<string>(StringComparer.Ordinal);
		DockPanel dockPanel = new DockPanel
		{
			Margin = new Thickness(0.0, 14.0, 0.0, 0.0)
		};
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal
		};
		Button button = new Button
		{
			Content = "取消",
			MinWidth = 76.0,
			IsCancel = true,
			Margin = new Thickness(0.0, 0.0, 8.0, 0.0)
		};
		Button create = new Button
		{
			Content = (editing ? "保存更改" : "创建分组"),
			MinWidth = 92.0,
			IsDefault = true,
			IsEnabled = editing
		};
		Style style = new Style(typeof(Button), (Style)FindResource("FolderActionButton"));
		Trigger trigger = new Trigger
		{
			Property = UIElement.IsEnabledProperty,
			Value = false
		};
		trigger.Setters.Add(new Setter(UIElement.OpacityProperty, 0.45));
		style.Triggers.Add(trigger);
		Style style2 = (create.Style = style);
		button.Style = style2;
		create.Background = new SolidColorBrush(Color.FromArgb(70, 10, 132, byte.MaxValue));
		stackPanel.Children.Add(button);
		stackPanel.Children.Add(create);
		DockPanel.SetDock(stackPanel, Dock.Right);
		dockPanel.Children.Add(stackPanel);
		TextBlock count = new TextBlock
		{
			Text = "请选择图标 · 已选 0 项",
			VerticalAlignment = VerticalAlignment.Center,
			Opacity = 0.7
		};
		dockPanel.Children.Add(count);
		Grid.SetRow(dockPanel, 3);
		grid.Children.Add(dockPanel);
		List<(CheckBox Box, string Search)> choices = new List<(CheckBox, string)>();
		foreach (string id in ids)
		{
			AppEntry appEntry = _services?.Library.Find(id);
			if (appEntry != null)
			{
				Border border = new Border
				{
					Width = 36.0,
					Height = 36.0,
					Margin = new Thickness(0.0, 0.0, 9.0, 0.0),
					Child = new TextBlock
					{
						Text = ((appEntry.DisplayName.Length > 0) ? appEntry.DisplayName.Substring(0, 1) : "A"),
						FontSize = 24.0,
						HorizontalAlignment = HorizontalAlignment.Center
					}
				};
				RequestIcon(appEntry, border, 36.0);
				TextBlock element3 = new TextBlock
				{
					Text = appEntry.DisplayName,
					Width = 106.0,
					TextTrimming = TextTrimming.CharacterEllipsis,
					VerticalAlignment = VerticalAlignment.Center
				};
				StackPanel stackPanel2 = new StackPanel
				{
					Orientation = Orientation.Horizontal
				};
				stackPanel2.Children.Add(border);
				stackPanel2.Children.Add(element3);
				CheckBox checkBox = new CheckBox
				{
					Content = stackPanel2,
					Tag = id,
					Width = 192.0,
					MinHeight = 58.0,
					Margin = new Thickness(0.0, 0.0, 8.0, 6.0),
					ToolTip = appEntry.DisplayName,
					VerticalContentAlignment = VerticalAlignment.Center
				};
				checkBox.IsChecked = selected.Contains(id);
				checkBox.SetBinding(Control.ForegroundProperty, new Binding("ForegroundBrush")
				{
					Source = dialog
				});
				AutomationProperties.SetName(checkBox, appEntry.DisplayName);
				checkBox.Checked += delegate
				{
					selected.Add(id);
					UpdateCount();
				};
				checkBox.Unchecked += delegate
				{
					selected.Remove(id);
					UpdateCount();
				};
				wrapPanel.Children.Add(checkBox);
				choices.Add((checkBox, appEntry.DisplayName + " " + string.Join(" ", appEntry.SearchAliases)));
			}
		}
		UpdateCount();
		search.TextChanged += delegate
		{
			string text = search.Text.Trim();
			foreach (var item in choices)
			{
				item.Box.Visibility = ((text.Length != 0 && !item.Search.Contains(text, StringComparison.OrdinalIgnoreCase)) ? Visibility.Collapsed : Visibility.Visible);
			}
		};
		create.Click += delegate
		{
			if (editing || selected.Count > 0)
			{
				dialog.DialogResult = true;
			}
		};
		SetPromptContent(dialog, grid);
		dialog.Loaded += delegate
		{
			search.Focus();
		};
		if (!ShowModalDialog(dialog.ShowDialog))
		{
			return null;
		}
		return ids.Where(selected.Contains).ToList();
		void UpdateCount()
		{
			count.Text = $"已选 {selected.Count} / {choices.Count} 项";
			create.IsEnabled = editing || selected.Count > 0;
		}
	}

	private void SwitchFilterGroup(string? target)
	{
		if (base.IsVisible)
		{
			ThemeService? current = ThemeService.Current;
			if (current == null || current.AnimationsEnabled)
			{
				if (_activeFilterGroupId != null)
				{
					BeginFilterTransition(closing: true, target);
					return;
				}
				StopFilterTransition(completeTarget: false);
				_activeFilterGroupId = target;
				_view.MoveToPage(0);
				RenderTiles();
				if (target != null)
				{
					BeginFilterTransition(closing: false, target);
				}
				return;
			}
		}
		StopFilterTransition(completeTarget: false);
		_activeFilterGroupId = target;
		_view.MoveToPage(0);
		RenderTiles();
	}

	private void BeginFilterTransition(bool closing, string? target)
	{
		Dictionary<FrameworkElement, (double, double)> dictionary = _filterTileMotions.ToDictionary((FilterTileMotion m) => m.Tile, (FilterTileMotion m) => (Opacity: m.Tile.Opacity, Y: m.Offset.Y));
		StopFilterTransition(completeTarget: false);
		FrameworkElement[] array = TileGrid.Children.OfType<FrameworkElement>().ToArray();
		if (array.Length == 0)
		{
			if (closing)
			{
				CompleteFilterSwitch(target);
			}
			return;
		}
		_filterTransitionActive = true;
		_filterTransitionClosing = closing;
		_filterTransitionTarget = target;
		_filterTransitionStarted = Stopwatch.GetTimestamp();
		for (int num = 0; num < array.Length; num++)
		{
			FrameworkElement frameworkElement = array[num];
			Transform renderTransform = frameworkElement.RenderTransform;
			double opacity = frameworkElement.Opacity;
			(double, double) valueOrDefault = dictionary.GetValueOrDefault(frameworkElement, (closing ? opacity : 0.0, closing ? 0.0 : (-18.0)));
			TranslateTransform translateTransform = new TranslateTransform(0.0, valueOrDefault.Item2);
			TransformGroup transformGroup = new TransformGroup();
			transformGroup.Children.Add(renderTransform);
			transformGroup.Children.Add(translateTransform);
			_filterTileMotions.Add(new FilterTileMotion(frameworkElement, renderTransform, opacity, translateTransform, valueOrDefault.Item1, valueOrDefault.Item2, num / _view.Columns));
			frameworkElement.RenderTransform = transformGroup;
			frameworkElement.Opacity = valueOrDefault.Item1;
		}
		PageScroll.IsHitTestVisible = false;
		RefreshFilterSelection();
		CompositionTarget.Rendering += OnFilterTransitionRendering;
		this.NavigationAvailabilityChanged?.Invoke(this, EventArgs.Empty);
	}

	private void OnFilterTransitionRendering(object? sender, EventArgs e)
	{
		UpdateFilterTransition(Stopwatch.GetElapsedTime(_filterTransitionStarted).TotalMilliseconds);
	}

	private void UpdateFilterTransition(double elapsedMilliseconds)
	{
		if (!_filterTransitionActive)
		{
			return;
		}
		int num = (_filterTileMotions.Count + _view.Columns - 1) / _view.Columns;
		foreach (FilterTileMotion filterTileMotion in _filterTileMotions)
		{
			int num2 = (_filterTransitionClosing ? (num - 1 - filterTileMotion.Row) : filterTileMotion.Row);
			double num3 = Math.Clamp((elapsedMilliseconds - (double)num2 * 35.0) / 190.0, 0.0, 1.0);
			double num4 = 1.0 - Math.Pow(1.0 - num3, 3.0);
			double num5 = (_filterTransitionClosing ? 0.0 : filterTileMotion.OriginalOpacity);
			int num6 = (_filterTransitionClosing ? (-18) : 0);
			filterTileMotion.Tile.Opacity = filterTileMotion.StartOpacity + (num5 - filterTileMotion.StartOpacity) * num4;
			filterTileMotion.Offset.Y = filterTileMotion.StartOffset + ((double)num6 - filterTileMotion.StartOffset) * num4;
		}
		if (!(elapsedMilliseconds < 190.0 + (double)(num - 1) * 35.0))
		{
			if (_filterTransitionClosing)
			{
				CompleteFilterSwitch(_filterTransitionTarget);
			}
			else
			{
				StopFilterTransition(completeTarget: false);
			}
		}
	}

	private void CompleteFilterSwitch(string? target)
	{
		StopFilterTransition(completeTarget: false);
		_activeFilterGroupId = target;
		_view.MoveToPage(0);
		RenderTiles();
		if (target != null && base.IsVisible)
		{
			ThemeService? current = ThemeService.Current;
			if (current == null || current.AnimationsEnabled)
			{
				BeginFilterTransition(closing: false, target);
			}
		}
	}

	private void StopFilterTransition(bool completeTarget = true)
	{
		if (!_filterTransitionActive)
		{
			return;
		}
		if (completeTarget)
		{
			_activeFilterGroupId = _filterTransitionTarget;
			if (_filterTransitionClosing)
			{
				_view.MoveToPage(0);
			}
		}
		CompositionTarget.Rendering -= OnFilterTransitionRendering;
		foreach (FilterTileMotion filterTileMotion in _filterTileMotions)
		{
			filterTileMotion.Tile.Opacity = filterTileMotion.OriginalOpacity;
			filterTileMotion.Tile.RenderTransform = filterTileMotion.OriginalTransform;
		}
		_filterTileMotions.Clear();
		_filterTransitionActive = (_filterTransitionClosing = false);
		_filterTransitionTarget = null;
		PageScroll.IsHitTestVisible = true;
		RefreshFilterSelection();
		this.NavigationAvailabilityChanged?.Invoke(this, EventArgs.Empty);
	}

	private bool UpdateFolderHover(Point gridPoint, bool exchange)
	{
		if (_settings.GroupModeEnabled || exchange || _dragOrigin == null || _dragIconId == null || _dragOrigin.Pages.SelectMany((LauncherPage p) => p.Slots).Any((LauncherSlot s) => s.Group?.Id == _dragIconId))
		{
			ClearFolderHover();
			return false;
		}
		Point point = TileGrid.TranslatePoint(new Point(gridPoint.X - IncomingPageMotion.X, gridPoint.Y), DragOverlay);
		Point point2 = new Point(point.X - _grabPoint.X + _ghostIconCenterOffset.X, point.Y - _grabPoint.Y + _ghostIconCenterOffset.Y);
		LauncherAppTileButton launcherAppTileButton = null;
		Rect folderHoverBounds = default(Rect);
		double num = double.MaxValue;
		if (_folderHoverTile != null && _folderHoverTile.IsVisible && (point2 - new Point(_folderHoverBounds.X + _folderHoverBounds.Width / 2.0, _folderHoverBounds.Y + _folderHoverBounds.Height / 2.0)).Length <= AppIconSize * 0.94)
		{
			launcherAppTileButton = _folderHoverTile;
			folderHoverBounds = _folderHoverBounds;
		}
		else
		{
			foreach (LauncherAppTileButton item in TileGrid.Children.OfType<LauncherAppTileButton>())
			{
				if (!object.Equals(item.Tag, _dragIconId) && !(item.Opacity < 0.1))
				{
					Rect rect = IconBounds(item, DragOverlay);
					double length = (point2 - new Point(rect.X + rect.Width / 2.0, rect.Y + rect.Height / 2.0)).Length;
					if (length < num && length <= AppIconSize * 0.94)
					{
						launcherAppTileButton = item;
						folderHoverBounds = rect;
						num = length;
					}
				}
			}
		}
		object obj = launcherAppTileButton?.Tag;
		string targetId = obj as string;
		if (targetId == null)
		{
			ClearFolderHover();
			return false;
		}
		if (_folderHoverTile != launcherAppTileButton)
		{
			_folderIntent.Reset();
			if (_layoutBeforePlacement != null && _dragEdit != null)
			{
				LauncherLayout layoutBeforePlacement = _layoutBeforePlacement;
				LauncherLayout draft = _dragEdit.Draft;
				int num2 = Math.Clamp((int)(gridPoint.X / (TileGrid.ActualWidth / (double)_view.Columns)), 0, _view.Columns - 1);
				int num3 = Math.Clamp((int)(gridPoint.Y / (TileGrid.ActualHeight / (double)_view.Rows)), 0, _view.Rows - 1);
				int num4 = _view.Page * _view.Capacity + num3 * _view.Columns + num2;
				if (Index(layoutBeforePlacement) == _previewIndex && num4 == _previewIndex && Index(layoutBeforePlacement) != Index(draft) && _iconOffsetMotions.TryGetValue(launcherAppTileButton, out IconOffsetMotion value) && value.Active)
				{
					int num5 = TileGrid.Children.IndexOf(launcherAppTileButton);
					int num6 = Index(layoutBeforePlacement) - _view.Page * _view.Capacity;
					folderHoverBounds.Offset((double)(num6 % _view.Columns - num5 % _view.Columns) * TileGrid.ActualWidth / (double)_view.Columns - value.transform.X, (double)(num6 / _view.Columns - num5 / _view.Columns) * TileGrid.ActualHeight / (double)_view.Rows - value.transform.Y);
				}
			}
			_folderHoverTile = launcherAppTileButton;
			_folderHoverBounds = folderHoverBounds;
		}
		Point point3 = new Point(folderHoverBounds.X + folderHoverBounds.Width / 2.0, folderHoverBounds.Y + folderHoverBounds.Height / 2.0);
		num = (point2 - point3).Length;
		bool overlapping = num <= AppIconSize * 0.58;
		_folderIntent.Update(targetId, overlapping, Stopwatch.GetElapsedTime(0L, Stopwatch.GetTimestamp()));
		_fusionA = point3;
		_fusionSpring.Target = (_folderIntent.IsReady ? 1 : 0);
		ThemeService? current2 = ThemeService.Current;
		if (current2 != null && !current2.AnimationsEnabled)
		{
			_fusionSpring.Snap(_fusionSpring.Target);
		}
		EnsureFolderFrames();
		return true;
		int Index(LauncherLayout layout)
		{
			for (int i = 0; i < layout.Pages.Count; i++)
			{
				int num7 = layout.Pages[i].Slots.FindIndex((LauncherSlot s) => s.ItemId == targetId || s.Group?.Id == targetId);
				if (num7 >= 0)
				{
					return i * _view.Capacity + num7;
				}
			}
			return -1;
		}
	}

	private bool ApplyFolderDrop(Point point, bool exchange)
	{
		if (_dragSourceGroupId != null && !_memberHasLeft)
		{
			return false;
		}
		if (!UpdateFolderHover(point, exchange) || !_folderIntent.IsReady || _dragEdit == null || _dragIconId == null)
		{
			return false;
		}
		string targetId = _folderIntent.TargetId;
		LauncherSlot launcherSlot = _dragEdit.Draft.Pages.SelectMany((LauncherPage p) => p.Slots).FirstOrDefault((LauncherSlot s) => s.ItemId == targetId || s.Group?.Id == targetId);
		if (launcherSlot == null)
		{
			return false;
		}
		if (launcherSlot.Kind == LauncherSlotKind.Group)
		{
			_dragEdit.Draft = LauncherLayoutEditor.AddToGroup(_dragEdit.Draft, _dragIconId, targetId, _view.Capacity);
			_dropLandingId = targetId;
		}
		else
		{
			string text = "folder-" + Guid.NewGuid().ToString("N");
			_dragEdit.Draft = LauncherLayoutEditor.CreateGroup(_dragEdit.Draft, _dragIconId, targetId, text, _view.Capacity);
			_dropLandingId = text;
		}
		return true;
	}

	private void ClearFolderHover(bool immediate = false)
	{
		_folderIntent.Reset();
		_folderHoverTile = null;
		_fusionSpring.Target = 0.0;
		if (immediate)
		{
			_fusionSpring.Snap(0.0);
			if (_fusionLens != null)
			{
				DragOverlay.Children.Remove(_fusionLens);
			}
			_fusionLens = null;
		}
		else if (_fusionLens != null || _fusionSpring.IsMoving)
		{
			EnsureFolderFrames();
		}
	}

	private void ApplyFusionFrame()
	{
		double num = Math.Clamp(_fusionSpring.Value, 0.0, 1.0);
		if (num < 0.002 && _fusionSpring.Target == 0.0)
		{
			if (_fusionLens != null)
			{
				DragOverlay.Children.Remove(_fusionLens);
			}
			_fusionLens = null;
			return;
		}
		if (_fusionLens == null || !DragOverlay.Children.Contains(_fusionLens))
		{
			_fusionLens = new SharedGlassLens
			{
				Source = GlassSurface
			};
			DragOverlay.Children.Insert(0, _fusionLens);
		}
		double num2 = AppIconSize * (1.04 + Math.Clamp(_fusionSpring.Value, 0.0, 1.1) * 0.1);
		Canvas.SetLeft(_fusionLens, _fusionA.X - num2 / 2.0);
		Canvas.SetTop(_fusionLens, _fusionA.Y - num2 / 2.0);
		_fusionLens.Width = num2;
		_fusionLens.Height = num2;
		_fusionLens.Radius = num2 * 0.38;
		_fusionLens.Highlight = num;
		_fusionLens.Opacity = Math.Clamp(num * 1.5, 0.0, 1.0);
		_fusionLens.InvalidateVisual();
	}

	private bool PreviewFolderMemberInside(Point point)
	{
		if (_dragSourceGroupId == null || _memberOrigin == null || _dragEdit == null || _dragIconId == null || _memberHasLeft)
		{
			return false;
		}
		Point point2 = TileGrid.TranslatePoint(new Point(point.X - IncomingPageMotion.X, point.Y), FolderCard);
		if (GroupPanel.Visibility != Visibility.Visible || _folderClosing || !new Rect(FolderCard.RenderSize).Contains(point2))
		{
			if (!_memberHasLeft)
			{
				_memberHasLeft = true;
				_previewIndex = -1;
				_dragEdit.Draft = LauncherLayoutRules.DeepClone(_dragOrigin);
			}
			if (!_folderClosing && GroupPanel.Visibility == Visibility.Visible)
			{
				CloseFolder();
			}
			ClearFolderHover();
			return false;
		}
		ClearFolderHover();
		Point point3 = TileGrid.TranslatePoint(new Point(point.X - IncomingPageMotion.X, point.Y), GroupItems);
		int count = _memberOrigin.Pages.SelectMany((LauncherPage pg) => pg.Slots).First((LauncherSlot s) => s.Group?.Id == _dragSourceGroupId).Group.ItemIds.Count;
		double num = Math.Max(1.0, GroupItems.ActualWidth / (double)GroupItems.Columns);
		double num2 = Math.Ceiling((double)count / (double)GroupItems.Columns);
		double num3 = Math.Max(1.0, GroupItems.ActualHeight / num2);
		int num4 = Math.Clamp((int)(point3.Y / num3) * GroupItems.Columns + Math.Clamp((int)(point3.X / num), 0, GroupItems.Columns - 1), 0, count - 1);
		if (_memberPreviewIndex != num4)
		{
			_memberPreviewIndex = num4;
			_dragEdit.Draft = LauncherLayoutEditor.MoveGroupMember(_memberOrigin, _dragSourceGroupId, _dragIconId, num4);
			List<string> itemIds = _dragEdit.Draft.Pages.SelectMany((LauncherPage pg) => pg.Slots).First((LauncherSlot s) => s.Group?.Id == _dragSourceGroupId).Group.ItemIds;
			Dictionary<string, Button> dictionary = GroupItems.Children.OfType<Button>().ToDictionary((Button t) => (string)t.Tag);
			Dictionary<Button, Point> dictionary2 = dictionary.Values.ToDictionary((Button t) => t, (Button t) => t.TranslatePoint(default(Point), GroupItems));
			GroupItems.Children.Clear();
			foreach (string item in itemIds)
			{
				GroupItems.Children.Add(dictionary[item]);
			}
			GroupItems.UpdateLayout();
			foreach (Button value in dictionary.Values)
			{
				Point point4 = value.TranslatePoint(default(Point), GroupItems);
				value.RenderTransform = new TranslateTransform();
				AnimateIconOffset((TranslateTransform)value.RenderTransform, dictionary2[value].X - point4.X, dictionary2[value].Y - point4.Y);
				value.Opacity = ((!object.Equals(value.Tag, _dragIconId)) ? 1 : 0);
			}
		}
		Point point5 = TileGrid.TranslatePoint(new Point(point.X - IncomingPageMotion.X, point.Y), GroupScroll);
		if (point5.Y < 30.0)
		{
			GroupScroll.ScrollToVerticalOffset(GroupScroll.VerticalOffset - 5.0);
		}
		else if (point5.Y > GroupScroll.ActualHeight - 30.0)
		{
			GroupScroll.ScrollToVerticalOffset(GroupScroll.VerticalOffset + 5.0);
		}
		return true;
	}

	private Grid CreateFolderIcon(LauncherGroup group)
	{
		double appIconSize = AppIconSize;
		Grid grid = new Grid
		{
			Width = appIconSize,
			Height = appIconSize
		};
		grid.Children.Add(new SharedGlassLens
		{
			Source = GlassSurface,
			Radius = appIconSize * 0.38
		});
		int num = Math.Min(9, group.ItemIds.Count);
		int num2 = ((num <= 4) ? 2 : 3);
		int val = (int)Math.Ceiling((double)num / (double)num2);
		UniformGrid uniformGrid = new UniformGrid
		{
			Columns = num2,
			Rows = Math.Max(1, val),
			Margin = new Thickness(appIconSize * 0.13),
			VerticalAlignment = VerticalAlignment.Center
		};
		double num3 = (appIconSize * 0.74 - (double)((num2 - 1) * 2)) / (double)num2;
		foreach (string item in group.ItemIds.Take(9))
		{
			AppEntry appEntry = _services?.Library.Find(item);
			Border border = new Border
			{
				Width = num3,
				Height = num3,
				Margin = new Thickness(1.0),
				CornerRadius = new CornerRadius(num3 * 0.24),
				Background = new SolidColorBrush(Color.FromArgb(26, byte.MaxValue, byte.MaxValue, byte.MaxValue))
			};
			if (appEntry != null)
			{
				RequestIcon(appEntry, border, num3);
			}
			uniformGrid.Children.Add(border);
		}
		grid.Children.Add(uniformGrid);
		return grid;
	}

	private static void AnimateFolderHighlight(SharedGlassLens lens, double value)
	{
		ThemeService? current = ThemeService.Current;
		if (current != null && !current.AnimationsEnabled)
		{
			lens.Highlight = value;
		}
		else
		{
			lens.BeginAnimation(SharedGlassLens.HighlightProperty, new DoubleAnimation(value, TimeSpan.FromMilliseconds(180L)));
		}
	}

	private void UpdateFolderPanel()
	{
		LauncherGroup group = _workspace?.Layout.Pages.SelectMany((LauncherPage p) => p.Slots).FirstOrDefault((LauncherSlot s) => s.Group?.Id == _view.OpenGroupId)?.Group;
		if (group == null)
		{
			if (_displayedFolderId != null && GroupPanel.Visibility == Visibility.Visible)
			{
				CloseFolder();
			}
			else
			{
				GroupPanel.Visibility = Visibility.Collapsed;
			}
			return;
		}
		bool flag = _displayedFolderId != group.Id || GroupPanel.Visibility != Visibility.Visible;
		_displayedFolderId = group.Id;
		_folderClosing = false;
		GroupPanel.Visibility = Visibility.Visible;
		if (flag)
		{
			this.NavigationAvailabilityChanged?.Invoke(this, EventArgs.Empty);
		}
		FolderGlass.Source = GlassSurface;
		GroupTitle.Text = group.Name;
		string text = group.Id + "\0" + string.Join("\0", group.ItemIds) + "\0" + _services?.Library.Version;
		if (_folderItemsKey != text)
		{
			_folderItemsKey = text;
			GroupItems.Children.Clear();
			GroupItems.Columns = Math.Clamp(_view.Columns, 2, 4);
			foreach (string itemId in group.ItemIds)
			{
				GroupItems.Children.Add(CreateTile(new LauncherSlot
				{
					ItemId = itemId
				}));
			}
		}
		GroupPanel.UpdateLayout();
		double num = Math.Max(180.0, Math.Min(600.0, FolderCanvas.ActualWidth - 24.0));
		double num2 = 104.0 + Math.Ceiling((double)group.ItemIds.Count / (double)GroupItems.Columns) * (AppIconSize + 48.0);
		double num3 = Math.Max(150.0, Math.Min(Math.Min(460.0, num2), FolderCanvas.ActualHeight - 24.0));
		FolderHint.Text = $"{group.ItemIds.Count} 个应用 · " + ((num2 > num3 + 1.0) ? "滚轮查看更多 · " : "") + "长按可整理或拖出";
		_folderDestination = new Rect(Math.Max(0.0, (FolderCanvas.ActualWidth - num) / 2.0), Math.Max(0.0, (FolderCanvas.ActualHeight - num3) / 2.0), num, num3);
		GroupContent.Width = Math.Max(100.0, num - 36.0);
		GroupContent.Height = Math.Max(100.0, num3 - 36.0);
		GroupContent.HorizontalAlignment = HorizontalAlignment.Left;
		GroupContent.VerticalAlignment = VerticalAlignment.Top;
		if (flag)
		{
			LauncherAppTileButton launcherAppTileButton = TileGrid.Children.OfType<LauncherAppTileButton>().FirstOrDefault((LauncherAppTileButton t) => object.Equals(t.Tag, group.Id));
			_folderOrigin = ((launcherAppTileButton != null) ? IconBounds(launcherAppTileButton, FolderCanvas) : new Rect(_folderDestination.X + num / 2.0 - AppIconSize / 2.0, _folderDestination.Y + num3 / 2.0 - AppIconSize / 2.0, AppIconSize, AppIconSize));
			_folderOpen.Snap(0.0);
			GroupScroll.ScrollToTop();
		}
		_folderOpen.Target = 1.0;
		ThemeService? current2 = ThemeService.Current;
		if (current2 != null && !current2.AnimationsEnabled)
		{
			_folderOpen.Snap(1.0);
		}
		ApplyFolderFrame();
		EnsureFolderFrames();
	}

	private static Rect IconBounds(LauncherAppTileButton tile, Visual relativeTo)
	{
		FrameworkElement iconHost = tile.IconHost;
		return iconHost.TransformToVisual(relativeTo).TransformBounds(new Rect(new Size(Math.Max(1.0, iconHost.ActualWidth), Math.Max(1.0, iconHost.ActualHeight))));
	}

	private void CloseFolder()
	{
		if (_displayedFolderId == null)
		{
			GroupPanel.Visibility = Visibility.Collapsed;
			_view.OpenGroupId = null;
			this.NavigationAvailabilityChanged?.Invoke(this, EventArgs.Empty);
			return;
		}
		LauncherAppTileButton launcherAppTileButton = TileGrid.Children.OfType<LauncherAppTileButton>().FirstOrDefault((LauncherAppTileButton t) => object.Equals(t.Tag, _displayedFolderId));
		if (launcherAppTileButton != null)
		{
			_folderOrigin = IconBounds(launcherAppTileButton, FolderCanvas);
		}
		_folderClosing = true;
		_folderOpen.Target = 0.0;
		ThemeService? current = ThemeService.Current;
		if (current != null && !current.AnimationsEnabled)
		{
			_folderOpen.Snap(0.0);
		}
		EnsureFolderFrames();
	}

	private void ResetFolderPresentation()
	{
		_view.OpenGroupId = null;
		_displayedFolderId = null;
		_folderItemsKey = null;
		_folderClosing = false;
		_folderOpen.Snap(0.0);
		GroupPanel.Visibility = Visibility.Collapsed;
		GroupItems.Children.Clear();
		TileGrid.Opacity = 1.0;
		foreach (Button item in TileGrid.Children.OfType<Button>())
		{
			item.Opacity = 1.0;
		}
		StopFolderFrames();
		ClearFolderHover(immediate: true);
		this.NavigationAvailabilityChanged?.Invoke(this, EventArgs.Empty);
	}

	private void OnFolderOutsideDown(object sender, MouseButtonEventArgs e)
	{
		if (!_logicalIconDrag)
		{
			CloseFolder();
		}
		e.Handled = true;
	}

	private void OnRenameOpenFolder(object sender, RoutedEventArgs e)
	{
		string openGroupId = _view.OpenGroupId;
		if (openGroupId != null)
		{
			RenameGroup(openGroupId);
		}
	}

	protected override void OnPreviewKeyDown(KeyEventArgs e)
	{
		if (!_logicalIconDrag && e.Key == Key.Escape && _displayedFolderId != null)
		{
			CloseFolder();
			e.Handled = true;
		}
		base.OnPreviewKeyDown(e);
	}

	protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
	{
		if (!_logicalIconDrag && !_folderClosing && _displayedFolderId != null && e.ChangedButton == MouseButton.Left && !new Rect(FolderCard.RenderSize).Contains(e.GetPosition(FolderCard)))
		{
			CloseFolder();
		}
		base.OnPreviewMouseDown(e);
	}

	private void EnsureFolderFrames()
	{
		if (!_folderFramesAttached)
		{
			_folderFramesAttached = true;
			_folderFrameTicks = Stopwatch.GetTimestamp();
			CompositionTarget.Rendering += OnFolderFrame;
		}
	}

	private void StopFolderFrames()
	{
		if (_folderFramesAttached)
		{
			CompositionTarget.Rendering -= OnFolderFrame;
		}
		_folderFramesAttached = false;
		_folderRenderingTime = TimeSpan.MinValue;
	}

	private void OnFolderFrame(object? sender, EventArgs e)
	{
		if (e is RenderingEventArgs e2)
		{
			if (_folderRenderingTime == e2.RenderingTime)
			{
				return;
			}
			_folderRenderingTime = e2.RenderingTime;
		}
		if (!base.IsVisible)
		{
			ResetFolderPresentation();
			return;
		}
		long timestamp = Stopwatch.GetTimestamp();
		double totalSeconds = Stopwatch.GetElapsedTime(_folderFrameTicks, timestamp).TotalSeconds;
		_folderFrameTicks = timestamp;
		ThemeService? current = ThemeService.Current;
		if (current != null && !current.AnimationsEnabled)
		{
			_folderOpen.Snap(_folderOpen.Target);
			_fusionSpring.Snap(_fusionSpring.Target);
		}
		else
		{
			_folderOpen.Advance(totalSeconds);
			_fusionSpring.Advance(totalSeconds);
		}
		if (_displayedFolderId != null)
		{
			ApplyFolderFrame();
		}
		ApplyFusionFrame();
		if (!_folderOpen.IsMoving && !_fusionSpring.IsMoving && _folderIntent.TargetId == null)
		{
			StopFolderFrames();
		}
	}

	private void ApplyFolderFrame()
	{
		double value = _folderOpen.Value;
		double num = Math.Clamp(value, 0.0, 1.0);
		Rect folderOrigin = _folderOrigin;
		Rect folderDestination = _folderDestination;
		Rect rect = new Rect(folderOrigin.X + (folderDestination.X - folderOrigin.X) * value, folderOrigin.Y + (folderDestination.Y - folderOrigin.Y) * value, Math.Max(1.0, folderOrigin.Width + (folderDestination.Width - folderOrigin.Width) * value), Math.Max(1.0, folderOrigin.Height + (folderDestination.Height - folderOrigin.Height) * value));
		Canvas.SetLeft(FolderCard, rect.X);
		Canvas.SetTop(FolderCard, rect.Y);
		FolderCard.Width = rect.Width;
		FolderCard.Height = rect.Height;
		double radius = AppIconSize * 0.38 + (44.0 - AppIconSize * 0.38) * num;
		FolderGlass.Radius = radius;
		FolderGlass.InvalidateVisual();
		FolderCard.Clip = SquircleGlassGeometry.CreateCompact(new Rect(rect.Size), radius);
		FolderShadow.Data = FolderCard.Clip;
		Canvas.SetLeft(FolderShadow, rect.X);
		Canvas.SetTop(FolderShadow, rect.Y + 5.0);
		FolderShadow.Opacity = num;
		GroupContent.Opacity = Math.Clamp((num - 0.24) / 0.58, 0.0, 1.0);
		FolderDismissArea.Opacity = num;
		TileGrid.Opacity = 1.0 - num * 0.3;
		foreach (LauncherAppTileButton item in TileGrid.Children.OfType<LauncherAppTileButton>())
		{
			if (_logicalIconDrag && !_folderClosing)
			{
				break;
			}
			if (_logicalIconDrag && object.Equals(item.Tag, _dragIconId))
			{
				item.Opacity = 0.0;
				continue;
			}
			Rect rect2 = IconBounds(item, FolderCanvas);
			item.Opacity = (rect.IntersectsWith(rect2) ? (1.0 - num) : 1.0);
		}
		int num2 = 0;
		foreach (Button item2 in GroupItems.Children.OfType<Button>())
		{
			if (_logicalIconDrag)
			{
				break;
			}
			double num3 = Math.Clamp((num - 0.32 - (double)Math.Min(num2++, 11) * 0.025) / 0.35, 0.0, 1.0);
			double num4 = 1.0 - Math.Pow(1.0 - num3, 3.0);
			item2.Opacity = num3;
			TransformGroup transformGroup = new TransformGroup();
			transformGroup.Children.Add(new ScaleTransform(0.84 + 0.16 * num4, 0.84 + 0.16 * num4));
			transformGroup.Children.Add(new TranslateTransform(0.0, (1.0 - num4) * 15.0));
			item2.RenderTransform = transformGroup;
		}
		if (!_folderClosing || _folderOpen.IsMoving || _folderOpen.Value != 0.0)
		{
			return;
		}
		_displayedFolderId = null;
		_folderItemsKey = null;
		_view.OpenGroupId = null;
		GroupPanel.Visibility = Visibility.Collapsed;
		GroupItems.Children.Clear();
		TileGrid.Opacity = 1.0;
		foreach (Button item3 in TileGrid.Children.OfType<Button>())
		{
			item3.Opacity = ((_dragOrigin == null || !object.Equals(item3.Tag, _dragIconId)) ? 1 : 0);
		}
		this.NavigationAvailabilityChanged?.Invoke(this, EventArgs.Empty);
	}

	private void PrepareIconDrag(Button source, Point grabPoint)
	{
		if (_workspace == null)
		{
			return;
		}
		object tag = source.Tag;
		string id = tag as string;
		if (id == null)
		{
			return;
		}
		ClearFolderHover(immediate: true);
		_dropLandingId = null;
		_dragSourceGroupId = (GroupItems.Children.Contains(source) ? _view.OpenGroupId : null);
		_memberOrigin = null;
		_memberPreviewIndex = -1;
		_layoutBeforePlacement = null;
		_placementCandidateIndex = -1;
		_memberHasLeft = false;
		LauncherSlot slot = CurrentSlots().FirstOrDefault((LauncherSlot s) => s.ItemId == id || s.Group?.Id == id) ?? new LauncherSlot
		{
			ItemId = id
		};
		_ghostIconCenterOffset = ((source is LauncherAppTileButton launcherAppTileButton) ? launcherAppTileButton.IconHost.TranslatePoint(new Point(launcherAppTileButton.IconHost.ActualWidth / 2.0, launcherAppTileButton.IconHost.ActualHeight / 2.0), source) : new Point(source.ActualWidth / 2.0, AppIconSize / 2.0));
		StopPageSlide();
		StopIconDragFrames();
		_dragPreviewCommits = 0L;
		ClearDragGhost();
		_pressedId = null;
		_pressedTile = null;
		_dropCommitted = false;
		_suppressTileClick = true;
		_dragEdit = (_settings.GroupModeEnabled ? _workspace.BeginGroupModeEdit(_activeFilterGroupId, _view.Capacity) : _workspace.BeginEdit());
		_dragOrigin = LauncherLayoutRules.DeepClone(_dragEdit.Draft);
		if (_dragSourceGroupId != null)
		{
			_memberOrigin = LauncherLayoutRules.DeepClone(_dragOrigin);
			int num = _dragOrigin.Pages.FindIndex((LauncherPage p) => p.Slots.Any((LauncherSlot s) => s.Group?.Id == _dragSourceGroupId));
			int num2 = _dragOrigin.Pages[num].Slots.FindIndex((LauncherSlot s) => s.Group?.Id == _dragSourceGroupId);
			_dragOrigin = LauncherLayoutEditor.RemoveFromGroup(_dragOrigin, id, _dragSourceGroupId, num, num2 + 1, _view.Capacity);
		}
		_retainedTilePages.Clear();
		_dragIconId = id;
		_dragPageCount = _dragOrigin.Pages.Count;
		_grabPoint = grabPoint;
		_previewIndex = _view.Page * _view.Capacity + TileGrid.Children.IndexOf(source);
		_previewExchange = false;
		_pagePreviewAfter = DateTimeOffset.MinValue;
		_edgePager.Reset();
		_dragTiles.Clear();
		_dragTileOffsets.Clear();
		_iconOffsetMotions.Clear();
		foreach (Button item in TileGrid.Children.OfType<Button>())
		{
			if (item.Tag is string key)
			{
				_dragTiles[key] = item;
			}
		}
		_dragGhost = CreateTile(slot);
		_dragGhost.Width = source.ActualWidth;
		_dragGhost.Height = source.ActualHeight;
		_dragGhost.Margin = new Thickness(0.0);
		_dragGhost.IsHitTestVisible = false;
		_dragGhost.ToolTip = null;
		_dragGhostMotion = new TranslateTransform();
		TransformGroup transformGroup = new TransformGroup();
		transformGroup.Children.Add(new ScaleTransform(1.06, 1.06));
		transformGroup.Children.Add(_dragGhostMotion);
		_dragGhost.RenderTransform = transformGroup;
		_dragGhost.Effect = new DropShadowEffect
		{
			BlurRadius = 12.0,
			ShadowDepth = 4.0,
			Opacity = 0.22
		};
		DragOverlay.Children.Add(_dragGhost);
		Point point = source.TranslatePoint(new Point(0.0, 0.0), DragOverlay);
		Canvas.SetLeft(_dragGhost, point.X);
		Canvas.SetTop(_dragGhost, point.Y);
		source.Opacity = 0.0;
		source.IsHitTestVisible = false;
		TrashDropTarget.Visibility = Visibility.Visible;
		EnsureIconDragFrames();
	}

	private void UpdateDragPointer()
	{
		if (_dragOrigin == null)
		{
			return;
		}
		(int, int)? cursorPixelPoint = DisplayService.GetCursorPixelPoint();
		if (!cursorPixelPoint.HasValue)
		{
			return;
		}
		(int, int) valueOrDefault = cursorPixelPoint.GetValueOrDefault();
		Point point = new Point(valueOrDefault.Item1, valueOrDefault.Item2);
		Point point2 = DragOverlay.PointFromScreen(point);
		Button dragGhost = _dragGhost;
		if (dragGhost != null)
		{
			TranslateTransform dragGhostMotion = _dragGhostMotion;
			if (dragGhostMotion != null)
			{
				dragGhostMotion.X = point2.X - _grabPoint.X - Canvas.GetLeft(dragGhost);
				dragGhostMotion.Y = point2.Y - _grabPoint.Y - Canvas.GetTop(dragGhost);
			}
		}
		_lastDragPoint = TileGrid.PointFromScreen(point);
		_lastDragPoint.X += IncomingPageMotion.X;
		Point point3 = PageViewport.PointFromScreen(point);
		Point point4 = TrashDropTarget.PointFromScreen(point);
		if (point3.X < 0.0 || point3.Y < 0.0 || point3.X >= PageViewport.ActualWidth || point3.Y >= PageViewport.ActualHeight || (point4.X >= 0.0 && point4.Y >= 0.0 && point4.X < TrashDropTarget.ActualWidth && point4.Y < TrashDropTarget.ActualHeight))
		{
			ClearFolderHover();
			_placementCandidateIndex = -1;
		}
		else
		{
			QueueIconPreview(_lastDragPoint, (Keyboard.Modifiers & ModifierKeys.Alt) != 0);
		}
	}

	private void UpdatePointerFromOleFeedback()
	{
		if (_trackDragPointer && _dragOrigin != null)
		{
			long timestamp = Stopwatch.GetTimestamp();
			if (_lastOlePointerSampleTicks == 0L || !(Stopwatch.GetElapsedTime(_lastOlePointerSampleTicks, timestamp).TotalMilliseconds < 8.0))
			{
				_lastOlePointerSampleTicks = timestamp;
				UpdateDragPointer();
			}
		}
	}

	private void QueueIconPreview(Point point, bool exchange)
	{
		if (_dragOrigin != null)
		{
			_pendingIconPreview = (point, exchange);
			EnsureIconDragFrames();
		}
	}

	private void EnsureIconDragFrames()
	{
		if (!_iconPreviewQueued)
		{
			_iconPreviewQueued = true;
			CompositionTarget.Rendering += CommitQueuedIconPreview;
		}
	}

	private void CommitQueuedIconPreview(object? sender, EventArgs e)
	{
		if (_dragOrigin == null)
		{
			return;
		}
		if (e is RenderingEventArgs e2)
		{
			if (_lastIconRenderingTime == e2.RenderingTime)
			{
				return;
			}
			_lastIconRenderingTime = e2.RenderingTime;
		}
		AdvanceIconMotions();
		if (_trackDragPointer)
		{
			UpdateDragPointer();
			UpdateDragEdge();
			QueueIconOleTransfer();
		}
		(Point, bool)? pendingIconPreview = _pendingIconPreview;
		CancelQueuedIconPreview();
		if (pendingIconPreview.HasValue)
		{
			(Point, bool) valueOrDefault = pendingIconPreview.GetValueOrDefault();
			if (_dragOrigin != null)
			{
				long timestamp = Stopwatch.GetTimestamp();
				PreviewIconPlacement(valueOrDefault.Item1, valueOrDefault.Item2);
				_lastDragPreviewMilliseconds = Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
				_dragPreviewCommits++;
			}
		}
	}

	private void CancelQueuedIconPreview()
	{
		_pendingIconPreview = null;
	}

	private void StopIconDragFrames()
	{
		if (_iconPreviewQueued)
		{
			CompositionTarget.Rendering -= CommitQueuedIconPreview;
		}
		_iconPreviewQueued = false;
		_trackDragPointer = false;
		_lastOlePointerSampleTicks = 0L;
		_lastIconRenderingTime = null;
		_pendingIconPreview = null;
	}

	private void PreviewIconPlacement(Point point, bool exchange)
	{
		PlaceIconAtPointer(point, exchange, snap: false);
	}

	private void PlaceIconAtPointer(Point point, bool exchange, bool snap)
	{
		if (snap)
		{
			CancelQueuedIconPreview();
			_placementCandidateIndex = -1;
		}
		if (_dragOrigin == null || _dragEdit == null || _dragIconId == null || TileGrid.ActualWidth <= 0.0 || TileGrid.ActualHeight <= 0.0 || (!snap && DateTimeOffset.UtcNow < _pagePreviewAfter))
		{
			return;
		}
		if (snap)
		{
			LauncherPage? launcherPage = _dragOrigin.Pages.ElementAtOrDefault(_view.Page);
			if (launcherPage == null || launcherPage.Slots.Count == 0)
			{
				point = new Point(Math.Clamp(point.X, 0.0, TileGrid.ActualWidth - 0.01), Math.Clamp(point.Y, 0.0, TileGrid.ActualHeight - 0.01));
			}
		}
		if (point.X < 0.0 || point.Y < 0.0 || point.X >= TileGrid.ActualWidth || point.Y >= TileGrid.ActualHeight)
		{
			_placementCandidateIndex = -1;
		}
		else
		{
			if (PreviewFolderMemberInside(point))
			{
				return;
			}
			if (!snap)
			{
				UpdateFolderHover(point, exchange);
				if (_folderIntent.TargetId != null)
				{
					_placementCandidateIndex = -1;
					return;
				}
			}
			double num = TileGrid.ActualWidth / (double)_view.Columns;
			double num2 = TileGrid.ActualHeight / (double)_view.Rows;
			int num3 = Math.Clamp((int)(point.X / num), 0, _view.Columns - 1);
			int num4 = Math.Clamp((int)(point.Y / num2), 0, _view.Rows - 1);
			List<LauncherSlot> obj = _dragOrigin.Pages.ElementAtOrDefault(_view.Page)?.Slots;
			int num5 = obj?.Count ?? 0;
			bool flag = obj?.Any((LauncherSlot s) => s.ItemId == _dragIconId || s.Group?.Id == _dragIconId) ?? false;
			int val = Math.Max(0, num5 - (flag ? 1 : 0));
			int num6 = _view.Page * _view.Capacity + Math.Min(num4 * _view.Columns + num3, val);
			if (num6 == _previewIndex && exchange == _previewExchange)
			{
				_placementCandidateIndex = -1;
				return;
			}
			if (!snap && _previewIndex >= 0 && num6 != _previewIndex)
			{
				double num7 = point.X - (double)num3 * num;
				double num8 = point.Y - (double)num4 * num2;
				if (num7 < 8.0 || num7 > num - 8.0 || num8 < 8.0 || num8 > num2 - 8.0)
				{
					_placementCandidateIndex = -1;
					return;
				}
			}
			if (!snap && !exchange && !_settings.GroupModeEnabled)
			{
				if (_placementCandidateIndex != num6)
				{
					_placementCandidateIndex = num6;
					_placementCandidateStarted = Stopwatch.GetTimestamp();
					return;
				}
				if (Stopwatch.GetElapsedTime(_placementCandidateStarted).TotalMilliseconds < 160.0)
				{
					return;
				}
			}
			_placementCandidateIndex = -1;
			_previewIndex = num6;
			_previewExchange = exchange;
			_layoutBeforePlacement = LauncherLayoutRules.DeepClone(_dragEdit.Draft);
			_dragEdit.Draft = (_settings.GroupModeEnabled ? LauncherFilterRules.PlaceAtSlot(_dragOrigin, _dragIconId, num6, _view.Capacity, exchange) : LauncherLayoutEditor.PlaceAtSlot(_dragOrigin, _dragIconId, num6, _view.Capacity, exchange));
			if (snap)
			{
				StopPageSlide();
			}
			RenderIconPreview();
		}
	}

	private void NavigateDragPage(int direction)
	{
		if (_dragOrigin != null)
		{
			CancelQueuedIconPreview();
			ClearFolderHover();
			int page = _view.Page;
			if (direction > 0 && page == _view.PageCount - 1 && page < _dragOrigin.Pages.Count && VisiblePageSlots().Any())
			{
				_dragPageCount = _view.PageCount + 1;
				UpdatePageCounts();
			}
			_view.MoveToPage(page + direction);
			if (_view.Page != page)
			{
				_previewIndex = -1;
				_placementCandidateIndex = -1;
				_pagePreviewAfter = DateTimeOffset.UtcNow.AddMilliseconds(250.0);
				SlideToDragPage(page, direction);
			}
		}
	}

	private void SlideToDragPage(int previousPage, int direction)
	{
		_view.MoveToPage(previousPage);
		RenderIconPreview(materialize: true);
		SlideToPage(direction);
		foreach (Button item in TileGrid.Children.OfType<Button>())
		{
			if (item.Tag is string text)
			{
				_dragTiles[text] = item;
				if (text == _dragIconId)
				{
					item.Opacity = 0.0;
					item.IsHitTestVisible = false;
				}
			}
		}
	}

	private void RenderIconPreview(bool materialize = false)
	{
		if (OutgoingPageGrid.Children.Count > 0)
		{
			StopPageSlide();
		}
		Dictionary<Button, int> dictionary = TileGrid.Children.OfType<Button>().Select((Button tile, int index) => (tile: tile, index: index)).ToDictionary(((Button tile, int index) pair) => pair.tile, ((Button tile, int index) pair) => pair.index);
		UpdatePageCounts();
		List<Button> list = new List<Button>();
		List<Button> list2 = new List<Button>();
		foreach (LauncherSlot item in VisiblePageSlots())
		{
			string text = ((item.Kind == LauncherSlotKind.Group) ? item.Group.Id : item.ItemId);
			if (!_dragTiles.TryGetValue(text, out Button value))
			{
				value = (_dragTiles[text] = CreateTile(item));
			}
			value.Opacity = ((!(text == _dragIconId)) ? 1 : 0);
			value.IsHitTestVisible = text != _dragIconId;
			list.Add(value);
		}
		if (!materialize && _dragOrigin != null && list.Count == dictionary.Count && list.All(dictionary.ContainsKey))
		{
			double num = TileGrid.ActualWidth / (double)_view.Columns;
			double num2 = TileGrid.ActualHeight / (double)_view.Rows;
			for (int num3 = 0; num3 < list.Count; num3++)
			{
				Button button2 = list[num3];
				int num4 = dictionary[button2];
				MoveIconToOffset(button2, (double)(num3 % _view.Columns - num4 % _view.Columns) * num, (double)(num3 / _view.Columns - num4 / _view.Columns) * num2);
			}
			_lastTilesRenderKey = null;
			return;
		}
		Dictionary<string, Point> dictionary2 = (from b in TileGrid.Children.OfType<Button>()
			where b.Tag is string
			select b).ToDictionary<Button, string, Point>((Button b) => (string)b.Tag, (Button b) => b.TranslatePoint(new Point(0.0, 0.0), TileGrid), StringComparer.Ordinal);
		_dragTileOffsets.Clear();
		_iconOffsetMotions.Clear();
		HashSet<Button> hashSet = list.ToHashSet();
		for (int num5 = TileGrid.Children.Count - 1; num5 >= 0; num5--)
		{
			if (!hashSet.Contains((Button)TileGrid.Children[num5]))
			{
				TileGrid.Children.RemoveAt(num5);
			}
		}
		for (int num6 = 0; num6 < list.Count; num6++)
		{
			Button button3 = list[num6];
			if (num6 >= TileGrid.Children.Count || TileGrid.Children[num6] != button3)
			{
				TileGrid.Children.Remove(button3);
				TileGrid.Children.Insert(num6, button3);
			}
			if (!dictionary.TryGetValue(button3, out var value2) || value2 != num6 || button3.RenderTransform is TranslateTransform)
			{
				button3.RenderTransform = new TranslateTransform();
				list2.Add(button3);
			}
		}
		TileGrid.UpdateLayout();
		foreach (Button item2 in list2)
		{
			if (item2.Tag is string key && dictionary2.TryGetValue(key, out var value3))
			{
				Point point = item2.TranslatePoint(new Point(0.0, 0.0), TileGrid);
				AnimateIconOffset((TranslateTransform)item2.RenderTransform, value3.X - point.X, value3.Y - point.Y);
			}
		}
		PageText.Text = $"{_view.Page + 1} / {_view.PageCount}";
		PreviousPage.IsEnabled = _view.Page > 0;
		NextPage.IsEnabled = _view.Page < _view.PageCount - 1;
		_lastTilesRenderKey = null;
	}

	private void MoveIconToOffset(Button tile, double x, double y)
	{
		TranslateTransform translateTransform = tile.RenderTransform as TranslateTransform;
		if (translateTransform == null)
		{
			if (Math.Abs(x) < 0.1 && Math.Abs(y) < 0.1)
			{
				return;
			}
			translateTransform = (TranslateTransform)(tile.RenderTransform = new TranslateTransform());
		}
		if (_dragTileOffsets.TryGetValue(tile, out var value) && Math.Abs(value.X - x) < 0.1 && Math.Abs(value.Y - y) < 0.1)
		{
			return;
		}
		_dragTileOffsets[tile] = new Point(x, y);
		double x2 = translateTransform.X;
		double y2 = translateTransform.Y;
		if (!_iconOffsetMotions.TryGetValue(tile, out IconOffsetMotion value2) || value2.transform != translateTransform)
		{
			translateTransform.BeginAnimation(TranslateTransform.XProperty, null);
			translateTransform.BeginAnimation(TranslateTransform.YProperty, null);
			value2 = (_iconOffsetMotions[tile] = new IconOffsetMotion(translateTransform));
		}
		translateTransform.X = x2;
		translateTransform.Y = y2;
		if (tile.Opacity != 0.0)
		{
			ThemeService? current = ThemeService.Current;
			if (current == null || current.AnimationsEnabled)
			{
				value2.Start = new Point(x2, y2);
				value2.Target = new Point(x, y);
				value2.Started = Stopwatch.GetTimestamp();
				value2.Active = true;
				EnsureIconDragFrames();
				return;
			}
		}
		value2.Active = false;
		translateTransform.X = x;
		translateTransform.Y = y;
	}

	private void AdvanceIconMotions()
	{
		long timestamp = Stopwatch.GetTimestamp();
		foreach (IconOffsetMotion value in _iconOffsetMotions.Values)
		{
			if (value.Active)
			{
				double num = Math.Min(1.0, Stopwatch.GetElapsedTime(value.Started, timestamp).TotalMilliseconds / 210.0);
				ThemeService? current2 = ThemeService.Current;
				if (current2 != null && !current2.AnimationsEnabled)
				{
					num = 1.0;
				}
				double num2 = 1.0 - num;
				double num3 = 1.0 - num2 * num2 * num2;
				value.transform.X = value.Start.X + (value.Target.X - value.Start.X) * num3;
				value.transform.Y = value.Start.Y + (value.Target.Y - value.Start.Y) * num3;
				if (num == 1.0)
				{
					value.Active = false;
				}
			}
		}
	}

	private static void AnimateIconOffset(TranslateTransform motion, double x, double y)
	{
		ThemeService? current = ThemeService.Current;
		if (current == null || current.AnimationsEnabled)
		{
			TimeSpan timeSpan = TimeSpan.FromMilliseconds(210L);
			CubicEase easingFunction = new CubicEase
			{
				EasingMode = EasingMode.EaseOut
			};
			if (Math.Abs(x) > 0.1)
			{
				motion.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(x, 0.0, timeSpan)
				{
					EasingFunction = easingFunction,
					FillBehavior = FillBehavior.Stop
				});
			}
			if (Math.Abs(y) > 0.1)
			{
				motion.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(y, 0.0, timeSpan)
				{
					EasingFunction = easingFunction,
					FillBehavior = FillBehavior.Stop
				});
			}
		}
	}

	private void FinishIconDrag()
	{
		StopIconDragFrames();
		bool flag = _dragOrigin != null;
		Button ghost = _dragGhost;
		string dragIconId = _dragIconId;
		string dragSourceGroupId = _dragSourceGroupId;
		bool flag2 = _dropLandingId != null;
		bool num = flag2 || dragSourceGroupId != null;
		string landingId = _dropLandingId ?? dragIconId;
		ClearFolderHover();
		_pressTimer?.Stop();
		_pressTimer = null;
		_edgePager.Reset();
		ClearDropTarget();
		_dragEdit = null;
		_dragOrigin = null;
		_dragPageCount = 0;
		_dragIconId = null;
		_pressedId = null;
		_pressedTile = null;
		_dragSourceGroupId = null;
		_memberOrigin = null;
		_dropLandingId = null;
		TrashDropTarget.Visibility = Visibility.Collapsed;
		StopPageSlide();
		if (!_dropCommitted && dragSourceGroupId != null)
		{
			_view.OpenGroupId = dragSourceGroupId;
			_folderClosing = false;
		}
		if (num)
		{
			_folderItemsKey = null;
			RenderTiles();
		}
		else if (flag)
		{
			RenderIconPreview();
		}
		else if (_dropCommitted)
		{
			RenderTiles();
		}
		if (num)
		{
			TileGrid.UpdateLayout();
		}
		_dragTiles.Clear();
		_dragTileOffsets.Clear();
		_iconOffsetMotions.Clear();
		Button landing = TileGrid.Children.OfType<Button>().FirstOrDefault((Button b) => object.Equals(b.Tag, landingId));
		if (_dropCommitted && ghost != null && landing != null && base.IsVisible)
		{
			ThemeService? current = ThemeService.Current;
			if (current == null || current.AnimationsEnabled)
			{
				landing.Opacity = (flag2 ? 1 : 0);
				if (flag2 && landing is LauncherAppTileButton launcherAppTileButton)
				{
					launcherAppTileButton.AnimateFolderArrival();
				}
				Point point = landing.TranslatePoint(new Point(0.0, 0.0), DragOverlay);
				CubicEase easingFunction = new CubicEase
				{
					EasingMode = EasingMode.EaseOut
				};
				TimeSpan timeSpan = TimeSpan.FromMilliseconds(180L);
				if (flag2)
				{
					ghost.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.0, timeSpan));
				}
				TranslateTransform? dragGhostMotion = _dragGhostMotion;
				DoubleAnimation animation = new DoubleAnimation(dragGhostMotion.X, point.X - Canvas.GetLeft(ghost), timeSpan)
				{
					EasingFunction = easingFunction
				};
				DoubleAnimation doubleAnimation = new DoubleAnimation(dragGhostMotion.Y, point.Y - Canvas.GetTop(ghost), timeSpan)
				{
					EasingFunction = easingFunction
				};
				doubleAnimation.Completed += delegate
				{
					landing.Opacity = 1.0;
					if (_dragGhost == ghost)
					{
						ClearDragGhost();
					}
				};
				dragGhostMotion.BeginAnimation(TranslateTransform.XProperty, animation);
				dragGhostMotion.BeginAnimation(TranslateTransform.YProperty, doubleAnimation);
				if (ghost.RenderTransform is TransformGroup transformGroup)
				{
					ScaleTransform scaleTransform = transformGroup.Children.OfType<ScaleTransform>().FirstOrDefault();
					if (scaleTransform != null)
					{
						scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(flag2 ? 0.3 : 1.0, timeSpan)
						{
							EasingFunction = easingFunction
						});
						scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(flag2 ? 0.3 : 1.0, timeSpan)
						{
							EasingFunction = easingFunction
						});
					}
				}
				goto IL_039d;
			}
		}
		ClearDragGhost();
		goto IL_039d;
		IL_039d:
		base.Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)delegate
		{
			_suppressTileClick = false;
		});
	}

	private void ClearDragGhost()
	{
		ClearFolderHover(immediate: true);
		DragOverlay.Children.Clear();
		_dragGhost = null;
		_dragGhostMotion = null;
		foreach (Button item in TileGrid.Children.OfType<Button>())
		{
			item.Opacity = 1.0;
			item.IsHitTestVisible = true;
		}
	}

	private void ActivateEntry(string id)
	{
		if (_services == null)
		{
			return;
		}
		ActivationResult activationResult = _services.Launch.ActivateOrLaunch(id);
		if (activationResult.Outcome == ActivationOutcome.MultipleWindowsFound)
		{
			ContextMenu contextMenu = CreateTrackedContextMenu();
			foreach (WindowCandidate candidate in activationResult.Candidates)
			{
				MenuItem menuItem = new MenuItem
				{
					Header = candidate.Title
				};
				menuItem.Click += delegate
				{
					ActivationResult activationResult2 = LaunchService.ActivateWindow(candidate.Handle);
					if (activationResult2.Outcome == ActivationOutcome.Failed)
					{
						ShowNotice(activationResult2.Message ?? "窗口未能激活。");
					}
					else
					{
						CloseWithTransition();
					}
				};
				contextMenu.Items.Add(menuItem);
			}
			contextMenu.PlacementTarget = TileGrid;
			contextMenu.IsOpen = true;
		}
		else if (activationResult.Outcome == ActivationOutcome.Failed)
		{
			ShowNotice(activationResult.Message ?? "应用未能打开。");
		}
		else
		{
			CloseWithTransition();
		}
	}

	private void ShowNotice(string notice, bool autoDismiss = false)
	{
		_noticeTimer?.Stop();
		_noticeTimer = null;
		_notice = notice;
		RefreshDiagnostics();
		if (!autoDismiss)
		{
			return;
		}
		DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Normal, base.Dispatcher)
		{
			Interval = TimeSpan.FromSeconds(3L)
		};
		timer.Tick += delegate
		{
			timer.Stop();
			if (_noticeTimer == timer)
			{
				_noticeTimer = null;
				if (!(_notice != notice))
				{
					_notice = null;
					RefreshDiagnostics();
				}
			}
		};
		_noticeTimer = timer;
		timer.Start();
	}

	private void CommitEdit(Func<LauncherLayout, LauncherLayout> transform)
	{
		if (_workspace == null || _view.IsSearching)
		{
			ShowNotice("搜索结果中不能整理应用；请先清空搜索。");
			return;
		}
		LauncherEditSession launcherEditSession = _workspace.BeginEdit();
		try
		{
			launcherEditSession.Draft = transform(launcherEditSession.Draft);
			if (!_workspace.Commit(launcherEditSession))
			{
				ShowNotice("应用库或布局已变化，请重试整理操作。");
			}
			else
			{
				_notice = null;
			}
		}
		catch (Exception ex) when (((ex is StorageException || ex is InvalidOperationException || ex is ArgumentException) ? 1 : 0) != 0)
		{
			ShowNotice("整理未保存，原布局已恢复：" + ex.Message);
		}
		RenderTiles();
	}

	private ContextMenu BuildTileMenu(LauncherSlot slot)
	{
		ContextMenu contextMenu = CreateTrackedContextMenu();
		string id = ((slot.Kind != LauncherSlotKind.Group) ? slot.ItemId : slot.Group?.Id);
		if (id == null)
		{
			return contextMenu;
		}
		if (slot.Kind == LauncherSlotKind.Group)
		{
			AddMenu(contextMenu, "打开文件夹", delegate
			{
				_view.OpenGroupId = id;
				RenderGroup();
			});
			AddMenu(contextMenu, "重命名文件夹", delegate
			{
				RenameGroup(id);
			});
			AddMenu(contextMenu, "解散文件夹", delegate
			{
				CommitEdit((LauncherLayout layout) => LauncherLayoutEditor.Dissolve(layout, id, _view.Capacity));
			});
		}
		else
		{
			AddMenu(contextMenu, "打开", delegate
			{
				ActivateEntry(id);
			});
			AddMenu(contextMenu, "编辑名称、图标和别名", delegate
			{
				EditEntry(id);
			});
			AppServiceHost? services = _services;
			if (services != null && services.Library.Find(id)?.IsTargetMissing == true)
			{
				AddMenu(contextMenu, "重新定位…", delegate
				{
					RelocateEntry(id);
				});
			}
			LauncherGroup group = _workspace?.Layout.Pages.SelectMany((LauncherPage page) => page.Slots).SingleOrDefault((LauncherSlot item) => item.Group?.ItemIds.Contains(id) ?? false)?.Group;
			if (group != null && !_settings.GroupModeEnabled)
			{
				int index = group.ItemIds.IndexOf(id);
				if (index > 0)
				{
					AddMenu(contextMenu, "在文件夹中前移", delegate
					{
						CommitEdit((LauncherLayout layout) => LauncherLayoutEditor.MoveGroupMember(layout, group.Id, id, index - 1));
					});
				}
				if (index < group.ItemIds.Count - 1)
				{
					AddMenu(contextMenu, "在文件夹中后移", delegate
					{
						CommitEdit((LauncherLayout layout) => LauncherLayoutEditor.MoveGroupMember(layout, group.Id, id, index + 1));
					});
				}
				AddMenu(contextMenu, "移出文件夹", delegate
				{
					CommitEdit((LauncherLayout layout) => LauncherLayoutEditor.RemoveFromGroup(layout, id, group.Id, _view.Page, 0, _view.Capacity));
				});
			}
		}
		return contextMenu;
	}

	private static void AddMenu(ContextMenu menu, string title, Action action)
	{
		MenuItem menuItem = new MenuItem
		{
			Header = title
		};
		menuItem.Click += delegate
		{
			action();
		};
		menu.Items.Add(menuItem);
	}

	private void BeginCapturedIconDrag()
	{
		_logicalIconDrag = true;
		_iconDragSession++;
		_cursorBeforeIconDrag = base.Cursor;
		base.Cursor = Cursors.Hand;
		AddHandler(Mouse.PreviewMouseUpEvent, new MouseButtonEventHandler(OnCapturedIconMouseUp), handledEventsToo: true);
		AddHandler(Mouse.LostMouseCaptureEvent, new MouseEventHandler(OnIconMouseCaptureLost), handledEventsToo: true);
		AddHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(OnCapturedIconKeyDown), handledEventsToo: true);
		try
		{
			this.InternalDragStarted?.Invoke(this, EventArgs.Empty);
			if (_logicalIconDrag && !CaptureIconMouse())
			{
				RunIconOleTransfer(returnToCapture: false);
			}
		}
		catch
		{
			EndCapturedIconDrag();
			throw;
		}
	}

	private bool CaptureIconMouse()
	{
		_capturedIconDrag = Mouse.Capture(this, CaptureMode.SubTree);
		return _capturedIconDrag;
	}

	private void ReleaseIconMouseCapture()
	{
		_capturedIconDrag = false;
		if (Mouse.Captured == this)
		{
			Mouse.Capture(null);
		}
	}

	private void EndCapturedIconDrag()
	{
		if (!_logicalIconDrag)
		{
			return;
		}
		_logicalIconDrag = false;
		_oleTransferQueued = false;
		RemoveHandler(Mouse.PreviewMouseUpEvent, new MouseButtonEventHandler(OnCapturedIconMouseUp));
		RemoveHandler(Mouse.LostMouseCaptureEvent, new MouseEventHandler(OnIconMouseCaptureLost));
		RemoveHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(OnCapturedIconKeyDown));
		ReleaseIconMouseCapture();
		try
		{
			FinishIconDrag();
		}
		finally
		{
			base.Cursor = _cursorBeforeIconDrag;
			Mouse.SetCursor(null);
			this.InternalDragFinished?.Invoke(this, EventArgs.Empty);
		}
	}

	private void OnCapturedIconMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (!_capturedIconDrag || e.ChangedButton != MouseButton.Left)
		{
			return;
		}
		e.Handled = true;
		try
		{
			if (_workspace == null || _dragEdit == null || _dragIconId == null || !PointerIsOverLauncher())
			{
				return;
			}
			(int, int)? cursorPixelPoint = DisplayService.GetCursorPixelPoint();
			if (!cursorPixelPoint.HasValue)
			{
				return;
			}
			(int, int) valueOrDefault = cursorPixelPoint.GetValueOrDefault();
			Point point = new Point(valueOrDefault.Item1, valueOrDefault.Item2);
			Point point2 = TrashDropTarget.PointFromScreen(point);
			if (point2.X >= 0.0 && point2.Y >= 0.0 && point2.X < TrashDropTarget.ActualWidth && point2.Y < TrashDropTarget.ActualHeight)
			{
				_dragEdit.Draft = LauncherLayoutEditor.RemoveShortcut(_dragEdit.Draft, _dragIconId, _view.Capacity);
			}
			else
			{
				Point point3 = TileGrid.PointFromScreen(point);
				point3.X += IncomingPageMotion.X;
				bool exchange = (Keyboard.Modifiers & ModifierKeys.Alt) != 0;
				if (!ApplyFolderDrop(point3, exchange))
				{
					PlaceIconAtPointer(point3, exchange, snap: true);
				}
			}
			_dropCommitted = _workspace.Commit(_dragEdit);
			if (!_dropCommitted)
			{
				ShowNotice("拖拽期间布局或应用库已变化，请重试。");
			}
		}
		catch (Exception ex) when (((ex is StorageException || ex is InvalidOperationException || ex is ArgumentException) ? 1 : 0) != 0)
		{
			ShowNotice("拖拽未保存，原布局已恢复：" + ex.Message);
		}
		finally
		{
			EndCapturedIconDrag();
		}
	}

	private void OnCapturedIconKeyDown(object sender, KeyEventArgs e)
	{
		if (_capturedIconDrag && e.Key == Key.Escape)
		{
			e.Handled = true;
			_dropCommitted = false;
			EndCapturedIconDrag();
		}
	}

	private void OnIconMouseCaptureLost(object sender, MouseEventArgs e)
	{
		if (_capturedIconDrag && Mouse.Captured != this)
		{
			_dropCommitted = false;
			EndCapturedIconDrag();
		}
	}

	private bool PointerIsOverLauncher()
	{
		if (base.Handle != 0)
		{
			(int, int)? cursorPixelPoint = DisplayService.GetCursorPixelPoint();
			if (cursorPixelPoint.HasValue)
			{
				(int, int) valueOrDefault = cursorPixelPoint.GetValueOrDefault();
				nint num = IconDragNative.WindowFromPoint(new IconDragNative.Point
				{
					X = valueOrDefault.Item1,
					Y = valueOrDefault.Item2
				});
				if (num != 0)
				{
					return IconDragNative.GetAncestor(num, 2u) == base.Handle;
				}
				return false;
			}
		}
		return false;
	}

	private void QueueIconOleTransfer()
	{
		if (!_capturedIconDrag || _oleIconDrag || _oleTransferQueued || PointerIsOverLauncher())
		{
			return;
		}
		_oleTransferQueued = true;
		long session = _iconDragSession;
		base.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)delegate
		{
			if (session == _iconDragSession)
			{
				_oleTransferQueued = false;
				if (_logicalIconDrag && _capturedIconDrag && base.IsVisible && Mouse.LeftButton == MouseButtonState.Pressed && !PointerIsOverLauncher())
				{
					RunIconOleTransfer();
				}
			}
		});
	}

	public GuidedTour BeginOnboardingGuide()
	{
		_onboardingGuide?.Dispose();
		string previousQuery = SearchBox.Text;
		int previousPage = _view.Page;
		_keepOpenWhileModalCount++;
		GuidedTour tour;
		try
		{
			tour = new GuidedTour(this, LauncherGuideRoot, new global::_003C_003Ez__ReadOnlyArray<GuidedTourStep>(new GuidedTourStep[4]
			{
				new GuidedTourStep(SearchBox, "先试着找一个应用", "在高亮的搜索框里输入应用名称、拼音或首字母。结果会即时出现。还没有添加应用时，可以先看完引导，再用“添加”导入。", GuideInteraction.Text),
				new GuidedTourStep(SearchBox, "清空搜索，回到完整页面", "选中搜索文字后删除，就能重新浏览全部页面。没有搜索文字时可以直接下一步；整理或移除图标前也要先清空搜索。", GuideInteraction.ClearText),
				new GuidedTourStep(NextPage, "认识页面切换", "应用分布在多页时，点击箭头翻页。只有一页或搜索结果不满一页时，箭头会变灰，这是正常的，可以直接继续。", GuideInteraction.Click),
				new GuidedTourStep(AddAppButton, "更多应用，从这里添加", "这里可以选择添加文件、程序、文件夹或网站。需要导入时再主动点击；教学不会替你选择文件，也不会启动任何应用。点击“完成实操”回到教学。")
			}));
		}
		catch
		{
			_keepOpenWhileModalCount--;
			throw;
		}
		_onboardingGuide = tour;
		tour.Closed += delegate
		{
			if (_onboardingGuide == tour)
			{
				_onboardingGuide = null;
			}
			_keepOpenWhileModalCount = Math.Max(0, _keepOpenWhileModalCount - 1);
			if (!base.Dispatcher.HasShutdownStarted && base.IsLoaded)
			{
				SearchBox.Text = previousQuery;
				_view.MoveToPage(previousPage);
				RenderTiles();
			}
		};
		return tour;
	}

	private void SlideToPage(int direction)
	{
		int page = _view.Page;
		_view.MoveToPage(page + direction);
		if (_view.Page == page)
		{
			return;
		}
		StopPageSlide();
		double actualWidth = PageViewport.ActualWidth;
		ThemeService? current = ThemeService.Current;
		int num;
		UIElement[] array;
		if (current == null || current.AnimationsEnabled)
		{
			num = ((actualWidth > 0.0) ? 1 : 0);
			if (num != 0)
			{
				array = TileGrid.Children.Cast<UIElement>().ToArray();
				goto IL_007c;
			}
		}
		else
		{
			num = 0;
		}
		array = Array.Empty<UIElement>();
		goto IL_007c;
		IL_007c:
		UIElement[] array2 = array;
		double actualHeight = TileGrid.ActualHeight;
		double verticalOffset = PageScroll.VerticalOffset;
		RenderTiles(reuseUnchanged: false, reusePageTiles: true);
		if (num != 0 && array2.Length != 0)
		{
			OutgoingPageGrid.Rows = TileGrid.Rows;
			OutgoingPageGrid.Columns = TileGrid.Columns;
			OutgoingPageGrid.Width = TileGrid.Width;
			OutgoingPageGrid.Height = actualHeight;
			UIElement[] array3 = array2;
			foreach (UIElement element in array3)
			{
				OutgoingPageGrid.Children.Add(element);
			}
			OutgoingPageScroll.Visibility = Visibility.Visible;
			OutgoingPageScroll.ScrollToVerticalOffset(verticalOffset);
			PageScroll.IsHitTestVisible = false;
			double num2 = (double)Math.Sign(direction) * actualWidth;
			IncomingPageMotion.X = num2;
			TimeSpan timeSpan = TimeSpan.FromMilliseconds(240L);
			DoubleAnimation doubleAnimation = new DoubleAnimation(num2, 0.0, timeSpan)
			{
				EasingFunction = new CubicEase
				{
					EasingMode = EasingMode.EaseOut
				},
				FillBehavior = FillBehavior.HoldEnd
			};
			DoubleAnimation animation = new DoubleAnimation(0.0, 0.0 - num2, timeSpan)
			{
				EasingFunction = new CubicEase
				{
					EasingMode = EasingMode.EaseOut
				},
				FillBehavior = FillBehavior.HoldEnd
			};
			doubleAnimation.Completed += delegate
			{
				StopPageSlide();
			};
			IncomingPageMotion.BeginAnimation(TranslateTransform.XProperty, doubleAnimation, HandoffBehavior.SnapshotAndReplace);
			OutgoingPageMotion.BeginAnimation(TranslateTransform.XProperty, animation, HandoffBehavior.SnapshotAndReplace);
		}
	}

	private void StopPageSlide()
	{
		IncomingPageMotion.BeginAnimation(TranslateTransform.XProperty, null);
		OutgoingPageMotion.BeginAnimation(TranslateTransform.XProperty, null);
		TranslateTransform incomingPageMotion = IncomingPageMotion;
		double x = (OutgoingPageMotion.X = 0.0);
		incomingPageMotion.X = x;
		OutgoingPageScroll.Visibility = Visibility.Collapsed;
		OutgoingPageGrid.Children.Clear();
		PageScroll.IsHitTestVisible = true;
	}

	private void ApplyContentReveal(double opacity)
	{
		opacity = Math.Clamp(opacity, 0.0, 1.0);
		LauncherContent.Opacity = opacity;
		ScaleTransform contentScaleTransform = ContentScaleTransform;
		double scaleX = (ContentScaleTransform.ScaleY = 0.96 + 0.04 * opacity);
		contentScaleTransform.ScaleX = scaleX;
	}

	private TilesRenderKey CurrentTilesRenderKey()
	{
		return new TilesRenderKey(_services.Library.Version, _services.Layouts.LauncherVersion, _view.Query, _view.Page, _view.Rows, _view.Columns, LauncherPanelRoot.Width, LauncherPanelRoot.Height, _view.OpenGroupId, IconPixelSize(AppIconSize), _settings.GroupModeEnabled, _activeFilterGroupId);
	}

	private int IconPixelSize(double size)
	{
		DpiScale dpi = VisualTreeHelper.GetDpi(this);
		return (int)Math.Ceiling(size * Math.Max(dpi.DpiScaleX, dpi.DpiScaleY) * 1.25);
	}

	private List<LauncherSlot> CurrentSlots()
	{
		if (_services == null || _workspace == null)
		{
			return new List<LauncherSlot>();
		}
		if (_view.IsSearching)
		{
			HashSet<string> members = LauncherLayoutRules.AppIds(BrowseLayout).ToHashSet<string>(StringComparer.Ordinal);
			return (from hit in _services.Search.SearchApps(_view.Query, int.MaxValue)
				where members.Contains(hit.Item.Id)
				select new LauncherSlot
				{
					ItemId = hit.Item.Id
				}).ToList();
		}
		return BrowseLayout.Pages.SelectMany((LauncherPage page) => page.Slots).ToList();
	}

	private IEnumerable<LauncherSlot> VisiblePageSlots()
	{
		if (!_view.IsSearching)
		{
			return BrowseLayout.Pages.ElementAtOrDefault(_view.Page)?.Slots ?? new List<LauncherSlot>();
		}
		return CurrentSlots().Skip(_view.Page * _view.Capacity).Take(_view.Capacity);
	}

	private void UpdatePageCounts()
	{
		if (_view.IsSearching)
		{
			_view.SetItemCount(CurrentSlots().Count);
			return;
		}
		List<int> list = BrowseLayout.Pages.Select((LauncherPage page) => page.Slots.Count).ToList();
		while (_dragOrigin != null && list.Count < _dragPageCount)
		{
			list.Add(0);
		}
		_view.SetPageCounts(list);
	}

	private void RenderTiles(bool reuseUnchanged = false, bool reusePageTiles = false)
	{
		if (_services == null || _workspace == null)
		{
			return;
		}
		StopFilterTransition();
		RenderFilterGroups();
		StopPageSlide();
		if (reuseUnchanged && _lastTilesRenderKey == CurrentTilesRenderKey())
		{
			RefreshDiagnostics();
			ApplyContentReveal((!_hasTransformPath) ? 1.0 : (_containerMotion?.CurrentFrame.ContentOpacity ?? 1.0));
			return;
		}
		_lastTilesRenderKey = null;
		List<LauncherSlot> list = CurrentSlots();
		UpdatePageCounts();
		TilesRenderKey tilesRenderKey = CurrentTilesRenderKey()with
		{
			Page = 0
		};
		if (reusePageTiles)
		{
			TilesRenderKey? retainedTileKey = _retainedTileKey;
			TilesRenderKey tilesRenderKey2 = tilesRenderKey;
			if (retainedTileKey.HasValue && !(retainedTileKey.GetValueOrDefault() != tilesRenderKey2) && _dragOrigin == null)
			{
				goto IL_0109;
			}
		}
		_retainedTilePages.Clear();
		_retainedTileKey = tilesRenderKey;
		goto IL_0109;
		IL_0109:
		TileGrid.Rows = _view.Rows;
		TileGrid.Columns = _view.Columns;
		TileGrid.MinHeight = (double)_view.Rows * (AppIconSize + 44.0);
		TileGrid.Children.Clear();
		if (_retainedTilePages.TryGetValue(_view.Page, out LauncherAppTileButton[] value))
		{
			LauncherAppTileButton[] array = value;
			foreach (LauncherAppTileButton element in array)
			{
				TileGrid.Children.Add(element);
			}
		}
		else
		{
			foreach (LauncherSlot item in VisiblePageSlots())
			{
				TileGrid.Children.Add(CreateTile(item));
			}
		}
		_retainedTilePages[_view.Page] = TileGrid.Children.Cast<LauncherAppTileButton>().ToArray();
		int[] array2 = _retainedTilePages.Keys.OrderBy((int p) => Math.Abs(p - _view.Page)).Skip(2).ToArray();
		foreach (int key in array2)
		{
			_retainedTilePages.Remove(key);
		}
		PageText.Text = $"{_view.Page + 1} / {_view.PageCount}";
		PreviousPage.IsEnabled = _view.Page > 0;
		NextPage.IsEnabled = _view.Page < _view.PageCount - 1;
		if (list.Count == 0)
		{
			_notice = (_view.IsSearching ? "没有匹配的应用。" : ((_activeFilterGroupId != null) ? "此分组暂无图标，再次点击分组可查看全部应用。" : "应用库为空。使用“添加”导入文件、文件夹或网址。"));
		}
		else
		{
			bool flag;
			switch (_notice)
			{
			case "没有匹配的应用。":
			case "应用库为空。使用“添加”导入文件、文件夹或网址。":
			case "此分组暂无图标，再次点击分组可查看全部应用。":
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (flag)
			{
				_notice = null;
			}
		}
		RefreshDiagnostics();
		RenderGroup();
		ApplyContentReveal((!_hasTransformPath) ? 1.0 : (_containerMotion?.CurrentFrame.ContentOpacity ?? 1.0));
		_lastTilesRenderKey = CurrentTilesRenderKey();
	}

	private LauncherAppTileButton CreateTile(LauncherSlot slot)
	{
		bool num = slot.Kind == LauncherSlotKind.Group;
		string text = ((!num) ? slot.ItemId : slot.Group?.Id);
		AppEntry appEntry = (num ? null : _services?.Library.Find(text));
		string text2 = ((!num) ? (appEntry?.DisplayName ?? "未知应用") : (slot.Group?.Name ?? "新建分组"));
		bool flag = appEntry?.IsTargetMissing ?? false;
		TextBlock element = new TextBlock
		{
			Text = (flag ? ("⚠ " + text2) : text2),
			TextTrimming = TextTrimming.CharacterEllipsis,
			TextAlignment = TextAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			MaxWidth = 128.0,
			Height = 18.0,
			FontSize = 12.0,
			Margin = new Thickness(0.0, 8.0, 0.0, 0.0)
		};
		StackPanel stackPanel = new StackPanel
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		FrameworkElement iconVisual;
		if (num)
		{
			iconVisual = CreateFolderIcon(slot.Group);
		}
		else
		{
			iconVisual = new PhoneBorder
			{
				Width = AppIconSize,
				Height = AppIconSize,
				CornerRadius = new CornerRadius(AppIconSize / 3.0),
				Background = new LinearGradientBrush(Color.FromRgb(91, 144, 190), Color.FromRgb(67, 92, 136), 90.0),
				Child = new TextBlock
				{
					Text = ((text2.Length > 0) ? text2.Substring(0, 1) : "A"),
					FontSize = 28.0,
					Foreground = Brushes.White,
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center
				}
			};
		}
		iconVisual.HorizontalAlignment = HorizontalAlignment.Center;
		Border border = new Border
		{
			Child = iconVisual
		};
		if (!num && appEntry != null)
		{
			RequestIcon(appEntry, border, AppIconSize);
		}
		stackPanel.Children.Add(border);
		stackPanel.Children.Add(element);
		LauncherAppTileButton button = new LauncherAppTileButton(border)
		{
			Content = stackPanel,
			Tag = text,
			ToolTip = (flag ? (text2 + "：目标已失效。右键可重新定位。") : text2),
			Margin = new Thickness(6.0, 4.0, 6.0, 4.0),
			Padding = new Thickness(4.0),
			MinWidth = 72.0,
			MinHeight = AppIconSize + 36.0,
			RenderTransformOrigin = new Point(0.5, 0.5),
			RenderTransform = new ScaleTransform(1.0, 1.0),
			Style = (Style)FindResource("LauncherAppTile"),
			Background = Brushes.Transparent,
			BorderBrush = Brushes.Transparent,
			BorderThickness = new Thickness(1.0)
		};
		AutomationProperties.SetName(button, flag ? (text2 + "，目标已失效") : text2);
		button.Click += delegate
		{
			OnTileActivated(slot);
		};
		if (!_view.IsSearching)
		{
			button.PreviewMouseLeftButtonDown += OnTileMouseDown;
			button.PreviewMouseMove += OnTileMouseMove;
		}
		button.PreviewMouseLeftButtonUp += OnTileMouseUp;
		if (num)
		{
			button.MouseEnter += delegate
			{
				button.SetFolderHovered(hovered: true);
				if (iconVisual is Grid grid && grid.Children[0] is SharedGlassLens lens)
				{
					AnimateFolderHighlight(lens, 1.0);
				}
			};
			button.MouseLeave += delegate
			{
				button.SetFolderHovered(hovered: false);
				if (iconVisual is Grid grid && grid.Children[0] is SharedGlassLens lens)
				{
					AnimateFolderHighlight(lens, 0.0);
				}
			};
		}
		button.ContextMenu = BuildTileMenu(slot);
		return button;
	}

	private void PreloadIcons()
	{
		if (_backgroundWorkSuspended || _services == null || _workspace == null)
		{
			return;
		}
		int num = IconPixelSize(AppIconSize);
		foreach (AppEntry item in (from slot in VisiblePageSlots()
			where slot.Kind != LauncherSlotKind.Group
			select _services.Library.Find(slot.ItemId)).OfType<AppEntry>().Concat(_services.Library.Items).DistinctBy((AppEntry entry) => entry.Id)
			.Take(_iconLoader.PreloadCapacity(num)))
		{
			QueueIconLoad(item, num);
		}
	}

	private Task<BitmapImage?> QueueIconLoad(AppEntry entry, int pixels)
	{
		IconCache icons = _services?.Icons;
		if (icons == null)
		{
			return Task.FromResult<BitmapImage>(null);
		}
		string source = entry.IconSource ?? string.Empty;
		string custom = entry.CustomIconPath;
		long version = _services.Library.Version;
		string key = $"{version}\0{custom}\0{source}\0{pixels}";
		return _iconLoader.LoadAsync(key, () => LoadIcon(icons, source, custom, pixels));
	}

	private void RequestIcon(AppEntry entry, Border host, double size)
	{
		long version = _services.Library.Version;
		int pixels = IconPixelSize(size);
		long generation = _iconRequestGeneration;
		Task<BitmapImage?> task = QueueIconLoad(entry, pixels);
		if (task.IsCompletedSuccessfully)
		{
			Apply(task.Result);
		}
		else
		{
			CompleteAsync();
		}
		void Apply(BitmapImage? image)
		{
			if (image != null)
			{
				AppServiceHost? services = _services;
				if (services != null && services.Library.Version == version)
				{
					if (_backgroundWorkSuspended || (generation != _iconRequestGeneration && !base.IsVisible))
					{
						_iconsNeedReapply = true;
					}
					else if (pixels == IconPixelSize(size))
					{
						host.Child = new Image
						{
							Source = image,
							Width = size,
							Height = size,
							Stretch = Stretch.Uniform,
							HorizontalAlignment = HorizontalAlignment.Center
						};
					}
				}
			}
		}
		async Task CompleteAsync()
		{
			BitmapImage image = await task.ConfigureAwait(continueOnCapturedContext: false);
			if (image == null || host.Dispatcher.HasShutdownStarted)
			{
				return;
			}
			try
			{
				await host.Dispatcher.InvokeAsync(delegate
				{
					Apply(image);
				}, DispatcherPriority.Background);
			}
			catch (TaskCanceledException)
			{
			}
		}
	}

	private static BitmapImage? LoadIcon(IconCache icons, string iconSource, string? customIcon, int pixels)
	{
		int num;
		string text;
		if (!string.IsNullOrWhiteSpace(customIcon))
		{
			num = (File.Exists(customIcon) ? 1 : 0);
			if (num != 0)
			{
				text = customIcon;
				goto IL_001e;
			}
		}
		else
		{
			num = 0;
		}
		text = icons.PathFor(iconSource);
		goto IL_001e;
		IL_001e:
		string text2 = text;
		if (num == 0 && !string.IsNullOrWhiteSpace(iconSource) && !string.IsNullOrWhiteSpace(text2) && !File.Exists(text2))
		{
			text2 = icons.EnsureFor(iconSource)?.Path ?? text2;
		}
		if (string.IsNullOrWhiteSpace(text2) || !File.Exists(text2))
		{
			return null;
		}
		try
		{
			return LauncherIconDecoder.DecodeFile(text2, pixels);
		}
		catch (Exception ex) when (((ex is IOException || ex is UriFormatException || ex is FileFormatException) ? 1 : 0) != 0)
		{
			return null;
		}
	}

	private void RenderGroup()
	{
		UpdateFolderPanel();
	}

	private void OnTileActivated(LauncherSlot slot)
	{
		if (_dragEdit == null && !_suppressTileClick && !_filterTransitionActive)
		{
			string text = ((slot.Kind != LauncherSlotKind.Group) ? slot.ItemId : slot.Group?.Id);
			if (slot.Kind == LauncherSlotKind.Group)
			{
				_view.OpenGroupId = text;
				RenderGroup();
			}
			else if (text != null)
			{
				ActivateEntry(text);
			}
		}
	}

	private void OnSearchChanged(object sender, TextChangedEventArgs e)
	{
		_view.Query = SearchBox.Text;
		_view.OpenGroupId = null;
		RenderTiles();
	}

	private void OnPreviousPage(object sender, RoutedEventArgs e)
	{
		NavigatePage(-1);
	}

	private void OnNextPage(object sender, RoutedEventArgs e)
	{
		NavigatePage(1);
	}

	private void OnCloseGroup(object sender, RoutedEventArgs e)
	{
		CloseFolder();
	}

	protected override byte ResolveAcrylicTintAlpha(GlassPalette palette)
	{
		if (!palette.IsDark)
		{
			return 66;
		}
		return 54;
	}

	protected override Geometry CreatePanelGeometryCore(Rect bounds, double radius)
	{
		return SquircleGlassGeometry.Create(bounds, radius);
	}

	protected override bool ContainsPanelGeometryCore(Rect bounds, double radius, Point point)
	{
		Rect? transitionPanelBoundsDip = _transitionPanelBoundsDip;
		if (transitionPanelBoundsDip.HasValue)
		{
			Rect valueOrDefault = transitionPanelBoundsDip.GetValueOrDefault();
			return SquircleGlassGeometry.Contains(valueOrDefault, _transitionPanelRadiusDip, point);
		}
		return SquircleGlassGeometry.Contains(bounds, radius, point);
	}

	protected override bool TryGetPanelGeometryEdgeCore(Rect bounds, double radius, Point point, out double depth, out Vector inwardNormal)
	{
		return SquircleGlassGeometry.TryGetEdge(bounds, radius, point, out depth, out inwardNormal);
	}

	public LauncherWindow()
	{
		InitializeComponent();
		base.Closed += delegate
		{
			StopFilterTransition();
			_noticeTimer?.Stop();
			CancelDrag();
			ResetFolderPresentation();
			StopPageSlide();
			_iconLoader.Dispose();
		};
		PageViewport.SizeChanged += delegate
		{
			if (_filterTransitionActive)
			{
				RenderTiles();
			}
			StopPageSlide();
		};
		base.Deactivated += OnLauncherDeactivated;
		base.IsVisibleChanged += OnVisibilityChanged;
		base.Loaded += delegate
		{
			RefreshLibrary(reuseUnchanged: true);
		};
		TextCompositionManager.AddPreviewTextInputStartHandler(SearchBox, delegate
		{
			_imeComposing = true;
			this.NavigationAvailabilityChanged?.Invoke(this, EventArgs.Empty);
		});
		TextCompositionManager.AddPreviewTextInputUpdateHandler(SearchBox, delegate
		{
			_imeComposing = true;
			this.NavigationAvailabilityChanged?.Invoke(this, EventArgs.Empty);
		});
		TextCompositionManager.AddPreviewTextInputHandler(SearchBox, delegate
		{
			_imeComposing = false;
			this.NavigationAvailabilityChanged?.Invoke(this, EventArgs.Empty);
		});
	}

	public void AttachServices(AppServiceHost services, bool libraryAvailable)
	{
		_services = services;
		_flatProjection = null;
		_filterButtonsKey = null;
		_workspace = new LauncherWorkspace(services, libraryAvailable);
		RefreshLibrary();
	}

	public void ApplySettings(LauncherSettings settings)
	{
		ArgumentNullException.ThrowIfNull(settings, "settings");
		StopFilterTransition();
		CancelDrag();
		ResetFolderPresentation();
		if (!settings.GroupModeEnabled)
		{
			_activeFilterGroupId = null;
		}
		_settings = settings;
		_view.SetGrid(Math.Clamp(settings.Rows, 2, 10), Math.Clamp(settings.Columns, 2, 12));
		PositionOnWorkingArea();
		RefreshLibrary();
		PreloadIcons();
	}

	public void PrepareToShow()
	{
		if (!_settings.RememberLastPage)
		{
			_view.MoveToPage(0);
		}
		RefreshLibrary(reuseUnchanged: true);
		_preparedForShow = base.IsLoaded && _services != null && _workspace != null;
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			if (base.IsVisible && _settings.FocusSearchOnOpen)
			{
				SearchBox.Focus();
			}
		}, DispatcherPriority.Input);
	}

	public void RefreshLibrary()
	{
		RefreshLibrary(reuseUnchanged: false);
	}

	private void RefreshLibrary(bool reuseUnchanged)
	{
		if (!base.IsLoaded || _services == null || _workspace == null)
		{
			return;
		}
		if (!reuseUnchanged)
		{
			_iconLoader.Clear();
		}
		if (reuseUnchanged && _lastTilesRenderKey == CurrentTilesRenderKey())
		{
			RenderTiles(reuseUnchanged: true);
			return;
		}
		try
		{
			_workspace.Reconcile(_view.Capacity);
		}
		catch (StorageException ex)
		{
			_notice = "启动台布局未能保存；已有应用库记录仍在。" + ex.Message;
		}
		RenderTiles(reuseUnchanged);
	}

	protected override double ResolveCornerRadius(AppearanceSettings appearance)
	{
		ArgumentNullException.ThrowIfNull(appearance, "appearance");
		return Math.Max(80.0, appearance.LauncherCornerRadius);
	}

	public void RefreshDisplayPlacement()
	{
		PositionOnWorkingArea();
	}

	public void PositionOnWorkingArea()
	{
		PrimaryDisplaySession primaryDisplaySession = base.PrimaryDisplaySession;
		if (primaryDisplaySession == null || primaryDisplaySession.IsAvailable)
		{
			PositionOnDisplay(primaryDisplaySession?.Display ?? DisplayService.Primary());
		}
	}

	public void PositionOnDisplay(DisplayInfo? display)
	{
		PrimaryDisplaySession primaryDisplaySession = base.PrimaryDisplaySession;
		if (primaryDisplaySession != null && !primaryDisplaySession.IsAvailable)
		{
			return;
		}
		display = primaryDisplaySession?.Display ?? display ?? DisplayService.Primary();
		if ((object)display == null)
		{
			return;
		}
		PixelRect pixelRect = WindowPlacementPolicy.Centered(display.WorkArea, _settings.WindowWorkAreaRatio, fullscreen: false);
		if (primaryDisplaySession != null)
		{
			pixelRect = primaryDisplaySession.ClampWindowToWorkArea(pixelRect);
		}
		double num = 96.0 / (double)Math.Max(1u, display.Dpi);
		if (_transformActive)
		{
			AbortContainerTransform();
		}
		if (_panelPixelBounds.Width > 0 && (_panelPixelBounds != pixelRect || _panelDipPerPixel != num))
		{
			_hasTransformPath = false;
			_containerMotion = null;
		}
		PixelRect transformHostBounds = _transformHostBounds;
		bool num2 = !base.IsVisible && transformHostBounds.Width > 0 && transformHostBounds != pixelRect && ContainsPixelRect(transformHostBounds, pixelRect);
		_panelPixelBounds = pixelRect;
		_panelDipPerPixel = num;
		PixelRect pixelRect2 = (_transformHostBounds = (num2 ? transformHostBounds : pixelRect));
		SetPanelRootLayout(pixelRect, pixelRect2, _panelDipPerPixel);
		if (!num2)
		{
			base.Width = display.ToDips(pixelRect2.Width);
			base.Height = display.ToDips(pixelRect2.Height);
			if (base.Handle != 0)
			{
				DisplayService.MoveWindowToPixelRect(base.Handle, pixelRect2);
				base.Width = display.ToDips(pixelRect2.Width);
				base.Height = display.ToDips(pixelRect2.Height);
				DisplayService.MoveWindowToPixelRect(base.Handle, pixelRect2);
				SetPanelRootLayout(pixelRect, pixelRect2, _panelDipPerPixel);
			}
		}
	}

	private void SetPanelRootLayout(PixelRect panelBounds, PixelRect hostBounds, double dipPerPixel)
	{
		Rect rect = new Rect((double)(panelBounds.X - hostBounds.X) * dipPerPixel, (double)(panelBounds.Y - hostBounds.Y) * dipPerPixel, (double)panelBounds.Width * dipPerPixel, (double)panelBounds.Height * dipPerPixel);
		LauncherPanelRoot.Width = rect.Width;
		LauncherPanelRoot.Height = rect.Height;
		TileGrid.Width = Math.Min(1120.0, Math.Max(1.0, rect.Width - 80.0));
		LauncherPanelRoot.Margin = new Thickness(rect.X, rect.Y, 0.0, 0.0);
		LauncherPanelRoot.RenderTransform = Transform.Identity;
		_transitionPanelBoundsDip = rect;
		_transitionPanelRadiusDip = base.PanelCornerRadius.TopLeft;
		Geometry geometry = SquircleGlassGeometry.Create(rect, _transitionPanelRadiusDip);
		GlassSurface.SetPanelShape(rect, _transitionPanelRadiusDip, geometry);
		SetSharedPanelGeometry(geometry);
		ContainerTransformClipRoot.Clip = geometry;
		SetTransitionShadowShape(rect, _transitionPanelRadiusDip);
	}

	private static bool ContainsPixelRect(PixelRect outer, PixelRect inner)
	{
		if (outer.X <= inner.X && outer.Y <= inner.Y && outer.Right >= inner.Right)
		{
			return outer.Bottom >= inner.Bottom;
		}
		return false;
	}

	private void OnLauncherDeactivated(object? sender, EventArgs e)
	{
		if (_keepOpenWhileModalCount > 0 || _dragEdit != null)
		{
			return;
		}
		base.Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)delegate
		{
			if (base.IsVisible && !base.IsActive && _keepOpenWhileModalCount <= 0 && _openContextMenuCount <= 0 && _dragEdit == null && !Application.Current.Windows.OfType<Window>().Any((Window window) => window != this && window.Owner == this && window.IsVisible))
			{
				CloseWithTransition();
			}
		});
	}

	public void RaiseAboveDockAfterDrag()
	{
		WindowStyles.BringToTopWithoutActivation(base.Handle);
	}

	private ContextMenu CreateTrackedContextMenu()
	{
		ContextMenu contextMenu = new ContextMenu();
		contextMenu.Opened += delegate
		{
			_openContextMenuCount++;
		};
		contextMenu.Closed += delegate
		{
			_openContextMenuCount = Math.Max(0, _openContextMenuCount - 1);
			base.Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)delegate
			{
				if (base.IsVisible && !base.IsActive && _openContextMenuCount == 0 && _keepOpenWhileModalCount == 0)
				{
					CloseWithTransition();
				}
			});
		};
		return contextMenu;
	}

	private bool ShowModalDialog(Func<bool?> showDialog)
	{
		if (_filterTransitionActive)
		{
			RenderTiles();
		}
		_keepOpenWhileModalCount++;
		this.NavigationAvailabilityChanged?.Invoke(this, EventArgs.Empty);
		try
		{
			return showDialog() == true;
		}
		finally
		{
			_keepOpenWhileModalCount = Math.Max(0, _keepOpenWhileModalCount - 1);
			this.NavigationAvailabilityChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public void NavigatePage(int delta)
	{
		if (CanNavigatePages && delta != 0)
		{
			if (_dragOrigin != null)
			{
				NavigateDragPage(Math.Sign(delta));
				return;
			}
			CancelDrag();
			ResetFolderPresentation();
			SlideToPage(Math.Sign(delta));
		}
	}

	public void RefreshDiagnostics()
	{
		if (_notice == null)
		{
			StatusText.Visibility = Visibility.Collapsed;
			return;
		}
		StatusText.Text = _notice;
		StatusText.Visibility = Visibility.Visible;
	}

	protected override void OnSourceInitialized(EventArgs e)
	{
		base.OnSourceInitialized(e);
		PositionOnWorkingArea();
		RefreshDiagnostics();
	}

	private void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		this.NavigationAvailabilityChanged?.Invoke(this, EventArgs.Empty);
		object newValue = e.NewValue;
		if (newValue is bool && !(bool)newValue)
		{
			StopFilterTransition();
			StopPageSlide();
			_preparedForShow = false;
			AbortContainerTransform();
			CancelDrag();
			ResetFolderPresentation();
			return;
		}
		if (!_preparedForShow)
		{
			RefreshLibrary();
		}
		_preparedForShow = false;
		if (_settings.FocusSearchOnOpen)
		{
			base.Dispatcher.BeginInvoke((Func<bool>)(() => SearchBox.Focus()), DispatcherPriority.Input);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.12.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/GlassDesk.Launcher;V0.2.2.0;component/launcherwindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.12.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((LauncherWindow)target).Drop += OnExternalDrop;
			((LauncherWindow)target).DragLeave += OnLauncherDragLeave;
			((LauncherWindow)target).DragOver += OnExternalDragOver;
			break;
		case 2:
			ContainerTransformClipRoot = (Border)target;
			break;
		case 3:
			LauncherGuideRoot = (Grid)target;
			break;
		case 4:
			GlassSurface = (LiquidGlassSurface)target;
			break;
		case 5:
			LauncherPanelRoot = (Grid)target;
			break;
		case 6:
			LauncherContent = (Grid)target;
			break;
		case 7:
			ContentScaleTransform = (ScaleTransform)target;
			break;
		case 8:
			SearchHeader = (DockPanel)target;
			break;
		case 9:
			AddAppButton = (Button)target;
			AddAppButton.Click += OnAddClick;
			break;
		case 10:
			SearchBox = (TextBox)target;
			SearchBox.TextChanged += OnSearchChanged;
			break;
		case 11:
			FilterGroupBar = (ScrollViewer)target;
			break;
		case 12:
			FilterGroupButtons = (StackPanel)target;
			break;
		case 13:
			PageViewport = (Grid)target;
			break;
		case 14:
			OutgoingPageScroll = (ScrollViewer)target;
			break;
		case 15:
			OutgoingPageGrid = (UniformGrid)target;
			break;
		case 16:
			OutgoingPageMotion = (TranslateTransform)target;
			break;
		case 17:
			PageScroll = (ScrollViewer)target;
			break;
		case 18:
			TileGrid = (UniformGrid)target;
			break;
		case 19:
			IncomingPageMotion = (TranslateTransform)target;
			break;
		case 20:
			GroupPanel = (Grid)target;
			break;
		case 21:
			FolderDismissArea = (Border)target;
			FolderDismissArea.MouseLeftButtonDown += OnFolderOutsideDown;
			break;
		case 22:
			FolderCanvas = (Canvas)target;
			break;
		case 23:
			FolderShadow = (System.Windows.Shapes.Path)target;
			break;
		case 24:
			FolderCard = (Grid)target;
			break;
		case 25:
			FolderGlass = (SharedGlassLens)target;
			break;
		case 26:
			GroupContent = (DockPanel)target;
			break;
		case 27:
			((Button)target).Click += OnCloseGroup;
			break;
		case 28:
			((Button)target).Click += OnRenameOpenFolder;
			break;
		case 29:
			GroupTitle = (TextBlock)target;
			break;
		case 30:
			FolderHint = (TextBlock)target;
			break;
		case 31:
			GroupScroll = (ScrollViewer)target;
			break;
		case 32:
			GroupItems = (UniformGrid)target;
			break;
		case 33:
			TrashDropTarget = (PhoneBorder)target;
			TrashDropTarget.DragOver += OnTrashDragOver;
			TrashDropTarget.Drop += OnTrashDrop;
			break;
		case 34:
			PageControls = (StackPanel)target;
			break;
		case 35:
			PreviousPage = (Button)target;
			PreviousPage.Click += OnPreviousPage;
			break;
		case 36:
			PageText = (TextBlock)target;
			break;
		case 37:
			NextPage = (Button)target;
			NextPage.Click += OnNextPage;
			break;
		case 38:
			StatusText = (TextBlock)target;
			break;
		case 39:
			CollapsedGlyph = (LaunchpadGlyph)target;
			break;
		case 40:
			DragOverlay = (Canvas)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
