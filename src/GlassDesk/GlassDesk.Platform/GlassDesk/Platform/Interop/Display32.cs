using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GlassDesk.Platform.Interop;

internal static class Display32
{
	internal delegate bool MonitorEnumProc(nint monitor, nint deviceContext, ref Rect monitorRect, nint data);

	internal const int MonitorInfoPrimary = 1;

	internal const int MonitorDefaultToNearest = 2;

	internal const int MonitorDefaultToPrimary = 1;

	[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool EnumDisplayMonitors(nint deviceContext, nint clipRect, MonitorEnumProc callback, nint data);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool GetMonitorInfo(nint monitor, ref MonitorInfoEx info);

	[DllImport("user32.dll")]
	internal static extern nint MonitorFromPoint(Point point, int flags);

	[DllImport("user32.dll")]
	internal static extern nint MonitorFromWindow(nint window, int flags);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool GetWindowRect(nint window, out Rect rect);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool GetClientRect(nint window, out Rect rect);

	[LibraryImport("user32.dll", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal unsafe static bool GetCursorPos(out Point point)
	{
		point = default;
		int num;
		int lastSystemError;
		fixed (Point* _point_native = &point)
		{
			Marshal.SetLastSystemError(0);
			num = __PInvoke(_point_native);
			lastSystemError = Marshal.GetLastSystemError();
		}
		bool result = num != 0;
		Marshal.SetLastPInvokeError(lastSystemError);
		return result;
		[DllImport("user32.dll", EntryPoint = "GetCursorPos", ExactSpelling = true)]
		unsafe static extern int __PInvoke(Point* __point_native);
	}
}
