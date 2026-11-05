using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace GlassDesk.Platform.Interop;

internal static class User32
{
	internal delegate void WinEventProc(nint hook, uint eventType, nint window, int objectId, int childId, uint threadId, uint time);

	internal delegate int EnumWindowsProc(nint window, nint parameter);

	internal const int GwlStyle = -16;

	internal const int GwlExStyle = -20;

	internal const int WsMaximize = 16777216;

	internal const int WsExNoactivate = 134217728;

	private const int ClassNameCapacity = 256;

	private const int WindowTextCapacity = 256;

	internal const uint EventSystemForeground = 3u;

	internal const uint EventObjectCreate = 32768u;

	internal const uint EventObjectDestroy = 32769u;

	internal const uint EventObjectShow = 32770u;

	internal const uint EventObjectHide = 32771u;

	internal const uint EventSystemMinimizeStart = 22u;

	internal const uint EventSystemMinimizeEnd = 23u;

	internal const uint EventObjectLocationChange = 32779u;

	internal const int ObjIdWindow = 0;

	internal const uint WineventOutOfContext = 0u;

	internal const uint WineventSkipOwnProcess = 2u;

	internal const uint WmQuit = 18u;

	internal const uint PmRemove = 1u;

	internal const uint PmNoRemove = 0u;

	internal const int GwOwner = 4;

	internal const int SwMinimize = 6;

	internal const int SwShow = 5;

	internal const int SwHide = 0;

	internal const uint WmClose = 16u;

	internal const int VirtualKeyLeftButton = 1;

	internal const int SwRestore = 9;

