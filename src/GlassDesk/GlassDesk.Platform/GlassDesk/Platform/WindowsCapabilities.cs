using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GlassDesk.Platform;

public static class WindowsCapabilities
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct RtlOsVersionInfo
	{
		public uint Size;

		public uint MajorVersion;

		public uint MinorVersion;

		public uint BuildNumber;

		public uint PlatformId;

		public unsafe fixed char ServicePack[128];
	}

	private static class NtDll
	{
		[LibraryImport("ntdll.dll")]
		[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.14.42308")]
		[SkipLocalsInit]
		internal unsafe static int RtlGetVersion(ref RtlOsVersionInfo versionInfo)
		{
			int result;
			fixed (RtlOsVersionInfo* _versionInfo_native = &versionInfo)
			{
				result = __PInvoke(_versionInfo_native);
			}
			return result;
			[DllImport("ntdll.dll", EntryPoint = "RtlGetVersion", ExactSpelling = true)]
			unsafe static extern int __PInvoke(RtlOsVersionInfo* __versionInfo_native);
		}
	}

	private const int Windows11Build = 22000;

	private const int SystemBackdropBuild = 22621;

	private const int ImmersiveDarkModeBuild = 18985;

	private const int MinimumSupportedBuild = 19041;

	public static Version OsVersion { get; }

	public static int BuildNumber { get; }

	public static bool IsSupported => IsProductBuildSupported(BuildNumber);

	public static bool IsWindows11OrGreater => BuildNumber >= 22000;

	public static bool SupportsSystemBackdrop => BuildNumber >= 22621;

	public static bool SupportsWindowCornerPreference => BuildNumber >= 22000;

	public static bool SupportsImmersiveDarkMode => BuildNumber >= 18985;

	static WindowsCapabilities()
	{
		(int Major, int Minor, int Build) tuple = ReadVersion();
		int item = tuple.Major;
		int item2 = tuple.Minor;
		int item3 = tuple.Build;
		OsVersion = new Version(item, item2, item3);
		BuildNumber = item3;
	}

	public static bool IsProductBuildSupported(int build)
	{
		return build >= 19041;
	}

	public static string Describe()
	{
		string value = (SupportsSystemBackdrop ? "支持系统背景材质" : "不支持系统背景材质（使用自绘面板）");
		return $"Windows {OsVersion}（Build {BuildNumber}）— {value}";
	}

	private static (int Major, int Minor, int Build) ReadVersion()
	{
		try
		{
			RtlOsVersionInfo versionInfo = new RtlOsVersionInfo
			{
				Size = (uint)Marshal.SizeOf<RtlOsVersionInfo>()
			};
			if (NtDll.RtlGetVersion(ref versionInfo) == 0)
			{
				return (Major: (int)versionInfo.MajorVersion, Minor: (int)versionInfo.MinorVersion, Build: (int)versionInfo.BuildNumber);
			}
		}
		catch (Exception ex) when ((ex is DllNotFoundException || ex is EntryPointNotFoundException) ? true : false)
		{
		}
		Version version = Environment.OSVersion.Version;
		return (Major: version.Major, Minor: version.Minor, Build: version.Build);
	}
}
