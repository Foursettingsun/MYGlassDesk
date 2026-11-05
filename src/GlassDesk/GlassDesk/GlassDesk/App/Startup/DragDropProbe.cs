using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using GlassDesk.Core.Storage;
using GlassDesk.Platform.Shell;

namespace GlassDesk.App.Startup;

internal static class DragDropProbe
{
	public static int Run(AppPaths paths)
	{
		ArgumentNullException.ThrowIfNull(paths, "paths");
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("阶段 4 探针 C：拖放管线（WPF / OLE 注册归属）");
		stringBuilder.AppendLine(new string('=', 46));
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		IFormatProvider provider = invariantCulture;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(3, 1, stringBuilder2, invariantCulture);
		handler.AppendLiteral("时间：");
		handler.AppendFormatted(DateTimeOffset.Now, "yyyy-MM-dd HH:mm:ss zzz");
		stringBuilder3.AppendLine(provider, ref handler);
		stringBuilder.AppendLine();
		Window window = new Window
		{
			Width = 200.0,
			Height = 100.0,
			WindowStyle = WindowStyle.None,
			ShowInTaskbar = false,
			ShowActivated = false,
			AllowDrop = true,
			Content = new Border()
		};
		try
		{
			window.Show();
			nint handle = new WindowInteropHelper(window).Handle;
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder2;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider2 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(32, 1, stringBuilder2, invariantCulture);
			handler.AppendLiteral("[C1] 测试窗口句柄 0x");
			handler.AppendFormatted(handle, "X");
			handler.AppendLiteral("，AllowDrop = true。");
			stringBuilder4.AppendLine(provider2, ref handler);
			bool flag = DropTargetRegistration.IsOleDropTargetRegistered(handle);
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder5 = stringBuilder2;
			invariantCulture = CultureInfo.InvariantCulture;
			IFormatProvider provider3 = invariantCulture;
			handler = new StringBuilder.AppendInterpolatedStringHandler(39, 1, stringBuilder2, invariantCulture);
			handler.AppendLiteral("[C2] 句柄属性表里的 OleDropTargetInterface 原子：");
			handler.AppendFormatted(flag ? "存在" : "不存在");
			stringBuilder5.AppendLine(provider3, ref handler);
			stringBuilder.AppendLine();
			stringBuilder.AppendLine(flag ? "结论：WPF 已自行完成 OLE IDropTarget 注册。Dock 的外部拖入走 WPF Drop 事件，产品代码不对同一句柄再做 RegisterDragDrop；拖入文件经 DataObject 的 FileDrop 格式到达。" : "结论：未探测到 OLE 注册原子——WPF 的注册时机与预期不符，需要人工核对后再定归属。");
			stringBuilder.AppendLine("跨进程真拖（资源管理器 → Dock，中文与空格路径）列入人工核对清单。");
		}
		finally
		{
			window.Close();
		}
		ReportWriter.Write(stringBuilder.ToString(), Path.Combine(paths.LogDirectory, "phase4-probe-dragpipe.log"));
		Console.Write(stringBuilder.ToString());
		return 0;
	}
}