	[LibraryImport("user32.dll", EntryPoint = "RegisterWindowMessageW", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	internal unsafe static uint RegisterWindowMessage(string name)
	{
		uint result;
		fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(name))
		{
			void* _name_native = ptr;
			result = __PInvoke((ushort*)_name_native);
		}
		return result;
		[DllImport("user32.dll", EntryPoint = "RegisterWindowMessageW", ExactSpelling = true)]
		unsafe static extern uint __PInvoke(ushort* __name_native);
	}

	[LibraryImport("user32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static bool DestroyIcon(nint icon)
	{
		return __PInvoke(icon) != 0;
		[DllImport("user32.dll", EntryPoint = "DestroyIcon", ExactSpelling = true)]
		static extern int __PInvoke(nint __icon_native);
	}

	[LibraryImport("user32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static bool SetForegroundWindow(nint window)
	{
		return __PInvoke(window) != 0;
		[DllImport("user32.dll", EntryPoint = "SetForegroundWindow", ExactSpelling = true)]
		static extern int __PInvoke(nint __window_native);
	}

	[DllImport("user32.dll", ExactSpelling = true)]
	[LibraryImport("user32.dll")]
	internal static extern nint GetForegroundWindow();

	[DllImport("user32.dll", ExactSpelling = true)]
	[LibraryImport("user32.dll")]
	internal static extern nint GetShellWindow();

	[DllImport("user32.dll", ExactSpelling = true)]
	[LibraryImport("user32.dll")]
	internal static extern nint GetDesktopWindow();

	[LibraryImport("user32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static bool IsWindowVisible(nint window)
	{
		return __PInvoke(window) != 0;
		[DllImport("user32.dll", EntryPoint = "IsWindowVisible", ExactSpelling = true)]
		static extern int __PInvoke(nint __window_native);
	}

	[LibraryImport("user32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static bool IsIconic(nint window)
	{
		return __PInvoke(window) != 0;
		[DllImport("user32.dll", EntryPoint = "IsIconic", ExactSpelling = true)]
		static extern int __PInvoke(nint __window_native);
	}

	[DllImport("user32.dll", EntryPoint = "GetWindowLongW", ExactSpelling = true)]
	[LibraryImport("user32.dll", EntryPoint = "GetWindowLongW")]
	internal static extern int GetWindowLong(nint window, int index);

	[DllImport("user32.dll", EntryPoint = "SetWindowLongW", ExactSpelling = true)]
	[LibraryImport("user32.dll", EntryPoint = "SetWindowLongW")]
	internal static extern int SetWindowLong(nint window, int index, int value);

	[LibraryImport("user32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	internal unsafe static uint GetWindowThreadProcessId(nint window, out uint processId)
	{
		processId = 0u;
		uint result;
		fixed (uint* _processId_native = &processId)
		{
			result = __PInvoke(window, _processId_native);
		}
		return result;
		[DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId", ExactSpelling = true)]
		unsafe static extern uint __PInvoke(nint __window_native, uint* __processId_native);
	}

	[DllImport("user32.dll", EntryPoint = "GetClassNameW", ExactSpelling = true)]
	[LibraryImport("user32.dll", EntryPoint = "GetClassNameW")]
	private unsafe static extern int GetClassNameCore(nint window, char* buffer, int capacity);

	[DllImport("user32.dll", EntryPoint = "GetWindowTextW", ExactSpelling = true)]
	[LibraryImport("user32.dll", EntryPoint = "GetWindowTextW")]
	private unsafe static extern int GetWindowTextCore(nint window, char* buffer, int capacity);

	internal unsafe static int GetClassName(nint window, Span<char> destination)
	{
		if (window == 0 || destination.IsEmpty)
		{
			return 0;
		}
		fixed (char* buffer = destination)
		{
			return GetClassNameCore(window, buffer, destination.Length);
		}
	}

	internal static string ReadClassName(nint window)
	{
		if (window == 0)
		{
			return string.Empty;
		}
		Span<char> destination = stackalloc char[256];
		int className = GetClassName(window, destination);
		if (className > 0)
		{
			return new string(destination.Slice(0, className));
		}
		return string.Empty;
	}

	internal unsafe static string ReadWindowText(nint window)
	{
		if (window == 0)
		{
			return string.Empty;
		}
		Span<char> span = stackalloc char[256];
		int windowTextCore;
		fixed (char* buffer = span)
		{
			windowTextCore = GetWindowTextCore(window, buffer, span.Length);
		}
		if (windowTextCore > 0)
		{
			return new string(span.Slice(0, windowTextCore));
		}
		return string.Empty;
	}

	[DllImport("user32.dll", SetLastError = true)]
	internal static extern nint SetWinEventHook(uint eventMin, uint eventMax, nint hookModule, WinEventProc callback, uint processId, uint threadId, uint flags);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool UnhookWinEvent(nint hook);

	[LibraryImport("user32.dll", EntryPoint = "GetMessageW")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	internal unsafe static int GetMessage(out Msg message, nint window, uint min, uint max)
	{
		message = default;
		int result;
		fixed (Msg* _message_native = &message)
		{
			result = __PInvoke(_message_native, window, min, max);
		}
		return result;
		[DllImport("user32.dll", EntryPoint = "GetMessageW", ExactSpelling = true)]
		unsafe static extern int __PInvoke(Msg* __message_native, nint __window_native, uint __min_native, uint __max_native);
	}

	[LibraryImport("user32.dll", EntryPoint = "PeekMessageW")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal unsafe static bool PeekMessage(out Msg message, nint window, uint min, uint max, uint remove)
	{
		message = default;
		int num;
		fixed (Msg* _message_native = &message)
		{
			num = __PInvoke(_message_native, window, min, max, remove);
		}
		return num != 0;
		[DllImport("user32.dll", EntryPoint = "PeekMessageW", ExactSpelling = true)]
		unsafe static extern int __PInvoke(Msg* __message_native, nint __window_native, uint __min_native, uint __max_native, uint __remove_native);
	}

	[LibraryImport("user32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal unsafe static bool TranslateMessage(in Msg message)
	{
		int num;
		fixed (Msg* _message_native = &message)
		{
			num = __PInvoke(_message_native);
		}
		return num != 0;
		[DllImport("user32.dll", EntryPoint = "TranslateMessage", ExactSpelling = true)]
		unsafe static extern int __PInvoke(Msg* __message_native);
	}

	[LibraryImport("user32.dll", EntryPoint = "DispatchMessageW")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	internal unsafe static nint DispatchMessage(in Msg message)
	{
		nint result;
		fixed (Msg* _message_native = &message)
		{
			result = __PInvoke(_message_native);
		}
		return result;
		[DllImport("user32.dll", EntryPoint = "DispatchMessageW", ExactSpelling = true)]
		unsafe static extern nint __PInvoke(Msg* __message_native);
	}

	[LibraryImport("user32.dll", EntryPoint = "PostThreadMessageW", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static bool PostThreadMessage(uint threadId, uint message, nuint wParam, nint lParam)
	{
		Marshal.SetLastSystemError(0);
		int num = __PInvoke(threadId, message, wParam, lParam);
		int lastSystemError = Marshal.GetLastSystemError();
		bool result = num != 0;
		Marshal.SetLastPInvokeError(lastSystemError);
		return result;
		[DllImport("user32.dll", EntryPoint = "PostThreadMessageW", ExactSpelling = true)]
		static extern int __PInvoke(uint __threadId_native, uint __message_native, nuint __wParam_native, nint __lParam_native);
	}

	[LibraryImport("user32.dll", EntryPoint = "PostMessageW", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static bool PostMessage(nint window, uint message, nuint wParam, nint lParam)
	{
		Marshal.SetLastSystemError(0);
		int num = __PInvoke(window, message, wParam, lParam);
		int lastSystemError = Marshal.GetLastSystemError();
		bool result = num != 0;
		Marshal.SetLastPInvokeError(lastSystemError);
		return result;
		[DllImport("user32.dll", EntryPoint = "PostMessageW", ExactSpelling = true)]
		static extern int __PInvoke(nint __window_native, uint __message_native, nuint __wParam_native, nint __lParam_native);
	}

	[LibraryImport("user32.dll", EntryPoint = "GetPropW", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	internal unsafe static nint GetProp(nint window, string atom)
	{
		nint result;
		fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(atom))
		{
			void* _atom_native = ptr;
			result = __PInvoke(window, (ushort*)_atom_native);
		}
		return result;
		[DllImport("user32.dll", EntryPoint = "GetPropW", ExactSpelling = true)]
		unsafe static extern nint __PInvoke(nint __window_native, ushort* __atom_native);
	}

	[DllImport("user32.dll", ExactSpelling = true)]
	[LibraryImport("user32.dll")]
	internal static extern short GetAsyncKeyState(int virtualKey);

	[LibraryImport("user32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static bool IsWindow(nint window)
	{
		return __PInvoke(window) != 0;
		[DllImport("user32.dll", EntryPoint = "IsWindow", ExactSpelling = true)]
		static extern int __PInvoke(nint __window_native);
	}

	[LibraryImport("user32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static bool ShowWindow(nint window, int command)
	{
		return __PInvoke(window, command) != 0;
		[DllImport("user32.dll", EntryPoint = "ShowWindow", ExactSpelling = true)]
		static extern int __PInvoke(nint __window_native, int __command_native);
	}

	[DllImport("user32.dll", ExactSpelling = true)]
	[LibraryImport("user32.dll")]
	internal static extern nint GetWindow(nint window, uint command);

	[LibraryImport("user32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static bool BringWindowToTop(nint window)
	{
		return __PInvoke(window) != 0;
		[DllImport("user32.dll", EntryPoint = "BringWindowToTop", ExactSpelling = true)]
		static extern int __PInvoke(nint __window_native);
	}

	[LibraryImport("user32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static bool AttachThreadInput(uint attach, uint attachTo, [MarshalAs(UnmanagedType.Bool)] bool attachFlag)
	{
		int _attachFlag_native = (attachFlag ? 1 : 0);
		return __PInvoke(attach, attachTo, _attachFlag_native) != 0;
		[DllImport("user32.dll", EntryPoint = "AttachThreadInput", ExactSpelling = true)]
		static extern int __PInvoke(uint __attach_native, uint __attachTo_native, int __attachFlag_native);
	}

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static extern bool EnumWindows(EnumWindowsProc callback, nint parameter);

	[LibraryImport("user32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal unsafe static bool GetIconInfo(nint icon, out IconInfo iconInfo)
	{
		iconInfo = default;
		int num;
		fixed (IconInfo* _iconInfo_native = &iconInfo)
		{
			num = __PInvoke(icon, _iconInfo_native);
		}
		return num != 0;
		[DllImport("user32.dll", EntryPoint = "GetIconInfo", ExactSpelling = true)]
		unsafe static extern int __PInvoke(nint __icon_native, IconInfo* __iconInfo_native);
	}
}
