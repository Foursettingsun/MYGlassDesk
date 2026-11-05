namespace GlassDesk.Platform.Interop;

internal struct PropVariant
{
	internal short Type;

	private short _reserved1;

	private short _reserved2;

	private short _reserved3;

	internal nint Pointer;

	private nint _unionTail;
}
