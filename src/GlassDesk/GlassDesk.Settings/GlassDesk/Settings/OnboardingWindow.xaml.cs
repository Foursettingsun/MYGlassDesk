using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using GlassDesk.Core.Storage;
using GlassDesk.UI.Controls;

namespace GlassDesk.Settings;

public partial class OnboardingWindow : GlassWindow, IComponentConnector
{
	private readonly SettingsStore _settings;

	private int _task;

	private bool _completed;

	private string? _pendingSource;

	private string? _dragSource;

	private Point _pressPosition;

	private long _pressTimestamp;

	private static readonly SolidColorBrush Accent = FrozenBrush(Color.FromRgb(139, 190, byte.MaxValue));

	private static readonly SolidColorBrush Quiet = FrozenBrush(Color.FromRgb(82, 100, 129));

	private static readonly string[] Titles = new string[5] { "把快捷方式添加到 Dock", "从 Dock 移除快捷方式", "通过右上角添加到启动台", "拖到下方红色垃圾桶移除", "从信息栏打开设置" };

	private static readonly string[] Instructions = new string[5] { "按住桌面左侧的“示例快捷方式”，拖到底部 Dock 的 ＋ 位置，再松开鼠标。", "在 Dock 的蓝色示例图标上点击鼠标右键，再选择“从 Dock 移除”。", "点击 Dock 的九宫格打开启动台，再点右上角“＋ 添加”，添加所选示例。", "点击九宫格打开启动台。长按“示例应用”并拖到底部中央的红色垃圾桶，再松开鼠标。", "点击右上角信息栏的箭头打开控制中心，再点击控制中心右上角的齿轮。" };

	protected override bool UseOpaquePanel => true;

	public event EventHandler<string>? SettingsPageRequested;

	private static SolidColorBrush FrozenBrush(Color color)
	{
		SolidColorBrush solidColorBrush = new SolidColorBrush(color);
		solidColorBrush.Freeze();
		return solidColorBrush;
	}

	public void PositionOnPrimary()
	{
		CenterOnPrimaryWorkArea();
	}

	public OnboardingWindow(SettingsStore settings)
	{
		ArgumentNullException.ThrowIfNull(settings, "settings");
		_settings = settings;
		InitializeComponent();
		MaxWidth = Math.Max(320.0, SystemParameters.WorkArea.Width - 24.0);
		MaxHeight = Math.Max(300.0, SystemParameters.WorkArea.Height - 24.0);
		MinWidth = Math.Min(MinWidth, MaxWidth);
		MinHeight = Math.Min(MinHeight, MaxHeight);
		PreviewKeyDown += OnPracticeKeyDown;
		Deactivated += (object? _, EventArgs _) =>
		{
			CancelDrag();
			HidePopovers();
		};
		RenderTask();
	}

	private void RenderTask()
	{
		CancelDrag();
		HidePopovers();
		_completed = false;
		FeedbackText.Foreground = new SolidColorBrush(Color.FromRgb(151, 177, 211));
		PageCounter.Text = $"{_task + 1:00} / 05";
		PageHeading.Text = Titles[_task];
		InstructionText.Text = Instructions[_task];
		FeedbackText.Text = ((_task == 3) ? "长按后拖动；点击启动台外面会收起它。移除入口不会删除原文件。" : "放心练习：不会改动你的真实图标、文件或布局。");
		PreviousButton.IsEnabled = _task > 0;
		NextButton.Content = "跳过此项";
		OpenSettingsButton.Visibility = Visibility.Collapsed;
		DesktopExample.Visibility = ((_task != 0) ? Visibility.Hidden : Visibility.Visible);
		DesktopExampleGroup.Visibility = ((_task != 0) ? Visibility.Hidden : Visibility.Visible);
		DockExample.Visibility = ((_task != 1) ? Visibility.Collapsed : Visibility.Visible);
		DockDropHint.Visibility = ((_task != 0) ? Visibility.Collapsed : Visibility.Visible);
		LauncherExampleGroup.Visibility = ((_task != 3) ? Visibility.Collapsed : Visibility.Visible);
		LauncherExample.Visibility = ((_task != 3) ? Visibility.Collapsed : Visibility.Visible);
		MockDock.BorderBrush = ((_task <= 1) ? Accent : Quiet);
		Button launcherToggleButton = LauncherToggleButton;
		int task = _task;
		bool flag = (uint)(task - 2) <= 1u;
		launcherToggleButton.BorderBrush = (flag ? Accent : Quiet);
		AddAppButton.BorderBrush = ((_task == 2) ? Accent : Quiet);
		ControlCenterArrowButton.BorderBrush = ((_task == 4) ? Accent : Quiet);
		SettingsGearButton.BorderBrush = ((_task == 4) ? Accent : Quiet);
	}

