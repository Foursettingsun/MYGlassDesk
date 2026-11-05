using System;
using System.Buffers.Binary;
using System.IO;
using System.Security;
using Microsoft.Win32;

namespace GlassDesk.Platform.Installation;

public sealed class InstallationLocator
{
	public const string AppId = "GlassDesk.Independent";

	public const string UninstallRegistrySubKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\GlassDesk.Independent_is1";

	private readonly string _registrySubKey;

	public InstallationLocator()
		: this("Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\GlassDesk.Independent_is1")
	{
	}

	internal InstallationLocator(string registrySubKey)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(registrySubKey, "registrySubKey");
		_registrySubKey = registrySubKey;
	}

	public InstallationInfo Inspect(string executablePath)
	{
		if (string.IsNullOrWhiteSpace(executablePath))
		{
			return Mismatch("当前 GlassDesk 可执行文件路径不可用。");
		}
		RegistryKey registryKey;
		try
		{
			registryKey = Registry.CurrentUser.OpenSubKey(_registrySubKey, writable: false);
		}
		catch (Exception ex) when (IsRegistryFailure(ex))
		{
			return Mismatch("无法读取当前用户的 GlassDesk 安装登记：" + ex.Message);
		}
		if (registryKey == null)
		{
			return new InstallationInfo(InstallationState.Portable);
		}
		using (registryKey)
		{
			try
			{
				string a = ReadString(registryKey, "DisplayName");
				string text = ReadString(registryKey, "InstallLocation");
				string text2 = ReadString(registryKey, "UninstallString");
				if (!string.Equals(a, "GlassDesk", StringComparison.Ordinal))
				{
					return Mismatch("固定产品登记的显示名称与 GlassDesk 不一致。");
				}
				if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
				{
					return Mismatch("固定产品登记缺少安装位置或卸载程序路径。");
				}
				string text3 = Path.TrimEndingDirectorySeparator(Path.GetFullPath(text));
				string fullPath = Path.GetFullPath(executablePath);
				if (!PathsEqual(Path.GetDirectoryName(fullPath) ?? string.Empty, text3) || !string.Equals(Path.GetFileName(fullPath), "GlassDesk.exe", StringComparison.OrdinalIgnoreCase))
				{
					return Mismatch("当前进程不在登记的 GlassDesk 安装目录中。");
				}
				if (!Directory.Exists(text3) || !File.Exists(fullPath) || !HasNoReparsePointInPath(fullPath))
				{
					return Mismatch("当前程序安装路径缺失或包含联接/符号链接。");
				}
				string text4 = ParseExecutablePath(text2);
				if (text4 == null)
				{
					return Mismatch("卸载登记不是可核验的可执行文件路径。");
				}
				text4 = Path.GetFullPath(text4);
				if (!PathsEqual(Path.GetDirectoryName(text4) ?? string.Empty, text3) || !IsInnoUninstallerName(Path.GetFileName(text4)) || !File.Exists(text4) || !HasNoReparsePointInPath(text4) || !IsPortableExecutable(text4))
				{
					return Mismatch("卸载程序不存在，或不属于当前 GlassDesk 安装目录。");
				}
				return new InstallationInfo(InstallationState.VerifiedInstalled, text3, text4);
			}
			catch (Exception ex2) when (((Func<bool>)delegate
			{
				// Could not convert BlockContainer to single expression
				bool flag = IsRegistryFailure(ex2);
				if (!flag)
				{
					flag = ((ex2 is IOException || ex2 is ArgumentException || ex2 is NotSupportedException) ? true : false);
				}
				return flag;
			}).Invoke())
			{
				return Mismatch("GlassDesk 安装登记无法安全核验：" + ex2.Message);
			}
		}
	}

	private static string? ReadString(RegistryKey key, string name)
	{
		return key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string;
	}

	private static string? ParseExecutablePath(string command)
	{
		string text = command.TrimStart();
		if (text.Length == 0)
		{
			return null;
		}
		if (text[0] == '"')
		{
			int num = text.IndexOf('"', 1);
			if (num <= 1 || text.Substring(num + 1).Trim().Length != 0)
			{
				return null;
			}
			return text.Substring(1, num - 1);
		}
		if (text.IndexOfAny(new char[2] { ' ', '\t' }) >= 0)
		{
			return null;
		}
		return text;
	}

	private static bool IsInnoUninstallerName(string fileName)
	{
		if (fileName.Length != 12 || !fileName.StartsWith("unins", StringComparison.OrdinalIgnoreCase) || !fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (char.IsAsciiDigit(fileName[5]) && char.IsAsciiDigit(fileName[6]))
		{
			return char.IsAsciiDigit(fileName[7]);
		}
		return false;
	}

	private static bool HasNoReparsePointInPath(string path)
	{
		string text = Path.GetFullPath(path);
		while (!string.IsNullOrWhiteSpace(text))
		{
			if ((File.GetAttributes(text) & FileAttributes.ReparsePoint) != 0)
			{
				return false;
			}
			string text2 = Directory.GetParent(text)?.FullName;
			if (text2 == null || PathsEqual(text2, text))
			{
				break;
			}
			text = text2;
		}
		return true;
	}

	private static bool IsPortableExecutable(string path)
	{
		using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		Span<byte> buffer = stackalloc byte[64];
		try
		{
			fileStream.ReadExactly(buffer);
		}
		catch (EndOfStreamException)
		{
			return false;
		}
		if (buffer[0] != 77 || buffer[1] != 90)
		{
			return false;
		}
		int num = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(60, 4));
		if (num < buffer.Length || num > fileStream.Length - 4)
		{
			return false;
		}
		fileStream.Position = num;
		Span<byte> buffer2 = stackalloc byte[4];
		try
		{
			fileStream.ReadExactly(buffer2);
		}
		catch (EndOfStreamException)
		{
			return false;
		}
		return buffer2[0] == 80 && buffer2[1] == 69 && buffer2[2] == 0 && buffer2[3] == 0;
	}

	private static bool IsRegistryFailure(Exception ex)
	{
		if (ex is SecurityException || ex is UnauthorizedAccessException || ex is IOException || ex is SystemException)
		{
			return true;
		}
		return false;
	}

	private static bool PathsEqual(string left, string right)
	{
		return string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(left)), Path.TrimEndingDirectorySeparator(Path.GetFullPath(right)), StringComparison.OrdinalIgnoreCase);
	}

	private static InstallationInfo Mismatch(string message)
	{
		return new InstallationInfo(InstallationState.RegistrationMismatch, null, null, message);
	}
}
