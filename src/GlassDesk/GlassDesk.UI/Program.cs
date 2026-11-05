using System;
using System.Windows.Threading;

internal static class Program
{
	internal static void Pump(TimeSpan duration)
	{
		DispatcherFrame frame = new DispatcherFrame();
		DispatcherTimer dispatcherTimer = new DispatcherTimer(DispatcherPriority.Background)
		{
			Interval = duration
		};
		dispatcherTimer.Tick += (object? _, EventArgs _) =>
		{
			frame.Continue = false;
		};
		dispatcherTimer.Start();
		try
		{
			Dispatcher.PushFrame(frame);
		}
		finally
		{
			dispatcherTimer.Stop();
		}
	}
}
