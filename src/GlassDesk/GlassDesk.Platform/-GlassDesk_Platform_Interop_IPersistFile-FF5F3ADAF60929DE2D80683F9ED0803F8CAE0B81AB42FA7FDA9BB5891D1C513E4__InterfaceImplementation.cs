using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using GlassDesk.Platform.Interop;

[DynamicInterfaceCastableImplementation]
internal interface _003CGlassDesk_Platform_Interop_IPersistFile_003EFF5F3ADAF60929DE2D80683F9ED0803F8CAE0B81AB42FA7FDA9BB5891D1C513E4__InterfaceImplementation : IPersistFile
{
	[FixedAddressValueType]
	static readonly _003CGlassDesk_Platform_Interop_IPersistFile_003EFF5F3ADAF60929DE2D80683F9ED0803F8CAE0B81AB42FA7FDA9BB5891D1C513E4__InterfaceImplementationVtable Vtable;

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe void IPersistFile.GetClassID(out Guid classId)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPersistFile));
		delegate* unmanaged[MemberFunction]<void*, Guid*, int> obj = (delegate* unmanaged[MemberFunction]<void*, Guid*, int>)ptr4[3];
		classId = default;
		int errorCode;
		fixed (Guid* ptr5 = &classId)
		{
			errorCode = obj(ptr3, ptr5);
		}
		Marshal.ThrowExceptionForHR(errorCode, new Guid((ReadOnlySpan<byte>)new byte[16]
		{
			11, 1, 0, 0, 0, 0, 0, 0, 192, 0,
			0, 0, 0, 0, 0, 70
		}), (nint)ptr3);
		GC.KeepAlive(this);
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe int IPersistFile.IsDirty()
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPersistFile));
		int result = ((delegate* unmanaged[MemberFunction]<void*, int>)ptr4[4])(ptr3);
		GC.KeepAlive(this);
		return result;
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe void IPersistFile.Load(string fileName, uint mode)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPersistFile));
		delegate* unmanaged[MemberFunction]<void*, ushort*, uint, int> obj = (delegate* unmanaged[MemberFunction]<void*, ushort*, uint, int>)ptr4[5];
		int errorCode;
		fixed (char* ptr5 = &Utf16StringMarshaller.GetPinnableReference(fileName))
		{
			void* ptr6 = ptr5;
			errorCode = obj(ptr3, (ushort*)ptr6, mode);
		}
		Marshal.ThrowExceptionForHR(errorCode, new Guid((ReadOnlySpan<byte>)new byte[16]
		{
			11, 1, 0, 0, 0, 0, 0, 0, 192, 0,
			0, 0, 0, 0, 0, 70
		}), (nint)ptr3);
		GC.KeepAlive(this);
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe void IPersistFile.Save(string fileName, bool remember)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPersistFile));
		delegate* unmanaged[MemberFunction]<void*, ushort*, int, int> obj = (delegate* unmanaged[MemberFunction]<void*, ushort*, int, int>)ptr4[6];
		int num = (remember ? 1 : 0);
		int errorCode;
		fixed (char* ptr5 = &Utf16StringMarshaller.GetPinnableReference(fileName))
		{
			void* ptr6 = ptr5;
			errorCode = obj(ptr3, (ushort*)ptr6, num);
		}
		Marshal.ThrowExceptionForHR(errorCode, new Guid((ReadOnlySpan<byte>)new byte[16]
		{
			11, 1, 0, 0, 0, 0, 0, 0, 192, 0,
			0, 0, 0, 0, 0, 70
		}), (nint)ptr3);
		GC.KeepAlive(this);
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe void IPersistFile.SaveCompleted(string fileName)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPersistFile));
		delegate* unmanaged[MemberFunction]<void*, ushort*, int> obj = (delegate* unmanaged[MemberFunction]<void*, ushort*, int>)ptr4[7];
		int errorCode;
		fixed (char* ptr5 = &Utf16StringMarshaller.GetPinnableReference(fileName))
		{
			void* ptr6 = ptr5;
			errorCode = obj(ptr3, (ushort*)ptr6);
		}
		Marshal.ThrowExceptionForHR(errorCode, new Guid((ReadOnlySpan<byte>)new byte[16]
		{
			11, 1, 0, 0, 0, 0, 0, 0, 192, 0,
			0, 0, 0, 0, 0, 70
		}), (nint)ptr3);
		GC.KeepAlive(this);
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe void IPersistFile.GetCurFile(out nint fileName)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IPersistFile));
		delegate* unmanaged[MemberFunction]<void*, nint*, int> obj = (delegate* unmanaged[MemberFunction]<void*, nint*, int>)ptr4[8];
		fileName = 0;
		int errorCode;
		fixed (nint* ptr5 = &fileName)
		{
			errorCode = obj(ptr3, ptr5);
		}
		Marshal.ThrowExceptionForHR(errorCode, new Guid((ReadOnlySpan<byte>)new byte[16]
		{
			11, 1, 0, 0, 0, 0, 0, 0, 192, 0,
			0, 0, 0, 0, 0, 70
		}), (nint)ptr3);
		GC.KeepAlive(this);
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetClassID(ComWrappers.ComInterfaceDispatch* __this_native, Guid* __classId_native__param)
	{
		ref Guid reference = ref *__classId_native__param;
		Guid classId = default;
		int num = 0;
		try
		{
			ComWrappers.ComInterfaceDispatch.GetInstance<IPersistFile>(__this_native).GetClassID(out classId);
			num = 0;
			reference = classId;
		}
		catch (Exception e)
		{
			num = ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
		return num;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_IsDirty(ComWrappers.ComInterfaceDispatch* __this_native)
	{
		int num = 0;
		try
		{
			return ComWrappers.ComInterfaceDispatch.GetInstance<IPersistFile>(__this_native).IsDirty();
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_Load(ComWrappers.ComInterfaceDispatch* __this_native, ushort* __fileName_native, uint mode)
	{
		string text = null;
		int num = 0;
		try
		{
			text = Utf16StringMarshaller.ConvertToManaged(__fileName_native);
			ComWrappers.ComInterfaceDispatch.GetInstance<IPersistFile>(__this_native).Load(text, mode);
			return 0;
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_Save(ComWrappers.ComInterfaceDispatch* __this_native, ushort* __fileName_native, int __remember_native)
	{
		string text = null;
		bool flag = false;
		int num = 0;
		try
		{
			flag = __remember_native != 0;
			text = Utf16StringMarshaller.ConvertToManaged(__fileName_native);
			ComWrappers.ComInterfaceDispatch.GetInstance<IPersistFile>(__this_native).Save(text, flag);
			return 0;
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_SaveCompleted(ComWrappers.ComInterfaceDispatch* __this_native, ushort* __fileName_native)
	{
		string text = null;
		int num = 0;
		try
		{
			text = Utf16StringMarshaller.ConvertToManaged(__fileName_native);
			ComWrappers.ComInterfaceDispatch.GetInstance<IPersistFile>(__this_native).SaveCompleted(text);
			return 0;
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetCurFile(ComWrappers.ComInterfaceDispatch* __this_native, nint* __fileName_native__param)
	{
		ref nint reference = ref *__fileName_native__param;
		nint fileName = 0;
		int num = 0;
		try
		{
			ComWrappers.ComInterfaceDispatch.GetInstance<IPersistFile>(__this_native).GetCurFile(out fileName);
			num = 0;
			reference = fileName;
		}
		catch (Exception e)
		{
			num = ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
		return num;
	}

	unsafe static _003CGlassDesk_Platform_Interop_IPersistFile_003EFF5F3ADAF60929DE2D80683F9ED0803F8CAE0B81AB42FA7FDA9BB5891D1C513E4__InterfaceImplementation()
	{
		ComWrappers.GetIUnknownImpl(out *(nint*)(&((_003CGlassDesk_Platform_Interop_IPersistFile_003EFF5F3ADAF60929DE2D80683F9ED0803F8CAE0B81AB42FA7FDA9BB5891D1C513E4__InterfaceImplementationVtable*)Unsafe.AsPointer(in Vtable))->QueryInterface_0), out *(nint*)(&((_003CGlassDesk_Platform_Interop_IPersistFile_003EFF5F3ADAF60929DE2D80683F9ED0803F8CAE0B81AB42FA7FDA9BB5891D1C513E4__InterfaceImplementationVtable*)Unsafe.AsPointer(in Vtable))->AddRef_1), out *(nint*)(&((_003CGlassDesk_Platform_Interop_IPersistFile_003EFF5F3ADAF60929DE2D80683F9ED0803F8CAE0B81AB42FA7FDA9BB5891D1C513E4__InterfaceImplementationVtable*)Unsafe.AsPointer(in Vtable))->Release_2));
		Vtable.GetClassID_3 = &ABI_GetClassID;
		Vtable.IsDirty_4 = &ABI_IsDirty;
		Vtable.Load_5 = &ABI_Load;
		Vtable.Save_6 = &ABI_Save;
		Vtable.SaveCompleted_7 = &ABI_SaveCompleted;
		Vtable.GetCurFile_8 = &ABI_GetCurFile;
	}
}
