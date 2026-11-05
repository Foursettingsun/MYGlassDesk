using System;

namespace GlassDesk.Platform.Shell;

public sealed record ShortcutInfo(string Target, string Arguments, string WorkingDirectory, string IconSource, string Description, bool HasShellItem, string? Aumid = null)
{
	public bool TargetIsUrl
	{
		get
		{
			if (Uri.TryCreate(Target, UriKind.Absolute, out Uri result))
			{
				if (!(result.Scheme == Uri.UriSchemeHttp))
				{
					return result.Scheme == Uri.UriSchemeHttps;
				}
				return true;
			}
			return false;
		}
	}

	public bool HasResolvedTarget => !string.IsNullOrWhiteSpace(Target);
}
