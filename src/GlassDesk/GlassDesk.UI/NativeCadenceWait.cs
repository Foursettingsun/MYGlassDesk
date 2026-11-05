using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

internal sealed class NativeCadenceWait : ICadenceWait, IDisposable
{
	private readonly SafeWaitHandle timer;

	internal NativeCadenceWait()
	{
		timer = new SafeWaitHandle(CreateWaitableTimerExW(0, null, 2u, 2031619u), ownsHandle: true);
		if (timer.IsInvalid)
		{
			timer.Dispose();
			throw new Win32Exception(Marshal.GetLastWin32Error(), "High-resolution waitable timer unavailable.");
		}
	}

	public void Wait(double milliseconds, CancellationToken cancellation)
	{
		if (!cancellation.IsCancellationRequested && !(milliseconds <= 0.0))
		{
			long due = -Math.Max(1L, (long)Math.Ceiling(milliseconds * 10000.0));
			if (!SetWaitableTimer(timer, ref due, 0, 0, 0, resume: false))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
			nint[] array = ((!cancellation.CanBeCanceled) ? new nint[1] { timer.DangerousGetHandle() } : new nint[2]
			{
				cancellation.WaitHandle.SafeWaitHandle.DangerousGetHandle(),
				timer.DangerousGetHandle()
			});
			if (WaitForMultipleObjects((uint)array.Length, array, waitAll: false, uint.MaxValue) == uint.MaxValue)
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
		}
	}

	public void Dispose()
	{
		timer.Dispose();
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern nint CreateWaitableTimerExW(nint attributes, string? name, uint flags, uint access);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool SetWaitableTimer(SafeWaitHandle timer, ref long due, int period, nint callback, nint argument, bool resume);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern uint WaitForMultipleObjects(uint count, nint[] handles, bool waitAll, uint timeout);
}
