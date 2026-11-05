using System;
using System.Threading;

namespace GlassDesk.App;

internal sealed class DeferredCommandGate
{
	private int _closed;

	public bool IsClosed => Volatile.Read(in _closed) != 0;

	public void Close()
	{
		Interlocked.Exchange(ref _closed, 1);
	}

	public bool TryRun(Action callback)
	{
		ArgumentNullException.ThrowIfNull(callback, "callback");
		if (IsClosed)
		{
			return false;
		}
		callback();
		return true;
	}
}
