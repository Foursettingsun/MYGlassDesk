using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Win32.SafeHandles;

namespace GlassDesk.Platform.Storage;

public sealed class AcceptanceWorkspace : IDisposable
{
	private struct UnicodeString
	{
		public ushort Length;

		public ushort MaximumLength;

		public nint Buffer;
	}

	private struct ObjectAttributes
	{
		public uint Length;

		public nint RootDirectory;

		public nint ObjectName;

		public uint Attributes;

		public nint SecurityDescriptor;

		public nint SecurityQualityOfService;
	}

	private struct IoStatusBlock
	{
		public nint Status;

		public nuint Information;
	}

	private struct AttributeTagInfo
	{
		public uint Attributes;

		public uint Tag;
	}

	private static class Native
	{
		[LibraryImport("ntdll.dll")]
		[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
		[SkipLocalsInit]
		internal unsafe static int NtCreateFile(out nint handle, uint access, ref ObjectAttributes attributes, out IoStatusBlock ioStatus, nint allocationSize, uint fileAttributes, uint shareAccess, uint disposition, uint options, nint eaBuffer, uint eaLength)
		{
			handle = 0;
			ioStatus = default;
			int result;
			fixed (IoStatusBlock* _ioStatus_native = &ioStatus)
			{
				fixed (ObjectAttributes* _attributes_native = &attributes)
				{
					fixed (nint* _handle_native = &handle)
					{
						result = __PInvoke(_handle_native, access, _attributes_native, _ioStatus_native, allocationSize, fileAttributes, shareAccess, disposition, options, eaBuffer, eaLength);
					}
				}
			}
			return result;
			[DllImport("ntdll.dll", EntryPoint = "NtCreateFile", ExactSpelling = true)]
			unsafe static extern int __PInvoke(nint* __handle_native, uint __access_native, ObjectAttributes* __attributes_native, IoStatusBlock* __ioStatus_native, nint __allocationSize_native, uint __fileAttributes_native, uint __shareAccess_native, uint __disposition_native, uint __options_native, nint __eaBuffer_native, uint __eaLength_native);
		}

		[DllImport("ntdll.dll", ExactSpelling = true)]
		[LibraryImport("ntdll.dll")]
		internal static extern uint RtlNtStatusToDosError(int status);

		[LibraryImport("kernel32.dll", SetLastError = true)]
		[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
		[SkipLocalsInit]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal unsafe static bool GetFileInformationByHandleEx(SafeFileHandle handle, int informationClass, out AttributeTagInfo info, uint size)
		{
			info = default;
			bool result = false;
			int num = 0;
			SafeHandleMarshaller<SafeFileHandle>.ManagedToUnmanagedIn managedToUnmanagedIn = default;
			int lastSystemError;
			try
			{
				managedToUnmanagedIn.FromManaged(handle);
				fixed (AttributeTagInfo* _info_native = &info)
				{
					nint _handle_native = managedToUnmanagedIn.ToUnmanaged();
					Marshal.SetLastSystemError(0);
					num = __PInvoke(_handle_native, informationClass, _info_native, size);
					lastSystemError = Marshal.GetLastSystemError();
				}
				result = num != 0;
			}
			finally
			{
				managedToUnmanagedIn.Free();
			}
			Marshal.SetLastPInvokeError(lastSystemError);
			return result;
			[DllImport("kernel32.dll", EntryPoint = "GetFileInformationByHandleEx", ExactSpelling = true)]
			unsafe static extern int __PInvoke(nint __handle_native, int __informationClass_native, AttributeTagInfo* __info_native, uint __size_native);
		}

		[LibraryImport("kernel32.dll", EntryPoint = "GetFinalPathNameByHandleW", SetLastError = true)]
		[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
		[SkipLocalsInit]
		internal unsafe static uint GetFinalPathNameByHandle(SafeFileHandle handle, char* path, uint length, uint flags)
		{
			uint result = 0u;
			SafeHandleMarshaller<SafeFileHandle>.ManagedToUnmanagedIn managedToUnmanagedIn = default;
			int lastSystemError;
			try
			{
				managedToUnmanagedIn.FromManaged(handle);
				nint _handle_native = managedToUnmanagedIn.ToUnmanaged();
				Marshal.SetLastSystemError(0);
				result = __PInvoke(_handle_native, path, length, flags);
				lastSystemError = Marshal.GetLastSystemError();
			}
			finally
			{
				managedToUnmanagedIn.Free();
			}
			Marshal.SetLastPInvokeError(lastSystemError);
			return result;
			[DllImport("kernel32.dll", EntryPoint = "GetFinalPathNameByHandleW", ExactSpelling = true)]
			unsafe static extern uint __PInvoke(nint __handle_native, char* __path_native, uint __length_native, uint __flags_native);
		}
	}

	private const uint ReadAttributes = 128u;

	private const uint Synchronize = 1048576u;

	private const uint OpenReparsePoint = 2097152u;

	private const uint SynchronousNonAlert = 32u;

	private const int NameNotFound = -1073741772;

	private readonly Dictionary<string, SafeFileHandle> _directories = new Dictionary<string, SafeFileHandle>(StringComparer.OrdinalIgnoreCase);

	private readonly Dictionary<string, Stream> _outputs = new Dictionary<string, Stream>(StringComparer.Ordinal);

	private bool _disposed;

	public string DataRoot { get; }

	private AcceptanceWorkspace(string dataRoot)
	{
		DataRoot = dataRoot;
	}

	public static AcceptanceWorkspace Create(AcceptanceRequest request)
	{
		ArgumentNullException.ThrowIfNull(request, "request");
		if (!OperatingSystem.IsWindows())
		{
			throw new PlatformNotSupportedException("验收目录只支持 Windows。");
		}
		string text = CanonicalPath(request.ProtectedDataRoot);
		string text2 = CanonicalPath(request.NewDataRoot);
		if (request.Outputs == null)
		{
			throw new IOException("缺少验收输出清单。");
		}
		AcceptanceOutput[] array = request.Outputs.ToArray();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		List<string> list = new List<string> { text2 };
		AcceptanceOutput[] array2 = array;
		foreach (AcceptanceOutput acceptanceOutput in array2)
		{
			if ((object)acceptanceOutput == null || string.IsNullOrWhiteSpace(acceptanceOutput.Name) || !hashSet.Add(acceptanceOutput.Name))
			{
				throw new IOException("验收输出名称为空或重复。");
			}
			list.Add(CanonicalPath(acceptanceOutput.Path));
		}
		for (int j = 0; j < list.Count; j++)
		{
			if (Overlaps(list[j], text))
			{
				throw new IOException("验收路径与真实用户数据目录重叠。");
			}
			for (int k = 0; k < j; k++)
			{
				if (Overlaps(list[j], list[k]))
				{
					throw new IOException("验收数据与输出路径互相重叠。");
				}
			}
		}
		AcceptanceWorkspace acceptanceWorkspace = new AcceptanceWorkspace(text2);
		try
		{
			acceptanceWorkspace.PinDirectoryChain(text, allowMissing: true);
			foreach (string item in list)
			{
				using SafeFileHandle safeFileHandle = OpenRelative(acceptanceWorkspace.PinDirectoryChain(Path.GetDirectoryName(item), allowMissing: false), Path.GetFileName(item), create: false, directory: false, probe: true);
				if (safeFileHandle != null)
				{
					throw new IOException("验收目标已经存在，拒绝复用或覆盖：" + item);
				}
			}
			SafeFileHandle safeFileHandle2 = OpenRelative(acceptanceWorkspace._directories[Path.GetDirectoryName(text2)], Path.GetFileName(text2), create: true, directory: true);
			acceptanceWorkspace._directories.Add(text2, safeFileHandle2);
			VerifyDirectory(safeFileHandle2, text2);
			for (int l = 0; l < array.Length; l++)
			{
				string path = list[l + 1];
				SafeFileHandle safeFileHandle3 = OpenRelative(acceptanceWorkspace._directories[Path.GetDirectoryName(path)], Path.GetFileName(path), create: true, directory: false);
				try
				{
					acceptanceWorkspace._outputs.Add(array[l].Name, new FileStream(safeFileHandle3, FileAccess.Write));
				}
				catch
				{
					safeFileHandle3.Dispose();
					throw;
				}
			}
			return acceptanceWorkspace;
		}
		catch
		{
			acceptanceWorkspace.Dispose();
			throw;
		}
	}

	public Stream GetOutput(string name)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		return _outputs[name];
	}

	public string CreateChildDataRoot(string name)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		bool flag = string.IsNullOrWhiteSpace(name);
		if (!flag)
		{
			bool flag2 = ((name == "." || name == "..") ? true : false);
			flag = flag2;
		}
		if (flag || Path.IsPathRooted(name) || name.Contains(Path.DirectorySeparatorChar) || name.Contains(Path.AltDirectorySeparatorChar))
		{
			throw new IOException("验收子数据根必须是单个目录名。");
		}
		string text = CanonicalPath(Path.Combine(DataRoot, name));
		if (!Path.GetDirectoryName(text).Equals(DataRoot, StringComparison.OrdinalIgnoreCase) || _directories.ContainsKey(text))
		{
			throw new IOException("验收子数据根必须是本工作区内的全新直属目录。");
		}
		SafeFileHandle safeFileHandle = OpenRelative(_directories[DataRoot], name, create: true, directory: true);
		try
		{
			VerifyDirectory(safeFileHandle, text);
			_directories.Add(text, safeFileHandle);
			return text;
		}
		catch
		{
			safeFileHandle.Dispose();
			throw;
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		Exception ex = null;
		foreach (Stream value in _outputs.Values)
		{
			try
			{
				value.Dispose();
			}
			catch (Exception ex2) when ((ex2 is IOException || ex2 is UnauthorizedAccessException) ? true : false)
			{
				if (ex2 == null)
				{
					ex = ex2;
				}
			}
		}
		foreach (SafeFileHandle item in _directories.Values.Reverse())
		{
			item.Dispose();
		}
		if (ex == null)
		{
			return;
		}
		throw new IOException("验收输出关闭失败。", ex);
	}

	private SafeFileHandle? PinDirectoryChain(string path, bool allowMissing)
	{
		if (_directories.TryGetValue(path, out SafeFileHandle value))
		{
			return value;
		}
		string pathRoot = Path.GetPathRoot(path);
		if (!_directories.TryGetValue(pathRoot, out SafeFileHandle value2))
		{
			value2 = OpenRelative(null, "\\??\\" + pathRoot, create: false, directory: true);
			_directories.Add(pathRoot, value2);
			VerifyDirectory(value2, pathRoot);
		}
		string text = pathRoot;
		string[] array = path.Substring(pathRoot.Length).Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
		foreach (string text2 in array)
		{
			text = Path.Combine(text, text2);
			if (_directories.TryGetValue(text, out SafeFileHandle value3))
			{
				value2 = value3;
				continue;
			}
			SafeFileHandle safeFileHandle = OpenRelative(value2, text2, create: false, directory: true, allowMissing);
			if (safeFileHandle == null)
			{
				return null;
			}
			_directories.Add(text, safeFileHandle);
			VerifyDirectory(safeFileHandle, text);
			value2 = safeFileHandle;
		}
		return value2;
	}

	private static string CanonicalPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path) || path.Length > 240 || path.Length < 4 || !char.IsAsciiLetter(path[0]) || path[1] != ':' || path[2] != '\\' || path.Contains('/'))
		{
			throw new IOException("验收路径必须是本地固定卷上的规范绝对路径。");
		}
		string text = path.TrimEnd('\\');
		if (text.Length == 2 || !string.Equals(Path.GetFullPath(text), text, StringComparison.OrdinalIgnoreCase))
		{
			throw new IOException("拒绝卷根或非规范验收路径。");
		}
		string[] array = text.Substring(3).Split('\\');
		foreach (string text2 in array)
		{
			string text3 = text2.Split('.')[0];
			if (text2.Length == 0 || text2.EndsWith('.') || text2.EndsWith(' ') || text2.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || text3.Equals("CON", StringComparison.OrdinalIgnoreCase) || text3.Equals("PRN", StringComparison.OrdinalIgnoreCase) || text3.Equals("AUX", StringComparison.OrdinalIgnoreCase) || text3.Equals("NUL", StringComparison.OrdinalIgnoreCase) || (text3.Length == 4 && (text3.StartsWith("COM", StringComparison.OrdinalIgnoreCase) || text3.StartsWith("LPT", StringComparison.OrdinalIgnoreCase)) && char.IsDigit(text3[3])))
			{
				throw new IOException("验收路径包含不安全的名称。");
			}
		}
		DriveInfo driveInfo = new DriveInfo(text.Substring(0, 3));
		if (driveInfo.DriveType != DriveType.Fixed || !driveInfo.DriveFormat.Equals("NTFS", StringComparison.OrdinalIgnoreCase))
		{
			throw new IOException("验收目录只支持本地固定 NTFS 卷。");
		}
		return text;
	}

	private static bool Overlaps(string left, string right)
	{
		if (!left.Equals(right, StringComparison.OrdinalIgnoreCase) && !left.StartsWith(right + "\\", StringComparison.OrdinalIgnoreCase))
		{
			return right.StartsWith(left + "\\", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private unsafe static SafeFileHandle? OpenRelative(SafeFileHandle? parent, string name, bool create, bool directory, bool probe = false)
	{
		fixed (char* buffer = name)
		{
			UnicodeString unicodeString = new UnicodeString
			{
				Length = checked((ushort)(name.Length * 2)),
				MaximumLength = checked((ushort)((name.Length + 1) * 2)),
				Buffer = (nint)buffer
			};
			ObjectAttributes attributes = new ObjectAttributes
			{
				Length = (uint)sizeof(ObjectAttributes),
				RootDirectory = (parent?.DangerousGetHandle() ?? IntPtr.Zero),
				ObjectName = (nint)(&unicodeString),
				Attributes = 64u
			};
			uint access = (uint)(0x100000 | ((create && !directory) ? 1073741824 : (0x80 | ((directory && parent != null) ? 1 : 0))));
			uint shareAccess;
			if (parent == null)
			{
				shareAccess = 7u;
			}
			else
			{
				shareAccess = (directory ? 3u : (probe ? 7u : 0u));
			}
			uint options = (uint)(0x200020 | (directory ? 1 : (create ? 64 : 0)));
			int num = Native.NtCreateFile(out var handle, access, ref attributes, out var _, IntPtr.Zero, 0u, shareAccess, (!create) ? 1u : 2u, options, IntPtr.Zero, 0u);
			GC.KeepAlive(parent);
			if (num < 0)
			{
				if (probe && num == -1073741772)
				{
					return null;
				}
				uint error = Native.RtlNtStatusToDosError(num);
				throw new IOException("无法安全取得验收路径对象：" + name, new Win32Exception((int)error));
			}
			return new SafeFileHandle(handle, ownsHandle: true);
		}
	}

	private unsafe static void VerifyDirectory(SafeFileHandle handle, string expectedPath)
	{
		if (!Native.GetFileInformationByHandleEx(handle, 9, out var info, (uint)sizeof(AttributeTagInfo)))
		{
			throw new IOException("无法验证验收目录属性。", new Win32Exception(Marshal.GetLastPInvokeError()));
		}
		if ((info.Attributes & 0x400) != 0 || (info.Attributes & 0x10) == 0)
		{
			throw new IOException("拒绝重解析点或非目录的验收祖先。");
		}
		char* ptr = stackalloc char[1024];
		uint finalPathNameByHandle = Native.GetFinalPathNameByHandle(handle, ptr, 1024u, 0u);
		if (finalPathNameByHandle == 0 || finalPathNameByHandle >= 1024)
		{
			throw new IOException("无法验证验收目录最终路径。");
		}
		string text = new string(ptr, 0, (int)finalPathNameByHandle);
		if (!text.StartsWith("\\\\?\\", StringComparison.Ordinal) || !text.Substring(4).TrimEnd('\\').Equals(expectedPath.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
		{
			throw new IOException("验收目录最终路径与请求不一致，拒绝别名或重定向。");
		}
	}
}
