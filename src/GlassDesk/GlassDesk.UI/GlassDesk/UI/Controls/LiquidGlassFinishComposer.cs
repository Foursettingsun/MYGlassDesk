using System;

namespace GlassDesk.UI.Controls;

internal static class LiquidGlassFinishComposer
{
	internal static bool IsComparisonView(LiquidGlassDebugView view)
	{
		if ((uint)(view - 18) <= 10u || (uint)(view - 32) <= 2u)
		{
			return true;
		}
		return false;
	}

	internal static LiquidGlassDebugView OpticsView(LiquidGlassDebugView view)
	{
		if ((uint)(view - 28) > 7u)
		{
			if (!IsComparisonView(view))
			{
				return view;
			}
			return LiquidGlassDebugView.None;
		}
		return view;
	}

	internal static bool UsesTint(LiquidGlassDebugView view)
	{
		switch (view)
		{
		case LiquidGlassDebugView.None:
		case LiquidGlassDebugView.TintOnly:
		case LiquidGlassDebugView.TintAndShadowFinal:
		case LiquidGlassDebugView.BrdfOff:
		case LiquidGlassDebugView.FinalWithBrdf:
		case LiquidGlassDebugView.DispersionOff:
		case LiquidGlassDebugView.InnerDispersionOnly:
		case LiquidGlassDebugView.OuterDispersionOnly:
		case LiquidGlassDebugView.FinalWithDispersion:
			return true;
		default:
			return false;
		}
	}

	internal static bool UsesBrdf(LiquidGlassDebugView view)
	{
		switch (view)
		{
		case LiquidGlassDebugView.None:
		case LiquidGlassDebugView.TintAndShadowFinal:
		case LiquidGlassDebugView.FresnelOnly:
		case LiquidGlassDebugView.GgxSpecularOnly:
		case LiquidGlassDebugView.BrdfSupportWeight:
		case LiquidGlassDebugView.FinalWithBrdf:
		case LiquidGlassDebugView.DispersionOff:
		case LiquidGlassDebugView.InnerDispersionOnly:
		case LiquidGlassDebugView.OuterDispersionOnly:
		case LiquidGlassDebugView.FinalWithDispersion:
			return true;
		default:
			return false;
		}
	}

	internal static bool UsesDispersion(LiquidGlassDebugView view)
	{
		switch (view)
		{
		case LiquidGlassDebugView.None:
		case LiquidGlassDebugView.TintOff:
		case LiquidGlassDebugView.TintOnly:
		case LiquidGlassDebugView.AmbientShadowOnly:
		case LiquidGlassDebugView.ContactShadowOnly:
		case LiquidGlassDebugView.TintAndShadowFinal:
		case LiquidGlassDebugView.BrdfOff:
		case LiquidGlassDebugView.FinalWithBrdf:
		case LiquidGlassDebugView.InnerDispersionOnly:
		case LiquidGlassDebugView.OuterDispersionOnly:
		case LiquidGlassDebugView.FinalWithDispersion:
		case LiquidGlassDebugView.DispersionTimesTenDebug:
			return true;
		default:
			return false;
		}
	}

	internal static bool IsBrdfComponentView(LiquidGlassDebugView view)
	{
		if ((uint)(view - 24) <= 2u)
		{
			return true;
		}
		return false;
	}

	internal static bool IncludesBaselineShadow(LiquidGlassDebugView view)
	{
		if (view != LiquidGlassDebugView.AmbientShadowOnly && view != LiquidGlassDebugView.ContactShadowOnly && view != LiquidGlassDebugView.BrdfSupportWeight && view != LiquidGlassDebugView.FresnelOnly && view != LiquidGlassDebugView.GgxSpecularOnly && view != LiquidGlassDebugView.RRayUv && view != LiquidGlassDebugView.GRayUv && view != LiquidGlassDebugView.BRayUv)
		{
			return view != LiquidGlassDebugView.DispersionTimesTenDebug;
		}
		return false;
	}

	internal static (bool Ambient, bool Contact) ShadowSelection(LiquidGlassDebugView view)
	{
		switch (view)
		{
		case LiquidGlassDebugView.TintOff:
		case LiquidGlassDebugView.TintOnly:
			return (Ambient: false, Contact: false);
		case LiquidGlassDebugView.AmbientShadowOnly:
			return (Ambient: true, Contact: false);
		case LiquidGlassDebugView.ContactShadowOnly:
			return (Ambient: false, Contact: true);
		case LiquidGlassDebugView.None:
		case LiquidGlassDebugView.TintAndShadowFinal:
			return (Ambient: true, Contact: true);
		case LiquidGlassDebugView.BrdfOff:
		case LiquidGlassDebugView.FinalWithBrdf:
			return (Ambient: true, Contact: true);
		case LiquidGlassDebugView.DispersionOff:
		case LiquidGlassDebugView.InnerDispersionOnly:
		case LiquidGlassDebugView.OuterDispersionOnly:
		case LiquidGlassDebugView.FinalWithDispersion:
			return (Ambient: true, Contact: true);
		case LiquidGlassDebugView.BrdfSupportWeight:
			return (Ambient: false, Contact: false);
		case LiquidGlassDebugView.FresnelOnly:
		case LiquidGlassDebugView.GgxSpecularOnly:
			return (Ambient: false, Contact: false);
		default:
			return (Ambient: false, Contact: false);
		}
	}

	internal static byte[] ApplyTint(byte[] opticalBgra, LiquidGlassFinishingParameters parameters, double bodyTintScale = 1.0)
	{
		ArgumentNullException.ThrowIfNull(opticalBgra, "opticalBgra");
		if (opticalBgra.Length % 4 != 0)
		{
			throw new ArgumentException("Optical pixels must contain complete BGRA pixels.", "opticalBgra");
		}
		parameters = parameters.Normalize();
		if (parameters.TintOpacity <= 0.0)
		{
			return opticalBgra;
		}
		byte[] array = (byte[])opticalBgra.Clone();
		for (int i = 0; i < array.Length; i += 4)
		{
			if (opticalBgra[i + 3] != 0)
			{
				byte b = opticalBgra[i];
				byte b2 = opticalBgra[i + 1];
				byte b3 = opticalBgra[i + 2];
				double num = (0.2126 * (double)(int)b3 + 0.7152 * (double)(int)b2 + 0.0722 * (double)(int)b) / 255.0;
				double opacity = Math.Clamp(parameters.TintOpacity * bodyTintScale, 0.0, 1.0) * (1.0 - parameters.TintLuminanceInfluence * num);
				array[i] = Mix(b, parameters.TintColor.B, opacity);
				array[i + 1] = Mix(b2, parameters.TintColor.G, opacity);
				array[i + 2] = Mix(b3, parameters.TintColor.R, opacity);
			}
		}
		return array;
	}

	private static byte Mix(byte source, byte tint, double opacity)
	{
		return (byte)Math.Clamp((int)Math.Round((double)(int)source * (1.0 - opacity) + (double)(int)tint * opacity), 0, 255);
	}
}
