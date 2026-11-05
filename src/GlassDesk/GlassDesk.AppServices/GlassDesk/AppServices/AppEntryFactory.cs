using System;
using System.Globalization;
using System.IO;
using GlassDesk.Core.Contracts;
using GlassDesk.Core.Models;
using GlassDesk.Core.Storage;
using GlassDesk.Platform.Shell;

namespace GlassDesk.AppServices;

public static class AppEntryFactory
{
	public static (AppEntry? Entry, ImportFailureReason Reason, string Message) TryCreate(ImportRequest request)
	{
		ArgumentNullException.ThrowIfNull(request, "request");
		if (!request.IsUrl)
		{
			return CreateFromPath(request);
		}
		return CreateFromUrl(request);
	}

	public static (AppEntry? Entry, ImportFailureReason Reason, string Message) TryCreateFromPath(string path, string? displayNameOverride = null)
	{
		return CreateFromPath(new ImportRequest
		{
			Source = path,
			DisplayName = displayNameOverride
		});
	}

	private static (AppEntry?, ImportFailureReason, string) CreateFromUrl(ImportRequest request)
	{
		string text = request.Source?.Trim() ?? string.Empty;
		if (!InternetShortcutReader.IsSupportedUrl(text))
		{
			return (null, ImportFailureReason.UnsupportedType, "不是可用的网页地址：" + text);
		}
		return (new AppEntry
		{
			Id = AppLibraryStore.NewId(),
			Kind = AppItemKind.Url,
			Target = text,
			DisplayName = ResolveName(request.DisplayName, DeriveUrlName(text)),
			IconSource = text
		}, ImportFailureReason.NotFound, string.Empty);
	}

	private static (AppEntry?, ImportFailureReason, string) CreateFromPath(ImportRequest request)
	{
		string text = request.Source?.Trim() ?? string.Empty;
		if (text.Length == 0)
		{
			return (null, ImportFailureReason.NotFound, "没有给出路径。");
		}
		string fullPath;
		try
		{
			fullPath = Path.GetFullPath(text);
		}
		catch (Exception ex) when ((ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException) ? true : false)
		{
			return (null, ImportFailureReason.NotFound, "路径无法解析：" + text);
		}
		bool flag;
		bool flag2;
		try
		{
			flag = Directory.Exists(fullPath);
			flag2 = flag || File.Exists(fullPath);
		}
		catch (Exception ex2) when ((ex2 is IOException || ex2 is UnauthorizedAccessException) ? true : false)
		{
			return (null, ImportFailureReason.AccessDenied, "读不到这个位置：" + fullPath);
		}
		if (!flag2)
		{
			return (null, ImportFailureReason.NotFound, "找不到：" + fullPath);
		}
		AppItemKind appItemKind = ItemKindDetector.Detect(fullPath, flag);
		return appItemKind switch
		{
			AppItemKind.Shortcut => CreateFromShortcut(request, fullPath), 
			AppItemKind.Url => CreateFromInternetShortcut(request, fullPath), 
			_ => (CreatePlain(request, fullPath, appItemKind), ImportFailureReason.NotFound, string.Empty), 
		};
	}

	private static (AppEntry?, ImportFailureReason, string) CreateFromShortcut(ImportRequest request, string shortcutPath)
	{
		ShortcutInfo shortcutInfo = ShortcutResolver.Resolve(shortcutPath);
		string derived = ItemKindDetector.DeriveDisplayName(shortcutPath, AppItemKind.Shortcut);
		if ((object)shortcutInfo == null)
		{
			return (null, ImportFailureReason.ShortcutUnreadable, "快捷方式读不出来：" + shortcutPath);
		}
		if (!shortcutInfo.HasResolvedTarget)
		{
			return (new AppEntry
			{
				Id = AppLibraryStore.NewId(),
				Kind = AppItemKind.Shortcut,
				Target = shortcutPath,
				DisplayName = ResolveName(request.DisplayName, derived),
				Arguments = Nullify(shortcutInfo.Arguments),
				WorkingDirectory = Nullify(shortcutInfo.WorkingDirectory),
				IconSource = shortcutPath,
				AppUserModelId = shortcutInfo.Aumid
			}, ImportFailureReason.NotFound, string.Empty);
		}
		if (shortcutInfo.TargetIsUrl)
		{
			return (new AppEntry
			{
				Id = AppLibraryStore.NewId(),
				Kind = AppItemKind.Url,
				Target = shortcutInfo.Target,
				DisplayName = ResolveName(request.DisplayName, derived),
				Arguments = Nullify(shortcutInfo.Arguments),
				WorkingDirectory = Nullify(shortcutInfo.WorkingDirectory),
				IconSource = ResolveShortcutIconSource(shortcutInfo, shortcutPath)
			}, ImportFailureReason.NotFound, string.Empty);
		}
		string text = ResolveShortcutTarget(shortcutInfo.Target, shortcutInfo.WorkingDirectory, shortcutPath);
		bool flag = ExistsQuietly(text);
		return (new AppEntry
		{
			Id = AppLibraryStore.NewId(),
			Kind = (flag ? ItemKindDetector.Detect(text, IsDirectoryQuietly(text)) : AppItemKind.Program),
			Target = text,
			DisplayName = ResolveName(request.DisplayName, derived),
			Arguments = Nullify(shortcutInfo.Arguments),
			WorkingDirectory = Nullify(shortcutInfo.WorkingDirectory),
			IconSource = ResolveShortcutIconSource(shortcutInfo, shortcutPath),
			IsTargetMissing = !flag,
			AppUserModelId = shortcutInfo.Aumid
		}, ImportFailureReason.NotFound, string.Empty);
	}

