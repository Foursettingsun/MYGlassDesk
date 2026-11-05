using System.Collections.Generic;

namespace GlassDesk.Core.Contracts;

public interface ISearchService
{
	IReadOnlyList<SearchHit> SearchApps(string query, int limit);
}
