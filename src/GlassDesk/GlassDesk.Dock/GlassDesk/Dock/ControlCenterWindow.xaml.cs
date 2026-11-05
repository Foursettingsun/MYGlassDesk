using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GlassDesk.Core.Models.Settings;
using GlassDesk.Platform;
using GlassDesk.Platform.Displays;
using GlassDesk.Platform.Shell;
using GlassDesk.UI.Controls;

namespace GlassDesk.Dock;

public partial class ControlCenterWindow : GlassWindow, IComponentConnector
{
	private sealed record MixerChange(ApplicationVolume Application, double? Volume);

	private sealed class MixerRow(ApplicationVolume application, Grid root, Slider slider, TextBlock label)
	{
		public ApplicationVolume Application { get; set; } = application;

		public Grid Root { get; } = root;

		public Slider Slider { get; } = slider;

		public TextBlock Label { get; } = label;
	}

	private readonly DispatcherTimer _mixerRefreshTimer = new DispatcherTimer
	{
		Interval = TimeSpan.FromSeconds(2L)
	};

	private readonly DispatcherTimer _mixerWriteTimer = new DispatcherTimer
	{
		Interval = TimeSpan.FromMilliseconds(70L)
	};

	private readonly Dictionary<string, MixerRow> _mixerRows = new Dictionary<string, MixerRow>(StringComparer.OrdinalIgnoreCase);

	private readonly Dictionary<string, MixerChange> _mixerChanges = new Dictionary<string, MixerChange>(StringComparer.OrdinalIgnoreCase);

	private bool _mixerExpanded;

	private bool _mixerReading;

	private bool _mixerWriting;

	private bool _mixerUpdatingControls;

	private bool _mixerClosed;

	private DateTimeOffset _lastMixerEdit;

	private readonly DevicePickerWindow _devicePicker = new DevicePickerWindow();

	private readonly PopupRevealAnimation _reveal;

	private bool _refreshing;

	private bool _closed;

	private long _generation;

	private PixelRect _anchor;

	private Func<bool>? _isGameSleepEnabled;

	private Action<bool>? _setGameSleepEnabled;

	private Action? _showGlassDeskSettings;

	public event EventHandler<MediaSessionSnapshot>? MediaSnapshotChanged;

	private void InitializeApplicationMixer()
	{
		_mixerRefreshTimer.Tick += (object? _, EventArgs _) =>
		{
			RefreshApplicationMixerAsync();
		};
		_mixerWriteTimer.Tick += (object? _, EventArgs _) =>
		{
			WriteMixerChangesAsync();
		};
	}

	private void OnApplicationMixerClick(object sender, RoutedEventArgs e)
	{
		SetMixerExpanded(!_mixerExpanded);
		if (_mixerExpanded)
		{
			RefreshApplicationMixerAsync();
		}
	}

	private void SetMixerExpanded(bool expanded)
	{
		_mixerExpanded = expanded;
		ApplicationMixerPanel.Visibility = ((!expanded) ? Visibility.Collapsed : Visibility.Visible);
		ApplicationMixerArrow.Text = (expanded ? "\ue70e" : "\ue70d");
		ResizeForMixer();
		if (expanded && IsVisible)
		{
			_mixerRefreshTimer.Start();
		}
		else
		{
			_mixerRefreshTimer.Stop();
		}
	}

	private void ResizeForMixer()
	{
		PrimaryDisplaySession primaryDisplaySession = PrimaryDisplaySession;
		double value = ((primaryDisplaySession != null && primaryDisplaySession.IsAvailable) ? ((double)primaryDisplaySession.Display.WorkArea.Height * 96.0 / (double)primaryDisplaySession.Display.Dpi - 430.0 - 32.0) : 180.0);
		double num = (_mixerExpanded ? Math.Clamp(value, 64.0, 180.0) : 0.0);
		ApplicationMixerRow.Height = new GridLength(num);
		BeginAnimation(FrameworkElement.HeightProperty, null);
		Height = 430.0 + num;
		PositionNearAnchor();
		if (IsVisible)
		{
			GlassSurface.RequestBackdropRefresh(revealing: true);
		}
	}

