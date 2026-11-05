using System;
using System.Runtime.InteropServices;

namespace GlassDesk.Platform;

public static class WindowAcrylic
{
	private const int WcaAccentPolicy = 19;

	private const int AccentEnableAcrylicBlurBehind = 4;

	private const int AccentEnableBlurBehind = 3;

	private static readonly int[] VerifiedBuilds = new int[1] { 26100 };

	public static bool IsSupportedOnThisBuild => Array.IndexOf(VerifiedBuilds, WindowsCapabilities.BuildNumber) >= 0;

	public static bool TryApply(nint window, byte tintAlpha, byte red, byte green, byte blue)
	{
		if (window == 0)
		{
			return false;
		}
		AccentPolicy structure = new AccentPolicy
		{
			AccentState = 4,
			AccentFlags = 2,
			GradientColor = ((tintAlpha << 24) | (blue << 16) | (green << 8) | red),
			AnimationId = 0
		};
		int num = Marshal.SizeOf<AccentPolicy>();
		nint num2 = Marshal.AllocHGlobal(num);
		try
		{
			Marshal.StructureToPtr(structure, num2, fDeleteOld: false);
			WindowCompositionAttributeData data = new WindowCompositionAttributeData
			{
				Attribute = 19,
				Data = num2,
				SizeOfData = num
			};
			return Composition32.SetWindowCompositionAttribute(window, ref data) != 0;
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return false;
		}
		finally
		{
			Marshal.FreeHGlobal(num2);
		}
	}

	public static bool TryRemove(nint window)
	{
		if (window == 0)
		{
			return false;
		}
		AccentPolicy structure = new AccentPolicy
		{
			AccentState = 0,
			AccentFlags = 0,
			GradientColor = 0,
			AnimationId = 0
		};
		int num = Marshal.SizeOf<AccentPolicy>();
		nint num2 = Marshal.AllocHGlobal(num);
		try
		{
			Marshal.StructureToPtr(structure, num2, fDeleteOld: false);
			WindowCompositionAttributeData data = new WindowCompositionAttributeData
			{
				Attribute = 19,
				Data = num2,
				SizeOfData = num
			};
			return Composition32.SetWindowCompositionAttribute(window, ref data) != 0;
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return false;
		}
		finally
		{
			Marshal.FreeHGlobal(num2);
		}
	}

	public static string DescribeSupport()
	{
		int buildNumber = WindowsCapabilities.BuildNumber;
		if (IsSupportedOnThisBuild)
		{
			return $"Build {buildNumber} 在已验证名单内，使用亚克力背景模糊";
		}
		return $"Build {buildNumber} 未经验证，走自绘面板降级（已验证：{string.Join('、', VerifiedBuilds)}）";
	}

	public static bool TryApplyBlurOnly(nint window)
	{
		if (window == 0)
		{
			return false;
		}
		AccentPolicy structure = new AccentPolicy
		{
			AccentState = 3,
			AccentFlags = 2,
			GradientColor = 0,
			AnimationId = 0
		};
		int num = Marshal.SizeOf<AccentPolicy>();
		nint num2 = Marshal.AllocHGlobal(num);
		try
		{
			Marshal.StructureToPtr(structure, num2, fDeleteOld: false);
			WindowCompositionAttributeData data = new WindowCompositionAttributeData
			{
				Attribute = 19,
				Data = num2,
				SizeOfData = num
			};
			return Composition32.SetWindowCompositionAttribute(window, ref data) != 0;
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
			return false;
		}
		finally
		{
			Marshal.FreeHGlobal(num2);
		}
	}
}
