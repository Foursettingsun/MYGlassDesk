using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace GlassDesk.Platform.Interop;

internal static class Shell32
{
	internal struct ShellExecuteInfo
	{
		internal int Size;

		internal uint Mask;

		internal nint Window;

		internal nint Verb;

		internal nint File;

		internal nint Parameters;

		internal nint Directory;

		internal int Show;

		internal nint InstApp;

		internal nint IdList;

		internal nint ClassName;

		internal nint ClassKey;

		internal uint HotKey;

		internal nint IconOrMonitor;

		internal nint Process;
	}

	internal struct ShellFileInfo
	{
		internal nint Icon;

		internal int IconIndex;

		internal uint Attributes;

		internal unsafe fixed char DisplayName[260];

		internal unsafe fixed char TypeName[80];
	}

	internal struct AppBarData
	{
		internal uint Size;

		internal nint Window;

		internal uint CallbackMessage;

		internal uint Edge;

		internal Rect Rectangle;

		internal nint Parameter;
	}

	internal struct NotifyIconData
	{
		internal int Size;

		internal nint WindowHandle;

		internal int Id;

		internal int Flags;

		internal int CallbackMessage;

		internal nint IconHandle;

		internal unsafe fixed char Tip[128];

		internal int State;

		internal int StateMask;

		internal unsafe fixed char Info[256];

		internal int VersionOrTimeout;

		internal unsafe fixed char InfoTitle[64];

		internal int InfoFlags;

		internal Guid ItemGuid;

		internal nint BalloonIconHandle;

