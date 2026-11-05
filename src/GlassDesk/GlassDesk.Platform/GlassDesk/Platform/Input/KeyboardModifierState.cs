using System.Runtime.InteropServices;

namespace GlassDesk.Platform.Input;

public static class KeyboardModifierState
{
	public static bool IsDown(int virtualKey)
	{
		return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
	}

	public static uint Read()
	{
		return (IsDown(18) ? 1u : 0u) | (uint)(IsDown(17) ? 2 : 0) | (uint)(IsDown(16) ? 4 : 0) | (uint)((IsDown(91) || IsDown(92)) ? 8 : 0);
	}

	[DllImport("user32.dll")]
	private static extern short GetAsyncKeyState(int virtualKey);
}
