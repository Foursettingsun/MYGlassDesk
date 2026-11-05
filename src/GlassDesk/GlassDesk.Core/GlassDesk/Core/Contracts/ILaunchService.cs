namespace GlassDesk.Core.Contracts;

public interface ILaunchService
{
	LaunchResult LaunchItem(string itemId);

	ActivationResult ActivateOrLaunch(string itemId);
}
