using GlassDesk.Core.Models;

namespace GlassDesk.Core.Contracts;

public sealed class SearchHit
{
	public AppEntry Item { get; init; }

	public SearchMatchKind MatchKind { get; init; }

	public int Score { get; init; }
}
