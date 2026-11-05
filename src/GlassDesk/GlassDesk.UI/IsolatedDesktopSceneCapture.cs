using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GlassDesk.UI.Controls;

internal sealed class IsolatedDesktopSceneCapture : IDesktopSceneCapture, IDisposable
{
	private readonly Process _process;

	private readonly Stream _input;

	private readonly Stream _output;

	private readonly ScreenRect _region;

	private readonly GlassBackdropSource _source;

	private SharedCapturePixels? _sharedPixels;

	private byte[]? _pixels;

	private bool _disposed;

	private long _sequence;

	private long _generation = 1L;

	private long _lastBarrier;

	private readonly Action<CaptureTrace>? _trace;

	private long _nextTracePoll;

	public bool AllowsStaticContent => true;

	public bool SupportsSampling => true;

	public long SceneRevision { get; private set; }

	internal nint CarrierHandle { get; private set; }

	nint IDesktopSceneCapture.CarrierHandle => CarrierHandle;

	internal IsolatedDesktopSceneCapture(ScreenRect region, Action<CaptureTrace>? trace = null, string? executable = null)
		: this(region, Start(executable), GlassBackdropSource.LiveDesktop, trace)
	{
	}

	internal IsolatedDesktopSceneCapture(ScreenRect region, bool documented, Action<CaptureTrace>? trace, string? executable = null)
		: this(region, Start(executable), documented ? GlassBackdropSource.CompatibleScene : GlassBackdropSource.LiveDesktop, trace)
	{
	}

	internal IsolatedDesktopSceneCapture(ScreenRect region, GlassBackdropSource source, Action<CaptureTrace>? trace, string? executable = null)
		: this(region, Start(executable), source, trace)
	{
	}

	internal IsolatedDesktopSceneCapture(ScreenRect region, GlassBackdropSource source, Action<CaptureTrace>? trace, string? executable, bool desktopOnly, bool documentedDesktop = false)
		: this(region, Start(executable), source, trace, desktopOnly, documentedDesktop)
	{
	}

	internal IsolatedDesktopSceneCapture(ScreenRect region, ProcessStartInfo start)
		: this(region, start, GlassBackdropSource.LiveDesktop, null)
	{
	}

	private static ProcessStartInfo Start(string? executable)
	{
		return new ProcessStartInfo(executable ?? Environment.ProcessPath ?? throw new InvalidOperationException("Cannot locate capture executable."));
	}

	private IsolatedDesktopSceneCapture(ScreenRect region, ProcessStartInfo start, GlassBackdropSource source, Action<CaptureTrace>? trace, bool desktopOnly = false, bool documentedDesktop = false)
	{
		IsolatedDesktopSceneCapture isolatedDesktopSceneCapture = this;
		if (region.Width <= 0 || region.Height <= 0 || (long)region.Width * (long)region.Height > 32000000)
		{
			throw new ArgumentOutOfRangeException("region");
		}
		GlassBackdropSource glassBackdropSource = source;
		if (((uint)(glassBackdropSource - 1) > 1u && glassBackdropSource != GlassBackdropSource.CompatibleScene) || 1 == 0)
		{
			throw new ArgumentOutOfRangeException("source");
		}
		_region = region;
		_source = source;
		_trace = trace;
		start.UseShellExecute = false;
		start.RedirectStandardInput = true;
		start.RedirectStandardOutput = true;
		start.CreateNoWindow = true;
		start.WindowStyle = ProcessWindowStyle.Hidden;
		start.ArgumentList.Add("--glass-backdrop-worker");
		start.ArgumentList.Add(Environment.ProcessId.ToString());
		CaptureProcessFamily.EnsureRetirementConfirmed();
		_process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start capture worker.");
		CaptureProcessFamily.Register(_process.Id);
		_input = _process.StandardInput.BaseStream;
		_output = _process.StandardOutput.BaseStream;
		try
		{
			_sharedPixels = SharedCapturePixels.Create(4000000);
			using CancellationTokenSource cancellationTokenSource = Deadline();
			Send((BinaryWriter writer) =>
			{
				writer.Write(1195655987);
				writer.Write(5);
				writer.Write((byte)((!desktopOnly) ? ((source != GlassBackdropSource.LiveDesktop) ? ((source == GlassBackdropSource.CompatibleScene) ? 1 : 2) : 0) : (documentedDesktop ? 4 : 3)));
				writer.Write(region.X);
				writer.Write(region.Y);
				writer.Write(region.Width);
				writer.Write(region.Height);
				writer.Write(isolatedDesktopSceneCapture._sharedPixels.Name);
				writer.Write(isolatedDesktopSceneCapture._sharedPixels.Capacity);
				WriteFamily(writer);
			}, cancellationTokenSource.Token);
			if (Status(cancellationTokenSource.Token) != 1)
			{
				throw new InvalidDataException("Capture worker did not initialize.");
			}
			if (BitConverter.ToInt32(Read(4, cancellationTokenSource.Token)) != 5)
			{
				throw new InvalidDataException("Capture worker protocol version does not match the application.");
			}
			CarrierHandle = (nint)BitConverter.ToInt64(Read(8, cancellationTokenSource.Token));
			trace?.Invoke(new CaptureTrace("BackdropProvider", source.ToString(), Stopwatch.GetTimestamp(), 0L, 0L, 0L, 0L, 0, 0L));
		}
		catch
		{
			Dispose();
			throw;
		}
	}

