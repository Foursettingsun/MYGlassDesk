using System;

namespace GlassDesk.Platform.Shell;

public sealed class DesktopIconSession : IDisposable
{
	private bool? original;

	private bool? lastWritten;

	private bool disposed;

	public bool IsSupported
	{
		get
		{
			if (!disposed)
			{
				return AreIconsVisible().HasValue;
			}
			return false;
		}
	}

	public string Status { get; private set; } = "尚未探测桌面图标状态";

	public bool? AreIconsVisible()
	{
		if (disposed)
		{
			return null;
		}
		try
		{
			bool flag = DesktopFolderView.Read();
			if (lastWritten.HasValue && lastWritten != flag)
			{
				original = null;
				lastWritten = null;
			}
			Status = "桌面 IFolderView2 状态可读";
			return flag;
		}
		catch (Exception ex)
		{
			Status = "桌面图标不支持：" + ex.Message;
			original = null;
			lastWritten = null;
			return null;
		}
	}

	public bool TrySetVisible(bool visible, out string? error)
	{
		error = null;
		bool? flag = AreIconsVisible();
		if (!flag.HasValue)
		{
			error = Status;
			return false;
		}
		if (flag.Value == visible)
		{
			return true;
		}
		try
		{
			bool? flag2 = original;
			if (!flag2.HasValue)
			{
				original = flag;
			}
			lastWritten = visible;
			DesktopFolderView.Write(visible);
			if (AreIconsVisible() != visible)
			{
				error = "桌面图标写入后的状态核对失败";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	public bool TryToggle(out string? error)
	{
		bool? flag = AreIconsVisible();
		if (!flag.HasValue)
		{
			error = Status;
			return false;
		}
		return TrySetVisible(!flag.Value, out error);
	}

	public void Dispose()
	{
		if (disposed)
		{
			return;
		}
		bool? flag = AreIconsVisible();
		if (original.HasValue && lastWritten.HasValue && flag == lastWritten)
		{
			try
			{
				DesktopFolderView.Write(original.Value);
			}
			catch
			{
			}
		}
		disposed = true;
	}
}
