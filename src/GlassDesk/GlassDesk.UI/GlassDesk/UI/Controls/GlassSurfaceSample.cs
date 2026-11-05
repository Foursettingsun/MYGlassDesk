using System.Numerics;

namespace GlassDesk.UI.Controls;

public readonly record struct GlassSurfaceSample(double SignedDistanceDip, double HeightDip, Vector3 Normal);
