using System;

namespace GlassDesk.UI.Controls;

public readonly record struct LiquidGlassWeightParameters(double OuterBandWidthDip, double FaceTransitionStartDip, double FaceTransitionWidthDip, double AntiAliasScale, double FaceWeightGain, double InnerWeightGain, double OuterWeightGain, double HeightBevelInfluence)
{
	public static LiquidGlassWeightParameters Default { get; } = new LiquidGlassWeightParameters(3.0, 8.0, 4.0, 1.0, 1.0, 1.0, 1.0, 0.5);

	public const double MinimumOuterBandWidthDip = 1.0;

	public const double MaximumOuterBandWidthDip = 5.0;

	public const double DefaultOuterBandWidthDip = 3.0;

	public const double OuterBandWidthDipStep = 0.25;

	public const double MinimumFaceTransitionStartDip = 5.0;

	public const double MaximumFaceTransitionStartDip = 14.0;

	public const double DefaultFaceTransitionStartDip = 8.0;

	public const double FaceTransitionStartDipStep = 0.25;

	public const double MinimumFaceTransitionWidthDip = 2.0;

	public const double MaximumFaceTransitionWidthDip = 8.0;

	public const double DefaultFaceTransitionWidthDip = 4.0;

	public const double FaceTransitionWidthDipStep = 0.25;

	public const double MinimumAntiAliasScale = 0.25;

	public const double MaximumAntiAliasScale = 2.0;

	public const double DefaultAntiAliasScale = 1.0;

	public const double AntiAliasScaleStep = 0.05;

	public const double MinimumFaceWeightGain = 0.25;

	public const double MaximumFaceWeightGain = 2.0;

	public const double DefaultFaceWeightGain = 1.0;

	public const double FaceWeightGainStep = 0.05;

	public const double MinimumInnerWeightGain = 0.25;

	public const double MaximumInnerWeightGain = 2.0;

	public const double DefaultInnerWeightGain = 1.0;

	public const double InnerWeightGainStep = 0.05;

	public const double MinimumOuterWeightGain = 0.25;

	public const double MaximumOuterWeightGain = 2.0;

	public const double DefaultOuterWeightGain = 1.0;

	public const double OuterWeightGainStep = 0.05;

	public const double MinimumHeightBevelInfluence = 0.0;

	public const double MaximumHeightBevelInfluence = 1.0;

	public const double DefaultHeightBevelInfluence = 0.5;

	public const double HeightBevelInfluenceStep = 0.05;

	public LiquidGlassWeightParameters Normalize()
	{
		return new LiquidGlassWeightParameters(Clamp(OuterBandWidthDip, Default.OuterBandWidthDip, 1.0, 5.0), Clamp(FaceTransitionStartDip, Default.FaceTransitionStartDip, 5.0, 14.0), Clamp(FaceTransitionWidthDip, Default.FaceTransitionWidthDip, 2.0, 8.0), Clamp(AntiAliasScale, Default.AntiAliasScale, 0.25, 2.0), Clamp(FaceWeightGain, Default.FaceWeightGain, 0.25, 2.0), Clamp(InnerWeightGain, Default.InnerWeightGain, 0.25, 2.0), Clamp(OuterWeightGain, Default.OuterWeightGain, 0.25, 2.0), Clamp(HeightBevelInfluence, Default.HeightBevelInfluence, 0.0, 1.0));
	}

	private static double Clamp(double value, double fallback, double minimum, double maximum)
	{
		if (!double.IsFinite(value))
		{
			return fallback;
		}
		return Math.Clamp(value, minimum, maximum);
	}
}
