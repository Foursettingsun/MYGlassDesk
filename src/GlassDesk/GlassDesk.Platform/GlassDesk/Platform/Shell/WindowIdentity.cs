using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using GlassDesk.Platform.Interop;

namespace GlassDesk.Platform.Shell;

public static class WindowIdentity
{
	private static readonly StrategyBasedComWrappers Wrappers = new StrategyBasedComWrappers();

	private const int IdentityCapacity = 512;

	public static string? TryGetWindowAumid(nint window)
	{
		if (window == 0)
		{
			return null;
		}
		IPropertyStore propertyStore = null;
		try
		{
			if (Shell32.SHGetPropertyStoreForWindow(window, in ShellComIds.PropertyStore, out var store) != 0 || store == 0)
			{
				return null;
			}
			propertyStore = (IPropertyStore)Wrappers.GetOrCreateObjectForComInstance(store, CreateObjectFlags.UniqueInstance);
			return ReadAumid(propertyStore);
		}
		catch (Exception ex) when (!(ex is OutOfMemoryException))
		{
			return null;
		}
		finally
		{
			if (propertyStore is IDisposable disposable)
			{
				disposable.Dispose();
			}
		}
	}

	public static string? TryGetProcessAumid(uint processId)
	{
		if (processId == 0)
		{
			return null;
		}
		nint num = Kernel32.OpenProcess(4096u, inheritHandle: false, processId);
		if (num == 0)
		{
			return null;
		}
		try
		{
			char[] array = new char[512];
			uint length = (uint)array.Length;
			int applicationUserModelId = Kernel32.GetApplicationUserModelId(num, ref length, array);
			if (applicationUserModelId == -2147024774 && length != 0 && length < 4096)
			{
				array = new char[length];
				length = (uint)array.Length;
				applicationUserModelId = Kernel32.GetApplicationUserModelId(num, ref length, array);
			}
			if (applicationUserModelId != 0)
			{
				return null;
			}
			string text = new string(array[..(int)Math.Min(length, (uint)array.Length)]).TrimEnd('\0');
			return (text.Length == 0) ? null : text;
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return null;
		}
		finally
		{
			Kernel32.CloseHandle(num);
		}
	}

	public static string? TryGetShortcutAumid(string shortcutPath)
	{
		if (string.IsNullOrWhiteSpace(shortcutPath))
		{
			return null;
		}
		IShellLinkW shellLinkW = null;
		IPropertyStore propertyStore = null;
		try
		{
			if (Ole32.CoCreateInstance(in ShellComIds.ShellLinkClass, 0, 1u, in ShellComIds.ShellLinkW, out var instance) != 0 || instance == 0)
			{
				return null;
			}
			try
			{
				shellLinkW = (IShellLinkW)Wrappers.GetOrCreateObjectForComInstance(instance, CreateObjectFlags.UniqueInstance);
				if (Marshal.QueryInterface(instance, in ShellComIds.PropertyStore, out var ppv) != 0 || ppv == 0)
				{
					return null;
				}
				try
				{
					propertyStore = (IPropertyStore)Wrappers.GetOrCreateObjectForComInstance(ppv, CreateObjectFlags.UniqueInstance);
					IPersistFile persistFile = (IPersistFile)Wrappers.GetOrCreateObjectForComInstance(instance, CreateObjectFlags.UniqueInstance);
					try
					{
						persistFile.Load(shortcutPath, 0u);
					}
					finally
					{
						if (persistFile is IDisposable disposable)
						{
							disposable.Dispose();
						}
					}
					return ReadAumid(propertyStore);
				}
				finally
				{
					Marshal.Release(ppv);
				}
			}
			finally
			{
				Marshal.Release(instance);
			}
		}
		catch (Exception ex) when (!(ex is OutOfMemoryException))
		{
			return null;
		}
		finally
		{
			if (shellLinkW is IDisposable disposable2)
			{
				disposable2.Dispose();
			}
			if (propertyStore is IDisposable disposable3)
			{
				disposable3.Dispose();
			}
		}
	}

	internal static string? ReadAumid(IPropertyStore store)
	{
		if (store.GetValue(in PropertyKeys.AppUserModelId, out var value) != 0 || value.Type == 0 || value.Pointer == 0)
		{
			return null;
		}
		if (value.Type != 31)
		{
			return null;
		}
		try
		{
			string text = Marshal.PtrToStringUni(value.Pointer);
			return string.IsNullOrWhiteSpace(text) ? null : text;
		}
		finally
		{
			Ole32.CoTaskMemFree(value.Pointer);
		}
	}
}
