using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using GlassDesk.Platform.Interop;

[DynamicInterfaceCastableImplementation]
internal interface _003CGlassDesk_Platform_Interop_IShellLinkW_003EFC159A7D2188C344C8DE588EDB0C2921240B69E43B8FB7138D5F76BACD273C869__InterfaceImplementation : IShellLinkW
{
	[FixedAddressValueType]
	static readonly _003CGlassDesk_Platform_Interop_IShellLinkW_003EFC159A7D2188C344C8DE588EDB0C2921240B69E43B8FB7138D5F76BACD273C869__InterfaceImplementationVtable Vtable;

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe void IShellLinkW.GetPath(nint fileOut, int maxPath, nint findData, uint flags)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IShellLinkW));
		Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, nint, int, nint, uint, int>)ptr4[3])(ptr3, fileOut, maxPath, findData, flags), new Guid((ReadOnlySpan<byte>)new byte[16]
		{
			249, 20, 2, 0, 0, 0, 0, 0, 192, 0,
			0, 0, 0, 0, 0, 70
		}), (nint)ptr3);
		GC.KeepAlive(this);
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe void IShellLinkW.GetIDList(out nint itemIdList)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IShellLinkW));
		delegate* unmanaged[MemberFunction]<void*, nint*, int> obj = (delegate* unmanaged[MemberFunction]<void*, nint*, int>)ptr4[4];
		itemIdList = 0;
		int errorCode;
		fixed (nint* ptr5 = &itemIdList)
		{
			errorCode = obj(ptr3, ptr5);
		}
		Marshal.ThrowExceptionForHR(errorCode, new Guid((ReadOnlySpan<byte>)new byte[16]
		{
			249, 20, 2, 0, 0, 0, 0, 0, 192, 0,
			0, 0, 0, 0, 0, 70
		}), (nint)ptr3);
		GC.KeepAlive(this);
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe void IShellLinkW.SetIDList(nint itemIdList)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IShellLinkW));
		Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, nint, int>)ptr4[5])(ptr3, itemIdList), new Guid((ReadOnlySpan<byte>)new byte[16]
		{
			249, 20, 2, 0, 0, 0, 0, 0, 192, 0,
			0, 0, 0, 0, 0, 70
		}), (nint)ptr3);
		GC.KeepAlive(this);
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe void IShellLinkW.GetDescription(nint nameOut, int maxName)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IShellLinkW));
		Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, nint, int, int>)ptr4[6])(ptr3, nameOut, maxName), new Guid((ReadOnlySpan<byte>)new byte[16]
		{
			249, 20, 2, 0, 0, 0, 0, 0, 192, 0,
			0, 0, 0, 0, 0, 70
		}), (nint)ptr3);
		GC.KeepAlive(this);
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe void IShellLinkW.SetDescription(string name)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IShellLinkW));
		delegate* unmanaged[MemberFunction]<void*, ushort*, int> obj = (delegate* unmanaged[MemberFunction]<void*, ushort*, int>)ptr4[7];
		int errorCode;
		fixed (char* ptr5 = &Utf16StringMarshaller.GetPinnableReference(name))
		{
			void* ptr6 = ptr5;
			errorCode = obj(ptr3, (ushort*)ptr6);
		}
		Marshal.ThrowExceptionForHR(errorCode, new Guid((ReadOnlySpan<byte>)new byte[16]
		{
			249, 20, 2, 0, 0, 0, 0, 0, 192, 0,
			0, 0, 0, 0, 0, 70
		}), (nint)ptr3);
		GC.KeepAlive(this);
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe void IShellLinkW.GetWorkingDirectory(nint dirOut, int maxPath)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IShellLinkW));
		Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, nint, int, int>)ptr4[8])(ptr3, dirOut, maxPath), new Guid((ReadOnlySpan<byte>)new byte[16]
		{
			249, 20, 2, 0, 0, 0, 0, 0, 192, 0,
			0, 0, 0, 0, 0, 70
		}), (nint)ptr3);
		GC.KeepAlive(this);
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe void IShellLinkW.SetWorkingDirectory(string dir)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IShellLinkW));
		delegate* unmanaged[MemberFunction]<void*, ushort*, int> obj = (delegate* unmanaged[MemberFunction]<void*, ushort*, int>)ptr4[9];
		int errorCode;
		fixed (char* ptr5 = &Utf16StringMarshaller.GetPinnableReference(dir))
		{
			void* ptr6 = ptr5;
			errorCode = obj(ptr3, (ushort*)ptr6);
		}
		Marshal.ThrowExceptionForHR(errorCode, new Guid((ReadOnlySpan<byte>)new byte[16]
		{
			249, 20, 2, 0, 0, 0, 0, 0, 192, 0,
			0, 0, 0, 0, 0, 70
		}), (nint)ptr3);
		GC.KeepAlive(this);
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe void IShellLinkW.GetArguments(nint argsOut, int maxPath)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IShellLinkW));
		Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, nint, int, int>)ptr4[10])(ptr3, argsOut, maxPath), new Guid((ReadOnlySpan<byte>)new byte[16]
		{
			249, 20, 2, 0, 0, 0, 0, 0, 192, 0,
			0, 0, 0, 0, 0, 70
		}), (nint)ptr3);
		GC.KeepAlive(this);
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe void IShellLinkW.SetArguments(string args)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IShellLinkW));
		delegate* unmanaged[MemberFunction]<void*, ushort*, int> obj = (delegate* unmanaged[MemberFunction]<void*, ushort*, int>)ptr4[11];
		int errorCode;
		fixed (char* ptr5 = &Utf16StringMarshaller.GetPinnableReference(args))
		{
			void* ptr6 = ptr5;
			errorCode = obj(ptr3, (ushort*)ptr6);
		}
		Marshal.ThrowExceptionForHR(errorCode, new Guid((ReadOnlySpan<byte>)new byte[16]
		{
			249, 20, 2, 0, 0, 0, 0, 0, 192, 0,
			0, 0, 0, 0, 0, 70
		}), (nint)ptr3);
		GC.KeepAlive(this);
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe void IShellLinkW.GetHotkey(out ushort hotkey)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IShellLinkW));
		delegate* unmanaged[MemberFunction]<void*, ushort*, int> obj = (delegate* unmanaged[MemberFunction]<void*, ushort*, int>)ptr4[12];
		hotkey = 0;
		int errorCode;
		fixed (ushort* ptr5 = &hotkey)
		{
			errorCode = obj(ptr3, ptr5);
		}
		Marshal.ThrowExceptionForHR(errorCode, new Guid((ReadOnlySpan<byte>)new byte[16]
		{
			249, 20, 2, 0, 0, 0, 0, 0, 192, 0,
			0, 0, 0, 0, 0, 70
		}), (nint)ptr3);
		GC.KeepAlive(this);
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe void IShellLinkW.SetHotkey(ushort hotkey)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IShellLinkW));
		Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, ushort, int>)ptr4[13])(ptr3, hotkey), new Guid((ReadOnlySpan<byte>)new byte[16]
		{
			249, 20, 2, 0, 0, 0, 0, 0, 192, 0,
			0, 0, 0, 0, 0, 70
		}), (nint)ptr3);
		GC.KeepAlive(this);
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe void IShellLinkW.GetShowCmd(out int showCommand)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IShellLinkW));
		delegate* unmanaged[MemberFunction]<void*, int*, int> obj = (delegate* unmanaged[MemberFunction]<void*, int*, int>)ptr4[14];
		showCommand = 0;
		int errorCode;
		fixed (int* ptr5 = &showCommand)
		{
			errorCode = obj(ptr3, ptr5);
		}
		Marshal.ThrowExceptionForHR(errorCode, new Guid((ReadOnlySpan<byte>)new byte[16]
		{
			249, 20, 2, 0, 0, 0, 0, 0, 192, 0,
			0, 0, 0, 0, 0, 70
		}), (nint)ptr3);
		GC.KeepAlive(this);
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe void IShellLinkW.SetShowCmd(int showCommand)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IShellLinkW));
		Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, int, int>)ptr4[15])(ptr3, showCommand), new Guid((ReadOnlySpan<byte>)new byte[16]
		{
			249, 20, 2, 0, 0, 0, 0, 0, 192, 0,
			0, 0, 0, 0, 0, 70
		}), (nint)ptr3);
		GC.KeepAlive(this);
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe void IShellLinkW.GetIconLocation(nint iconOut, int maxPath, out int iconIndex)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IShellLinkW));
		delegate* unmanaged[MemberFunction]<void*, nint, int, int*, int> obj = (delegate* unmanaged[MemberFunction]<void*, nint, int, int*, int>)ptr4[16];
		iconIndex = 0;
		int errorCode;
		fixed (int* ptr5 = &iconIndex)
		{
			errorCode = obj(ptr3, iconOut, maxPath, ptr5);
		}
		Marshal.ThrowExceptionForHR(errorCode, new Guid((ReadOnlySpan<byte>)new byte[16]
		{
			249, 20, 2, 0, 0, 0, 0, 0, 192, 0,
			0, 0, 0, 0, 0, 70
		}), (nint)ptr3);
		GC.KeepAlive(this);
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe void IShellLinkW.SetIconLocation(string iconPath, int iconIndex)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IShellLinkW));
		delegate* unmanaged[MemberFunction]<void*, ushort*, int, int> obj = (delegate* unmanaged[MemberFunction]<void*, ushort*, int, int>)ptr4[17];
		int errorCode;
		fixed (char* ptr5 = &Utf16StringMarshaller.GetPinnableReference(iconPath))
		{
			void* ptr6 = ptr5;
			errorCode = obj(ptr3, (ushort*)ptr6, iconIndex);
		}
		Marshal.ThrowExceptionForHR(errorCode, new Guid((ReadOnlySpan<byte>)new byte[16]
		{
			249, 20, 2, 0, 0, 0, 0, 0, 192, 0,
			0, 0, 0, 0, 0, 70
		}), (nint)ptr3);
		GC.KeepAlive(this);
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe void IShellLinkW.SetRelativePath(string pathRel, uint reserved)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IShellLinkW));
		delegate* unmanaged[MemberFunction]<void*, ushort*, uint, int> obj = (delegate* unmanaged[MemberFunction]<void*, ushort*, uint, int>)ptr4[18];
		int errorCode;
		fixed (char* ptr5 = &Utf16StringMarshaller.GetPinnableReference(pathRel))
		{
			void* ptr6 = ptr5;
			errorCode = obj(ptr3, (ushort*)ptr6, reserved);
		}
		Marshal.ThrowExceptionForHR(errorCode, new Guid((ReadOnlySpan<byte>)new byte[16]
		{
			249, 20, 2, 0, 0, 0, 0, 0, 192, 0,
			0, 0, 0, 0, 0, 70
		}), (nint)ptr3);
		GC.KeepAlive(this);
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe void IShellLinkW.Resolve(nint window, uint flags)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IShellLinkW));
		Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, nint, uint, int>)ptr4[19])(ptr3, window, flags), new Guid((ReadOnlySpan<byte>)new byte[16]
		{
			249, 20, 2, 0, 0, 0, 0, 0, 192, 0,
			0, 0, 0, 0, 0, 70
		}), (nint)ptr3);
		GC.KeepAlive(this);
	}

	[GeneratedCode("Microsoft.Interop.ComInterfaceGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	unsafe void IShellLinkW.SetPath(string path)
	{
		var (ptr3, ptr4) = ((IUnmanagedVirtualMethodTableProvider)this).GetVirtualMethodTableInfoForKey(typeof(IShellLinkW));
		delegate* unmanaged[MemberFunction]<void*, ushort*, int> obj = (delegate* unmanaged[MemberFunction]<void*, ushort*, int>)ptr4[20];
		int errorCode;
		fixed (char* ptr5 = &Utf16StringMarshaller.GetPinnableReference(path))
		{
			void* ptr6 = ptr5;
			errorCode = obj(ptr3, (ushort*)ptr6);
		}
		Marshal.ThrowExceptionForHR(errorCode, new Guid((ReadOnlySpan<byte>)new byte[16]
		{
			249, 20, 2, 0, 0, 0, 0, 0, 192, 0,
			0, 0, 0, 0, 0, 70
		}), (nint)ptr3);
		GC.KeepAlive(this);
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetPath(ComWrappers.ComInterfaceDispatch* __this_native, nint fileOut, int maxPath, nint findData, uint flags)
	{
		int num = 0;
		try
		{
			ComWrappers.ComInterfaceDispatch.GetInstance<IShellLinkW>(__this_native).GetPath(fileOut, maxPath, findData, flags);
			return 0;
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetIDList(ComWrappers.ComInterfaceDispatch* __this_native, nint* __itemIdList_native__param)
	{
		ref nint reference = ref *__itemIdList_native__param;
		nint itemIdList = 0;
		int num = 0;
		try
		{
			ComWrappers.ComInterfaceDispatch.GetInstance<IShellLinkW>(__this_native).GetIDList(out itemIdList);
			num = 0;
			reference = itemIdList;
		}
		catch (Exception e)
		{
			num = ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
		return num;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_SetIDList(ComWrappers.ComInterfaceDispatch* __this_native, nint itemIdList)
	{
		int num = 0;
		try
		{
			ComWrappers.ComInterfaceDispatch.GetInstance<IShellLinkW>(__this_native).SetIDList(itemIdList);
			return 0;
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetDescription(ComWrappers.ComInterfaceDispatch* __this_native, nint nameOut, int maxName)
	{
		int num = 0;
		try
		{
			ComWrappers.ComInterfaceDispatch.GetInstance<IShellLinkW>(__this_native).GetDescription(nameOut, maxName);
			return 0;
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_SetDescription(ComWrappers.ComInterfaceDispatch* __this_native, ushort* __name_native)
	{
		string text = null;
		int num = 0;
		try
		{
			text = Utf16StringMarshaller.ConvertToManaged(__name_native);
			ComWrappers.ComInterfaceDispatch.GetInstance<IShellLinkW>(__this_native).SetDescription(text);
			return 0;
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetWorkingDirectory(ComWrappers.ComInterfaceDispatch* __this_native, nint dirOut, int maxPath)
	{
		int num = 0;
		try
		{
			ComWrappers.ComInterfaceDispatch.GetInstance<IShellLinkW>(__this_native).GetWorkingDirectory(dirOut, maxPath);
			return 0;
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_SetWorkingDirectory(ComWrappers.ComInterfaceDispatch* __this_native, ushort* __dir_native)
	{
		string text = null;
		int num = 0;
		try
		{
			text = Utf16StringMarshaller.ConvertToManaged(__dir_native);
			ComWrappers.ComInterfaceDispatch.GetInstance<IShellLinkW>(__this_native).SetWorkingDirectory(text);
			return 0;
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetArguments(ComWrappers.ComInterfaceDispatch* __this_native, nint argsOut, int maxPath)
	{
		int num = 0;
		try
		{
			ComWrappers.ComInterfaceDispatch.GetInstance<IShellLinkW>(__this_native).GetArguments(argsOut, maxPath);
			return 0;
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_SetArguments(ComWrappers.ComInterfaceDispatch* __this_native, ushort* __args_native)
	{
		string text = null;
		int num = 0;
		try
		{
			text = Utf16StringMarshaller.ConvertToManaged(__args_native);
			ComWrappers.ComInterfaceDispatch.GetInstance<IShellLinkW>(__this_native).SetArguments(text);
			return 0;
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetHotkey(ComWrappers.ComInterfaceDispatch* __this_native, ushort* __hotkey_native__param)
	{
		ref ushort reference = ref *__hotkey_native__param;
		ushort hotkey = 0;
		int num = 0;
		try
		{
			ComWrappers.ComInterfaceDispatch.GetInstance<IShellLinkW>(__this_native).GetHotkey(out hotkey);
			num = 0;
			reference = hotkey;
		}
		catch (Exception e)
		{
			num = ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
		return num;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_SetHotkey(ComWrappers.ComInterfaceDispatch* __this_native, ushort hotkey)
	{
		int num = 0;
		try
		{
			ComWrappers.ComInterfaceDispatch.GetInstance<IShellLinkW>(__this_native).SetHotkey(hotkey);
			return 0;
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetShowCmd(ComWrappers.ComInterfaceDispatch* __this_native, int* __showCommand_native__param)
	{
		ref int reference = ref *__showCommand_native__param;
		int showCommand = 0;
		int num = 0;
		try
		{
			ComWrappers.ComInterfaceDispatch.GetInstance<IShellLinkW>(__this_native).GetShowCmd(out showCommand);
			num = 0;
			reference = showCommand;
		}
		catch (Exception e)
		{
			num = ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
		return num;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_SetShowCmd(ComWrappers.ComInterfaceDispatch* __this_native, int showCommand)
	{
		int num = 0;
		try
		{
			ComWrappers.ComInterfaceDispatch.GetInstance<IShellLinkW>(__this_native).SetShowCmd(showCommand);
			return 0;
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_GetIconLocation(ComWrappers.ComInterfaceDispatch* __this_native, nint iconOut, int maxPath, int* __iconIndex_native__param)
	{
		ref int reference = ref *__iconIndex_native__param;
		int iconIndex = 0;
		int num = 0;
		try
		{
			ComWrappers.ComInterfaceDispatch.GetInstance<IShellLinkW>(__this_native).GetIconLocation(iconOut, maxPath, out iconIndex);
			num = 0;
			reference = iconIndex;
		}
		catch (Exception e)
		{
			num = ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
		return num;
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_SetIconLocation(ComWrappers.ComInterfaceDispatch* __this_native, ushort* __iconPath_native, int iconIndex)
	{
		string text = null;
		int num = 0;
		try
		{
			text = Utf16StringMarshaller.ConvertToManaged(__iconPath_native);
			ComWrappers.ComInterfaceDispatch.GetInstance<IShellLinkW>(__this_native).SetIconLocation(text, iconIndex);
			return 0;
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_SetRelativePath(ComWrappers.ComInterfaceDispatch* __this_native, ushort* __pathRel_native, uint reserved)
	{
		string text = null;
		int num = 0;
		try
		{
			text = Utf16StringMarshaller.ConvertToManaged(__pathRel_native);
			ComWrappers.ComInterfaceDispatch.GetInstance<IShellLinkW>(__this_native).SetRelativePath(text, reserved);
			return 0;
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_Resolve(ComWrappers.ComInterfaceDispatch* __this_native, nint window, uint flags)
	{
		int num = 0;
		try
		{
			ComWrappers.ComInterfaceDispatch.GetInstance<IShellLinkW>(__this_native).Resolve(window, flags);
			return 0;
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvMemberFunction) })]
	internal unsafe static int ABI_SetPath(ComWrappers.ComInterfaceDispatch* __this_native, ushort* __path_native)
	{
		string text = null;
		int num = 0;
		try
		{
			text = Utf16StringMarshaller.ConvertToManaged(__path_native);
			ComWrappers.ComInterfaceDispatch.GetInstance<IShellLinkW>(__this_native).SetPath(text);
			return 0;
		}
		catch (Exception e)
		{
			return ExceptionAsHResultMarshaller<int>.ConvertToUnmanaged(e);
		}
	}

	unsafe static _003CGlassDesk_Platform_Interop_IShellLinkW_003EFC159A7D2188C344C8DE588EDB0C2921240B69E43B8FB7138D5F76BACD273C869__InterfaceImplementation()
	{
		ComWrappers.GetIUnknownImpl(out *(nint*)(&((_003CGlassDesk_Platform_Interop_IShellLinkW_003EFC159A7D2188C344C8DE588EDB0C2921240B69E43B8FB7138D5F76BACD273C869__InterfaceImplementationVtable*)Unsafe.AsPointer(in Vtable))->QueryInterface_0), out *(nint*)(&((_003CGlassDesk_Platform_Interop_IShellLinkW_003EFC159A7D2188C344C8DE588EDB0C2921240B69E43B8FB7138D5F76BACD273C869__InterfaceImplementationVtable*)Unsafe.AsPointer(in Vtable))->AddRef_1), out *(nint*)(&((_003CGlassDesk_Platform_Interop_IShellLinkW_003EFC159A7D2188C344C8DE588EDB0C2921240B69E43B8FB7138D5F76BACD273C869__InterfaceImplementationVtable*)Unsafe.AsPointer(in Vtable))->Release_2));
		Vtable.GetPath_3 = &ABI_GetPath;
		Vtable.GetIDList_4 = &ABI_GetIDList;
		Vtable.SetIDList_5 = &ABI_SetIDList;
		Vtable.GetDescription_6 = &ABI_GetDescription;
		Vtable.SetDescription_7 = &ABI_SetDescription;
		Vtable.GetWorkingDirectory_8 = &ABI_GetWorkingDirectory;
		Vtable.SetWorkingDirectory_9 = &ABI_SetWorkingDirectory;
		Vtable.GetArguments_10 = &ABI_GetArguments;
		Vtable.SetArguments_11 = &ABI_SetArguments;
		Vtable.GetHotkey_12 = &ABI_GetHotkey;
		Vtable.SetHotkey_13 = &ABI_SetHotkey;
		Vtable.GetShowCmd_14 = &ABI_GetShowCmd;
		Vtable.SetShowCmd_15 = &ABI_SetShowCmd;
		Vtable.GetIconLocation_16 = &ABI_GetIconLocation;
		Vtable.SetIconLocation_17 = &ABI_SetIconLocation;
		Vtable.SetRelativePath_18 = &ABI_SetRelativePath;
		Vtable.Resolve_19 = &ABI_Resolve;
		Vtable.SetPath_20 = &ABI_SetPath;
	}
}