	private async Task RefreshApplicationMixerAsync()
	{
		if (_mixerClosed || !_mixerExpanded || !IsVisible || _mixerReading || _mixerWriting || _mixerChanges.Count > 0 || ApplicationMixerPanel.IsMouseCaptureWithin || DateTimeOffset.Now - _lastMixerEdit < TimeSpan.FromSeconds(1L))
		{
			return;
		}
		_mixerReading = true;
		DateTimeOffset startedAt = DateTimeOffset.Now;
		try
		{
			ApplicationVolumeSnapshot applicationVolumeSnapshot = await Task.Run((Func<ApplicationVolumeSnapshot>)ApplicationVolumeController.Read);
			if (_mixerClosed || !_mixerExpanded || !IsVisible || _lastMixerEdit >= startedAt || ApplicationMixerPanel.IsMouseCaptureWithin)
			{
				return;
			}
			_mixerUpdatingControls = true;
			try
			{
				HashSet<string> keys = applicationVolumeSnapshot.Applications.Select((ApplicationVolume app) => app.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
				string[] array = _mixerRows.Keys.Where((string item) => !keys.Contains(item)).ToArray();
				foreach (string key in array)
				{
					ApplicationMixerRows.Children.Remove(_mixerRows[key].Root);
					_mixerRows.Remove(key);
				}
				foreach (ApplicationVolume application in applicationVolumeSnapshot.Applications)
				{
					if (!_mixerRows.TryGetValue(application.Key, out MixerRow value))
					{
						value = CreateMixerRow(application);
						_mixerRows.Add(application.Key, value);
						ApplicationMixerRows.Children.Add(value.Root);
					}
					value.Application = application;
					value.Slider.Value = application.VolumePercent;
					UpdateMixerRow(value);
				}
				ApplicationMixerMessage.Text = ((!applicationVolumeSnapshot.Available) ? "音频设备暂不可用" : "暂无应用音频，播放声音后会自动出现");
				ApplicationMixerMessage.Visibility = ((applicationVolumeSnapshot.Applications.Count != 0) ? Visibility.Collapsed : Visibility.Visible);
			}
			finally
			{
				_mixerUpdatingControls = false;
			}
		}
		finally
		{
			_mixerReading = false;
		}
	}

	private MixerRow CreateMixerRow(ApplicationVolume app)
	{
		Grid grid = new Grid
		{
			Margin = new Thickness(3.0, 6.0, 3.0, 6.0)
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(28.0)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(34.0)
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(19.0)
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(26.0)
		});
		Grid grid2 = new Grid
		{
			Width = 23.0,
			Height = 23.0,
			ToolTip = app.Name
		};
		grid2.Children.Add(new TextBlock
		{
			Text = "\ue7f4",
			FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
			Foreground = ForegroundBrush,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center
		});
		Grid.SetRowSpan(grid2, 2);
		grid.Children.Add(grid2);
		TextBlock element = new TextBlock
		{
			Text = app.Name,
			FontSize = 10.5,
			Foreground = ForegroundBrush,
			TextTrimming = TextTrimming.CharacterEllipsis,
			ToolTip = app.Name,
			Margin = new Thickness(7.0, 0.0, 0.0, 0.0)
		};
		Grid.SetColumn(element, 1);
		Grid.SetColumnSpan(element, 2);
		grid.Children.Add(element);
		Slider slider = new Slider
		{
			Minimum = 0.0,
			Maximum = 100.0,
			Value = app.VolumePercent,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(7.0, 0.0, 7.0, 0.0)
		};
		AutomationProperties.SetName(slider, app.Name + " 音量");
		Grid.SetRow(slider, 1);
		Grid.SetColumn(slider, 1);
		grid.Children.Add(slider);
		TextBlock textBlock = new TextBlock
		{
			FontSize = 10.0,
			Foreground = ForegroundBrush,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Right
		};
		Grid.SetColumn(textBlock, 2);
		Grid.SetRow(textBlock, 1);
		grid.Children.Add(textBlock);
		MixerRow row = new MixerRow(app, grid, slider, textBlock);
		grid.Children.Remove(grid2);
		Button button = new Button
		{
			Content = grid2,
			Background = Brushes.Transparent,
			BorderThickness = new Thickness(0.0),
			Padding = new Thickness(0.0),
			Cursor = Cursors.Hand,
			ToolTip = app.Name + "：静音/取消静音",
			FocusVisualStyle = null
		};
		Grid.SetRowSpan(button, 2);
		grid.Children.Add(button);
		button.Click += (object _, RoutedEventArgs _) =>
		{
			row.Application = row.Application with
			{
				IsMuted = !row.Application.IsMuted
			};
			QueueMixerChange(row, null);
		};
		slider.ValueChanged += (object _, RoutedPropertyChangedEventArgs<double> args) =>
		{
			if (!_mixerUpdatingControls)
			{
				row.Application = row.Application with
				{
					VolumePercent = args.NewValue,
					IsMuted = (!(args.NewValue > 0.0) && row.Application.IsMuted)
				};
				QueueMixerChange(row, args.NewValue);
			}
		};
		LoadMixerIconAsync(grid2, app.ExecutablePath);
		return row;
	}

