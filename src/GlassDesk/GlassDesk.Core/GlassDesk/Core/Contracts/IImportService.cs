using System.Collections.Generic;

namespace GlassDesk.Core.Contracts;

public interface IImportService
{
	ImportResult ImportItems(IReadOnlyList<ImportRequest> requests);
}
