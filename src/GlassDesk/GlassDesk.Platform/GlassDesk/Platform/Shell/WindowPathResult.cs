using System;
using GlassDesk.Platform.Displays;

namespace GlassDesk.Platform.Shell;

public sealed record WindowPathResult(FullscreenVerdict Verdict, string Reason, PixelRect? WindowRect, PixelRect? VisibleFrame, PixelRect? MonitorBounds, string MonitorDeviceName, TimeSpan Duration)
{
	public bool? IsFullscreen => Verdict switch
	{
		FullscreenVerdict.Fullscreen => (bool?)true, 
		FullscreenVerdict.NotFullscreen => false, 
		_ => null, 
	};
}
