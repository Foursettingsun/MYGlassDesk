using System;
using System.Diagnostics;
using GlassDesk.Platform;
using GlassDesk.UI.Controls;

internal static class DesktopSceneCaptureFactory
{
	internal static DesktopSceneProvider SelectProvider(int build)
	{
		if (build >= 19041)
		{
			if (build < 22000)
			{
				return DesktopSceneProvider.IsolatedDocumentedScene;
			}
			return DesktopSceneProvider.IsolatedSharedVisual;
		}
		throw new PlatformNotSupportedException("This version requires Windows 10 2004 or later.");
	}

	internal static IDesktopSceneCapture Create(ScreenRect region, bool diagnosticOnScreen, Action<CaptureTrace>? trace)
	{
		DesktopSceneProvider provider = SelectProvider(WindowsCapabilities.BuildNumber);
		return new RecoveringDesktopSceneCapture(region, provider, trace);
	}

	internal static IDesktopSceneCapture CreateDesktopOnly(ScreenRect region, Action<CaptureTrace>? trace, string? executable = null)
	{
		return new RecoveringDesktopSceneCapture(region, () => new IsolatedDesktopSceneCapture(region, GlassBackdropSource.CompatibleScene, trace, executable, desktopOnly: true), () => new RecoveringDesktopSceneCapture(region, () => new IsolatedDesktopSceneCapture(region, GlassBackdropSource.CompatibleScene, trace, executable, desktopOnly: true, documentedDesktop: true), () => new IsolatedDesktopSceneCapture(region, GlassBackdropSource.Wallpaper, trace, executable), Stopwatch.GetTimestamp, trace), Stopwatch.GetTimestamp, trace);
	}
}
