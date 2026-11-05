using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GlassDesk.Core.Input;
using GlassDesk.Core.Models.Settings;
using GlassDesk.Platform.Input;

namespace GlassDesk.Settings;

public sealed class ShortcutRecorder : Button, IDisposable
{
	private string _text = "";

	private string _originalText = "";

	private MouseSideButtonHook? _mouseCapture;

	private DispatcherTimer? _mouseReleaseTimer;

	public string Text
	{
		get
		{
			return _text;
		}
		set
		{
			_text = value ?? "";
			RefreshDisplay();
		}
	}

	public bool IsRecording { get; private set; }

	public event EventHandler? RecordingCompleted;

	public ShortcutRecorder()
	{
		SetResourceReference(FrameworkElement.StyleProperty, typeof(Button));
		MinHeight = 36.0;
		Margin = new Thickness(0.0, 3.0, 0.0, 3.0);
		RefreshDisplay();
		Unloaded += (object _, RoutedEventArgs _) =>
		{
			Dispose();
		};
	}

	protected override void OnClick()
	{
		base.OnClick();
		BeginRecording();
	}

	public void BeginRecording()
	{
		if (IsRecording)
		{
			return;
		}
		Focus();
		_originalText = Text;
		IsRecording = true;
		_mouseReleaseTimer?.Stop();
		if (_mouseCapture == null)
		{
			_mouseCapture = new MouseSideButtonHook((int button) =>
			{
				if (!IsRecording)
				{
					return false;
				}
				ModifierKeys modifiers = (ModifierKeys)KeyboardModifierState.Read();
				Dispatcher.BeginInvoke(DispatcherPriority.Input, (Action)(() =>
				{
					CaptureMouseButton(button, modifiers);
				}));
				return true;
			}, (int delta) =>
			{
				if (!IsRecording || delta == 0)
				{
					return false;
				}
				ModifierKeys modifiers = (ModifierKeys)KeyboardModifierState.Read();
				Dispatcher.BeginInvoke(DispatcherPriority.Input, (Action)(() =>
				{
					CaptureWheel(delta, modifiers);
				}));
				return true;
			});
			if (!_mouseCapture.TryStart(out string _))
			{
				_mouseCapture.Dispose();
				_mouseCapture = null;
			}
		}
		Background = new SolidColorBrush(Color.FromArgb(65, 10, 132, byte.MaxValue));
		BorderBrush = new SolidColorBrush(Color.FromRgb(102, 181, byte.MaxValue));
		RefreshDisplay();
	}

