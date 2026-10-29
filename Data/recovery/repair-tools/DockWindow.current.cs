using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
using GlassDesk.UI.Settings;
using GlassDesk.UI.Theming;
using Microsoft.Win32;

namespace GlassDesk.Dock;

public class DockWindow : GlassWindow, IComponentConnector
{
	private DockRenderLoop? _entryHoverLoop;

	private readonly ScaleTransform _launcherHoverScale = new ScaleTransform(1.0, 1.0);

	private readonly ScaleTransform _thisPcHoverScale = new ScaleTransform(1.0, 1.0);

	private readonly ScaleTransform _desktopIconsHoverScale = new ScaleTransform(1.0, 1.0);

	private bool _launcherHovered;

	private bool _thisPcHovered;

	private bool _desktopIconsHovered;

	private bool _performanceSleepRequested;

	private bool _performanceSleeping;

	private bool _showAfterPerformanceSleep;

	private bool _tilesDirty;

	private bool _lastPerformanceSurfaceVisible;

	private bool _lastPerformanceCaptureRequested;

	private bool _backgroundCaptureSleeping;

	private static readonly TimeSpan FailureNoticeDuration = TimeSpan.FromSeconds(5L);

	private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(16L);

	private AppServiceHost? _services;

	private DockSettingsHost? _dockSettings;

	private Action<PixelRect?>? _toggleLauncher;

	private DispatcherTimer? _failureNoticeTimer;

	private readonly DockTileStrip _tileStrip;

	private readonly List<DockTileModel> _models = new List<DockTileModel>();

	private AutoHideController? _autoHide;

	private DispatcherTimer? _frameTimer;

	private PixelRect _restingRect;

	private int _fullyHiddenOffsetPixels;

	private bool _revealLocked;

	private bool _edgeRevealedDuringSuppression;

	private bool _launcherContainerActive;

	private Rect? _slidePanelBoundsDip;

	private bool _backdropWasActive;

	private bool _panelWasConcealed;

	private long _revealPreparationStarted;

	private double _slideOffsetPixels;

	private bool _launcherWasOpenOnPress;

	private bool _launcherReleaseClickPending;

	private int _launcherPressGeneration;

	internal LiquidGlassSurface GlassSurface;

	internal Grid DockVisualRoot;

	internal TranslateTransform DockSlideTransform;

	internal StackPanel DockContentLayer;

	internal Button LauncherButton;

	internal PhoneBorder LaunchpadTile;

	internal LaunchpadGlyph LauncherDefaultGlyph;

	internal Image LauncherCustomIcon;

	internal Button ThisPcButton;

	internal Viewbox ThisPcTile;

	internal Canvas ThisPcDefaultGlyph;

	internal Image ThisPcCustomIcon;

	internal Button DesktopIconsButton;

	internal Viewbox DesktopIconsTile;

	internal Canvas DesktopIconsDefaultGlyph;

	internal Image DesktopIconsCustomIcon;

	internal ContentControl TileStripHost;

	internal TextBlock StatusText;

	internal Canvas DockDragOverlay;

	private bool _contentLoaded;

	public bool IsPerformanceSleeping => _performanceSleeping;

	public bool IsPerformanceSurfaceVisible
	{
		get
		{
			if (base.IsVisible && IsOpticalSurfaceVisible)
			{
				return !_performanceSleeping;
			}
			return false;
		}
	}

	public bool IsPerformanceCaptureRequested
	{
		get
		{
			bool flag = base.IsVisible && !_performanceSleepRequested;
			bool flag2;
			bool flag3;
			if (flag)
			{
				flag2 = IsPerformanceSurfaceVisible;
				if (!flag2)
				{
					AutoHideState? autoHideState = _autoHide?.State;
					if (autoHideState.HasValue)
					{
						AutoHideState valueOrDefault = autoHideState.GetValueOrDefault();
						if ((uint)valueOrDefault <= 1u)
						{
							flag3 = true;
							goto IL_0060;
						}
					}
					flag3 = false;
					goto IL_0060;
				}
				goto IL_0063;
			}
			goto IL_0065;
			IL_0065:
			return flag;
			IL_0063:
			flag = flag2;
			goto IL_0065;
			IL_0060:
			flag2 = flag3;
			goto IL_0063;
		}
	}

	protected override bool SupportsDesktopLayer => true;

	public Action? ToggleDesktopIcons { get; set; }

	internal bool IsOpticalSurfaceVisible => _slideOffsetPixels < (double)Math.Max(1, _restingRect.Height - 4);

	internal PixelRect OpticalBackdropBounds => _restingRect;

	private DisplayInfo? CurrentPrimaryDisplay
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

	protected override bool UsesSquirclePanelGeometryCore => true;

	public DockSettings Settings => _dockSettings?.Settings ?? new DockSettings();

	public int TileCount => _models.Count;

	private double DockScale
	{
		get
		{
			if (!double.IsFinite(Settings.SizeScale))
			{
				return 1.0;
			}
			return Math.Clamp(Settings.SizeScale, 2.0 / 3.0, 1.5);
		}
	}

	private double BuiltinEntryWidthDip => 56 * ((Settings.ShowLauncherEntry ? 1 : 0) + (Settings.ShowThisPcEntry ? 1 : 0) + (Settings.ShowDesktopIconsEntry ? 1 : 0));

	public bool SuppressPassiveTriggersWhenFullscreen { get; set; } = true;

	public event EventHandler? PerformanceSurfaceVisibilityChanged;

