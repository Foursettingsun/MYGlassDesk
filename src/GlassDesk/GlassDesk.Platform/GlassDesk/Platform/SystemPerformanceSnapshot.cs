namespace GlassDesk.Platform;

public readonly record struct SystemPerformanceSnapshot(int? CpuPercent, int? MemoryPercent);
