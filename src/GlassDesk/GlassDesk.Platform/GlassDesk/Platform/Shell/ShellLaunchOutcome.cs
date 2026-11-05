namespace GlassDesk.Platform.Shell;

public sealed record ShellLaunchOutcome(bool Started, int ErrorCode, uint ProcessId)
{
	public bool IsFileNotFound => ErrorCode == 2;

	public bool IsNoAssociation => ErrorCode == 31;

	public bool IsAccessDenied
	{
		get
		{
			int errorCode = ErrorCode;
			if (errorCode == 5 || errorCode == 1223)
			{
				return true;
			}
			return false;
		}
	}
}
