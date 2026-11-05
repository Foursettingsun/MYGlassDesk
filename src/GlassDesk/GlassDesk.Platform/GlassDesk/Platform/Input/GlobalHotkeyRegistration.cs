using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace GlassDesk.Platform.Input;

public sealed class GlobalHotkeyRegistration(nint hwnd) : IDisposable
{
	private readonly HashSet<int> ids = new HashSet<int>();

	private bool disposed;

	public bool TryRegister(int id, uint modifiers, uint virtualKey, out int error)
	{
		error = 0;
		if (disposed || id < 0 || id > 49151 || ids.Contains(id))
		{
			error = 87;
			return false;
		}
		if (!RegisterHotKey(hwnd, id, modifiers, virtualKey))
		{
			error = Marshal.GetLastWin32Error();
			return false;
		}
		ids.Add(id);
		return true;
	}

	public bool Unregister(int id)
	{
		if (!ids.Contains(id))
		{
			return false;
		}
		if (!UnregisterHotKey(hwnd, id))
		{
			return false;
		}
		ids.Remove(id);
		return true;
	}

	public void Dispose()
	{
		if (!disposed)
		{
			int[] array = ids.ToArray();
			foreach (int id in array)
			{
				Unregister(id);
			}
			disposed = true;
		}
	}

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool RegisterHotKey(nint hwnd, int id, uint modifiers, uint key);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool UnregisterHotKey(nint hwnd, int id);
}
