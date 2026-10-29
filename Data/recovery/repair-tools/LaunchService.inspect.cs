using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GlassDesk.Core.Contracts;
using GlassDesk.Core.Models;
using GlassDesk.Core.Storage;
using GlassDesk.Platform;
using GlassDesk.Platform.Shell;

namespace GlassDesk.AppServices;

public sealed class LaunchService : ILaunchService
{
	private readonly AppLibraryStore _library;

	public uint LastLaunchedProcessId { get; private set; }

	public LaunchService(AppLibraryStore library)
	{
		ArgumentNullException.ThrowIfNull(library, "library");
		_library = library;
	}

	public LaunchResult LaunchItem(string itemId)
	{
		AppEntry appEntry = _library.Find(itemId);
		if (appEntry == null)
		{
			return new LaunchResult
			{
				Succeeded = false,
				Failure = LaunchFailureReason.UnknownItem,
				Message = "应用库里没有这一条记录。"
			};
		}
		if (appEntry.Kind == AppItemKind.Url)
		{
			return OpenWithShell(appEntry);
		}
		if (IsTargetGone(appEntry))
		{
			return new LaunchResult
			{
				Succeeded = false,
				Failure = LaunchFailureReason.TargetMissing,
				Message = "目标已不在原处，请重新定位：" + appEntry.Target
			};
		}
		return OpenWithShell(appEntry);
	}

	public ActivationResult ActivateOrLaunch(string itemId)
	{
		return ActivateOrLaunch(itemId, minimizeIfForeground: false);
	}

	public ActivationResult ActivateOrLaunch(string itemId, bool minimizeIfForeground)
	{
		AppEntry appEntry = _library.Find(itemId);
		if (appEntry == null)
		{
			return new ActivationResult
			{
				Outcome = ActivationOutcome.Failed,
				Message = "应用库里没有这一条记录。"
			};
		}
		if (appEntry.Kind == AppItemKind.Url)
		{
			LaunchResult launchResult = LaunchItem(itemId);
			if (!launchResult.Succeeded)
			{
				return new ActivationResult
				{
					Outcome = ActivationOutcome.Failed,
					Message = launchResult.Message
				};
			}
			return new ActivationResult
			{
				Outcome = ActivationOutcome.Launched
			};
		}
		if (IsTargetGone(appEntry))
		{
			return new ActivationResult
			{
				Outcome = ActivationOutcome.Failed,
				Message = "目标已不在原处，请重新定位：" + appEntry.Target
			};
		}
		if (string.IsNullOrWhiteSpace(WindowMatcher.ResolveAumid(appEntry)) && !WindowMatcher.CanMatchByImage(appEntry))
		{
			LaunchResult launchResult2 = LaunchItem(itemId);
			if (!launchResult2.Succeeded)
			{
				return new ActivationResult
				{
					Outcome = ActivationOutcome.Failed,
					Message = launchResult2.Message
				};
			}
			return new ActivationResult
			{
				Outcome = ActivationOutcome.Launched
			};
		}
		IReadOnlyList<WindowIdentityInfo> readOnlyList = WindowMatcher.FindWindows(appEntry);
		int count = readOnlyList.Count;
		if (count <= 1)
		{
			if (count == 1)
			{
				if (minimizeIfForeground && readOnlyList[0].Handle == WindowControl.GetForegroundWindow() && !WindowControl.IsMinimized(readOnlyList[0].Handle))
				{
					WindowControl.Minimize(readOnlyList[0].Handle);
					if (!WindowControl.IsMinimized(readOnlyList[0].Handle))
					{
						return new ActivationResult
						{
							Outcome = ActivationOutcome.Failed,
							Message = "未能最小化该窗口，请重试。"
						};
					}
					return new ActivationResult
					{
						Outcome = ActivationOutcome.Minimized
					};
				}
				return Raise(new WindowCandidateInfo(readOnlyList[0].Handle, readOnlyList[0].Title, readOnlyList[0].ProcessId, readOnlyList[0].IsMinimized));
			}
			LaunchResult launchResult3 = OpenWithShell(appEntry);
			if (!launchResult3.Succeeded)
			{
				return new ActivationResult
				{
					Outcome = ActivationOutcome.Failed,
					Message = launchResult3.Message
				};
			}
			return new ActivationResult
			{
				Outcome = ActivationOutcome.Launched
			};
		}
		return new ActivationResult
		{
			Outcome = ActivationOutcome.MultipleWindowsFound,
			Candidates = readOnlyList.Select((WindowIdentityInfo window) => new WindowCandidate
			{
				Handle = window.Handle,
				Title = window.Title
			}).ToList()
		};
	}

