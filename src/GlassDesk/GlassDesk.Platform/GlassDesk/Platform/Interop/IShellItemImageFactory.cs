using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace GlassDesk.Platform.Interop;

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
[IUnknownDerived<_003CGlassDesk_Platform_Interop_IShellItemImageFactory_003EF9E2209CFDD053EE2CAC00BE767B164BF0FFB25647047E33864DA6E1C345105CF__InterfaceInformation, _003CGlassDesk_Platform_Interop_IShellItemImageFactory_003EF9E2209CFDD053EE2CAC00BE767B164BF0FFB25647047E33864DA6E1C345105CF__InterfaceImplementation>]
internal interface IShellItemImageFactory
{
	[PreserveSig]
	int GetImage(Size size, int flags, out nint bitmap);
}