	internal bool UpdateExclusions()
	{
		using CancellationTokenSource cancellationTokenSource = Deadline();
		long sequence = ++_sequence;
		Command(3, sequence, cancellationTokenSource.Token);
		bool flag = Status(cancellationTokenSource.Token) == 1;
		ValidateReply(sequence, cancellationTokenSource.Token);
		if (flag)
		{
			_generation++;
		}
		return flag;
	}

	bool IDesktopSceneCapture.UpdateExclusions()
	{
		return UpdateExclusions();
	}

	internal CapturePixels? TryRead(TimeSpan waitForFrame = default(TimeSpan), CancellationToken cancellation = default(CancellationToken), long sourceNotBeforeTicks = 0L, bool reusePixels = false)
	{
		int num = checked(_region.Width * _region.Height * 4);
		byte[] array = (reusePixels ? (_pixels ?? (_pixels = new byte[num])) : new byte[num]);
		if (num <= 4000000)
		{
			return TryReadSampled(new PixelBounds(0, 0, _region.Width, _region.Height), _region.Width, _region.Height, array, waitForFrame, cancellation, sourceNotBeforeTicks);
		}
		int num2 = Math.Max(1, 1000000 / _region.Width);
		byte[] array2 = new byte[checked(_region.Width * Math.Min(num2, _region.Height) * 4)];
		long num3 = 0L;
		for (int i = 0; i < _region.Height; i += num2)
		{
			int num4 = Math.Min(num2, _region.Height - i);
			if (array2.Length != _region.Width * num4 * 4)
			{
				array2 = new byte[_region.Width * num4 * 4];
			}
			CapturePixels capturePixels = ReadSampled(new PixelBounds(0, i, _region.Width, i + num4), _region.Width, num4, array2, waitForFrame, cancellation, sourceNotBeforeTicks, num3);
			if ((object)capturePixels == null)
			{
				return null;
			}
			Buffer.BlockCopy(array2, 0, array, checked(i * _region.Width * 4), array2.Length);
			num3 = capturePixels.SourceQpcTicks;
		}
		return new CapturePixels(_region.Width, _region.Height, array)
		{
			SourceQpcTicks = num3,
			IsStatic = true,
			Source = _source
		};
	}

	internal CapturePixels? TryReadSampled(PixelBounds crop, int width, int height, byte[] target, TimeSpan waitForFrame = default(TimeSpan), CancellationToken cancellation = default(CancellationToken), long sourceNotBeforeTicks = 0L)
	{
		return ReadSampled(crop, width, height, target, waitForFrame, cancellation, sourceNotBeforeTicks, 0L);
	}

