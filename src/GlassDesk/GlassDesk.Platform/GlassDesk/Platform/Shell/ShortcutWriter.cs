using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using GlassDesk.Platform.Interop;

namespace GlassDesk.Platform.Shell;

public static class ShortcutWriter
{
	private static readonly StrategyBasedComWrappers Wrappers = new StrategyBasedComWrappers();

	public static bool Write(string shortcutPath, ShortcutDefinition definition)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(shortcutPath, "shortcutPath");
		ArgumentNullException.ThrowIfNull(definition, "definition");
		IShellLinkW shellLinkW = null;
		IPersistFile persistFile = null;
		try
		{
			if (Ole32.CoCreateInstance(in ShellComIds.ShellLinkClass, 0, 1u, in ShellComIds.ShellLinkW, out var instance) != 0 || instance == 0)
			{
				return false;
			}
			shellLinkW = (IShellLinkW)Wrappers.GetOrCreateObjectForComInstance(instance, CreateObjectFlags.UniqueInstance);
			try
			{
				if (Marshal.QueryInterface(instance, in ShellComIds.PersistFile, out var ppv) != 0 || ppv == 0)
				{
					return false;
				}
				persistFile = (IPersistFile)Wrappers.GetOrCreateObjectForComInstance(ppv, CreateObjectFlags.UniqueInstance);
				try
				{
					shellLinkW.SetPath(definition.TargetPath);
					if (!string.IsNullOrEmpty(definition.Arguments))
					{
						shellLinkW.SetArguments(definition.Arguments);
					}
					if (!string.IsNullOrEmpty(definition.WorkingDirectory))
					{
						shellLinkW.SetWorkingDirectory(definition.WorkingDirectory);
					}
					if (!string.IsNullOrEmpty(definition.Description))
					{
						shellLinkW.SetDescription(definition.Description);
					}
					if (!string.IsNullOrEmpty(definition.IconLocation))
					{
						var (iconPath, num) = IconExtractor.SplitIconSource(definition.IconLocation);
						shellLinkW.SetIconLocation(iconPath, num.GetValueOrDefault());
					}
					if (!string.IsNullOrEmpty(definition.Aumid))
					{
						if (Marshal.QueryInterface(instance, in ShellComIds.PropertyStore, out var ppv2) != 0 || ppv2 == 0)
						{
							return false;
						}
						IPropertyStore propertyStore = null;
						try
						{
							propertyStore = (IPropertyStore)Wrappers.GetOrCreateObjectForComInstance(ppv2, CreateObjectFlags.UniqueInstance);
							nint num2 = Marshal.StringToCoTaskMemUni(definition.Aumid);
							if (num2 == 0)
							{
								return false;
							}
							try
							{
								PropVariant value = new PropVariant
								{
									Type = 31,
									Pointer = num2
								};
								if (propertyStore.SetValue(in PropertyKeys.AppUserModelId, in value) != 0 || propertyStore.Commit() != 0)
								{
									return false;
								}
							}
							finally
							{
								Ole32.CoTaskMemFree(num2);
							}
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
					persistFile.Save(shortcutPath, remember: true);
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
			return true;
		}
		catch (Exception ex) when (!(ex is OutOfMemoryException))
		{
			return false;
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
}
