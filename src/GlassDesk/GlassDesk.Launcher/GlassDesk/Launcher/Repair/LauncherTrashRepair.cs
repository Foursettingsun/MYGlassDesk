using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace GlassDesk.Launcher.Repair;

public static class LauncherTrashRepair
{
	private sealed class Placement
	{
		public Border Trash;

		public Grid Content;

		public FrameworkElement SearchHeader;

		public Thickness OriginalHeaderMargin;

		public DependencyPropertyDescriptor VisibilityDescriptor;

		public void VisibilityChanged(object sender, EventArgs args)
		{
			UpdateHeader();
		}

		public void SizeChanged(object sender, SizeChangedEventArgs args)
		{
			UpdateHeader();
		}

		public void Closed(object sender, EventArgs args)
		{
			VisibilityDescriptor.RemoveValueChanged(Trash, VisibilityChanged);
			Content.SizeChanged -= SizeChanged;
		}

		public void UpdateHeader()
		{
			double num = (double.IsFinite(SearchHeader.MaxWidth) ? SearchHeader.MaxWidth : 600.0);
			bool num2 = Trash.Visibility == Visibility.Visible && Content.ActualWidth < num + 100.0;
			Thickness originalHeaderMargin = OriginalHeaderMargin;
			Thickness thickness = (num2 ? new Thickness(originalHeaderMargin.Left + 50.0, originalHeaderMargin.Top, originalHeaderMargin.Right, originalHeaderMargin.Bottom) : originalHeaderMargin);
			if (SearchHeader.Margin != thickness)
			{
				SearchHeader.Margin = thickness;
			}
		}
	}

	public static void Configure(object window)
	{
		Border border = (Border)Read(window, "TrashDropTarget");
		Grid grid = (Grid)Read(window, "LauncherContent");
		FrameworkElement frameworkElement = (FrameworkElement)Read(window, "SearchHeader");
		Placement placement = new Placement
		{
			Trash = border,
			Content = grid,
			SearchHeader = frameworkElement,
			OriginalHeaderMargin = frameworkElement.Margin
		};
		if (border.Parent is Panel panel)
		{
			panel.Children.Remove(border);
			border.Width = 38.0;
			border.Height = 29.0;
			border.MinWidth = 0.0;
			border.MinHeight = 0.0;
			border.AllowDrop = true;
			border.HorizontalAlignment = HorizontalAlignment.Left;
			border.VerticalAlignment = VerticalAlignment.Top;
			border.Margin = new Thickness(0.0, 3.0, 0.0, 0.0);
			CornerRadius cornerRadius = border.CornerRadius;
			border.CornerRadius = new CornerRadius(cornerRadius.TopLeft * 0.5, cornerRadius.TopRight * 0.5, cornerRadius.BottomRight * 0.5, cornerRadius.BottomLeft * 0.5);
			Thickness padding = border.Padding;
			border.Padding = new Thickness(padding.Left * 0.5, padding.Top * 0.5, padding.Right * 0.5, padding.Bottom * 0.5);
			if (border.Child is Viewbox viewbox)
			{
				viewbox.Width = 14.0;
				viewbox.Height = 14.0;
			}
			Grid.SetRow(border, 0);
			Grid.SetColumn(border, 0);
			Grid.SetRowSpan(border, 1);
			Grid.SetColumnSpan(border, Math.Max(1, grid.ColumnDefinitions.Count));
			Panel.SetZIndex(border, 100);
			grid.Children.Add(border);
			placement.VisibilityDescriptor = DependencyPropertyDescriptor.FromProperty(UIElement.VisibilityProperty, typeof(Border));
			placement.VisibilityDescriptor.AddValueChanged(border, placement.VisibilityChanged);
			grid.SizeChanged += placement.SizeChanged;
			((Window)window).Closed += placement.Closed;
			placement.UpdateHeader();
			return;
		}
		throw new InvalidOperationException("Unexpected launcher trash parent.");
	}

	private static object Read(object window, string name)
	{
		return window.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(window);
	}
}