	public void CancelRecording()
	{
		IsRecording = false;
		ClearValue(Control.BackgroundProperty);
		ClearValue(Control.BorderBrushProperty);
		RefreshDisplay();
		if (_mouseCapture == null)
		{
			return;
		}
		if (_mouseReleaseTimer == null)
		{
			_mouseReleaseTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(25L), DispatcherPriority.Background, (object? _, EventArgs _) =>
			{
				if (!KeyboardModifierState.IsDown(5) && !KeyboardModifierState.IsDown(6))
				{
					_mouseReleaseTimer.Stop();
					_mouseCapture?.Dispose();
					_mouseCapture = null;
				}
			}, Dispatcher);
		}
		_mouseReleaseTimer.Start();
	}

	public void RestorePreviousBinding()
	{
		Text = _originalText;
	}

	public void Dispose()
	{
		IsRecording = false;
		_mouseReleaseTimer?.Stop();
		_mouseCapture?.Dispose();
		_mouseCapture = null;
		RefreshDisplay();
	}

	protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
	{
		base.OnLostKeyboardFocus(e);
		if (!IsKeyboardFocusWithin)
		{
			CancelRecording();
		}
	}

	protected override void OnPreviewKeyDown(KeyEventArgs e)
	{
		if (!IsRecording)
		{
			base.OnPreviewKeyDown(e);
			return;
		}
		e.Handled = true;
		if (!e.IsRepeat)
		{
			Key key;
			if (e.Key == Key.System)
			{
				key = e.SystemKey;
			}
			else
			{
				key = ((e.Key == Key.ImeProcessed) ? e.ImeProcessedKey : e.Key);
			}
			CaptureKey(key, Keyboard.Modifiers);
		}
	}

	public void CaptureKey(Key key, ModifierKeys modifiers)
	{
		if (!IsRecording)
		{
			return;
		}
		bool flag;
		switch (key)
		{
		case Key.Escape:
			CancelRecording();
			return;
		case Key.None:
		case Key.LWin:
		case Key.RWin:
		case Key.LeftShift:
		case Key.RightShift:
		case Key.LeftCtrl:
		case Key.RightCtrl:
		case Key.LeftAlt:
		case Key.RightAlt:
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			return;
		}
		if ((key == Key.Back || key == Key.Delete) ? true : false)
		{
			Complete("");
			return;
		}
		int num = KeyInterop.VirtualKeyFromKey(key);
		string text;
		switch (num)
		{
		case 48:
		case 49:
		case 50:
		case 51:
		case 52:
		case 53:
		case 54:
		case 55:
		case 56:
		case 57:
		case 65:
		case 66:
		case 67:
		case 68:
		case 69:
		case 70:
		case 71:
		case 72:
		case 73:
		case 74:
		case 75:
		case 76:
		case 77:
		case 78:
		case 79:
		case 80:
		case 81:
		case 82:
		case 83:
		case 84:
		case 85:
		case 86:
		case 87:
		case 88:
		case 89:
		case 90:
			text = ((char)num).ToString();
			break;
		case 112:
		case 113:
		case 114:
		case 115:
		case 116:
		case 117:
		case 118:
		case 119:
		case 120:
		case 121:
		case 122:
		case 123:
		case 124:
		case 125:
		case 126:
		case 127:
		case 128:
		case 129:
		case 130:
		case 131:
		case 132:
		case 133:
		case 134:
		case 135:
			text = "F" + (num - 111);
			break;
		case 106:
			text = "Multiply";
			break;
		case 107:
			text = "Add";
			break;
		case 108:
			text = "Separator";
			break;
		case 109:
			text = "Subtract";
			break;
		case 110:
			text = "Decimal";
			break;
		case 111:
			text = "Divide";
			break;
		case 13:
			text = "Enter";
			break;
		case 32:
			text = "Space";
			break;
		case 9:
			text = "Tab";
			break;
		case 33:
			text = "PageUp";
			break;
		case 34:
			text = "PageDown";
			break;
		case 35:
			text = "End";
			break;
		case 36:
			text = "Home";
			break;
		case 37:
			text = "Left";
			break;
		case 38:
			text = "Up";
			break;
		case 39:
			text = "Right";
			break;
		case 40:
			text = "Down";
			break;
		case 45:
			text = "Insert";
			break;
		case 186:
			text = "OemSemicolon";
			break;
		case 187:
			text = "OemPlus";
			break;
		case 188:
			text = "OemComma";
			break;
		case 189:
			text = "OemMinus";
			break;
		case 190:
			text = "OemPeriod";
			break;
		case 191:
			text = "OemQuestion";
			break;
		case 192:
			text = "OemTilde";
			break;
		case 219:
			text = "OemOpenBrackets";
			break;
		case 220:
			text = "OemPipe";
			break;
		case 221:
			text = "OemCloseBrackets";
			break;
		case 222:
			text = "OemQuotes";
			break;
		case 226:
			text = "OemBackslash";
			break;
		default:
			text = key.ToString();
			break;
		}
		string keyName = text;
		CaptureNamedKey(keyName, modifiers);
	}

	public void CaptureMouseButton(int button, ModifierKeys modifiers)
	{
		bool flag = IsRecording;
		if (flag)
		{
			bool flag2 = (uint)(button - 1) <= 1u;
			flag = flag2;
		}
		if (flag)
		{
			CaptureNamedKey("XButton" + button, modifiers);
		}
	}

	public void CaptureWheel(int delta, ModifierKeys modifiers)
	{
		if (IsRecording && delta != 0)
		{
			CaptureNamedKey((delta > 0) ? "WheelUp" : "WheelDown", modifiers);
		}
	}

	protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
	{
		if (!IsRecording)
		{
			base.OnPreviewMouseWheel(e);
			return;
		}
		e.Handled = true;
		CaptureWheel(e.Delta, Keyboard.Modifiers);
	}

	protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
	{
		bool flag = IsRecording;
		if (flag)
		{
			MouseButton changedButton = e.ChangedButton;
			bool flag2 = (uint)(changedButton - 3) <= 1u;
			flag = flag2;
		}
		if (flag)
		{
			e.Handled = true;
			CaptureMouseButton((e.ChangedButton == MouseButton.XButton1) ? 1 : 2, Keyboard.Modifiers);
		}
		else
		{
			base.OnPreviewMouseDown(e);
		}
	}

	private void CaptureNamedKey(string keyName, ModifierKeys modifiers)
	{
		List<string> list = new List<string>();
		if (modifiers.HasFlag(ModifierKeys.Control))
		{
			list.Add("Ctrl");
		}
		if (modifiers.HasFlag(ModifierKeys.Alt))
		{
			list.Add("Alt");
		}
		if (modifiers.HasFlag(ModifierKeys.Shift))
		{
			list.Add("Shift");
		}
		if (modifiers.HasFlag(ModifierKeys.Windows))
		{
			list.Add("Win");
		}
		list.Add(keyName);
		if (!HotkeyText.TryParse(string.Join('+', list), out HotkeyDefinition definition, out string error))
		{
			Content = error;
		}
		else
		{
			Complete(HotkeyText.Format(definition));
		}
	}

	private void Complete(string text)
	{
		_text = text;
		CancelRecording();
		RecordingCompleted?.Invoke(this, EventArgs.Empty);
	}

	private void RefreshDisplay()
	{
		ShortcutRecorder shortcutRecorder = this;
		object content;
		if (IsRecording)
		{
			content = "请按下新的快捷键…";
		}
		else
		{
			content = (string.IsNullOrEmpty(Text) ? "未设置 · 点击录制" : HotkeyText.DisplayFormat(Text));
		}
		shortcutRecorder.Content = content;
		ToolTip = (IsRecording ? "Esc 取消；Backspace / Delete 清除绑定" : "点击录制快捷键");
	}
}
