using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace GlassDesk.App.Updates;

internal sealed class CloudDownloadWindow : Window
{
	private sealed record Download(string Name, Uri Url, string? Code);

	private readonly TextBlock _status;

	private readonly Action<Uri> _openPage;

	private readonly Action<string> _copyText;

	public CloudDownloadWindow(string configuration, Version? version = null, string? notes = null, Action<Uri>? openPage = null, Action<string>? copyText = null, Func<bool>? snoozeNotice = null)
	{
		CloudDownloadWindow cloudDownloadWindow = this;
		ReadOnlyCollection<Download> readOnlyCollection = ParseDownloads(configuration);
		_openPage = openPage ?? ((Action<Uri>)((Uri url) =>
		{
			using (Process.Start(new ProcessStartInfo(url.AbsoluteUri)
			{
				UseShellExecute = true
			}))
			{
			}
		}));
		_copyText = copyText ?? new Action<string>(Clipboard.SetText);
		Title = "GlassDesk 网盘下载";
		Width = 560.0;
		Height = (((object)version == null) ? 600 : 720);
		MinWidth = 450.0;
		MinHeight = 400.0;
		WindowStartupLocation = WindowStartupLocation.CenterScreen;
		Background = Brush(23, 26, 33);
		Foreground = Brush(239, 243, 249);
		FontFamily = new FontFamily("Microsoft YaHei UI");
		Grid grid = new Grid
		{
			Margin = new Thickness(26.0)
		};
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
		StackPanel stackPanel = new StackPanel
		{
			Margin = new Thickness(0.0, 0.0, 0.0, 18.0)
		};
		TextBlock textBlock = Text(((object)version == null) ? "选择下载网盘" : $"发现新版本 {version}", 24.0);
		textBlock.FontWeight = FontWeights.SemiBold;
		AutomationProperties.SetName(textBlock, "网盘下载标题");
		stackPanel.Children.Add(textBlock);
		stackPanel.Children.Add(Text("打开网盘页面后，可自行转存或下载，再手动运行安装包。", 13.0, muted: true, 10.0));
		Add(stackPanel, 0);
		StackPanel stackPanel2 = new StackPanel();
		if (!string.IsNullOrWhiteSpace(notes))
		{
			stackPanel2.Children.Add(Text("更新说明", 16.0));
			TextBlock textBlock2 = Text((notes.Length > 12000) ? notes.Substring(0, 12000) : notes, 13.0, muted: true, 8.0);
			AutomationProperties.SetName(textBlock2, "网盘更新说明");
			stackPanel2.Children.Add(new ScrollViewer
			{
				Content = textBlock2,
				MaxHeight = 132.0,
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
				Margin = new Thickness(0.0, 0.0, 0.0, 20.0)
			});
		}
		stackPanel2.Children.Add(Text("请选择网盘，并在页面中核对安装包版本。", 13.0, muted: true));
		foreach (Download download in readOnlyCollection)
		{
			StackPanel stackPanel3 = new StackPanel();
			TextBlock textBlock3 = Text(download.Name, 17.0);
			textBlock3.FontWeight = FontWeights.SemiBold;
			stackPanel3.Children.Add(textBlock3);
			stackPanel3.Children.Add(Text((download.Code == null) ? "可在网盘页面转存或下载" : ("提取码：" + download.Code), 13.0, muted: true, 6.0));
			WrapPanel wrapPanel = new WrapPanel
			{
				Margin = new Thickness(0.0, 14.0, 0.0, 0.0)
			};
			Button button = Button("打开网盘", "打开" + download.Name);
			button.Background = Brush(55, 104, 190);
			button.Click += (object _, RoutedEventArgs _) =>
			{
				cloudDownloadWindow.RunAction(() =>
				{
					cloudDownloadWindow._openPage(download.Url);
				}, "已打开" + download.Name + "，请在浏览器中继续。", "无法打开网盘：");
			};
			wrapPanel.Children.Add(button);
			if (download.Code != null)
			{
				Button button2 = Button("复制提取码", "复制" + download.Name + "提取码");
				button2.Click += (object _, RoutedEventArgs _) =>
				{
					cloudDownloadWindow.RunAction(() =>
					{
						cloudDownloadWindow._copyText(download.Code);
					}, "已复制" + download.Name + "提取码。", "无法复制，可使用上方提取码：");
				};
				wrapPanel.Children.Add(button2);
			}
			stackPanel3.Children.Add(wrapPanel);
			stackPanel2.Children.Add(new Border
			{
				Child = stackPanel3,
				CornerRadius = new CornerRadius(12.0),
				Padding = new Thickness(16.0),
				Margin = new Thickness(0.0, 12.0, 0.0, 0.0),
				Background = Brush(32, 38, 49),
				BorderBrush = Brush(57, 68, 86),
				BorderThickness = new Thickness(1.0)
			});
		}
		ScrollViewer scrollViewer = new ScrollViewer
		{
			Content = stackPanel2,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
		};
		AutomationProperties.SetName(scrollViewer, "网盘下载内容");
		Add(scrollViewer, 1);
		_status = Text("请选择一个网盘。打开页面不会关闭 GlassDesk。", 12.0, muted: true, 18.0);
		AutomationProperties.SetName(_status, "网盘下载状态");
		StackPanel stackPanel4 = new StackPanel
		{
			Children = { (UIElement)_status }
		};
		if ((object)version != null && snoozeNotice != null)
		{
			Button button3 = Button("7 天内不再弹出", "7 天内不再弹出更新公告");
			button3.Margin = new Thickness(0.0, 12.0, 0.0, 0.0);
			button3.HorizontalAlignment = HorizontalAlignment.Right;
			button3.Click += (object _, RoutedEventArgs _) =>
			{
				try
				{
					if (snoozeNotice())
					{
						cloudDownloadWindow.Close();
					}
					else
					{
						cloudDownloadWindow._status.Text = "未能保存提醒设置，请重试。";
					}
				}
				catch (Exception ex)
				{
					cloudDownloadWindow._status.Text = "未能保存提醒设置：" + ex.Message;
				}
			};
			stackPanel4.Children.Add(button3);
		}
		Add(stackPanel4, 2);
		Content = grid;
		void Add(UIElement child, int row)
		{
			Grid.SetRow(child, row);
			grid.Children.Add(child);
		}
	}

