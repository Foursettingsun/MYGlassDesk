using System.Runtime.InteropServices;

namespace GlassDesk.Platform.Interop;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct MonitorInfoEx
{
	internal int Size;

	internal Rect Monitor;

	internal Rect Work;

	internal int Flags;

	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
	internal string DeviceName;
}