	private static async Task LoadMixerIconAsync(Grid panel, string? path)
	{
		if (!string.IsNullOrEmpty(path))
		{
			ExtractedIcon extractedIcon = await Task.Run(() => IconExtractor.Extract(path));
			if ((object)extractedIcon != null)
			{
				BitmapSource bitmapSource = BitmapSource.Create(extractedIcon.Width, extractedIcon.Height, 96.0, 96.0, PixelFormats.Bgra32, null, extractedIcon.Bgra, extractedIcon.Width * 4);
				bitmapSource.Freeze();
				panel.Children.Clear();
				panel.Children.Add(new Image
				{
					Source = bitmapSource,
					Width = 21.0,
					Height = 21.0
				});
			}
		}
	}

	private static void UpdateMixerRow(MixerRow row)
	{
		row.Label.Text = (row.Application.IsMuted ? "静音" : $"{row.Application.VolumePercent:0}%");
		row.Slider.Opacity = (row.Application.IsMuted ? 0.55 : 1.0);
	}

	private void QueueMixerChange(MixerRow row, double? volume)
	{
		if (_mixerChanges.TryGetValue(row.Application.Key, out MixerChange value))
		{
			double? num = volume;
			if (!num.HasValue)
			{
				volume = value.Volume;
			}
		}
		_mixerChanges[row.Application.Key] = new MixerChange(row.Application, volume);
		_lastMixerEdit = DateTimeOffset.Now;
		UpdateMixerRow(row);
		_mixerWriteTimer.Start();
	}

	private async Task WriteMixerChangesAsync()
	{
		if (_mixerClosed || _mixerWriting || _mixerChanges.Count == 0)
		{
			return;
		}
		_mixerWriteTimer.Stop();
		_mixerWriting = true;
		MixerChange[] pending = _mixerChanges.Values.ToArray();
		_mixerChanges.Clear();
		try
		{
			bool flag = await Task.Run(() =>
			{
				bool flag2 = true;
				MixerChange[] array = pending;
				foreach (MixerChange mixerChange in array)
				{
					double? volume = mixerChange.Volume;
					if (volume.HasValue)
					{
						double valueOrDefault = volume.GetValueOrDefault();
						flag2 &= ApplicationVolumeController.SetVolume(mixerChange.Application, valueOrDefault);
					}
					flag2 &= ApplicationVolumeController.SetMuted(mixerChange.Application, mixerChange.Application.IsMuted);
				}
				return flag2;
			});
			if (!_mixerClosed && !flag)
			{
				ApplicationMixerMessage.Text = "应用已退出或输出设备已变化，正在刷新列表";
				ApplicationMixerMessage.Visibility = Visibility.Visible;
			}
		}
		finally
		{
			_mixerWriting = false;
			if (!_mixerClosed && _mixerChanges.Count > 0)
			{
				_mixerWriteTimer.Start();
			}
		}
	}

