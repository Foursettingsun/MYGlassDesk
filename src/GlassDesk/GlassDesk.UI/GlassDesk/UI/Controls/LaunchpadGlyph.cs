using System;
using System.Windows;
using System.Windows.Media;

namespace GlassDesk.UI.Controls;

public sealed class LaunchpadGlyph : FrameworkElement
{
	public static readonly DependencyProperty OutlineBrushProperty = DependencyProperty.Register("OutlineBrush", typeof(Brush), typeof(LaunchpadGlyph), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

	public static readonly DependencyProperty IsFilledProperty = DependencyProperty.Register("IsFilled", typeof(bool), typeof(LaunchpadGlyph), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

	public bool IsFilled
	{
		get
		{
			return (bool)GetValue(IsFilledProperty);
		}
		set
		{
			SetValue(IsFilledProperty, value);
		}
	}

	public Brush? OutlineBrush
	{
		get
		{
			return (Brush)GetValue(OutlineBrushProperty);
		}
		set
		{
			SetValue(OutlineBrushProperty, value);
		}
	}

	protected override void OnRender(DrawingContext drawingContext)
	{
		base.OnRender(drawingContext);
		double num = Math.Min(ActualWidth, ActualHeight) / 48.0;
		if (num <= 0.0)
		{
			return;
		}
		Brush outlineBrush = OutlineBrush;
		if (outlineBrush != null)
		{
			Pen pen = (IsFilled ? null : new Pen(outlineBrush, 3.5 * num)
			{
				LineJoin = PenLineJoin.Round
			});
			for (int i = 0; i < 9; i++)
			{
				double num2 = (IsFilled ? 4.0 : 5.75);
				double num3 = (IsFilled ? 12.0 : 8.5);
				double num4 = (IsFilled ? 3.5 : 1.8) * num;
				drawingContext.DrawRoundedRectangle(rectangle: new Rect((num2 + (double)(i % 3 * 14)) * num, (num2 + (double)(i / 3 * 14)) * num, num3 * num, num3 * num), brush: IsFilled ? outlineBrush : null, pen: pen, radiusX: num4, radiusY: num4);
			}
			return;
		}
		string[] array = new string[9] { "#BEDBFF", "#C2EEE7", "#E6DCFF", "#D3E5FF", "#F1DCCA", "#C8E9EB", "#DCD9FA", "#BBDEEE", "#EADFEB" };
		for (int j = 0; j < 9; j++)
		{
			drawingContext.DrawGeometry(geometry: SquircleGlassGeometry.Create(new Rect((double)(8 + j % 3 * 11) * num, (double)(8 + j / 3 * 11) * num, 9.0 * num, 9.0 * num), 3.0 * num), brush: new SolidColorBrush((Color)ColorConverter.ConvertFromString(array[j])), pen: null);
		}
	}
}