	private CapturePixels? ReadSampled(PixelBounds crop, int width, int height, byte[] target, TimeSpan waitForFrame, CancellationToken cancellation, long sourceNotBeforeTicks, long cachedSourceTicks = 0L)
	{
		if (crop.Left < 0 || crop.Top < 0 || crop.Right > _region.Width || crop.Bottom > _region.Height || crop.Width <= 0 || crop.Height <= 0 || width <= 0 || height <= 0 || (long)width * (long)height > 1000000 || target.Length != (long)width * (long)height * 4)
		{
			throw new ArgumentException("Invalid sampled capture geometry.");
		}
		cancellation.ThrowIfCancellationRequested();
		if (sourceNotBeforeTicks > _lastBarrier)
		{
			_lastBarrier = sourceNotBeforeTicks;
			_generation++;
		}
		using CancellationTokenSource cancellationTokenSource = Deadline(cancellation);
		long sequence = ++_sequence;
		Command((byte)((cachedSourceTicks == 0L) ? 1 : 4), sequence, cancellationTokenSource.Token, (BinaryWriter writer) =>
		{
			writer.Write(sourceNotBeforeTicks);
			writer.Write((int)Math.Clamp(waitForFrame.TotalMilliseconds, 0.0, 100.0));
			writer.Write(crop.Left);
			writer.Write(crop.Top);
			writer.Write(crop.Right);
			writer.Write(crop.Bottom);
			writer.Write(width);
			writer.Write(height);
			if (cachedSourceTicks != 0L)
			{
				writer.Write(cachedSourceTicks);
			}
		}, cancellation);
		byte b = Status(cancellationTokenSource.Token, cancellation);
		ValidateReply(sequence, cancellationTokenSource.Token, cancellation);
		if (b == 0)
		{
			PollNativeTrace(cancellation);
			return null;
		}
		byte[] value = Read(36, cancellationTokenSource.Token, cancellation);
		int num = BitConverter.ToInt32(value, 0);
		int num2 = BitConverter.ToInt32(value, 4);
		long num3 = BitConverter.ToInt64(value, 8);
		int num4 = BitConverter.ToInt32(value, 16);
		PixelBounds pixelBounds = new PixelBounds(BitConverter.ToInt32(value, 20), BitConverter.ToInt32(value, 24), BitConverter.ToInt32(value, 28), BitConverter.ToInt32(value, 32));
		if (num != width || num2 != height || pixelBounds != crop || num4 != target.Length || (cachedSourceTicks != 0L && num3 != cachedSourceTicks) || num3 <= 0 || num3 < sourceNotBeforeTicks || num3 > Stopwatch.GetTimestamp() + Stopwatch.Frequency / 30)
		{
			throw new InvalidDataException("Capture worker returned invalid geometry or source time.");
		}
		_sharedPixels.Read(target);
		PollNativeTrace(cancellation);
		return new CapturePixels(width, height, target)
		{
			SourceQpcTicks = num3,
			IsStatic = true,
			Source = _source
		};
	}

	private void PollNativeTrace(CancellationToken cancellation)
	{
		long timestamp = Stopwatch.GetTimestamp();
		if (_trace == null || timestamp < _nextTracePoll)
		{
			return;
		}
		_nextTracePoll = timestamp + Stopwatch.Frequency / 4;
		using CancellationTokenSource cancellationTokenSource = Deadline(cancellation);
		long sequence = ++_sequence;
		Command(5, sequence, cancellationTokenSource.Token, null, cancellation);
		if (Status(cancellationTokenSource.Token, cancellation) != 0)
		{
			throw new InvalidDataException("Invalid native trace reply.");
		}
		ValidateReply(sequence, cancellationTokenSource.Token, cancellation);
		int num = BitConverter.ToInt32(Read(4, cancellationTokenSource.Token, cancellation));
		if (num < 2 || num > 65536)
		{
			throw new InvalidDataException("Invalid native trace envelope length.");
		}
		CaptureTrace[] array;
		try
		{
			array = JsonSerializer.Deserialize<CaptureTrace[]>(Read(num, cancellationTokenSource.Token, cancellation)) ?? throw new InvalidDataException("Missing native trace events.");
		}
		catch (JsonException innerException)
		{
			throw new InvalidDataException("Invalid native trace events.", innerException);
		}
		if (array.Length > 64)
		{
			throw new InvalidDataException("Too many native trace events.");
		}
		CaptureTrace[] array2 = array;
		int num2 = 0;
		while (num2 < array2.Length)
		{
			CaptureTrace captureTrace = array2[num2];
			if (captureTrace.Stage != null && captureTrace.Reason != null && captureTrace.Stage.Length <= 64 && captureTrace.Reason.Length <= 128)
			{
				string? windowClass = captureTrace.WindowClass;
				if (windowClass == null || windowClass.Length <= 128)
				{
					_trace(captureTrace with
					{
						WorkerProcess = _process.Id
					});
					num2++;
					continue;
				}
			}
			throw new InvalidDataException("Invalid native trace metadata.");
		}
	}

	CapturePixels? IDesktopSceneCapture.TryRead(TimeSpan wait, CancellationToken cancellation, long notBefore, bool reuse)
	{
		return TryRead(wait, cancellation, notBefore, reuse);
	}

	CapturePixels? IDesktopSceneCapture.TryReadSampled(PixelBounds crop, int width, int height, byte[] target, TimeSpan wait, CancellationToken cancellation, long notBefore)
	{
		return TryReadSampled(crop, width, height, target, wait, cancellation, notBefore);
	}