	public ControlCenterWindow()
	{
		InitializeComponent();
		InitializeApplicationMixer();
		_reveal = new PopupRevealAnimation(this, PopupContent);
		PanelCornerRadius = new CornerRadius(30.0);
		GlassSurface.IsBackdropActive = () => IsVisible && !_closed;
		MediaPlayer.SnapshotChanged += (object? _, MediaSessionSnapshot snapshot) =>
		{
			MediaSnapshotChanged?.Invoke(this, snapshot);
		};
		_devicePicker.IsVisibleChanged += (object _, DependencyPropertyChangedEventArgs _) =>
		{
			UpdateArrows();
		};
		_devicePicker.OutputChanged += (object? _, EventArgs _) =>
		{
			if (!_closed && IsVisible)
			{
				RefreshAsync();
			}
		};
		Deactivated += (object? _, EventArgs _) =>
		{
			Hide();
		};
		IsVisibleChanged += (object _, DependencyPropertyChangedEventArgs _) =>
		{
			if (!IsVisible)
			{
				_generation++;
				_devicePicker.Hide();
				_mixerRefreshTimer.Stop();
			}
			else if (_mixerExpanded)
			{
				_mixerRefreshTimer.Start();
				RefreshApplicationMixerAsync();
			}
			UpdateArrows();
		};
		PreviewKeyDown += (object _, KeyEventArgs e) =>
		{
			if (e.Key == Key.Escape)
			{
				if (_devicePicker.IsVisible)
				{
					_devicePicker.Hide();
				}
				else
				{
					Hide();
				}
				e.Handled = true;
			}
		};
	}

	protected override double ResolveCornerRadius(AppearanceSettings appearance)
	{
		return 30.0;
	}

	internal void StartMedia()
	{
		MediaPlayer.Start();
	}

	internal void StopMedia()
	{
		MediaPlayer.Stop();
	}

	internal void ConfigureGameSleep(Func<bool> isEnabled, Action<bool> setEnabled)
	{
		_isGameSleepEnabled = isEnabled;
		_setGameSleepEnabled = setEnabled;
		RefreshGameSleepControl();
	}

	internal void ConfigureSettings(Action showSettings)
	{
		_showGlassDeskSettings = showSettings;
		SettingsButton.IsEnabled = true;
	}

	internal void RequestGlassRefresh()
	{
		if (IsVisible)
		{
			GlassSurface.RequestBackdropRefresh();
		}
		_devicePicker.RequestGlassRefresh();
	}

	public void ShowNear(PixelRect anchor)
	{
		if (_closed)
		{
			return;
		}
		_anchor = anchor;
		bool flag = !IsVisible;
		if (flag)
		{
			new WindowInteropHelper(this).EnsureHandle();
		}
		PixelRect pixelRect = PositionNearAnchor();
		if (flag)
		{
			PrimaryDisplaySession primaryDisplaySession = PrimaryDisplaySession;
			if (primaryDisplaySession != null && primaryDisplaySession.IsAvailable)
			{
				GlassSurface.PrepareRevealMaterial(pixelRect, pixelRect, primaryDisplaySession.Display.Dpi);
			}
			_reveal.Prepare();
			Show();
		}
		Activate();
		GlassSurface.RequestBackdropRefresh(revealing: true);
		RefreshAsync();
	}

