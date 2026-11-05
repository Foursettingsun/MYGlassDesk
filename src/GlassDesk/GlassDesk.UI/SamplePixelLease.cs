using System;

internal sealed class SamplePixelLease(SamplePixelOwner owner) : IDisposable
{
	private SamplePixelOwner? _owner = owner;

	internal byte[] Pixels => _owner?.Pixels ?? throw new ObjectDisposedException("SamplePixelLease");

	internal SamplePixelLease Acquire()
	{
		SamplePixelOwner samplePixelOwner = _owner ?? throw new ObjectDisposedException("SamplePixelLease");
		lock (samplePixelOwner.Gate)
		{
			if (samplePixelOwner != _owner)
			{
				throw new ObjectDisposedException("SamplePixelLease");
			}
			samplePixelOwner.References++;
			return new SamplePixelLease(samplePixelOwner);
		}
	}

	public void Dispose()
	{
		SamplePixelOwner owner = _owner;
		if (owner == null)
		{
			return;
		}
		lock (owner.Gate)
		{
			if (owner == _owner)
			{
				_owner = null;
				owner.References--;
			}
		}
	}
}
