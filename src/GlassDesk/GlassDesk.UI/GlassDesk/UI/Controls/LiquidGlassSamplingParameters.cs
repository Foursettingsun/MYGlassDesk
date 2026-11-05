using System;

namespace GlassDesk.UI.Controls;

public readonly record struct LiquidGlassSamplingParameters(double FaceOpticalCenterScale, double InnerRimBoost, double InnerRimWidthDip, double OuterRimStrength, double OuterRimWidthDip)
{
	public static LiquidGlassSamplingParameters Default { get; } = new LiquidGlassSamplingParameters(0.0015, 0.8, 9.0, 0.22, 2.5);

	public double OuterRimCaptureMarginDip => 1.5 * Math.Clamp(Normalize().OuterRimStrength / 0.75, 0.0, 1.0);

	public const double MinimumFaceOpticalCenterScale = 0.0;

	public const double MaximumFaceOpticalCenterScale = 0.004;

	public const double DefaultFaceOpticalCenterScale = 0.0015;

	public const double FaceOpticalCenterScaleStep = 0.0001;

	public const double MinimumInnerRimBoost = 0.0;

	public const double MaximumInnerRimBoost = 1.2;

	public const double DefaultInnerRimBoost = 0.8;

	public const double InnerRimBoostStep = 0.05;

	public const double MinimumInnerRimWidthDip = 3.0;

	public const double MaximumInnerRimWidthDip = 10.0;

	public const double DefaultInnerRimWidthDip = 9.0;

	public const double InnerRimWidthDipStep = 0.25;

	public const double MinimumOuterRimStrength = 0.0;

	public const double MaximumOuterRimStrength = 0.75;

	public const double DefaultOuterRimStrength = 0.22;

	public const double OuterRimStrengthStep = 0.025;

	public const double MinimumOuterRimWidthDip = 1.0;

	public const double MaximumOuterRimWidthDip = 5.0;

	public const double DefaultOuterRimWidthDip = 2.5;

	public const double OuterRimWidthDipStep = 0.25;

	public const double MaximumOuterRimCaptureMarginDip = 1.5;

	public LiquidGlassSamplingParameters Normalize()
	{
		return new LiquidGlassSamplingParameters(Clamp(FaceOpticalCenterScale, Default.FaceOpticalCenterScale, 0.0, 0.004), Clamp(InnerRimBoost, Default.InnerRimBoost, 0.0, 1.2), Clamp(InnerRimWidthDip, Default.InnerRimWidthDip, 3.0, 10.0), Clamp(OuterRimStrength, Default.OuterRimStrength, 0.0, 0.75), Clamp(OuterRimWidthDip, Default.OuterRimWidthDip, 1.0, 5.0));
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
