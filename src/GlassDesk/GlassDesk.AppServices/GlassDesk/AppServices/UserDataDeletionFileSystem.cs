using System.Collections.Generic;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace GlassDesk.AppServices;

internal sealed class UserDataDeletionFileSystem : IUserDataDeletionFileSystem
{
	public bool Exists(string path)
	{
		if (!Directory.Exists(path))
		{
			return File.Exists(path);
		}
		return true;
	}

	public FileAttributes GetAttributes(string path)
	{
		return File.GetAttributes(path);
	}

	public IEnumerable<string> EnumerateFileSystemEntries(string path)
	{
		return Directory.EnumerateFileSystemEntries(path);
	}

	public void MoveDirectory(string source, string destination)
	{
		Directory.Move(source, destination);
	}

	public void DeleteFileByHandle(SafeFileHandle handle, string path)
	{
		UserDataDeletionService.DeleteFileByHandle(handle, path);
	}

	public void DeleteDirectoryByHandle(SafeFileHandle handle)
	{
		UserDataDeletionService.MarkDirectoryForDeletion(handle);
	}
}
