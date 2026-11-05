using System;
using System.IO;
using System.Security;
using Microsoft.Win32;

namespace GlassDesk.Platform.Startup;

public sealed class StartupRegistration
{
	private const string RunKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";

	private const int MaximumCommandLineLength = 260;

	private const string AutostartArgument = "--autostart";

	public const string ValueName = "GlassDesk.Independent";

	private readonly Func<bool, RegistryKey?> _openRunKey;

	private readonly string _valueName;

	private readonly bool _ownsOpenedKeys;

	private readonly ScheduledStartupRegistration? _scheduled;

	public StartupRegistration()
		: this(OpenCurrentUserRunKey, "GlassDesk.Independent", ownsOpenedKeys: true)
	{
		_scheduled = new ScheduledStartupRegistration();
	}

	internal StartupRegistration(RegistryKey runKey, string valueName)
		: this((bool _) => runKey, valueName, ownsOpenedKeys: false)
	{
		ArgumentNullException.ThrowIfNull(runKey, "runKey");
	}

	internal StartupRegistration(Func<bool, RegistryKey?> openRunKey, string valueName)
		: this(openRunKey, valueName, ownsOpenedKeys: true)
	{
	}

	private StartupRegistration(Func<bool, RegistryKey?> openRunKey, string valueName, bool ownsOpenedKeys)
	{
		ArgumentNullException.ThrowIfNull(openRunKey, "openRunKey");
		ArgumentException.ThrowIfNullOrWhiteSpace(valueName, "valueName");
		_openRunKey = openRunKey;
		_valueName = valueName;
		_ownsOpenedKeys = ownsOpenedKeys;
	}

	private StartupRegistrationStatus QueryLegacy(string executablePath)
	{
		if (!TryCreateCommand(executablePath, out string fullPath, out string command, out string error))
		{
			return new StartupRegistrationStatus(StartupRegistrationState.Unavailable, null, error);
		}
		return ReadStatus(fullPath, command);
	}

	private StartupChangeResult SetLegacyEnabled(string executablePath, bool enabled)
	{
		if (!TryCreateCommand(executablePath, out string fullPath, out string command, out string error))
		{
			StartupRegistrationStatus status = new StartupRegistrationStatus(StartupRegistrationState.Unavailable, null, error);
			return new StartupChangeResult(Succeeded: false, status, error);
		}
		StartupRegistrationStatus startupRegistrationStatus = ReadStatus(fullPath, command);
		if (startupRegistrationStatus.State == StartupRegistrationState.Unavailable)
		{
			return new StartupChangeResult(Succeeded: false, startupRegistrationStatus, startupRegistrationStatus.Error);
		}
		if (enabled && startupRegistrationStatus.State == StartupRegistrationState.Owned)
		{
			return new StartupChangeResult(Succeeded: true, startupRegistrationStatus, null);
		}
		if (!enabled && startupRegistrationStatus.State == StartupRegistrationState.Missing)
		{
			return new StartupChangeResult(Succeeded: true, startupRegistrationStatus, null);
		}
		if (startupRegistrationStatus.State == StartupRegistrationState.OtherTarget)
		{
			string error2 = "同名登录启动项指向其它文件；为保护该项，GlassDesk 没有修改它。";
			return new StartupChangeResult(Succeeded: false, startupRegistrationStatus with
			{
				Error = error2
			}, error2);
		}
		RegistryKey registryKey = null;
		try
		{
			registryKey = _openRunKey(arg: true);
			if (registryKey == null)
			{
				throw new IOException("无法打开当前用户的登录启动注册表项。");
			}
			StartupRegistrationStatus startupRegistrationStatus2 = ReadStatusFromKey(registryKey, fullPath, command);
			if (enabled && startupRegistrationStatus2.State == StartupRegistrationState.Owned)
			{
				return new StartupChangeResult(Succeeded: true, startupRegistrationStatus2, null);
			}
			if (!enabled && startupRegistrationStatus2.State == StartupRegistrationState.Missing)
			{
				return new StartupChangeResult(Succeeded: true, startupRegistrationStatus2, null);
			}
			if (startupRegistrationStatus2.State == StartupRegistrationState.OtherTarget)
			{
				string error3 = "登录启动项已被其它位置修改；为保护该项，GlassDesk 没有覆盖或删除它。";
				return new StartupChangeResult(Succeeded: false, startupRegistrationStatus2 with
				{
					Error = error3
				}, error3);
			}
			if (startupRegistrationStatus2.State == StartupRegistrationState.Unavailable)
			{
				return new StartupChangeResult(Succeeded: false, startupRegistrationStatus2, startupRegistrationStatus2.Error);
			}
			if (enabled)
			{
				registryKey.SetValue(_valueName, command, RegistryValueKind.String);
			}
			else
			{
				registryKey.DeleteValue(_valueName, throwOnMissingValue: false);
			}
			StartupRegistrationStatus startupRegistrationStatus3 = ReadStatus(fullPath, command);
			StartupRegistrationState startupRegistrationState = (enabled ? StartupRegistrationState.Owned : StartupRegistrationState.Missing);
			if (startupRegistrationStatus3.State == startupRegistrationState)
			{
				return new StartupChangeResult(Succeeded: true, startupRegistrationStatus3, null);
			}
			string error4 = startupRegistrationStatus3.Error ?? "登录启动项写入后核验未达到预期状态。";
			if (enabled && startupRegistrationStatus.State == StartupRegistrationState.Missing)
			{
				TryRemoveOwnedValue(command);
			}
			StartupRegistrationStatus startupRegistrationStatus4 = ReadStatus(fullPath, command);
			return new StartupChangeResult(Succeeded: false, startupRegistrationStatus4 with
			{
				Error = error4
			}, error4);
		}
		catch (Exception ex) when (IsRegistryFailure(ex))
		{
			if (enabled && startupRegistrationStatus.State == StartupRegistrationState.Missing)
			{
				TryRemoveOwnedValue(command);
			}
			StartupRegistrationStatus startupRegistrationStatus5 = ReadStatus(fullPath, command);
			if (enabled && startupRegistrationStatus5.State == StartupRegistrationState.Owned)
			{
				TryRemoveOwnedValue(command);
				startupRegistrationStatus5 = ReadStatus(fullPath, command);
			}
			string error5 = ToUserError(ex);
			return new StartupChangeResult(Succeeded: false, startupRegistrationStatus5 with
			{
				Error = error5
			}, error5);
		}
		finally
		{
			if (_ownsOpenedKeys)
			{
				registryKey?.Dispose();
			}
		}
	}

