using System.Collections.Generic;

namespace GlassDesk.Platform;

public sealed record ApplicationVolumeSnapshot(bool Available, IReadOnlyList<ApplicationVolume> Applications);
