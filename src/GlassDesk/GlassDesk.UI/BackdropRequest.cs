internal sealed record BackdropRequest(PixelBounds Region, int Width, int Height, long Generation)
{
	internal long SourceNotBeforeTicks { get; init; }
}
