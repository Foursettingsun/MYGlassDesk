using System;
using System.Windows;
using System.Windows.Media;
using GlassDesk.Core.Models;
using GlassDesk.UI.Theming;

namespace GlassDesk.UI.Controls;

public sealed class SharedGlassLens : FrameworkElement
{
	private LiquidGlassSurface? _source;

	public static readonly DependencyProperty HighlightProperty = DependencyProperty.Register("Highlight", typeof(double), typeof(SharedGlassLens), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

	public LiquidGlassSurface? Source
	{
		get
		{
			return _source;
		}
		set
		{
			if (_source != null)
			{
				_source.SharedMaterialChanged -= OnMaterialChanged;
			}
			_source = value;
			if (IsLoaded && _source != null)
			{
				_source.SharedMaterialChanged += OnMaterialChanged;
			}
			InvalidateVisual();
		}
	}

	public double Highlight
	{
		get
		{
			return (double)GetValue(HighlightProperty);
		}
		set
		{
			SetValue(HighlightProperty, value);
		}
	}

	public double Radius { get; set; } = 24.0;

	public SharedGlassLens()
	{
		IsHitTestVisible = false;
		Loaded += (object _, RoutedEventArgs _) =>
		{
			if (_source != null)
			{
				_source.SharedMaterialChanged += OnMaterialChanged;
			}
		};
		Unloaded += (object _, RoutedEventArgs _) =>
		{
			if (_source != null)
			{
				_source.SharedMaterialChanged -= OnMaterialChanged;
			}
		};
	}

	private void OnMaterialChanged(object? sender, EventArgs e)
	{
		if (IsVisible)
		{
			InvalidateVisual();
		}
	}

	protected override void OnRender(DrawingContext dc)
	{
		base.OnRender(dc);
		if (ActualWidth <= 0.0 || ActualHeight <= 0.0)
		{
			return;
		}
		Rect rect = new Rect(RenderSize);
		Geometry geometry = SquircleGlassGeometry.CreateCompact(rect, Radius);
		ThemeService? current = ThemeService.Current;
		if (current != null && current.Appearance.GlassMaterial == GlassMaterialMode.PlainGlass)
		{
			dc.DrawGeometry(new SolidColorBrush(LiquidGlassSurface.PlainGlassColor(launcher: true)), null, geometry);
			return;
		}
		bool flag = ThemeService.Current?.IsDark ?? true;
		ThemeService? current2 = ThemeService.Current;
		bool flag2 = (current2 != null && current2.UseOpaquePanels) || BackdropRecovery.RequiresBasicRendering;
		for (int num = 4; num >= 1; num--)
		{
			dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb((byte)(3 + num), 0, 0, 0)), num * 2), geometry);
		}
		dc.PushClip(geometry);
		if (!flag2)
		{
			LiquidGlassSurface source = _source;
			if (source != null && source.IsLoaded)
			{
				Point location = TranslatePoint(default, _source);
				_source.DrawSharedMaterial(dc, new Rect(location, RenderSize));
			}
		}
		DrawingContext drawingContext = dc;
		Color color;
		if (flag2)
		{
			color = ThemeService.Current?.Palette.FallbackPanelBackground ?? (flag ? Color.FromRgb(35, 35, 35) : Color.FromRgb(242, 242, 242));
		}
		else
		{
			color = (flag ? Color.FromArgb(28, 0, 0, 0) : Color.FromArgb(36, byte.MaxValue, byte.MaxValue, byte.MaxValue));
		}
		drawingContext.DrawRectangle(new SolidColorBrush(color), null, rect);
		LinearGradientBrush brush = new LinearGradientBrush(Color.FromArgb((byte)(18.0 + Math.Clamp(Highlight, 0.0, 1.0) * 18.0), byte.MaxValue, byte.MaxValue, byte.MaxValue), Color.FromArgb(2, byte.MaxValue, byte.MaxValue, byte.MaxValue), new Point(0.0, 0.0), new Point(0.65, 1.0));
		dc.DrawRectangle(brush, null, rect);
		dc.Pop();
		LinearGradientBrush brush2 = new LinearGradientBrush(new GradientStopCollection
		{
			new GradientStop(Color.FromArgb((byte)(100.0 + Math.Clamp(Highlight, 0.0, 1.0) * 100.0), byte.MaxValue, byte.MaxValue, byte.MaxValue), 0.0),
			new GradientStop(Color.FromArgb(18, byte.MaxValue, byte.MaxValue, byte.MaxValue), 0.46),
			new GradientStop(Color.FromArgb(65, byte.MaxValue, byte.MaxValue, byte.MaxValue), 1.0)
		}, new Point(0.0, 0.0), new Point(0.8, 1.0));
		dc.DrawGeometry(null, new Pen(brush2, 0.9 + Math.Clamp(Highlight, 0.0, 1.0) * 0.45), geometry);
	}
}
