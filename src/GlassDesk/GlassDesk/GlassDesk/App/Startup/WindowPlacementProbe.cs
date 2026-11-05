using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using GlassDesk.Platform.Displays;

namespace GlassDesk.App.Startup;

internal static class WindowPlacementProbe
{
	private readonly record struct PlacementResult(PixelRect Expected, PixelRect Actual)
	{
		public int DeltaX => Actual.X - Expected.X;

		public int DeltaY => Actual.Y - Expected.Y;

		public bool IsExact
		{
			get
			{
				if (DeltaX == 0)
				{
					return DeltaY == 0;
				}
				return false;
			}
		}

		public string Describe()
		{
			return $"期望 {Expected}，实际 {Actual}，偏差 ({DeltaX},{DeltaY})";
		}
	}

	private const int ProbeWidth = 320;

	private const int ProbeHeight = 80;

	public static int Run(StringBuilder report, IReadOnlyList<DisplayInfo> displays)
	{
		ArgumentNullException.ThrowIfNull(report, "report");
		ArgumentNullException.ThrowIfNull(displays, "displays");
		CultureInfo invariantCulture = CultureInfo.InvariantCulture;
		report.AppendLine("【多屏定位实测】");
		report.AppendLine("  目标位置取各屏工作区底部居中，也就是 Dock 将要去的地方。");
		report.AppendLine("  A = WPF 的 Left/Top（按目标屏 DPI 换算）；B = Win32 SetWindowPos（物理像素）。");
		report.AppendLine();
		if (displays.Count == 0)
		{
			report.AppendLine("✗ 没有显示器可用于摆放实验。");
			return 1;
		}
		DisplayInfo primary = displays.FirstOrDefault((DisplayInfo d) => d.IsPrimary) ?? displays[0];
		Window window = new Window
		{
			Width = 200.0,
			Height = 60.0,
			WindowStyle = WindowStyle.None,
			ShowInTaskbar = false,
			ShowActivated = false,
			Topmost = false,
			Opacity = 0.01,
			WindowStartupLocation = WindowStartupLocation.Manual
		};
		int num = 0;
		int num2 = 0;
		try
		{
			window.Show();
			nint handle = new WindowInteropHelper(window).Handle;
			if (handle == 0)
			{
				report.AppendLine("✗ 探测窗口没有取得句柄。");
				return 1;
			}
			StringBuilder stringBuilder;
			IFormatProvider formatProvider;
			StringBuilder.AppendInterpolatedStringHandler handler;
			foreach (DisplayInfo display in displays)
			{
				PixelRect target = BottomCentreOfWorkArea(display);
				stringBuilder = report;
				StringBuilder stringBuilder2 = stringBuilder;
				formatProvider = invariantCulture;
				IFormatProvider provider = formatProvider;
				handler = new StringBuilder.AppendInterpolatedStringHandler(4, 2, stringBuilder, formatProvider);
				handler.AppendLiteral("· ");
				handler.AppendFormatted(display.DeviceName);
				handler.AppendLiteral("（");
				handler.AppendFormatted(display.ScaleFactor, "P0");
				handler.AppendLiteral("）");
				stringBuilder2.AppendLine(provider, ref handler);
				ResetToPrimary(window, handle, primary);
				PlacementResult placementResult = PlaceWithWpfCoordinates(window, handle, display, target);
				stringBuilder = report;
				StringBuilder stringBuilder3 = stringBuilder;
				formatProvider = invariantCulture;
				IFormatProvider provider2 = formatProvider;
				handler = new StringBuilder.AppendInterpolatedStringHandler(20, 1, stringBuilder, formatProvider);
				handler.AppendLiteral("    A  WPF Left/Top：");
				handler.AppendFormatted(placementResult.Describe());
				stringBuilder3.AppendLine(provider2, ref handler);
				if (placementResult.IsExact)
				{
					report.AppendLine("       ✓ 落点精确");
				}
				else
				{
					num++;
					report.AppendLine("       ✗ 有偏移");
				}
				ResetToPrimary(window, handle, primary);
				PlacementResult placementResult2 = PlaceWithPixelRect(handle, target);
				stringBuilder = report;
				StringBuilder stringBuilder4 = stringBuilder;
				formatProvider = invariantCulture;
				IFormatProvider provider3 = formatProvider;
				handler = new StringBuilder.AppendInterpolatedStringHandler(20, 1, stringBuilder, formatProvider);
				handler.AppendLiteral("    B  SetWindowPos：");
				handler.AppendFormatted(placementResult2.Describe());
				stringBuilder4.AppendLine(provider3, ref handler);
				if (placementResult2.IsExact)
				{
					report.AppendLine("       ✓ 落点精确");
				}
				else
				{
					num2++;
					report.AppendLine("       ✗ 有偏移");
				}
				report.AppendLine();
			}
			report.AppendLine("  小结：");
			stringBuilder = report;
			StringBuilder stringBuilder5 = stringBuilder;
			formatProvider = invariantCulture;
			IFormatProvider provider4 = formatProvider;
			handler = new StringBuilder.AppendInterpolatedStringHandler(29, 2, stringBuilder, formatProvider);
			handler.AppendLiteral("    方式 A（WPF DIP 换算）：");
			handler.AppendFormatted(displays.Count - num);
			handler.AppendLiteral("/");
			handler.AppendFormatted(displays.Count);
			handler.AppendLiteral(" 台显示器精确");
			stringBuilder5.AppendLine(provider4, ref handler);
			stringBuilder = report;
			StringBuilder stringBuilder6 = stringBuilder;
			formatProvider = invariantCulture;
			IFormatProvider provider5 = formatProvider;
			handler = new StringBuilder.AppendInterpolatedStringHandler(29, 2, stringBuilder, formatProvider);
			handler.AppendLiteral("    方式 B（Win32 物理像素）：");
			handler.AppendFormatted(displays.Count - num2);
			handler.AppendLiteral("/");
			handler.AppendFormatted(displays.Count);
			handler.AppendLiteral(" 台显示器精确");
			stringBuilder6.AppendLine(provider5, ref handler);
		}
		finally
		{
			window.Close();
		}
		if (num > 0 && num2 == 0)
		{
			report.AppendLine("    → 采用方式 B。跨屏定位不得依赖 WPF 的 Left/Top 换算，");
			report.AppendLine("      否则混合 DPI 下窗口会出现在错误位置且不报错。");
		}
		else if (num > 0 && num2 > 0)
		{
			report.AppendLine("    → 两种方式都有偏移，需要继续排查。");
		}
		report.AppendLine();
		return num2;
	}

