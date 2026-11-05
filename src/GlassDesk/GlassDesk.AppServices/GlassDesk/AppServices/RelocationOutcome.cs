namespace GlassDesk.AppServices;

public sealed record RelocationOutcome(RelocateResult Result, string Message, string? ItemId = null);
