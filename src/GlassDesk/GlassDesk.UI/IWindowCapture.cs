using System;
using System.Threading;

internal interface IWindowCapture : IDisposable
{
	CapturePixels? TryRead(long notBeforeQpcTicks = 0L, TimeSpan waitForFrame = default(TimeSpan), CancellationToken cancellation = default(CancellationToken), bool reusePixels = false);

	void RefreshSessionAtSceneBoundary();

	void RetireQueuedFramesAtSceneBoundary(long barrierTicks);

	void ReleaseReadbackBuffers();
}