	private void RunAction(Action action, string success, string failure)
	{
		try
		{
			action();
			_status.Text = success;
		}
		catch (Exception ex)
		{
			_status.Text = failure + ex.Message;
		}
	}

	private static ReadOnlyCollection<Download> ParseDownloads(string configuration)
	{
		try
		{
			if (configuration.Length > 8192)
			{
				throw new InvalidDataException("网盘下载配置过大。");
			}
			using JsonDocument jsonDocument = JsonDocument.Parse(configuration);
			JsonElement property = jsonDocument.RootElement.GetProperty("cloudDownloads");
			int arrayLength = property.GetArrayLength();
			if ((arrayLength < 1 || arrayLength > 8) ? true : false)
			{
				throw new InvalidDataException("请配置 1 到 8 个网盘下载入口。");
			}
			List<Download> list = new List<Download>();
			foreach (JsonElement item in property.EnumerateArray())
			{
				string name = item.GetProperty("name").GetString()?.Trim();
				string text = item.GetProperty("url").GetString()?.Trim();
				string text2 = ((!item.TryGetProperty("extractionCode", out var value)) ? null : value.GetString()?.Trim());
				if (string.IsNullOrWhiteSpace(text2))
				{
					text2 = null;
				}
				if (string.IsNullOrWhiteSpace(name) || name.Length > 32 || name.Any(char.IsControl) || text == null || text.Length > 2048 || !Uri.TryCreate(text, UriKind.Absolute, out Uri uri) || uri.Scheme != "https" || !uri.IsDefaultPort || uri.UserInfo.Length != 0 || uri.Fragment.Length != 0 || (text2 != null && text2.Length > 64) || (text2 != null && text2.Any(char.IsControl)) || list.Any((Download x) => x.Name == name || x.Url == uri))
				{
					throw new InvalidDataException("网盘名称、HTTPS 分享链接或提取码无效。");
				}
				list.Add(new Download(name, uri, text2));
			}
			return list.AsReadOnly();
		}
		catch (Exception ex) when ((ex is JsonException || ex is KeyNotFoundException || ex is InvalidOperationException) ? true : false)
		{
			throw new InvalidDataException("网盘下载配置格式无效。", ex);
		}
	}

	private static SolidColorBrush Brush(byte r, byte g, byte b)
	{
		return new SolidColorBrush(Color.FromRgb(r, g, b));
	}

	private static TextBlock Text(string text, double size, bool muted = false, double top = 0.0)
	{
		return new TextBlock
		{
			Text = text,
			FontSize = size,
			TextWrapping = TextWrapping.Wrap,
			LineHeight = size * 1.65,
			Foreground = (muted ? Brush(173, 187, 209) : Brush(239, 243, 249)),
			Margin = new Thickness(0.0, top, 0.0, 0.0)
		};
	}

	private static Button Button(string text, string automationName)
	{
		Button button = new Button
		{
			Content = text,
			Padding = new Thickness(15.0, 8.0, 15.0, 8.0),
			Margin = new Thickness(0.0, 0.0, 8.0, 4.0),
			Background = Brush(42, 52, 67),
			Foreground = Brushes.White,
			BorderBrush = Brush(70, 83, 104),
			MinWidth = 100.0
		};
		AutomationProperties.SetName(button, automationName);
		return button;
	}
}
