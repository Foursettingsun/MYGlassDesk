using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GlassDesk.Platform.Interop;

internal static class Ole32
{
	internal const uint ClsCtxInprocServer = 1u;

	internal const uint StgmRead = 0u;

	internal const int SoK = 0;

	internal const int EInsufficientBuffer = -2147024774;

	[LibraryImport("ole32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	internal unsafe static int CoCreateInstance(in Guid classId, nint outer, uint context, in Guid interfaceId, out nint instance)
	{
		instance = 0;
		int result;
		fixed (nint* _instance_native = &instance)
		{
			fixed (Guid* _interfaceId_native = &interfaceId)
			{
				fixed (Guid* _classId_native = &classId)
				{
					result = __PInvoke(_classId_native, outer, context, _interfaceId_native, _instance_native);
				}
			}
		}
		return result;
		[DllImport("ole32.dll", EntryPoint = "CoCreateInstance", ExactSpelling = true)]
		unsafe static extern int __PInvoke(Guid* __classId_native, nint __outer_native, uint __context_native, Guid* __interfaceId_native, nint* __instance_native);
	}

	[DllImport("ole32.dll", ExactSpelling = true)]
	[LibraryImport("ole32.dll")]
	internal static extern void CoTaskMemFree(nint memory);
}
