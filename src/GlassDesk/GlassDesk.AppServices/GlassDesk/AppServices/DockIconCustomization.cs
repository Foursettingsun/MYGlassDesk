using System;
using System.IO;
using GlassDesk.Core.Models;
using GlassDesk.Core.Storage;

namespace GlassDesk.AppServices;

public sealed class DockIconCustomization(GlassDeskDataStore data, Action changed)
{
	public static bool IsBuiltin(string id)
	{
		switch (id)
		{
		case "entry:launcher":
		case "entry:this-pc":
		case "entry:desktop-icons":
			return true;
		default:
			return false;
		}
	}

	public string? GetIconPath(string id)
	{
		return id switch
		{
			"entry:launcher" => data.Settings.Current.Dock.LauncherCustomIconPath, 
			"entry:this-pc" => data.Settings.Current.Dock.ThisPcCustomIconPath, 
			"entry:desktop-icons" => data.Settings.Current.Dock.DesktopIconsCustomIconPath, 
			_ => data.Library.Find(id)?.CustomIconPath, 
		};
	}

	public bool SetIcon(string id, string? source)
	{
		bool flag = IsBuiltin(id);
		AppEntry entry = (flag ? null : data.Library.Find(id));
		if (!flag && entry == null)
		{
			return false;
		}
		string iconPath = GetIconPath(id);
		string text = null;
		if (!string.IsNullOrWhiteSpace(source))
		{
			ImageFormat? imageFormat = ImageFormatSniffer.Detect(source);
			if (!imageFormat.HasValue)
			{
				return false;
			}
			text = Path.Combine(data.Paths.IconCacheDirectory, "custom-" + Guid.NewGuid().ToString("N") + ImageFormatSniffer.FileExtensionFor(imageFormat.Value));
			try
			{
				Directory.CreateDirectory(data.Paths.IconCacheDirectory);
				File.Copy(source, text);
			}
			catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
			{
				DeleteManaged(text);
				return false;
			}
		}
		try
		{
			Assign(text);
			if (flag)
			{
				data.Settings.Save();
			}
			else
			{
				data.Library.Save();
			}
		}
		catch (Exception ex2) when ((ex2 is StorageException || ex2 is IOException || ex2 is UnauthorizedAccessException) ? true : false)
		{
			Assign(iconPath);
			DeleteManaged(text);
			return false;
		}
		DeleteManaged(iconPath);
		changed();
		return true;
		void Assign(string? value)
		{
			switch (id)
			{
			case "entry:launcher":
				data.Settings.Current.Dock.LauncherCustomIconPath = value;
				break;
			case "entry:this-pc":
				data.Settings.Current.Dock.ThisPcCustomIconPath = value;
				break;
			case "entry:desktop-icons":
				data.Settings.Current.Dock.DesktopIconsCustomIconPath = value;
				break;
			default:
				entry.CustomIconPath = value;
				data.Library.Update(entry);
				break;
			}
		}
	}

	private void DeleteManaged(string? path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return;
		}
		try
		{
			if (string.Equals(Path.GetDirectoryName(Path.GetFullPath(path)), Path.GetFullPath(data.Paths.IconCacheDirectory), StringComparison.OrdinalIgnoreCase) && Path.GetFileName(path).StartsWith("custom-", StringComparison.OrdinalIgnoreCase))
			{
				File.Delete(path);
			}
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException) ? true : false)
		{
		}
	}
}
