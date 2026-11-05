using System;
using GlassDesk.Platform.Interop;

namespace GlassDesk.Platform;

public static class WindowAppearance
{
	public static WindowAppearanceResult Apply(nint hwnd, WindowAppearanceRequest request)
	{
		ArgumentNullException.ThrowIfNull(request, "request");
		if (hwnd == 0)
		{
			return default;
		}
		bool flag = false;
		if (request.Backdrop != SystemBackdropType.None && WindowsCapabilities.SupportsSystemBackdrop)
		{
			flag = DwmApi.TrySetAttribute(hwnd, 38, (int)request.Backdrop);
		}
		if (flag && request.ExtendFrameIntoClientArea)
		{
			DwmApi.TryExtendFrameIntoClientArea(hwnd, -1, -1, -1, -1);
		}
		bool cornersApplied = false;
		if (WindowsCapabilities.SupportsWindowCornerPreference)
		{
			cornersApplied = DwmApi.TrySetAttribute(hwnd, 33, (int)request.Corners);
		}
		bool darkModeApplied = false;
		if (WindowsCapabilities.SupportsImmersiveDarkMode)
		{
			darkModeApplied = DwmApi.TrySetAttribute(hwnd, 20, request.DarkMode ? 1 : 0);
		}
		return new WindowAppearanceResult(flag, cornersApplied, darkModeApplied);
	}
}
