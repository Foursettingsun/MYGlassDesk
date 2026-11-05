using System.Collections.Generic;

namespace GlassDesk.Platform;

public sealed record ApplicationVolume(string Key, string Name, string? ExecutablePath, string EndpointId, IReadOnlyList<string> SessionIds, double VolumePercent, bool IsMuted);
