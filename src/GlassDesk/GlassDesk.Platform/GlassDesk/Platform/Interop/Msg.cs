namespace GlassDesk.Platform.Interop;

internal struct Msg
{
	internal nint Window;

	internal uint Message;

	internal nuint WParam;

	internal nint LParam;

	internal uint Time;

	internal int PointX;

	internal int PointY;
}