		internal unsafe static void SetText(char* destination, int capacity, string? value)
		{
			ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1, "capacity");
			if (string.IsNullOrEmpty(value))
			{
				*destination = '\0';
				return;
			}
			int num = Math.Min(value.Length, capacity - 1);
			for (int i = 0; i < num; i++)
			{
				destination[i] = value[i];
			}
			destination[num] = '\0';
		}
	}

	internal const int WmTrayCallback = 2048;

	internal const int NifMessage = 1;

	internal const int NifIcon = 2;

	internal const int NifTip = 4;

	internal const int NimAdd = 0;

	internal const int NimModify = 1;

	internal const int NimDelete = 2;

	internal const int NimSetVersion = 4;

	internal const int WmLButtonUp = 514;

	internal const int WmRButtonUp = 517;

	internal const int WmLButtonDblClk = 515;

	internal const int NotifyIconVersion3 = 3;

	internal const int TipLength = 128;

	internal const uint AbmGetState = 4u;

	internal const uint AbsAutoHide = 1u;

	internal const uint AbsAlwaysOnTop = 2u;

	internal static readonly Guid FolderIdPrograms = new Guid("a77f5d77-2e2b-44c3-a6a2-aba601054a51");

	internal static readonly Guid FolderIdCommonPrograms = new Guid("0139d44e-6afe-49f2-8690-3dafcae6ffb8");

	internal const uint ShgfiIcon = 256u;

	internal const uint ShgfiLargeIcon = 0u;

	internal const uint ShgfiUseFileAttributes = 16u;

	internal const uint ShgfiShellIconSize = 0u;

	internal const uint SigdnUrl = 2147909632u;

	internal const uint SigdnDesktopAbsoluteParsing = 2147647488u;

	internal const uint SeeMaskNoCloseProcess = 64u;

	internal const uint SeeMaskFlagNoUi = 1024u;

	internal const int SeErrSuccess = 0;

	internal const int SeErrFileNotFound = 2;

	internal const int SeErrNoAssociation = 31;

	internal const int SeErrAccessDenied = 5;

	internal const int SeErrCancelled = 1223;

	[LibraryImport("shell32.dll", EntryPoint = "Shell_NotifyIconW")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal unsafe static bool Shell_NotifyIcon(int message, ref NotifyIconData data)
	{
		int num;
		fixed (NotifyIconData* _data_native = &data)
		{
			num = __PInvoke(message, _data_native);
		}
		return num != 0;
		[DllImport("shell32.dll", EntryPoint = "Shell_NotifyIconW", ExactSpelling = true)]
		unsafe static extern int __PInvoke(int __message_native, NotifyIconData* __data_native);
	}

	[LibraryImport("shell32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	internal unsafe static int SHQueryUserNotificationState(out int state)
	{
		state = 0;
		int result;
		fixed (int* _state_native = &state)
		{
			result = __PInvoke(_state_native);
		}
		return result;
		[DllImport("shell32.dll", EntryPoint = "SHQueryUserNotificationState", ExactSpelling = true)]
		unsafe static extern int __PInvoke(int* __state_native);
	}

	[LibraryImport("shell32.dll", EntryPoint = "SHAppBarMessage")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	internal unsafe static nuint SHAppBarMessage(uint message, ref AppBarData data)
	{
		nuint result;
		fixed (AppBarData* _data_native = &data)
		{
			result = __PInvoke(message, _data_native);
		}
		return result;
		[DllImport("shell32.dll", EntryPoint = "SHAppBarMessage", ExactSpelling = true)]
		unsafe static extern nuint __PInvoke(uint __message_native, AppBarData* __data_native);
	}

	[LibraryImport("shell32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	internal unsafe static int SHGetKnownFolderPath(in Guid folderId, uint flags, nint token, out nint path)
	{
		path = 0;
		int result;
		fixed (nint* _path_native = &path)
		{
			fixed (Guid* _folderId_native = &folderId)
			{
				result = __PInvoke(_folderId_native, flags, token, _path_native);
			}
		}
		return result;
		[DllImport("shell32.dll", EntryPoint = "SHGetKnownFolderPath", ExactSpelling = true)]
		unsafe static extern int __PInvoke(Guid* __folderId_native, uint __flags_native, nint __token_native, nint* __path_native);
	}

	[LibraryImport("shell32.dll", EntryPoint = "SHGetFileInfoW", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	internal unsafe static nint SHGetFileInfo(string path, uint attributes, ref ShellFileInfo info, uint infoSize, uint flags)
	{
		nint result;
		fixed (ShellFileInfo* _info_native = &info)
		{
			fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(path))
			{
				void* _path_native = ptr;
				result = __PInvoke((ushort*)_path_native, attributes, _info_native, infoSize, flags);
			}
		}
		return result;
		[DllImport("shell32.dll", EntryPoint = "SHGetFileInfoW", ExactSpelling = true)]
		unsafe static extern nint __PInvoke(ushort* __path_native, uint __attributes_native, ShellFileInfo* __info_native, uint __infoSize_native, uint __flags_native);
	}

	[LibraryImport("shell32.dll", EntryPoint = "ExtractIconExW", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	internal unsafe static uint ExtractIconEx(string file, int index, out nint large, out nint small, uint count)
	{
		large = 0;
		small = 0;
		uint result;
		fixed (nint* _small_native = &small)
		{
			fixed (nint* _large_native = &large)
			{
				fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(file))
				{
					void* _file_native = ptr;
					result = __PInvoke((ushort*)_file_native, index, _large_native, _small_native, count);
				}
			}
		}
		return result;
		[DllImport("shell32.dll", EntryPoint = "ExtractIconExW", ExactSpelling = true)]
		unsafe static extern uint __PInvoke(ushort* __file_native, int __index_native, nint* __large_native, nint* __small_native, uint __count_native);
	}

	[LibraryImport("shell32.dll", EntryPoint = "SHDefExtractIconW", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	internal unsafe static int SHDefExtractIcon(string file, int index, uint flags, out nint large, nint small, uint iconSize)
	{
		large = 0;
		int result;
		fixed (nint* _large_native = &large)
		{
			fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(file))
			{
				void* _file_native = ptr;
				result = __PInvoke((ushort*)_file_native, index, flags, _large_native, small, iconSize);
			}
		}
		return result;
		[DllImport("shell32.dll", EntryPoint = "SHDefExtractIconW", ExactSpelling = true)]
		unsafe static extern int __PInvoke(ushort* __file_native, int __index_native, uint __flags_native, nint* __large_native, nint __small_native, uint __iconSize_native);
	}

	[LibraryImport("shell32.dll", EntryPoint = "SHCreateItemFromParsingName", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	internal unsafe static int SHCreateItemFromParsingName(string path, nint bindContext, in Guid interfaceId, out nint instance)
	{
		instance = 0;
		int result;
		fixed (nint* _instance_native = &instance)
		{
			fixed (Guid* _interfaceId_native = &interfaceId)
			{
				fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(path))
				{
					void* _path_native = ptr;
					result = __PInvoke((ushort*)_path_native, bindContext, _interfaceId_native, _instance_native);
				}
			}
		}
		return result;
		[DllImport("shell32.dll", EntryPoint = "SHCreateItemFromParsingName", ExactSpelling = true)]
		unsafe static extern int __PInvoke(ushort* __path_native, nint __bindContext_native, Guid* __interfaceId_native, nint* __instance_native);
	}

	[LibraryImport("shell32.dll", EntryPoint = "SHGetNameFromIDList")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	internal unsafe static int SHGetNameFromIDList(nint itemIdList, uint sigdn, out nint name)
	{
		name = 0;
		int result;
		fixed (nint* _name_native = &name)
		{
			result = __PInvoke(itemIdList, sigdn, _name_native);
		}
		return result;
		[DllImport("shell32.dll", EntryPoint = "SHGetNameFromIDList", ExactSpelling = true)]
		unsafe static extern int __PInvoke(nint __itemIdList_native, uint __sigdn_native, nint* __name_native);
	}

	[LibraryImport("shell32.dll", EntryPoint = "SHParseDisplayName", StringMarshalling = StringMarshalling.Utf16)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	internal unsafe static int SHParseDisplayName(string name, nint bindContext, out nint itemIdList, uint attributesIn, out uint attributesOut)
	{
		itemIdList = 0;
		attributesOut = 0u;
		int result;
		fixed (uint* _attributesOut_native = &attributesOut)
		{
			fixed (nint* _itemIdList_native = &itemIdList)
			{
				fixed (char* ptr = &Utf16StringMarshaller.GetPinnableReference(name))
				{
					void* _name_native = ptr;
					result = __PInvoke((ushort*)_name_native, bindContext, _itemIdList_native, attributesIn, _attributesOut_native);
				}
			}
		}
		return result;
		[DllImport("shell32.dll", EntryPoint = "SHParseDisplayName", ExactSpelling = true)]
		unsafe static extern int __PInvoke(ushort* __name_native, nint __bindContext_native, nint* __itemIdList_native, uint __attributesIn_native, uint* __attributesOut_native);
	}

	[DllImport("shell32.dll", ExactSpelling = true)]
	[LibraryImport("shell32.dll")]
	internal static extern void ILFree(nint itemIdList);

	[LibraryImport("shell32.dll", EntryPoint = "ShellExecuteExW", SetLastError = true)]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	[return: MarshalAs(UnmanagedType.Bool)]
	internal unsafe static bool ShellExecuteEx(ref ShellExecuteInfo info)
	{
		int num;
		int lastSystemError;
		fixed (ShellExecuteInfo* _info_native = &info)
		{
			Marshal.SetLastSystemError(0);
			num = __PInvoke(_info_native);
			lastSystemError = Marshal.GetLastSystemError();
		}
		bool result = num != 0;
		Marshal.SetLastPInvokeError(lastSystemError);
		return result;
		[DllImport("shell32.dll", EntryPoint = "ShellExecuteExW", ExactSpelling = true)]
		unsafe static extern int __PInvoke(ShellExecuteInfo* __info_native);
	}

	[LibraryImport("shell32.dll")]
	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
	[SkipLocalsInit]
	internal unsafe static int SHGetPropertyStoreForWindow(nint window, in Guid interfaceId, out nint store)
	{
		store = 0;
		int result;
		fixed (nint* _store_native = &store)
		{
			fixed (Guid* _interfaceId_native = &interfaceId)
			{
				result = __PInvoke(window, _interfaceId_native, _store_native);
			}
		}
		return result;
		[DllImport("shell32.dll", EntryPoint = "SHGetPropertyStoreForWindow", ExactSpelling = true)]
		unsafe static extern int __PInvoke(nint __window_native, Guid* __interfaceId_native, nint* __store_native);
	}
}
