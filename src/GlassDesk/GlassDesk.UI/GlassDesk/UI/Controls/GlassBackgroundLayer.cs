using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using GlassDesk.Platform;
using GlassDesk.Platform.Displays;

namespace GlassDesk.UI.Controls;

internal sealed class GlassBackgroundLayer : IDisposable
{
	internal sealed record CaptureTiming(long SourceTicks, long StartTicks, long EndTicks, object PixelStatistics);

	private readonly SharedBackdropFeed _compatible;

	private readonly SharedBackdropFeed _wallpaperFeed = new SharedBackdropFeed((ScreenRect region, bool _, Action<CaptureTrace>? trace) => new IsolatedDesktopSceneCapture(region, GlassBackdropSource.Wallpaper, trace));

	private readonly SharedBackdropFeed _desktopOnlyFeed = new SharedBackdropFeed((ScreenRect region, bool _, Action<CaptureTrace>? trace) => DesktopSceneCaptureFactory.CreateDesktopOnly(region, trace));

	private bool _desktopOnly;

	private long _pendingSince;

	private GlassBackgroundFrame? _basic;

	private SamplePixelPool _extensionPool = new SamplePixelPool();

	private bool _sleeping;

	private readonly bool _useOpaqueCaptureFallback;

	private bool _rejectedWallpaper;

	private bool _usingLiveCapture;

	internal CaptureTiming? LastCaptureTiming { get; private set; }

	public static TimeSpan LiveFrameInterval => TimeSpan.FromMilliseconds(16L);

	public static TimeSpan TransitionFrameInterval => LiveFrameInterval;

	internal bool RequiresBasicBackground { get; private set; }

	internal bool UsesTranslucentCaptureFallback => !_useOpaqueCaptureFallback;

	internal bool MustDiscardUnverifiedMaterial
	{
		get
		{
			if (_usingLiveCapture && !_useOpaqueCaptureFallback)
			{
				if (!_desktopOnly)
				{
					if (!_rejectedWallpaper)
					{
						return _compatible.Failure != null;
					}
					return true;
				}
				return _desktopOnlyFeed.Failure != null;
			}
			return false;
		}
	}

	internal GlassBackgroundLayer()
		: this(WindowsCapabilities.BuildNumber)
	{
	}

	internal GlassBackgroundLayer(int windowsBuild, SharedBackdropFeed? compatible = null)
	{
		_useOpaqueCaptureFallback = windowsBuild >= 22000;
		_compatible = compatible ?? new SharedBackdropFeed();
	}

	internal object InspectPerformance()
	{
		return new
		{
			live = _compatible.InspectPerformance(),
			wallpaper = _wallpaperFeed.InspectPerformance(),
			desktopOnly = _desktopOnlyFeed.InspectPerformance(),
			desktopOnlySelected = _desktopOnly
		};
	}

	internal void SetAnimationActive(bool active)
	{
		_compatible.SetAnimationActive(active);
		_wallpaperFeed.SetAnimationActive(active);
		_desktopOnlyFeed.SetAnimationActive(active);
	}

	internal void SetInteractionActive(bool active)
	{
		_compatible.SetInteractionActive(active);
		_wallpaperFeed.SetInteractionActive(active);
		_desktopOnlyFeed.SetInteractionActive(active);
	}

