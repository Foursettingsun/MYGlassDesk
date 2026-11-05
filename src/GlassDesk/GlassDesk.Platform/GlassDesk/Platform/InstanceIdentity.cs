using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;

namespace GlassDesk.Platform;

internal sealed class InstanceIdentity
{
	private const string MutexPrefix = "GlassDesk.SingleInstance.";

	private const string PipePrefix = "GlassDesk.Pipe.";

	public string UserSid { get; }

	public int SessionId { get; }

	public static InstanceIdentity Current
	{
		get
		{
			using WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();
			using Process process = Process.GetCurrentProcess();
			return new InstanceIdentity(windowsIdentity.User?.Value ?? throw new InvalidOperationException("无法读取当前 Windows 用户 SID。"), process.SessionId);
		}
	}

	public InstanceIdentity(string userSid, int sessionId)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(userSid, "userSid");
		if (userSid.Contains('\\') || userSid.Contains('/') || sessionId < 0)
		{
			throw new ArgumentException("单实例身份必须包含有效 SID 和非负会话编号。");
		}
		UserSid = userSid;
		SessionId = sessionId;
	}

	public string GetMutexName(string instanceName)
	{
		ValidateInstanceName(instanceName);
		return $"Local\\{"GlassDesk.SingleInstance."}{UserSid}.{SessionId}.{instanceName}";
	}

	public string GetPipeName(string instanceName)
	{
		ValidateInstanceName(instanceName);
		return $"{"GlassDesk.Pipe."}{UserSid}.{SessionId}.{instanceName}";
	}

	private static void ValidateInstanceName(string instanceName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(instanceName, "instanceName");
		if (instanceName.Contains('\\') || instanceName.Contains('/') || instanceName.Any(char.IsControl))
		{
			throw new ArgumentException("单实例名称不能包含路径分隔符或控制字符。", "instanceName");
		}
	}
}
