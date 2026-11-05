using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using GlassDesk.Platform.Displays;
using GlassDesk.Platform.Interop;

namespace GlassDesk.Platform.Shell;

public static class FullscreenDetector
{
	private readonly record struct WindowGeometry(PixelRect WindowRect, PixelRect? VisibleFrame, PixelRect MonitorBounds, string MonitorDeviceName, bool IsMaximized);

	private const int CoverageTolerance = 1;

	private static readonly string[] ShellWindowClasses = new string[4] { "Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd" };

	public static FullscreenSnapshot Probe(FullscreenOptions? options = null)
	{
		FullscreenOptions options2 = options ?? FullscreenOptions.Default;
		nint foregroundWindow = User32.GetForegroundWindow();
		Stopwatch stopwatch = Stopwatch.StartNew();
		var (verdict, reason, windowGeometry) = EvaluateWindowPath(foregroundWindow, options2);
		stopwatch.Stop();
		Stopwatch stopwatch2 = Stopwatch.StartNew();
		var (state, error) = QueryShellStateCore();
		stopwatch2.Stop();
		return new FullscreenSnapshot(new ForegroundWindowInfo(foregroundWindow, User32.ReadClassName(foregroundWindow), User32.ReadWindowText(foregroundWindow), GetProcessId(foregroundWindow), ReadProcessName(foregroundWindow), (User32.GetWindowLong(foregroundWindow, -16) & 0x1000000) != 0), new WindowPathResult(verdict, reason, windowGeometry.WindowRect, windowGeometry.VisibleFrame, windowGeometry.MonitorBounds, windowGeometry.MonitorDeviceName, stopwatch.Elapsed), new ShellPathResult(state, IsFullscreenState(state), error, stopwatch2.Elapsed));
	}

	public static bool IsFullscreenActive(FullscreenOptions? options = null)
	{
		FullscreenOptions fullscreenOptions = options ?? FullscreenOptions.Default;
		if (GetForegroundVerdict(fullscreenOptions) == FullscreenVerdict.Fullscreen)
		{
			return true;
		}
		if (fullscreenOptions.IncludeShellState)
		{
			return IsFullscreenState(QueryShellStateCore().State);
		}
		return false;
	}

	public static PerformanceFullscreenObservation ReadPerformanceObservation()
	{
		nint foregroundWindow = User32.GetForegroundWindow();
		if (foregroundWindow == 0)
		{
			return new PerformanceFullscreenObservation(null, BelongsToCurrentProcess: false);
		}
		if (GetProcessId(foregroundWindow) == (uint)Environment.ProcessId)
		{
			return new PerformanceFullscreenObservation(null, BelongsToCurrentProcess: true);
		}
		if (IsExcluded(foregroundWindow, FullscreenOptions.Default, out string reason))
		{
			return new PerformanceFullscreenObservation(false, BelongsToCurrentProcess: false);
		}
		if (!TryGetGeometry(foregroundWindow, out WindowGeometry geometry, out reason))
		{
			return new PerformanceFullscreenObservation(null, BelongsToCurrentProcess: false);
		}
		return new PerformanceFullscreenObservation(Covers(geometry.VisibleFrame ?? geometry.WindowRect, geometry.MonitorBounds) || QueryShellStateCore().State == UserNotificationState.RunningD3dFullScreen, BelongsToCurrentProcess: false);
	}

	public static FullscreenVerdict GetForegroundVerdict(FullscreenOptions? options = null)
	{
		FullscreenOptions options2 = options ?? FullscreenOptions.Default;
		nint foregroundWindow = User32.GetForegroundWindow();
		if (foregroundWindow == 0)
		{
			return FullscreenVerdict.Unknown;
		}
		if (IsExcluded(foregroundWindow, options2, out string reason))
		{
			return FullscreenVerdict.Excluded;
		}
		if (!TryGetGeometry(foregroundWindow, out WindowGeometry geometry, out reason))
		{
			return FullscreenVerdict.Unknown;
		}
		if (!Covers(geometry.VisibleFrame ?? geometry.WindowRect, geometry.MonitorBounds))
		{
			return FullscreenVerdict.NotFullscreen;
		}
		return FullscreenVerdict.Fullscreen;
	}

