using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using GlassDesk.Platform.Interop;

namespace GlassDesk.Platform.Shell;

public static class ShortcutResolver
{
	private const int PathCapacity = 1024;

	private static readonly StrategyBasedComWrappers Wrappers = new StrategyBasedComWrappers();

	public static ShortcutInfo? Resolve(string shortcutPath)
	{
		if (string.IsNullOrWhiteSpace(shortcutPath))
		{
			return null;
		}
		IShellLinkW shellLinkW = null;
		IPersistFile persistFile = null;
		string text = null;
		try
		{
			if (Ole32.CoCreateInstance(in ShellComIds.ShellLinkClass, 0, 1u, in ShellComIds.ShellLinkW, out var instance) != 0 || instance == 0)
			{
				return null;
			}
			shellLinkW = (IShellLinkW)Wrappers.GetOrCreateObjectForComInstance(instance, CreateObjectFlags.UniqueInstance);
			try
			{
				if (Marshal.QueryInterface(instance, in ShellComIds.PersistFile, out var ppv) != 0 || ppv == 0)
				{
					return null;
				}
				persistFile = (IPersistFile)Wrappers.GetOrCreateObjectForComInstance(ppv, CreateObjectFlags.UniqueInstance);
				try
				{
					persistFile.Load(shortcutPath, 0u);
				}
				finally
				{
					Marshal.Release(ppv);
				}
				if (Marshal.QueryInterface(instance, in ShellComIds.PropertyStore, out var ppv2) == 0 && ppv2 != 0)
				{
					IPropertyStore propertyStore = null;
					try
					{
						propertyStore = (IPropertyStore)Wrappers.GetOrCreateObjectForComInstance(ppv2, CreateObjectFlags.UniqueInstance);
						text = WindowIdentity.ReadAumid(propertyStore);
					}
					finally
					{
						Marshal.Release(ppv2);
						if (propertyStore is IDisposable disposable)
						{
							disposable.Dispose();
						}
					}
				}
			}
			finally
			{
				Marshal.Release(instance);
			}
			string text2 = Read(shellLinkW, (IShellLinkW shellLinkW2, nint buffer, int capacity) =>
			{
				shellLinkW2.GetPath(buffer, capacity, 0, 0u);
			});
			nint num = ReadIdList(shellLinkW);
			try
			{
				if (text == null)
				{
					text = ReadShellIdentityFromIdList(num);
				}
				if (string.IsNullOrWhiteSpace(text2))
				{
					text2 = ReadUrlFromIdList(num) ?? string.Empty;
				}
				if (string.IsNullOrWhiteSpace(text2) && num == 0)
				{
					return null;
				}
				string arguments = Read(shellLinkW, (IShellLinkW shellLinkW2, nint buffer, int capacity) =>
				{
					shellLinkW2.GetArguments(buffer, capacity);
				});
				string workingDirectory = Read(shellLinkW, (IShellLinkW shellLinkW2, nint buffer, int capacity) =>
				{
					shellLinkW2.GetWorkingDirectory(buffer, capacity);
				});
				string description = Read(shellLinkW, (IShellLinkW shellLinkW2, nint buffer, int capacity) =>
				{
					shellLinkW2.GetDescription(buffer, capacity);
				});
				int iconIndex = 0;
				string iconLocation = Read(shellLinkW, (IShellLinkW shellLinkW2, nint buffer, int capacity) =>
				{
					shellLinkW2.GetIconLocation(buffer, capacity, out iconIndex);
				});
				return new ShortcutInfo(text2, arguments, workingDirectory, BuildIconSource(iconLocation, iconIndex, text2), description, num != 0, text);
			}
			finally
			{
				if (num != 0)
				{
					Shell32.ILFree(num);
				}
			}
		}
		catch (Exception ex) when (!(ex is OutOfMemoryException))
		{
			return null;
		}
		finally
		{
			if (persistFile is IDisposable disposable2)
			{
				disposable2.Dispose();
			}
			if (shellLinkW is IDisposable disposable3)
			{
				disposable3.Dispose();
			}
		}
	}

	private static nint ReadIdList(IShellLinkW link)
	{
		link.GetIDList(out var itemIdList);
		return itemIdList;
	}

	private static string? ReadShellIdentityFromIdList(nint itemIdList)
	{
		if (itemIdList == 0)
		{
			return null;
		}
		nint name = 0;
		try
		{
			return (Shell32.SHGetNameFromIDList(itemIdList, 2147647488u, out name) == 0 && name != 0) ? LaunchTargetIdentity.FromShellTarget(Marshal.PtrToStringUni(name)) : null;
		}
		finally
		{
			if (name != 0)
			{
				Ole32.CoTaskMemFree(name);
			}
		}
	}

	private static string? ReadUrlFromIdList(nint itemIdList)
	{
		if (itemIdList == 0)
		{
			return null;
		}
		nint name = 0;
		try
		{
			if (Shell32.SHGetNameFromIDList(itemIdList, 2147909632u, out name) != 0 || name == 0)
			{
				return null;
			}
			string text = Marshal.PtrToStringUni(name);
			Uri result;
			return (Uri.TryCreate(text, UriKind.Absolute, out result) && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps)) ? text : null;
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return null;
		}
		finally
		{
			if (name != 0)
			{
				Ole32.CoTaskMemFree(name);
			}
		}
	}

	private static string BuildIconSource(string iconLocation, int iconIndex, string target)
	{
		string text = (string.IsNullOrWhiteSpace(iconLocation) ? target : iconLocation);
		if (string.IsNullOrWhiteSpace(text))
		{
			return string.Empty;
		}
		if (iconIndex != 0)
		{
			return $"{text},{iconIndex}";
		}
		return text;
	}

	private unsafe static string Read(IShellLinkW link, Action<IShellLinkW, nint, int> read)
	{
		int num = 2048;
		nint num2 = Marshal.AllocHGlobal(num);
		try
		{
			new Span<byte>((void*)num2, num).Clear();
			read(link, num2, 1024);
			return Marshal.PtrToStringUni(num2) ?? string.Empty;
		}
		finally
		{
			Marshal.FreeHGlobal(num2);
		}
	}
}