	private void Complete(string message)
	{
		_completed = true;
		FeedbackText.Text = "✓ 完成！" + message;
		FeedbackText.Foreground = new SolidColorBrush(Color.FromRgb(147, 223, 186));
		NextButton.Content = ((_task == 4) ? "完成教学" : "下一项");
		OpenSettingsButton.Visibility = ((_task != 4) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void OnNextClick(object sender, RoutedEventArgs e)
	{
		if (_task == 4)
		{
			Close();
			return;
		}
		_task++;
		RenderTask();
	}

	private void OnPreviousClick(object sender, RoutedEventArgs e)
	{
		if (_task > 0)
		{
			_task--;
			RenderTask();
		}
	}

	private void OnRestartClick(object sender, RoutedEventArgs e)
	{
		RenderTask();
	}

	private void OnSkipClick(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void OnRealSettingsClick(object sender, RoutedEventArgs e)
	{
		Close();
		SettingsPageRequested?.Invoke(this, "关于");
	}

	private void OnLauncherClick(object sender, RoutedEventArgs e)
	{
		bool flag = MockLauncher.Visibility != Visibility.Visible;
		HidePopovers();
		MockLauncher.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		if (flag && _task == 2 && !_completed)
		{
			FeedbackText.Text = "找到右上角“＋ 添加”。启动台外面不能拖入添加，点击外面会收起启动台。";
		}
	}

	private void OnAddAppClick(object sender, RoutedEventArgs e)
	{
		if (_task != 2 || _completed)
		{
			FeedbackText.Text = "本项先练习指示的操作；可以点击“重练本项”重新开始。";
		}
		else
		{
			MockPicker.Visibility = Visibility.Visible;
		}
	}

	private void OnConfirmAddClick(object sender, RoutedEventArgs e)
	{
		if (_task == 2 && MockPicker.Visibility == Visibility.Visible)
		{
			MockPicker.Visibility = Visibility.Collapsed;
			LauncherExampleGroup.Visibility = Visibility.Visible;
			LauncherExample.Visibility = Visibility.Visible;
			Complete("已通过右上角添加按钮添加入口。实际使用时可选择文件、文件夹、网站或从桌面批量添加。");
		}
	}

	private void OnRemoveDockClick(object sender, RoutedEventArgs e)
	{
		if (_task == 1 && DockExample.Visibility == Visibility.Visible)
		{
			DockExample.Visibility = Visibility.Collapsed;
			Complete("仅移除了 Dock 入口，原应用和文件仍然保留。");
		}
	}

	private void OnControlCenterClick(object sender, RoutedEventArgs e)
	{
		bool flag = MockControlCenter.Visibility != Visibility.Visible;
		HidePopovers();
		MockControlCenter.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		if (flag && _task == 4 && !_completed)
		{
			FeedbackText.Text = "控制中心已展开。点击它右上角的齿轮，打开 GlassDesk 设置。";
		}
	}

	private void OnSettingsGearClick(object sender, RoutedEventArgs e)
	{
		if (MockControlCenter.Visibility == Visibility.Visible)
		{
			HidePopovers();
			MockSettings.Visibility = Visibility.Visible;
			if (_task == 4)
			{
				Complete("已学会从信息栏打开设置。你可以完成教学，或主动打开真实设置。");
			}
		}
	}

	private void HidePopovers()
	{
		Border mockPicker = MockPicker;
		Border mockLauncher = MockLauncher;
		Border mockControlCenter = MockControlCenter;
		Visibility visibility = (MockSettings.Visibility = Visibility.Collapsed);
		Visibility visibility3 = (mockControlCenter.Visibility = visibility);
		Visibility visibility5 = (mockLauncher.Visibility = visibility3);
		mockPicker.Visibility = visibility5;
	}

	private bool Contains(FrameworkElement element, Point point)
	{
		if (element.Visibility != Visibility.Visible)
		{
			return false;
		}
		return element.TransformToAncestor(SimulationSurface).TransformBounds(new Rect(element.RenderSize)).Contains(point);
	}

	private void DismissPopoversAt(Point position)
	{
		if (!Contains(MockLauncher, position) && !Contains(LauncherToggleButton, position))
		{
			Border mockPicker = MockPicker;
			Visibility visibility = (MockLauncher.Visibility = Visibility.Collapsed);
			mockPicker.Visibility = visibility;
		}
		if (!Contains(MockControlCenter, position) && !Contains(ControlCenterArrowButton, position))
		{
			MockControlCenter.Visibility = Visibility.Collapsed;
		}
		if (!Contains(MockSettings, position))
		{
			MockSettings.Visibility = Visibility.Collapsed;
		}
	}

	private void OnSurfaceMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (_dragSource == null)
		{
			DismissPopoversAt(e.GetPosition(SimulationSurface));
		}
	}

	private void OnItemMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (sender is FrameworkElement { Tag: string tag } && e.ChangedButton == MouseButton.Left && CanDrag(tag))
		{
			_pendingSource = tag;
			_pressPosition = e.GetPosition(SimulationSurface);
			_pressTimestamp = Stopwatch.GetTimestamp();
			SimulationSurface.CaptureMouse();
			e.Handled = true;
		}
	}

	private bool CanDrag(string source)
	{
		if (!_completed)
		{
			if (!(source == "desktop") || _task != 0)
			{
				if (source == "launcher" && _task == 3 && MockLauncher.Visibility == Visibility.Visible)
				{
					return LauncherExample.Visibility == Visibility.Visible;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	private void OnSurfaceMouseMove(object sender, MouseEventArgs e)
	{
		if (e.LeftButton != MouseButtonState.Pressed)
		{
			CancelDrag();
			return;
		}
		Point position = e.GetPosition(SimulationSurface);
		if (_dragSource == null && _pendingSource != null && (position - _pressPosition).Length >= 6.0)
		{
			if (_pendingSource == "launcher" && Stopwatch.GetElapsedTime(_pressTimestamp).TotalMilliseconds < 350.0)
			{
				FeedbackText.Text = "先按住图标片刻，再拖向下方中央的红色垃圾桶。";
				return;
			}
			StartDrag(_pendingSource, position);
		}
		if (_dragSource != null)
		{
			Canvas.SetLeft(DragGhost, position.X - 28.0);
			Canvas.SetTop(DragGhost, position.Y - 28.0);
			if (_dragSource == "launcher")
			{
				TrashDropTarget.Background = (Contains(TrashDropTarget, position) ? new SolidColorBrush(Color.FromArgb(120, 239, 68, 68)) : new SolidColorBrush(Color.FromArgb(34, 239, 68, 68)));
			}
		}
	}

	private void StartDrag(string source, Point position)
	{
		if (CanDrag(source))
		{
			_dragSource = source;
			DragGhost.Visibility = Visibility.Visible;
			Canvas.SetLeft(DragGhost, position.X - 28.0);
			Canvas.SetTop(DragGhost, position.Y - 28.0);
			if (source == "launcher")
			{
				TrashDropTarget.Visibility = Visibility.Visible;
			}
			SimulationSurface.UpdateLayout();
		}
	}

	private void FinishDrag(Point position)
	{
		string dragSource = _dragSource;
		if (dragSource == "desktop" && Contains(MockDock, position))
		{
			DockExample.Visibility = Visibility.Visible;
			DockDropHint.Visibility = Visibility.Collapsed;
			Complete("快捷方式已加入模拟 Dock。以后可从这里快速打开应用。");
		}
		else if (dragSource == "launcher" && Contains(TrashDropTarget, position))
		{
			Button launcherExample = LauncherExample;
			Visibility visibility = (LauncherExampleGroup.Visibility = Visibility.Collapsed);
			launcherExample.Visibility = visibility;
			Complete("启动台入口已移除；原应用与文件不会被删除。");
		}
		else if (dragSource != null)
		{
			FeedbackText.Text = ((dragSource == "desktop") ? "还没放入 Dock。把图标拖到底部 Dock 内，再松开鼠标。" : "还没放入垃圾桶。请拖到底部中央的红色垃圾桶后松开。");
		}
		CancelDrag();
	}

	private void OnSurfaceMouseUp(object sender, MouseButtonEventArgs e)
	{
		if (e.ChangedButton == MouseButton.Left && (_pendingSource != null || _dragSource != null))
		{
			FinishDrag(e.GetPosition(SimulationSurface));
			e.Handled = true;
		}
	}

	private void CancelDrag()
	{
		_pendingSource = (_dragSource = null);
		Border dragGhost = DragGhost;
		Visibility visibility = (TrashDropTarget.Visibility = Visibility.Collapsed);
		dragGhost.Visibility = visibility;
		if (SimulationSurface.IsMouseCaptured)
		{
			SimulationSurface.ReleaseMouseCapture();
		}
	}

	private void OnLostMouseCapture(object sender, MouseEventArgs e)
	{
		if (_pendingSource != null || _dragSource != null)
		{
			CancelDrag();
		}
	}

	private void OnPracticeKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Escape)
		{
			e.Handled = true;
			if (_pendingSource != null || _dragSource != null)
			{
				CancelDrag();
			}
			else if (MockPicker.IsVisible)
			{
				MockPicker.Visibility = Visibility.Collapsed;
			}
			else if (MockLauncher.IsVisible || MockControlCenter.IsVisible || MockSettings.IsVisible)
			{
				HidePopovers();
			}
			else
			{
				Close();
			}
		}
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		base.OnClosing(e);
		if (e.Cancel)
		{
			return;
		}
		CancelDrag();
		if (_settings.Current.OnboardingDismissed == true)
		{
			return;
		}
		try
		{
			_settings.DismissOnboarding();
		}
		catch (StorageException)
		{
			MessageBox.Show("新手教学的完成状态未能保存，下次启动可能再次显示。你仍可正常使用 GlassDesk。", "GlassDesk", MessageBoxButton.OK, MessageBoxImage.Asterisk);
		}
	}

	private void OnDragArea(object sender, MouseButtonEventArgs e)
	{
		if (e.OriginalSource is TextBlock && e.ChangedButton == MouseButton.Left && Mouse.LeftButton == MouseButtonState.Pressed)
		{
			e.Handled = true;
			DragMove();
		}
	}
}