	private PixelRect PositionNearAnchor()
	{
		PrimaryDisplaySession primaryDisplaySession = PrimaryDisplaySession;
		if (primaryDisplaySession == null || !primaryDisplaySession.IsAvailable)
		{
			return default;
		}
		DisplayInfo display = primaryDisplaySession.Display;
		int num = display.ToPixels(Width);
		int num2 = display.ToPixels(Height);
		int num3 = display.ToPixels(16.0);
		int num4 = _anchor.Bottom + num3;
		int num5 = _anchor.Y - num3 - num2;
		int y = ((num4 + num2 <= display.WorkArea.Bottom) ? num4 : num5);
		return PlaceWindowWithinPrimaryWorkArea(new PixelRect(_anchor.Right - num, y, num, num2));
	}

	private async Task RefreshAsync()
	{
		long generation = ++_generation;
		Task<(SystemStatusSnapshot Status, AudioOutputDevice Output)> statusTask = Task.Run(() => (Status: SystemStatusReader.Read(), Output: AudioOutputController.GetDevices().FirstOrDefault((AudioOutputDevice device) => device.IsDefault)));
		Task<SystemRadioState> wifiTask = SystemRadioController.ReadWifiAsync();
		Task<SystemRadioState> bluetoothTask = SystemRadioController.ReadBluetoothAsync();
		InlineArray3<Task> buffer = default;
		buffer[0] = statusTask;
		buffer[1] = wifiTask;
		buffer[2] = bluetoothTask;
		await Task.WhenAll(buffer);
		if (_closed || Dispatcher.HasShutdownStarted)
		{
			return;
		}
		await Dispatcher.InvokeAsync(() =>
		{
			if (_closed || !IsVisible || generation != _generation)
			{
				return;
			}
			_refreshing = true;
			try
			{
				RefreshGameSleepControl();
				var (systemStatusSnapshot, audioOutputDevice) = statusTask.Result;
				VolumeSlider.Value = systemStatusSnapshot.VolumePercent.GetValueOrDefault();
				TextBlock volumeValueText = VolumeValueText;
				int? volumePercent = systemStatusSnapshot.VolumePercent;
				string text;
				if (volumePercent.HasValue)
				{
					int valueOrDefault = volumePercent.GetValueOrDefault();
					text = $"{valueOrDefault}%";
				}
				else
				{
					text = "--";
				}
				volumeValueText.Text = text;
				VolumeGlyph.Text = (systemStatusSnapshot.IsMuted ? "\ue74f" : "\ue767");
				OutputDeviceNameText.Text = audioOutputDevice?.Name ?? "没有可用的输出设备";
				OutputDeviceNameText.ToolTip = OutputDeviceNameText.Text;
				ApplyRadioState(wifiTask.Result, WifiStateText, WifiIconBubble);
				ApplyRadioState(bluetoothTask.Result, BluetoothStateText, BluetoothIconBubble);
			}
			finally
			{
				_refreshing = false;
			}
		});
	}

	private static void ApplyRadioState(SystemRadioState state, TextBlock label, Border bubble)
	{
		TextBlock textBlock = label;
		string text;
		if (state.Available)
		{
			text = (state.IsOn ? "已开启" : "已关闭");
		}
		else
		{
			text = "打开设置";
		}
		textBlock.Text = text;
		bubble.Background = new SolidColorBrush((state.Available && state.IsOn) ? Color.FromRgb(65, 145, byte.MaxValue) : Color.FromArgb(48, byte.MaxValue, byte.MaxValue, byte.MaxValue));
	}

	private void OnGameSleepClick(object sender, RoutedEventArgs e)
	{
		if (_isGameSleepEnabled != null && _setGameSleepEnabled != null)
		{
			_setGameSleepEnabled(!_isGameSleepEnabled());
			RefreshGameSleepControl();
		}
	}

	private void OnSettingsClick(object sender, RoutedEventArgs e)
	{
		_showGlassDeskSettings?.Invoke();
	}