	private void RefreshBuiltinIcons()
	{
		(string, Image, FrameworkElement)[] array = new(string, Image, FrameworkElement)[3]
		{
			("entry:launcher", LauncherCustomIcon, LauncherDefaultGlyph),
			("entry:this-pc", ThisPcCustomIcon, ThisPcDefaultGlyph),
			("entry:desktop-icons", DesktopIconsCustomIcon, DesktopIconsDefaultGlyph)
		};
		for (int i = 0; i < array.Length; i++)
		{
			(string, Image, FrameworkElement) tuple = array[i];
			string item = tuple.Item1;
			Image item2 = tuple.Item2;
			FrameworkElement item3 = tuple.Item3;
			string text = _services?.DockIcons.GetIconPath(item);
			item2.Source = ((text != null && File.Exists(text)) ? LoadBitmap(text) : null);
			item2.Visibility = ((item2.Source == null) ? Visibility.Collapsed : Visibility.Visible);
			item3.Visibility = ((item2.Source != null) ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	private void AddIconMenuItems(ContextMenu menu, string id)
	{
		menu.Items.Add(MenuItem("自定义图标…", delegate
		{
			ChooseCustomIcon(id);
		}));
		MenuItem menuItem = MenuItem("恢复默认图标", delegate
		{
			ApplyCustomIcon(id, null);
		});
		menuItem.IsEnabled = !string.IsNullOrWhiteSpace(_services?.DockIcons.GetIconPath(id));
		menu.Items.Add(menuItem);
	}

	private void OnBuiltinIconContextMenu(object sender, MouseButtonEventArgs e)
	{
		if (_services != null && sender is Button { Tag: string tag } button)
		{
			ContextMenu contextMenu = new ContextMenu
			{
				Placement = PlacementMode.MousePoint
			};
			AddIconMenuItems(contextMenu, tag);
			button.ContextMenu = contextMenu;
			e.Handled = true;
			OpenDockContextMenu(contextMenu);
		}
	}

	private void OpenDockContextMenu(ContextMenu menu)
	{
		_autoHide?.PushSuppress(DateTime.UtcNow);
		nint foregroundBefore = WindowControl.GetForegroundWindow();
		nint menuHandle = 0;
		menu.Opened += delegate
		{
			menuHandle = (PresentationSource.FromVisual(menu) as HwndSource)?.Handle ?? 0;
		};
		menu.Closed += delegate
		{
			_autoHide?.PopSuppress(DateTime.UtcNow);
			RestorePopupForeground(foregroundBefore, menuHandle);
		};
		WindowActivation.TryBringToForeground(base.Handle);
		menu.IsOpen = true;
	}

	private void ChooseCustomIcon(string id)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Title = "选择自定义图标",
			Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.ico|所有文件|*.*",
			CheckFileExists = true
		};
		_autoHide?.PushSuppress(DateTime.UtcNow);
		try
		{
			if (openFileDialog.ShowDialog(this) == true)
			{
				if (LoadBitmap(openFileDialog.FileName) == null)
				{
					ShowFailureNotice("无法读取这张图片，请选择有效的图片文件。");
				}
				else
				{
					ApplyCustomIcon(id, openFileDialog.FileName);
				}
			}
		}
		finally
		{
			_autoHide?.PopSuppress(DateTime.UtcNow);
		}
	}

	private void ApplyCustomIcon(string id, string? path)
	{
		AppServiceHost? services = _services;
		if (services == null || !services.DockIcons.SetIcon(id, path))
		{
			ShowFailureNotice("图标保存失败，已保留原图标。");
		}
		RefreshTiles();
	}

	private void InitializeEntryHover()
	{
		PhoneBorder launchpadTile = LaunchpadTile;
		Viewbox thisPcTile = ThisPcTile;
		Point point = (DesktopIconsTile.RenderTransformOrigin = new Point(0.5, 0.5));
		Point renderTransformOrigin = (thisPcTile.RenderTransformOrigin = point);
		launchpadTile.RenderTransformOrigin = renderTransformOrigin;
		LaunchpadTile.RenderTransform = _launcherHoverScale;
		ThisPcTile.RenderTransform = _thisPcHoverScale;
		DesktopIconsTile.RenderTransform = _desktopIconsHoverScale;
		_entryHoverLoop = new DockRenderLoop(AnimateEntryHoverFrame);
		base.IsVisibleChanged += delegate(object _, DependencyPropertyChangedEventArgs e)
		{
			object newValue = e.NewValue;
			if (newValue is bool && !(bool)newValue)
			{
				ResetEntryHover();
			}
		};
		base.Closed += delegate
		{
			ResetEntryHover();
		};
	}

	private void OnEntryMouseEnter(object sender, MouseEventArgs e)
	{
		SetEntryHovered(sender, hovered: true);
	}

	private void OnEntryMouseLeave(object sender, MouseEventArgs e)
	{
		SetEntryHovered(sender, hovered: false);
	}

	private void SetEntryHovered(object sender, bool hovered)
	{
		if (sender == LauncherButton)
		{
			_launcherHovered = hovered;
		}
		else if (sender == ThisPcButton)
		{
			_thisPcHovered = hovered;
		}
		else if (sender == DesktopIconsButton)
		{
			_desktopIconsHovered = hovered;
		}
		if (base.IsVisible && !_performanceSleepRequested)
		{
			_entryHoverLoop?.Start();
		}
	}

	private bool AnimateEntryHoverFrame(double deltaSeconds)
	{
		double num = (_launcherHovered ? 1.38 : 1.0);
		double num2 = (_thisPcHovered ? 1.38 : 1.0);
		double num3 = (_desktopIconsHovered ? 1.38 : 1.0);
		ThemeService? current = ThemeService.Current;
		double scaleX;
		if (current != null && !current.AnimationsEnabled)
		{
			ScaleTransform launcherHoverScale = _launcherHoverScale;
			scaleX = (_launcherHoverScale.ScaleY = num);
			launcherHoverScale.ScaleX = scaleX;
			ScaleTransform thisPcHoverScale = _thisPcHoverScale;
			scaleX = (_thisPcHoverScale.ScaleY = num2);
			thisPcHoverScale.ScaleX = scaleX;
			ScaleTransform desktopIconsHoverScale = _desktopIconsHoverScale;
			scaleX = (_desktopIconsHoverScale.ScaleY = num3);
			desktopIconsHoverScale.ScaleX = scaleX;
			return true;
		}
		double num7 = DockHoverScale.Advance(_launcherHoverScale.ScaleX, num, deltaSeconds);
		double num8 = DockHoverScale.Advance(_thisPcHoverScale.ScaleX, num2, deltaSeconds);
		double num9 = DockHoverScale.Advance(_desktopIconsHoverScale.ScaleX, num3, deltaSeconds);
		ScaleTransform launcherHoverScale2 = _launcherHoverScale;
		scaleX = (_launcherHoverScale.ScaleY = num7);
		launcherHoverScale2.ScaleX = scaleX;
		ScaleTransform thisPcHoverScale2 = _thisPcHoverScale;
		scaleX = (_thisPcHoverScale.ScaleY = num8);
		thisPcHoverScale2.ScaleX = scaleX;
		ScaleTransform desktopIconsHoverScale2 = _desktopIconsHoverScale;
		scaleX = (_desktopIconsHoverScale.ScaleY = num9);
		desktopIconsHoverScale2.ScaleX = scaleX;
		if (num7 == num && num8 == num2)
		{
			return num9 == num3;
		}
		return false;
	}

	private void ResetEntryHover()
	{
		_entryHoverLoop?.Stop();
		_launcherHovered = (_thisPcHovered = (_desktopIconsHovered = false));
		ScaleTransform launcherHoverScale = _launcherHoverScale;
		double scaleX = (_launcherHoverScale.ScaleY = 1.0);
		launcherHoverScale.ScaleX = scaleX;
		ScaleTransform thisPcHoverScale = _thisPcHoverScale;
		scaleX = (_thisPcHoverScale.ScaleY = 1.0);
		thisPcHoverScale.ScaleX = scaleX;
		ScaleTransform desktopIconsHoverScale = _desktopIconsHoverScale;
		scaleX = (_desktopIconsHoverScale.ScaleY = 1.0);
		desktopIconsHoverScale.ScaleX = scaleX;
	}

	public void SetBackgroundCaptureSleeping(bool sleeping)
	{
		base.Dispatcher.VerifyAccess();
		if (_backgroundCaptureSleeping == sleeping)
		{
			return;
		}
		_backgroundCaptureSleeping = sleeping;
		GlassSurface.SetPerformanceSleeping(sleeping || _performanceSleepRequested);
		if (!sleeping && !_performanceSleepRequested)
		{
			PrepareConcealedReveal();
		}
		if (sleeping && !_performanceSleepRequested)
		{
			if (_frameTimer == null)
			{
				RebuildAutoHide();
			}
			_frameTimer.Interval = TimeSpan.FromMilliseconds(100L);
			_frameTimer.Start();
		}
	}

	private void PrepareConcealedReveal()
	{
		if (_restingRect.Width > 0 && _panelWasConcealed)
		{
			DisplayInfo currentPrimaryDisplay = CurrentPrimaryDisplay;
			if ((object)currentPrimaryDisplay != null)
			{
				GlassSurface.PrepareRevealMaterial(_restingRect, _restingRect, currentPrimaryDisplay.Dpi);
			}
		}
	}

	public void SetPerformanceSleeping(bool sleeping)
	{
		base.Dispatcher.VerifyAccess();
		if (_performanceSleepRequested == sleeping)
		{
			return;
		}
		_performanceSleepRequested = sleeping;
		if (sleeping)
		{
			_showAfterPerformanceSleep = base.IsVisible;
			ResetEntryHover();
			_failureNoticeTimer?.Stop();
			if (base.IsVisible && _services != null && _restingRect.Width != 0)
			{
				ThemeService? current = ThemeService.Current;
				if (current == null || current.AnimationsEnabled)
				{
					_frameTimer.Interval = FrameInterval;
					_frameTimer.Start();
					OnAutoHideFrame();
					return;
				}
			}
			FinishPerformanceSleep();
			return;
		}
		double num = (_performanceSleeping ? ((double)_fullyHiddenOffsetPixels) : _slideOffsetPixels);
		_performanceSleeping = false;
		GlassSurface.SetPerformanceSleeping(_backgroundCaptureSleeping);
		RebuildAutoHide();
		_slideOffsetPixels = num;
		if (_tilesDirty)
		{
			_tilesDirty = false;
			RefreshTiles();
		}
		_slideOffsetPixels = num;
		RefreshDisplayPlacement();
		if (num > 0.0)
		{
			_slideOffsetPixels = num;
			ApplySlide(_restingRect, _slideOffsetPixels);
			PrepareConcealedReveal();
		}
		if (_showAfterPerformanceSleep && (object)CurrentPrimaryDisplay != null)
		{
			Show();
		}
		NotifyPerformanceSurfaceVisibility();
	}

	private void AdvancePerformanceSleep()
	{
		if (!_performanceSleeping)
		{
			_autoHide?.ForceHide(DateTime.UtcNow);
			double num = _fullyHiddenOffsetPixels;
			ThemeService? current = ThemeService.Current;
			if (current != null && !current.AnimationsEnabled)
			{
				_slideOffsetPixels = num;
			}
			else
			{
				_slideOffsetPixels += (num - _slideOffsetPixels) * 0.25;
			}
			if (Math.Abs(_slideOffsetPixels - num) <= 0.5)
			{
				_slideOffsetPixels = num;
			}
			ApplySlide(_restingRect, _slideOffsetPixels);
			NotifyPerformanceSurfaceVisibility();
			if (_slideOffsetPixels == num)
			{
				FinishPerformanceSleep();
			}
		}
	}

	private void FinishPerformanceSleep()
	{
		_slideOffsetPixels = _fullyHiddenOffsetPixels;
		_performanceSleeping = true;
		_frameTimer?.Stop();
		GlassSurface.SetPerformanceSleeping(sleeping: true);
		if (base.IsVisible)
		{
			Hide();
		}
		NotifyPerformanceSurfaceVisibility();
	}

	private void NotifyPerformanceSurfaceVisibility()
	{
		bool isPerformanceSurfaceVisible = IsPerformanceSurfaceVisible;
		bool isPerformanceCaptureRequested = IsPerformanceCaptureRequested;
		if (_lastPerformanceSurfaceVisible != isPerformanceSurfaceVisible || _lastPerformanceCaptureRequested != isPerformanceCaptureRequested)
		{
			_lastPerformanceSurfaceVisible = isPerformanceSurfaceVisible;
			_lastPerformanceCaptureRequested = isPerformanceCaptureRequested;
			this.PerformanceSurfaceVisibilityChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public void SetLauncherContainerState(bool expanded)
	{
		_launcherContainerActive = expanded;
		if (expanded && !_performanceSleepRequested && _restingRect.Width > 0)
		{
			_autoHide?.ForceShown(DateTime.UtcNow);
			_slideOffsetPixels = 0.0;
			ApplySlide(_restingRect, 0.0);
		}
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
		return SquircleGlassGeometry.CreateCompact(bounds, radius);
	}

	protected override bool ContainsPanelGeometryCore(Rect bounds, double radius, Point point)
	{
		return SquircleGlassGeometry.Contains(_slidePanelBoundsDip ?? bounds, radius, point);
	}

	protected override bool TryGetPanelGeometryEdgeCore(Rect bounds, double radius, Point point, out double depth, out Vector inwardNormal)
	{
		return SquircleGlassGeometry.TryGetEdge(_slidePanelBoundsDip ?? bounds, radius, point, out depth, out inwardNormal);
	}

	public DockWindow()
	{
		InitializeComponent();
		InitializeEntryHover();
		base.PanelCornerRadius = new CornerRadius(40.0);
		GlassSurface.IsBackdropActive = () => IsOpticalSurfaceVisible;
		_tileStrip = new DockTileStrip();
		_tileStrip.DragOverlay = DockDragOverlay;
		_tileStrip.TileClicked += OnTileClicked;
		_tileStrip.TileContextMenuRequested += OnTileContextMenuRequested;
		_tileStrip.ReorderFinished += delegate
		{
			RefreshTiles();
		};
		_tileStrip.UnpinRequested += OnUnpinRequested;
		_tileStrip.SetReorderModel(new DockReorderModel(SaveDockOrder));
		TileStripHost.Content = _tileStrip;
		RefreshTiles();
	}

	private void SaveDockOrder(IReadOnlyList<string> order)
	{
		if (_services != null)
		{
			List<string> pinnedItemIds = DockLayoutPolicy.Sanitize(order, (string id) => _services.Library.Find(id) != null);
			_services.Layouts.Dock.PinnedItemIds = pinnedItemIds;
			_services.Layouts.SaveDock();
		}
	}

	public void SanitizePinnedLayout()
	{
		if (_services == null || _services.Library.Items.Count == 0)
		{
			return;
		}
		List<string> pinnedItemIds = _services.Layouts.Dock.PinnedItemIds;
		List<string> list = DockLayoutPolicy.Sanitize(pinnedItemIds, (string id) => _services.Library.Find(id) != null);
		if (!DockLayoutPolicy.SameOrder(pinnedItemIds, list))
		{
			int value = pinnedItemIds.Count - list.Count;
			_services.Layouts.Dock.PinnedItemIds = list;
			try
			{
				_services.Layouts.SaveDock();
			}
			catch (StorageException)
			{
				AppendLayoutNotice("清理旧固定项时无法保存；磁盘布局已保留，启动继续。请检查数据目录的写入权限。");
				return;
			}
			AppendLayoutNotice($"布局文件里清理了 {value} 个不属于应用库的固定项（内置入口或已失效的条目）。");
		}
	}

	private void OnUnpinRequested(object? sender, string entryId)
	{
		if (_services != null && !DockLayoutPolicy.IsEntryTile(entryId) && _services.Layouts.UnpinFromDock(entryId))
		{
			try
			{
				_services.Layouts.SaveDock();
			}
			catch (StorageException)
			{
				ShowFailureNotice("未能保存取消固定操作，原固定项已恢复。");
			}
			RefreshTiles();
		}
	}

	private void AppendLayoutNotice(string message)
	{
		if (_services == null)
		{
			return;
		}
		try
		{
			_services.Paths.EnsureCreated();
			File.AppendAllText(Path.Combine(_services.Paths.LogDirectory, "startup.log"), $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}] Dock 布局：{message}{Environment.NewLine}");
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
		}
	}

	private void OnExternalDragOver(object sender, DragEventArgs e)
	{
		if (e.Data.GetDataPresent("GlassDesk.Launcher.Slot") && e.Data.GetData("GlassDesk.Launcher.Slot") is string id)
		{
			AppEntry appEntry = _services?.Library.Find(id);
			e.Effects = ((appEntry != null && appEntry.IsUsable) ? DragDropEffects.Copy : DragDropEffects.None);
			e.Handled = true;
		}
		else
		{
			e.Effects = (e.Data.GetDataPresent(DataFormats.FileDrop, autoConvert: true) ? DragDropEffects.Copy : DragDropEffects.None);
			e.Handled = true;
		}
	}

	private void OnExternalDrop(object sender, DragEventArgs e)
	{
		if (_services == null)
		{
			return;
		}
		if (e.Data.GetDataPresent("GlassDesk.Launcher.Slot") && e.Data.GetData("GlassDesk.Launcher.Slot") is string itemId)
		{
			e.Handled = true;
			PinLauncherItem(itemId, e);
		}
		else
		{
			if (!e.Data.GetDataPresent(DataFormats.FileDrop, autoConvert: true))
			{
				return;
			}
			e.Handled = true;
			if (!(e.Data.GetData(DataFormats.FileDrop, autoConvert: true) is string[] array) || array.Length <= 0)
			{
				return;
			}
			_autoHide?.PushSuppress(DateTime.UtcNow);
			try
			{
				List<ImportRequest> requests = array.Select((string source) => new ImportRequest
				{
					Source = source
				}).ToList();
				ImportResult importResult = _services.Import.ImportItems(requests);
				List<string> pinnedItemIds = _services.Layouts.Dock.PinnedItemIds;
				List<string> list = DockPinMerge.Merge(pinnedItemIds, importResult.Added.Select((AppEntry entry) => entry.Id), importResult.Duplicates.Select((AppEntry entry) => entry.Id));
				int num = list.Count - pinnedItemIds.Count;
				if (num > 0)
				{
					_services.Layouts.Dock.PinnedItemIds = list;
					try
					{
						_services.Layouts.SaveDock();
					}
					catch (StorageException)
					{
						ShowFailureNotice("已导入应用库，但未能保存 Dock 固定；原固定顺序已恢复。");
						RefreshTiles();
						return;
					}
				}
				ShowTemporaryNotice((importResult.Failures.Count == 0) ? $"已固定 {num} 项（新增 {importResult.Added.Count}，已在库 {importResult.Duplicates.Count}）。" : $"已固定 {num} 项，{importResult.Failures.Count} 项失败。");
				RefreshTiles();
			}
			finally
			{
				_autoHide?.PopSuppress(DateTime.UtcNow);
			}
		}
	}

	private void PinLauncherItem(string itemId, DragEventArgs e)
	{
		AppEntry appEntry = _services?.Library.Find(itemId);
		if (appEntry == null || !appEntry.IsUsable)
		{
			e.Effects = DragDropEffects.None;
			return;
		}
		List<string> pinnedItemIds = _services.Layouts.Dock.PinnedItemIds;
		List<string> list = DockPinMerge.Merge(pinnedItemIds, new global::<>z__ReadOnlySingleElementList<string>(appEntry.Id), Array.Empty<string>());
		if (list.Count == pinnedItemIds.Count)
		{
			ShowTemporaryNotice("此应用已在 Dock 中。");
			e.Effects = DragDropEffects.Copy;
			RefreshTiles();
			return;
		}
		_services.Layouts.Dock.PinnedItemIds = list;
		try
		{
			_services.Layouts.SaveDock();
			ShowTemporaryNotice("已添加到 Dock。");
			e.Effects = DragDropEffects.Copy;
			RefreshTiles();
		}
		catch (StorageException)
		{
			ShowFailureNotice("未能保存 Dock 布局，原有图标顺序已恢复。");
			e.Effects = DragDropEffects.None;
			RefreshTiles();
		}
	}

	public void AttachServices(AppServiceHost services, DockSettingsHost settings, Action toggleLauncher)
	{
		ArgumentNullException.ThrowIfNull(toggleLauncher, "toggleLauncher");
		AttachServices(services, settings, (Action<PixelRect?>)delegate
		{
			toggleLauncher();
		});
	}

	public void AttachServices(AppServiceHost services, DockSettingsHost settings, Action<PixelRect?> toggleLauncher)
	{
		ArgumentNullException.ThrowIfNull(services, "services");
		ArgumentNullException.ThrowIfNull(settings, "settings");
		ArgumentNullException.ThrowIfNull(toggleLauncher, "toggleLauncher");
		if (_dockSettings != null)
		{
			_dockSettings.Changed -= OnDockSettingsChanged;
		}
		if (_services != null)
		{
			_services.Running.Changed -= OnRunningChanged;
		}
		_services = services;
		_dockSettings = settings;
		_toggleLauncher = toggleLauncher;
		_dockSettings.Changed += OnDockSettingsChanged;
		_services.Running.Changed += OnRunningChanged;
		SanitizePinnedLayout();
		RebuildAutoHide();
		RefreshTiles();
		RefreshFromSettings();
	}

	public void RaiseAboveLauncherDuringDrag()
	{
		if (!base.IsFixedDesktopLayer)
		{
			WindowStyles.BringToTopWithoutActivation(base.Handle);
		}
	}

	protected override double ResolveCornerRadius(AppearanceSettings appearance)
	{
		ArgumentNullException.ThrowIfNull(appearance, "appearance");
		return Math.Max(40.0, appearance.DockCornerRadius) * DockScale;
	}

	public void RefreshTiles()
	{
		if (_performanceSleepRequested)
		{
			_tilesDirty = true;
			return;
		}
		RefreshBuiltinIcons();
		DockSettings settings = Settings;
		RunningAppTracker tracker = _services?.Running;
		SolidColorBrush dotBrush = ThemeService.Current?.Palette.ForegroundBrush;
		_models.Clear();
		if (_services != null)
		{
			foreach (string pinnedItemId in _services.Layouts.Dock.PinnedItemIds)
			{
				AppEntry appEntry = _services.Library.Find(pinnedItemId);
				if (appEntry != null && appEntry.IsUsable)
				{
					_models.Add(CreateTile(appEntry, settings, tracker, dotBrush));
				}
			}
		}
		_tileStrip.SetTiles(_models);
		ApplyStripMetrics();
	}

	public void RequestGlassRefresh()
	{
		if (base.IsVisible && IsOpticalSurfaceVisible)
		{
			GlassSurface.RequestBackdropRefresh();
		}
	}

	private DockTileModel CreateTile(AppEntry entry, DockSettings settings, RunningAppTracker tracker, Brush? dotBrush)
	{
		string text = entry.DisplayName;
		if (entry.IsTargetMissing)
		{
			text += "（目标已不在原处，可重新定位）";
		}
		DockTileModel obj = new DockTileModel
		{
			EntryId = entry.Id,
			DisplayName = entry.DisplayName,
			ToolTipText = text,
			Icon = LoadTileIcon(entry),
			DotBrush = dotBrush,
			IconSize = 48.0 * DockScale,
			TileMargin = new Thickness(8.0 * DockScale / 2.0, 0.0, 8.0 * DockScale / 2.0, 0.0),
			IsMissing = entry.IsTargetMissing
		};
		DockTileModel.ApplyRunningIndicatorSetting(obj, (!tracker.IsRunning(entry)) ? Visibility.Collapsed : Visibility.Visible, settings.ShowRunningIndicator);
		return obj;
	}

	private void ApplyStripMetrics()
	{
		DockSettings settings = Settings;
		DisplayInfo currentPrimaryDisplay = CurrentPrimaryDisplay;
		double dockScale = DockScale;
		DockContentLayer.Margin = new Thickness(10.0 * dockScale, 8.0 * dockScale, 10.0 * dockScale, 8.0 * dockScale);
		if (LauncherButton.Visibility == Visibility.Visible != settings.ShowLauncherEntry || ThisPcButton.Visibility == Visibility.Visible != settings.ShowThisPcEntry || DesktopIconsButton.Visibility == Visibility.Visible != settings.ShowDesktopIconsEntry)
		{
			ResetEntryHover();
		}
		LauncherButton.Visibility = ((!settings.ShowLauncherEntry) ? Visibility.Collapsed : Visibility.Visible);
		ThisPcButton.Visibility = ((!settings.ShowThisPcEntry) ? Visibility.Collapsed : Visibility.Visible);
		DesktopIconsButton.Visibility = ((!settings.ShowDesktopIconsEntry) ? Visibility.Collapsed : Visibility.Visible);
		RefreshBuiltinIcons();
		Button[] array = new Button[3] { LauncherButton, ThisPcButton, DesktopIconsButton };
		foreach (Button obj in array)
		{
			obj.Width = 48.0 * dockScale;
			obj.Height = 64.0 * dockScale;
			obj.Margin = new Thickness(0.0, 0.0, 8.0 * dockScale, 0.0);
		}
		PhoneBorder launchpadTile = LaunchpadTile;
		PhoneBorder launchpadTile2 = LaunchpadTile;
		Viewbox thisPcTile = ThisPcTile;
		Viewbox thisPcTile2 = ThisPcTile;
		Viewbox desktopIconsTile = DesktopIconsTile;
		double num = (DesktopIconsTile.Height = 48.0 * dockScale);
		double num3 = (desktopIconsTile.Width = num);
		double num5 = (thisPcTile2.Height = num3);
		double num7 = (thisPcTile.Width = num5);
		double width = (launchpadTile2.Height = num7);
		launchpadTile.Width = width;
		LaunchpadTile.CornerRadius = new CornerRadius(16.0 * dockScale);
		base.PanelCornerRadius = new CornerRadius(40.0 * dockScale);
		double maxContentWidth = (((object)currentPrimaryDisplay == null) ? 0.0 : Math.Max(1.0, currentPrimaryDisplay.ToDips(currentPrimaryDisplay.WorkArea.Width) - 32.0 - (28.0 + BuiltinEntryWidthDip) * dockScale));
		if (settings.FrameLengthDip > 0.0)
		{
			maxContentWidth = Math.Max(1.0, ResolveEffectiveFrameLengthDip(settings.FrameLengthDip, currentPrimaryDisplay) - (22.0 + BuiltinEntryWidthDip) * dockScale);
		}
		_tileStrip.SetMetrics(48.0 * dockScale, 8.0 * dockScale, 1.38, 8.0 * dockScale, maxContentWidth);
	}

	private double ResolveEffectiveFrameLengthDip(double requested, DisplayInfo? display)
	{
		double num = Math.Max(180.0, BuiltinEntryWidthDip + 22.0) * DockScale;
		if ((object)display == null)
		{
			return Math.Clamp(requested, num, 10000.0);
		}
		double val = Math.Max(64.0, display.ToDips(display.WorkArea.Width) - 32.0);
		val = Math.Min(10000.0, val);
		double min = Math.Min(num, val);
		return Math.Clamp(requested, min, val);
	}

	private void ApplyFrameLength()
	{
		double frameLengthDip = Settings.FrameLengthDip;
		if (frameLengthDip <= 0.0)
		{
			base.SizeToContent = SizeToContent.WidthAndHeight;
			base.Width = double.NaN;
		}
		else
		{
			DisplayInfo currentPrimaryDisplay = CurrentPrimaryDisplay;
			base.SizeToContent = SizeToContent.Height;
			base.Width = ResolveEffectiveFrameLengthDip(frameLengthDip, currentPrimaryDisplay);
		}
	}

	private BitmapImage? LoadTileIcon(AppEntry entry)
	{
		string customIconPath = entry.CustomIconPath;
		if (!string.IsNullOrEmpty(customIconPath) && File.Exists(customIconPath))
		{
			return LoadBitmap(customIconPath);
		}
		if (_services == null || string.IsNullOrEmpty(entry.IconSource))
		{
			return null;
		}
		string text = _services.Icons.PathFor(entry.IconSource);
		if (!File.Exists(text))
		{
			text = _services.Icons.EnsureFor(entry.IconSource)?.Path ?? text;
		}
		if (!File.Exists(text))
		{
			return null;
		}
		return LoadBitmap(text);
	}

	private static BitmapImage? LoadBitmap(string path)
	{
		try
		{
			BitmapImage bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.UriSource = new Uri(path, UriKind.Absolute);
			bitmapImage.EndInit();
			bitmapImage.Freeze();
			return bitmapImage;
		}
		catch (Exception ex) when (((ex is IOException || ex is UriFormatException || ex is FileFormatException || ex is NotSupportedException || ex is ArgumentException) ? 1 : 0) != 0)
		{
			return null;
		}
	}

	private void OnTileClicked(object? sender, string entryId)
	{
		OpenEntry(entryId, minimizeIfForeground: true);
	}

	private void OpenEntry(string entryId, bool minimizeIfForeground)
	{
		if (entryId.StartsWith("entry:", StringComparison.Ordinal))
		{
			RunEntryAction(entryId.Substring("entry:".Length));
		}
		else
		{
			if (_services == null)
			{
				return;
			}
			AppEntry appEntry = _services.Library.Find(entryId);
			if (appEntry != null && appEntry.Kind == AppItemKind.Folder && !appEntry.IsTargetMissing)
			{
				ShowFolderPanel(appEntry);
				return;
			}
			ActivationResult activationResult = _services.Launch.ActivateOrLaunch(entryId, minimizeIfForeground);
			switch (activationResult.Outcome)
			{
			case ActivationOutcome.MultipleWindowsFound:
				ShowWindowChooser(activationResult.Candidates);
				break;
			case ActivationOutcome.Failed:
				ShowFailureNotice(activationResult.Message);
				break;
			}
		}
	}

	private void RunEntryAction(string name)
	{
		switch (name)
		{
		case "launcher":
			RequestLauncherToggle();
			break;
		case "showdesktop":
			DesktopCommands.ShowDesktop();
			break;
		case "recyclebin":
			if (!ShellExecutor.Open("shell:RecycleBinFolder").Started)
			{
				ShowFailureNotice("回收站没能打开。");
			}
			break;
		}
	}

	private void ShowFolderPanel(AppEntry folderEntry)
	{
		_autoHide?.PushSuppress(DateTime.UtcNow);
		nint foregroundBefore = WindowControl.GetForegroundWindow();
		GlassPalette glassPalette = ThemeService.Current?.Palette;
		TextBlock element = new TextBlock
		{
			Text = folderEntry.DisplayName,
			FontWeight = FontWeights.Medium,
			Margin = new Thickness(6.0, 4.0, 6.0, 2.0)
		};
		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(element);
		stackPanel.Children.Add(PanelItem("打开这个文件夹", delegate
		{
			if (!ShellExecutor.Open(folderEntry.Target).Started)
			{
				ShowFailureNotice("文件夹没能打开。");
			}
		}));
		string[] array;
		try
		{
			array = Directory.EnumerateFileSystemEntries(folderEntry.Target).OrderBy<string, string>(Path.GetFileName, StringComparer.CurrentCulture).Take(50)
				.ToArray();
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException) ? 1 : 0) != 0)
		{
			array = Array.Empty<string>();
		}
		if (array.Length == 0)
		{
			stackPanel.Children.Add(new TextBlock
			{
				Text = "（空文件夹，或读不到内容）",
				Margin = new Thickness(10.0, 4.0, 10.0, 6.0)
			});
		}
		string[] array2 = array;
		foreach (string text in array2)
		{
			string path = text;
			stackPanel.Children.Add(PanelItem(Path.GetFileName(path), delegate
			{
				if (!ShellExecutor.Open(path).Started)
				{
					ShowFailureNotice("没能打开：" + Path.GetFileName(path));
				}
			}));
		}
		PhoneBorder phoneBorder = new PhoneBorder
		{
			Background = (glassPalette?.ResolvePanelBrush(acrylicApplied: false) ?? Brushes.DimGray),
			BorderBrush = (glassPalette?.PanelBorderBrush ?? Brushes.Gray),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0),
			Padding = new Thickness(4.0),
			MinWidth = 220.0,
			MaxHeight = 420.0,
			Child = new ScrollViewer
			{
				Content = stackPanel,
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto
			}
		};
		Popup popup = new Popup
		{
			Child = phoneBorder,
			Placement = PlacementMode.MousePoint,
			StaysOpen = false,
			AllowsTransparency = true
		};
		phoneBorder.Focusable = true;
		phoneBorder.KeyDown += delegate(object _, KeyEventArgs args)
		{
			if (args.Key == Key.Escape)
			{
				popup.IsOpen = false;
				args.Handled = true;
			}
		};
		popup.Closed += delegate
		{
			_autoHide?.PopSuppress(DateTime.UtcNow);
			RestorePopupForeground(foregroundBefore, 0);
		};
		WindowActivation.TryBringToForeground(base.Handle);
		popup.IsOpen = true;
		phoneBorder.Focus();
	}

	private static TextBlock PanelItem(string text, Action onClick)
	{
		TextBlock textBlock = new TextBlock();
		textBlock.Text = text;
		textBlock.Padding = new Thickness(10.0, 5.0, 10.0, 5.0);
		textBlock.Cursor = Cursors.Hand;
		textBlock.MouseLeftButtonUp += delegate
		{
			onClick();
		};
		return textBlock;
	}

	private void OnTileContextMenuRequested(object? sender, string entryId)
	{
		if (_services == null)
		{
			return;
		}
		AppEntry entry = _services.Library.Find(entryId);
		if (entry == null)
		{
			return;
		}
		ContextMenu contextMenu = new ContextMenu
		{
			Placement = PlacementMode.MousePoint
		};
		contextMenu.Items.Add(MenuItem("打开", delegate
		{
			OpenEntry(entryId, minimizeIfForeground: false);
		}));
		if (entry.Kind == AppItemKind.Folder)
		{
			contextMenu.Items.Add(MenuItem("在面板中查看内容", delegate
			{
				ShowFolderPanel(entry);
			}));
		}
		contextMenu.Items.Add(new Separator());
		AddIconMenuItems(contextMenu, entryId);
		contextMenu.Items.Add(new Separator());
		if (entry.IsTargetMissing)
		{
			contextMenu.Items.Add(MenuItem("重新定位…", delegate
			{
				RelocateFromDialog(entry);
			}));
		}
		contextMenu.Items.Add(MenuItem("从 Dock 移除", delegate
		{
			OnUnpinRequested(this, entryId);
		}));
		OpenDockContextMenu(contextMenu);
	}

	private void RelocateFromDialog(AppEntry entry)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Title = "重新定位「" + entry.DisplayName + "」",
			CheckFileExists = false
		};
		if (openFileDialog.ShowDialog(this) == true && !string.IsNullOrWhiteSpace(openFileDialog.FileName))
		{
			RelocationOutcome relocationOutcome = _services.Maintenance.Relocate(entry.Id, openFileDialog.FileName);
			if (relocationOutcome.Result != RelocateResult.Relocated)
			{
				ShowFailureNotice(relocationOutcome.Message);
			}
			RefreshTiles();
		}
	}

	private static MenuItem MenuItem(string header, Action onClick)
	{
		MenuItem menuItem = new MenuItem();
		menuItem.Header = header;
		menuItem.Click += delegate
		{
			onClick();
		};
		return menuItem;
	}

	private void ShowWindowChooser(IReadOnlyList<WindowCandidate> candidates)
	{
		if (candidates.Count == 0)
		{
			return;
		}
		_autoHide?.PushSuppress(DateTime.UtcNow);
		nint foregroundBefore = WindowControl.GetForegroundWindow();
		bool activated = false;
		GlassPalette glassPalette = ThemeService.Current?.Palette;
		ListBox list = new ListBox
		{
			BorderThickness = new Thickness(0.0),
			Background = Brushes.Transparent
		};
		for (int i = 0; i < candidates.Count; i++)
		{
			WindowCandidate windowCandidate = candidates[i];
			string content = (string.IsNullOrWhiteSpace(windowCandidate.Title) ? $"窗口 {i + 1}" : windowCandidate.Title);
			ListBoxItem newItem = new ListBoxItem
			{
				Content = content,
				Padding = new Thickness(10.0, 5.0, 10.0, 5.0),
				Tag = windowCandidate.Handle
			};
			list.Items.Add(newItem);
		}
		if (list.Items.Count > 0)
		{
			list.SelectedIndex = 0;
		}
		PhoneBorder phoneBorder = new PhoneBorder
		{
			Background = (glassPalette?.ResolvePanelBrush(acrylicApplied: false) ?? Brushes.DimGray),
			BorderBrush = (glassPalette?.PanelBorderBrush ?? Brushes.Gray),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0),
			Padding = new Thickness(4.0),
			MinWidth = 160.0
		};
		phoneBorder.Child = list;
		Popup popup = new Popup
		{
			Child = phoneBorder,
			Placement = PlacementMode.MousePoint,
			StaysOpen = false,
			AllowsTransparency = true
		};
		list.PreviewMouseLeftButtonUp += delegate(object _, MouseButtonEventArgs args)
		{
			nint num = WindowChooserHit.Resolve(list, args.OriginalSource);
			if (num != 0)
			{
				_ = Activate(num);
			}
		};
		list.KeyDown += delegate(object _, KeyEventArgs args)
		{
			switch (args.Key)
			{
			case Key.Return:
				if (list.SelectedItem is ListBoxItem { Tag: var tag } && tag is nint num && num != 0)
				{
					_ = Activate(num);
				}
				args.Handled = true;
				break;
			case Key.Escape:
				popup.IsOpen = false;
				args.Handled = true;
				break;
			}
		};
		popup.Closed += delegate
		{
			_autoHide?.PopSuppress(DateTime.UtcNow);
			if (!activated)
			{
				RestorePopupForeground(foregroundBefore, 0);
			}
		};
		WindowActivation.TryBringToForeground(base.Handle);
		popup.IsOpen = true;
		list.Focus();
		bool Activate(nint handle)
		{
			popup.IsOpen = false;
			ActivationResult activationResult = LaunchService.ActivateWindow(handle);
			if (activationResult.Outcome == ActivationOutcome.Activated)
			{
				activated = true;
				return true;
			}
			ShowFailureNotice(activationResult.Message ?? "没能把那个窗口切到前台。");
			return false;
		}
	}

	private void RestorePopupForeground(nint previous, nint popupHandle)
	{
		base.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, (Action)delegate
		{
			if (WindowControl.IsAlive(previous))
			{
				nint foregroundWindow = WindowControl.GetForegroundWindow();
				if (foregroundWindow == base.Handle || foregroundWindow == 0 || (popupHandle != 0 && foregroundWindow == popupHandle))
				{
					WindowActivation.TryBringToForeground(previous);
				}
			}
		});
	}

	private void ShowFailureNotice(string? message)
	{
		ShowTemporaryNotice(message);
	}

	private void ShowTemporaryNotice(string? message)
	{
		if (!string.IsNullOrWhiteSpace(message))
		{
			StatusText.Text = message;
			StatusText.Visibility = Visibility.Visible;
			if (_failureNoticeTimer == null)
			{
				_failureNoticeTimer = NewFailureNoticeTimer();
			}
			_failureNoticeTimer.Stop();
			_failureNoticeTimer.Start();
		}
	}

	private DispatcherTimer NewFailureNoticeTimer()
	{
		DispatcherTimer timer = new DispatcherTimer
		{
			Interval = FailureNoticeDuration
		};
		timer.Tick += delegate
		{
			timer.Stop();
			RefreshDiagnostics();
			RefreshFromSettings();
		};
		return timer;
	}

	private void OnRunningChanged(object? sender, EventArgs e)
	{
		if (base.Dispatcher.HasShutdownStarted || base.Dispatcher.HasShutdownFinished)
		{
			return;
		}
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			if (_services == null || _performanceSleepRequested)
			{
				return;
			}
			foreach (DockTileModel model in _models)
			{
				AppEntry appEntry = _services.Library.Find(model.EntryId);
				DockTileModel.ApplyRunningIndicatorSetting(model, (appEntry == null || !_services.Running.IsRunning(appEntry)) ? Visibility.Collapsed : Visibility.Visible, Settings.ShowRunningIndicator);
			}
		});
	}

	public void RefreshDiagnostics()
	{
		ThemeService current = ThemeService.Current;
		string text = ((base.SurfaceMode == GlassSurfaceMode.Acrylic) ? "亚克力背景模糊" : "自绘半透明面板（无背景模糊）");
		StatusText.Text = text + "｜" + ((current != null && current.IsDark) ? "深色" : "浅色");
		StatusText.Visibility = Visibility.Collapsed;
	}

	public void RefreshFromSettings()
	{
		StatusText.Visibility = Visibility.Collapsed;
		ApplyFrameLength();
		ApplyStripMetrics();
	}

	private void OnDockSettingsChanged(object? sender, EventArgs e)
	{
		RebuildAutoHide();
		RefreshFromSettings();
		RefreshTiles();
		_slideOffsetPixels = 0.0;
		PositionOnDisplay();
	}

	private void RequestLauncherToggle(PixelRect? sourceBounds = null)
	{
		_toggleLauncher?.Invoke(sourceBounds);
	}

	private void OnLauncherButtonPreviewMouseDown(object sender, MouseButtonEventArgs e)
	{
		_launcherPressGeneration++;
		_launcherWasOpenOnPress = _launcherContainerActive;
		_launcherReleaseClickPending = false;
	}

	private void OnLauncherButtonPreviewMouseUp(object sender, MouseButtonEventArgs e)
	{
		_launcherReleaseClickPending = true;
		ClearLauncherPressAfterInput();
	}

	private void OnLauncherButtonLostMouseCapture(object sender, MouseEventArgs e)
	{
		ClearLauncherPressAfterInput();
	}

	private void ClearLauncherPressAfterInput()
	{
		int generation = _launcherPressGeneration;
		base.Dispatcher.BeginInvoke(DispatcherPriority.Input, (Action)delegate
		{
			if (_launcherPressGeneration == generation)
			{
				_launcherWasOpenOnPress = false;
				_launcherReleaseClickPending = false;
			}
		});
	}

	private void OnLauncherButtonClick(object sender, RoutedEventArgs e)
	{
		bool num = _launcherReleaseClickPending && _launcherWasOpenOnPress;
		_launcherWasOpenOnPress = false;
		_launcherReleaseClickPending = false;
		if (!num || _launcherContainerActive)
		{
			RequestLauncherToggle(GetLauncherButtonPixelBounds());
		}
	}

	public PixelRect? GetLauncherButtonPixelBounds()
	{
		if (!Settings.ShowLauncherEntry || LauncherButton.Visibility != Visibility.Visible || !base.IsVisible || !IsOpticalSurfaceVisible || LauncherButton.ActualWidth < 1.0 || LauncherButton.ActualHeight < 1.0)
		{
			return null;
		}
		try
		{
			Point point = LaunchpadTile.PointToScreen(default(Point));
			Point point2 = LaunchpadTile.PointToScreen(new Point(LaunchpadTile.ActualWidth, LaunchpadTile.ActualHeight));
			return new PixelRect((int)Math.Floor(point.X), (int)Math.Floor(point.Y), Math.Max(1, (int)Math.Ceiling(point2.X - point.X)), Math.Max(1, (int)Math.Ceiling(point2.Y - point.Y)));
		}
		catch (InvalidOperationException)
		{
			return null;
		}
	}

	private void OnThisPcButtonClick(object sender, RoutedEventArgs e)
	{
		if (!ShellExecutor.Open("shell:MyComputerFolder").Started)
		{
			ShowFailureNotice("此电脑没能打开。");
		}
	}

	private void OnDesktopIconsButtonClick(object sender, RoutedEventArgs e)
	{
		ToggleDesktopIcons?.Invoke();
	}

	public void RefreshDisplayPlacement()
	{
		ApplyFrameLength();
		ApplyStripMetrics();
		PositionOnDisplay();
	}

	public void StopForPrimaryDisplayChange()
	{
		_frameTimer?.Stop();
		Hide();
	}

	public void ResumeAfterPrimaryDisplayChange(bool show)
	{
		RebuildAutoHide();
		RefreshDisplayPlacement();
		if (show && !_performanceSleepRequested)
		{
			Show();
		}
	}

	public void PositionOnDisplay()
	{
		if (base.Handle == 0)
		{
			return;
		}
		DisplayInfo currentPrimaryDisplay = CurrentPrimaryDisplay;
		if ((object)currentPrimaryDisplay == null)
		{
			return;
		}
		double num = ((base.ActualWidth > 0.0) ? base.ActualWidth : base.DesiredSize.Width);
		double num2 = ((base.ActualHeight > 0.0) ? base.ActualHeight : base.DesiredSize.Height);
		if (!(num <= 0.0) && !(num2 <= 0.0))
		{
			int windowWidthPixels = Math.Max(1, currentPrimaryDisplay.ToPixels(num));
			int windowHeightPixels = Math.Max(1, currentPrimaryDisplay.ToPixels(num2));
			_restingRect = DockPlacementCalculator.Calculate(currentPrimaryDisplay.WorkArea, Settings.Placement, Settings.OffsetX, Settings.OffsetY, windowWidthPixels, windowHeightPixels, currentPrimaryDisplay.Dpi);
			PrimaryDisplaySession primaryDisplaySession = base.PrimaryDisplaySession;
			if (primaryDisplaySession != null && primaryDisplaySession.IsAvailable)
			{
				_restingRect = primaryDisplaySession.ClampWindowToWorkArea(_restingRect);
			}
			_fullyHiddenOffsetPixels = SlideOutDistancePixels(_restingRect, currentPrimaryDisplay.Bounds, Settings.Placement);
			ApplySlide(_restingRect, _slideOffsetPixels);
		}
	}

	private void ApplySlide(PixelRect restingRect, double slideOffsetPixels)
	{
		int num = (int)Math.Round(slideOffsetPixels * (double)SlideOutDirection(), MidpointRounding.AwayFromZero);
		bool flag = restingRect.Height > 0 && Math.Abs(num) >= restingRect.Height;
		if (flag && !_panelWasConcealed)
		{
			GlassSurface.RetireConcealedMaterial();
		}
		_panelWasConcealed = flag;
		bool isOpticalSurfaceVisible = IsOpticalSurfaceVisible;
		if (isOpticalSurfaceVisible && !_backdropWasActive)
		{
			GlassSurface.RequestBackdropRefresh(revealing: true);
		}
		_backdropWasActive = isOpticalSurfaceVisible;
		PrimaryDisplaySession primaryDisplaySession = base.PrimaryDisplaySession;
		if (primaryDisplaySession != null)
		{
			if (primaryDisplaySession.IsAvailable)
			{
				PixelRect pixelRect = primaryDisplaySession.ClampWindowToWorkArea(restingRect);
				if (DisplayService.GetWindowPixelBounds(base.Handle) != pixelRect)
				{
					DisplayService.MoveWindowToPixelRect(base.Handle, pixelRect);
				}
				double num2 = Math.Max(0.01, primaryDisplaySession.Display.ScaleFactor);
				double num3 = (double)num / num2;
				DockSlideTransform.Y = num3;
				_slidePanelBoundsDip = new Rect(0.0, num3, base.ActualWidth, base.ActualHeight);
				if (base.ActualWidth > 0.0 && base.ActualHeight > 0.0)
				{
					double num4 = (double)(_fullyHiddenOffsetPixels * SlideOutDirection()) / num2;
					GlassSurface.SetPanelShape(_slidePanelBoundsDip.Value, base.PanelCornerRadius.TopLeft);
					GlassSurface.SetContainerTransitionBounds(new Rect(0.0, Math.Min(0.0, num4), base.ActualWidth, base.ActualHeight + Math.Abs(num4)));
					SetSharedPanelClip(_slidePanelBoundsDip.Value, base.PanelCornerRadius.TopLeft);
				}
				double strength = Math.Clamp(1.0 - Math.Abs(num3) / Math.Max(1.0, base.ActualHeight), 0.0, 1.0);
				SetTransitionShadowShape((Math.Abs(num3) < 0.01) ? ((Rect?)null) : _slidePanelBoundsDip, base.PanelCornerRadius.TopLeft, strength);
			}
		}
		else
		{
			DockSlideTransform.Y = 0.0;
			SetTransitionShadowShape(null, 0.0);
			PixelRect rect = restingRect with
			{
				Y = restingRect.Y + num
			};
			DisplayService.MoveWindowToPixelRect(base.Handle, rect);
		}
	}

	private int SlideOutDirection()
	{
		if (Settings.Placement != DockPlacement.TopCenter)
		{
			return 1;
		}
		return -1;
	}

	private static int SlideOutDistancePixels(PixelRect restingRect, PixelRect displayBounds, DockPlacement placement)
	{
		int val = ((placement == DockPlacement.TopCenter) ? (restingRect.Bottom - displayBounds.Y) : (displayBounds.Bottom - restingRect.Y));
		return Math.Max(0, val) + 2;
	}

	private void RebuildAutoHide()
	{
		DockSettings settings = Settings;
		_autoHide = new AutoHideController(settings.AutoHideLeaveDelayMs);
		_autoHide.ForceShown(DateTime.UtcNow);
		_slideOffsetPixels = 0.0;
		if (_frameTimer == null)
		{
			_frameTimer = new DispatcherTimer(DispatcherPriority.Send)
			{
				Interval = FrameInterval
			};
			_frameTimer.Tick += delegate
			{
				OnAutoHideFrame();
			};
		}
		_frameTimer.IsEnabled = !_performanceSleepRequested;
		_frameTimer.Interval = TimeSpan.FromMilliseconds(100L);
	}

	private void OnAutoHideFrame()
	{
		if (_performanceSleepRequested)
		{
			AdvancePerformanceSleep();
		}
		else
		{
			if (_autoHide == null || _services == null || _restingRect.Width == 0)
			{
				return;
			}
			DateTime utcNow = DateTime.UtcNow;
			(int, int)? cursorPixelPoint = DisplayService.GetCursorPixelPoint();
			bool flag = FullscreenDetector.IsFullscreenActive();
			bool flag2 = ForegroundCoverage.IsCovered(_restingRect);
			bool flag3 = flag || flag2;
			int num;
			if (cursorPixelPoint.HasValue)
			{
				(int, int) valueOrDefault = cursorPixelPoint.GetValueOrDefault();
				num = (Inflate(GetVisibleDockRect(), 2).Contains(valueOrDefault.Item1, valueOrDefault.Item2) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			bool pointerOverVisibleDock = (byte)num != 0;
			int num2;
			if (cursorPixelPoint.HasValue)
			{
				(int, int) valueOrDefault2 = cursorPixelPoint.GetValueOrDefault();
				num2 = (Inflate(_restingRect, 2).Contains(valueOrDefault2.Item1, valueOrDefault2.Item2) ? 1 : 0);
			}
			else
			{
				num2 = 0;
			}
			bool pointerOverDockPosition = (byte)num2 != 0;
			UpdateAutoHideState(utcNow, flag, flag2, pointerOverDockPosition, pointerOverVisibleDock);
			double num3 = ((_autoHide.State == AutoHideState.Hidden) ? ((double)_fullyHiddenOffsetPixels) : 0.0);
			NotifyPerformanceSurfaceVisibility();
			if (num3 == 0.0 && _backgroundCaptureSleeping)
			{
				SetBackgroundCaptureSleeping(sleeping: false);
			}
			else if (num3 == 0.0)
			{
				PrepareConcealedReveal();
			}
			if (num3 == 0.0 && !CanStartPreparedReveal(Stopwatch.GetTimestamp()))
			{
				if (_frameTimer != null)
				{
					_frameTimer.Interval = FrameInterval;
				}
				NotifyPerformanceSurfaceVisibility();
				return;
			}
			if (num3 != 0.0)
			{
				_revealPreparationStarted = 0L;
			}
			ThemeService? current = ThemeService.Current;
			if (current != null && !current.AnimationsEnabled)
			{
				_slideOffsetPixels = num3;
				ApplySlide(_restingRect, num3);
				NotifyPerformanceSurfaceVisibility();
				return;
			}
			if (Math.Abs(_slideOffsetPixels - num3) > 0.5)
			{
				_slideOffsetPixels += (num3 - _slideOffsetPixels) * 0.25;
				if (Math.Abs(_slideOffsetPixels - num3) <= 0.5)
				{
					_slideOffsetPixels = num3;
				}
				ApplySlide(_restingRect, _slideOffsetPixels);
			}
			else if (_slideOffsetPixels != num3)
			{
				_slideOffsetPixels = num3;
				ApplySlide(_restingRect, _slideOffsetPixels);
			}
			if (_frameTimer != null)
			{
				_frameTimer.Interval = (((!_backgroundCaptureSleeping && (flag3 || _autoHide.Suppressed)) || Math.Abs(_slideOffsetPixels - num3) > 0.5) ? FrameInterval : TimeSpan.FromMilliseconds(100L));
			}
			NotifyPerformanceSurfaceVisibility();
		}
	}

	private bool CanStartPreparedReveal(long now)
	{
		if (_restingRect.Height <= 0 || !_panelWasConcealed || GlassSurface.IsCurrentRevealMaterialReady)
		{
			_revealPreparationStarted = 0L;
			return true;
		}
		if (_revealPreparationStarted == 0L)
		{
			_revealPreparationStarted = now;
		}
		return now - _revealPreparationStarted >= Stopwatch.Frequency * 3;
	}

	private void UpdateAutoHideState(DateTime now, bool fullscreen, bool covered, bool pointerOverDockPosition, bool pointerOverVisibleDock)
	{
		if (_autoHide == null)
		{
			return;
		}
		if (base.IsFixedDesktopLayer)
		{
			_edgeRevealedDuringSuppression = false;
			_autoHide.ForceShown(now);
		}
		else if (_launcherContainerActive)
		{
			_edgeRevealedDuringSuppression = false;
			_autoHide.ForceShown(now);
		}
		else if (_autoHide.Suppressed || _tileStrip.IsDragging)
		{
			_edgeRevealedDuringSuppression = fullscreen || covered;
			_autoHide.ForceShown(now);
		}
		else if (fullscreen || covered)
		{
			if (_revealLocked)
			{
				_edgeRevealedDuringSuppression = false;
				_autoHide.ForceHide(now);
			}
			else if (_autoHide.State == AutoHideState.Hidden)
			{
				if (PassiveRevealPolicy.CanRevealDuringSuppression(pointerOverDockPosition, _revealLocked) && _autoHide.TryReveal(isCovered: false, now))
				{
					_edgeRevealedDuringSuppression = true;
				}
			}
			else if (_edgeRevealedDuringSuppression)
			{
				if (pointerOverVisibleDock)
				{
					_autoHide.PointerEnter(now);
				}
				else
				{
					_autoHide.PointerLeave(now);
				}
				_autoHide.Poll(now);
			}
			else
			{
				_autoHide.ForceHide(now);
			}
		}
		else if (Settings.AutoHide)
		{
			_edgeRevealedDuringSuppression = false;
			if (pointerOverDockPosition || pointerOverVisibleDock)
			{
				_autoHide.PointerEnter(now);
			}
			else
			{
				_autoHide.PointerLeave(now);
				_autoHide.Poll(now);
			}
		}
		else
		{
			_edgeRevealedDuringSuppression = false;
			_autoHide.ForceShown(now);
		}
		if (_autoHide.State == AutoHideState.Hidden)
		{
			_edgeRevealedDuringSuppression = false;
		}
	}

	private PixelRect GetVisibleDockRect()
	{
		int num = (int)Math.Round(_slideOffsetPixels * (double)SlideOutDirection(), MidpointRounding.AwayFromZero);
		PixelRect result = _restingRect with
		{
			Y = _restingRect.Y + num
		};
		if (base.PrimaryDisplaySession == null)
		{
			return result;
		}
		int num2 = Math.Max(_restingRect.X, result.X);
		int num3 = Math.Max(_restingRect.Y, result.Y);
		int num4 = Math.Min(_restingRect.Right, result.Right);
		int num5 = Math.Min(_restingRect.Bottom, result.Bottom);
		if (num4 > num2 && num5 > num3)
		{
			return new PixelRect(num2, num3, num4 - num2, num5 - num3);
		}
		return new PixelRect(_restingRect.X, _restingRect.Y, 0, 0);
	}

	public void ToggleRevealLock()
	{
		_revealLocked = !_revealLocked;
		if (!_performanceSleepRequested)
		{
			if (_frameTimer != null)
			{
				_frameTimer.Interval = FrameInterval;
				_frameTimer.IsEnabled = true;
			}
			OnAutoHideFrame();
		}
	}

	private static PixelRect Inflate(PixelRect rect, int by)
	{
		return new PixelRect(rect.X - by, rect.Y - by, rect.Width + by * 2, rect.Height + by * 2);
	}

	protected override void OnSourceInitialized(EventArgs e)
	{
		base.OnSourceInitialized(e);
		RefreshDiagnostics();
		WindowStyles.MakeNoActivate(base.Handle);
	}

	protected override void OnClosed(EventArgs e)
	{
		_frameTimer?.Stop();
		_failureNoticeTimer?.Stop();
		if (_dockSettings != null)
		{
			_dockSettings.Changed -= OnDockSettingsChanged;
		}
		if (_services != null)
		{
			_services.Running.Changed -= OnRunningChanged;
		}
		base.OnClosed(e);
	}

	protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
	{
		base.OnRenderSizeChanged(sizeInfo);
		PositionOnDisplay();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.12.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/GlassDesk.Dock;V0.2.2.0;component/dockwindow.xaml", UriKind.Relative);
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
			((DockWindow)target).Drop += OnExternalDrop;
			((DockWindow)target).DragOver += OnExternalDragOver;
			break;
		case 2:
			GlassSurface = (LiquidGlassSurface)target;
			break;
		case 3:
			DockVisualRoot = (Grid)target;
			break;
		case 4:
			DockSlideTransform = (TranslateTransform)target;
			break;
		case 5:
			DockContentLayer = (StackPanel)target;
			break;
		case 6:
			LauncherButton = (Button)target;
			LauncherButton.MouseRightButtonUp += OnBuiltinIconContextMenu;
			LauncherButton.MouseEnter += OnEntryMouseEnter;
			LauncherButton.MouseLeave += OnEntryMouseLeave;
			LauncherButton.PreviewMouseLeftButtonDown += OnLauncherButtonPreviewMouseDown;
			LauncherButton.PreviewMouseLeftButtonUp += OnLauncherButtonPreviewMouseUp;
			LauncherButton.LostMouseCapture += OnLauncherButtonLostMouseCapture;
			LauncherButton.Click += OnLauncherButtonClick;
			break;
		case 7:
			LaunchpadTile = (PhoneBorder)target;
			break;
		case 8:
			LauncherDefaultGlyph = (LaunchpadGlyph)target;
			break;
		case 9:
			LauncherCustomIcon = (Image)target;
			break;
		case 10:
			ThisPcButton = (Button)target;
			ThisPcButton.MouseRightButtonUp += OnBuiltinIconContextMenu;
			ThisPcButton.MouseEnter += OnEntryMouseEnter;
			ThisPcButton.MouseLeave += OnEntryMouseLeave;
			ThisPcButton.Click += OnThisPcButtonClick;
			break;
		case 11:
			ThisPcTile = (Viewbox)target;
			break;
		case 12:
			ThisPcDefaultGlyph = (Canvas)target;
			break;
		case 13:
			ThisPcCustomIcon = (Image)target;
			break;
		case 14:
			DesktopIconsButton = (Button)target;
			DesktopIconsButton.MouseRightButtonUp += OnBuiltinIconContextMenu;
			DesktopIconsButton.MouseEnter += OnEntryMouseEnter;
			DesktopIconsButton.MouseLeave += OnEntryMouseLeave;
			DesktopIconsButton.Click += OnDesktopIconsButtonClick;
			break;
		case 15:
			DesktopIconsTile = (Viewbox)target;
			break;
		case 16:
			DesktopIconsDefaultGlyph = (Canvas)target;
			break;
		case 17:
			DesktopIconsCustomIcon = (Image)target;
			break;
		case 18:
			TileStripHost = (ContentControl)target;
			break;
		case 19:
			StatusText = (TextBlock)target;
			break;
		case 20:
			DockDragOverlay = (Canvas)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.1.0.9782' (yours is '9.1.0.7988')
