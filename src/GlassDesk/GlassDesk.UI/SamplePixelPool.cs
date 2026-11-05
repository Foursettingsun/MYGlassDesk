using System;
using System.Collections.Generic;
using System.Linq;

internal sealed class SamplePixelPool : IDisposable
{
	private readonly object _gate = new object();

	private readonly List<SamplePixelOwner> _slots = new List<SamplePixelOwner>();

	private int _size;

	private bool _disposed;

	internal SamplePixelLease? TryRent(int bytes)
	{
		if (bytes <= 0 || bytes > 4000000 || bytes % 4 != 0)
		{
			throw new ArgumentOutOfRangeException("bytes");
		}
		lock (_gate)
		{
			if (_disposed)
			{
				return null;
			}
			if (_size != bytes)
			{
				_slots.Clear();
				_size = bytes;
			}
			SamplePixelOwner samplePixelOwner = _slots.FirstOrDefault((SamplePixelOwner value) => value.References == 0);
			if (samplePixelOwner == null)
			{
				if (_slots.Count == 3)
				{
					return null;
				}
				samplePixelOwner = new SamplePixelOwner(new byte[bytes], _gate);
				_slots.Add(samplePixelOwner);
			}
			samplePixelOwner.References++;
			return new SamplePixelLease(samplePixelOwner);
		}
	}

	public void Dispose()
	{
		lock (_gate)
		{
			_disposed = true;
			_slots.Clear();
			_size = 0;
		}
	}
}
