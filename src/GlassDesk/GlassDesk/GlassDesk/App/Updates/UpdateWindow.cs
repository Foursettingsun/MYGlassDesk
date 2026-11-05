using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using GlassDesk.AppServices.Updates;

namespace GlassDesk.App.Updates;

internal sealed class UpdateWindow : Window, IDisposable
{
	private readonly Func<IProgress<int>, CancellationToken, Task> _install;

	private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();

	private readonly TextBlock _status;

	private readonly ProgressBar _progress;

	private readonly Button _update;

	private readonly Button _later;

	private bool _busy;

	private bool _handedOff;

	private bool _disposed;

	public UpdateWindow(UpdateRelease release, bool canInstall, Func<IProgress<int>, CancellationToken, Task> install)
	{
		_install = install;
		Title = "GlassDesk 更新";
		Width = 520.0;
		Height = 480.0;
		MinWidth = 430.0;
		MinHeight = 350.0;
		WindowStartupLocation = WindowStartupLocation.CenterScreen;
		Background = new SolidColorBrush(Color.FromRgb(23, 26, 33));
		Foreground = new SolidColorBrush(Color.FromRgb(239, 243, 249));
		FontFamily = new FontFamily("Microsoft YaHei UI");
		Grid grid = new Grid
		{
			Margin = new Thickness(26.0)
		};
		GridLength[] array = new GridLength[6]
		{
			GridLength.Auto,
			GridLength.Auto,
			new GridLength(1.0, GridUnitType.Star),
			GridLength.Auto,
			GridLength.Auto,
			GridLength.Auto
		};
		foreach (GridLength height in array)
		{
			grid.RowDefinitions.Add(new RowDefinition
			{
				Height = height
			});
		}
		Add(new TextBlock
		{
			Text = $"发现新版本 {release.Version}",
			FontSize = 24.0,
			FontWeight = FontWeights.SemiBold
		}, 0);
		Add(new TextBlock
		{
			Text = "下载完成后会关闭 GlassDesk，安装更新并重新打开。",
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(0.0, 10.0, 0.0, 16.0)
		}, 1);
		TextBlock textBlock = new TextBlock
		{
			Text = (string.IsNullOrWhiteSpace(release.Notes) ? "此版本未提供更新说明。" : release.Notes),
			TextWrapping = TextWrapping.Wrap,
			LineHeight = 23.0
		};
		AutomationProperties.SetName(textBlock, "更新说明");
		Add(new ScrollViewer
		{
			Content = textBlock,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			Margin = new Thickness(0.0, 0.0, 0.0, 16.0)
		}, 2);
		_status = new TextBlock
		{
			Text = (canInstall ? "准备好后点击“立即更新”。" : "当前程序以便携方式运行或安装登记不匹配。请先手动安装正式安装包。"),
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(0.0, 0.0, 0.0, 8.0)
		};
		AutomationProperties.SetName(_status, "更新下载状态");
		Add(_status, 3);
		_progress = new ProgressBar
		{
			Minimum = 0.0,
			Maximum = 100.0,
			Height = 6.0,
			Visibility = Visibility.Collapsed,
			Margin = new Thickness(0.0, 0.0, 0.0, 18.0)
		};
		AutomationProperties.SetName(_progress, "更新下载进度");
		Add(_progress, 4);
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right
		};
		_later = Button("稍后", "稍后更新 GlassDesk");
		_update = Button("立即更新", "立即更新 GlassDesk");
		_update.IsEnabled = canInstall;
		_later.Click += (object _, RoutedEventArgs _) =>
		{
			Close();
		};
		_update.Click += OnUpdateClick;
		stackPanel.Children.Add(_later);
		stackPanel.Children.Add(_update);
		Add(stackPanel, 5);
		Content = grid;
		Closing += (object? _, CancelEventArgs e) =>
		{
			if (!_handedOff)
			{
				_cancellation.Cancel();
			}
		};
		Closed += (object? _, EventArgs _) =>
		{
			Dispose();
		};
		void Add(UIElement child, int row)
		{
			Grid.SetRow(child, row);
			grid.Children.Add(child);
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			if (!_handedOff)
			{
				_cancellation.Cancel();
			}
			if (!_busy)
			{
				_cancellation.Dispose();
			}
			GC.SuppressFinalize(this);
		}
	}

	public void MarkInstalling()
	{
		_status.Text = "正在启动安装程序，更新完成后会自动重新打开 GlassDesk。";
		_later.IsEnabled = false;
		_handedOff = true;
	}

	private async void OnUpdateClick(object sender, RoutedEventArgs e)
	{
		if (_busy)
		{
			return;
		}
		_busy = true;
		_update.IsEnabled = false;
		_later.Content = "取消下载";
		_progress.Visibility = Visibility.Visible;
		_progress.Value = 0.0;
		_status.Text = "正在下载更新 0%";
		try
		{
			await _install(new Progress<int>((int value) =>
			{
				if (!_handedOff && !_cancellation.IsCancellationRequested)
				{
					_progress.Value = value;
					_status.Text = ((value == 100) ? "下载完成，正在核验安装信息…" : $"正在下载更新 {value}%");
				}
			}), _cancellation.Token);
		}
		catch (OperationCanceledException)
		{
			if (IsVisible)
			{
				_status.Text = "下载已取消或超时，请重试。";
			}
		}
		catch (Exception ex2)
		{
			if (IsVisible)
			{
				_status.Text = "更新失败：" + ex2.Message;
			}
		}
		finally
		{
			_busy = false;
			if (!IsVisible)
			{
				_cancellation.Dispose();
			}
			else if (!_handedOff)
			{
				_update.IsEnabled = true;
				_update.Content = "重试更新";
				_later.Content = "稍后";
			}
		}
	}

	private static Button Button(string text, string automationName)
	{
		Button button = new Button
		{
			Content = text,
			Padding = new Thickness(18.0, 9.0, 18.0, 9.0),
			Margin = new Thickness(8.0, 0.0, 0.0, 0.0),
			Background = new SolidColorBrush(Color.FromRgb(42, 52, 67)),
			Foreground = Brushes.White,
			BorderBrush = new SolidColorBrush(Color.FromRgb(70, 83, 104)),
			MinWidth = 100.0
		};
		AutomationProperties.SetName(button, automationName);
		return button;
	}
}
