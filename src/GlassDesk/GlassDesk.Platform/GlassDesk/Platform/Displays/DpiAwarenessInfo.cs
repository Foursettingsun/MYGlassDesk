namespace GlassDesk.Platform.Displays;

public readonly record struct DpiAwarenessInfo(DpiAwarenessLevel Level, bool IsPerMonitorV2, string RawContext, uint SystemDpi)
{
	public string Describe()
	{
		switch (Level)
		{
		case DpiAwarenessLevel.PerMonitorAware:
			if (IsPerMonitorV2)
			{
				return "每屏感知 V2（PerMonitorV2）";
			}
			return "每屏感知 V1（PerMonitor）";
		case DpiAwarenessLevel.SystemAware:
			return "系统感知（System Aware）——跨屏不会重新缩放";
		case DpiAwarenessLevel.Unaware:
			return "DPI 不感知——坐标会被系统拉伸";
		default:
			return "未知";
		}
	}
}
