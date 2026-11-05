using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Xml;
using System.Xml.Linq;

namespace GlassDesk.Platform.Startup;

internal sealed class ScheduledStartupRegistration
{
	private const string Marker = "GlassDesk.CurrentUser.Autostart";

	private static readonly XNamespace Ns = "http://schemas.microsoft.com/windows/2004/02/mit/task";

	private readonly string _taskName;

	private readonly string _userSid;

	private readonly string _userName;

	internal ScheduledStartupRegistration(string? taskName = null)
	{
		_userSid = WindowsIdentity.GetCurrent().User.Value;
		_userName = WindowsIdentity.GetCurrent().Name;
		_taskName = taskName ?? ("GlassDesk.Autostart-" + _userSid);
	}

	internal StartupRegistrationStatus Query(string executable)
	{
		try
		{
			return WithFolder((dynamic folder) =>
			{
				object task = GetTask(folder);
				try
				{
					return ReadStatus(task, executable);
				}
				finally
				{
					Release(task);
				}
			});
		}
		catch (Exception ex) when (IsFailure(ex))
		{
			return new StartupRegistrationStatus(StartupRegistrationState.Unavailable, executable, "无法读取登录启动计划任务：" + ex.Message);
		}
	}

	internal StartupChangeResult SetEnabled(string executable, bool enabled)
	{
		try
		{
			return WithFolder((dynamic folder) =>
			{
				object task = GetTask(folder);
				bool flag = task != null;
				StartupRegistrationStatus startupRegistrationStatus;
				try
				{
					startupRegistrationStatus = ReadStatus(task, executable);
				}
				finally
				{
					Release(task);
				}
				StartupRegistrationState state = startupRegistrationStatus.State;
				if ((uint)(state - 2) <= 1u)
				{
					return new StartupChangeResult(Succeeded: false, startupRegistrationStatus, startupRegistrationStatus.Error);
				}
				if (enabled)
				{
					Release((object?)folder.RegisterTask(_taskName, CreateXml(executable), flag ? 4 : 2, _userSid, null, 3, null));
				}
				else if (flag)
				{
					folder.DeleteTask(_taskName, 0);
				}
				StartupRegistrationStatus startupRegistrationStatus2 = Query(executable);
				StartupRegistrationState startupRegistrationState = (enabled ? StartupRegistrationState.Owned : StartupRegistrationState.Missing);
				return new StartupChangeResult(startupRegistrationStatus2.State == startupRegistrationState, startupRegistrationStatus2, (startupRegistrationStatus2.State == startupRegistrationState) ? null : (startupRegistrationStatus2.Error ?? "计划任务写入后核验失败。"));
			});
		}
		catch (Exception ex) when (IsFailure(ex))
		{
			return new StartupChangeResult(Succeeded: false, Query(executable), "无法更改登录启动计划任务：" + ex.Message);
		}
	}

	private StartupRegistrationStatus ReadStatus(object? task, string executable)
	{
		if (task == null)
		{
			return new StartupRegistrationStatus(StartupRegistrationState.Missing, executable, null);
		}
		XElement? root = XDocument.Parse((string)((dynamic)task).Xml).Root;
		XElement xElement = root.Element(Ns + "Principals")?.Elements().SingleOrDefault();
		XElement[] array = root.Element(Ns + "Actions")?.Elements().ToArray() ?? Array.Empty<XElement>();
		XElement[] array2 = root.Element(Ns + "Triggers")?.Elements().ToArray() ?? Array.Empty<XElement>();
		if (!(root.Element(Ns + "RegistrationInfo")?.Element(Ns + "Source")?.Value == "GlassDesk.CurrentUser.Autostart") || !IsCurrentUser(xElement?.Element(Ns + "UserId")?.Value) || !(xElement?.Element(Ns + "LogonType")?.Value == "InteractiveToken") || !((xElement?.Element(Ns + "RunLevel")?.Value ?? "LeastPrivilege") == "LeastPrivilege") || array.Length != 1 || !(array[0].Name == Ns + "Exec") || !string.Equals(array[0].Element(Ns + "Command")?.Value, executable, StringComparison.OrdinalIgnoreCase) || !(array[0].Element(Ns + "Arguments")?.Value == "--autostart") || array2.Length != 1 || !(array2[0].Name == Ns + "LogonTrigger") || !IsCurrentUser(array2[0].Element(Ns + "UserId")?.Value))
		{
			return new StartupRegistrationStatus(StartupRegistrationState.OtherTarget, null, "同名登录任务不属于当前 GlassDesk；没有修改它。");
		}
		return new StartupRegistrationStatus(((bool)((dynamic)task).Enabled) ? StartupRegistrationState.Owned : StartupRegistrationState.Missing, executable, null);
	}

	private bool IsCurrentUser(string? value)
	{
		if (!string.Equals(value, _userSid, StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(value, _userName, StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private string CreateXml(string executable)
	{
		return new XDocument(new XElement(Ns + "Task", new XAttribute("version", "1.2"), E("RegistrationInfo", new object[2]
		{
			E("Description", "GlassDesk 登录时立即启动"),
			E("Source", "GlassDesk.CurrentUser.Autostart")
		}), E("Triggers", E("LogonTrigger", new object[2]
		{
			E("Enabled", "true"),
			E("UserId", _userSid)
		})), E("Principals", new XElement(Ns + "Principal", new XAttribute("id", "CurrentUser"), E("UserId", _userSid), E("LogonType", "InteractiveToken"), E("RunLevel", "LeastPrivilege"))), E("Settings", new object[11]
		{
			E("MultipleInstancesPolicy", "IgnoreNew"),
			E("DisallowStartIfOnBatteries", "false"),
			E("StopIfGoingOnBatteries", "false"),
			E("StartWhenAvailable", "true"),
			E("RunOnlyIfNetworkAvailable", "false"),
			E("RunOnlyIfIdle", "false"),
			E("AllowStartOnDemand", "true"),
			E("Enabled", "true"),
			E("Hidden", "false"),
			E("ExecutionTimeLimit", "PT0S"),
			E("Priority", "6")
		}), new XElement(Ns + "Actions", new XAttribute("Context", "CurrentUser"), E("Exec", new object[3]
		{
			E("Command", executable),
			E("Arguments", "--autostart"),
			E("WorkingDirectory", Path.GetDirectoryName(executable))
		})))).ToString();
		static XElement E(string name, object value)
		{
			return new XElement(Ns + name, value);
		}
	}

	private object? GetTask(dynamic folder)
	{
		try
		{
			return folder.GetTask(_taskName);
		}
		catch (Exception ex) when (ex.HResult == -2147024894)
		{
			return null;
		}
	}

	private static T WithFolder<T>(Func<dynamic, T> action)
	{
		dynamic val = Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service"));
		object obj = null;
		try
		{
			val.Connect();
			obj = val.GetFolder("\\");
			return action(obj);
		}
		finally
		{
			Release(obj);
			ScheduledStartupRegistration.Release(val);
		}
	}

	private static void Release(object? value)
	{
		if (value != null && Marshal.IsComObject(value))
		{
			Marshal.FinalReleaseComObject(value);
		}
	}

	private static bool IsFailure(Exception ex)
	{
		if (ex is COMException || ex is UnauthorizedAccessException || ex is SecurityException || ex is IOException || ex is ArgumentException || ex is InvalidOperationException || ex is XmlException)
		{
			return true;
		}
		return false;
	}
}
