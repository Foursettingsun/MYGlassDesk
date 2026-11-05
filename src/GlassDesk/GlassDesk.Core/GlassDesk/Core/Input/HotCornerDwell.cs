using GlassDesk.Core.Models.Settings;

namespace GlassDesk.Core.Input;

public sealed class HotCornerDwell
{
	private string? _token;

	private long _entered;

	private bool _fired;

	public bool Update(string? token, long nowMs, int dwellMs, bool suppressed)
	{
		if (suppressed || token == null)
		{
			_token = null;
			_fired = false;
			return false;
		}
		if (_token != token || nowMs < _entered)
		{
			_token = token;
			_entered = nowMs;
			_fired = false;
		}
		if (_fired || nowMs - _entered < dwellMs)
		{
			return false;
		}
		_fired = true;
		return true;
	}

	public static bool IsInCorner(int x, int y, int left, int top, int width, int height, HotCorner corner, int thickness)
	{
		if (width <= 0 || height <= 0 || thickness <= 0 || x < left || y < top || x >= left + width || y >= top + height)
		{
			return false;
		}
		bool flag = x < left + thickness;
		bool flag2 = x >= left + width - thickness;
		bool flag3 = y < top + thickness;
		bool flag4 = y >= top + height - thickness;
		return corner switch
		{
			HotCorner.TopLeft => flag & flag3, 
			HotCorner.TopRight => flag2 & flag3, 
			HotCorner.BottomLeft => flag & flag4, 
			HotCorner.BottomRight => flag2 & flag4, 
			_ => false, 
		};
	}
}
