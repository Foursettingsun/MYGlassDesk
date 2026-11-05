using System.Collections.Generic;

namespace GlassDesk.App.Startup;

internal sealed record SampleSet(string Directory, IReadOnlyList<SampleSource> ShouldImport, IReadOnlyList<SampleSource> ShouldFail, string BrokenTarget, string RelocationSource);
