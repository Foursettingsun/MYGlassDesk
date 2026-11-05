using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GlassDesk.Core.Models;
using GlassDesk.Core.Storage;
using GlassDesk.Platform;
using GlassDesk.Platform.Shell;

namespace GlassDesk.AppServices;

public sealed class RunningAppTracker : IDisposable
{
	private static readonly TimeSpan MinimumReconcileInterval = TimeSpan.FromMilliseconds(150L);

	private readonly AppLibraryStore _library;

	private readonly ConcurrentDictionary<string, string?> _identityCache = new ConcurrentDictionary<string, string>();

	private readonly Lock _sync = new Lock();

	private WindowEventWatcher? _watcher;

	private IReadOnlyList<WindowIdentityInfo> _windows = Array.Empty<WindowIdentityInfo>();

	private nint _foregroundWindow;

	private DateTime _lastReconcileUtc = DateTime.MinValue;

	private volatile bool _disposed;

	private bool _suspended;

	private bool _dirty;

	private long _runtimeGeneration;

	private long _reconcileCount;

	public long ReconcileCount => Interlocked.Read(in _reconcileCount);

	public bool IsSuspended
	{
		get
		{
			using (_sync.EnterScope())
			{
				return _suspended;
			}
		}
	}

	public IReadOnlyList<WindowIdentityInfo> LastWindows
	{
		get
		{
			using (_sync.EnterScope())
			{
				return _windows;
			}
		}
	}

	public event EventHandler? Changed;

	public void SetSuspended(bool suspended)
	{
		bool flag;
		using (_sync.EnterScope())
		{
			if (_disposed || _suspended == suspended)
			{
				return;
			}
			_suspended = suspended;
			_runtimeGeneration++;
			flag = !suspended && _dirty;
			if (flag)
			{
				_lastReconcileUtc = DateTime.MinValue;
			}
		}
		if (flag)
		{
			Reconcile();
		}
	}

	public RunningAppTracker(AppLibraryStore library)
	{
		ArgumentNullException.ThrowIfNull(library, "library");
		_library = library;
	}

	public void Start()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		Reconcile();
		_watcher = WindowEventWatcher.TryStart(() =>
		{
			Reconcile();
		});
	}

	public int Reconcile()
	{
		if (_disposed)
		{
			return 0;
		}
		long runtimeGeneration;
		using (_sync.EnterScope())
		{
			if (_suspended)
			{
				_dirty = true;
				return _windows.Count;
			}
			if (DateTime.UtcNow - _lastReconcileUtc < MinimumReconcileInterval)
			{
				return _windows.Count;
			}
			_lastReconcileUtc = DateTime.UtcNow;
			runtimeGeneration = _runtimeGeneration;
		}
		IReadOnlyList<WindowIdentityInfo> readOnlyList = WindowLocator.EnumerateTopLevelWindows();
		nint foregroundWindow = WindowControl.GetForegroundWindow();
		bool flag;
		using (_sync.EnterScope())
		{
			if (_disposed || _suspended || runtimeGeneration != _runtimeGeneration)
			{
				_dirty = true;
				return _windows.Count;
			}
			_dirty = false;
			_reconcileCount++;
			flag = !SameWindowSet(_windows, readOnlyList) || _foregroundWindow != foregroundWindow;
			_windows = readOnlyList;
			_foregroundWindow = foregroundWindow;
		}
		if (flag)
		{
			Changed?.Invoke(this, EventArgs.Empty);
		}
		return readOnlyList.Count;
	}

	public bool IsRunning(AppEntry entry)
	{
		return FindWindows(entry).Count > 0;
	}

	public bool IsActive(AppEntry entry)
	{
		nint foreground = WindowControl.GetForegroundWindow();
		if (foreground != 0)
		{
			return FindWindows(entry).Any((WindowIdentityInfo window) => window.Handle == foreground);
		}
		return false;
	}

	public IReadOnlyList<WindowIdentityInfo> FindWindows(AppEntry entry)
	{
		ArgumentNullException.ThrowIfNull(entry, "entry");
		return WindowMatcher.MatchWindows(entry, LastWindows, WindowMatcher.ResolveAumid(entry, _identityCache));
	}

	public IReadOnlyList<string> FindRunningIds(IEnumerable<AppEntry> candidates)
	{
		ArgumentNullException.ThrowIfNull(candidates, "candidates");
		using (_sync.EnterScope())
		{
			List<string> list = new List<string>();
			foreach (AppEntry candidate in candidates)
			{
				string aumid = WindowMatcher.ResolveAumid(candidate, _identityCache);
				if (WindowMatcher.MatchWindows(candidate, _windows, aumid).Count > 0)
				{
					list.Add(candidate.Id);
				}
			}
			return list;
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_watcher?.Dispose();
			_watcher = null;
		}
	}

	private static bool SameWindowSet(IReadOnlyList<WindowIdentityInfo> left, IReadOnlyList<WindowIdentityInfo> right)
	{
		if (left.Count != right.Count)
		{
			return false;
		}
		HashSet<nint> hashSet = new HashSet<nint>(left.Select((WindowIdentityInfo window) => window.Handle));
		foreach (WindowIdentityInfo item in right)
		{
			if (!hashSet.Remove(item.Handle))
			{
				return false;
			}
		}
		return hashSet.Count == 0;
	}
}
