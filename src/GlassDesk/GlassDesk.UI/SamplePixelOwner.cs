internal sealed class SamplePixelOwner(byte[] pixels, object gate)
{
	internal readonly byte[] Pixels = pixels;

	internal readonly object Gate = gate;

	internal int References;
}
