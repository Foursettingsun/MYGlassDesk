using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace GlassDesk.UI.Controls;

internal sealed class GlassFrameWorker
{
	private int _busy;

	private long _generation;

	private byte[]? _pixels;

	private byte[]? _scratch;

	internal bool IsBusy => Volatile.Read(in _busy) != 0;

	internal void Invalidate()
	{
		Interlocked.Increment(ref _generation);
	}

	internal bool IsCurrent(GlassFrameResult result)
	{
		return result.Generation == Interlocked.Read(in _generation);
	}

	internal void ClearCachedBuffers()
	{
		Interlocked.Exchange(ref _pixels, null);
		Interlocked.Exchange(ref _scratch, null);
	}

	internal async Task<GlassFrameResult?> RenderBufferedAsync(int length, Action<byte[], byte[]> render)
	{
		if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
		{
			return null;
		}
		long generation = Interlocked.Read(in _generation);
		bool leased = false;
		try
		{
			byte[] pixels = _pixels;
			byte[] array = pixels;
			if (array == null || array.Length != length)
			{
				_pixels = (pixels = new byte[length]);
			}
			byte[] scratch = _scratch;
			byte[] array2 = scratch;
			if (array2 == null || array2.Length != length)
			{
				_scratch = (scratch = new byte[length]);
			}
			GlassFrameResult result = await Task.Run(() =>
			{
				long timestamp = Stopwatch.GetTimestamp();
				render(pixels, scratch);
				return new GlassFrameResult(pixels, Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds, generation, () =>
				{
					Volatile.Write(ref _busy, 0);
				});
			}).ConfigureAwait(continueOnCapturedContext: false);
			if (generation != Interlocked.Read(in _generation))
			{
				return null;
			}
			leased = true;
			return result;
		}
		finally
		{
			if (!leased)
			{
				Volatile.Write(ref _busy, 0);
			}
		}
	}

	internal async Task<GlassFrameResult?> RenderAsync(Func<byte[]> render)
	{
		if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
		{
			return null;
		}
		long generation = Interlocked.Read(in _generation);
		try
		{
			GlassFrameResult glassFrameResult = await Task.Run(() =>
			{
				long timestamp = Stopwatch.GetTimestamp();
				return new GlassFrameResult(render(), Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds, generation);
			}).ConfigureAwait(continueOnCapturedContext: false);
			return (generation == Interlocked.Read(in _generation)) ? glassFrameResult : null;
		}
		finally
		{
			Volatile.Write(ref _busy, 0);
		}
	}
}
