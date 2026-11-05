using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GlassDesk.Platform.Interop;

internal static class Dpi32
{
	[DllImport("user32.dll", ExactSpelling = true)]
	[LibraryImport("user32.dll")]
	internal static extern uint GetDpiForWindow(nint window);

	[DllImport("user32.dll", ExactSpelling = true)]
	[LibraryImport("user32.dll")]
	internal static extern uint GetDpiForSystem();

	[DllImport("user32.dll", ExactSpelling = true)]
	[LibraryImport("user32.dll")]
	internal static extern nint GetThreadDpiAwarenessContext();

	[DllImport("user32.dll", ExactSpelling = true)]
	[LibraryImport("user32.dll")]
	internal static extern nint GetWindowDpiAwarenessContext(nint window);

	[DllImport("user32.dll", ExactSpelling = true)]
	[LibraryImport("user32.dll")]
	internal static extern int GetAwarenessFromDpiAwarenessContext(nint awarenessContext);

	[LibraryImport("user32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static bool AreDpiAwarenessContextsEqual(nint awarenessContext1, nint awarenessContext2)
	{
		return __PInvoke(awarenessContext1, awarenessContext2) != 0;
		[DllImport("user32.dll", EntryPoint = "AreDpiAwarenessContextsEqual", ExactSpelling = true)]
		static extern int __PInvoke(nint __awarenessContext1_native, nint __awarenessContext2_native);
	}

	[LibraryImport("user32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static bool SetWindowPos(nint window, nint insertAfter, int x, int y, int width, int height, uint flags)
	{
		return __PInvoke(window, insertAfter, x, y, width, height, flags) != 0;
		[DllImport("user32.dll", EntryPoint = "SetWindowPos", ExactSpelling = true)]
		static extern int __PInvoke(nint __window_native, nint __insertAfter_native, int __x_native, int __y_native, int __width_native, int __height_native, uint __flags_native);
	}
}
