#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GlassDesk.UI;

internal sealed class BackdropIncidentRecorder : IDisposable
{
	private sealed record Incident(long AtTicks, CaptureTrace[] Events, TaskCompletionSource Completion);

	private readonly object _gate = new object();

	private readonly string _directory;

	private readonly Func<long> _clock;

	private readonly long _postTicks;

	private readonly Queue<CaptureTrace> _recent = new Queue<CaptureTrace>();

	private CaptureTrace? _lastTrace;

	private readonly Channel<Incident> _writes = Channel.CreateBounded<Incident>(new BoundedChannelOptions(2)
	{
		SingleReader = true,
		FullMode = BoundedChannelFullMode.Wait
	});

	private readonly Timer _timer;

	private readonly Task _writer;

	private Task _lastWrite = Task.CompletedTask;

	private long _incidentAt;

	private long _incidentEnd;

	private bool _pending;

	private bool _disposed;

	internal BackdropIncidentRecorder(string directory, Func<long> clock, TimeSpan postWindow)
	{
		_directory = Path.GetFullPath(directory);
		_clock = clock;
		_postTicks = (long)(postWindow.TotalSeconds * (double)Stopwatch.Frequency);
		Directory.CreateDirectory(_directory);
		_writer = Task.Run((Func<Task?>)WriteLoop);
		_timer = new Timer((object? _) =>
		{
			FlushReady();
		}, null, 250, 250);
	}

	internal void Observe(CaptureTrace trace)
	{
		if (!CaptureTraceLog.IsStateEvent(trace))
		{
			return;
		}
		lock (_gate)
		{
			if (_disposed)
			{
				return;
			}
			long num = _clock();
			CloseReady(num);
			Trim(num);
			CaptureTrace? lastTrace = _lastTrace;
			if (lastTrace.HasValue)
			{
				CaptureTrace valueOrDefault = lastTrace.GetValueOrDefault();
				if (valueOrDefault.Stage == trace.Stage && valueOrDefault.Reason == trace.Reason && valueOrDefault.Generation == trace.Generation && valueOrDefault.SurfaceId == trace.SurfaceId && trace.AtTicks - valueOrDefault.AtTicks < Stopwatch.Frequency / 4)
				{
					return;
				}
			}
			if (_recent.Count == 1024)
			{
				_recent.Dequeue();
			}
			_recent.Enqueue(trace);
			_lastTrace = trace;
			if (!_pending && IsTrigger(trace))
			{
				_pending = true;
				_incidentAt = num;
				_incidentEnd = num + _postTicks;
			}
		}
	}

	private static bool IsTrigger(CaptureTrace trace)
	{
		bool flag;
		switch (trace.Stage)
		{
		case "CaptureFailure":
		case "MaterialLoss":
		case "VisualSwap":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (!flag && (!(trace.Stage == "UIScene") || !(trace.Reason == "UnknownAfterRetry")) && (!(trace.Stage == "Rendering") || !(trace.Reason == "BasicBackgroundRequired")))
		{
			if (trace.Stage == "NativePixels")
			{
				if (!(trace.TransparentFraction > 0.0))
				{
					return trace.NearBlackFraction >= 0.95;
				}
				return true;
			}
			return false;
		}
		return true;
	}

	private void Trim(long now)
	{
		long num = (_pending ? _incidentAt : now) - Stopwatch.Frequency * 30;
		CaptureTrace result;
		while (_recent.TryPeek(out result) && result.AtTicks < num)
		{
			_recent.Dequeue();
		}
	}

	internal void FlushReady()
	{
		lock (_gate)
		{
			if (!_disposed)
			{
				CloseReady(_clock());
			}
		}
	}

	private void CloseReady(long now)
	{
		if (_pending && now >= _incidentEnd)
		{
			EnqueueIncident();
		}
	}

	private void EnqueueIncident()
	{
		TaskCompletionSource taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		long start = _incidentAt - Stopwatch.Frequency * 30;
		Incident item = new Incident(_incidentAt, _recent.Where((CaptureTrace e) => e.AtTicks >= start && e.AtTicks <= _incidentEnd).ToArray(), taskCompletionSource);
		if (_writes.Writer.TryWrite(item))
		{
			_lastWrite = taskCompletionSource.Task;
		}
		else
		{
			taskCompletionSource.SetResult();
		}
		_pending = false;
	}

	internal Task WaitForWritesAsync()
	{
		lock (_gate)
		{
			return _lastWrite;
		}
	}

	private async Task WriteLoop()
	{
		await foreach (Incident incident in _writes.Reader.ReadAllAsync())
		{
			try
			{
				string path = "dock-backdrop-" + DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssfff") + "-" + Guid.NewGuid().ToString("N") + ".json";
				string path2 = Path.Combine(_directory, path);
				var value = new
				{
					timestamp = DateTimeOffset.Now,
					processId = Environment.ProcessId,
					applicationVersion = FileVersionInfo.GetVersionInfo(typeof(BackdropIncidentRecorder).Assembly.Location).FileVersion,
					qpcFrequency = Stopwatch.Frequency,
					incidentAtTicks = incident.AtTicks,
					preWindowSeconds = 30,
					postWindowSeconds = (double)_postTicks / (double)Stopwatch.Frequency,
					screenPixelsIncluded = false,
					windowTitlesIncluded = false,
					note = "State and sampled pixel statistics only. A dark scene is valid content. These events do not measure displayed FPS.",
					events = incident.Events
				};
				await File.WriteAllBytesAsync(path2 + ".tmp", JsonSerializer.SerializeToUtf8Bytes(value));
				File.Move(path2 + ".tmp", path2);
				Rotate();
			}
			catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException) ? true : false)
			{
				Trace.WriteLine("Backdrop incident write unavailable: " + ex.GetType().Name);
			}
			finally
			{
				incident.Completion.TrySetResult();
			}
		}
	}

	private void Rotate()
	{
		FileInfo[] array = new DirectoryInfo(_directory).GetFiles("dock-backdrop-*.json").OrderByDescending((FileInfo f) => f.Name, StringComparer.Ordinal).ToArray();
		long num = 0L;
		for (int num2 = 0; num2 < array.Length; num2++)
		{
			num += array[num2].Length;
			if (num2 >= 10 || num > 20971520)
			{
				array[num2].Delete();
			}
		}
	}

	public void Dispose()
	{
		lock (_gate)
		{
			if (!_disposed)
			{
				_disposed = true;
				_timer.Dispose();
				if (_pending)
				{
					EnqueueIncident();
				}
				_writes.Writer.TryComplete();
			}
		}
	}
}
