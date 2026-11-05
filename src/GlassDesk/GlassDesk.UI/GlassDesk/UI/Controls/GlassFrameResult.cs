using System;
using System.Threading;

namespace GlassDesk.UI.Controls;

internal sealed class GlassFrameResult(byte[] pixels, double milliseconds, long generation, Action? release = null) : IDisposable
{
	private Action? _release = release;

	internal byte[] Pixels { get; } = pixels;

	internal double Milliseconds { get; } = milliseconds;

	internal long Generation { get; } = generation;

	public void Dispose()
	{
		Interlocked.Exchange(ref _release, null)?.Invoke();
	}
}
