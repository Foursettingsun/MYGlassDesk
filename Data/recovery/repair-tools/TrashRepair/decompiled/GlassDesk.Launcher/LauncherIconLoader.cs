#define TRACE
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace GlassDesk.Launcher;

internal sealed class LauncherIconLoader : IDisposable
{
	private sealed class Entry
	{
		internal readonly TaskCompletionSource<BitmapImage?> Completion = new TaskCompletionSource<BitmapImage>(TaskCreationOptions.RunContinuationsAsynchronously);

		internal LinkedListNode<string>? Node;

		internal long Bytes;
	}

	private sealed record Work(string Key, Func<BitmapImage?> Resolve, Entry Entry);

	private readonly object _gate = new object();

	private readonly Dictionary<string, Entry> _cache = new Dictionary<string, Entry>(StringComparer.Ordinal);

	private readonly LinkedList<string> _lru = new LinkedList<string>();

	private readonly long _budget;

	private long _decodedBytes;

	private readonly BlockingCollection<Work> _queue = new BlockingCollection<Work>(512);

	private readonly Thread _thread;

	private bool _disposed;

	private bool _suspended;

	internal LauncherIconLoader()
		: this(16777216L)
	{
	}

	internal LauncherIconLoader(long budgetBytes)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(budgetBytes, "budgetBytes");
		_budget = budgetBytes;
		_thread = new Thread(Run)
		{
			IsBackground = true,
			Name = "GlassDesk launcher icons"
		};
		_thread.SetApartmentState(ApartmentState.STA);
		_thread.Start();
	}

	internal Task<BitmapImage?> LoadAsync(string key, Func<BitmapImage?> resolve)
	{
		lock (_gate)
		{
			if (_disposed)
			{
				return Task.FromResult<BitmapImage>(null);
			}
			if (_cache.TryGetValue(key, out Entry value))
			{
				Touch(value);
				return value.Completion.Task;
			}
			Entry entry = new Entry();
			if (!_queue.TryAdd(new Work(key, resolve, entry)))
			{
				return Task.FromResult<BitmapImage>(null);
			}
			_cache[key] = entry;
			return entry.Completion.Task;
		}
	}

	internal void SetSuspended(bool suspended)
	{
		lock (_gate)
		{
			_suspended = suspended;
			Monitor.PulseAll(_gate);
		}
	}

	internal (long Bytes, int Cached, int Pending, bool Suspended) InspectCache()
	{
		lock (_gate)
		{
			return (Bytes: _decodedBytes, Cached: _cache.Count, Pending: _queue.Count, Suspended: _suspended);
		}
	}

	internal int PreloadCapacity(int maximumPixels)
	{
		return (int)Math.Clamp(_budget / (Math.Max(1L, maximumPixels) * Math.Max(1L, maximumPixels) * 4), 1L, 256L);
	}

	internal void Clear()
	{
		lock (_gate)
		{
			_cache.Clear();
			_lru.Clear();
			_decodedBytes = 0L;
		}
	}

	private void Touch(Entry entry)
	{
		LinkedListNode<string> node = entry.Node;
		if (node != null)
		{
			_lru.Remove(node);
			_lru.AddLast(node);
		}
	}

	private void Trim()
	{
		while (_decodedBytes > _budget || _lru.Count > 256)
		{
			LinkedListNode<string> first = _lru.First;
			if (first != null)
			{
				_lru.RemoveFirst();
				if (_cache.Remove(first.Value, out Entry value))
				{
					_decodedBytes -= value.Bytes;
				}
				continue;
			}
			break;
		}
	}

	private void Run()
	{
		foreach (Work item in _queue.GetConsumingEnumerable())
		{
			lock (_gate)
			{
				while (_suspended && !_disposed)
				{
					Monitor.Wait(_gate);
				}
				if (_disposed)
				{
					item.Entry.Completion.TrySetResult(null);
					continue;
				}
			}
			BitmapImage bitmapImage = null;
			try
			{
				bitmapImage = item.Resolve();
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Launcher icon unavailable: " + ex.Message);
			}
			lock (_gate)
			{
				if (_disposed)
				{
					bitmapImage = null;
				}
				if (_cache.TryGetValue(item.Key, out Entry value) && value == item.Entry)
				{
					if (bitmapImage == null)
					{
						_cache.Remove(item.Key);
					}
					else
					{
						value.Bytes = (long)bitmapImage.PixelWidth * (long)bitmapImage.PixelHeight * 4;
						_decodedBytes += value.Bytes;
						value.Node = _lru.AddLast(item.Key);
						Trim();
					}
				}
				item.Entry.Completion.TrySetResult(bitmapImage);
			}
		}
		lock (_gate)
		{
			_queue.Dispose();
		}
	}

	public void Dispose()
	{
		lock (_gate)
		{
			if (!_disposed)
			{
				_disposed = true;
				_cache.Clear();
				_lru.Clear();
				_decodedBytes = 0L;
				_queue.CompleteAdding();
				Monitor.PulseAll(_gate);
				Work item;
				while (_queue.TryTake(out item))
				{
					item.Entry.Completion.TrySetResult(null);
				}
			}
		}
	}
}
