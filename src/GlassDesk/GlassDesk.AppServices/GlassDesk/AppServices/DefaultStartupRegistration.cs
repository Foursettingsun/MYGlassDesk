using GlassDesk.Core.Storage;
using GlassDesk.Platform.Installation;
using GlassDesk.Platform.Startup;

namespace GlassDesk.AppServices;

public static class DefaultStartupRegistration
{
	public static StartupChangeResult? Apply(SettingsStore settings, LoadStatus loadStatus, InstallationState installation, StartupRegistration registration, string executablePath)
	{
		if (loadStatus != LoadStatus.FirstRun || installation != InstallationState.VerifiedInstalled)
		{
			return null;
		}
		StartupRegistrationStatus startupRegistrationStatus = registration.Query(executablePath);
		StartupRegistrationState state = startupRegistrationStatus.State;
		if ((uint)(state - 2) <= 1u)
		{
			return new StartupChangeResult(Succeeded: false, startupRegistrationStatus, startupRegistrationStatus.Error ?? "现有登录启动项不属于当前程序，未自动修改。");
		}
		try
		{
			settings.Save();
		}
		catch (StorageException ex)
		{
			return new StartupChangeResult(Succeeded: false, startupRegistrationStatus, "无法保存首次运行设置：" + ex.Message);
		}
		if (startupRegistrationStatus.State != StartupRegistrationState.Owned)
		{
			return registration.SetEnabled(executablePath, enabled: true);
		}
		return new StartupChangeResult(Succeeded: true, startupRegistrationStatus, null);
	}
}
