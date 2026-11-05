using System;

namespace GlassDesk.Platform.Shell;

public sealed record ShellPathResult(UserNotificationState State, bool IsFullscreen, string? Error, TimeSpan Duration)
{
	public override string ToString()
	{
		if (Error == null)
		{
			return $"{State}（{(IsFullscreen ? "全屏" : "非全屏")}）";
		}
		return $"{State}——{Error}";
	}
}
