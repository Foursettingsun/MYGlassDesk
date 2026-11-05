using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace GlassDesk.Platform.Interop;

internal static class Gdi32
{
	internal const int DibRgbColors = 0;

	internal const int BiBitfields = 3;

	internal const int DibRgb = 0;

	[DllImport("user32.dll", ExactSpelling = true)]
	[LibraryImport("user32.dll")]
	internal static extern nint GetDC(nint window);

	[DllImport("user32.dll", ExactSpelling = true)]
	[LibraryImport("user32.dll")]
	internal static extern int ReleaseDC(nint window, nint dc);

	[LibraryImport("gdi32.dll", EntryPoint = "CreateDIBSection")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	internal unsafe static nint CreateDibSection(nint dc, ref BitmapV5Header header, int usage, out nint bits, nint section, int offset)
	{
		bits = 0;
		nint result;
		fixed (nint* _bits_native = &bits)
		{
			fixed (BitmapV5Header* _header_native = &header)
			{
				result = __PInvoke(dc, _header_native, usage, _bits_native, section, offset);
			}
		}
		return result;
		[DllImport("gdi32.dll", EntryPoint = "CreateDIBSection", ExactSpelling = true)]
		unsafe static extern nint __PInvoke(nint __dc_native, BitmapV5Header* __header_native, int __usage_native, nint* __bits_native, nint __section_native, int __offset_native);
	}

	[LibraryImport("gdi32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	internal unsafe static nint CreateBitmap(int width, int height, uint planes, uint bitsPerPixel, byte[]? bits)
	{
		nint result;
		fixed (byte* ptr = &ArrayMarshaller<byte, byte>.ManagedToUnmanagedIn.GetPinnableReference(bits))
		{
			void* _bits_native = ptr;
			result = __PInvoke(width, height, planes, bitsPerPixel, (byte*)_bits_native);
		}
		return result;
		[DllImport("gdi32.dll", EntryPoint = "CreateBitmap", ExactSpelling = true)]
		unsafe static extern nint __PInvoke(int __width_native, int __height_native, uint __planes_native, uint __bitsPerPixel_native, byte* __bits_native);
	}

	[LibraryImport("gdi32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal static bool DeleteObject(nint handle)
	{
		return __PInvoke(handle) != 0;
		[DllImport("gdi32.dll", EntryPoint = "DeleteObject", ExactSpelling = true)]
		static extern int __PInvoke(nint __handle_native);
	}

	[LibraryImport("user32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	internal unsafe static nint CreateIconIndirect(ref IconInfo iconInfo)
	{
		nint result;
		fixed (IconInfo* _iconInfo_native = &iconInfo)
		{
			result = __PInvoke(_iconInfo_native);
		}
		return result;
		[DllImport("user32.dll", EntryPoint = "CreateIconIndirect", ExactSpelling = true)]
		unsafe static extern nint __PInvoke(IconInfo* __iconInfo_native);
	}

	[LibraryImport("gdi32.dll", EntryPoint = "GetObjectW")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	internal unsafe static int GetObject(nint handle, int size, out Bitmap target)
	{
		target = default;
		int result;
		fixed (Bitmap* _target_native = &target)
		{
			result = __PInvoke(handle, size, _target_native);
		}
		return result;
		[DllImport("gdi32.dll", EntryPoint = "GetObjectW", ExactSpelling = true)]
		unsafe static extern int __PInvoke(nint __handle_native, int __size_native, Bitmap* __target_native);
	}

	internal unsafe static int GetDIBits(nint deviceContext, nint bitmap, uint start, uint lines, byte* bits, BitmapInfo* info)
	{
		return GetDIBitsCore(deviceContext, bitmap, start, lines, bits, info, 0);
	}

	[DllImport("gdi32.dll", EntryPoint = "GetDIBits", ExactSpelling = true)]
	[LibraryImport("gdi32.dll", EntryPoint = "GetDIBits")]
	private unsafe static extern int GetDIBitsCore(nint deviceContext, nint bitmap, uint start, uint lines, byte* bits, BitmapInfo* info, int usage);
}
