using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GlassDesk.Platform.Displays;

internal static class Shcore
{
	[LibraryImport("shcore.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	internal unsafe static int GetDpiForMonitor(nint monitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY)
	{
		dpiX = 0u;
		dpiY = 0u;
		int result;
		fixed (uint* _dpiY_native = &dpiY)
		{
			fixed (uint* _dpiX_native = &dpiX)
			{
				result = __PInvoke(monitor, dpiType, _dpiX_native, _dpiY_native);
			}
		}
		return result;
		[DllImport("shcore.dll", EntryPoint = "GetDpiForMonitor", ExactSpelling = true)]
		unsafe static extern int __PInvoke(nint __monitor_native, MonitorDpiType __dpiType_native, uint* __dpiX_native, uint* __dpiY_native);
	}
}
