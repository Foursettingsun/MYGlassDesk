using System.Collections.Generic;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace GlassDesk.AppServices;

internal interface IUserDataDeletionFileSystem
{
	bool Exists(string path);

	FileAttributes GetAttributes(string path);

	IEnumerable<string> EnumerateFileSystemEntries(string path);

	void MoveDirectory(string source, string destination);

	void DeleteFileByHandle(SafeFileHandle handle, string path);

	void DeleteDirectoryByHandle(SafeFileHandle handle);
}
