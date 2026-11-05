using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace GlassDesk.UI.Controls;

internal sealed class GlassBackgroundFrame : IDisposable
{
	private IDisposable? _lease;

	public byte[] Pixels { get; }

	public int Width { get; }

	public int Height { get; }

	public GlassBackdropSource Source { get; }

	public long Sequence { get; internal init; }

	public long CaptureGeneration { get; internal init; }

	public bool IsCaptured { get; internal init; }

	public GlassBackgroundFrame(byte[] pixels, int width, int height, GlassBackdropSource source)
		: this(pixels, width, height, source, ownsPixels: false)
	{
	}

	internal GlassBackgroundFrame(byte[] pixels, int width, int height, GlassBackdropSource source, bool ownsPixels)
	{
		ArgumentNullException.ThrowIfNull(pixels, "pixels");
		if (width <= 0 || height <= 0 || pixels.Length != (long)width * (long)height * 4)
		{
			throw new ArgumentException("Invalid BGRA background frame.", "pixels");
		}
		Pixels = (ownsPixels ? pixels : ((byte[])pixels.Clone()));
		Width = width;
		Height = height;
		Source = source;
	}

	internal static GlassBackgroundFrame FromOwnedPixels(byte[] pixels, int width, int height, GlassBackdropSource source)
	{
		return new GlassBackgroundFrame(pixels, width, height, source, ownsPixels: true);
	}

	internal static GlassBackgroundFrame FromCapture(BackdropFrame captured)
	{
		if (!MemoryMarshal.TryGetArray(captured.Pixels, out var segment) || segment.Offset != 0 || segment.Count != segment.Array.Length)
		{
			using (captured)
			{
				return FromOwnedPixels(captured.Pixels.ToArray(), captured.Request.Width, captured.Request.Height, captured.Source);
			}
		}
		return new GlassBackgroundFrame(segment.Array, captured.Request.Width, captured.Request.Height, captured.Source, ownsPixels: true)
		{
			_lease = captured,
			Sequence = captured.ContentRevision,
			CaptureGeneration = captured.Request.Generation,
			IsCaptured = true
		};
	}

	public void Dispose()
	{
		Interlocked.Exchange(ref _lease, null)?.Dispose();
	}

	internal static GlassBackgroundFrame FromExtensionLease(SamplePixelLease lease, GlassBackgroundFrame original, int width, int height)
	{
		return new GlassBackgroundFrame(lease.Pixels, width, height, original.Source, ownsPixels: true)
		{
			_lease = lease,
			Sequence = original.Sequence,
			CaptureGeneration = original.CaptureGeneration,
			IsCaptured = original.IsCaptured
		};
	}
}
