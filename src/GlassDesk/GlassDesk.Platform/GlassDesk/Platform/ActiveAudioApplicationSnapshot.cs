namespace GlassDesk.Platform;

public readonly record struct ActiveAudioApplicationSnapshot(bool Available, string Title, string ApplicationName, int ProcessId);