	public GlassBackgroundFrame? Capture(PixelRect region, int width, int height, double radius, bool allowDesktop, bool desktopOnly = false)
	{
		if (_desktopOnly != desktopOnly)
		{
			Suspend();
			_desktopOnly = desktopOnly;
		}
		_usingLiveCapture = allowDesktop;
		if (BackdropRecovery.RequiresBasicRendering)
		{
			RequiresBasicBackground = true;
			_compatible.Suspend();
			_wallpaperFeed.Suspend();
			_desktopOnlyFeed.Suspend();
			return Basic(width, height);
		}
		if (desktopOnly)
		{
			_compatible.Suspend();
			_wallpaperFeed.Suspend();
			RequiresBasicBackground = false;
			if (_desktopOnlyFeed.TryRead(region, width, height, out BackdropFrame frame) && frame != null)
			{
				LastCaptureTiming = Describe(frame);
				return GlassBackgroundFrame.FromCapture(frame);
			}
			return null;
		}
		_desktopOnlyFeed.Suspend();
		if (allowDesktop)
		{
			_wallpaperFeed.Suspend();
		}
		if (allowDesktop && _compatible.TryRead(region, width, height, out BackdropFrame frame2) && frame2 != null)
		{
			if (_useOpaqueCaptureFallback || frame2.Source != GlassBackdropSource.Wallpaper)
			{
				_rejectedWallpaper = false;
				_pendingSince = 0L;
				RequiresBasicBackground = false;
				LastCaptureTiming = Describe(frame2);
				return GlassBackgroundFrame.FromCapture(frame2);
			}
			frame2.Dispose();
			_rejectedWallpaper = true;
		}
		if (allowDesktop)
		{
			if (_pendingSince == 0L)
			{
				_pendingSince = Stopwatch.GetTimestamp();
			}
			RequiresBasicBackground = _useOpaqueCaptureFallback && Stopwatch.GetElapsedTime(_pendingSince) >= TimeSpan.FromSeconds(3L);
			if (!RequiresBasicBackground)
			{
				return null;
			}
			return Basic(width, height);
		}
		_compatible.Suspend();
		if (_wallpaperFeed.TryRead(region, width, height, out BackdropFrame frame3) && frame3 != null)
		{
			RequiresBasicBackground = false;
			LastCaptureTiming = Describe(frame3);
			return GlassBackgroundFrame.FromCapture(frame3);
		}
		RequiresBasicBackground = true;
		return Basic(width, height);
	}

	private static CaptureTiming Describe(BackdropFrame frame)
	{
		ReadOnlySpan<byte> span = frame.Pixels.Span;
		int num = span.Length / 4;
		int num2 = Math.Max(1, num / 512);
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		long num6 = 0L;
		for (int i = 0; i < num; i += num2)
		{
			int num7 = i * 4;
			byte b = Math.Max(span[num7], Math.Max(span[num7 + 1], span[num7 + 2]));
			num3++;
			if (b <= 3)
			{
				num4++;
			}
			if (span[num7 + 3] == 0)
			{
				num5++;
			}
			num6 += span[num7] + span[num7 + 1] + span[num7 + 2];
		}
		return new CaptureTiming(frame.SourceTicks, frame.StartTicks, frame.EndTicks, new
		{
			samples = num3,
			nearBlackFraction = (double)num4 / (double)Math.Max(1, num3),
			transparentFraction = (double)num5 / (double)Math.Max(1, num3),
			meanChannel = (double)num6 / (double)Math.Max(1, num3 * 3)
		});
	}

	private GlassBackgroundFrame Basic(int width, int height)
	{
		if (_basic == null || _basic.Width != width || _basic.Height != height)
		{
			byte[] array = new byte[checked(width * height * 4)];
			for (int i = 0; i < array.Length; i += 4)
			{
				array[i] = 40;
				array[i + 1] = 34;
				array[i + 2] = 30;
				array[i + 3] = byte.MaxValue;
			}
			_basic = GlassBackgroundFrame.FromOwnedPixels(array, width, height, GlassBackdropSource.Basic);
		}
		return _basic;
	}

	public void Suspend()
	{
		_rejectedWallpaper = false;
		SetAnimationActive(active: false);
		_compatible.Suspend();
		_wallpaperFeed.Suspend();
		_desktopOnlyFeed.Suspend();
		_pendingSince = 0L;
		RequiresBasicBackground = false;
		LastCaptureTiming = null;
	}

