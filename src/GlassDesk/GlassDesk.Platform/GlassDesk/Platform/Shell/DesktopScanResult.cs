using System.Collections.Generic;

namespace GlassDesk.Platform.Shell;

public sealed record DesktopScanResult(IReadOnlyList<DesktopItem> Items, IReadOnlyList<string> Warnings);
