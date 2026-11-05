using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace GlassDesk.Settings;

public sealed class ShortcutHelpIcon : Button
{
	public static readonly DependencyProperty HelpTitleProperty = DependencyProperty.Register("HelpTitle", typeof(string), typeof(ShortcutHelpIcon), new PropertyMetadata(string.Empty, OnHelpChanged));

	public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register("Description", typeof(string), typeof(ShortcutHelpIcon), new PropertyMetadata(string.Empty, OnHelpChanged));

	private readonly ToolTip _help = new ToolTip
	{
		Placement = PlacementMode.Bottom,
		HorizontalOffset = 0.0,
		VerticalOffset = 7.0,
		MaxWidth = 340.0
	};

	public string HelpTitle
	{
		get
		{
			return (string)GetValue(HelpTitleProperty);
		}
		set
		{
			SetValue(HelpTitleProperty, value);
		}
	}

	public string Description
	{
		get
		{
			return (string)GetValue(DescriptionProperty);
		}
		set
		{
			SetValue(DescriptionProperty, value);
		}
	}

	public ShortcutHelpIcon()
	{
		SetResourceReference(FrameworkElement.StyleProperty, typeof(Button));
		Width = (Height = 22.0);
		Margin = new Thickness(7.0, 0.0, 0.0, 0.0);
		Padding = new Thickness(0.0);
		Content = "ⓘ";
		FontSize = 16.0;
		Cursor = Cursors.Help;
		Background = Brushes.Transparent;
		BorderThickness = new Thickness(0.0);
		Foreground = new SolidColorBrush(Color.FromRgb(117, 162, 222));
		VerticalAlignment = VerticalAlignment.Center;
		ToolTip = _help;
		ToolTipService.SetInitialShowDelay(this, 250);
		ToolTipService.SetBetweenShowDelay(this, 0);
		ToolTipService.SetShowDuration(this, 45000);
		Click += (object _, RoutedEventArgs _) =>
		{
			_help.PlacementTarget = this;
			_help.IsOpen = true;
		};
		MouseLeave += (object _, MouseEventArgs _) =>
		{
			_help.IsOpen = false;
		};
		LostKeyboardFocus += (object _, KeyboardFocusChangedEventArgs _) =>
		{
			_help.IsOpen = false;
		};
		Unloaded += (object _, RoutedEventArgs _) =>
		{
			_help.IsOpen = false;
		};
		UpdateHelp();
	}

	private static void OnHelpChanged(DependencyObject owner, DependencyPropertyChangedEventArgs e)
	{
		((ShortcutHelpIcon)owner).UpdateHelp();
	}

	private void UpdateHelp()
	{
		TextBlock textBlock = new TextBlock
		{
			TextWrapping = TextWrapping.Wrap,
			MaxWidth = 300.0,
			LineHeight = 21.0,
			FontSize = 13.0
		};
		textBlock.Inlines.Add(new Bold(new Run(HelpTitle)));
		textBlock.Inlines.Add(new LineBreak());
		textBlock.Inlines.Add(new Run(Description));
		textBlock.Inlines.Add(new LineBreak());
		textBlock.Inlines.Add(new LineBreak());
		textBlock.Inlines.Add(new Run("修改方式：点击右侧快捷键，再按新组合。Esc 取消，Backspace / Delete 清除；录制后自动保存。")
		{
			Foreground = new SolidColorBrush(Color.FromRgb(165, 183, 208))
		});
		_help.Content = textBlock;
		_help.Background = new SolidColorBrush(Color.FromRgb(30, 40, 58));
		_help.Foreground = Brushes.White;
		_help.Padding = new Thickness(15.0);
		_help.BorderBrush = new SolidColorBrush(Color.FromRgb(90, 118, 153));
		AutomationProperties.SetName(this, HelpTitle + "：功能说明");
		AutomationProperties.SetHelpText(this, Description);
	}
}
