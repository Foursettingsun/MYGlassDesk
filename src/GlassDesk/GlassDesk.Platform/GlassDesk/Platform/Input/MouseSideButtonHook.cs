using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace GlassDesk.Platform.Input;

public sealed class MouseSideButtonHook : IDisposable
{
	private struct MouseInfo
	{
		public int X;

		public int Y;

		public uint MouseData;

		public uint Flags;

		public uint Time;

		public nuint ExtraInfo;
	}

	private delegate nint HookProc(int code, nint message, nint data);

	private readonly Func<int, bool> onButton;

	private readonly Func<int, bool>? onWheel;

	private readonly HookProc callback;

	private readonly bool[] consumed = new bool[3];

	private nint handle;

	private bool disposed;

	public MouseSideButtonHook(Func<int, bool> onButton, Func<int, bool>? onWheel = null)
	{
		this.onButton = onButton ?? throw new ArgumentNullException("onButton");
		this.onWheel = onWheel;
		callback = Hook;
	}

	public bool TryStart(out string? error)
	{
		error = null;
		if (disposed)
		{
			error = "侧键监听已释放";
			return false;
		}
		if (handle != 0)
		{
			return true;
		}
		handle = SetWindowsHookEx(14, callback, GetModuleHandle(null), 0u);
		if (handle != 0)
		{
			return true;
		}
		error = new Win32Exception(Marshal.GetLastWin32Error()).Message;
		return false;
	}

	private bool HandleButton(int button, bool down, bool injected)
	{
		bool flag = disposed | injected;
		if (!flag)
		{
			bool flag2 = ((button < 1 || button > 2) ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			return false;
		}
		if (!down)
		{
			bool result = consumed[button];
			consumed[button] = false;
			return result;
		}
		if (consumed[button])
		{
			return true;
		}
		try
		{
			consumed[button] = (flag = onButton(button));
			return flag;
		}
		catch
		{
			return false;
		}
	}

	private nint Hook(int code, nint message, nint data)
	{
		try
		{
			if (code >= 0 && ((int)message == 523 || (int)message == 524 || (int)message == 522))
			{
				MouseInfo mouseInfo = Marshal.PtrToStructure<MouseInfo>(data);
				if ((int)message == 522)
				{
					if (!disposed && (mouseInfo.Flags & 3) == 0)
					{
						Func<int, bool>? func = onWheel;
						if (func != null && func((short)(mouseInfo.MouseData >> 16)))
						{
							return 1;
						}
					}
				}
				else if (HandleButton((int)(mouseInfo.MouseData >> 16), (int)message == 523, (mouseInfo.Flags & 3) != 0))
				{
					return 1;
				}
			}
		}
		catch
		{
		}
		return CallNextHookEx(handle, code, message, data);
	}

	public void Dispose()
	{
		if (!disposed)
		{
			if (handle != 0)
			{
				UnhookWindowsHookEx(handle);
			}
			handle = 0;
			disposed = true;
			GC.KeepAlive(callback);
		}
	}

	[DllImport("user32.dll", SetLastError = true)]
	private static extern nint SetWindowsHookEx(int kind, HookProc callback, nint module, uint thread);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool UnhookWindowsHookEx(nint hook);

	[DllImport("user32.dll")]
	private static extern nint CallNextHookEx(nint hook, int code, nint message, nint data);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	private static extern nint GetModuleHandle(string? name);
}
