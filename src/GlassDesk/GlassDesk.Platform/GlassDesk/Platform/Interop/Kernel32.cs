using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace GlassDesk.Platform.Interop;

internal static class Kernel32
{
	internal const uint ProcessQueryLimitedInformation = 4096u;

	internal const uint WaitTimeout = 258u;

	internal const int AppModelErrorNoApplication = -2144927470;

	[LibraryImport("kernel32.dll", EntryPoint = "AttachConsole", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static bool AttachConsole(int processId)
	{
		Marshal.SetLastSystemError(0);
		int num = __PInvoke(processId);
		int lastSystemError = Marshal.GetLastSystemError();
		bool result = num != 0;
		Marshal.SetLastPInvokeError(lastSystemError);
		return result;
		[DllImport("kernel32.dll", EntryPoint = "AttachConsole", ExactSpelling = true)]
		static extern int __PInvoke(int __processId_native);
	}

	[LibraryImport("kernel32.dll", EntryPoint = "SetConsoleOutputCP")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static bool SetConsoleOutputCP(uint codePage)
	{
		return __PInvoke(codePage) != 0;
		[DllImport("kernel32.dll", EntryPoint = "SetConsoleOutputCP", ExactSpelling = true)]
		static extern int __PInvoke(uint __codePage_native);
	}

	[DllImport("kernel32.dll", ExactSpelling = true)]
	[LibraryImport("kernel32.dll")]
	internal static extern uint GetCurrentThreadId();

	[LibraryImport("kernel32.dll", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	internal static nint OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint processId)
	{
		int _inheritHandle_native = (inheritHandle ? 1 : 0);
		Marshal.SetLastSystemError(0);
		nint result = __PInvoke(access, _inheritHandle_native, processId);
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		return result;
		[DllImport("kernel32.dll", EntryPoint = "OpenProcess", ExactSpelling = true)]
		static extern nint __PInvoke(uint __access_native, int __inheritHandle_native, uint __processId_native);
	}

	[LibraryImport("kernel32.dll", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static bool CloseHandle(nint handle)
	{
		Marshal.SetLastSystemError(0);
		int num = __PInvoke(handle);
		int lastSystemError = Marshal.GetLastSystemError();
		bool result = num != 0;
		Marshal.SetLastPInvokeError(lastSystemError);
		return result;
		[DllImport("kernel32.dll", EntryPoint = "CloseHandle", ExactSpelling = true)]
		static extern int __PInvoke(nint __handle_native);
	}

	[LibraryImport("kernel32.dll", EntryPoint = "QueryFullProcessImageNameW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal unsafe static bool QueryFullProcessImageName(nint process, uint flags, [Out] char[] name, ref uint size)
	{
		int num;
		int lastSystemError;
		fixed (uint* _size_native = &size)
		{
			fixed (char* ptr = &ArrayMarshaller<char, char>.ManagedToUnmanagedIn.GetPinnableReference(name))
			{
				void* _name_native = ptr;
				Marshal.SetLastSystemError(0);
				num = __PInvoke(process, flags, (char*)_name_native, _size_native);
				lastSystemError = Marshal.GetLastSystemError();
			}
		}
		bool result = num != 0;
		Marshal.SetLastPInvokeError(lastSystemError);
		return result;
		[DllImport("kernel32.dll", EntryPoint = "QueryFullProcessImageNameW", ExactSpelling = true)]
		unsafe static extern int __PInvoke(nint __process_native, uint __flags_native, char* __name_native, uint* __size_native);
	}

	[LibraryImport("kernel32.dll", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	internal static uint WaitForSingleObject(nint handle, uint milliseconds)
	{
		Marshal.SetLastSystemError(0);
		uint result = __PInvoke(handle, milliseconds);
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		return result;
		[DllImport("kernel32.dll", EntryPoint = "WaitForSingleObject", ExactSpelling = true)]
		static extern uint __PInvoke(nint __handle_native, uint __milliseconds_native);
	}

	[LibraryImport("kernel32.dll", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	internal static uint GetProcessId(nint process)
	{
		Marshal.SetLastSystemError(0);
		uint result = __PInvoke(process);
		Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
		return result;
		[DllImport("kernel32.dll", EntryPoint = "GetProcessId", ExactSpelling = true)]
		static extern uint __PInvoke(nint __process_native);
	}

	[LibraryImport("kernel32.dll", EntryPoint = "GetApplicationUserModelId", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	internal unsafe static int GetApplicationUserModelId(nint process, ref uint length, [Out] char[] buffer)
	{
		int result;
		int lastSystemError;
		fixed (char* ptr = &ArrayMarshaller<char, char>.ManagedToUnmanagedIn.GetPinnableReference(buffer))
		{
			void* _buffer_native = ptr;
			fixed (uint* _length_native = &length)
			{
				Marshal.SetLastSystemError(0);
				result = __PInvoke(process, _length_native, (char*)_buffer_native);
				lastSystemError = Marshal.GetLastSystemError();
			}
		}
		Marshal.SetLastPInvokeError(lastSystemError);
		return result;
		[DllImport("kernel32.dll", EntryPoint = "GetApplicationUserModelId", ExactSpelling = true)]
		unsafe static extern int __PInvoke(nint __process_native, uint* __length_native, char* __buffer_native);
	}
}
