using System;
using System.Threading;

internal interface IDesktopSceneCapture : IDisposable
{
	nint CarrierHandle { get; }

	bool AllowsStaticContent => false;

	bool SupportsSampling => false;

	long SceneRevision => 0L;

	bool UpdateExclusions();

	CapturePixels? TryRead(TimeSpan waitForFrame = default(TimeSpan), CancellationToken cancellation = default(CancellationToken), long sourceNotBeforeTicks = 0L, bool reusePixels = false);

	void ReleaseReadbackBuffers();

	CapturePixels? TryReadSampled(PixelBounds crop, int width, int height, byte[] target, TimeSpan waitForFrame = default(TimeSpan), CancellationToken cancellation = default(CancellationToken), long sourceNotBeforeTicks = 0L)
	{
		throw new NotSupportedException("Provider does not support sampled acquisition.");
	}
}
