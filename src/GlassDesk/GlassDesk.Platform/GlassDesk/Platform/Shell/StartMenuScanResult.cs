using System.Collections.Generic;

namespace GlassDesk.Platform.Shell;

public sealed record StartMenuScanResult(IReadOnlyList<StartMenuShortcut> Shortcuts, int ScannedFileCount, int DuplicateNameCount, IReadOnlyList<string> Roots);
