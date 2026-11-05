using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using GlassDesk.Platform.Interop;

namespace GlassDesk.Platform.Displays;

public static class DisplayService
{
	private static readonly nint ContextUnaware = -1;

	private static readonly nint ContextSystemAware = -2;

	private static readonly nint ContextPerMonitorAware = -3;

	private static readonly nint ContextPerMonitorAwareV2 = -4;

	private static readonly nint ContextUnawareGdiScaled = -5;

	public static IReadOnlyList<DisplayInfo> Enumerate()
	{
		List<DisplayInfo> displays = new List<DisplayInfo>();
		Display32.MonitorEnumProc callback = delegate(nint monitor, nint deviceContext, ref Rect monitorRect, nint data)
		{
			MonitorInfoEx info = new MonitorInfoEx
			{
				Size = Marshal.SizeOf<MonitorInfoEx>(),
				DeviceName = string.Empty
			};
			if (Display32.GetMonitorInfo(monitor, ref info))
			{
				displays.Add(new DisplayInfo(info.DeviceName, (info.Flags & 1) != 0, ToPixelRect(info.Monitor), ToPixelRect(info.Work), DpiForMonitor(monitor)));
			}
			return true;
		};
		Display32.EnumDisplayMonitors(0, 0, callback, 0);
		return displays.OrderByDescending((DisplayInfo d) => d.IsPrimary).ThenBy((DisplayInfo d) => d.DeviceName, StringComparer.OrdinalIgnoreCase).ToList();
	}

	public static DisplayInfo? Primary()
	{
		return Enumerate().FirstOrDefault((DisplayInfo d) => d.IsPrimary);
	}

	public static (int X, int Y)? GetCursorPixelPoint()
	{
		try
		{
			Point point;
			return Display32.GetCursorPos(out point) ? new (int, int)?((point.X, point.Y)) : (((int, int)?)null);
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return null;
		}
	}

	public static DisplayInfo? FindByDeviceName(string? deviceName)
	{
		if (string.IsNullOrWhiteSpace(deviceName))
		{
			return null;
		}
		return Enumerate().FirstOrDefault((DisplayInfo d) => string.Equals(d.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase));
	}

	public static DisplayInfo? ResolveOrPrimary(string? deviceName)
	{
		return FindByDeviceName(deviceName) ?? Primary();
	}

	public static DisplayInfo? FromPixelPoint(int x, int y)
	{
		nint num = Display32.MonitorFromPoint(new Point
		{
			X = x,
			Y = y
		}, 2);
		if (num == 0)
		{
			return null;
		}
		MonitorInfoEx info = new MonitorInfoEx
		{
			Size = Marshal.SizeOf<MonitorInfoEx>(),
			DeviceName = string.Empty
		};
		if (!Display32.GetMonitorInfo(num, ref info))
		{
			return null;
		}
		return new DisplayInfo(info.DeviceName, (info.Flags & 1) != 0, ToPixelRect(info.Monitor), ToPixelRect(info.Work), DpiForMonitor(num));
	}

	public static DpiAwarenessInfo ProbeDpiAwareness()
	{
		nint threadDpiAwarenessContext = Dpi32.GetThreadDpiAwarenessContext();
		return new DpiAwarenessInfo(Dpi32.GetAwarenessFromDpiAwarenessContext(threadDpiAwarenessContext) switch
		{
			0 => DpiAwarenessLevel.Unaware, 
			1 => DpiAwarenessLevel.SystemAware, 
			2 => DpiAwarenessLevel.PerMonitorAware, 
			_ => DpiAwarenessLevel.Unknown, 
		}, IsContext(threadDpiAwarenessContext, ContextPerMonitorAwareV2), DescribeContext(threadDpiAwarenessContext), Dpi32.GetDpiForSystem());
	}

	private static bool IsContext(nint context, nint reference)
	{
		try
		{
			return Dpi32.AreDpiAwarenessContextsEqual(context, reference);
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return false;
		}
	}

	public static uint DpiForWindow(nint window)
	{
		if (window == 0)
		{
			return 96u;
		}
		uint dpiForWindow = Dpi32.GetDpiForWindow(window);
		if (dpiForWindow != 0)
		{
			return dpiForWindow;
		}
		return 96u;
	}

	public static bool MoveWindowToPixelRect(nint window, PixelRect rect)
	{
		if (window == 0)
		{
			return false;
		}
		if (!TrySetPositionOnce(window, rect))
		{
			return false;
		}
		DrainPendingWindowMessages(window);
		return TrySetPositionOnce(window, rect);
	}

	private static bool TrySetPositionOnce(nint window, PixelRect rect)
	{
		return Dpi32.SetWindowPos(window, 0, rect.X, rect.Y, rect.Width, rect.Height, 20u);
	}

	private static void DrainPendingWindowMessages(nint window)
	{
		for (int i = 0; i < 64; i++)
		{
			if (!User32.PeekMessage(out var message, window, 0u, 0u, 0u))
			{
				break;
			}
			if (message.Message == 18)
			{
				break;
			}
			if (!User32.PeekMessage(out message, window, 0u, 0u, 1u))
			{
				break;
			}
			User32.TranslateMessage(in message);
			User32.DispatchMessage(in message);
		}
	}

	public static PixelRect? GetWindowPixelBounds(nint window)
	{
		if (window == 0)
		{
			return null;
		}
		if (!Display32.GetWindowRect(window, out var rect))
		{
			return null;
		}
		return ToPixelRect(rect);
	}

	private static string DescribeContext(nint context)
	{
		if (context == ContextUnaware)
		{
			return "UNAWARE";
		}
		if (context == ContextSystemAware)
		{
			return "SYSTEM_AWARE";
		}
		if (context == ContextPerMonitorAware)
		{
			return "PER_MONITOR_AWARE";
		}
		if (context == ContextPerMonitorAwareV2)
		{
			return "PER_MONITOR_AWARE_V2";
		}
		if (context == ContextUnawareGdiScaled)
		{
			return "UNAWARE_GDISCALED";
		}
		return $"未知（0x{context:X}）";
	}

	private static uint DpiForMonitor(nint monitor)
	{
		try
		{
			if (Shcore.GetDpiForMonitor(monitor, MonitorDpiType.Effective, out var dpiX, out var _) == 0 && dpiX != 0)
			{
				return dpiX;
			}
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
		}
		uint dpiForSystem = Dpi32.GetDpiForSystem();
		if (dpiForSystem != 0)
		{
			return dpiForSystem;
		}
		return 96u;
	}

	private static PixelRect ToPixelRect(Rect rect)
	{
		return new PixelRect(rect.Left, rect.Top, rect.Width, rect.Height);
	}
}
