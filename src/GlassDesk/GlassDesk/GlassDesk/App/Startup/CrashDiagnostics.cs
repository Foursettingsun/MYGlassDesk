using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace GlassDesk.App.Startup;

internal sealed class CrashDiagnostics : IDisposable
{
	private readonly Application _application;

	private readonly string _path;

	private readonly object _writeLock = new object();

	internal CrashDiagnostics(Application application, string logDirectory)
	{
		_application = application;
		_path = Path.Combine(logDirectory, "crash.log");
		application.DispatcherUnhandledException += OnDispatcherException;
		AppDomain.CurrentDomain.UnhandledException += OnDomainException;
	}

	private void OnDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs args)
	{
		lock (_writeLock)
		{
			TryWrite(_path, "Dispatcher", args.Exception);
		}
	}

	private void OnDomainException(object sender, UnhandledExceptionEventArgs args)
	{
		lock (_writeLock)
		{
			TryWrite(_path, "AppDomain", args.ExceptionObject);
		}
	}

	internal static string FormatReport(string source, object exception)
	{
		string newLine = Environment.NewLine;
		InlineArray8<string> buffer = default;
		buffer[0] = DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture);
		buffer[1] = "Source: " + source;
		buffer[2] = "Executable: " + Environment.ProcessPath;
		buffer[3] = "Base directory: " + AppContext.BaseDirectory;
		buffer[4] = $"Version: {typeof(CrashDiagnostics).Assembly.GetName().Version}";
		buffer[5] = $"Runtime: {Environment.Version}; OS: {Environment.OSVersion}";
		buffer[6] = exception.ToString();
		buffer[7] = "";
		return string.Join(newLine, (ReadOnlySpan<string?>)buffer);
	}

	internal static void TryWrite(string path, string source, object exception)
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.AppendAllText(path, FormatReport(source, exception) + Environment.NewLine);
		}
		catch (Exception)
		{
		}
	}

	public void Dispose()
	{
		_application.DispatcherUnhandledException -= OnDispatcherException;
		AppDomain.CurrentDomain.UnhandledException -= OnDomainException;
	}
}
