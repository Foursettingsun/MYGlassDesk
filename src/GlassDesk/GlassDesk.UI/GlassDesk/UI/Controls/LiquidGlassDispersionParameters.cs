using System;

namespace GlassDesk.UI.Controls;

public readonly record struct LiquidGlassDispersionParameters(double DispersionStrength, double InnerDispersionGain, double OuterDispersionGain, double FaceDispersionGain)
{
	public static LiquidGlassDispersionParameters Default { get; } = new LiquidGlassDispersionParameters(0.003, 0.25, 0.4, 0.0);

	public const double MinimumDispersionStrength = 0.0;

	public const double MaximumDispersionStrength = 0.012;

	public const double DefaultDispersionStrength = 0.003;

	public const double DispersionStrengthStep = 0.0001;

	public const double MinimumInnerDispersionGain = 0.0;

	public const double MaximumInnerDispersionGain = 1.0;

	public const double DefaultInnerDispersionGain = 0.25;

	public const double InnerDispersionGainStep = 0.025;

	public const double MinimumOuterDispersionGain = 0.0;

	public const double MaximumOuterDispersionGain = 1.0;

	public const double DefaultOuterDispersionGain = 0.4;

	public const double OuterDispersionGainStep = 0.025;

	public const double MinimumFaceDispersionGain = 0.0;

	public const double MaximumFaceDispersionGain = 0.25;

	public const double DefaultFaceDispersionGain = 0.0;

	public const double FaceDispersionGainStep = 0.01;

	public LiquidGlassDispersionParameters Normalize()
	{
		LiquidGlassDispersionParameters liquidGlassDispersionParameters = Default;
		return this with
		{
			DispersionStrength = Clamp(DispersionStrength, 0.0, 0.012, liquidGlassDispersionParameters.DispersionStrength),
			InnerDispersionGain = Clamp(InnerDispersionGain, 0.0, 1.0, liquidGlassDispersionParameters.InnerDispersionGain),
			OuterDispersionGain = Clamp(OuterDispersionGain, 0.0, 1.0, liquidGlassDispersionParameters.OuterDispersionGain),
			FaceDispersionGain = Clamp(FaceDispersionGain, 0.0, 0.25, liquidGlassDispersionParameters.FaceDispersionGain)
		};
	}

	private static double Clamp(double value, double minimum, double maximum, double fallback)
	{
		if (!double.IsFinite(value))
		{
			return fallback;
		}
		return Math.Clamp(value, minimum, maximum);
	}
}
