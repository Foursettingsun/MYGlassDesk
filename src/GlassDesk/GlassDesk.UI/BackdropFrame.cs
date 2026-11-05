using System;
using System.Threading;
using GlassDesk.UI.Controls;

internal sealed class BackdropFrame : IDisposable
{
	private SamplePixelLease? _lease;

	private bool _pooled;

	public BackdropRequest Request { get; }

	public long Sequence { get; }

	internal long ContentRevision { get; set; }

	public long StartTicks { get; }

	public long EndTicks { get; }

	public long SourceTicks { get; }

	public ReadOnlyMemory<byte> Pixels { get; }

	internal bool IsStatic { get; init; }

	internal GlassBackdropSource Source { get; init; } = GlassBackdropSource.LiveDesktop;

	internal BackdropFrame(BackdropRequest request, long sequence, long start, long end, byte[] pixels, long? sourceTicks = null)
		: this(request, sequence, start, end, pixels, ownsPixels: false, sourceTicks)
	{
	}

	private BackdropFrame(BackdropRequest request, long sequence, long start, long end, byte[] pixels, bool ownsPixels, long? sourceTicks)
	{
		if (request.Width <= 0 || request.Height <= 0 || request.Region.Width <= 0 || request.Region.Height <= 0 || (long)request.Width * (long)request.Height > 1000000 || pixels.Length != (long)request.Width * (long)request.Height * 4 || start > end || (sourceTicks.HasValue && sourceTicks.GetValueOrDefault() < 0))
		{
			throw new ArgumentException($"Invalid backdrop extent, timing or pixels: {request.Width}x{request.Height}, start={start}, end={end}, source={sourceTicks}.");
		}
		Request = request;
		Sequence = sequence;
		ContentRevision = sequence;
		StartTicks = start;
		EndTicks = end;
		SourceTicks = sourceTicks ?? end;
		Pixels = (ownsPixels ? pixels : ((byte[])pixels.Clone()));
	}

	internal static BackdropFrame FromOwnedPixels(BackdropRequest request, long sequence, long start, long end, byte[] pixels, long? sourceTicks = null, bool isStatic = false, GlassBackdropSource source = GlassBackdropSource.LiveDesktop)
	{
		return new BackdropFrame(request, sequence, start, end, pixels, ownsPixels: true, sourceTicks)
		{
			IsStatic = isStatic,
			Source = source
		};
	}

	internal static BackdropFrame FromLease(BackdropRequest request, long sequence, long start, long end, SamplePixelLease lease, long? sourceTicks = null, bool isStatic = false, GlassBackdropSource source = GlassBackdropSource.LiveDesktop)
	{
		try
		{
			return new BackdropFrame(request, sequence, start, end, lease.Pixels, ownsPixels: true, sourceTicks)
			{
				_lease = lease,
				_pooled = true,
				IsStatic = isStatic,
				Source = source
			};
		}
		catch
		{
			lease.Dispose();
			throw;
		}
	}

	internal BackdropFrame Acquire()
	{
		if (!_pooled)
		{
			return this;
		}
		BackdropFrame backdropFrame = FromLease(Request, Sequence, StartTicks, EndTicks, (_lease ?? throw new ObjectDisposedException("BackdropFrame")).Acquire(), SourceTicks, IsStatic, Source);
		backdropFrame.ContentRevision = ContentRevision;
		return backdropFrame;
	}

	public void Dispose()
	{
		Interlocked.Exchange(ref _lease, null)?.Dispose();
	}
}
