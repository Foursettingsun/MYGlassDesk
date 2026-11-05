using System;
using System.Numerics;
using System.Windows;

namespace GlassDesk.UI.Controls;

internal static class LiquidGlassBrdf
{
	private readonly record struct AreaLight(Vector3 Direction, double Intensity, double Blue, double Green, double Red);

	private const double MaximumAddedLinearRadiance = 0.035;

	private static readonly Vector3 ViewDirection = Vector3.UnitZ;

	private static readonly AreaLight[] AreaLights = new AreaLight[4]
	{
		new AreaLight(Vector3.Normalize(new Vector3(-0.58f, -0.72f, 0.91f)), 0.34, 1.0, 0.985, 0.96),
		new AreaLight(Vector3.Normalize(new Vector3(0.71f, -0.43f, 0.88f)), 0.29, 0.96, 0.99, 1.0),
		new AreaLight(Vector3.Normalize(new Vector3(-0.44f, 0.39f, 1f)), 0.21, 0.98, 1.0, 0.99),
		new AreaLight(Vector3.Normalize(new Vector3(0.53f, 0.55f, 0.94f)), 0.16, 1.0, 0.99, 0.97)
	};

	internal static byte[] Apply(byte[] opticalBgra, LiquidGlassOptics.Sample[] samples, int pixelWidth, int pixelHeight, Size sampleSizeDip, LiquidGlassWeightParameters weightParameters, LiquidGlassBrdfParameters parameters, double indexOfRefraction, LiquidGlassDebugView debugView)
	{
		ArgumentNullException.ThrowIfNull(opticalBgra, "opticalBgra");
		ArgumentNullException.ThrowIfNull(samples, "samples");
		if (pixelWidth <= 0 || pixelHeight <= 0 || opticalBgra.Length != pixelWidth * pixelHeight * 4 || samples.Length < pixelWidth * pixelHeight || !double.IsFinite(sampleSizeDip.Width) || !double.IsFinite(sampleSizeDip.Height) || sampleSizeDip.Width <= 0.0 || sampleSizeDip.Height <= 0.0)
		{
			throw new ArgumentOutOfRangeException("pixelWidth");
		}
		if (!Enum.IsDefined(debugView))
		{
			throw new ArgumentOutOfRangeException("debugView");
		}
		if (debugView == LiquidGlassDebugView.BrdfOff)
		{
			return opticalBgra;
		}
		parameters = parameters.Normalize();
		weightParameters = weightParameters.Normalize();
		double pixelDipX = sampleSizeDip.Width / (double)pixelWidth;
		double pixelDipY = sampleSizeDip.Height / (double)pixelHeight;
		bool flag;
		switch (debugView)
		{
		case LiquidGlassDebugView.BrdfSupportWeight:
			return RenderSupportWeight(opticalBgra, samples, pixelWidth, pixelHeight, pixelDipX, pixelDipY, weightParameters, parameters);
		case LiquidGlassDebugView.None:
		case LiquidGlassDebugView.TintAndShadowFinal:
		case LiquidGlassDebugView.DispersionOff:
		case LiquidGlassDebugView.InnerDispersionOnly:
		case LiquidGlassDebugView.OuterDispersionOnly:
		case LiquidGlassDebugView.FinalWithDispersion:
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		bool flag2 = flag;
		if (debugView != LiquidGlassDebugView.FresnelOnly && debugView != LiquidGlassDebugView.GgxSpecularOnly && debugView != LiquidGlassDebugView.FinalWithBrdf && !flag2)
		{
			return opticalBgra;
		}
		flag = (uint)(debugView - 24) <= 1u;
		bool flag3 = flag;
		if (parameters.SpecularStrength <= 0.0 || parameters.FresnelStrength <= 0.0 || parameters.LightIntensity <= 0.0 || parameters.BevelSpecularSupport <= 0.0)
		{
			if (!flag3)
			{
				return opticalBgra;
			}
			return EmptyComponent(opticalBgra);
		}
		double f = DielectricF0(indexOfRefraction);
		byte[] array = (flag3 ? EmptyComponent(opticalBgra) : ((byte[])opticalBgra.Clone()));
		for (int i = 0; i < pixelWidth * pixelHeight; i++)
		{
			int num = i * 4;
			if (opticalBgra[num + 3] == 0)
			{
				continue;
			}
			LiquidGlassOptics.Sample sample = samples[i];
			double num2 = CalculateSupportWeight(sample, pixelDipX, pixelDipY, weightParameters, parameters);
			if (!(num2 <= 0.0))
			{
				Vector3 vector = NormalizeSurfaceNormal(sample.SurfaceNormal);
				float num3 = Math.Clamp(Vector3.Dot(vector, ViewDirection), 0f, 1f);
				if (!(num3 <= 0f))
				{
					(double, double, double) tuple = EvaluateEnvironment(vector, num3, f, parameters, debugView == LiquidGlassDebugView.FresnelOnly);
					double num4 = Math.Clamp(num2 * parameters.SpecularStrength * parameters.FresnelStrength, 0.0, 1.0);
					double radiance = Math.Min(0.035, tuple.Item1 * num4);
					double radiance2 = Math.Min(0.035, tuple.Item2 * num4);
					double radiance3 = Math.Min(0.035, tuple.Item3 * num4);
					byte srgb = (byte)((!flag3) ? opticalBgra[num] : 0);
					byte srgb2 = (byte)((!flag3) ? opticalBgra[num + 1] : 0);
					byte srgb3 = (byte)((!flag3) ? opticalBgra[num + 2] : 0);
					array[num] = AddLinearRadiance(srgb, radiance);
					array[num + 1] = AddLinearRadiance(srgb2, radiance2);
					array[num + 2] = AddLinearRadiance(srgb3, radiance3);
				}
			}
		}
		return array;
	}

	internal static double CalculateSupportWeight(LiquidGlassOptics.Sample sample, double pixelDipX, double pixelDipY, LiquidGlassWeightParameters weightParameters, LiquidGlassBrdfParameters parameters)
	{
		if (!sample.Inside || !double.IsFinite(sample.Depth) || !double.IsFinite(sample.HeightFraction) || !float.IsFinite(sample.SurfaceNormal.X) || !float.IsFinite(sample.SurfaceNormal.Y) || !float.IsFinite(sample.SurfaceNormal.Z))
		{
			return 0.0;
		}
		parameters = parameters.Normalize();
		Vector3 vector = NormalizeSurfaceNormal(sample.SurfaceNormal);
		double value = Math.Sqrt(Math.Max(0f, vector.X * vector.X + vector.Y * vector.Y));
		double num = SmoothStep(0.015, 0.55, value);
		if (num <= 0.0 || parameters.BevelSpecularSupport <= 0.0)
		{
			return 0.0;
		}
		float num2 = Math.Clamp(sample.HeightFraction, 0f, 1f);
		LiquidGlassWeightParameters parameters2 = weightParameters.Normalize();
		double minimum = Math.Max(parameters2.OuterBandWidthDip, parameters2.FaceTransitionStartDip * 0.5);
		double maximum = parameters2.FaceTransitionStartDip + parameters2.FaceTransitionWidthDip + parameters2.OuterBandWidthDip;
		double num3 = 1.0 - SmoothStep(minimum, maximum, sample.Depth);
		double num4 = 0.55 + 0.45 * SmoothStep(0.02, 0.45, Math.Clamp(sample.SurfaceCurvature, 0f, 1f));
		LiquidGlassWeights liquidGlassWeights = LiquidGlassWeights.FromSdf(in sample, pixelDipX, pixelDipY, parameters2);
		double val = liquidGlassWeights.Outer * (double)(1f - num2) * num;
		double val2 = liquidGlassWeights.Inner * num3 * num * num4;
		return Math.Clamp(Math.Max(val, val2) * parameters.BevelSpecularSupport, 0.0, 1.0);
	}

	internal static double DielectricF0(double indexOfRefraction)
	{
		if (!double.IsFinite(indexOfRefraction) || indexOfRefraction < 1.0)
		{
			indexOfRefraction = 1.46;
		}
		double num = (indexOfRefraction - 1.0) / (indexOfRefraction + 1.0);
		return num * num;
	}

	internal static double TrowbridgeReitzDistribution(double nDotH, double roughness)
	{
		roughness = (double.IsFinite(roughness) ? Math.Clamp(roughness, 0.05, 1.0) : 0.72);
		nDotH = (double.IsFinite(nDotH) ? Math.Clamp(nDotH, 0.0, 1.0) : 0.0);
		double num = roughness * roughness;
		double num2 = num * num;
		double num3 = nDotH * nDotH * (num2 - 1.0) + 1.0;
		return num2 / Math.Max(Math.PI * num3 * num3, 1E-12);
	}

	internal static double SmithHeightCorrelatedVisibility(double nDotV, double nDotL, double roughness)
	{
		if (!double.IsFinite(nDotV) || !double.IsFinite(nDotL))
		{
			return 0.0;
		}
		nDotV = Math.Clamp(nDotV, 0.0, 1.0);
		nDotL = Math.Clamp(nDotL, 0.0, 1.0);
		if (nDotV <= 0.0 || nDotL <= 0.0)
		{
			return 0.0;
		}
		roughness = (double.IsFinite(roughness) ? Math.Clamp(roughness, 0.05, 1.0) : 0.72);
		double num = roughness * roughness;
		double num2 = num * num;
		double num3 = nDotL * Math.Sqrt(nDotV * nDotV * (1.0 - num2) + num2);
		double num4 = nDotV * Math.Sqrt(nDotL * nDotL * (1.0 - num2) + num2);
		return 0.5 / Math.Max(num3 + num4, 1E-12);
	}

	private static byte[] RenderSupportWeight(byte[] opticalBgra, LiquidGlassOptics.Sample[] samples, int pixelWidth, int pixelHeight, double pixelDipX, double pixelDipY, LiquidGlassWeightParameters weights, LiquidGlassBrdfParameters parameters)
	{
		byte[] array = new byte[opticalBgra.Length];
		for (int i = 0; i < pixelWidth * pixelHeight; i++)
		{
			int num = i * 4;
			array[num + 3] = opticalBgra[num + 3];
			if (opticalBgra[num + 3] != 0)
			{
				byte b = Encode(CalculateSupportWeight(samples[i], pixelDipX, pixelDipY, weights, parameters));
				array[num] = (array[num + 1] = (array[num + 2] = b));
			}
		}
		return array;
	}

	private static byte[] EmptyComponent(byte[] opticalBgra)
	{
		byte[] array = new byte[opticalBgra.Length];
		for (int i = 3; i < opticalBgra.Length; i += 4)
		{
			array[i] = opticalBgra[i];
		}
		return array;
	}

	private static (double Blue, double Green, double Red) EvaluateEnvironment(Vector3 normal, double nDotV, double f0, LiquidGlassBrdfParameters parameters, bool fresnelOnly)
	{
		double num = 0.0;
		double num2 = 0.0;
		double num3 = 0.0;
		AreaLight[] areaLights = AreaLights;
		for (int i = 0; i < areaLights.Length; i++)
		{
			AreaLight areaLight = areaLights[i];
			float num4 = Math.Max(0f, Vector3.Dot(normal, areaLight.Direction));
			if (!(num4 <= 0f))
			{
				Vector3 vector = Vector3.Normalize(ViewDirection + areaLight.Direction);
				float num5 = Math.Clamp(Vector3.Dot(ViewDirection, vector), 0f, 1f);
				double num6 = f0 + (1.0 - f0) * Math.Pow(1f - num5, 5.0);
				double num7 = (fresnelOnly ? (num6 * (double)num4) : (TrowbridgeReitzDistribution(Math.Max(0f, Vector3.Dot(normal, vector)), parameters.Roughness) * SmithHeightCorrelatedVisibility(nDotV, num4, parameters.Roughness) * num6 * (double)num4)) * areaLight.Intensity * parameters.LightIntensity;
				num += num7 * areaLight.Blue;
				num2 += num7 * areaLight.Green;
				num3 += num7 * areaLight.Red;
			}
		}
		return (Blue: num, Green: num2, Red: num3);
	}

	private static Vector3 NormalizeSurfaceNormal(Vector3 normal)
	{
		if (!float.IsFinite(normal.X) || !float.IsFinite(normal.Y) || !float.IsFinite(normal.Z) || normal.LengthSquared() < 1E-08f)
		{
			return Vector3.UnitZ;
		}
		return Vector3.Normalize(normal);
	}

	private static double SmoothStep(double minimum, double maximum, double value)
	{
		double num = Math.Clamp((value - minimum) / Math.Max(0.0001, maximum - minimum), 0.0, 1.0);
		return num * num * (3.0 - 2.0 * num);
	}

	private static byte AddLinearRadiance(byte srgb, double radiance)
	{
		double num = Math.Clamp(ToLinear((double)(int)srgb / 255.0) + radiance, 0.0, 1.0);
		return (byte)Math.Clamp((int)Math.Round(((num <= 0.0031308) ? (num * 12.92) : (1.055 * Math.Pow(num, 5.0 / 12.0) - 0.055)) * 255.0), 0, 255);
	}

	private static double ToLinear(double srgb)
	{
		if (!(srgb <= 0.04045))
		{
			return Math.Pow((srgb + 0.055) / 1.055, 2.4);
		}
		return srgb / 12.92;
	}

	private static byte Encode(double weight)
	{
		return (byte)Math.Clamp((int)Math.Round(Math.Clamp(weight, 0.0, 1.0) * 255.0), 0, 255);
	}
}
