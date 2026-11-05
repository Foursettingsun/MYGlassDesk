using System.Diagnostics;

internal sealed class BackdropProviderPolicy
{
	private int _failures;

	private long _retryAt;

	internal bool RetryAllowed(long now)
	{
		return now >= _retryAt;
	}

	internal void RecordFailure(long now)
	{
		_retryAt = now + ++_failures switch
		{
			1 => 1, 
			2 => 2, 
			3 => 5, 
			4 => 10, 
			_ => 30, 
		} * Stopwatch.Frequency;
	}

	internal void RecordSuccess()
	{
		_failures = 0;
		_retryAt = 0L;
	}
}
