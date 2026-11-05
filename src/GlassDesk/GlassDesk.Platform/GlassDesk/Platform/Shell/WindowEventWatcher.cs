using System;
using System.Collections.Generic;
using System.Threading;
using GlassDesk.Platform.Interop;

namespace GlassDesk.Platform.Shell;

public sealed class WindowEventWatcher : IDisposable
{
	private const int CoalesceMilliseconds = 120;

	private static readonly uint[] WatchedEvents = new uint[7] { 32768u, 32769u, 32770u, 32771u, 22u, 23u, 3u };

	private readonly Action? _onChanged;

	private readonly Action<uint, nint, int, int>? _onRawEvent;

	private readonly ManualResetEventSlim _ready = new ManualResetEventSlim(initialState: false);

	private readonly Thread _hookThread;

	private readonly List<nint> _hooks = new List<nint>();

	private readonly Lock _sync = new Lock();

	private readonly User32.WinEventProc _callback;

	private Timer? _coalesceTimer;

	private uint _threadId;

	private bool _hooksInstalled;

	private volatile bool _disposed;

	public uint HookThreadId => _threadId;

	private WindowEventWatcher(Action? onChanged, Action<uint, nint, int, int>? onRawEvent)
	{
		_onChanged = onChanged;
		_onRawEvent = onRawEvent;
		_callback = OnWinEvent;
		_hookThread = new Thread(HookThreadMain)
		{
			IsBackground = true,
			Name = "GlassDesk 窗口事件源"
		};
		_coalesceTimer = new Timer(OnCoalesceElapsed, null, -1, -1);
	}

	public static WindowEventWatcher? TryStart(Action? onChanged, Action<uint, nint, int, int>? onRawEvent = null)
	{
		WindowEventWatcher windowEventWatcher = new WindowEventWatcher(onChanged, onRawEvent);
		if (windowEventWatcher.Start())
		{
			return windowEventWatcher;
		}
		windowEventWatcher.Dispose();
		return null;
	}

	public void Dispose()
	{
		using (_sync.EnterScope())
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
		}
		_coalesceTimer?.Dispose();
		_coalesceTimer = null;
		if (_hookThread.IsAlive)
		{
			if (_threadId != 0)
			{
				try
				{
					User32.PostThreadMessage(_threadId, 18u, 0u, 0);
				}
				catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
				{
				}
			}
			_hookThread.Join(TimeSpan.FromSeconds(2L));
		}
		_ready.Dispose();
	}

	private bool Start()
	{
		_hookThread.Start();
		if (!_ready.Wait(TimeSpan.FromSeconds(5L)))
		{
			return false;
		}
		return _hooksInstalled;
	}

	private void HookThreadMain()
	{
		try
		{
			_threadId = Kernel32.GetCurrentThreadId();
			User32.PeekMessage(out var _, 0, 0u, 0u, 0u);
			uint[] watchedEvents = WatchedEvents;
			foreach (uint num in watchedEvents)
			{
				nint num2 = User32.SetWinEventHook(num, num, 0, _callback, 0u, 0u, 0u);
				if (num2 != 0)
				{
					_hooks.Add(num2);
				}
			}
			_hooksInstalled = _hooks.Count > 0;
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			_hooksInstalled = false;
		}
		finally
		{
			_ready.Set();
		}
		if (!_hooksInstalled)
		{
			return;
		}
		try
		{
			Msg message2;
			while (User32.GetMessage(out message2, 0, 0u, 0u) > 0)
			{
				User32.TranslateMessage(in message2);
				User32.DispatchMessage(in message2);
			}
		}
		catch (Exception ex2) when ((ex2 is DllNotFoundException || ex2 is EntryPointNotFoundException) ? true : false)
		{
		}
		foreach (nint hook in _hooks)
		{
			User32.UnhookWinEvent(hook);
		}
		_hooks.Clear();
	}

	private void OnWinEvent(nint hook, uint eventType, nint window, int objectId, int childId, uint threadId, uint time)
	{
		if (!_disposed && objectId == 0 && childId == 0 && window != 0)
		{
			try
			{
				_onRawEvent?.Invoke(eventType, window, objectId, childId);
			}
			catch (Exception ex) when (!(ex is OutOfMemoryException))
			{
			}
			ScheduleReconcile();
		}
	}

	private void ScheduleReconcile()
	{
		Timer coalesceTimer = _coalesceTimer;
		if (coalesceTimer == null)
		{
			return;
		}
		try
		{
			coalesceTimer.Change(120, -1);
		}
		catch (ObjectDisposedException)
		{
		}
	}

	private void OnCoalesceElapsed(object? state)
	{
		if (_disposed)
		{
			return;
		}
		try
		{
			_onChanged?.Invoke();
		}
		catch (Exception ex) when (!(ex is OutOfMemoryException))
		{
		}
	}
}
