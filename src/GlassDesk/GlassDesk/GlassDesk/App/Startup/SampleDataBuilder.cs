using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using GlassDesk.Platform.Shell;

namespace GlassDesk.App.Startup;

internal static class SampleDataBuilder
{
	public static SampleSet Build(string? rootOverride = null)
	{
		string text = rootOverride ?? Path.Combine(Path.GetTempPath(), "GlassDesk-样本 测试");
		string text2 = Path.Combine(text, "程序 目录");
		string text3 = Path.Combine(text, "文档 目录");
		string text4 = Path.Combine(text, "快捷方式 目录");
		string text5 = Path.Combine(text, "失效 目录");
		string[] array = new string[5] { text, text2, text3, text4, text5 };
		for (int i = 0; i < array.Length; i++)
		{
			Directory.CreateDirectory(array[i]);
		}
		List<SampleSource> list = new List<SampleSource>();
		List<SampleSource> list2 = new List<SampleSource>();
		string text6 = Path.Combine(text2, "记事 本 模拟器.cmd");
		File.WriteAllText(text6, "@echo off" + Environment.NewLine + "echo 这是用来验收的程序样本。" + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		list.Add(new SampleSource("program", text6, "程序（中文与空格路径）"));
		string text7 = Path.Combine(text3, "我的 资料 文件夹");
		Directory.CreateDirectory(text7);
		File.WriteAllText(Path.Combine(text7, "里面的文件.txt"), "内容", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		list.Add(new SampleSource("folder", text7, "文件夹（中文与空格路径）"));
		string path = Path.Combine(text3, "季度 报告 2026.txt");
		File.WriteAllText(path, "样本内容", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		list.Add(new SampleSource("file", path, "文件（中文与空格路径）"));
		string text8 = Path.Combine(text4, "带 参数 的快捷方式.lnk");
		if (ShortcutWriter.Write(text8, new ShortcutDefinition(text6, "--flag \"带 空格 的值\" --second", text2, "C:\\Windows\\System32\\shell32.dll,3", "阶段 3 验收样本：参数与自定义图标位置都要保留")))
		{
			list.Add(new SampleSource("shortcut-arguments", text8, "快捷方式（带参数、中文、自定义图标索引）"));
		}
		else
		{
			list2.Add(new SampleSource("shortcut-arguments", text8, "本机无法创建快捷方式样本"));
		}
		string text9 = Path.Combine(text4, "网页 快捷方式.lnk");
		if (ShortcutWriter.Write(text9, new ShortcutDefinition("https://example.com/路径 带空格")))
		{
			list.Add(new SampleSource("shortcut-url", text9, "快捷方式（指向网页）"));
		}
		string path2 = Path.Combine(text4, "互联网 快捷方式.url");
		File.WriteAllText(path2, "[InternetShortcut]" + Environment.NewLine + "URL=https://example.org/a b" + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		list.Add(new SampleSource("url-file", path2, "网页（.url 文件）"));
		list.Add(new SampleSource("url-port-8001", "http://localhost:8001/app", "网页（端口 8001）"));
		list.Add(new SampleSource("url-port-8002", "http://localhost:8002/app", "网页（端口 8002）"));
		string text10 = Path.Combine(text5, "马上 会被删掉 的程序.cmd");
		File.WriteAllText(text10, "@echo off" + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		string text11 = Path.Combine(text5, "指向 将失效目标 的快捷方式.lnk");
		if (ShortcutWriter.Write(text11, new ShortcutDefinition(text10)))
		{
			list.Add(new SampleSource("broken-later", text11, "快捷方式（目标稍后会被删除，用于验证失效保留）"));
		}
		string path3 = Path.Combine(text4, "内容 不完整 的.url");
		File.WriteAllText(path3, "这不是一个 Internet 快捷方式" + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		list.Add(new SampleSource("bogus-url-file", path3, "普通文件（后缀是 .url 但内容不是）"));
		list2.Add(new SampleSource("missing-path", Path.Combine(text, "根本 不存在 的路径.txt"), "路径不存在"));
		string path4 = Path.Combine(text4, "空 的快捷方式.lnk");
		File.WriteAllBytes(path4, default(ReadOnlySpan<byte>));
		list2.Add(new SampleSource("empty-shortcut", path4, "空文件伪装成快捷方式"));
		return new SampleSet(text, list, list2, text10, text11);
	}

	public static string Describe(SampleSet set)
	{
		ArgumentNullException.ThrowIfNull(set, "set");
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(6, 1, stringBuilder2, invariantCulture);
		handler.AppendLiteral("样本根目录：");
		handler.AppendFormatted(set.Directory);
		stringBuilder3.AppendLine(provider, ref handler);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("应当成功导入：");
		foreach (SampleSource item in set.ShouldImport)
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder2;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider2 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(7, 2, stringBuilder2, invariantCulture);
			handler.AppendLiteral("  · [");
			handler.AppendFormatted(item.Key);
			handler.AppendLiteral("] ");
			handler.AppendFormatted(item.Description);
			stringBuilder4.AppendLine(provider2, ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder5 = stringBuilder2;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider3 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(6, 1, stringBuilder2, invariantCulture);
			handler.AppendLiteral("      ");
			handler.AppendFormatted(item.Path);
			stringBuilder5.AppendLine(provider3, ref handler);
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("应当被逐条拒绝：");
		foreach (SampleSource item2 in set.ShouldFail)
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder6 = stringBuilder2;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider4 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(7, 2, stringBuilder2, invariantCulture);
			handler.AppendLiteral("  · [");
			handler.AppendFormatted(item2.Key);
			handler.AppendLiteral("] ");
			handler.AppendFormatted(item2.Description);
			stringBuilder6.AppendLine(provider4, ref handler);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder7 = stringBuilder2;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider5 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(6, 1, stringBuilder2, invariantCulture);
			handler.AppendLiteral("      ");
			handler.AppendFormatted(item2.Path);
			stringBuilder7.AppendLine(provider5, ref handler);
		}
		stringBuilder.AppendLine();
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder8 = stringBuilder2;
		invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider6 = invariantCulture;
		handler = new StringBuilder.AppendInterpolatedStringHandler(9, 1, stringBuilder2, invariantCulture);
		handler.AppendLiteral("稍后要删掉的目标：");
		handler.AppendFormatted(set.BrokenTarget);
		stringBuilder8.AppendLine(provider6, ref handler);
		return stringBuilder.ToString();
	}

	public static bool DeleteTarget(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
				return true;
			}
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
		}
		return false;
	}
}