	private static (AppEntry?, ImportFailureReason, string) CreateFromInternetShortcut(ImportRequest request, string path)
	{
		string text = InternetShortcutReader.ReadUrl(path);
		if (text == null)
		{
			return (CreatePlain(request, path, AppItemKind.File), ImportFailureReason.NotFound, string.Empty);
		}
		return (new AppEntry
		{
			Id = AppLibraryStore.NewId(),
			Kind = AppItemKind.Url,
			Target = text,
			DisplayName = ResolveName(request.DisplayName, ItemKindDetector.DeriveDisplayName(path, AppItemKind.Url)),
			IconSource = text
		}, ImportFailureReason.NotFound, string.Empty);
	}

	private static AppEntry CreatePlain(ImportRequest request, string path, AppItemKind kind)
	{
		return new AppEntry
		{
			Id = AppLibraryStore.NewId(),
			Kind = kind,
			Target = path,
			DisplayName = ResolveName(request.DisplayName, ItemKindDetector.DeriveDisplayName(path, kind)),
			IconSource = path
		};
	}

	private static string ResolveShortcutTarget(string target, string workingDirectory, string shortcutPath)
	{
		try
		{
			if (Path.IsPathFullyQualified(target))
			{
				return Path.GetFullPath(target);
			}
			if (!string.IsNullOrWhiteSpace(workingDirectory))
			{
				string fullPath = Path.GetFullPath(Path.Combine(workingDirectory, target));
				if (ExistsQuietly(fullPath))
				{
					return fullPath;
				}
			}
			return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(shortcutPath) ?? string.Empty, target));
		}
		catch (Exception ex) when ((ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException || ex is IOException) ? true : false)
		{
			return target;
		}
	}

	private static string ResolveShortcutIconSource(ShortcutInfo resolved, string shortcutPath)
	{
		if (!string.IsNullOrWhiteSpace(resolved.IconSource))
		{
			if (ExistsQuietly(IconExtractor.SplitIconSource(resolved.IconSource).Path) || string.Equals(resolved.IconSource, resolved.Target, StringComparison.OrdinalIgnoreCase))
			{
				return resolved.IconSource;
			}
			if (ExistsQuietly(resolved.Target))
			{
				return resolved.Target;
			}
		}
		if (!ExistsQuietly(shortcutPath))
		{
			return resolved.Target;
		}
		return shortcutPath;
	}

	private static bool ExistsQuietly(string path)
	{
		try
		{
			return File.Exists(path) || Directory.Exists(path);
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
			return false;
		}
	}

	private static bool IsDirectoryQuietly(string path)
	{
		try
		{
			return Directory.Exists(path);
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
			return false;
		}
	}

	private static string ResolveName(string? overrideName, string derived)
	{
		if (!string.IsNullOrWhiteSpace(overrideName))
		{
			return overrideName.Trim();
		}
		return derived;
	}

	private static string DeriveUrlName(string url)
	{
		if (!Uri.TryCreate(url, UriKind.Absolute, out Uri result) || string.IsNullOrWhiteSpace(result.Host))
		{
			return url;
		}
		if (!result.IsDefaultPort)
		{
			return result.Host + ":" + result.Port.ToString(CultureInfo.InvariantCulture);
		}
		return result.Host;
	}

	private static string? Nullify(string value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		return null;
	}
}
