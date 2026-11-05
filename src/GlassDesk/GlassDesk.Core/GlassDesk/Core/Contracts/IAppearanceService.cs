using GlassDesk.Core.Models.Settings;

namespace GlassDesk.Core.Contracts;

public interface IAppearanceService
{
	void ApplyAppearance(AppearanceSettings appearance);
}
