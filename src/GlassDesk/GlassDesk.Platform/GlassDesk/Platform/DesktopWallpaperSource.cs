using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using GlassDesk.Platform.Displays;
using Microsoft.Win32;

namespace GlassDesk.Platform;

public static class DesktopWallpaperSource
{
	private struct NativeRect
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	[ComImport]
	[Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IDesktopWallpaper
	{
		[PreserveSig]
		int SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorId, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);

		[PreserveSig]
		int GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorId, [MarshalAs(UnmanagedType.LPWStr)] out string? wallpaper);

		[PreserveSig]
		int GetMonitorDevicePathAt(uint monitorIndex, [MarshalAs(UnmanagedType.LPWStr)] out string? monitorId);

		[PreserveSig]
		int GetMonitorDevicePathCount(out uint count);

		[PreserveSig]
		int GetMonitorRect([MarshalAs(UnmanagedType.LPWStr)] string monitorId, out NativeRect displayRect);

		[PreserveSig]
		int SetBackgroundColor(uint color);

		[PreserveSig]
		int GetBackgroundColor(out uint color);

		[PreserveSig]
		int SetPosition(DesktopWallpaperPosition position);

		[PreserveSig]
		int GetPosition(out DesktopWallpaperPosition position);
	}

	private static readonly Guid WallpaperClassId = new Guid("C2CF3110-460E-4FC1-B9D0-8A1C0C9CC4BD");

	public static DesktopWallpaperSnapshot? TryRead()
	{
		return TryReadCom() ?? TryReadSystemWallpaper();
	}

	private static DesktopWallpaperSnapshot? TryReadCom()
	{
		if (!OperatingSystem.IsWindowsVersionAtLeast(6, 2))
		{
			return null;
		}
		object obj = null;
		try
		{
			Type typeFromCLSID = Type.GetTypeFromCLSID(WallpaperClassId, throwOnError: false);
			if ((object)typeFromCLSID == null)
			{
				return null;
			}
			obj = Activator.CreateInstance(typeFromCLSID);
			IDesktopWallpaper desktopWallpaper = obj as IDesktopWallpaper;
			uint count = default;
			bool flag = desktopWallpaper == null || desktopWallpaper.GetMonitorDevicePathCount(out count) != 0;
			if (!flag)
			{
				bool flag2 = ((count > 256 || count == 0) ? true : false);
				flag = flag2;
			}
			if (flag || desktopWallpaper.GetPosition(out var position) != 0 || position > DesktopWallpaperPosition.Span || desktopWallpaper.GetBackgroundColor(out var color) != 0)
			{
				return null;
			}
			List<WallpaperMonitor> list = new List<WallpaperMonitor>((int)count);
			for (uint num = 0u; num < count; num++)
			{
				if (desktopWallpaper.GetMonitorDevicePathAt(num, out string monitorId) == 0 && !string.IsNullOrEmpty(monitorId) && desktopWallpaper.GetMonitorRect(monitorId, out var displayRect) == 0 && desktopWallpaper.GetWallpaper(monitorId, out string wallpaper) == 0)
				{
					long num2 = (long)displayRect.Right - (long)displayRect.Left;
					long num3 = (long)displayRect.Bottom - (long)displayRect.Top;
					flag = ((num2 <= 0 || num2 > int.MaxValue) ? true : false);
					bool flag2 = flag;
					if (!flag2)
					{
						bool flag3 = ((num3 <= 0 || num3 > int.MaxValue) ? true : false);
						flag2 = flag3;
					}
					if (!flag2)
					{
						list.Add(new WallpaperMonitor(wallpaper ?? string.Empty, new PixelRect(displayRect.Left, displayRect.Top, (int)num2, (int)num3)));
					}
				}
			}
			return (list.Count == 0) ? null : new DesktopWallpaperSnapshot(Array.AsReadOnly(list.ToArray()), position, color);
		}
		catch (Exception ex) when ((ex is COMException || ex is UnauthorizedAccessException || ex is InvalidCastException || ex is NotSupportedException || ex is TypeLoadException) ? true : false)
		{
			return null;
		}
		finally
		{
			if (obj != null && Marshal.IsComObject(obj))
			{
				Marshal.ReleaseComObject(obj);
			}
		}
	}

	private static DesktopWallpaperSnapshot? TryReadSystemWallpaper()
	{
		char[] array = new char[260];
		if (!SystemParametersInfo(115u, (uint)array.Length, array, 0u))
		{
			return null;
		}
		DisplayInfo displayInfo = DisplayService.Primary();
		if ((object)displayInfo == null)
		{
			return null;
		}
		DesktopWallpaperPosition position = DesktopWallpaperPosition.Fill;
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop", writable: false);
			DesktopWallpaperPosition desktopWallpaperPosition = ((registryKey?.GetValue("TileWallpaper")?.ToString() == "1") ? DesktopWallpaperPosition.Tile : (registryKey?.GetValue("WallpaperStyle")?.ToString() switch
			{
				"0" => DesktopWallpaperPosition.Center, 
				"2" => DesktopWallpaperPosition.Stretch, 
				"6" => DesktopWallpaperPosition.Fit, 
				_ => DesktopWallpaperPosition.Fill, 
			}));
			position = desktopWallpaperPosition;
		}
		catch (Exception ex) when ((ex is UnauthorizedAccessException || ex is SecurityException) ? true : false)
		{
			return null;
		}
		int num = Array.IndexOf(array, '\0');
		return new DesktopWallpaperSnapshot(new _003C_003Ez__ReadOnlySingleElementList<WallpaperMonitor>(new WallpaperMonitor(new string(array, 0, (num < 0) ? array.Length : num), displayInfo.Bounds)), position, GetSysColor(1));
	}

	[DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "SystemParametersInfoW", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SystemParametersInfo(uint action, uint count, [Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2)] char[] value, uint flags);

	[DllImport("user32.dll")]
	private static extern uint GetSysColor(int index);
}
