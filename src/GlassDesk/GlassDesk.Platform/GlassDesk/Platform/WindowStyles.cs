using System;
using System.Runtime.InteropServices;
using GlassDesk.Platform.Interop;

namespace GlassDesk.Platform;

public static class WindowStyles
{
	private struct WindowPosition
	{
		public nint Window;

		public nint InsertAfter;

		public int X;

		public int Y;

		public int Width;

		public int Height;

		public uint Flags;
	}

	private static readonly nint HwndTopmost = new IntPtr(-1);

	private const uint SwpNoSize = 1u;

	private const uint SwpNoMove = 2u;

	private const uint SwpNoActivate = 16u;

	private const uint SwpNoZOrder = 4u;

	private const uint SwpNoOwnerZOrder = 512u;

	public static bool SendToDesktopLayer(nint window)
	{
		if (window != 0)
		{
			return Dpi32.SetWindowPos(window, new IntPtr(1), 0, 0, 0, 0, 531u);
		}
		return false;
	}

	public static void ConstrainToDesktopLayer(nint windowPosition)
	{
		if (windowPosition != 0)
		{
			WindowPosition structure = Marshal.PtrToStructure<WindowPosition>(windowPosition);
			if ((structure.Flags & 4) == 0)
			{
				structure.InsertAfter = new IntPtr(1);
				structure.Flags |= 528u;
				Marshal.StructureToPtr(structure, windowPosition, fDeleteOld: false);
			}
		}
	}

	public static bool BringToTopWithoutActivation(nint window)
	{
		if (window == 0)
		{
			return false;
		}
		try
		{
			return Dpi32.SetWindowPos(window, HwndTopmost, 0, 0, 0, 0, 19u);
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return false;
		}
	}

	public static bool MakeNoActivate(nint window)
	{
		if (window == 0)
		{
			return false;
		}
		try
		{
			int windowLong = User32.GetWindowLong(window, -20);
			if ((windowLong & 0x8000000) != 0)
			{
				return true;
			}
			User32.SetWindowLong(window, -20, windowLong | 0x8000000);
			return IsNoActivate(window);
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return false;
		}
	}

	public static bool IsNoActivate(nint window)
	{
		if (window == 0)
		{
			return false;
		}
		try
		{
			return (User32.GetWindowLong(window, -20) & 0x8000000) != 0;
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return false;
		}
	}
}
