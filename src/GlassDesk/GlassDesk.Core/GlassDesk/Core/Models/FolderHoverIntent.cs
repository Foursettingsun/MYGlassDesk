using System;

namespace GlassDesk.Core.Models;

public sealed class FolderHoverIntent
{
	private TimeSpan _entered;

	public string? TargetId { get; private set; }

	public bool IsReady { get; private set; }

	public double Progress { get; private set; }

	public bool Update(string? targetId, bool overlapping, TimeSpan timestamp)
	{
		if (!overlapping || targetId == null)
		{
			Reset();
			return false;
		}
		if (targetId != TargetId)
		{
			TargetId = targetId;
			_entered = timestamp;
		}
		Progress = Math.Clamp((timestamp - _entered).TotalMilliseconds / 500.0, 0.0, 1.0);
		return IsReady = Progress >= 1.0;
	}

	public void Reset()
	{
		TargetId = null;
		Progress = 0.0;
		IsReady = false;
	}
}