	public void SetPerformanceSleeping(bool sleeping)
	{
		_rejectedWallpaper = false;
		_sleeping = sleeping;
		if (sleeping)
		{
			_extensionPool.Dispose();
			_extensionPool = new SamplePixelPool();
		}
		_compatible.SetPerformanceSleeping(sleeping);
		_wallpaperFeed.SetPerformanceSleeping(sleeping);
		_desktopOnlyFeed.SetPerformanceSleeping(sleeping);
		_basic = null;
		LastCaptureTiming = null;
		_pendingSince = 0L;
		RequiresBasicBackground = false;
	}

	public void Dispose()
	{
		_extensionPool.Dispose();
		_compatible.Dispose();
		_wallpaperFeed.Dispose();
		_desktopOnlyFeed.Dispose();
		_basic = null;
		LastCaptureTiming = null;
		_pendingSince = 0L;
		RequiresBasicBackground = false;
	}

	internal static GlassBackgroundFrame ExtendCapture(GlassBackgroundFrame frame, PixelRect capturedRegion, PixelRect requestedRegion, int width, int height)
	{
		if (capturedRegion == requestedRegion && frame.Width == width && frame.Height == height)
		{
			return frame;
		}
		byte[] pixels = new byte[checked(width * height * 4)];
		CopyExtendedPixels(frame, capturedRegion, requestedRegion, width, height, pixels);
		return new GlassBackgroundFrame(pixels, width, height, frame.Source, ownsPixels: true)
		{
			Sequence = frame.Sequence,
			CaptureGeneration = frame.CaptureGeneration,
			IsCaptured = frame.IsCaptured
		};
	}

	internal GlassBackgroundFrame? ExtendCapturedFrame(GlassBackgroundFrame frame, PixelRect capturedRegion, PixelRect requestedRegion, int width, int height)
	{
		if (_sleeping)
		{
			return null;
		}
		if (capturedRegion == requestedRegion && frame.Width == width && frame.Height == height)
		{
			return frame;
		}
		SamplePixelLease samplePixelLease = _extensionPool.TryRent(checked(width * height * 4));
		if (samplePixelLease == null)
		{
			return null;
		}
		try
		{
			CopyExtendedPixels(frame, capturedRegion, requestedRegion, width, height, samplePixelLease.Pixels);
			return GlassBackgroundFrame.FromExtensionLease(samplePixelLease, frame, width, height);
		}
		catch
		{
			samplePixelLease.Dispose();
			throw;
		}
	}

	private static void CopyExtendedPixels(GlassBackgroundFrame frame, PixelRect capturedRegion, PixelRect requestedRegion, int width, int height, byte[] pixels)
	{
		int[] array = ArrayPool<int>.Shared.Rent(width);
		try
		{
			for (int i = 0; i < width; i++)
			{
				double num = (double)requestedRegion.X + ((double)i + 0.5) * (double)requestedRegion.Width / (double)width;
				array[i] = Math.Clamp((int)Math.Floor((num - (double)capturedRegion.X) * (double)frame.Width / (double)capturedRegion.Width), 0, frame.Width - 1);
			}
			Span<uint> span = MemoryMarshal.Cast<byte, uint>(frame.Pixels.AsSpan());
			Span<uint> span2 = MemoryMarshal.Cast<byte, uint>(pixels.AsSpan());
			int num2 = -1;
			for (int j = 0; j < height; j++)
			{
				int num3 = Math.Clamp((int)Math.Floor(((double)requestedRegion.Y + ((double)j + 0.5) * (double)requestedRegion.Height / (double)height - (double)capturedRegion.Y) * (double)frame.Height / (double)capturedRegion.Height), 0, frame.Height - 1);
				Span<uint> destination = span2.Slice(j * width, width);
				if (num3 == num2)
				{
					span2.Slice((j - 1) * width, width).CopyTo(destination);
				}
				else
				{
					for (int k = 0; k < width; k++)
					{
						destination[k] = span[num3 * frame.Width + array[k]];
					}
				}
				num2 = num3;
			}
		}
		finally
		{
			ArrayPool<int>.Shared.Return(array);
		}
	}
}