	public static UserNotificationState QueryShellState()
	{
		return QueryShellStateCore().State;
	}

	public static bool IsFullscreenState(UserNotificationState state)
	{
		if ((uint)(state - 2) <= 1u || state == UserNotificationState.App)
		{
			return true;
		}
		return false;
	}

	public static bool IsTaskbarAutoHidden()
	{
		try
		{
			Shell32.AppBarData data = new Shell32.AppBarData
			{
				Size = (uint)Marshal.SizeOf<Shell32.AppBarData>()
			};
			return (Shell32.SHAppBarMessage(4u, ref data) & 1) != 0;
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return false;
		}
	}

	private static (FullscreenVerdict Verdict, string Reason, WindowGeometry Geometry) EvaluateWindowPath(nint handle, FullscreenOptions options)
	{
		if (handle == 0)
		{
			return (Verdict: FullscreenVerdict.Unknown, Reason: "当前没有前台窗口（会话正在切换，或已锁屏）。", Geometry: default);
		}
		if (IsExcluded(handle, options, out string reason))
		{
			return (Verdict: FullscreenVerdict.Excluded, Reason: reason, Geometry: default);
		}
		if (!TryGetGeometry(handle, out WindowGeometry geometry, out string failure))
		{
			return (Verdict: FullscreenVerdict.Unknown, Reason: failure, Geometry: default);
		}
		PixelRect pixelRect = geometry.VisibleFrame ?? geometry.WindowRect;
		if (Covers(pixelRect, geometry.MonitorBounds))
		{
			string value = (geometry.IsMaximized ? "（该窗口带最大化状态；任务栏自动隐藏时，最大化窗口与全屏在几何上无法区分）" : string.Empty);
			return (Verdict: FullscreenVerdict.Fullscreen, Reason: $"可见边框 {pixelRect} 覆盖了 {geometry.MonitorDeviceName} 的全部 {geometry.MonitorBounds}{value}", Geometry: geometry);
		}
		if (Covers(geometry.WindowRect, geometry.MonitorBounds))
		{
			return (Verdict: FullscreenVerdict.NotFullscreen, Reason: $"GetWindowRect {geometry.WindowRect} 在数值上覆盖了整屏，但可见边框 {pixelRect} 停在 {geometry.MonitorDeviceName} 的工作区之内——这是最大化窗口的不可见调整边框，不是全屏", Geometry: geometry);
		}
		return (Verdict: FullscreenVerdict.NotFullscreen, Reason: $"可见边框 {pixelRect} 未覆盖 {geometry.MonitorDeviceName} 的全部 {geometry.MonitorBounds}", Geometry: geometry);
	}

	private static bool IsExcluded(nint handle, FullscreenOptions options, out string reason)
	{
		if (handle == User32.GetShellWindow() || handle == User32.GetDesktopWindow())
		{
			reason = "前台窗口是桌面（Progman/WorkerW），桌面铺满屏幕是常态，不算全屏应用。";
			return true;
		}
		if (IsShellWindowClass(handle))
		{
			reason = "前台窗口是外壳的任务栏（类名 " + User32.ReadClassName(handle) + "），不算全屏应用。";
			return true;
		}
		if (options.ExcludeCurrentProcess)
		{
			uint processId = GetProcessId(handle);
			if (processId != 0 && processId == (uint)Environment.ProcessId)
			{
				reason = "前台窗口属于本产品自己，不算全屏应用（否则 Dock 会把自己的窗口当成全屏而不再唤出）。";
				return true;
			}
		}
		if (User32.IsIconic(handle))
		{
			reason = "前台窗口已最小化，不可能铺满屏幕。";
			return true;
		}
		if (!User32.IsWindowVisible(handle))
		{
			reason = "前台窗口不可见。";
			return true;
		}
		if (DwmApi.IsCloaked(handle))
		{
			reason = "前台窗口已被 DWM 隐藏（cloaked），通常是挂起的 UWP 应用留下的空壳。";
			return true;
		}
		reason = string.Empty;
		return false;
	}

