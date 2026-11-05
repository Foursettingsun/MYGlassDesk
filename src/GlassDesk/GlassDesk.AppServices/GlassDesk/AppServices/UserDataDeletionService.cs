using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security;
using GlassDesk.Core.Storage;
using GlassDesk.Platform.Storage;
using Microsoft.Win32.SafeHandles;

namespace GlassDesk.AppServices;

public sealed class UserDataDeletionService
{
	private readonly record struct DirectoryIdentity(uint VolumeSerial, uint FileIndexHigh, uint FileIndexLow);

	private sealed class DirectoryTraversal(SafeFileHandle handle, string[] entries, DirectoryIdentity identity) : IDisposable
	{
		public SafeFileHandle Handle { get; } = handle;

		public string[] Entries { get; } = entries;

		public DirectoryIdentity Identity { get; } = identity;

		public int NextEntry { get; set; }

		public void Dispose()
		{
			Handle.Dispose();
		}
	}

	private sealed class UnsafeDataPathException : IOException
	{
		public UnsafeDataPathException(string message)
			: base(message)
		{
		}
	}

	private struct ByHandleFileInformation
	{
		public uint FileAttributes;

		public FILETIME CreationTime;

		public FILETIME LastAccessTime;

		public FILETIME LastWriteTime;

		public uint VolumeSerialNumber;

		public uint FileSizeHigh;

		public uint FileSizeLow;

		public uint NumberOfLinks;

		public uint FileIndexHigh;

		public uint FileIndexLow;
	}

	private struct FileDispositionInformation
	{
		public byte DeleteFile;
	}

	private struct FileBasicInformation
	{
		public long CreationTime;

		public long LastAccessTime;

		public long LastWriteTime;

		public long ChangeTime;

		public uint FileAttributes;

		public uint Reserved;
	}

	private readonly string _dataRoot;

	private readonly string _expectedDataRoot;

	private readonly IReadOnlyList<string> _protectedRoots;

	private readonly IUserDataDeletionFileSystem _fileSystem;

	private readonly string? _portableInstallationDirectory;