	private StartupRegistrationStatus ReadStatus(string fullPath, string expectedCommand)
	{
		RegistryKey registryKey = null;
		try
		{
			registryKey = _openRunKey(arg: false);
			return (registryKey == null) ? new StartupRegistrationStatus(StartupRegistrationState.Missing, fullPath, null) : ReadStatusFromKey(registryKey, fullPath, expectedCommand);
		}
		catch (Exception ex) when (IsRegistryFailure(ex))
		{
			return new StartupRegistrationStatus(StartupRegistrationState.Unavailable, fullPath, ToUserError(ex));
		}
		finally
		{
			if (_ownsOpenedKeys)
			{
				registryKey?.Dispose();
			}
		}
	}

	private StartupRegistrationStatus ReadStatusFromKey(RegistryKey key, string fullPath, string expectedCommand)
	{
		if (!key.GetValueNames().Contains(_valueName, StringComparer.OrdinalIgnoreCase))
		{
			return new StartupRegistrationStatus(StartupRegistrationState.Missing, fullPath, null);
		}
		if (key.GetValueKind(_valueName) != RegistryValueKind.String)
		{
			return new StartupRegistrationStatus(StartupRegistrationState.OtherTarget, null, "同名登录启动项不是普通字符串值，GlassDesk 不会更改它。");
		}
		if (!string.Equals(key.GetValue(_valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string, expectedCommand, StringComparison.OrdinalIgnoreCase))
		{
			return new StartupRegistrationStatus(StartupRegistrationState.OtherTarget, null, "同名登录启动项指向其它文件或包含其它参数。");
		}
		return new StartupRegistrationStatus(StartupRegistrationState.Owned, fullPath, null);
	}

	private void TryRemoveOwnedValue(string expectedCommand)
	{
		RegistryKey registryKey = null;
		try
		{
			registryKey = _openRunKey(arg: true);
			if (registryKey != null && ReadStatusFromKey(registryKey, string.Empty, expectedCommand).State == StartupRegistrationState.Owned)
			{
				registryKey.DeleteValue(_valueName, throwOnMissingValue: false);
			}
		}
		catch (Exception ex) when (IsRegistryFailure(ex))
		{
		}
		finally
		{
			if (_ownsOpenedKeys)
			{
				registryKey?.Dispose();
			}
		}
	}

	private static RegistryKey? OpenCurrentUserRunKey(bool writable)
	{
		if (!writable)
		{
			return Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: false);
		}
		return Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true);
	}

	private static bool TryCreateCommand(string executablePath, out string fullPath, out string command, out string? error)
	{
		fullPath = string.Empty;
		command = string.Empty;
		error = null;
		try
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(executablePath, "executablePath");
			if (!Path.IsPathFullyQualified(executablePath))
			{
				throw new ArgumentException("必须提供 GlassDesk.exe 的完整路径。", "executablePath");
			}
			fullPath = Path.GetFullPath(executablePath);
			if (fullPath.Contains('"'))
			{
				throw new ArgumentException("程序路径包含无效引号。", "executablePath");
			}
			if (!string.Equals(Path.GetExtension(fullPath), ".exe", StringComparison.OrdinalIgnoreCase))
			{
				throw new ArgumentException("登录启动目标必须是 .exe 文件。", "executablePath");
			}
			command = "\"" + fullPath + "\" --autostart";
			if (command.Length > 260)
			{
				throw new ArgumentException($"登录启动命令不能超过 {260} 个字符。", "executablePath");
			}
			if (!File.Exists(fullPath))
			{
				throw new FileNotFoundException("找不到 GlassDesk 可执行文件。", fullPath);
			}
			return true;
		}
		catch (Exception ex) when ((ex is ArgumentException || ex is IOException || ex is NotSupportedException || ex is SecurityException) ? true : false)
		{
			error = ex.Message;
			return false;
		}
	}

	private static bool IsRegistryFailure(Exception ex)
	{
		if (ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException || ex is ArgumentException || ex is InvalidOperationException)
		{
			return true;
		}
		return false;
	}

	private static string ToUserError(Exception ex)
	{
		if (ex is UnauthorizedAccessException || ex is SecurityException)
		{
			return "没有权限修改当前用户的登录启动项。" + Environment.NewLine + ex.Message;
		}
		return "读取或修改当前用户的登录启动项失败：" + ex.Message;
	}

	internal StartupRegistration(RegistryKey runKey, string valueName, string taskName)
		: this(runKey, valueName)
	{
		_scheduled = new ScheduledStartupRegistration(taskName);
	}

	public StartupRegistrationStatus Query(string executablePath)
	{
		StartupRegistrationStatus startupRegistrationStatus = QueryLegacy(executablePath);
		bool flag = _scheduled == null;
		if (!flag)
		{
			StartupRegistrationState state = startupRegistrationStatus.State;
			bool flag2 = (uint)(state - 2) <= 1u;
			flag = flag2;
		}
		if (flag)
		{
			return startupRegistrationStatus;
		}
		StartupRegistrationStatus startupRegistrationStatus2 = _scheduled.Query(Path.GetFullPath(executablePath));
		if (startupRegistrationStatus2.State != StartupRegistrationState.Missing)
		{
			return startupRegistrationStatus2;
		}
		return startupRegistrationStatus;
	}

	public StartupChangeResult SetEnabled(string executablePath, bool enabled)
	{
		if (_scheduled == null)
		{
			return SetLegacyEnabled(executablePath, enabled);
		}
		StartupRegistrationStatus startupRegistrationStatus = QueryLegacy(executablePath);
		StartupRegistrationState state = startupRegistrationStatus.State;
		if ((uint)(state - 2) <= 1u)
		{
			return new StartupChangeResult(Succeeded: false, startupRegistrationStatus, startupRegistrationStatus.Error);
		}
		StartupChangeResult startupChangeResult = _scheduled.SetEnabled(Path.GetFullPath(executablePath), enabled);
		if (!startupChangeResult.Succeeded)
		{
			return new StartupChangeResult(Succeeded: false, Query(executablePath), startupChangeResult.Error);
		}
		StartupChangeResult startupChangeResult2 = SetLegacyEnabled(executablePath, enabled: false);
		if (!startupChangeResult2.Succeeded)
		{
			return new StartupChangeResult(Succeeded: false, Query(executablePath), startupChangeResult2.Error);
		}
		StartupRegistrationStatus startupRegistrationStatus2 = Query(executablePath);
		StartupRegistrationState startupRegistrationState = (enabled ? StartupRegistrationState.Owned : StartupRegistrationState.Missing);
		return new StartupChangeResult(startupRegistrationStatus2.State == startupRegistrationState, startupRegistrationStatus2, (startupRegistrationStatus2.State == startupRegistrationState) ? null : (startupRegistrationStatus2.Error ?? "登录启动登记未达到预期状态。"));
	}
}
