using System;

namespace GlassDesk.UI.Controls;

public readonly record struct LiquidGlassLodParameters(double FaceFootprintDip, double InnerFootprintDip, double OuterFootprintDip)
{
	public static LiquidGlassLodParameters Default { get; } = new LiquidGlassLodParameters(0.7, 0.1, 0.95);

	public double MaximumActiveFootprintDip
	{
		get
		{
			LiquidGlassLodParameters liquidGlassLodParameters = Normalize();
			return Math.Max(liquidGlassLodParameters.FaceFootprintDip, Math.Max(liquidGlassLodParameters.InnerFootprintDip, liquidGlassLodParameters.OuterFootprintDip));
		}
	}

	public const double MinimumFootprintDip = 0.0;

	public const double MaximumFootprintDip = 1.5;

	public const double FootprintDipStep = 0.05;

	public const double DefaultFaceFootprintDip = 0.7;

	public const double DefaultInnerFootprintDip = 0.1;

	public const double DefaultOuterFootprintDip = 0.95;

	public LiquidGlassLodParameters Normalize()
	{
		return new LiquidGlassLodParameters(Clamp(FaceFootprintDip, 0.7), Clamp(InnerFootprintDip, 0.1), Clamp(OuterFootprintDip, 0.95));
	}

	private static double Clamp(double value, double fallback)
	{
		if (!double.IsFinite(value))
		{
			return fallback;
		}
		return Math.Clamp(value, 0.0, 1.5);
	}
}
