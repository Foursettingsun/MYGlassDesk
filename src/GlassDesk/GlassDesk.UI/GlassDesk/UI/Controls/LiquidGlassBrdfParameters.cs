using System;

namespace GlassDesk.UI.Controls;

public readonly record struct LiquidGlassBrdfParameters(double Roughness, double SpecularStrength, double FresnelStrength, double LightIntensity, double BevelSpecularSupport)
{
	public static LiquidGlassBrdfParameters Default { get; } = new LiquidGlassBrdfParameters(0.5, 1.0, 1.0, 1.65, 0.6);

	public const double MinimumRoughness = 0.35;

	public const double MaximumRoughness = 0.95;

	public const double RoughnessStep = 0.02;

	public const double DefaultRoughness = 0.5;

	public const double MinimumSpecularStrength = 0.0;

	public const double MaximumSpecularStrength = 1.0;

	public const double SpecularStrengthStep = 0.02;

	public const double DefaultSpecularStrength = 1.0;

	public const double MinimumFresnelStrength = 0.0;

	public const double MaximumFresnelStrength = 1.0;

	public const double FresnelStrengthStep = 0.02;

	public const double DefaultFresnelStrength = 1.0;

	public const double MinimumLightIntensity = 0.0;

	public const double MaximumLightIntensity = 2.0;

	public const double LightIntensityStep = 0.02;

	public const double DefaultLightIntensity = 1.65;

	public const double MinimumBevelSpecularSupport = 0.0;

	public const double MaximumBevelSpecularSupport = 0.8;

	public const double BevelSpecularSupportStep = 0.02;

	public const double DefaultBevelSpecularSupport = 0.6;

	public LiquidGlassBrdfParameters Normalize()
	{
		LiquidGlassBrdfParameters liquidGlassBrdfParameters = Default;
		return this with
		{
			Roughness = Clamp(Roughness, 0.35, 0.95, liquidGlassBrdfParameters.Roughness),
			SpecularStrength = Clamp(SpecularStrength, 0.0, 1.0, liquidGlassBrdfParameters.SpecularStrength),
			FresnelStrength = Clamp(FresnelStrength, 0.0, 1.0, liquidGlassBrdfParameters.FresnelStrength),
			LightIntensity = Clamp(LightIntensity, 0.0, 2.0, liquidGlassBrdfParameters.LightIntensity),
			BevelSpecularSupport = Clamp(BevelSpecularSupport, 0.0, 0.8, liquidGlassBrdfParameters.BevelSpecularSupport)
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