	private static bool IsShellWindowClass(nint handle)
	{
		Span<char> destination = stackalloc char[64];
		int className = User32.GetClassName(handle, destination);
		if (className <= 0)
		{
			return false;
		}
		Span<char> span = destination.Slice(0, className);
		string[] shellWindowClasses = ShellWindowClasses;
		foreach (string text in shellWindowClasses)
		{
			if (MemoryExtensions.Equals(span, text, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static bool TryGetGeometry(nint handle, out WindowGeometry geometry, out string failure)
	{
		geometry = default;
		failure = string.Empty;
		if (!Display32.GetWindowRect(handle, out var rect))
		{
			failure = "GetWindowRect 失败，取不到前台窗口的矩形。";
			return false;
		}
		nint num = Display32.MonitorFromWindow(handle, 2);
		if (num == 0)
		{
			failure = "MonitorFromWindow 失败，无法确定前台窗口在哪块显示器上。";
			return false;
		}
		MonitorInfoEx info = new MonitorInfoEx
		{
			Size = Marshal.SizeOf<MonitorInfoEx>(),
			DeviceName = string.Empty
		};
		if (!Display32.GetMonitorInfo(num, ref info))
		{
			failure = "GetMonitorInfo 失败，取不到显示器边界。";
			return false;
		}
		PixelRect? visibleFrame = (DwmApi.TryGetExtendedFrameBounds(handle, out var bounds) ? new PixelRect?(ToPixelRect(bounds)) : ((PixelRect?)null));
		geometry = new WindowGeometry(ToPixelRect(rect), visibleFrame, ToPixelRect(info.Monitor), info.DeviceName, (User32.GetWindowLong(handle, -16) & 0x1000000) != 0);
		return true;
	}

	private static bool Covers(PixelRect window, PixelRect monitor)
	{
		if (window.X <= monitor.X + 1 && window.Y <= monitor.Y + 1 && window.Right >= monitor.Right - 1)
		{
			return window.Bottom >= monitor.Bottom - 1;
		}
		return false;
	}

	private static (UserNotificationState State, string? Error) QueryShellStateCore()
	{
		try
		{
			int num = Shell32.SHQueryUserNotificationState(out var state);
			if (num < 0)
			{
				return (State: UserNotificationState.Unknown, Error: $"SHQueryUserNotificationState 返回失败 HRESULT 0x{num:X8}");
			}
			UserNotificationState userNotificationState = (UserNotificationState)state;
			return IsKnownState(userNotificationState) ? (State: userNotificationState, Error: null) : (State: UserNotificationState.Unknown, Error: $"SHQueryUserNotificationState 返回了未知状态 {state}");
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return (State: UserNotificationState.Unknown, Error: "本机的 shell32.dll 没有 SHQueryUserNotificationState。");
		}
	}

	private static bool IsKnownState(UserNotificationState state)
	{
		if ((uint)(state - 1) <= 6u)
		{
			return true;
		}
		return false;
	}

	private static uint GetProcessId(nint handle)
	{
		if (User32.GetWindowThreadProcessId(handle, out var processId) != 0)
		{
			return processId;
		}
		return 0u;
	}

	private static string ReadProcessName(nint handle)
	{
		uint processId = GetProcessId(handle);
		if (processId == 0)
		{
			return "?";
		}
		try
		{
			using Process process = Process.GetProcessById((int)processId);
			return process.ProcessName;
		}
		catch (Exception ex) when ((ex is ArgumentException || ex is InvalidOperationException || ex is Win32Exception) ? true : false)
		{
			return "?";
		}
	}

	private static PixelRect ToPixelRect(Rect rect)
	{
		return new PixelRect(rect.Left, rect.Top, rect.Width, rect.Height);
	}
}
