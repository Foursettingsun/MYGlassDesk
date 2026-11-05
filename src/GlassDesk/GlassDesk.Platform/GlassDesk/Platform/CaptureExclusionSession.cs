using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using GlassDesk.Platform.Displays;

namespace GlassDesk.Platform;

public sealed class CaptureExclusionSession : IDisposable
{
	private delegate bool EnumWindowProc(nint handle, nint data);

	private struct NativeRect
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	private static readonly Dictionary<nint, bool> Windows = new Dictionary<nint, bool>();

	private static readonly object Gate = new object();

	private readonly nint _handle;

	private readonly uint _previous;

	private bool _disposed;

	public static bool CanCapture
	{
		get
		{
			lock (Gate)
			{
				nint[] array = Windows.Keys.Where((nint handle) => !IsWindow(handle)).ToArray();
				foreach (nint key in array)
				{
					Windows.Remove(key);
				}
				return Windows.Count > 0 && Windows.All((KeyValuePair<nint, bool> pair) => pair.Value && GetWindowDisplayAffinity(pair.Key, out var affinity) && affinity == 17);
			}
		}
	}

	private CaptureExclusionSession(nint handle, uint previous)
	{
		_handle = handle;
		_previous = previous;
	}

	public static CaptureExclusionSession? TryCreate(nint handle)
	{
		lock (Gate)
		{
			if (handle == 0 || GetWindowThreadProcessId(handle, out var process) == 0 || process != Environment.ProcessId)
			{
				return null;
			}
			Windows[handle] = false;
			if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041) || !GetWindowDisplayAffinity(handle, out var affinity))
			{
				return null;
			}
			if (!SetWindowDisplayAffinity(handle, 17u))
			{
				return null;
			}
			if (!GetWindowDisplayAffinity(handle, out var affinity2) || affinity2 != 17)
			{
				SetWindowDisplayAffinity(handle, affinity);
				return null;
			}
			Windows[handle] = true;
			return new CaptureExclusionSession(handle, affinity);
		}
	}

	internal static bool CanCaptureRegion(PixelRect region)
	{
		if (!CanCapture)
		{
			return false;
		}
		bool safe = true;
		return EnumWindows((nint handle, nint _) =>
		{
			if (GetWindowThreadProcessId(handle, out var process) == 0)
			{
				safe = false;
				return false;
			}
			if (process != Environment.ProcessId || !IsWindowVisible(handle))
			{
				return true;
			}
			lock (Gate)
			{
				if (Windows.TryGetValue(handle, out var value) & value)
				{
					return true;
				}
			}
			if (GetWindowRect(handle, out var rect) && rect.Left < region.Right && rect.Right > region.X && rect.Top < region.Bottom && rect.Bottom > region.Y)
			{
				safe = false;
				return false;
			}
			return true;
		}, 0) & safe;
	}

	internal static bool CanCaptureBeforeReveal(PixelRect region, nint emptyOwner = 0)
	{
		bool safe = true;
		return EnumWindows((nint handle, nint _) =>
		{
			if (GetWindowThreadProcessId(handle, out var process) == 0)
			{
				safe = false;
				return false;
			}
			if (process != Environment.ProcessId || !IsWindowVisible(handle) || handle == emptyOwner)
			{
				return true;
			}
			if (!GetWindowRect(handle, out var rect))
			{
				safe = false;
				return false;
			}
			if (rect.Left < region.Right && rect.Right > region.X && rect.Top < region.Bottom && rect.Bottom > region.Y)
			{
				safe = false;
				return false;
			}
			return true;
		}, 0) & safe;
	}

	public static void Forget(nint handle)
	{
		lock (Gate)
		{
			Windows.Remove(handle);
		}
	}

	public void Dispose()
	{
		lock (Gate)
		{
			if (!_disposed)
			{
				_disposed = true;
				if (IsWindow(_handle))
				{
					SetWindowDisplayAffinity(_handle, _previous);
				}
				Windows.Remove(_handle);
			}
		}
	}

	[DllImport("user32.dll")]
	private static extern bool SetWindowDisplayAffinity(nint handle, uint affinity);

	[DllImport("user32.dll")]
	private static extern bool GetWindowDisplayAffinity(nint handle, out uint affinity);

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(nint handle, out uint process);

	[DllImport("user32.dll")]
	private static extern bool IsWindow(nint handle);

	[DllImport("user32.dll")]
	private static extern bool IsWindowVisible(nint handle);

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(nint handle, out NativeRect rect);

	[DllImport("user32.dll")]
	private static extern bool EnumWindows(EnumWindowProc callback, nint data);
}
