using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using GlassDesk.UI.Controls;

internal sealed class WallpaperSceneCapture(ScreenRect region) : IDesktopSceneCapture, IDisposable
{
	private WallpaperBackdrop _wallpaper = new WallpaperBackdrop();

	private CapturePixels? _frame;

	private bool _delivered;

	private bool _disposed;

	public nint CarrierHandle => 0;

	public bool AllowsStaticContent => true;

	public long SceneRevision => _frame?.SourceQpcTicks ?? 0;

	public bool UpdateExclusions()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (!_wallpaper.Update(new Rect(region.X, region.Y, region.Width, region.Height), region.Width, region.Height, 0.0))
		{
			return false;
		}
		byte[] array = _wallpaper.Pixels ?? throw new InvalidOperationException("Wallpaper pixels unavailable.");
		_frame = new CapturePixels(region.Width, region.Height, (byte[])array.Clone())
		{
			SourceQpcTicks = Stopwatch.GetTimestamp(),
			IsStatic = true,
			Source = GlassBackdropSource.Wallpaper
		};
		_delivered = false;
		return true;
	}

	public CapturePixels? TryRead(TimeSpan waitForFrame = default(TimeSpan), CancellationToken cancellation = default(CancellationToken), long sourceNotBeforeTicks = 0L, bool reusePixels = false)
	{
		cancellation.ThrowIfCancellationRequested();
		if ((object)_frame != null && _frame.SourceQpcTicks < sourceNotBeforeTicks)
		{
			_wallpaper = new WallpaperBackdrop();
		}
		UpdateExclusions();
		if (_delivered || (object)_frame == null)
		{
			return null;
		}
		_delivered = true;
		return _frame;
	}

	public void ReleaseReadbackBuffers()
	{
		_frame = null;
		_wallpaper = new WallpaperBackdrop();
		_delivered = false;
	}

	public void Dispose()
	{
		_disposed = true;
		_frame = null;
	}
}
