using System;
using System.Collections.Generic;

namespace GlassDesk.Core.Models;

public sealed class AppEntry
{
	public string Id { get; set; } = string.Empty;

	public string DisplayName { get; set; } = string.Empty;

	public AppItemKind Kind { get; set; }

	public string Target { get; set; } = string.Empty;

	public string? Arguments { get; set; }

	public string? WorkingDirectory { get; set; }

	public string? AppUserModelId { get; set; }

	public string? IconSource { get; set; }

	public string? CustomIconPath { get; set; }

	public List<string> SearchAliases { get; set; } = new List<string>();

	public string? PinyinIndex { get; set; }

	public string? PinyinInitials { get; set; }

	public bool IsTargetMissing { get; set; }

	public DateTimeOffset AddedAtUtc { get; set; } = DateTimeOffset.UtcNow;

	public bool HasLaunchTarget => !string.IsNullOrWhiteSpace(Target);

	public bool IsUsable
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(Id))
			{
				return HasLaunchTarget;
			}
			return false;
		}
	}
}
