using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;

internal static class CaptureProcessFamily
{
	private static readonly ConcurrentDictionary<int, byte> Workers = new ConcurrentDictionary<int, byte>();

	private static readonly ConcurrentDictionary<int, byte> PendingRetirement = new ConcurrentDictionary<int, byte>();

	private static readonly object Gate = new object();

	private static int[] _otherProductProcesses = Array.Empty<int>();

	private static long _refreshAt;

	internal static int[] WorkerProcesses => Workers.Keys.Order().ToArray();

	internal static int PendingRetirementCount => PendingRetirement.Count;

	internal static int[] KnownProcesses => Workers.Keys.Concat(Volatile.Read(in _otherProductProcesses)).Append(Environment.ProcessId).Distinct()
		.Order()
		.ToArray();

	internal static void Register(int pid)
	{
		Workers.TryAdd(pid, 0);
	}

	internal static void Unregister(int pid)
	{
		Workers.TryRemove(pid, out var _);
	}

	internal static void Quarantine(int pid)
	{
		PendingRetirement.TryAdd(pid, 0);
	}

	internal static void ConfirmRetirement(int pid)
	{
		PendingRetirement.TryRemove(pid, out var _);
		Unregister(pid);
	}

	internal static void EnsureRetirementConfirmed()
	{
		if (!PendingRetirement.IsEmpty)
		{
			throw new InvalidOperationException("Previous capture worker exit is still pending; new ownership is disabled.");
		}
	}

	internal static int[] Snapshot()
	{
		lock (Gate)
		{
			if (Stopwatch.GetTimestamp() >= _refreshAt)
			{
				_refreshAt = Stopwatch.GetTimestamp() + Stopwatch.Frequency;
				Process[] processesByName = Process.GetProcessesByName("GlassDesk");
				try
				{
					Volatile.Write(ref _otherProductProcesses, processesByName.Select((Process p) => p.Id).ToArray());
				}
				finally
				{
					Process[] array = processesByName;
					for (int num = 0; num < array.Length; num++)
					{
						array[num].Dispose();
					}
				}
			}
		}
		return KnownProcesses;
	}
}
