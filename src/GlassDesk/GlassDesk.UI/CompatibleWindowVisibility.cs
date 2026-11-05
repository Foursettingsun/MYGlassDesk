using System;
using System.Runtime.InteropServices;
using System.Text;

internal static class CompatibleWindowVisibility
{
	private struct NativeRect
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	private struct MonitorInfo
	{
		public uint Size;

		public NativeRect Bounds;

		public NativeRect WorkArea;

		public uint Flags;
	}

	internal static string? ExclusionReason(nint window)
	{
		long num = ((IntPtr)GetWindowLongPtrW(window, -20)).ToInt64();
		if ((num & 0x80000) == 0L)
		{
			return null;
		}
		bool layeredWindowAttributes = GetLayeredWindowAttributes(window, out var _, out var alpha, out var flags);
		if (layeredWindowAttributes && (flags & 2) != 0 && alpha == 0)
		{
			return "ZeroOpacity";
		}
		if ((num & 0x8080080) != 134742144)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder(128);
		GetClassNameW(window, stringBuilder, stringBuilder.Capacity);
		if (stringBuilder.ToString() != "CEF-OSC-WIDGET")
		{
			return null;
		}
		if (IsMonitorSized(window))
		{
			return "ScreenSizedLayeredCefOverlay";
		}
		if (layeredWindowAttributes && (flags & 2) != 0 && (flags & 1) == 0)
		{
			return null;
		}
		return "UnsupportedLayeredCefOverlay";
	}

	private static bool IsMonitorSized(nint window)
	{
		MonitorInfo info = new MonitorInfo
		{
			Size = (uint)Marshal.SizeOf<MonitorInfo>()
		};
		if (!GetWindowRect(window, out var bounds) || !GetMonitorInfoW(MonitorFromWindow(window, 2u), ref info))
		{
			return false;
		}
		int num = Math.Max(0, Math.Min(bounds.Right, info.Bounds.Right) - Math.Max(bounds.Left, info.Bounds.Left));
		int num2 = Math.Max(0, Math.Min(bounds.Bottom, info.Bounds.Bottom) - Math.Max(bounds.Top, info.Bounds.Top));
		long num3 = (long)(info.Bounds.Right - info.Bounds.Left) * (long)(info.Bounds.Bottom - info.Bounds.Top);
		if (num3 > 0)
		{
			return (long)num * (long)num2 * 100 >= num3 * 98;
		}
		return false;
	}

	[DllImport("user32.dll")]
	private static extern nint GetWindowLongPtrW(nint window, int index);

	[DllImport("user32.dll")]
	private static extern bool GetLayeredWindowAttributes(nint window, out uint color, out byte alpha, out uint flags);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int GetClassNameW(nint window, StringBuilder name, int count);

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(nint window, out NativeRect bounds);

	[DllImport("user32.dll")]
	private static extern nint MonitorFromWindow(nint window, uint flags);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern bool GetMonitorInfoW(nint monitor, ref MonitorInfo info);
}
