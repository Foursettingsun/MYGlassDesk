namespace GlassDesk.AppServices;

public enum RelocateResult
{
	Relocated,
	UnknownItem,
	TargetNotFound,
	ConflictsWithExisting,
	NotRelocatable,
	SaveFailed
}