	internal UserDataDeletionService(string dataRoot, string expectedDataRoot, IReadOnlyList<string> protectedRoots, IUserDataDeletionFileSystem? fileSystem = null, string? portableInstallationDirectory = null)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(dataRoot, "dataRoot");
		ArgumentException.ThrowIfNullOrWhiteSpace(expectedDataRoot, "expectedDataRoot");
		ArgumentNullException.ThrowIfNull(protectedRoots, "protectedRoots");
		_dataRoot = Path.GetFullPath(dataRoot);
		_expectedDataRoot = Path.GetFullPath(expectedDataRoot);
		_protectedRoots = protectedRoots.Select(Path.GetFullPath).ToArray();
		_fileSystem = fileSystem ?? new UserDataDeletionFileSystem();
		if (!string.IsNullOrWhiteSpace(portableInstallationDirectory) && PathsEqual(_dataRoot, Path.Combine(portableInstallationDirectory, "Data")) && PathsEqual(_dataRoot, _expectedDataRoot))
		{
			_portableInstallationDirectory = Path.GetFullPath(portableInstallationDirectory);
		}
	}

	public static UserDataDeletionService ForCurrentUser(string? installationDirectory = null)
	{
		AppPaths paths = AppPaths.ForCurrentUser();
		List<string> list = new List<string>();
		if (!string.IsNullOrWhiteSpace(installationDirectory))
		{
			list.Add(installationDirectory);
		}
		string processPath = Environment.ProcessPath;
		if (!string.IsNullOrWhiteSpace(processPath))
		{
			string directoryName = Path.GetDirectoryName(processPath);
			if (!string.IsNullOrWhiteSpace(directoryName))
			{
				list.Add(directoryName);
			}
		}
		return CreateForPaths(paths, AppContext.BaseDirectory, list);
	}

	internal static UserDataDeletionService CreateForPaths(AppPaths paths, string applicationDirectory, IReadOnlyList<string> protectedRoots)
	{
		string rootDirectory = paths.RootDirectory;
		string rootDirectory2 = paths.RootDirectory;
		int num = 0;
		string[] array = new string[1 + protectedRoots.Count];
		foreach (string protectedRoot in protectedRoots)
		{
			array[num] = protectedRoot;
			num++;
		}
		array[num] = applicationDirectory;
		return new UserDataDeletionService(rootDirectory, rootDirectory2, new _003C_003Ez__ReadOnlyArray<string>(array), null, applicationDirectory);
	}

	public UserDataDeletionResult DeleteAll(bool confirmed)
	{
		if (!confirmed)
		{
			return new UserDataDeletionResult(UserDataDeletionStatus.Cancelled);
		}
		if (!IsConfiguredPathSafe(out string error))
		{
			return new UserDataDeletionResult(UserDataDeletionStatus.UnsafePath, 0, null, error);
		}
		if (!UserDataLease.TryAcquireExclusive(_dataRoot, out UserDataLease lease) || lease == null)
		{
			return new UserDataDeletionResult(UserDataDeletionStatus.Busy, 0, null, "GlassDesk 数据仍被运行中的实例使用，或当前文件夹无法取得独占租约。请先退出其它 GlassDesk 实例后重试。");
		}
		using (lease)
		{
			bool flag = false;
			string text = null;
			int deletedEntries = 0;
			try
			{
				if (!_fileSystem.Exists(_dataRoot))
				{
					return new UserDataDeletionResult(UserDataDeletionStatus.NotFound);
				}
				ValidateDirectoryTree(_dataRoot);
				ValidatePhysicalIdentity(_dataRoot, _expectedDataRoot);
				ValidateProtectedRoots(_dataRoot);
				using SafeFileHandle handle = OpenDirectoryIdentity(_dataRoot);
				DirectoryIdentity directoryIdentity = ReadDirectoryIdentity(handle);
				string finalPath = GetFinalPath(handle);
				string right = ResolveFinalPath(_expectedDataRoot);
				if (!PathsEqual(finalPath, right))
				{
					throw new UnsafeDataPathException("数据目录的物理路径与当前用户数据目录不一致。");
				}
				text = CreateQuarantinePath();
				_fileSystem.MoveDirectory(_dataRoot, text);
				flag = true;
				using SafeFileHandle handle2 = OpenDirectoryIdentity(text);
				if (ReadDirectoryIdentity(handle2) != directoryIdentity || !PathsEqual(GetFinalPath(handle2), text))
				{
					throw new UnsafeDataPathException("改名后的目录身份与已核验的数据目录不一致。");
				}
				ValidateDirectoryTree(text);
				ValidateProtectedRoots(text, text);
				DeleteDirectoryContents(text, directoryIdentity, ref deletedEntries);
				return new UserDataDeletionResult(UserDataDeletionStatus.Deleted, deletedEntries);
			}
			catch (UnsafeDataPathException ex)
			{
				return flag ? new UserDataDeletionResult(UserDataDeletionStatus.Residual, deletedEntries, text, ex.Message) : new UserDataDeletionResult(UserDataDeletionStatus.UnsafePath, 0, null, ex.Message);
			}
			catch (Exception ex2) when (IsFileSystemFailure(ex2))
			{
				return flag ? new UserDataDeletionResult(UserDataDeletionStatus.Residual, deletedEntries, text, ex2.Message) : new UserDataDeletionResult(UserDataDeletionStatus.Failed, 0, null, ex2.Message);
			}
		}
	}

	private bool IsConfiguredPathSafe(out string? error)
	{
		error = null;
		if (!PathsEqual(_dataRoot, _expectedDataRoot))
		{
			error = "拒绝删除：目标目录不是当前用户配置的数据目录。";
			return false;
		}
		string pathRoot = Path.GetPathRoot(_dataRoot);
		if (string.IsNullOrWhiteSpace(pathRoot) || PathsEqual(_dataRoot, pathRoot) || IsSameOrAncestor(_dataRoot, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)))
		{
			error = "拒绝删除：目标目录不能是磁盘根目录或用户配置文件根目录。";
			return false;
		}
		foreach (string protectedRoot in _protectedRoots)
		{
			if (OverlapsProtectedRoot(_dataRoot, protectedRoot))
			{
				error = "拒绝删除：目标目录与程序安装目录或受保护路径重叠。";
				return false;
			}
		}
		return true;
	}

	private void ValidateProtectedRoots(string targetRoot, string? quarantinePath = null)
	{
		foreach (string protectedRoot2 in _protectedRoots)
		{
			if (OverlapsProtectedRoot(targetRoot, protectedRoot2, quarantinePath))
			{
				throw new UnsafeDataPathException("数据目录与程序安装目录或受保护路径重叠。");
			}
			if (_fileSystem.Exists(protectedRoot2))
			{
				string protectedRoot = ResolveFinalPath(protectedRoot2);
				if (OverlapsProtectedRoot(ResolveFinalPath(targetRoot), protectedRoot, quarantinePath))
				{
					throw new UnsafeDataPathException("数据目录与受保护路径的物理位置重叠。");
				}
			}
		}
	}

	private bool OverlapsProtectedRoot(string targetRoot, string protectedRoot, string? quarantinePath = null)
	{
		if (!PathsOverlap(targetRoot, protectedRoot))
		{
			return false;
		}
		if (_portableInstallationDirectory != null && PathsEqual(protectedRoot, _portableInstallationDirectory) && PathsEqual(Path.GetDirectoryName(targetRoot) ?? targetRoot, _portableInstallationDirectory))
		{
			if (!PathsEqual(targetRoot, _dataRoot))
			{
				if (quarantinePath != null)
				{
					return !PathsEqual(targetRoot, quarantinePath);
				}
				return true;
			}
			return false;
		}
		return true;
	}

	private static void ValidatePhysicalIdentity(string actualPath, string expectedPath)
	{
		RejectReparseAncestors(actualPath);
		RejectReparseAncestors(expectedPath);
		if (!PathsEqual(ResolveFinalPath(actualPath), ResolveFinalPath(expectedPath)))
		{
			throw new UnsafeDataPathException("数据目录的最终物理路径与配置路径不一致。");
		}
	}

	private void ValidateDirectoryTree(string root)
	{
		Stack<(string, bool)> stack = new Stack<(string, bool)>();
		stack.Push((root, true));
		while (stack.Count > 0)
		{
			(string, bool) tuple = stack.Pop();
			string item = tuple.Item1;
			bool item2 = tuple.Item2;
			FileAttributes attributes = _fileSystem.GetAttributes(item);
			if ((attributes & FileAttributes.ReparsePoint) != 0)
			{
				throw new UnsafeDataPathException("检测到目录联接或符号链接，已拒绝删除：" + item);
			}
			if (item2 && (attributes & FileAttributes.Directory) == 0)
			{
				throw new IOException("数据路径不是目录。");
			}
			if (item2)
			{
				RejectReparseAncestors(item);
			}
			if ((attributes & FileAttributes.Directory) == 0)
			{
				continue;
			}
			using DirectoryTraversal directoryTraversal = OpenDirectoryForDeletion(item);
			string[] entries = directoryTraversal.Entries;
			foreach (string item3 in entries)
			{
				stack.Push((item3, false));
			}
		}
	}

	private void DeleteDirectoryContents(string directory, DirectoryIdentity expectedRootIdentity, ref int deletedEntries)
	{
		Stack<DirectoryTraversal> stack = new Stack<DirectoryTraversal>();
		stack.Push(OpenDirectoryForDeletion(directory, expectedRootIdentity));
		try
		{
			while (stack.Count > 0)
			{
				DirectoryTraversal directoryTraversal = stack.Peek();
				if (directoryTraversal.NextEntry == directoryTraversal.Entries.Length)
				{
					_fileSystem.DeleteDirectoryByHandle(directoryTraversal.Handle);
					stack.Pop().Dispose();
					deletedEntries++;
					continue;
				}
				string text = directoryTraversal.Entries[directoryTraversal.NextEntry++];
				FileAttributes attributes = _fileSystem.GetAttributes(text);
				if ((attributes & FileAttributes.ReparsePoint) != 0)
				{
					throw new UnsafeDataPathException("隔离目录中新增了联接或符号链接，已停止删除：" + text);
				}
				if ((attributes & FileAttributes.Directory) != 0)
				{
					stack.Push(OpenDirectoryForDeletion(text));
					continue;
				}
				using SafeFileHandle handle = OpenFileForDeletion(text);
				_fileSystem.DeleteFileByHandle(handle, text);
				deletedEntries++;
			}
		}
		finally
		{
			while (stack.Count > 0)
			{
				stack.Pop().Dispose();
			}
		}
	}

	private DirectoryTraversal OpenDirectoryForDeletion(string path, DirectoryIdentity? expectedIdentity = null)
	{
		SafeFileHandle safeFileHandle = CreateFileW(path, 65664u, 3u, IntPtr.Zero, 3u, 35651584u, IntPtr.Zero);
		if (safeFileHandle.IsInvalid)
		{
			int lastWin32Error = Marshal.GetLastWin32Error();
			safeFileHandle.Dispose();
			throw new Win32Exception(lastWin32Error, "无法锁定并核验待删除目录：" + path);
		}
		try
		{
			ByHandleFileInformation info = ReadFileInformation(safeFileHandle);
			uint fileAttributes = info.FileAttributes;
			if ((fileAttributes & 0x400) != 0)
			{
				throw new UnsafeDataPathException("检测到待删除目录被替换为联接或符号链接：" + path);
			}
			if ((fileAttributes & 0x10) == 0)
			{
				throw new IOException("待删除路径不再是目录：" + path);
			}
			if (!PathsEqual(GetFinalPath(safeFileHandle), path))
			{
				throw new UnsafeDataPathException("待删除目录的物理路径与隔离路径不一致：" + path);
			}
			DirectoryIdentity directoryIdentity = ToDirectoryIdentity(info);
			if (expectedIdentity.HasValue)
			{
				DirectoryIdentity valueOrDefault = expectedIdentity.GetValueOrDefault();
				if (directoryIdentity != valueOrDefault)
				{
					throw new UnsafeDataPathException("待删除目录已被替换，身份与改名时核验的目录不一致：" + path);
				}
			}
			string[] entries = _fileSystem.EnumerateFileSystemEntries(path).ToArray();
			return new DirectoryTraversal(safeFileHandle, entries, directoryIdentity);
		}
		catch
		{
			safeFileHandle.Dispose();
			throw;
		}
	}

	private static SafeFileHandle OpenFileForDeletion(string path)
	{
		SafeFileHandle safeFileHandle = CreateFileW(path, 65920u, 0u, IntPtr.Zero, 3u, 2097152u, IntPtr.Zero);
		if (safeFileHandle.IsInvalid)
		{
			int lastWin32Error = Marshal.GetLastWin32Error();
			safeFileHandle.Dispose();
			throw new Win32Exception(lastWin32Error, "无法锁定并核验待删除文件：" + path);
		}
		try
		{
			ByHandleFileInformation byHandleFileInformation = ReadFileInformation(safeFileHandle);
			uint fileAttributes = byHandleFileInformation.FileAttributes;
			if ((fileAttributes & 0x400) != 0)
			{
				throw new UnsafeDataPathException("隔离目录中新增了联接或符号链接，已停止删除：" + path);
			}
			if ((fileAttributes & 0x10) != 0)
			{
				throw new UnsafeDataPathException("待删除文件路径已被替换为目录：" + path);
			}
			if (byHandleFileInformation.NumberOfLinks != 1)
			{
				throw new UnsafeDataPathException("检测到硬链接文件，为避免影响数据目录外的同一文件，已停止删除：" + path);
			}
			if (!PathsEqual(GetFinalPath(safeFileHandle), path))
			{
				throw new UnsafeDataPathException("待删除文件的物理路径与隔离路径不一致：" + path);
			}
			return safeFileHandle;
		}
		catch
		{
			safeFileHandle.Dispose();
			throw;
		}
	}

	private string CreateQuarantinePath()
	{
		string path = Path.GetDirectoryName(_dataRoot) ?? throw new UnsafeDataPathException("无法确定数据目录的同级隔离位置。");
		for (int i = 0; i < 8; i++)
		{
			string text = Path.Combine(path, "GlassDesk.deleted-" + Guid.NewGuid().ToString("N"));
			if (!_fileSystem.Exists(text))
			{
				return text;
			}
		}
		throw new IOException("无法为本次数据删除分配唯一隔离目录。");
	}

	private static string ResolveFinalPath(string path)
	{
		using SafeFileHandle handle = OpenDirectoryIdentity(path);
		return GetFinalPath(handle);
	}

	private static void RejectReparseAncestors(string path)
	{
		string text = Path.GetFullPath(path);
		while (!string.IsNullOrWhiteSpace(text))
		{
			try
			{
				if ((File.GetAttributes(text) & FileAttributes.ReparsePoint) != 0)
				{
					throw new UnsafeDataPathException("路径链中包含联接或符号链接：" + text);
				}
			}
			catch (FileNotFoundException) when (!PathsEqual(text, path))
			{
			}
			string text2 = Directory.GetParent(text)?.FullName;
			if (text2 != null && !PathsEqual(text2, text))
			{
				text = text2;
				continue;
			}
			break;
		}
	}

	private static SafeFileHandle OpenDirectoryIdentity(string path)
	{
		SafeFileHandle safeFileHandle = CreateFileW(path, 128u, 7u, IntPtr.Zero, 3u, 33554432u, IntPtr.Zero);
		if (safeFileHandle.IsInvalid)
		{
			int lastWin32Error = Marshal.GetLastWin32Error();
			safeFileHandle.Dispose();
			throw new Win32Exception(lastWin32Error, "无法打开并核验目录：" + path);
		}
		return safeFileHandle;
	}

	private static DirectoryIdentity ReadDirectoryIdentity(SafeFileHandle handle)
	{
		return ToDirectoryIdentity(ReadFileInformation(handle));
	}

	private static DirectoryIdentity ToDirectoryIdentity(ByHandleFileInformation info)
	{
		return new DirectoryIdentity(info.VolumeSerialNumber, info.FileIndexHigh, info.FileIndexLow);
	}

	private static ByHandleFileInformation ReadFileInformation(SafeFileHandle handle)
	{
		if (!GetFileInformationByHandle(handle, out var fileInformation))
		{
			throw new Win32Exception(Marshal.GetLastWin32Error(), "无法读取文件或目录身份。");
		}
		return fileInformation;
	}

	internal static void MarkDirectoryForDeletion(SafeFileHandle handle)
	{
		FileDispositionInformation fileInformation = new FileDispositionInformation
		{
			DeleteFile = 1
		};
		if (!SetFileInformationByHandle(handle, 4, ref fileInformation, (uint)Marshal.SizeOf<FileDispositionInformation>()))
		{
			throw new Win32Exception(Marshal.GetLastWin32Error(), "无法通过已核验的目录句柄删除目录。");
		}
	}

	internal static void DeleteFileByHandle(SafeFileHandle handle, string path)
	{
		ByHandleFileInformation byHandleFileInformation = ReadFileInformation(handle);
		if (byHandleFileInformation.NumberOfLinks != 1)
		{
			throw new UnsafeDataPathException("待删除文件在锁定后出现了硬链接，已停止删除：" + path);
		}
		if ((byHandleFileInformation.FileAttributes & 1) != 0)
		{
			uint num = byHandleFileInformation.FileAttributes & 0xFFFFFFFEu;
			if (num == 0)
			{
				num = 128u;
			}
			FileBasicInformation fileInformation = new FileBasicInformation
			{
				CreationTime = -1L,
				LastAccessTime = -1L,
				LastWriteTime = -1L,
				ChangeTime = -1L,
				FileAttributes = num
			};
			if (!SetFileInformationByHandle(handle, 0, ref fileInformation, (uint)Marshal.SizeOf<FileBasicInformation>()))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error(), "无法清除待删除文件的只读属性：" + path);
			}
		}
		FileDispositionInformation fileInformation2 = new FileDispositionInformation
		{
			DeleteFile = 1
		};
		if (!SetFileInformationByHandle(handle, 4, ref fileInformation2, (uint)Marshal.SizeOf<FileDispositionInformation>()))
		{
			throw new Win32Exception(Marshal.GetLastWin32Error(), "无法通过已核验的文件句柄删除文件：" + path);
		}
	}

	private static string GetFinalPath(SafeFileHandle handle)
	{
		char[] array = new char[32768];
		uint finalPathNameByHandleW = GetFinalPathNameByHandleW(handle, array, (uint)array.Length, 0u);
		if (finalPathNameByHandleW == 0 || finalPathNameByHandleW >= array.Length)
		{
			throw new Win32Exception(Marshal.GetLastWin32Error(), "无法核验目录的最终物理路径。");
		}
		string text = new string(array, 0, checked((int)finalPathNameByHandleW));
		if (text.StartsWith("\\\\?\\UNC\\", StringComparison.OrdinalIgnoreCase))
		{
			text = "\\\\" + text.Substring(8);
		}
		else if (text.StartsWith("\\\\?\\", StringComparison.OrdinalIgnoreCase))
		{
			text = text.Substring(4);
		}
		return Path.TrimEndingDirectorySeparator(Path.GetFullPath(text));
	}

	private static bool IsFileSystemFailure(Exception ex)
	{
		if (ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException || ex is ArgumentException || ex is NotSupportedException || ex is Win32Exception)
		{
			return true;
		}
		return false;
	}

	private static bool PathsEqual(string left, string right)
	{
		return string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(left)), Path.TrimEndingDirectorySeparator(Path.GetFullPath(right)), OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
	}

	private static bool PathsOverlap(string left, string right)
	{
		if (!IsSameOrAncestor(left, right))
		{
			return IsSameOrAncestor(right, left);
		}
		return true;
	}

	private static bool IsSameOrAncestor(string possibleAncestor, string child)
	{
		if (string.IsNullOrWhiteSpace(child))
		{
			return false;
		}
		string text = Path.TrimEndingDirectorySeparator(Path.GetFullPath(possibleAncestor));
		string text2 = Path.TrimEndingDirectorySeparator(Path.GetFullPath(child));
		StringComparison comparisonType = (OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
		if (string.Equals(text, text2, comparisonType))
		{
			return true;
		}
		return text2.StartsWith(text + Path.DirectorySeparatorChar, comparisonType);
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern SafeFileHandle CreateFileW(string fileName, uint desiredAccess, uint shareMode, nint securityAttributes, uint creationDisposition, uint flagsAndAttributes, nint templateFile);

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetFileInformationByHandle(SafeFileHandle file, out ByHandleFileInformation fileInformation);

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SetFileInformationByHandle(SafeFileHandle file, int fileInformationClass, ref FileDispositionInformation fileInformation, uint bufferSize);

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SetFileInformationByHandle(SafeFileHandle file, int fileInformationClass, ref FileBasicInformation fileInformation, uint bufferSize);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern uint GetFinalPathNameByHandleW(SafeFileHandle file, [Out] char[] filePath, uint filePathSize, uint flags);
}