	private static PixelRect BottomCentreOfWorkArea(DisplayInfo display)
	{
		PixelRect workArea = display.WorkArea;
		return new PixelRect(workArea.X + (workArea.Width - 320) / 2, workArea.Bottom - 80 - display.ToPixels(8.0), 320, 80);
	}

	private static void ResetToPrimary(Window window, nint handle, DisplayInfo primary)
	{
		DisplayService.MoveWindowToPixelRect(handle, new PixelRect(primary.WorkArea.X + 40, primary.WorkArea.Y + 40, 320, 80));
		window.UpdateLayout();
	}

	private static PlacementResult PlaceWithWpfCoordinates(Window window, nint handle, DisplayInfo display, PixelRect target)
	{
		window.Width = display.ToDips(target.Width);
		window.Height = display.ToDips(target.Height);
		window.Left = display.ToDips(target.X);
		window.Top = display.ToDips(target.Y);
		window.UpdateLayout();
		PixelRect valueOrDefault = DisplayService.GetWindowPixelBounds(handle).GetValueOrDefault();
		return new PlacementResult(target, valueOrDefault);
	}

	private static PlacementResult PlaceWithPixelRect(nint handle, PixelRect target)
	{
		DisplayService.MoveWindowToPixelRect(handle, target);
		PixelRect valueOrDefault = DisplayService.GetWindowPixelBounds(handle).GetValueOrDefault();
		return new PlacementResult(target, valueOrDefault);
	}
}
