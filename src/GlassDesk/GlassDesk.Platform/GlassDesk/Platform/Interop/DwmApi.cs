using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GlassDesk.Platform.Interop;

internal static class DwmApi
{
	internal const int DwmwaSystemBackdropType = 38;

	internal const int DwmwaWindowCornerPreference = 33;

	internal const int DwmwaUseImmersiveDarkMode = 20;

	internal const int DwmwaBorderColor = 34;

	internal const int DwmwaExtendedFrameBounds = 9;

	internal const int DwmwaCloaked = 14;

	[LibraryImport("dwmapi.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	private unsafe static int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int size)
	{
		int result;
		fixed (int* _value_native = &value)
		{
			result = __PInvoke(hwnd, attribute, _value_native, size);
		}
		return result;
		[DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute", ExactSpelling = true)]
		unsafe static extern int __PInvoke(nint __hwnd_native, int __attribute_native, int* __value_native, int __size_native);
	}

	[LibraryImport("dwmapi.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	private unsafe static int DwmExtendFrameIntoClientArea(nint hwnd, ref Margins margins)
	{
		int result;
		fixed (Margins* _margins_native = &margins)
		{
			result = __PInvoke(hwnd, _margins_native);
		}
		return result;
		[DllImport("dwmapi.dll", EntryPoint = "DwmExtendFrameIntoClientArea", ExactSpelling = true)]
		unsafe static extern int __PInvoke(nint __hwnd_native, Margins* __margins_native);
	}

	[LibraryImport("dwmapi.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	private unsafe static int DwmGetWindowAttribute(nint hwnd, int attribute, out Rect value, int size)
	{
		value = default;
		int result;
		fixed (Rect* _value_native = &value)
		{
			result = __PInvoke(hwnd, attribute, _value_native, size);
		}
		return result;
		[DllImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute", ExactSpelling = true)]
		unsafe static extern int __PInvoke(nint __hwnd_native, int __attribute_native, Rect* __value_native, int __size_native);
	}

	[LibraryImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	private unsafe static int DwmGetWindowIntAttribute(nint hwnd, int attribute, out int value, int size)
	{
		value = 0;
		int result;
		fixed (int* _value_native = &value)
		{
			result = __PInvoke(hwnd, attribute, _value_native, size);
		}
		return result;
		[DllImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute", ExactSpelling = true)]
		unsafe static extern int __PInvoke(nint __hwnd_native, int __attribute_native, int* __value_native, int __size_native);
	}

	internal static bool TryGetExtendedFrameBounds(nint hwnd, out Rect bounds)
	{
		bounds = default;
		if (hwnd == 0)
		{
			return false;
		}
		try
		{
			return DwmGetWindowAttribute(hwnd, 9, out bounds, Marshal.SizeOf<Rect>()) == 0 && bounds.Width > 0 && bounds.Height > 0;
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return false;
		}
	}

	internal static bool IsCloaked(nint hwnd)
	{
		if (hwnd == 0)
		{
			return false;
		}
		try
		{
			int value;
			return DwmGetWindowIntAttribute(hwnd, 14, out value, 4) == 0 && value != 0;
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return false;
		}
	}

	internal static bool TrySetAttribute(nint hwnd, int attribute, int value)
	{
		if (hwnd == 0)
		{
			return false;
		}
		try
		{
			return DwmSetWindowAttribute(hwnd, attribute, ref value, 4) == 0;
		}
		catch (DllNotFoundException)
		{
			return false;
		}
		catch (EntryPointNotFoundException)
		{
			return false;
		}
	}

	internal static bool TryExtendFrameIntoClientArea(nint hwnd, int left, int top, int right, int bottom)
	{
		if (hwnd == 0)
		{
			return false;
		}
		Margins margins = new Margins
		{
			Left = left,
			Top = top,
			Right = right,
			Bottom = bottom
		};
		try
		{
			return DwmExtendFrameIntoClientArea(hwnd, ref margins) == 0;
		}
		catch (DllNotFoundException)
		{
			return false;
		}
		catch (EntryPointNotFoundException)
		{
			return false;
		}
	}
}
