using System;
using System.Threading;

internal interface ICadenceWait : IDisposable
{
	void Wait(double milliseconds, CancellationToken cancellation);
}