	private void RefreshGameSleepControl()
	{
		bool flag = _isGameSleepEnabled != null && _setGameSleepEnabled != null;
		bool flag2 = flag && _isGameSleepEnabled();
		GameSleepButton.IsEnabled = flag;
		TextBlock gameSleepStateText = GameSleepStateText;
		string text;
		if (flag)
		{
			text = (flag2 ? "已开启 · 快捷键或托盘菜单恢复" : "开启后隐藏桌面组件");
		}
		else
		{
			text = "当前不可用";
		}
		gameSleepStateText.Text = text;
		GameSleepIconBubble.Background = new SolidColorBrush(flag2 ? Color.FromRgb(81, 156, byte.MaxValue) : Color.FromArgb(48, byte.MaxValue, byte.MaxValue, byte.MaxValue));
	}

	private void OnWifiClick(object sender, RoutedEventArgs e)
	{
		TogglePicker(DevicePickerKind.Wifi, WifiButton);
	}

	private void OnBluetoothClick(object sender, RoutedEventArgs e)
	{
		TogglePicker(DevicePickerKind.Bluetooth, BluetoothButton);
	}

	private void OnOutputDeviceClick(object sender, RoutedEventArgs e)
	{
		TogglePicker(DevicePickerKind.AudioOutput, OutputDeviceArrowButton);
	}

	private void TogglePicker(DevicePickerKind kind, FrameworkElement button)
	{
		if (_closed || !IsVisible)
		{
			return;
		}
		PrimaryDisplaySession primaryDisplaySession = PrimaryDisplaySession;
		if (primaryDisplaySession == null || !primaryDisplaySession.IsAvailable)
		{
			return;
		}
		if (_devicePicker.IsVisible && _devicePicker.Kind == kind)
		{
			_devicePicker.Hide();
			return;
		}
		if (_devicePicker.PrimaryDisplaySession == null)
		{
			_devicePicker.BindPrimaryDisplay(primaryDisplaySession);
		}
		DevicePickerWindow devicePicker = _devicePicker;
		if (devicePicker.Owner == null)
		{
			Window window = (devicePicker.Owner = this);
		}
		_devicePicker.Icon = Icon;
		Point point = button.PointToScreen(new Point(0.0, button.ActualHeight));
		_devicePicker.ShowNear(kind, new PixelRect((int)point.X, (int)point.Y, (int)Math.Max(1.0, button.ActualWidth * (double)primaryDisplaySession.Display.Dpi / 96.0), 0));
		UpdateArrows();
	}

	private void UpdateArrows()
	{
		UpdateArrow(WifiButton, WifiArrowGlyph, DevicePickerKind.Wifi, "Wi-Fi 网络");
		UpdateArrow(BluetoothButton, BluetoothArrowGlyph, DevicePickerKind.Bluetooth, "蓝牙设备");
		UpdateArrow(OutputDeviceArrowButton, OutputDeviceArrowGlyph, DevicePickerKind.AudioOutput, "输出设备");
	}

	private void UpdateArrow(Button button, TextBlock glyph, DevicePickerKind kind, string label)
	{
		bool flag = _devicePicker.IsVisible && _devicePicker.Kind == kind;
		glyph.Text = (flag ? "\ue70e" : "\ue70d");
		button.ToolTip = (flag ? "收起" : "展开") + label;
		AutomationProperties.SetName(button, (string)button.ToolTip);
	}

	private void OnVolumeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		if (!_refreshing)
		{
			int num = (int)Math.Round(e.NewValue);
			SystemStatusReader.SetVolumePercent(num);
			if (num > 0)
			{
				SystemStatusReader.SetMuted(muted: false);
			}
			VolumeValueText.Text = $"{num}%";
			VolumeGlyph.Text = ((num == 0) ? "\ue74f" : "\ue767");
		}
	}

	private void OnMuteClick(object sender, RoutedEventArgs e)
	{
		SystemStatusReader.SetMuted(!SystemStatusReader.Read().IsMuted);
		RefreshAsync();
	}

	protected override void OnClosed(EventArgs e)
	{
		_closed = true;
		_generation++;
		_mixerClosed = true;
		_mixerRefreshTimer.Stop();
		_mixerWriteTimer.Stop();
		_devicePicker.Close();
		MediaPlayer.Dispose();
		GlassSurface.Dispose();
		base.OnClosed(e);
	}

}
