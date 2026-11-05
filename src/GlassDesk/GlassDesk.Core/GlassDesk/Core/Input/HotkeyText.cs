using System;
using System.Collections.Generic;
using System.Linq;
using GlassDesk.Core.Models.Settings;

namespace GlassDesk.Core.Input;

public static class HotkeyText
{
	private static readonly Dictionary<string, uint> Keys = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase)
	{
		["Space"] = 32u,
		["Enter"] = 13u,
		["Escape"] = 27u,
		["Tab"] = 9u,
		["Left"] = 37u,
		["Up"] = 38u,
		["Right"] = 39u,
		["Down"] = 40u,
		["Home"] = 36u,
		["End"] = 35u,
		["PageUp"] = 33u,
		["PageDown"] = 34u,
		["Insert"] = 45u,
		["Delete"] = 46u,
		["OemSemicolon"] = 186u,
		["OemPlus"] = 187u,
		["OemComma"] = 188u,
		["OemMinus"] = 189u,
		["OemPeriod"] = 190u,
		["OemQuestion"] = 191u,
		["OemTilde"] = 192u,
		["OemOpenBrackets"] = 219u,
		["OemPipe"] = 220u,
		["OemCloseBrackets"] = 221u,
		["OemQuotes"] = 222u,
		["OemBackslash"] = 226u,
		["XButton1"] = 5u,
		["XButton2"] = 6u,
		["WheelUp"] = 256u,
		["WheelDown"] = 257u,
		["Multiply"] = 106u,
		["Add"] = 107u,
		["Separator"] = 108u,
		["Subtract"] = 109u,
		["Decimal"] = 110u,
		["Divide"] = 111u
	};

	public static string DisplayFormat(string text)
	{
		if (!TryParse(text, out HotkeyDefinition definition, out string error))
		{
			return text.Replace("+", " + ");
		}
		switch (definition.Key)
		{
		case "OemSemicolon":
			error = ";";
			break;
		case "OemPlus":
			error = "=";
			break;
		case "OemComma":
			error = ",";
			break;
		case "OemMinus":
			error = "-";
			break;
		case "OemPeriod":
			error = ".";
			break;
		case "OemQuestion":
			error = "/";
			break;
		case "OemTilde":
			error = "`";
			break;
		case "OemOpenBrackets":
			error = "[";
			break;
		case "OemBackslash":
		case "OemPipe":
			error = "\\";
			break;
		case "OemCloseBrackets":
			error = "]";
			break;
		case "OemQuotes":
			error = "'";
			break;
		case "XButton1":
			error = "鼠标侧键 1";
			break;
		case "XButton2":
			error = "鼠标侧键 2";
			break;
		case "WheelUp":
			error = "滚轮向上";
			break;
		case "WheelDown":
			error = "滚轮向下";
			break;
		default:
			error = definition.Key;
			break;
		}
		string text2 = error;
		string text3 = Format(definition);
		if (text3.Length != 0)
		{
			string text4 = text3;
			int length = definition.Key.Length;
			return text4.Substring(0, text4.Length - length).Replace("+", " + ") + text2;
		}
		return "";
	}

	public static string Format(HotkeyDefinition definition)
	{
		if (!definition.IsBound)
		{
			return "";
		}
		return definition.Describe();
	}

	public static bool TryParse(string text, out HotkeyDefinition definition, out string? error)
	{
		definition = new HotkeyDefinition();
		error = null;
		if (string.IsNullOrWhiteSpace(text))
		{
			return true;
		}
		string[] array = text.Trim().Split('+', StringSplitOptions.TrimEntries);
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string[] subArray = array[..^1];
		foreach (string text2 in subArray)
		{
			string text3 = (text2.Equals("Control", StringComparison.OrdinalIgnoreCase) ? "Ctrl" : text2);
			if (!hashSet.Add(text3))
			{
				error = "修饰键重复。";
				return false;
			}
			switch (text3.ToUpperInvariant())
			{
			case "CTRL":
				definition.Control = true;
				break;
			case "ALT":
				definition.Alt = true;
				break;
			case "SHIFT":
				definition.Shift = true;
				break;
			case "WIN":
				definition.Win = true;
				break;
			default:
				error = "快捷键格式应为 Ctrl+Alt+Space 或 F2 等组合。";
				return false;
			}
		}
		string key = array[^1];
		definition.Key = Keys.Keys.FirstOrDefault((string k) => k.Equals(key, StringComparison.OrdinalIgnoreCase)) ?? key.ToUpperInvariant();
		definition.Enabled = true;
		HotkeyChord chord;
		return TryGetChord(definition, out chord, out error);
	}

	public static bool TryGetChord(HotkeyDefinition definition, out HotkeyChord chord, out string? error)
	{
		chord = default;
		error = null;
		if (!definition.Enabled)
		{
			return false;
		}
		string text = definition.Key?.Trim() ?? "";
		int result = 0;
		bool flag = text.StartsWith('F') && int.TryParse(text.AsSpan(1), out result) && result >= 1 && result <= 24;
		uint value;
		if (flag)
		{
			value = (uint)(result + 111);
		}
		else if (text.Length == 1 && char.IsAsciiLetterOrDigit(text[0]))
		{
			value = char.ToUpperInvariant(text[0]);
		}
		else if (!Keys.TryGetValue(text, out value))
		{
			error = "键名无效。可使用字母、数字、F1–F24、Space、方向键等。";
			return false;
		}
		uint num = (definition.Alt ? 1u : 0u) | (uint)(definition.Control ? 2 : 0) | (uint)(definition.Shift ? 4 : 0) | (uint)(definition.Win ? 8 : 0);
		bool flag2 = ((value - 5 <= 1 || value - 256 <= 1) ? true : false);
		bool flag3 = flag2;
		switch (value)
		{
		case 106u:
		case 107u:
		case 108u:
		case 109u:
		case 110u:
		case 111u:
		case 186u:
		case 187u:
		case 188u:
		case 189u:
		case 190u:
		case 191u:
		case 192u:
		case 219u:
		case 220u:
		case 221u:
		case 222u:
		case 226u:
			flag2 = true;
			break;
		default:
			flag2 = false;
			break;
		}
		bool flag4 = flag2;
		if (num == 0 && !flag && !flag3 && !flag4)
		{
			error = "普通按键需要 Ctrl、Alt、Shift 或 Win 修饰键，以免拦截日常输入。";
			return false;
		}
		chord = new HotkeyChord(num, value);
		return true;
	}

	public static IEnumerable<(InputAction Action, HotkeyDefinition Definition)> Bindings(ShortcutSettings settings)
	{
		yield return (Action: InputAction.ToggleLauncher, Definition: settings.ToggleLauncher);
		yield return (Action: InputAction.HideLauncher, Definition: settings.HideLauncher);
		yield return (Action: InputAction.NextPage, Definition: settings.NextPage);
		yield return (Action: InputAction.PreviousPage, Definition: settings.PreviousPage);
		yield return (Action: InputAction.ToggleDesktopIcons, Definition: settings.ToggleDesktopIcons);
		yield return (Action: InputAction.ToggleDockRevealLock, Definition: settings.ToggleDockRevealLock);
		yield return (Action: InputAction.ToggleGameSleep, Definition: settings.ToggleGameSleep);
	}
}
