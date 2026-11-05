using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GlassDesk.UI.Controls;

public sealed class PhoneBorder : Border
{
	protected override void OnRender(DrawingContext dc)
	{
		Rect bounds = new Rect(RenderSize);
		if (!bounds.IsEmpty && !(bounds.Width <= 0.0) && !(bounds.Height <= 0.0))
		{
			double num = ResolveRadius();
			dc.DrawGeometry(Background, null, SquircleGlassGeometry.Create(bounds, num));
			Thickness borderThickness = BorderThickness;
			double num2 = Math.Max(Math.Max(borderThickness.Left, borderThickness.Top), Math.Max(borderThickness.Right, borderThickness.Bottom));
			if (BorderBrush != null && !(num2 <= 0.0) && !(bounds.Width <= num2) && !(bounds.Height <= num2))
			{
				double num3 = num2 / 2.0;
				bounds.Inflate(0.0 - num3, 0.0 - num3);
				dc.DrawGeometry(null, new Pen(BorderBrush, num2), SquircleGlassGeometry.Create(bounds, Math.Max(0.0, num - num3)));
			}
		}
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		Size size = base.ArrangeOverride(finalSize);
		UpdateClip(size);
		return size;
	}

	protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);
		if (e.Property == Border.CornerRadiusProperty)
		{
			UpdateClip(RenderSize);
		}
	}

	private void UpdateClip(Size size)
	{
		if (!(size.Width <= 0.0) && !(size.Height <= 0.0))
		{
			Clip = SquircleGlassGeometry.Create(new Rect(size), ResolveRadius());
		}
	}

	private double ResolveRadius()
	{
		CornerRadius cornerRadius = CornerRadius;
		return Math.Max(Math.Max(cornerRadius.TopLeft, cornerRadius.TopRight), Math.Max(cornerRadius.BottomLeft, cornerRadius.BottomRight));
	}
}
