using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GlassDesk.Core.Input;
using GlassDesk.Core.Models.Settings;
using GlassDesk.Core.Storage;

namespace GlassDesk.AppServices;

public sealed class InteractionSettingsService(SettingsStore store, IHotkeyRegistry registry) : IDisposable
{
	private Dictionary<HotkeyChord, (int Id, InputAction Action)> active = new Dictionary<HotkeyChord, (int, InputAction)>();

	private int nextId = 4096;

	private bool disposed;

	public InputAction ResolveAction(int id)
	{
		return active.Values.FirstOrDefault(((int Id, InputAction Action) value) => value.Id == id).Action;
	}

	public string? Apply(ShortcutSettings shortcuts, DisplaySettings display, bool persist = true)
	{
		if (disposed)
		{
			return "快捷操作服务已经关闭。";
		}
		GlassDeskSettings glassDeskSettings = SettingsPageEditor.Clone(store.Current);
		glassDeskSettings.Shortcuts = shortcuts;
		glassDeskSettings.Display = display;
		glassDeskSettings = SettingsPageEditor.Clone(glassDeskSettings);
		glassDeskSettings.Dock.TargetDisplayId = glassDeskSettings.Display.DockDisplayId;
		glassDeskSettings.Shortcuts.HotCornersEnabled = false;
		glassDeskSettings.Shortcuts.MouseSideButtonsEnabled = false;
		string text = SettingsPageEditor.Validate(glassDeskSettings);
		if (text != null)
		{
			return text;
		}
		Dictionary<HotkeyChord, (int Id, InputAction Action)> desired = new Dictionary<HotkeyChord, (int, InputAction)>();
		List<int> list = new List<int>();
		foreach (var (item, hotkeyDefinition) in HotkeyText.Bindings(glassDeskSettings.Shortcuts))
		{
			if (!hotkeyDefinition.Enabled)
			{
				continue;
			}
			HotkeyText.TryGetChord(hotkeyDefinition, out HotkeyChord chord, out string _);
			if (active.TryGetValue(chord, out (int, InputAction) value))
			{
				desired.Add(chord, (value.Item1, item));
				continue;
			}
			int num = nextId++;
			if (!registry.TryRegister(num, chord, out var errorCode))
			{
				foreach (int item2 in list)
				{
					registry.Unregister(item2);
				}
				return $"快捷键 {hotkeyDefinition.Describe()} 注册失败（错误 {errorCode}），可能已被其他程序占用；原设置已保留。";
			}
			list.Add(num);
			desired.Add(chord, (num, item));
		}
		GlassDeskSettings source = SettingsPageEditor.Clone(store.Current);
		try
		{
			SettingsPageEditor.Apply(store.Current, glassDeskSettings);
			if (persist)
			{
				store.Save();
			}
		}
		catch (Exception ex) when ((ex is StorageException || ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
			SettingsPageEditor.Apply(store.Current, source);
			foreach (int item3 in list)
			{
				registry.Unregister(item3);
			}
			return "保存失败，已恢复原设置及快捷键：" + ex.Message;
		}
		foreach (KeyValuePair<HotkeyChord, (int, InputAction)> item4 in active.Where((KeyValuePair<HotkeyChord, (int Id, InputAction Action)> pair) => !desired.ContainsKey(pair.Key)))
		{
			registry.Unregister(item4.Value.Item1);
		}
		active = desired;
		return null;
	}

	public void Dispose()
	{
		if (!disposed)
		{
			disposed = true;
			registry.Dispose();
			active.Clear();
		}
	}
}
