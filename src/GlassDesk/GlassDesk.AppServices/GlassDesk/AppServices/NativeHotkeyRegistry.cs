using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using GlassDesk.Core.Input;
using GlassDesk.Platform.Input;

namespace GlassDesk.AppServices;

public sealed class NativeHotkeyRegistry(nint hwnd, Func<int, bool>? canDispatchWheel = null) : IHotkeyRegistry, IDisposable
{
	private readonly GlobalHotkeyRegistration registry = new GlobalHotkeyRegistration(hwnd);

	private readonly Dictionary<int, HotkeyChord> mouseBindings = new Dictionary<int, HotkeyChord>();

	private MouseSideButtonHook? mouseHook;

	private int wheelRemainder;

	private uint wheelModifiers;

	public bool TryRegister(int id, HotkeyChord chord, out int errorCode)
	{
		uint virtualKey = chord.VirtualKey;
		if ((virtualKey - 5 > 1 && virtualKey - 256 > 1) || 1 == 0)
		{
			return registry.TryRegister(id, chord.Modifiers | 0x4000, chord.VirtualKey, out errorCode);
		}
		errorCode = 0;
		if (mouseBindings.ContainsKey(id) || mouseBindings.ContainsValue(chord))
		{
			errorCode = 1409;
			return false;
		}
		if (mouseHook == null)
		{
			MouseSideButtonHook mouseSideButtonHook = new MouseSideButtonHook((int button) => DispatchSideButton(button, KeyboardModifierState.Read()), (int delta) => DispatchWheel(delta, KeyboardModifierState.Read()));
			if (!mouseSideButtonHook.TryStart(out string _))
			{
				errorCode = Marshal.GetLastWin32Error();
				mouseSideButtonHook.Dispose();
				return false;
			}
			mouseHook = mouseSideButtonHook;
		}
		mouseBindings.Add(id, chord);
		return true;
	}

	public bool DispatchSideButton(int button, uint modifiers)
	{
		bool flag = (uint)(button - 1) <= 1u;
		if (!flag)
		{
			return false;
		}
		HotkeyChord hotkeyChord = new HotkeyChord(modifiers, (button == 1) ? 5u : 6u);
		foreach (KeyValuePair<int, HotkeyChord> mouseBinding in mouseBindings)
		{
			if (!(mouseBinding.Value == hotkeyChord))
			{
				continue;
			}
			Func<int, bool>? func = canDispatchWheel;
			flag = (func == null || func(mouseBinding.Key)) && PostMessage(hwnd, 786, mouseBinding.Key, (nint)((hotkeyChord.VirtualKey << 16) | modifiers));
			goto IL_00ab;
		}
		return false;
		IL_00ab:
		return flag;
	}

	public bool Unregister(int id)
	{
		if (!mouseBindings.Remove(id))
		{
			return registry.Unregister(id);
		}
		wheelRemainder = 0;
		if (mouseBindings.Count == 0)
		{
			mouseHook?.Dispose();
			mouseHook = null;
		}
		return true;
	}

	public bool DispatchWheel(int delta, uint modifiers)
	{
		if (delta == 0)
		{
			return false;
		}
		HotkeyChord hotkeyChord = new HotkeyChord(modifiers, (delta > 0) ? 256u : 257u);
		foreach (KeyValuePair<int, HotkeyChord> mouseBinding in mouseBindings)
		{
			if (mouseBinding.Value != hotkeyChord)
			{
				continue;
			}
			Func<int, bool>? func = canDispatchWheel;
			if (func != null && !func(mouseBinding.Key))
			{
				wheelRemainder = 0;
				return false;
			}
			if (wheelModifiers != modifiers || Math.Sign(wheelRemainder) != Math.Sign(delta))
			{
				wheelRemainder = 0;
			}
			wheelModifiers = modifiers;
			wheelRemainder += delta;
			while (Math.Abs(wheelRemainder) >= 120)
			{
				if (!PostMessage(hwnd, 786, mouseBinding.Key, (nint)((hotkeyChord.VirtualKey << 16) | modifiers)))
				{
					wheelRemainder = 0;
					return false;
				}
				wheelRemainder -= Math.Sign(wheelRemainder) * 120;
			}
			return true;
		}
		wheelRemainder = 0;
		return false;
	}

	public void Dispose()
	{
		mouseHook?.Dispose();
		mouseHook = null;
		mouseBindings.Clear();
		registry.Dispose();
	}

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool PostMessage(nint hwnd, int message, nint wParam, nint lParam);
}
