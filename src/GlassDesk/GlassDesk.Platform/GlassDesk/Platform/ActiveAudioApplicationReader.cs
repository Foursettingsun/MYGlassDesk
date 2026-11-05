using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace GlassDesk.Platform;

public static class ActiveAudioApplicationReader
{
	private enum EDataFlow
	{
		Render,
		Capture,
		All
	}

	private enum ERole
	{
		Console,
		Multimedia,
		Communications
	}

	private enum AudioSessionState
	{
		Inactive,
		Active,
		Expired
	}

	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
	private interface IMMDeviceEnumerator
	{
		[PreserveSig]
		int EnumAudioEndpoints(EDataFlow dataFlow, uint stateMask, out nint devices);

		[PreserveSig]
		int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice device);
	}

	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
	private interface IMMDevice
	{
		[PreserveSig]
		int Activate(ref Guid iid, uint clsCtx, nint activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object instance);
	}

	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("BFA971F1-4D5E-40BB-935E-967039BFBEE4")]
	private interface IAudioSessionManager2
	{
		[PreserveSig]
		int GetAudioSessionControl(ref Guid sessionGuid, uint streamFlags, out nint sessionControl);

		[PreserveSig]
		int GetSimpleAudioVolume(ref Guid sessionGuid, uint streamFlags, out nint simpleAudioVolume);

		[PreserveSig]
		int GetSessionEnumerator(out IAudioSessionEnumerator sessionEnumerator);

		[PreserveSig]
		int RegisterSessionNotification(nint sessionNotification);

		[PreserveSig]
		int UnregisterSessionNotification(nint sessionNotification);

		[PreserveSig]
		int RegisterDuckNotification([MarshalAs(UnmanagedType.LPWStr)] string sessionId, nint duckNotification);

		[PreserveSig]
		int UnregisterDuckNotification(nint duckNotification);
	}

	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8")]
	private interface IAudioSessionEnumerator
	{
		[PreserveSig]
		int GetCount(out int sessionCount);

		[PreserveSig]
		int GetSession(int sessionIndex, out IAudioSessionControl2 sessionControl);
	}

	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D")]
	private interface IAudioSessionControl2
	{
		[PreserveSig]
		int GetState(out AudioSessionState state);

		[PreserveSig]
		int GetDisplayName(out nint displayName);

		[PreserveSig]
		int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string value, nint eventContext);

		[PreserveSig]
		int GetIconPath(out nint iconPath);

		[PreserveSig]
		int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string value, nint eventContext);

		[PreserveSig]
		int GetGroupingParam(out Guid groupingId);

		[PreserveSig]
		int SetGroupingParam(ref Guid groupingId, nint eventContext);

		[PreserveSig]
		int RegisterAudioSessionNotification(nint events);

		[PreserveSig]
		int UnregisterAudioSessionNotification(nint events);

		[PreserveSig]
		int GetSessionIdentifier(out nint sessionIdentifier);

		[PreserveSig]
		int GetSessionInstanceIdentifier(out nint sessionInstanceIdentifier);

		[PreserveSig]
		int GetProcessId(out uint processId);

		[PreserveSig]
		int IsSystemSoundsSession();

		[PreserveSig]
		int SetDuckingPreference([MarshalAs(UnmanagedType.Bool)] bool optOut);
	}

	private const uint ClsCtxAll = 23u;

	public static ActiveAudioApplicationSnapshot Read()
	{
		object obj = null;
		IMMDevice device = null;
		object instance = null;
		IAudioSessionEnumerator sessionEnumerator = null;
		try
		{
			obj = new AudioDeviceEnumerator();
			if (((IMMDeviceEnumerator)obj).GetDefaultAudioEndpoint(EDataFlow.Render, ERole.Multimedia, out device) != 0 || device == null)
			{
				return default;
			}
			Guid iid = typeof(IAudioSessionManager2).GUID;
			if (device.Activate(ref iid, 23u, 0, out instance) != 0 || !(instance is IAudioSessionManager2 audioSessionManager) || audioSessionManager.GetSessionEnumerator(out sessionEnumerator) != 0 || sessionEnumerator.GetCount(out var sessionCount) != 0)
			{
				return default;
			}
			List<ActiveAudioApplicationSnapshot> list = new List<ActiveAudioApplicationSnapshot>();
			for (int i = 0; i < sessionCount; i++)
			{
				IAudioSessionControl2 sessionControl = null;
				try
				{
					if (sessionEnumerator.GetSession(i, out sessionControl) != 0 || sessionControl == null || sessionControl.GetState(out var state) != 0 || state != AudioSessionState.Active || sessionControl.GetProcessId(out var processId) != 0 || processId == 0 || processId == Environment.ProcessId)
					{
						continue;
					}
					using Process process = Process.GetProcessById((int)processId);
					if (!process.HasExited && !MediaSourceFilter.IsExcluded(process.ProcessName))
					{
						string text = ResolveApplicationName(process);
						if (!MediaSourceFilter.IsExcluded(text))
						{
							string text2 = SafeMainWindowTitle(process);
							list.Add(new ActiveAudioApplicationSnapshot(Available: true, string.IsNullOrWhiteSpace(text2) ? (text + " 正在播放") : text2, text, (int)processId));
						}
					}
				}
				catch (Exception ex) when ((ex is ArgumentException || ex is InvalidOperationException || ex is Win32Exception || ex is COMException) ? true : false)
				{
				}
				finally
				{
					ReleaseCom(sessionControl);
				}
			}
			return list.OrderByDescending((ActiveAudioApplicationSnapshot candidate) => !string.IsNullOrWhiteSpace(candidate.Title)).FirstOrDefault();
		}
		catch (Exception ex2) when ((ex2 is COMException || ex2 is InvalidCastException || ex2 is DllNotFoundException || ex2 is EntryPointNotFoundException || ex2 is PlatformNotSupportedException) ? true : false)
		{
			return default;
		}
		finally
		{
			ReleaseCom(sessionEnumerator);
			ReleaseCom(instance);
			ReleaseCom(device);
			ReleaseCom(obj);
		}
	}

	private static string ResolveApplicationName(Process process)
	{
		try
		{
			string text = process.MainModule?.FileVersionInfo.FileDescription;
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
		}
		catch (Exception ex) when ((ex is Win32Exception || ex is InvalidOperationException || ex is NotSupportedException) ? true : false)
		{
		}
		return process.ProcessName;
	}

	private static string SafeMainWindowTitle(Process process)
	{
		try
		{
			return process.MainWindowTitle;
		}
		catch (InvalidOperationException)
		{
			return string.Empty;
		}
	}

	private static void ReleaseCom(object? value)
	{
		if (value != null && Marshal.IsComObject(value))
		{
			Marshal.ReleaseComObject(value);
		}
	}
}
