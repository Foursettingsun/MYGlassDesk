using System;
using System.Threading;

namespace GlassDesk.Platform.Displays;

public sealed class PrimaryDisplaySession
{
	private DisplayInfo _display;

	private int _requiresRestart;

	public string StartupDeviceName { get; }

	public DisplayInfo Display => Volatile.Read(in _display);

	public bool IsAvailable => Volatile.Read(in _requiresRestart) == 0;

	public bool RequiresRestart => !IsAvailable;

	public PrimaryDisplaySession(DisplayInfo startupPrimary)
	{
		ArgumentNullException.ThrowIfNull(startupPrimary, "startupPrimary");
		if (!startupPrimary.IsPrimary)
		{
			throw new ArgumentException("The display session must be captured from the current Primary Monitor.", "startupPrimary");
		}
		_display = startupPrimary;
		StartupDeviceName = startupPrimary.DeviceName;
	}

	public bool TryRefreshPrimary(DisplayInfo? currentPrimary)
	{
		if (RequiresRestart)
		{
			return false;
		}
		if ((object)currentPrimary == null || !currentPrimary.IsPrimary || !string.Equals(currentPrimary.DeviceName, Display.DeviceName, StringComparison.OrdinalIgnoreCase))
		{
			Interlocked.Exchange(ref _requiresRestart, 1);
			return false;
		}
		Volatile.Write(ref _display, currentPrimary);
		return true;
	}

	public bool TryRebindPrimary(DisplayInfo? currentPrimary)
	{
		if ((object)currentPrimary == null || !currentPrimary.IsPrimary)
		{
			Interlocked.Exchange(ref _requiresRestart, 1);
			return false;
		}
		Volatile.Write(ref _display, currentPrimary);
		Interlocked.Exchange(ref _requiresRestart, 0);
		return true;
	}

	public bool ContainsPrimaryPoint(int x, int y)
	{
		return Display.Bounds.Contains(x, y);
	}

	public PixelRect ClipToPrimaryBounds(PixelRect region)
	{
		return Clip(region, Display.Bounds);
	}

	public PixelRect ClampWindowToWorkArea(PixelRect requested)
	{
		PixelRect workArea = Display.WorkArea;
		int num = Math.Max(1, Math.Min(requested.Width, workArea.Width));
		int num2 = Math.Max(1, Math.Min(requested.Height, workArea.Height));
		int x = Math.Clamp(requested.X, workArea.X, workArea.Right - num);
		int y = Math.Clamp(requested.Y, workArea.Y, workArea.Bottom - num2);
		return new PixelRect(x, y, num, num2);
	}

	public bool ContainsInWorkArea(PixelRect rect)
	{
		PixelRect workArea = Display.WorkArea;
		if (rect.Width > 0 && rect.Height > 0 && rect.X >= workArea.X && rect.Y >= workArea.Y && rect.Right <= workArea.Right)
		{
			return rect.Bottom <= workArea.Bottom;
		}
		return false;
	}

	private static PixelRect Clip(PixelRect rect, PixelRect bounds)
	{
		int num = Math.Max(rect.X, bounds.X);
		int num2 = Math.Max(rect.Y, bounds.Y);
		int num3 = Math.Min(rect.Right, bounds.Right);
		int num4 = Math.Min(rect.Bottom, bounds.Bottom);
		if (num3 <= num || num4 <= num2)
		{
			return new PixelRect(Math.Clamp(num, bounds.X, bounds.Right), Math.Clamp(num2, bounds.Y, bounds.Bottom), 0, 0);
		}
		return new PixelRect(num, num2, num3 - num, num4 - num2);
	}
}
