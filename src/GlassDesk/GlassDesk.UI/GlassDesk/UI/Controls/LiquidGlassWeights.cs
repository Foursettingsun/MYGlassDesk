using System;

namespace GlassDesk.UI.Controls;

internal readonly record struct LiquidGlassWeights(double Face, double Inner, double Outer)
{
	internal static LiquidGlassWeights FromSdf(in LiquidGlassOptics.Sample sample, double pixelDipX, double pixelDipY, LiquidGlassWeightParameters parameters)
	{
		if (!sample.Inside)
		{
			return default;
		}
		double num = (double)Math.Abs(sample.InwardX) * pixelDipX + (double)Math.Abs(sample.InwardY) * pixelDipY;
		double num2 = Math.Max(0.0001, num * 0.5 * parameters.AntiAliasScale);
		float num3 = Math.Max(0f, sample.Depth);
		double num4 = 1.0 - SmoothStep(0.0 - num2, parameters.OuterBandWidthDip + num2, num3);
		double num5 = SmoothStep(parameters.FaceTransitionStartDip - num2, parameters.FaceTransitionStartDip + parameters.FaceTransitionWidthDip + num2, num3);
		float num6 = Math.Clamp(sample.HeightFraction, 0f, 1f);
		double num7 = 1.0 - parameters.HeightBevelInfluence + parameters.HeightBevelInfluence * (double)num6;
		double num8 = 1.0 - parameters.HeightBevelInfluence * (double)num6;
		double num9 = num5 * num7 * parameters.FaceWeightGain;
		double num10 = (1.0 - num5) * (1.0 - num4) * num7 * parameters.InnerWeightGain;
		double num11 = num4 * num8 * parameters.OuterWeightGain;
		double num12 = num9 + num10 + num11;
		if (!double.IsFinite(num12) || num12 <= 1E-09)
		{
			if (!((double)num3 >= parameters.FaceTransitionStartDip))
			{
				return new LiquidGlassWeights(0.0, 1.0, 0.0);
			}
			return new LiquidGlassWeights(1.0, 0.0, 0.0);
		}
		return new LiquidGlassWeights(num9 / num12, num10 / num12, num11 / num12);
	}

	private static double SmoothStep(double minimum, double maximum, double value)
	{
		double num = Math.Clamp((value - minimum) / Math.Max(0.0001, maximum - minimum), 0.0, 1.0);
		return num * num * (3.0 - 2.0 * num);
	}
}
