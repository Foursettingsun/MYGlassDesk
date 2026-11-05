using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GlassDesk.Platform;

internal static class Composition32
{
	[LibraryImport("user32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	internal unsafe static int SetWindowCompositionAttribute(nint window, ref WindowCompositionAttributeData data)
	{
		int result;
		fixed (WindowCompositionAttributeData* _data_native = &data)
		{
			result = __PInvoke(window, _data_native);
		}
		return result;
		[DllImport("user32.dll", EntryPoint = "SetWindowCompositionAttribute", ExactSpelling = true)]
		unsafe static extern int __PInvoke(nint __window_native, WindowCompositionAttributeData* __data_native);
	}
}
