namespace GlassDesk.Dock;

public static class PassiveRevealPolicy
{
	public static bool CanReveal(bool inHotzone, bool edgeEnabled, bool fullscreen, bool suppressFullscreen, bool covered, bool blockCovered)
	{
		if ((inHotzone & edgeEnabled) && !(fullscreen & suppressFullscreen))
		{
			return !(covered & blockCovered);
		}
		return false;
	}

	public static bool CanRevealDuringSuppression(bool pointerOverDock, bool revealLocked)
	{
		return !revealLocked & pointerOverDock;
	}
}
