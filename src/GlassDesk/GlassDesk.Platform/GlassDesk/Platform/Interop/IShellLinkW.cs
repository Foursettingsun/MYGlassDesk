using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace GlassDesk.Platform.Interop;

[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("000214F9-0000-0000-C000-000000000046")]
[IUnknownDerived<_003CGlassDesk_Platform_Interop_IShellLinkW_003EFC159A7D2188C344C8DE588EDB0C2921240B69E43B8FB7138D5F76BACD273C869__InterfaceInformation, _003CGlassDesk_Platform_Interop_IShellLinkW_003EFC159A7D2188C344C8DE588EDB0C2921240B69E43B8FB7138D5F76BACD273C869__InterfaceImplementation>]
internal interface IShellLinkW
{
	void GetPath(nint fileOut, int maxPath, nint findData, uint flags);

	void GetIDList(out nint itemIdList);

	void SetIDList(nint itemIdList);

	void GetDescription(nint nameOut, int maxName);

	void SetDescription(string name);

	void GetWorkingDirectory(nint dirOut, int maxPath);

	void SetWorkingDirectory(string dir);

	void GetArguments(nint argsOut, int maxPath);

	void SetArguments(string args);

	void GetHotkey(out ushort hotkey);

	void SetHotkey(ushort hotkey);

	void GetShowCmd(out int showCommand);

	void SetShowCmd(int showCommand);

	void GetIconLocation(nint iconOut, int maxPath, out int iconIndex);

	void SetIconLocation(string iconPath, int iconIndex);

	void SetRelativePath(string pathRel, uint reserved);

	void Resolve(nint window, uint flags);

	void SetPath(string path);
}
