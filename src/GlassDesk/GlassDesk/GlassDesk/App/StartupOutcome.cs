namespace GlassDesk.App;

internal enum StartupOutcome
{
	Started,
	HandedOffToRunningInstance,
	NothingToDo,
	LegacyInstanceBlocked,
	UserDataLeaseUnavailable,
	PrimaryDisplayUnavailable
}
