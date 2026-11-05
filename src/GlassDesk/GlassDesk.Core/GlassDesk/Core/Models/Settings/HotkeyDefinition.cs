using System.Collections.Generic;

namespace GlassDesk.Core.Models.Settings;

public sealed class HotkeyDefinition
{
	public bool Enabled { get; set; }

	public bool Control { get; set; }

	public bool Alt { get; set; }

	public bool Shift { get; set; }

	public bool Win { get; set; }

	public string Key { get; set; } = string.Empty;

	public bool IsBound
	{
		get
		{
			if (Enabled)
			{
				return !string.IsNullOrWhiteSpace(Key);
			}
			return false;
		}
	}

	public string Describe()
	{
		if (!IsBound)
		{
			return "未设置";
		}
		List<string> list = new List<string>(5);
		if (Control)
		{
			list.Add("Ctrl");
		}
		if (Alt)
		{
			list.Add("Alt");
		}
		if (Shift)
		{
			list.Add("Shift");
		}
		if (Win)
		{
			list.Add("Win");
		}
		list.Add(Key);
		return string.Join('+', list);
	}
}
