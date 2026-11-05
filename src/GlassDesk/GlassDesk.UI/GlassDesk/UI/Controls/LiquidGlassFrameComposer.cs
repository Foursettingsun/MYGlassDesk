using System;
using System.Windows;
using GlassDesk.Core.Models;

namespace GlassDesk.UI.Controls;

internal static class LiquidGlassFrameComposer
{
	internal static byte[] Compose(LiquidGlassOptics optics, byte[] sourceBgra, int pixelWidth, int pixelHeight, Size sampleSizeDip, double cornerRadiusDip, bool squircle, double refractionStrength, double detailSoftnessDip, LiquidGlassDebugView debugView, LiquidGlassSamplingParameters samplingParameters, LiquidGlassWeightParameters weightParameters, Rect glassBoundsDip, LiquidGlassLodParameters lodParameters, LiquidGlassFinishingParameters finishingParameters, LiquidGlassBrdfParameters brdfParameters, LiquidGlassDispersionParameters dispersionParameters, Action<byte[]>? afterOpticsBeforeFinishing = null, GlassMaterialMode material = GlassMaterialMode.PhysicalGlass, bool dark = true, AndroidLiquidGlassRenderer? androidRenderer = null, double tintOpacity = 0.16, double backgroundClarity = 0.5, bool launchpadOptics = false)
	{
		ArgumentNullException.ThrowIfNull(optics, "optics");
		ArgumentNullException.ThrowIfNull(sourceBgra, "sourceBgra");
		if (material == GlassMaterialMode.FrostedGlass && debugView == LiquidGlassDebugView.None)
		{
			return FrostedGlassRenderer.Render(sourceBgra, pixelWidth, pixelHeight, sampleSizeDip, glassBoundsDip, cornerRadiusDip, squircle, dark, refractionStrength, backgroundClarity);
		}
		if (material == GlassMaterialMode.AndroidLiquidGlass && debugView == LiquidGlassDebugView.None)
		{
			return (androidRenderer ?? new AndroidLiquidGlassRenderer()).Render(sourceBgra, pixelWidth, pixelHeight, sampleSizeDip, glassBoundsDip, cornerRadiusDip, squircle, dark, refractionStrength, tintOpacity, backgroundClarity, dispersionParameters, launchpadOptics);
		}
		LiquidGlassDebugView liquidGlassDebugView = LiquidGlassFinishComposer.OpticsView(debugView);
		bool flag = LiquidGlassFinishComposer.UsesDispersion(debugView);
		LiquidGlassDebugView debugView2 = (flag ? LiquidGlassDebugView.DispersionOff : liquidGlassDebugView);
		byte[] array = optics.Render(sourceBgra, pixelWidth, pixelHeight, sampleSizeDip, cornerRadiusDip, squircle, refractionStrength, detailSoftnessDip, 1.46, 4.0, 12.0, 8.0, debugView2, samplingParameters, weightParameters, glassBoundsDip, lodParameters, dispersionParameters, flag);
		afterOpticsBeforeFinishing?.Invoke(array);
		if (LiquidGlassFinishComposer.UsesTint(debugView))
		{
			array = LiquidGlassFinishComposer.ApplyTint(array, finishingParameters, AndroidLiquidGlassRenderer.NormalizeTintOpacity(tintOpacity) / 0.16);
		}
		if (LiquidGlassFinishComposer.UsesBrdf(debugView))
		{
			array = optics.ApplyBrdf(array, sampleSizeDip, weightParameters, brdfParameters, debugView);
		}
		if (flag)
		{
			array = optics.ApplyDispersion(sourceBgra, array, samplingParameters, weightParameters, lodParameters, dispersionParameters, debugView);
		}
		return array;
	}
}
