using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using GlassDesk.Core.Contracts;
using GlassDesk.Core.Models;

namespace GlassDesk.AppServices;

public sealed class SearchService : ISearchService
{
	private sealed record PinyinText(string Name, string Full, string Initials)
	{
		internal long Characters => (long)Name.Length + (long)Full.Length + Initials.Length;
	}

	private readonly Func<IEnumerable<AppEntry>> _entries;

	private readonly Func<LauncherLayout> _layout;

	private const int MaximumPinyinNames = 512;

	private const int MaximumPinyinCharacters = 131072;

	private readonly Lock _pinyinGate = new Lock();

	private readonly Dictionary<string, LinkedListNode<PinyinText>> _pinyin = new Dictionary<string, LinkedListNode<PinyinText>>(StringComparer.Ordinal);

	private readonly LinkedList<PinyinText> _pinyinLru = new LinkedList<PinyinText>();

	private int _pinyinCharacters;

	public SearchService(Func<IEnumerable<AppEntry>> entries, Func<LauncherLayout> layout)
	{
		_entries = entries;
		_layout = layout;
	}

	public IReadOnlyList<SearchHit> SearchApps(string query, int limit)
	{
		string needle = Normalize(query);
		if (needle.Length == 0 || limit <= 0)
		{
			return Array.Empty<SearchHit>();
		}
		IReadOnlyList<string> source = LauncherLayoutRules.AppIds(_layout());
		Dictionary<string, int> positions = source.Select((string id, int index) => (id: id, index: index)).GroupBy(((string id, int index) pair) => pair.id, StringComparer.Ordinal).ToDictionary((IGrouping<string, (string id, int index)> group) => group.Key, (IGrouping<string, (string id, int index)> group) => group.First().index, StringComparer.Ordinal);
		return (from pair in (from pair in (from item in _entries()
					where item?.IsUsable ?? false
					select (item: item, kind: Match(item, needle))).Where(((AppEntry item, SearchMatchKind? kind) pair) =>
				{
					SearchMatchKind? item = pair.kind;
					return item.HasValue;
				})
				orderby pair.kind, positions.GetValueOrDefault(pair.item.Id, int.MaxValue)
				select pair).ThenBy(((AppEntry item, SearchMatchKind? kind) pair) => pair.item.Id, StringComparer.Ordinal).Take(limit)
			select new SearchHit
			{
				Item = pair.item,
				MatchKind = pair.kind.Value,
				Score = (int)(100 - pair.kind.Value)
			}).ToList();
	}

	private SearchMatchKind? Match(AppEntry item, string needle)
	{
		string text = Normalize(item.DisplayName);
		if (text == needle)
		{
			return SearchMatchKind.ExactName;
		}
		if (text.StartsWith(needle, StringComparison.Ordinal))
		{
			return SearchMatchKind.NamePrefix;
		}
		var (text2, text3) = PinyinFor(item.DisplayName);
		if (text2.Contains(needle, StringComparison.Ordinal))
		{
			return SearchMatchKind.Pinyin;
		}
		if (text3.Contains(needle, StringComparison.Ordinal))
		{
			return SearchMatchKind.PinyinInitials;
		}
		if (text.Contains(needle, StringComparison.Ordinal))
		{
			return SearchMatchKind.NameContains;
		}
		if ((item.SearchAliases ?? new List<string>()).Any((string alias) => Normalize(alias).Contains(needle, StringComparison.Ordinal)))
		{
			return SearchMatchKind.Alias;
		}
		return null;
	}

	private (string Full, string Initials) PinyinFor(string? text)
	{
		string text2 = text ?? string.Empty;
		using (_pinyinGate.EnterScope())
		{
			if (_pinyin.TryGetValue(text2, out LinkedListNode<PinyinText> value))
			{
				_pinyinLru.Remove(value);
				_pinyinLru.AddLast(value);
				return (Full: value.Value.Full, Initials: value.Value.Initials);
			}
			(string, string) result = OfflinePinyin.Index(text2);
			PinyinText pinyinText = new PinyinText(text2, result.Item1, result.Item2);
			if (pinyinText.Characters > 131072)
			{
				return result;
			}
			while (_pinyin.Count >= 512 || _pinyinCharacters + pinyinText.Characters > 131072)
			{
				LinkedListNode<PinyinText> first = _pinyinLru.First;
				_pinyinLru.RemoveFirst();
				_pinyin.Remove(first.Value.Name);
				_pinyinCharacters -= (int)first.Value.Characters;
			}
			_pinyin.Add(text2, _pinyinLru.AddLast(pinyinText));
			_pinyinCharacters += (int)pinyinText.Characters;
			return result;
		}
	}

	private static string Normalize(string? text)
	{
		return (text ?? string.Empty).Trim().Normalize(NormalizationForm.FormKC).ToLowerInvariant();
	}
}
