using System;
using System.Collections.Generic;
using System.Threading;
using GlassDesk.Platform.Interop;

namespace GlassDesk.Platform.Shell;

public sealed class FullscreenWatcher : IDisposable
{
	private const int CoalesceMilliseconds = 40;

	private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(5L);

	private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(2L);

	private readonly FullscreenOptions _options;

	private readonly Action<bool>? _onChanged;

	private readonly Action? _onStateInvalidated;

	private readonly Func<long>? _getStateGeneration;

	private readonly Action<long, bool>? _onStateRefreshed;

	private readonly Action<PerformanceFullscreenObservation>? _onPerformanceObservation;

	private readonly ManualResetEventSlim _ready = new ManualResetEventSlim(initialState: false);

	private readonly Thread _hookThread;

	private readonly List<nint> _hooks = new List<nint>();

	private readonly Lock _sync = new Lock();

	private readonly User32.WinEventProc _callback;

	private Timer? _coalesceTimer;

	private uint _threadId;

	private volatile bool _hooksInstalled;

	private bool _isFullscreen;

	private volatile bool _disposed;

	public bool IsFullscreen
	{
		get
		{
			using (_sync.EnterScope())
			{
				return _isFullscreen;
			}
		}
	}

	public bool IsAvailable
	{
		get
		{
			if (!_disposed && _hooksInstalled)
			{
				return _hookThread.IsAlive;
			}
			return false;
		}
	}

	private FullscreenWatcher(Action<bool>? onChanged, FullscreenOptions options, Action? onStateInvalidated, Func<long>? getStateGeneration, Action<long, bool>? onStateRefreshed, Action<PerformanceFullscreenObservation>? onPerformanceObservation)
	{
		_onChanged = onChanged;
		_options = options;
		_onStateInvalidated = onStateInvalidated;
		_getStateGeneration = getStateGeneration;
		_onStateRefreshed = onStateRefreshed;
		_onPerformanceObservation = onPerformanceObservation;
		_callback = OnWinEvent;
		_hookThread = new Thread(HookThreadMain)
		{
			IsBackground = true,
			Name = "GlassDesk 全屏事件钩子"
		};
		_coalesceTimer = new Timer(OnCoalesceElapsed, null, -1, -1);
	}

	public static FullscreenWatcher? TryStart(Action<bool>? onFullscreenChanged = null, FullscreenOptions? options = null, Action? onStateInvalidated = null, Func<long>? getStateGeneration = null, Action<long, bool>? onStateRefreshed = null, Action<PerformanceFullscreenObservation>? onPerformanceObservation = null)
	{
		FullscreenWatcher fullscreenWatcher = new FullscreenWatcher(onFullscreenChanged, options ?? FullscreenOptions.Default, onStateInvalidated, getStateGeneration, onStateRefreshed, onPerformanceObservation);
		if (fullscreenWatcher.Start())
		{
			return fullscreenWatcher;
		}
		fullscreenWatcher.Dispose();
		return null;
	}

	public bool Refresh()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		return Recompute();
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
			_hookThread.Join(ShutdownTimeout);
		}
		_ready.Dispose();
	}

	private bool Start()
	{
		_hookThread.Start();
		if (!_ready.Wait(StartupTimeout))
		{
			return false;
		}
		if (!_hooksInstalled)
		{
			return false;
		}
		Recompute();
		return true;
	}

	private void HookThreadMain()
	{
		try
		{
			_threadId = Kernel32.GetCurrentThreadId();
			User32.PeekMessage(out var _, 0, 0u, 0u, 0u);
			nint num = User32.SetWinEventHook(3u, 3u, 0, _callback, 0u, 0u, 0u);
			if (num != 0)
			{
				_hooks.Add(num);
			}
			nint num2 = User32.SetWinEventHook(32779u, 32779u, 0, _callback, 0u, 0u, 0u);
			if (num2 != 0)
			{
				_hooks.Add(num2);
			}
			_hooksInstalled = num != 0 && num2 != 0;
		}
		catch (Exception)
		{
			_hooksInstalled = false;
		}
		finally
		{
			_ready.Set();
		}
		if (_hooksInstalled)
		{
			try
			{
				int num3 = 1;
				try
				{
					Msg message2;
					while ((num3 = User32.GetMessage(out message2, 0, 0u, 0u)) > 0)
					{
						User32.TranslateMessage(in message2);
						User32.DispatchMessage(in message2);
					}
				}
				catch (Exception)
				{
					num3 = -1;
				}
				if (num3 <= 0 && !_disposed)
				{
					MarkMonitorUnavailable();
				}
			}
			catch (Exception)
			{
				if (!_disposed)
				{
					MarkMonitorUnavailable();
				}
			}
		}
		_hooksInstalled = false;
		foreach (nint hook in _hooks)
		{
			try
			{
				User32.UnhookWinEvent(hook);
			}
			catch (Exception)
			{
			}
		}
		_hooks.Clear();
	}

	private void OnWinEvent(nint hook, uint eventType, nint window, int objectId, int childId, uint threadId, uint time)
	{
		if (_disposed)
		{
			return;
		}
		try
		{
			if (_hooksInstalled && (eventType != 32779 || (objectId == 0 && childId == 0 && window == User32.GetForegroundWindow())))
			{
				NotifyStateInvalidated();
				ScheduleRecompute();
			}
		}
		catch (Exception)
		{
			MarkMonitorUnavailable();
		}
	}

	private void ScheduleRecompute()
	{
		Timer coalesceTimer = _coalesceTimer;
		if (coalesceTimer == null)
		{
			return;
		}
		try
		{
			coalesceTimer.Change(40, -1);
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
		if (!IsAvailable)
		{
			MarkMonitorUnavailable();
			return;
		}
		try
		{
			Recompute();
		}
		catch (ObjectDisposedException)
		{
		}
		catch (Exception)
		{
			MarkMonitorUnavailable();
		}
	}

	private bool Recompute()
	{
		long num = _getStateGeneration?.Invoke() ?? 0;
		PerformanceFullscreenObservation obj = ((_onPerformanceObservation != null) ? FullscreenDetector.ReadPerformanceObservation() : default(PerformanceFullscreenObservation));
		bool flag = ((_onPerformanceObservation == null) ? FullscreenDetector.IsFullscreenActive(_options) : (obj.IsFullscreen ?? IsFullscreen));
		if (_getStateGeneration != null && _getStateGeneration() != num)
		{
			return flag;
		}
		Action<bool> action = null;
		using (_sync.EnterScope())
		{
			if (flag != _isFullscreen)
			{
				_isFullscreen = flag;
				action = _onChanged;
			}
		}
		try
		{
			_onStateRefreshed?.Invoke(num, flag);
		}
		catch (Exception)
		{
		}
		try
		{
			action?.Invoke(flag);
		}
		catch (Exception)
		{
		}
		try
		{
			_onPerformanceObservation?.Invoke(obj);
		}
		catch (Exception)
		{
		}
		return flag;
	}

	private void NotifyStateInvalidated()
	{
		try
		{
			_onStateInvalidated?.Invoke();
		}
		catch (Exception)
		{
		}
	}

	private void MarkMonitorUnavailable()
	{
		_hooksInstalled = false;
		try
		{
			_coalesceTimer?.Change(-1, -1);
		}
		catch (ObjectDisposedException)
		{
		}
		if (!_disposed)
		{
			NotifyStateInvalidated();
		}
	}
}
