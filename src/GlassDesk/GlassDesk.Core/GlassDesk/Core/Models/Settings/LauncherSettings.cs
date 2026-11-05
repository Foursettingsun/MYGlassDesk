using System;

namespace GlassDesk.Core.Models.Settings;

public sealed class LauncherSettings
{
	public bool GroupModeEnabled { get; set; } = true;

	public int Rows { get; set; } = 5;

	public int Columns { get; set; } = 7;

	public double WindowWorkAreaRatio { get; set; } = 0.66;

	public bool FullscreenMode { get; set; }

	public bool FocusSearchOnOpen { get; set; }

	public bool RememberLastPage { get; set; } = true;

	public bool HighRefreshAnimationsEnabled { get; set; } = true;

	public double OffsetY
	{
		get
		{
			return field;
		}
		set
		{
			field = ((!double.IsFinite(value)) ? 0.0 : Math.Clamp(value, -10000.0, 10000.0));
		}
	}
}
