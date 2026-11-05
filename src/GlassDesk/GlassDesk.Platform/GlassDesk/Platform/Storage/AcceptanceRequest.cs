using System.Collections.Generic;

namespace GlassDesk.Platform.Storage;

public sealed record AcceptanceRequest(string ProtectedDataRoot, string NewDataRoot, IReadOnlyList<AcceptanceOutput> Outputs);
