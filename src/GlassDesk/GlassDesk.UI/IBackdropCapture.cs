using System;

internal interface IBackdropCapture : IDisposable
{
	BackdropFrame Capture(BackdropRequest request, long sequence);
}
