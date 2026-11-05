using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace GlassDesk.Platform.Interop;

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
[IUnknownDerived<_003CGlassDesk_Platform_Interop_IPropertyStore_003EF57EBA8FEBF09B3B59B40A38D613645008C336C3D47690A5CF845E03C4F790F00__InterfaceInformation, _003CGlassDesk_Platform_Interop_IPropertyStore_003EF57EBA8FEBF09B3B59B40A38D613645008C336C3D47690A5CF845E03C4F790F00__InterfaceImplementation>]
internal interface IPropertyStore
{
	[PreserveSig]
	int GetCount(out uint propertyCount);

	[PreserveSig]
	int GetAt(uint propertyIndex, out PropertyKey key);

	[PreserveSig]
	int GetValue(in PropertyKey key, out PropVariant value);

	[PreserveSig]
	int SetValue(in PropertyKey key, in PropVariant value);

	[PreserveSig]
	int Commit();
}