	public static ActivationResult ActivateWindow(nint handle)
	{
		if (handle == 0)
		{
			return new ActivationResult
			{
				Outcome = ActivationOutcome.Failed,
				Message = "没有指定窗口。"
			};
		}
		if (!WindowControl.IsAlive(handle))
		{
			return new ActivationResult
			{
				Outcome = ActivationOutcome.Failed,
				Message = "那个窗口已经关掉了，请重新点一次图标。"
			};
		}
		if (!WindowLocator.TryBringToForeground(handle))
		{
			return new ActivationResult
			{
				Outcome = ActivationOutcome.Failed,
				Message = "系统拒绝了把该窗口切到前台，请从任务栏切换。"
			};
		}
		return new ActivationResult
		{
			Outcome = ActivationOutcome.Activated
		};
	}

	public static bool IsTargetGone(AppEntry entry)
	{
		ArgumentNullException.ThrowIfNull(entry, "entry");
		if (entry.Kind == AppItemKind.Url || string.IsNullOrWhiteSpace(entry.Target))
		{
			return false;
		}
		try
		{
			return !File.Exists(entry.Target) && !Directory.Exists(entry.Target);
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			return false;
		}
	}

	private static ActivationResult Raise(WindowCandidateInfo window)
	{
		if (WindowLocator.TryBringToForeground(window.Handle))
		{
			return new ActivationResult
			{
				Outcome = ActivationOutcome.Activated
			};
		}
		return new ActivationResult
		{
			Outcome = ActivationOutcome.Failed,
			Message = "找到了正在运行的窗口，但系统拒绝了把它切到前台，请从任务栏切换。"
		};
	}

	private LaunchResult OpenWithShell(AppEntry entry)
	{
		ShellLaunchOutcome shellLaunchOutcome = ShellExecutor.Open(entry.Target, entry.Arguments, entry.WorkingDirectory);
		if (shellLaunchOutcome.Started)
		{
			LastLaunchedProcessId = shellLaunchOutcome.ProcessId;
			return new LaunchResult
			{
				Succeeded = true
			};
		}
		return new LaunchResult
		{
			Succeeded = false,
			Failure = Classify(shellLaunchOutcome),
			Message = Describe(shellLaunchOutcome, entry)
		};
	}

	private static LaunchFailureReason Classify(ShellLaunchOutcome outcome)
	{
		if ((object)outcome != null)
		{
			if (outcome.IsFileNotFound)
			{
				return LaunchFailureReason.TargetMissing;
			}
			if (outcome.IsAccessDenied)
			{
				return LaunchFailureReason.AccessDenied;
			}
			if (outcome.IsNoAssociation)
			{
				return LaunchFailureReason.Unsupported;
			}
		}
		return LaunchFailureReason.ShellRejected;
	}

	private static string Describe(ShellLaunchOutcome outcome, AppEntry entry)
	{
		if ((object)outcome == null)
		{
			goto IL_004b;
		}
		string text;
		if (!outcome.IsFileNotFound)
		{
			if (!outcome.IsAccessDenied)
			{
				if (!outcome.IsNoAssociation)
				{
					goto IL_004b;
				}
				text = "系统不知道该怎么打开这一类文件：" + entry.Target;
			}
			else
			{
				text = "系统拒绝了这次打开，目标可能需要管理员权限，或者用户取消了提权提示。";
			}
		}
		else
		{
			text = "找不到目标：" + entry.Target;
		}
		goto IL_0051;
		IL_004b:
		text = "外壳没有完成这次打开。";
		goto IL_0051;
		IL_0051:
		string value = text;
		return $"{value}（外壳错误码 {outcome.ErrorCode}）";
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.1.0.9782' (yours is '9.1.0.7988')
