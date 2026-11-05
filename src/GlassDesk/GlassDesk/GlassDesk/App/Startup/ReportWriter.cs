using System;
using System.IO;
using System.Text;
using GlassDesk.Platform;

namespace GlassDesk.App.Startup;

internal static class ReportWriter
{
	public static void Write(string text, string? logFilePath)
	{
		if (ConsoleAttach.TryAttachToParentConsole())
		{
			StreamWriter streamWriter = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			streamWriter.AutoFlush = true;
			streamWriter.Write(text);
			streamWriter.Write(Environment.NewLine);
			streamWriter.Flush();
		}
		if (string.IsNullOrWhiteSpace(logFilePath))
		{
			return;
		}
		try
		{
			string directoryName = Path.GetDirectoryName(logFilePath);
			if (!string.IsNullOrEmpty(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			File.WriteAllText(logFilePath, text, Encoding.UTF8);
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
		}
	}
}
