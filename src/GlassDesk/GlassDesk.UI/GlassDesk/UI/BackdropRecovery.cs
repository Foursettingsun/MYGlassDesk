#define TRACE
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.Json;
using System.Threading;
using GlassDesk.Platform;

namespace GlassDesk.UI;

public static class BackdropRecovery
{
	private sealed class IncidentScope(BackdropIncidentRecorder recorder) : IDisposable
	{
		public void Dispose()
		{
			Interlocked.CompareExchange(ref _incidents, null, recorder);
			recorder.Dispose();
		}
	}

	private static long _generation;

	private static int _basicRendering;

	private static readonly CaptureTraceLog Diagnostics = new CaptureTraceLog();

	private static BackdropIncidentRecorder? _incidents;

	internal static bool RequiresBasicRendering => Volatile.Read(in _basicRendering) != 0;

	internal static long Generation => Interlocked.Read(in _generation);

	public static void RetryBackground()
	{
		Volatile.Write(ref _basicRendering, 0);
		Interlocked.Increment(ref _generation);
		Record(new CaptureTrace("Recovery", "RebuildRequested", Stopwatch.GetTimestamp(), 0L, 0L, 0L, 0L, 0, 0L));
	}

	public static void RetryBackground(string trigger)
	{
		Record(new CaptureTrace("RecoveryTrigger", trigger, Stopwatch.GetTimestamp(), 0L, 0L, 0L, 0L, 0, 0L));
		RetryBackground();
	}

	public static void UseBasicRendering()
	{
		if (Interlocked.Exchange(ref _basicRendering, 1) == 0)
		{
			Interlocked.Increment(ref _generation);
			Record(new CaptureTrace("Rendering", "BasicBackgroundRequired", Stopwatch.GetTimestamp(), 0L, 0L, 0L, 0L, 0, 0L));
		}
	}

	internal static void Record(CaptureTrace trace)
	{
		Diagnostics.Observe(trace);
		Volatile.Read(in _incidents)?.Observe(trace);
	}

	internal static IDisposable? BeginAutomaticDiagnostics(string logDirectory)
	{
		try
		{
			BackdropIncidentRecorder backdropIncidentRecorder = new BackdropIncidentRecorder(Path.Combine(logDirectory, "dock-backdrop-incidents"), Stopwatch.GetTimestamp, TimeSpan.FromSeconds(3L));
			Interlocked.Exchange(ref _incidents, backdropIncidentRecorder)?.Dispose();
			return new IncidentScope(backdropIncidentRecorder);
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException) ? true : false)
		{
			Trace.WriteLine("Backdrop diagnostics unavailable: " + ex.GetType().Name);
			return null;
		}
	}

	public static void ExportDiagnosticReport(string path)
	{
		string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "dwmapi.dll");
		var value = new
		{
			timestamp = DateTimeOffset.Now,
			os = WindowsCapabilities.OsVersion.ToString(),
			architecture = RuntimeInformation.ProcessArchitecture.ToString(),
			applicationVersion = FileVersionInfo.GetVersionInfo(typeof(BackdropRecovery).Assembly.Location).FileVersion,
			dwmVersion = (File.Exists(text) ? FileVersionInfo.GetVersionInfo(text).FileVersion : null),
			recoveryGeneration = Generation,
			screenPixelsIncluded = false,
			eventRetentionMinutes = 15,
			note = "Local capture availability and recovery metadata. Counters do not measure displayed FPS.",
			capture = Diagnostics.Snapshot()
		};
		File.WriteAllText(path, JsonSerializer.Serialize(value, new JsonSerializerOptions
		{
			WriteIndented = true
		}));
	}
}
