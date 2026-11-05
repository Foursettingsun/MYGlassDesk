using System;
using System.Runtime.InteropServices;
using GlassDesk.Platform.Interop;

internal struct _003CGlassDesk_Platform_Interop_IPropertyStore_003EF57EBA8FEBF09B3B59B40A38D613645008C336C3D47690A5CF845E03C4F790F00__InterfaceImplementationVtable
{
	public unsafe delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int> QueryInterface_0;

	public unsafe delegate* unmanaged[MemberFunction]<void*, uint> AddRef_1;

	public unsafe delegate* unmanaged[MemberFunction]<void*, uint> Release_2;

	public unsafe delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, uint*, int> GetCount_3;

	public unsafe delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, uint, PropertyKey*, int> GetAt_4;

	public unsafe delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, PropertyKey*, PropVariant*, int> GetValue_5;

	public unsafe delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, PropertyKey*, PropVariant*, int> SetValue_6;

	public unsafe delegate* unmanaged[MemberFunction]<ComWrappers.ComInterfaceDispatch*, int> Commit_7;
}
