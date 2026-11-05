using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using GlassDesk.Platform.Displays;

namespace GlassDesk.UI.Controls;

internal sealed class GlassBackdropScene : IEquatable<GlassBackdropScene>
{
	internal readonly record struct SceneWindow(nint Handle, uint ProcessId, int Left, int Top, int Right, int Bottom, bool IsTransientOverlay = false);

	private struct NativeRect
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	private delegate bool EnumWindowsProc(nint window, nint parameter);

	private readonly SceneWindow[] _windows;

	private readonly long _recoveryGeneration = BackdropRecovery.Generation;

	internal GlassBackdropScene(params SceneWindow[] windows)
	{
		_windows = windows.ToArray();
	}

	internal static GlassBackdropScene? Capture()
	{
		return Capture(null);
	}

	internal static GlassBackdropScene? Capture(PixelRect? region)
	{
		return CaptureVerifiedSnapshot(() => ReadSnapshot(region));
	}

	internal static GlassBackdropScene? CaptureVerifiedSnapshot(Func<GlassBackdropScene?> readSnapshot)
	{
		GlassBackdropScene glassBackdropScene = readSnapshot();
		if (glassBackdropScene != null)
		{
			return glassBackdropScene;
		}
		GlassBackdropScene glassBackdropScene2 = readSnapshot();
		BackdropRecovery.Record(new CaptureTrace("UIScene", (glassBackdropScene2 == null) ? "UnknownAfterRetry" : "RetryResolved", Stopwatch.GetTimestamp(), 0L, 0L, 0L, 0L, 0, 0L));
		return glassBackdropScene2;
	}

	private static GlassBackdropScene? ReadSnapshot(PixelRect? region)
	{
		List<SceneWindow> windows = new List<SceneWindow>();
		bool complete = true;
		int[] family = CaptureProcessFamily.KnownProcesses;
		if (!(EnumWindows((nint handle, nint _) =>
		{
			if (!IsWindowVisible(handle) || IsIconic(handle) || (DwmGetWindowAttribute(handle, 14u, out var value, 4) >= 0 && value != 0))
			{
				return true;
			}
			uint windowThreadProcessId = GetWindowThreadProcessId(handle, out var process);
			if (windowThreadProcessId == 0)
			{
				Failed(handle, "ProcessUnavailable");
				return true;
			}
			if (process == Environment.ProcessId || family.Contains((int)process))
			{
				return true;
			}
			if (!GetWindowRect(handle, out var rect))
			{
				Failed(handle, "BoundsUnavailable");
				return true;
			}
			if (rect.Right <= rect.Left || rect.Bottom <= rect.Top)
			{
				return true;
			}
			if (region.HasValue)
			{
				PixelRect valueOrDefault = region.GetValueOrDefault();
				if (rect.Right <= valueOrDefault.X || rect.Bottom <= valueOrDefault.Y || rect.Left >= valueOrDefault.Right || rect.Top >= valueOrDefault.Bottom)
				{
					return true;
				}
			}
			if (GetWindowThreadProcessId(handle, out var process2) != windowThreadProcessId || process2 != process)
			{
				Failed(handle, "IdentityChanged");
				return true;
			}
			bool isTransientOverlay = (((IntPtr)GetWindowLongPtr(handle, -20)).ToInt64() & 0x800A0) == 524448;
			windows.Add(new SceneWindow(handle, process, rect.Left, rect.Top, rect.Right, rect.Bottom, isTransientOverlay));
			return true;
		}, 0) & complete))
		{
			return null;
		}
		return new GlassBackdropScene(windows.ToArray());
		void Failed(nint handle, string reason)
		{
			complete = false;
			BackdropRecovery.Record(new CaptureTrace("UIScene", reason, Stopwatch.GetTimestamp(), 0L, 0L, 0L, 0L, 0, ((IntPtr)handle).ToInt64()));
		}
	}

	internal bool CanRetainMaterialFrom(GlassBackdropScene? previous)
	{
		if (previous != null && _recoveryGeneration == previous._recoveryGeneration)
		{
			return previous._windows.All((SceneWindow window) => window.IsTransientOverlay || _windows.Contains(window));
		}
		return false;
	}

	internal bool CanHandoffMaterialFrom(GlassBackdropScene? previous)
	{
		if (previous != null)
		{
			return _recoveryGeneration == previous._recoveryGeneration;
		}
		return false;
	}

	public bool Equals(GlassBackdropScene? other)
	{
		if (other != null && _recoveryGeneration == other._recoveryGeneration)
		{
			return ((ReadOnlySpan<SceneWindow>)_windows.AsSpan()).SequenceEqual((ReadOnlySpan<SceneWindow>)other._windows);
		}
		return false;
	}

	public override bool Equals(object? obj)
	{
		if (obj is GlassBackdropScene other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		HashCode hashCode = default;
		hashCode.Add(_recoveryGeneration);
		SceneWindow[] windows = _windows;
		foreach (SceneWindow value in windows)
		{
			hashCode.Add(value);
		}
		return hashCode.ToHashCode();
	}

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool EnumWindows(EnumWindowsProc callback, nint parameter);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool IsWindowVisible(nint window);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool IsIconic(nint window);

	[DllImport("dwmapi.dll")]
	private static extern int DwmGetWindowAttribute(nint window, uint attribute, out int value, int size);

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(nint window, out uint process);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetWindowRect(nint window, out NativeRect rect);

	[DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
	private static extern nint GetWindowLongPtr(nint window, int index);
}
