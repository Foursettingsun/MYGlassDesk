using System.Windows;
using GlassDesk.Platform.Displays;

namespace GlassDesk.UI.Controls;

internal readonly record struct GlassCaptureGeometry(Size SampleSizeDip, Rect GlassBoundsDip, Rect FrameBoundsDip)
{
	internal static GlassCaptureGeometry Create(PixelRect requestedRegion, PixelRect clippedRegion, Point panelScreenOrigin, Rect panel, double dpiScale, bool expanded)
	{
		PixelRect pixelRect = requestedRegion;
		Size sampleSizeDip = (expanded ? new Size((double)pixelRect.Width / dpiScale, (double)pixelRect.Height / dpiScale) : panel.Size);
		Rect glassBoundsDip = (expanded ? new Rect((panelScreenOrigin.X - (double)pixelRect.X) / dpiScale, (panelScreenOrigin.Y - (double)pixelRect.Y) / dpiScale, panel.Width, panel.Height) : new Rect(panel.Size));
		Rect frameBoundsDip = (expanded ? new Rect(panel.X + ((double)pixelRect.X - panelScreenOrigin.X) / dpiScale, panel.Y + ((double)pixelRect.Y - panelScreenOrigin.Y) / dpiScale, sampleSizeDip.Width, sampleSizeDip.Height) : panel);
		return new GlassCaptureGeometry(sampleSizeDip, glassBoundsDip, frameBoundsDip);
	}
}
