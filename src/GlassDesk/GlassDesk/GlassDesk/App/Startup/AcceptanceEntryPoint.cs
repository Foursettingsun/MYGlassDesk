using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GlassDesk.Core.Storage;
using GlassDesk.Platform.Storage;

namespace GlassDesk.App.Startup;

internal static class AcceptanceEntryPoint
{
	public static bool IsValidIsolatedRun(CommandLineOptions options)
	{
		if (options.SelfTest ^ options.Diagnose)
		{
			string acceptanceDataRoot = options.AcceptanceDataRoot;
			if (acceptanceDataRoot != null && acceptanceDataRoot.Length > 0)
			{
				acceptanceDataRoot = options.AcceptanceReport;
				if (acceptanceDataRoot != null && acceptanceDataRoot.Length > 0 && options.Verb == AppServiceVerb.None && !options.Exit && !options.ShowLauncher && !options.ShowSettings && options.Sample == AppearanceSample.None && !options.Help)
				{
					return options.Unknown.Count == 0;
				}
			}
		}
		return false;
	}

	public static int Run(AppPaths realPaths, string dataRoot, IReadOnlyList<AcceptanceOutput> outputs, Func<AcceptanceWorkspace, int> action)
	{
		ArgumentNullException.ThrowIfNull(realPaths, "realPaths");
		ArgumentNullException.ThrowIfNull(outputs, "outputs");
		ArgumentNullException.ThrowIfNull(action, "action");
		AcceptanceWorkspace acceptanceWorkspace = Prepare(realPaths, dataRoot, outputs);
		if (acceptanceWorkspace != null)
		{
			return Run(acceptanceWorkspace, action);
		}
		return 2;
	}

	public static AcceptanceWorkspace? Prepare(AppPaths realPaths, string dataRoot, IReadOnlyList<AcceptanceOutput> outputs)
	{
		try
		{
			return AcceptanceWorkspace.Create(new AcceptanceRequest(realPaths.RootDirectory, dataRoot, outputs));
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is PlatformNotSupportedException) ? true : false)
		{
			Console.Error.WriteLine("验收隔离初始化失败（报告路径或数据目录被拒绝）：" + ex.Message);
			return null;
		}
	}

	public static int Run(AcceptanceWorkspace workspace, Func<AcceptanceWorkspace, int> action)
	{
		try
		{
			using (workspace)
			{
				return action(workspace);
			}
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine("验收动作或必需输出写入/关闭失败：" + ex.Message);
			return 1;
		}
	}

	public static void WriteText(Stream output, string text)
	{
		using StreamWriter streamWriter = new StreamWriter(output, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 1024, leaveOpen: true);
		streamWriter.Write(text);
		streamWriter.Flush();
	}
}
