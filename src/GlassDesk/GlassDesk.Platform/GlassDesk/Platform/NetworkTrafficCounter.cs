namespace GlassDesk.Platform;

public sealed record NetworkTrafficCounter(string Id, string Name, long ReceivedBytes, long SentBytes);
