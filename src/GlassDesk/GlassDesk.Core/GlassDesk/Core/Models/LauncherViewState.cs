using System;
using System.Collections.Generic;
using System.Linq;

namespace GlassDesk.Core.Models;

public sealed class LauncherViewState
{
	private int _rows;

	private int _columns;

	private int _browseCount;

	private int[]? _browsePageCounts;

	private int _searchCount;

	private int _browsePage;

	private int _searchPage;

	private string _query = string.Empty;

	public string Query
	{
		get
		{
			return _query;
		}
		set
		{
			string text = value ?? string.Empty;
			if (!(text == _query))
			{
				bool isSearching = IsSearching;
				_query = text;
				if (!isSearching && IsSearching)
				{
					_searchPage = 0;
				}
			}
		}
	}

	public bool IsSearching => !string.IsNullOrWhiteSpace(_query);

	public string? OpenGroupId { get; set; }

	public int Rows => _rows;

	public int Columns => _columns;

	public int Capacity => _rows * _columns;

	public int ItemCount
	{
		get
		{
			if (!IsSearching)
			{
				return _browseCount;
			}
			return _searchCount;
		}
	}

	public int Page
	{
		get
		{
			if (!IsSearching)
			{
				return _browsePage;
			}
			return _searchPage;
		}
	}

	public int PageCount
	{
		get
		{
			if (!IsSearching)
			{
				int[] browsePageCounts = _browsePageCounts;
				if (browsePageCounts != null)
				{
					return Math.Max(1, browsePageCounts.Length);
				}
			}
			return Math.Max(1, (ItemCount + Capacity - 1) / Capacity);
		}
	}

	public int VisibleCount
	{
		get
		{
			if (!IsSearching)
			{
				int[] browsePageCounts = _browsePageCounts;
				if (browsePageCounts != null)
				{
					return browsePageCounts.ElementAtOrDefault(Page);
				}
			}
			return Math.Max(0, Math.Min(Capacity, ItemCount - Page * Capacity));
		}
	}

	public LauncherViewState(int rows, int columns)
	{
		SetGrid(rows, columns);
	}

	public void SetGrid(int rows, int columns)
	{
		bool flag = ((rows < 2 || rows > 10) ? true : false);
		bool flag2 = flag;
		if (!flag2)
		{
			bool flag3 = ((columns < 2 || columns > 12) ? true : false);
			flag2 = flag3;
		}
		if (flag2)
		{
			throw new ArgumentOutOfRangeException("rows", "Grid must be 2–10 rows and 2–12 columns.");
		}
		_rows = rows;
		_columns = columns;
		ClampPages();
	}

	public void SetItemCount(int count)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(count, "count");
		if (IsSearching)
		{
			_searchCount = count;
		}
		else
		{
			_browseCount = count;
			_browsePageCounts = null;
		}
		ClampPages();
	}

	public void SetPageCounts(IEnumerable<int> counts)
	{
		int[] array = counts.ToArray();
		if (array.Any((int count) => count < 0 || count > Capacity))
		{
			throw new ArgumentOutOfRangeException("counts");
		}
		_browsePageCounts = array;
		_browseCount = array.Sum();
		ClampPages();
	}

	public void MoveToPage(int page)
	{
		int num = Math.Clamp(page, 0, PageCount - 1);
		if (IsSearching)
		{
			_searchPage = num;
		}
		else
		{
			_browsePage = num;
		}
	}

	public LauncherEscapeAction Escape()
	{
		if (OpenGroupId != null)
		{
			OpenGroupId = null;
			return LauncherEscapeAction.CloseGroup;
		}
		if (IsSearching)
		{
			Query = string.Empty;
			return LauncherEscapeAction.ClearSearch;
		}
		return LauncherEscapeAction.HideWindow;
	}

	private void ClampPages()
	{
		_browsePage = Math.Min(_browsePage, Math.Max(0, (_browsePageCounts?.Length ?? ((_browseCount + Capacity - 1) / Capacity)) - 1));
		_searchPage = Math.Min(_searchPage, Math.Max(0, (_searchCount + Capacity - 1) / Capacity - 1));
	}
}
