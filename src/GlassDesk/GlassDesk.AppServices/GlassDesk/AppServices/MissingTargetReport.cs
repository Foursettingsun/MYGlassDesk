using System.Collections.Generic;
using GlassDesk.Core.Models;

namespace GlassDesk.AppServices;

public sealed record MissingTargetReport(int Checked, int Missing, IReadOnlyList<AppEntry> NewlyBroken, IReadOnlyList<AppEntry> Recovered, bool Changed);
