using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace GlassDesk.Platform.Storage;

public sealed class UserDataLease : IDisposable
{
	private readonly FileStream _stream;

	private int _disposed;

	private UserDataLease(FileStream stream)
	{
		_stream = stream;
	}

	public static bool TryAcquireShared(string dataRoot, out UserDataLease? lease)
	{
		return TryAcquire(dataRoot, FileShare.ReadWrite, out lease);
	}

	public static bool TryAcquireExclusive(string dataRoot, out UserDataLease? lease)
	{
		return TryAcquire(dataRoot, FileShare.None, out lease);
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _disposed, 1) == 0)
		{
			_stream.Dispose();
		}
	}

	private static bool TryAcquire(string dataRoot, FileShare sharing, out UserDataLease? lease)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(dataRoot, "dataRoot");
		lease = null;
		try
		{
			string leasePath = GetLeasePath(dataRoot);
			string directoryName = Path.GetDirectoryName(leasePath);
			if (string.IsNullOrWhiteSpace(directoryName))
			{
				return false;
			}
			Directory.CreateDirectory(directoryName);
			FileStream stream = new FileStream(leasePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, sharing);
			lease = new UserDataLease(stream);
			return true;
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException || ex is ArgumentException || ex is NotSupportedException) ? true : false)
		{
			return false;
		}
	}

	private static string GetLeasePath(string dataRoot)
	{
		string text = Path.TrimEndingDirectorySeparator(Path.GetFullPath(dataRoot));
		string b = Path.TrimEndingDirectorySeparator(Path.GetPathRoot(text) ?? string.Empty);
		StringComparison comparisonType = (OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
		if (string.Equals(text, b, comparisonType))
		{
			throw new ArgumentException("数据目录不能是卷根。", "dataRoot");
		}
		string s = (OperatingSystem.IsWindows() ? text.ToUpperInvariant() : text);
		string text2 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)));
		return Path.Combine(Directory.GetParent(text)?.FullName ?? throw new ArgumentException("无法为数据目录确定同级租约位置。", "dataRoot"), ".glassdesk-data-" + text2 + ".lease");
	}
}
