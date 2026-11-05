using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace GlassDesk.Platform.Interop;

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("0000010B-0000-0000-C000-000000000046")]
[IUnknownDerived<_003CGlassDesk_Platform_Interop_IPersistFile_003EFF5F3ADAF60929DE2D80683F9ED0803F8CAE0B81AB42FA7FDA9BB5891D1C513E4__InterfaceInformation, _003CGlassDesk_Platform_Interop_IPersistFile_003EFF5F3ADAF60929DE2D80683F9ED0803F8CAE0B81AB42FA7FDA9BB5891D1C513E4__InterfaceImplementation>]
internal interface IPersistFile
{
	void GetClassID(out Guid classId);

	[PreserveSig]
	int IsDirty();

	void Load(string fileName, uint mode);

	void Save(string? fileName, [MarshalAs(UnmanagedType.Bool)] bool remember);

	void SaveCompleted(string fileName);

	void GetCurFile(out nint fileName);
}
