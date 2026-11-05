using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using GlassDesk.Platform.Interop;

[DynamicInterfaceCastableImplementation]
internal interface _003CGlassDesk_Platform_Interop_IShellItemImageFactory_003EF9E2209CFDD053EE2CAC00BE767B164BF0FFB25647047E33864DA6E1C345105CF__InterfaceImplementation : IShellItemImageFactory
{
	[FixedAddressValueType]
	static readonly _003CGlassDesk_Platform_Interop_IShellItemImageFactory_003EF9E2209CFDD053EE2CAC00BE767B164BF0FFB25647047E33864DA6E1C345105CF__InterfaceImplementationVtable Vtable;

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe int IShellItemImageFactory.GetImage(Size size, int flags, out nint bitmap)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IShellItemImageFactory));
		delegate* unmanaged[MemberFunction]<void*, Size, int, nint*, int> obj = (delegate* unmanaged[MemberFunction]<void*, Size, int, nint*, int>)ptr4[3];
		bitmap = 0;
		int result;
		fixed (nint* ptr5 = &bitmap)
		{
			result = obj(ptr3, size, flags, ptr5);
		}
		GC.KeepAlive(this);
		return result;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetImage(ComWrappers.ComInterfaceDispatch* __this_native, Size size, int flags, nint* __bitmap_native__param)
	{
		ref nint reference = ref *__bitmap_native__param;
		nint bitmap = 0;
		int num = 0;
		try
		{
			num = ComWrappers.ComInterfaceDispatch.GetInstance<IShellItemImageFactory>(__this_native).GetImage(size, flags, out bitmap);
			reference = bitmap;
		}
		catch (Exception e)
		{
			num = ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
		return num;
	}

	unsafe static _003CGlassDesk_Platform_Interop_IShellItemImageFactory_003EF9E2209CFDD053EE2CAC00BE767B164BF0FFB25647047E33864DA6E1C345105CF__InterfaceImplementation()
	{
		ComWrappers.GetIUnknownImpl(out *(nint*)(&((_003CGlassDesk_Platform_Interop_IShellItemImageFactory_003EF9E2209CFDD053EE2CAC00BE767B164BF0FFB25647047E33864DA6E1C345105CF__InterfaceImplementationVtable*)Unsafe.AsPointer(in Vtable))->QueryInterface_0), out *(nint*)(&((_003CGlassDesk_Platform_Interop_IShellItemImageFactory_003EF9E2209CFDD053EE2CAC00BE767B164BF0FFB25647047E33864DA6E1C345105CF__InterfaceImplementationVtable*)Unsafe.AsPointer(in Vtable))->AddRef_1), out *(nint*)(&((_003CGlassDesk_Platform_Interop_IShellItemImageFactory_003EF9E2209CFDD053EE2CAC00BE767B164BF0FFB25647047E33864DA6E1C345105CF__InterfaceImplementationVtable*)Unsafe.AsPointer(in Vtable))->Release_2));
		Vtable.GetImage_3 = &ABI_GetImage;
	}
}
