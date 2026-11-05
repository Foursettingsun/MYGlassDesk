using System;
using System.IO;
using System.IO.Pipes;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GlassDesk.Platform;

public sealed class SingleInstanceGuard : IDisposable
{
	private const string LegacyMutexPrefix = "Local\\GlassDesk.SingleInstance.";

	private readonly string _instanceName;

	private readonly InstanceIdentity _identity;

	private readonly Mutex _mutex;

	private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();

	private Mutex? _legacyMutex;

	private Task? _listener;

	private bool _disposed;

	public bool IsFirstInstance { get; }

	public event EventHandler<string>? CommandReceived;

	private SingleInstanceGuard(string instanceName, InstanceIdentity identity, Mutex mutex, bool isFirstInstance)
	{
		_instanceName = instanceName;
		_identity = identity;
		_mutex = mutex;
		IsFirstInstance = isFirstInstance;
	}

	public static SingleInstanceGuard Acquire(string instanceName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(instanceName, "instanceName");
		InstanceIdentity current = InstanceIdentity.Current;
		Mutex mutex = new Mutex(initiallyOwned: false, current.GetMutexName(instanceName), out var createdNew);
		return new SingleInstanceGuard(instanceName, current, mutex, createdNew);
	}

	public bool TryAcquireLegacyProtocolLease(out LegacyInstanceStatus status)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (!IsFirstInstance)
		{
			throw new InvalidOperationException("只有首个新版实例可以取得旧协议兼容租约。");
		}
		if (_legacyMutex != null)
		{
			status = LegacyInstanceStatus.NotRunning;
			return true;
		}
		try
		{
			Mutex mutex = new Mutex(initiallyOwned: false, "Local\\GlassDesk.SingleInstance." + _instanceName, out var createdNew);
			if (!createdNew)
			{
				mutex.Dispose();
				status = LegacyInstanceStatus.Running;
				return false;
			}
			_legacyMutex = mutex;
			status = LegacyInstanceStatus.NotRunning;
			return true;
		}
		catch (Exception ex) when ((ex is UnauthorizedAccessException || ex is SecurityException || ex is IOException) ? true : false)
		{
			status = LegacyInstanceStatus.Unknown;
			return false;
		}
	}

	public void StartListening()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (IsFirstInstance && _listener == null)
		{
			_listener = Task.Run(() => ListenAsync(_cancellation.Token));
		}
	}

	public static async Task<bool> TrySendCommandAsync(string instanceName, string command, TimeSpan timeout)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(instanceName, "instanceName");
		ArgumentException.ThrowIfNullOrWhiteSpace(command, "command");
		string pipeName = InstanceIdentity.Current.GetPipeName(instanceName);
		DateTime deadline = DateTime.UtcNow + timeout;
		while (DateTime.UtcNow < deadline)
		{
			try
			{
				bool result;
				await using (NamedPipeClientStream client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous))
				{
					await client.ConnectAsync(250).ConfigureAwait(continueOnCapturedContext: false);
					byte[] bytes = Encoding.UTF8.GetBytes(command + "\n");
					await client.WriteAsync(bytes).ConfigureAwait(continueOnCapturedContext: false);
					await client.FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
					result = true;
				}
				return result;
			}
			catch (Exception ex) when ((ex is TimeoutException || ex is IOException || ex is UnauthorizedAccessException) ? true : false)
			{
				await Task.Delay(60).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		return false;
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_cancellation.Cancel();
			try
			{
				_listener?.Wait(TimeSpan.FromSeconds(1L));
			}
			catch (AggregateException)
			{
			}
			_cancellation.Dispose();
			_legacyMutex?.Dispose();
			_legacyMutex = null;
			_mutex.Dispose();
			_disposed = true;
		}
	}

	private async Task ListenAsync(CancellationToken cancellationToken)
	{
		NamedPipeServerStream server = new NamedPipeServerStream(_identity.GetPipeName(_instanceName), PipeDirection.In, -1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
		try
		{
			_ = 1;
			try
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					using StreamReader reader = new StreamReader(server, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, -1, leaveOpen: true);
					string text = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					if (!string.IsNullOrWhiteSpace(text))
					{
						CommandReceived?.Invoke(this, text.Trim());
					}
					server.Disconnect();
					if (cancellationToken.IsCancellationRequested)
					{
						break;
					}
				}
			}
			catch (OperationCanceledException)
			{
			}
			catch (IOException)
			{
			}
		}
		finally
		{
			await server.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
	}
}
