using System;
using System.Windows;
using System.Windows.Media;

namespace GlassDesk.UI.Theming;

public sealed class GlassPalette
{
	public static readonly Color DefaultAccent = Color.FromRgb(10, 132, byte.MaxValue);

	public bool IsDark { get; }

	public Color Accent { get; }

	public Color AcrylicOverlayBackground { get; }

	public Color FallbackPanelBackground { get; }

	public Color AcrylicTint { get; }

	public byte AcrylicTintAlpha { get; }

	public Color PanelBorder { get; }

	public Color Foreground { get; }

	public Color SecondaryForeground { get; }

	public SolidColorBrush AccentBrush => CreateFrozenBrush(Accent);

	public Brush PanelBorderBrush => CreateBorderBrush();

	public SolidColorBrush ForegroundBrush => CreateFrozenBrush(Foreground);

	public SolidColorBrush SecondaryForegroundBrush => CreateFrozenBrush(SecondaryForeground);

	private GlassPalette(bool isDark, Color accent, Color acrylicOverlay, Color fallbackPanel, Color acrylicTint, byte acrylicTintAlpha, Color panelBorder, Color foreground, Color secondaryForeground)
	{
		IsDark = isDark;
		Accent = accent;
		AcrylicOverlayBackground = acrylicOverlay;
		FallbackPanelBackground = fallbackPanel;
		AcrylicTint = acrylicTint;
		AcrylicTintAlpha = acrylicTintAlpha;
		PanelBorder = panelBorder;
		Foreground = foreground;
		SecondaryForeground = secondaryForeground;
	}

	public Brush ResolvePanelBrush(bool acrylicApplied)
	{
		return CreatePanelBrush(acrylicApplied);
	}

	public static GlassPalette Create(bool isDark, Color accent, bool opaquePanels)
	{
		if (isDark)
		{
			return new GlassPalette(isDark: true, accent, Color.FromArgb((byte)((!opaquePanels) ? 10 : 0), byte.MaxValue, byte.MaxValue, byte.MaxValue), opaquePanels ? Color.FromArgb(244, 28, 29, 36) : Color.FromArgb(18, 28, 30, 38), Color.FromRgb(26, 26, 31), 24, Color.FromArgb(38, byte.MaxValue, byte.MaxValue, byte.MaxValue), Color.FromRgb(242, 242, 247), Color.FromArgb(179, 242, 242, 247));
		}
		return new GlassPalette(isDark: false, accent, Color.FromArgb((byte)((!opaquePanels) ? 20 : 0), byte.MaxValue, byte.MaxValue, byte.MaxValue), opaquePanels ? Color.FromArgb(244, 247, 248, 252) : Color.FromArgb(20, 240, 244, 252), Color.FromRgb(245, 245, 248), 32, Color.FromArgb(89, byte.MaxValue, byte.MaxValue, byte.MaxValue), Color.FromRgb(28, 28, 30), Color.FromArgb(179, 28, 28, 30));
	}

	public static Color ParseAccent(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return DefaultAccent;
		}
		try
		{
			object obj = ColorConverter.ConvertFromString(value.Trim());
			if (obj is Color)
			{
				return (Color)obj;
			}
		}
		catch (FormatException)
		{
		}
		catch (NotSupportedException)
		{
		}
		return DefaultAccent;
	}

	private static SolidColorBrush CreateFrozenBrush(Color color)
	{
		SolidColorBrush solidColorBrush = new SolidColorBrush(color);
		solidColorBrush.Freeze();
		return solidColorBrush;
	}

	private LinearGradientBrush CreatePanelBrush(bool acrylicApplied)
	{
		LinearGradientBrush linearGradientBrush = new LinearGradientBrush
		{
			StartPoint = new Point(0.0, 0.0),
			EndPoint = new Point(0.0, 1.0)
		};
		if (acrylicApplied)
		{
			byte a = (byte)(IsDark ? 28 : 50);
			byte a2 = (byte)(IsDark ? 8 : 22);
			byte a3 = (byte)(IsDark ? 19 : 34);
			linearGradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(a, byte.MaxValue, byte.MaxValue, byte.MaxValue), 0.0));
			linearGradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(a2, byte.MaxValue, byte.MaxValue, byte.MaxValue), 0.52));
			linearGradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(a3, Accent.R, Accent.G, Accent.B), 1.0));
		}
		else
		{
			Color fallbackPanelBackground = FallbackPanelBackground;
			Color color = (IsDark ? Color.FromArgb(fallbackPanelBackground.A, 41, 44, 54) : Color.FromArgb(fallbackPanelBackground.A, 231, 236, 245));
			linearGradientBrush.GradientStops.Add(new GradientStop(fallbackPanelBackground, 0.0));
			linearGradientBrush.GradientStops.Add(new GradientStop(color, 1.0));
		}
		linearGradientBrush.Freeze();
		return linearGradientBrush;
	}

	private LinearGradientBrush CreateBorderBrush()
	{
		LinearGradientBrush linearGradientBrush = new LinearGradientBrush();
		linearGradientBrush.StartPoint = new Point(0.0, 0.0);
		linearGradientBrush.EndPoint = new Point(1.0, 1.0);
		linearGradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(IsDark ? 160 : 184), byte.MaxValue, byte.MaxValue, byte.MaxValue), 0.0));
		linearGradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(IsDark ? 52 : 88), byte.MaxValue, byte.MaxValue, byte.MaxValue), 0.32));
		linearGradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(IsDark ? 32 : 66), byte.MaxValue, byte.MaxValue, byte.MaxValue), 0.72));
		linearGradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(IsDark ? 116 : 154), Accent.R, Accent.G, Accent.B), 1.0));
		linearGradientBrush.Freeze();
		return linearGradientBrush;
	}
}
