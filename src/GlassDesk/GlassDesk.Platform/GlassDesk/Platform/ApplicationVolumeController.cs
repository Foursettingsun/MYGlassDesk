using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace GlassDesk.Platform;

public static class ApplicationVolumeController
{
	private sealed record SessionVolume(string Key, string Name, string? Path, string Endpoint, string Id, double Percent, bool Muted);

	[ComImport]
	[Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
	private sealed class DeviceEnumerator
	{
	}

	[ComImport]
	[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IDeviceEnumerator
	{
		[PreserveSig]
		int EnumAudioEndpoints(int flow, uint state, out nint devices);

		[PreserveSig]
		int GetDefaultAudioEndpoint(int flow, int role, out IDevice device);
	}

	[ComImport]
	[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IDevice
	{
		[PreserveSig]
		int Activate(ref Guid iid, uint context, nint parameters, [MarshalAs(UnmanagedType.IUnknown)] out object instance);

		[PreserveSig]
		int OpenPropertyStore(uint access, out nint store);

		[PreserveSig]
		int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
	}

	[ComImport]
	[Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IAudioSessionManager2
	{
		[PreserveSig]
		int GetAudioSessionControl(ref Guid session, uint flags, out nint control);

		[PreserveSig]
		int GetSimpleAudioVolume(ref Guid session, uint flags, out nint volume);

		[PreserveSig]
		int GetSessionEnumerator(out IAudioSessionEnumerator enumerator);
	}

	[ComImport]
	[Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IAudioSessionEnumerator
	{
		[PreserveSig]
		int GetCount(out int count);

		[PreserveSig]
		int GetSession(int index, out IAudioSessionControl2 control);
	}

	[ComImport]
	[Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IAudioSessionControl2
	{
		[PreserveSig]
		int GetState(out int state);

		[PreserveSig]
		int GetDisplayName(out nint name);

		[PreserveSig]
		int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string value, nint context);

		[PreserveSig]
		int GetIconPath(out nint path);

		[PreserveSig]
		int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string value, nint context);

		[PreserveSig]
		int GetGroupingParam(out Guid group);

		[PreserveSig]
		int SetGroupingParam(ref Guid group, nint context);

		[PreserveSig]
		int RegisterAudioSessionNotification(nint events);

		[PreserveSig]
		int UnregisterAudioSessionNotification(nint events);

		[PreserveSig]
		int GetSessionIdentifier(out nint id);

		[PreserveSig]
		int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string id);

		[PreserveSig]
		int GetProcessId(out uint pid);

		[PreserveSig]
		int IsSystemSoundsSession();
	}

	[ComImport]
	[Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface ISimpleAudioVolume
	{
		[PreserveSig]
		int SetMasterVolume(float level, nint context);

		[PreserveSig]
		int GetMasterVolume(out float level);

		[PreserveSig]
		int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, nint context);

		[PreserveSig]
		int GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
	}

	public static ApplicationVolumeSnapshot Read()
	{
		List<SessionVolume> sessions = new List<SessionVolume>();
		return new ApplicationVolumeSnapshot(VisitSessions(null, (string endpoint, IAudioSessionControl2 session, ISimpleAudioVolume volume) =>
		{
			if (session.GetState(out var state) >= 0 && state != 2 && session.GetProcessId(out var pid) >= 0 && session.GetSessionInstanceIdentifier(out string id) >= 0 && !string.IsNullOrEmpty(id) && volume.GetMasterVolume(out var level) >= 0 && volume.GetMute(out var mute) >= 0)
			{
				string text = null;
				bool flag = session.IsSystemSoundsSession() == 0;
				string name = (flag ? "系统声音" : $"应用 {pid}");
				if (!flag && pid != 0)
				{
					try
					{
						using Process process = Process.GetProcessById((int)pid);
						name = process.ProcessName;
						text = process.MainModule?.FileName;
						string text2 = process.MainModule?.FileVersionInfo.FileDescription;
						if (!string.IsNullOrWhiteSpace(text2))
						{
							name = text2;
						}
					}
					catch (Exception ex) when ((ex is ArgumentException || ex is InvalidOperationException || ex is Win32Exception) ? true : false)
					{
					}
				}
				string key = (flag ? "system-sounds" : (text ?? $"process:{pid}"));
				sessions.Add(new SessionVolume(key, name, text, endpoint, id, Math.Clamp((double)level * 100.0, 0.0, 100.0), mute));
			}
		}), (from @group in sessions.GroupBy((SessionVolume session) => session.Key, StringComparer.OrdinalIgnoreCase)
			select new ApplicationVolume(@group.Key, @group.First().Name, @group.First().Path, @group.First().Endpoint, @group.Select((SessionVolume session) => session.Id).ToArray(), @group.Average((SessionVolume session) => session.Percent), @group.All((SessionVolume session) => session.Muted))).OrderBy((ApplicationVolume app) => app.Name, StringComparer.CurrentCultureIgnoreCase).ToArray());
	}

	public static bool SetVolume(ApplicationVolume application, double percent)
	{
		if (!double.IsFinite(percent))
		{
			return false;
		}
		return Change(application, (ISimpleAudioVolume volume) =>
		{
			bool flag = volume.SetMasterVolume((float)(Math.Clamp(percent, 0.0, 100.0) / 100.0), IntPtr.Zero) >= 0;
			if (percent > 0.0)
			{
				flag &= volume.SetMute(mute: false, IntPtr.Zero) >= 0;
			}
			return flag;
		});
	}

	public static bool SetMuted(ApplicationVolume application, bool muted)
	{
		return Change(application, (ISimpleAudioVolume volume) => volume.SetMute(muted, IntPtr.Zero) >= 0);
	}

	private static bool Change(ApplicationVolume application, Func<ISimpleAudioVolume, bool> action)
	{
		if (string.IsNullOrEmpty(application.EndpointId) || application.SessionIds.Count == 0)
		{
			return false;
		}
		HashSet<string> ids = application.SessionIds.ToHashSet(StringComparer.Ordinal);
		int changed = 0;
		bool success = true;
		if (VisitSessions(application.EndpointId, (string _, IAudioSessionControl2 session, ISimpleAudioVolume volume) =>
		{
			if (session.GetState(out var state) >= 0 && state != 2 && session.GetSessionInstanceIdentifier(out string id) >= 0 && ids.Contains(id))
			{
				changed++;
				success &= action(volume);
			}
		}) & success)
		{
			return changed > 0;
		}
		return false;
	}

	private static bool VisitSessions(string? expectedEndpoint, Action<string, IAudioSessionControl2, ISimpleAudioVolume> action)
	{
		object obj = null;
		IDevice device = null;
		object instance = null;
		IAudioSessionEnumerator enumerator = null;
		try
		{
			obj = new DeviceEnumerator();
			if (((IDeviceEnumerator)obj).GetDefaultAudioEndpoint(0, 1, out device) < 0 || device.GetId(out string id) < 0 || (expectedEndpoint != null && !string.Equals(id, expectedEndpoint, StringComparison.Ordinal)))
			{
				return false;
			}
			Guid iid = typeof(IAudioSessionManager2).GUID;
			if (device.Activate(ref iid, 23u, IntPtr.Zero, out instance) < 0 || !(instance is IAudioSessionManager2 audioSessionManager) || audioSessionManager.GetSessionEnumerator(out enumerator) < 0 || enumerator.GetCount(out var count) < 0)
			{
				return false;
			}
			for (int i = 0; i < count; i++)
			{
				IAudioSessionControl2 control = null;
				try
				{
					if (enumerator.GetSession(i, out control) >= 0 && control is ISimpleAudioVolume arg)
					{
						action(id, control, arg);
					}
				}
				catch (COMException)
				{
				}
				finally
				{
					Release(control);
				}
			}
			return true;
		}
		catch (Exception ex2) when ((ex2 is COMException || ex2 is InvalidCastException || ex2 is PlatformNotSupportedException) ? true : false)
		{
			return false;
		}
		finally
		{
			Release(enumerator);
			Release(instance);
			Release(device);
			Release(obj);
		}
	}

	private static void Release(object? value)
	{
		if (value != null && Marshal.IsComObject(value))
		{
			Marshal.ReleaseComObject(value);
		}
	}
}
