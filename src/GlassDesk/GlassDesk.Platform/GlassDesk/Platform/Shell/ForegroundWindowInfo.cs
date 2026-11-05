namespace GlassDesk.Platform.Shell;

public sealed record ForegroundWindowInfo(nint Handle, string ClassName, string Title, uint ProcessId, string ProcessName, bool IsMaximized)
{
	public override string ToString()
	{
		return $"句柄 0x{Handle:X}  类名 {ClassName}  进程 {ProcessName}（{ProcessId}）{(IsMaximized ? "  已最大化" : string.Empty)}{(string.IsNullOrEmpty(Title) ? string.Empty : ("  标题「" + Title + "」"))}";
	}
}
