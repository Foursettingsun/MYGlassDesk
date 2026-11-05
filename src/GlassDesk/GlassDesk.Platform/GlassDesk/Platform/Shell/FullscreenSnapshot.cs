namespace GlassDesk.Platform.Shell;

public sealed record FullscreenSnapshot(ForegroundWindowInfo Foreground, WindowPathResult WindowPath, ShellPathResult ShellPath)
{
	public bool ShouldSuppressTriggers
	{
		get
		{
			if (WindowPath.Verdict != FullscreenVerdict.Fullscreen)
			{
				return ShellPath.IsFullscreen;
			}
			return true;
		}
	}

	public bool PathsAgree
	{
		get
		{
			if (WindowPath.IsFullscreen.HasValue)
			{
				return WindowPath.IsFullscreen == ShellPath.IsFullscreen;
			}
			return true;
		}
	}

	public string Describe()
	{
		return $"路径一 {DescribeVerdict(WindowPath.Verdict)}，路径二 {ShellPath.State}，" + (PathsAgree ? "两路一致" : "两路不一致");
	}

	private static string DescribeVerdict(FullscreenVerdict verdict)
	{
		return verdict switch
		{
			FullscreenVerdict.Fullscreen => "全屏", 
			FullscreenVerdict.NotFullscreen => "非全屏", 
			FullscreenVerdict.Excluded => "已排除（无结论）", 
			_ => "无法判定", 
		};
	}
}
