using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GlassDesk.Core.Imaging;
using GlassDesk.Platform.Shell;
using GlassDesk.UI.Controls;

namespace GlassDesk.Launcher;

public sealed class DesktopImportWindow : GlassWindow
{
	private readonly List<(DesktopItem Item, CheckBox Choice, Image Icon)> _rows = new List<(DesktopItem, CheckBox, Image)>();

	private readonly TextBlock _selectionCount = new TextBlock
	{
		VerticalAlignment = VerticalAlignment.Center
	};

	private readonly Button _add = new Button
	{
		Content = "添加所选",
		MinWidth = 100.0,
		IsDefault = true,
		IsEnabled = false
	};

	private LauncherIconLoader? _icons;

	private bool _changingSelection;

	private bool _closed;

	public IReadOnlyList<string> SelectedPaths => (from row in _rows
		where row.Choice.IsChecked == true
		select row.Item.Path).ToArray();

	public DesktopImportWindow(DesktopScanResult scan)
	{
		ArgumentNullException.ThrowIfNull(scan, "scan");
		base.Title = "从桌面批量添加";
		AutomationProperties.SetName(this, base.Title);
		base.Width = 600.0;
		base.Height = 560.0;
		base.MinWidth = 420.0;
		base.MinHeight = 320.0;
		base.MaxHeight = Math.Max(base.MinHeight, SystemParameters.WorkArea.Height - 32.0);
		base.WindowStartupLocation = WindowStartupLocation.CenterOwner;
		base.ShowInTaskbar = false;
		NameScope.SetNameScope(this, new NameScope());
		DockPanel dockPanel = new DockPanel
		{
			Margin = new Thickness(22.0)
		};
		StackPanel stackPanel = new StackPanel
		{
			Children = 
			{
				(UIElement)new TextBlock
				{
					Text = base.Title,
					FontSize = 20.0,
					FontWeight = FontWeights.SemiBold
				},
				(UIElement)new TextBlock
				{
					Text = "勾选要添加的桌面文件、程序、快捷方式或文件夹。",
					TextWrapping = TextWrapping.Wrap,
					Margin = new Thickness(0.0, 8.0, 0.0, 14.0),
					Opacity = 0.8
				}
			}
		};
		DockPanel dockPanel2 = new DockPanel
		{
			Margin = new Thickness(0.0, 0.0, 0.0, 12.0)
		};
		DockPanel.SetDock(_selectionCount, Dock.Right);
		dockPanel2.Children.Add(_selectionCount);
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal
		};
		Button button = new Button
		{
			Content = "全选",
			MinWidth = 66.0,
			Margin = new Thickness(0.0, 0.0, 8.0, 0.0)
		};
		Button button2 = new Button
		{
			Content = "取消全选",
			MinWidth = 82.0
		};
		RegisterName("SelectAllButton", button);
		RegisterName("ClearSelectionButton", button2);
		button.Click += delegate
		{
			SelectAll(selected: true);
		};
		button2.Click += delegate
		{
			SelectAll(selected: false);
		};
		stackPanel2.Children.Add(button);
		stackPanel2.Children.Add(button2);
		dockPanel2.Children.Add(stackPanel2);
		stackPanel.Children.Add(dockPanel2);
		DockPanel.SetDock(stackPanel, Dock.Top);
		dockPanel.Children.Add(stackPanel);
		StackPanel stackPanel3 = new StackPanel();
		if (scan.Warnings.Count > 0)
		{
			stackPanel3.Children.Add(new TextBlock
			{
				Text = "部分桌面项目无法读取，已列出其余可用项目。",
				ToolTip = string.Join(Environment.NewLine, scan.Warnings),
				TextWrapping = TextWrapping.Wrap,
				Margin = new Thickness(0.0, 10.0, 0.0, 0.0)
			});
		}
		StackPanel stackPanel4 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness(0.0, 14.0, 0.0, 0.0)
		};
		Button element = new Button
		{
			Content = "取消",
			MinWidth = 76.0,
			IsCancel = true,
			Margin = new Thickness(0.0, 0.0, 8.0, 0.0)
		};
		RegisterName("AddSelectedButton", _add);
		_add.Click += delegate
		{
			if (SelectedPaths.Count > 0)
			{
				base.DialogResult = true;
			}
		};
		stackPanel4.Children.Add(element);
		stackPanel4.Children.Add(_add);
		stackPanel3.Children.Add(stackPanel4);
		DockPanel.SetDock(stackPanel3, Dock.Bottom);
		dockPanel.Children.Add(stackPanel3);
		ListBox list = new ListBox
		{
			HorizontalContentAlignment = HorizontalAlignment.Stretch
		};
		AutomationProperties.SetName(list, "桌面项目");
		ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Disabled);
		RegisterName("DesktopItemsList", list);
		foreach (DesktopItem item in scan.Items)
		{
			Image image = new Image
			{
				Width = 28.0,
				Height = 28.0,
				Margin = new Thickness(0.0, 0.0, 10.0, 0.0),
				Stretch = Stretch.Uniform
			};
			StackPanel element2 = new StackPanel
			{
				Children = 
				{
					(UIElement)new TextBlock
					{
						Text = item.DisplayName,
						TextTrimming = TextTrimming.CharacterEllipsis
					},
					(UIElement)new TextBlock
					{
						Text = (item.FromCurrentUser ? "个人桌面" : "公共桌面") + (item.IsDirectory ? " · 文件夹" : ""),
						FontSize = 11.0,
						Opacity = 0.65,
						Margin = new Thickness(0.0, 3.0, 0.0, 0.0)
					}
				}
			};
			DockPanel dockPanel3 = new DockPanel();
			DockPanel.SetDock(image, Dock.Left);
			dockPanel3.Children.Add(image);
			dockPanel3.Children.Add(element2);
			CheckBox checkBox = new CheckBox
			{
				Content = dockPanel3,
				ToolTip = item.Path,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				Padding = new Thickness(4.0, 5.0, 4.0, 5.0)
			};
			AutomationProperties.SetName(checkBox, item.DisplayName);
			checkBox.Checked += delegate
			{
				UpdateSelection();
			};
			checkBox.Unchecked += delegate
			{
				UpdateSelection();
			};
			_rows.Add((item, checkBox, image));
			list.Items.Add(checkBox);
		}
		if (_rows.Count == 0)
		{
			dockPanel.Children.Add(new TextBlock
			{
				Text = "桌面上没有可添加的文件或快捷方式。",
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				TextWrapping = TextWrapping.Wrap,
				Opacity = 0.75
			});
		}
		else
		{
			dockPanel.Children.Add(list);
		}
		base.Content = new Grid
		{
			Children = 
			{
				(UIElement)new LiquidGlassSurface
				{
					UseLauncherTransparency = true
				},
				(UIElement)dockPanel
			}
		};
		UpdateSelection();
		base.Loaded += delegate
		{
			list.Focus();
			LoadIcons();
		};
		base.Closed += delegate
		{
			_closed = true;
			_icons?.Dispose();
		};
	}

	private void SelectAll(bool selected)
	{
		_changingSelection = true;
		try
		{
			foreach (var row in _rows)
			{
				row.Choice.IsChecked = selected;
			}
		}
		finally
		{
			_changingSelection = false;
			UpdateSelection();
		}
	}

	private void UpdateSelection()
	{
		if (!_changingSelection)
		{
			int num = _rows.Count<(DesktopItem, CheckBox, Image)>(((DesktopItem Item, CheckBox Choice, Image Icon) row) => row.Choice.IsChecked == true);
			_selectionCount.Text = $"已选 {num} / {_rows.Count} 项";
			_add.IsEnabled = num > 0;
		}
	}

	private void LoadIcons()
	{
		if (_icons != null || _closed || _rows.Count == 0)
		{
			return;
		}
		_icons = new LauncherIconLoader();
		foreach (var row in _rows)
		{
			LoadIconAsync(row.Item.Path, row.Icon);
		}
	}

	private async Task LoadIconAsync(string path, Image host)
	{
		BitmapImage image = await _icons.LoadAsync(path, delegate
		{
			ExtractedIcon extractedIcon = IconExtractor.Extract(path);
			if ((object)extractedIcon == null)
			{
				return (BitmapImage?)null;
			}
			using MemoryStream streamSource = new MemoryStream(PngWriter.EncodeBgra(extractedIcon.Bgra, extractedIcon.Width, extractedIcon.Height));
			BitmapImage bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.StreamSource = streamSource;
			bitmapImage.DecodePixelWidth = 48;
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.EndInit();
			bitmapImage.Freeze();
			return bitmapImage;
		}).ConfigureAwait(continueOnCapturedContext: false);
		if (image == null || host.Dispatcher.HasShutdownStarted)
		{
			return;
		}
		try
		{
			await host.Dispatcher.InvokeAsync(delegate
			{
				if (!_closed)
				{
					host.Source = image;
				}
			}, DispatcherPriority.Background);
		}
		catch (TaskCanceledException)
		{
		}
	}
}