	internal void ReleaseReadbackBuffers()
	{
		using CancellationTokenSource cancellationTokenSource = Deadline();
		long sequence = ++_sequence;
		Command(2, sequence, cancellationTokenSource.Token);
		Status(cancellationTokenSource.Token);
		ValidateReply(sequence, cancellationTokenSource.Token);
		_pixels = null;
	}

	void IDesktopSceneCapture.ReleaseReadbackBuffers()
	{
		ReleaseReadbackBuffers();
	}

	private CancellationTokenSource Deadline(CancellationToken cancellation = default(CancellationToken))
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
		cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(3L));
		return cancellationTokenSource;
	}

	private void Command(byte command, long sequence, CancellationToken token, Action<BinaryWriter>? payload = null, CancellationToken caller = default(CancellationToken))
	{
		Send((BinaryWriter writer) =>
		{
			writer.Write(command);
			writer.Write(sequence);
			writer.Write(_generation);
			WriteFamily(writer);
			payload?.Invoke(writer);
		}, token, caller);
	}

	private static void WriteFamily(BinaryWriter writer)
	{
		int[] array = CaptureProcessFamily.Snapshot();
		writer.Write(array.Length);
		int[] array2 = array;
		foreach (int value in array2)
		{
			writer.Write(value);
		}
	}

	private void Send(Action<BinaryWriter> write, CancellationToken token, CancellationToken caller = default(CancellationToken))
	{
		using MemoryStream memoryStream = new MemoryStream();
		using (BinaryWriter obj = new BinaryWriter(memoryStream, Encoding.UTF8, leaveOpen: true))
		{
			write(obj);
		}
		try
		{
			_input.WriteAsync(memoryStream.GetBuffer().AsMemory(0, (int)memoryStream.Length), token).AsTask().GetAwaiter()
				.GetResult();
			_input.FlushAsync(token).GetAwaiter().GetResult();
		}
		catch (OperationCanceledException) when (!caller.IsCancellationRequested)
		{
			throw new TimeoutException("Background capture request timed out.");
		}
	}

	private void ValidateReply(long sequence, CancellationToken token, CancellationToken cancellation = default(CancellationToken))
	{
		byte[] value = Read(24, token, cancellation);
		if (BitConverter.ToInt64(value, 0) != sequence || BitConverter.ToInt64(value, 8) != _generation)
		{
			throw new InvalidDataException("Capture worker returned a stale request or scene generation.");
		}
		SceneRevision = BitConverter.ToInt64(value, 16);
	}

	private byte Status(CancellationToken token, CancellationToken cancellation = default(CancellationToken))
	{
		byte b = Read(1, token, cancellation)[0];
		if (b == 2)
		{
			int num = BitConverter.ToInt32(Read(4, token, cancellation));
			if (num < 0 || num > 8192)
			{
				throw new InvalidDataException("Invalid capture error length.");
			}
			throw new InvalidOperationException("Isolated capture failed: " + Encoding.UTF8.GetString(Read(num, token, cancellation)));
		}
		if (b > 2)
		{
			throw new InvalidDataException("Invalid capture worker response.");
		}
		return b;
	}

	private byte[] Read(int bytes, CancellationToken token, CancellationToken cancellation = default(CancellationToken))
	{
		byte[] array = new byte[bytes];
		try
		{
			_output.ReadExactlyAsync(array, token).AsTask().GetAwaiter()
				.GetResult();
			return array;
		}
		catch (OperationCanceledException) when (!cancellation.IsCancellationRequested)
		{
			throw new TimeoutException("Background capture worker stopped responding.");
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		try
		{
			try
			{
				if (!_process.HasExited)
				{
					_process.Kill();
				}
			}
			catch (InvalidOperationException) when (_process.HasExited)
			{
			}
			if (!_process.WaitForExit(1000))
			{
				throw new TimeoutException("Capture worker exit was not confirmed.");
			}
		}
		catch
		{
			CaptureProcessFamily.Quarantine(_process.Id);
			Task.Run(async () =>
			{
				await _process.WaitForExitAsync();
				ReleaseExitedResources();
			});
			throw;
		}
		ReleaseExitedResources();
	}

	private void ReleaseExitedResources()
	{
		CaptureProcessFamily.ConfirmRetirement(_process.Id);
		_input.Dispose();
		_output.Dispose();
		_process.Dispose();
		_pixels = null;
		_sharedPixels?.Dispose();
		_sharedPixels = null;
		CarrierHandle = 0;
	}
}
