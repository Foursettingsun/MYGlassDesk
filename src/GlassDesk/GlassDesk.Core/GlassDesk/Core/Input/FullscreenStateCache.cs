using System.Threading;

namespace GlassDesk.Core.Input;

public sealed class FullscreenStateCache
{
	private const long StateMask = 3L;

	private const int Unknown = 0;

	private const int Windowed = 1;

	private const int Fullscreen = 2;

	private long _state;

	public long Generation => Volatile.Read(in _state) >> 2;

	public long MarkUnknown()
	{
		long num;
		long num2;
		long value;
		do
		{
			num = Volatile.Read(in _state);
			num2 = (num >> 2) + 1;
			value = num2 << 2;
		}
		while (Interlocked.CompareExchange(ref _state, value, num) != num);
		return num2;
	}

	public void Update(bool isFullscreen)
	{
		Update(0L, isFullscreen);
	}

	public void Update(long generation, bool isFullscreen)
	{
		int num = ((!isFullscreen) ? 1 : 2);
		long num2;
		long value;
		do
		{
			num2 = Volatile.Read(in _state);
			if (num2 >> 2 != generation)
			{
				break;
			}
			value = (generation << 2) | num;
		}
		while (Interlocked.CompareExchange(ref _state, value, num2) != num2);
	}

	public bool CanRunPassive(bool suppressWhenFullscreen, bool monitorAvailable = true)
	{
		if (!suppressWhenFullscreen)
		{
			return true;
		}
		if (monitorAvailable)
		{
			return (Volatile.Read(in _state) & 3) == 1;
		}
		return false;
	}
}
