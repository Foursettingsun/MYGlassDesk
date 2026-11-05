using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using GlassDesk.Platform.Interop;

[DynamicInterfaceCastableImplementation]
internal interface _003CGlassDesk_Platform_Interop_IPropertyStore_003EF57EBA8FEBF09B3B59B40A38D613645008C336C3D47690A5CF845E03C4F790F00__InterfaceImplementation : IPropertyStore
{
	[FixedAddressValueType]
	static readonly _003CGlassDesk_Platform_Interop_IPropertyStore_003EF57EBA8FEBF09B3B59B40A38D613645008C336C3D47690A5CF845E03C4F790F00__InterfaceImplementationVtable Vtable;

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe int IPropertyStore.GetCount(out uint propertyCount)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPropertyStore));
		delegate* unmanaged[MemberFunction]<void*, uint*, int> obj = (delegate* unmanaged[MemberFunction]<void*, uint*, int>)ptr4[3];
		propertyCount = 0u;
		int result;
		fixed (uint* ptr5 = &propertyCount)
		{
			result = obj(ptr3, ptr5);
		}
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe int IPropertyStore.GetAt(uint propertyIndex, out PropertyKey key)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPropertyStore));
		delegate* unmanaged[MemberFunction]<void*, uint, PropertyKey*, int> obj = (delegate* unmanaged[MemberFunction]<void*, uint, PropertyKey*, int>)ptr4[4];
		key = default;
		int result;
		fixed (PropertyKey* ptr5 = &key)
		{
			result = obj(ptr3, propertyIndex, ptr5);
		}
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe int IPropertyStore.GetValue(in PropertyKey key, out PropVariant value)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPropertyStore));
		delegate* unmanaged[MemberFunction]<void*, PropertyKey*, PropVariant*, int> obj = (delegate* unmanaged[MemberFunction]<void*, PropertyKey*, PropVariant*, int>)ptr4[5];
		value = default;
		int result;
		fixed (PropVariant* ptr5 = &value)
		{
			fixed (PropertyKey* ptr6 = &key)
			{
				result = obj(ptr3, ptr6, ptr5);
			}
		}
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe int IPropertyStore.SetValue(in PropertyKey key, in PropVariant value)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPropertyStore));
		delegate* unmanaged[MemberFunction]<void*, PropertyKey*, PropVariant*, int> obj = (delegate* unmanaged[MemberFunction]<void*, PropertyKey*, PropVariant*, int>)ptr4[6];
		int result;
		fixed (PropVariant* ptr5 = &value)
		{
			fixed (PropertyKey* ptr6 = &key)
			{
				result = obj(ptr3, ptr6, ptr5);
			}
		}
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe int IPropertyStore.Commit()
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPropertyStore));
		int result = ((delegate* unmanaged[MemberFunction]<void*, int>)ptr4[7])(ptr3);
		GC.KeepAlive(this);
		return result;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetCount(ComWrappers.ComInterfaceDispatch* __this_native, uint* __propertyCount_native__param)
	{
		ref uint reference = ref *__propertyCount_native__param;
		uint propertyCount = 0u;
		int num = 0;
		try
		{
			num = ComWrappers.ComInterfaceDispatch.GetInstance<IPropertyStore>(__this_native).GetCount(out propertyCount);
			reference = propertyCount;
		}
		catch (Exception e)
		{
			num = ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
		return num;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetAt(ComWrappers.ComInterfaceDispatch* __this_native, uint propertyIndex, PropertyKey* __key_native__param)
	{
		ref PropertyKey reference = ref *__key_native__param;
		PropertyKey key = default;
		int num = 0;
		try
		{
			num = ComWrappers.ComInterfaceDispatch.GetInstance<IPropertyStore>(__this_native).GetAt(propertyIndex, out key);
			reference = key;
		}
		catch (Exception e)
		{
			num = ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
		return num;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetValue(ComWrappers.ComInterfaceDispatch* __this_native, PropertyKey* __key_native__param, PropVariant* __value_native__param)
	{
		ref PropertyKey reference = ref *__key_native__param;
		PropertyKey propertyKey = default;
		ref PropVariant reference2 = ref *__value_native__param;
		PropVariant value = default;
		int num = 0;
		try
		{
			propertyKey = reference;
			num = ComWrappers.ComInterfaceDispatch.GetInstance<IPropertyStore>(__this_native).GetValue(in propertyKey, out value);
			reference2 = value;
		}
		catch (Exception e)
		{
			num = ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
		return num;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_SetValue(ComWrappers.ComInterfaceDispatch* __this_native, PropertyKey* __key_native__param, PropVariant* __value_native__param)
	{
		ref PropertyKey reference = ref *__key_native__param;
		PropertyKey propertyKey = default;
		ref PropVariant reference2 = ref *__value_native__param;
		PropVariant propVariant = default;
		int num = 0;
		try
		{
			propVariant = reference2;
			propertyKey = reference;
			return ComWrappers.ComInterfaceDispatch.GetInstance<IPropertyStore>(__this_native).SetValue(in propertyKey, in propVariant);
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_Commit(ComWrappers.ComInterfaceDispatch* __this_native)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IPropertyStore>(__this_native).Commit();
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	unsafe static _003CGlassDesk_Platform_Interop_IPropertyStore_003EF57EBA8FEBF09B3B59B40A38D613645008C336C3D47690A5CF845E03C4F790F00__InterfaceImplementation()
	{
		ComWrappers.GetIUnknownImpl(out *(nint*)(&((_003CGlassDesk_Platform_Interop_IPropertyStore_003EF57EBA8FEBF09B3B59B40A38D613645008C336C3D47690A5CF845E03C4F790F00__InterfaceImplementationVtable*)Unsafe.AsPointer(in Vtable))->QueryInterface_0), out *(nint*)(&((_003CGlassDesk_Platform_Interop_IPropertyStore_003EF57EBA8FEBF09B3B59B40A38D613645008C336C3D47690A5CF845E03C4F790F00__InterfaceImplementationVtable*)Unsafe.AsPointer(in Vtable))->AddRef_1), out *(nint*)(&((_003CGlassDesk_Platform_Interop_IPropertyStore_003EF57EBA8FEBF09B3B59B40A38D613645008C336C3D47690A5CF845E03C4F790F00__InterfaceImplementationVtable*)Unsafe.AsPointer(in Vtable))->Release_2));
		Vtable.GetCount_3 = &ABI_GetCount;
		Vtable.GetAt_4 = &ABI_GetAt;
		Vtable.GetValue_5 = &ABI_GetValue;
		Vtable.SetValue_6 = &ABI_SetValue;
		Vtable.Commit_7 = &ABI_Commit;
	}
}
