using System;
using System.Windows.Media;

namespace GlassDesk.UI.Controls;

public readonly record struct LiquidGlassFinishingParameters(Color TintColor, double TintOpacity, double TintLuminanceInfluence, double AmbientShadowOpacity, double AmbientShadowExtentDip, double AmbientShadowOffsetDip, double ContactShadowOpacity, double ContactShadowExtentDip, double ContactShadowOffsetDip)
{
	public static LiquidGlassFinishingParameters Default { get; } = new LiquidGlassFinishingParameters(Color.FromRgb(232, 239, 249), 0.02, 0.9, 0.06, 32.0, 6.0, 0.045, 6.0, 1.0);

	public const double MinimumTintOpacity = 0.0;

	public const double MaximumTintOpacity = 0.08;

	public const double TintOpacityStep = 0.002;

	public const double DefaultTintOpacity = 0.02;

	public const double MinimumTintLuminanceInfluence = 0.0;

	public const double MaximumTintLuminanceInfluence = 1.0;

	public const double TintLuminanceInfluenceStep = 0.05;

	public const double DefaultTintLuminanceInfluence = 0.9;

	public const double MinimumAmbientShadowOpacity = 0.0;

	public const double MaximumAmbientShadowOpacity = 0.12;

	public const double AmbientShadowOpacityStep = 0.005;

	public const double DefaultAmbientShadowOpacity = 0.06;

	public const double MinimumAmbientShadowExtentDip = 1.0;

	public const double MaximumAmbientShadowExtentDip = 34.0;

	public const double AmbientShadowExtentDipStep = 0.5;

	public const double DefaultAmbientShadowExtentDip = 32.0;

	public const double MinimumAmbientShadowOffsetDip = 0.0;

	public const double MaximumAmbientShadowOffsetDip = 8.0;

	public const double AmbientShadowOffsetDipStep = 0.25;

	public const double DefaultAmbientShadowOffsetDip = 6.0;

	public const double MinimumContactShadowOpacity = 0.0;

	public const double MaximumContactShadowOpacity = 0.1;

	public const double ContactShadowOpacityStep = 0.005;

	public const double DefaultContactShadowOpacity = 0.045;

	public const double MinimumContactShadowExtentDip = 0.5;

	public const double MaximumContactShadowExtentDip = 10.0;

	public const double ContactShadowExtentDipStep = 0.5;

	public const double DefaultContactShadowExtentDip = 6.0;

	public const double MinimumContactShadowOffsetDip = 0.0;

	public const double MaximumContactShadowOffsetDip = 3.0;

	public const double ContactShadowOffsetDipStep = 0.25;

	public const double DefaultContactShadowOffsetDip = 1.0;

	public LiquidGlassFinishingParameters Normalize()
	{
		LiquidGlassFinishingParameters liquidGlassFinishingParameters = Default;
		return this with
		{
			TintColor = Color.FromRgb(TintColor.R, TintColor.G, TintColor.B),
			TintOpacity = Clamp(TintOpacity, 0.0, 0.08, liquidGlassFinishingParameters.TintOpacity),
			TintLuminanceInfluence = Clamp(TintLuminanceInfluence, 0.0, 1.0, liquidGlassFinishingParameters.TintLuminanceInfluence),
			AmbientShadowOpacity = Clamp(AmbientShadowOpacity, 0.0, 0.12, liquidGlassFinishingParameters.AmbientShadowOpacity),
			AmbientShadowExtentDip = Clamp(AmbientShadowExtentDip, 1.0, 34.0, liquidGlassFinishingParameters.AmbientShadowExtentDip),
			AmbientShadowOffsetDip = Clamp(AmbientShadowOffsetDip, 0.0, 8.0, liquidGlassFinishingParameters.AmbientShadowOffsetDip),
			ContactShadowOpacity = Clamp(ContactShadowOpacity, 0.0, 0.1, liquidGlassFinishingParameters.ContactShadowOpacity),
			ContactShadowExtentDip = Clamp(ContactShadowExtentDip, 0.5, 10.0, liquidGlassFinishingParameters.ContactShadowExtentDip),
			ContactShadowOffsetDip = Clamp(ContactShadowOffsetDip, 0.0, 3.0, liquidGlassFinishingParameters.ContactShadowOffsetDip)
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
