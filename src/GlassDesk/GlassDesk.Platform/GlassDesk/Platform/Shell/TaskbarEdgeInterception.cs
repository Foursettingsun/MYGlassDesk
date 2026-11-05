namespace GlassDesk.Platform.Shell;

public static class TaskbarEdgeInterception
{
	public static bool IsSupported => false;

	public static string Status => "不支持强制拦截系统任务栏边缘；请使用 Windows 任务栏设置。";
}
