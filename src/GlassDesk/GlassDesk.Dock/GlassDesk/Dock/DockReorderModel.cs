using System;
using System.Collections.Generic;

namespace GlassDesk.Dock;

public sealed class DockReorderModel
{
	private readonly List<string> _originalOrder = new List<string>();

	private readonly List<string> _previewOrder = new List<string>();

	private readonly Action<IReadOnlyList<string>> _save;

	private bool _active;

	public bool IsActive => _active;

	public IReadOnlyList<string> Order
	{
		get
		{
			if (!_active)
			{
				return _originalOrder;
			}
			return _previewOrder;
		}
	}

	public DockReorderModel(Action<IReadOnlyList<string>> save)
	{
		ArgumentNullException.ThrowIfNull(save, "save");
		_save = save;
	}

	public void Begin(IReadOnlyList<string> order)
	{
		ArgumentNullException.ThrowIfNull(order, "order");
		_originalOrder.Clear();
		_originalOrder.AddRange(order);
		_previewOrder.Clear();
		_previewOrder.AddRange(order);
		_active = true;
	}

	public void PreviewMove(string itemId, int targetIndex)
	{
		if (!_active)
		{
			return;
		}
		int num = _previewOrder.IndexOf(itemId);
		if (num >= 0)
		{
			targetIndex = Math.Clamp(targetIndex, 0, _previewOrder.Count - 1);
			if (targetIndex != num)
			{
				_previewOrder.RemoveAt(num);
				_previewOrder.Insert(targetIndex, itemId);
			}
		}
	}

	public ReorderOutcome Commit()
	{
		if (!_active)
		{
			return ReorderOutcome.Idle;
		}
		_active = false;
		if (SameOrder(_previewOrder, _originalOrder))
		{
			return ReorderOutcome.Unchanged;
		}
		string[] obj = _previewOrder.ToArray();
		try
		{
			_save(obj);
			return ReorderOutcome.Committed;
		}
		catch (Exception ex) when (!(ex is OutOfMemoryException))
		{
			_previewOrder.Clear();
			_previewOrder.AddRange(_originalOrder);
			return ReorderOutcome.SaveFailed;
		}
	}

	public ReorderOutcome Cancel()
	{
		if (!_active)
		{
			return ReorderOutcome.Idle;
		}
		_active = false;
		_previewOrder.Clear();
		_previewOrder.AddRange(_originalOrder);
		return ReorderOutcome.Cancelled;
	}

	private static bool SameOrder(List<string> left, List<string> right)
	{
		if (left.Count != right.Count)
		{
			return false;
		}
		for (int i = 0; i < left.Count; i++)
		{
			if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
			{
				return false;
			}
		}
		return true;
	}
}
