using System;
using GlassDesk.Platform.Interop;

namespace GlassDesk.Platform;

public static class ConsoleAttach
{
	private const int AttachParentProcess = -1;

	private const uint Utf8CodePage = 65001u;

	private static bool? Field;

	public static bool TryAttachToParentConsole()
	{
		bool? field = Field;
		if (field.HasValue)
		{
			return Field.Value;
		}
		bool flag = false;
		try
		{
			flag = Kernel32.AttachConsole(-1);
			if (flag)
			{
				Kernel32.SetConsoleOutputCP(65001u);
			}
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			flag = false;
		}
		Field = flag;
		return flag;
	}
}
