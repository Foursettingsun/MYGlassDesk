using System;
using System.Windows;

namespace GlassDesk.UI.Controls;

internal static class LiquidGlassDispersion
{
	private const double BaseUvDiagnosticRangeDip = 4.0;

	private const double DispersionUvDiagnosticRangeDip = 0.04;

	internal static double ChannelIor(double centerIor, double dispersion, LiquidGlassDispersionChannel channel)
	{
		if (!double.IsFinite(centerIor) || centerIor < 1.0)
		{
			centerIor = 1.46;
		}
		dispersion = (double.IsFinite(dispersion) ? Math.Max(0.0, dispersion) : 0.0);
		return channel switch
		{
			LiquidGlassDispersionChannel.Red => Math.Max(1.0, centerIor - dispersion), 
			LiquidGlassDispersionChannel.Green => centerIor, 
			LiquidGlassDispersionChannel.Blue => centerIor + dispersion, 
			_ => throw new ArgumentOutOfRangeException("channel"), 
		};
	}

	internal static byte[] Apply(byte[] sourceBgra, byte[] chapter09Bgra, LiquidGlassOptics.Sample[] samples, int pixelWidth, int pixelHeight, Size sampleSizeDip, Rect glassBoundsDip, double normalTransitionDip, double glassThicknessDip, double backgroundDistanceDip, double refractionStrength, LiquidGlassSamplingParameters samplingParameters, LiquidGlassWeightParameters weightParameters, LiquidGlassLodParameters lodParameters, LiquidGlassDispersionParameters dispersionParameters, LiquidGlassDebugView debugView, double indexOfRefraction)
	{
		ArgumentNullException.ThrowIfNull(sourceBgra, "sourceBgra");
		ArgumentNullException.ThrowIfNull(chapter09Bgra, "chapter09Bgra");
		ArgumentNullException.ThrowIfNull(samples, "samples");
		if (sourceBgra.Length != pixelWidth * pixelHeight * 4 || chapter09Bgra.Length != sourceBgra.Length || samples.Length < pixelWidth * pixelHeight)
		{
			throw new ArgumentException("Dispersion inputs must describe the same complete BGRA frame.");
		}
		dispersionParameters = dispersionParameters.Normalize();
		if (debugView == LiquidGlassDebugView.DispersionOff || dispersionParameters.DispersionStrength <= 0.0)
		{
			return chapter09Bgra;
		}
		int num = ((debugView != LiquidGlassDebugView.DispersionTimesTenDebug) ? 1 : 10);
		double strength = dispersionParameters.DispersionStrength * (double)num;
		bool forceInnerOnly = debugView == LiquidGlassDebugView.InnerDispersionOnly;
		bool forceOuterOnly = debugView == LiquidGlassDebugView.OuterDispersionOnly;
		LiquidGlassSamplingParameters sampling = samplingParameters.Normalize();
		LiquidGlassWeightParameters parameters = weightParameters.Normalize();
		LiquidGlassLodParameters liquidGlassLodParameters = lodParameters.Normalize();
		double pixelDipX = sampleSizeDip.Width / (double)pixelWidth;
		double pixelDipY = sampleSizeDip.Height / (double)pixelHeight;
		double physicalPerDipY = (double)pixelHeight / sampleSizeDip.Height;
		double centerPixelX = (glassBoundsDip.X + glassBoundsDip.Width / 2.0) * (double)pixelWidth / sampleSizeDip.Width - 0.5;
		double centerPixelY = (glassBoundsDip.Y + glassBoundsDip.Height / 2.0) * (double)pixelHeight / sampleSizeDip.Height - 0.5;
		double faceStartDip = normalTransitionDip * 1.5;
		double faceEndDip = normalTransitionDip * 3.0;
		float item = (float)(liquidGlassLodParameters.FaceFootprintDip * (double)pixelWidth / sampleSizeDip.Width);
		float item2 = (float)(liquidGlassLodParameters.FaceFootprintDip * (double)pixelHeight / sampleSizeDip.Height);
		float item3 = (float)(liquidGlassLodParameters.InnerFootprintDip * (double)pixelWidth / sampleSizeDip.Width);
		float item4 = (float)(liquidGlassLodParameters.InnerFootprintDip * (double)pixelHeight / sampleSizeDip.Height);
		float item5 = (float)(liquidGlassLodParameters.OuterFootprintDip * (double)pixelWidth / sampleSizeDip.Width);
		float item6 = (float)(liquidGlassLodParameters.OuterFootprintDip * (double)pixelHeight / sampleSizeDip.Height);
		byte[] array = (byte[])chapter09Bgra.Clone();
		int y;
		for (y = 0; y < pixelHeight; y++)
		{
			int x;
			for (x = 0; x < pixelWidth; x++)
			{
				int num2 = y * pixelWidth + x;
				LiquidGlassOptics.Sample sample = samples[num2];
				LiquidGlassWeights weights;
				(float faceOffsetX, float faceOffsetY, double FaceFootprintDip) faceFootprint;
				(float innerOffsetX, float innerOffsetY, double InnerFootprintDip) innerFootprint;
				(float outerOffsetX, float outerOffsetY, double OuterFootprintDip) outerFootprint;
				if (sample.Inside)
				{
					weights = LiquidGlassWeights.FromSdf(in sample, pixelDipX, pixelDipY, parameters);
					double num3 = DispersionForRegion(in sample, weights, LiquidGlassDispersionRegion.Face, strength, dispersionParameters, forceInnerOnly, forceOuterOnly);
					double num4 = DispersionForRegion(in sample, weights, LiquidGlassDispersionRegion.Inner, strength, dispersionParameters, forceInnerOnly, forceOuterOnly);
					double num5 = DispersionForRegion(in sample, weights, LiquidGlassDispersionRegion.Outer, strength, dispersionParameters, forceInnerOnly, forceOuterOnly);
					double faceDelta = num3;
					double innerDelta = num4;
					double outerDelta = num5;
					if (!(num3 + num4 + num5 <= 1E-12))
					{
						faceFootprint = (faceOffsetX: item, faceOffsetY: item2, FaceFootprintDip: liquidGlassLodParameters.FaceFootprintDip);
						innerFootprint = (innerOffsetX: item3, innerOffsetY: item4, InnerFootprintDip: liquidGlassLodParameters.InnerFootprintDip);
						outerFootprint = (outerOffsetX: item5, outerOffsetY: item6, OuterFootprintDip: liquidGlassLodParameters.OuterFootprintDip);
						byte b = BlendRegionChannel(LiquidGlassDispersionChannel.Red, 2, num3, num4, num5);
						byte b2 = BlendRegionChannel(LiquidGlassDispersionChannel.Blue, 0, faceDelta, innerDelta, outerDelta);
						int num6 = num2 * 4;
						array[num6] = b2;
						array[num6 + 2] = b;
					}
				}
				byte BlendRegionChannel(LiquidGlassDispersionChannel channel, int sourceChannel, double dispersion, double dispersion2, double dispersion3)
				{
					Point point = RegionUv(in sample, x, y, LiquidGlassDispersionRegion.Face, pixelWidth, pixelHeight, channel, dispersion, centerPixelX, centerPixelY, faceStartDip, faceEndDip, sampling, indexOfRefraction, glassThicknessDip, backgroundDistanceDip, physicalPerDipY, refractionStrength);
					Point point2 = RegionUv(in sample, x, y, LiquidGlassDispersionRegion.Inner, pixelWidth, pixelHeight, channel, dispersion2, centerPixelX, centerPixelY, faceStartDip, faceEndDip, sampling, indexOfRefraction, glassThicknessDip, backgroundDistanceDip, physicalPerDipY, refractionStrength);
					Point point3 = RegionUv(in sample, x, y, LiquidGlassDispersionRegion.Outer, pixelWidth, pixelHeight, channel, dispersion3, centerPixelX, centerPixelY, faceStartDip, faceEndDip, sampling, indexOfRefraction, glassThicknessDip, backgroundDistanceDip, physicalPerDipY, refractionStrength);
					byte b3 = ReadFootprint(sourceBgra, point.X, point.Y, sourceChannel, pixelWidth, pixelHeight, faceFootprint.faceOffsetX, faceFootprint.faceOffsetY, faceFootprint.FaceFootprintDip);
					byte b4 = ReadFootprint(sourceBgra, point2.X, point2.Y, sourceChannel, pixelWidth, pixelHeight, innerFootprint.innerOffsetX, innerFootprint.innerOffsetY, innerFootprint.InnerFootprintDip);
					byte b5 = ReadFootprint(sourceBgra, point3.X, point3.Y, sourceChannel, pixelWidth, pixelHeight, outerFootprint.outerOffsetX, outerFootprint.outerOffsetY, outerFootprint.OuterFootprintDip);
					return (byte)Math.Clamp((int)Math.Round((double)(int)b3 * weights.Face + (double)(int)b4 * weights.Inner + (double)(int)b5 * weights.Outer), 0, 255);
				}
			}
		}
		return array;
	}

	internal static byte[] RenderRayUv(LiquidGlassOptics.Sample[] samples, int pixelWidth, int pixelHeight, Size sampleSizeDip, Rect glassBoundsDip, double normalTransitionDip, double glassThicknessDip, double backgroundDistanceDip, double refractionStrength, LiquidGlassSamplingParameters samplingParameters, LiquidGlassWeightParameters weightParameters, LiquidGlassDispersionParameters dispersionParameters, double indexOfRefraction, LiquidGlassDispersionChannel channel)
	{
		ArgumentNullException.ThrowIfNull(samples, "samples");
		dispersionParameters = dispersionParameters.Normalize();
		double strength = dispersionParameters.DispersionStrength;
		double num = sampleSizeDip.Width / (double)pixelWidth;
		double num2 = sampleSizeDip.Height / (double)pixelHeight;
		double physicalPerDipY = (double)pixelHeight / sampleSizeDip.Height;
		double centerPixelX = (glassBoundsDip.X + glassBoundsDip.Width / 2.0) * (double)pixelWidth / sampleSizeDip.Width - 0.5;
		double centerPixelY = (glassBoundsDip.Y + glassBoundsDip.Height / 2.0) * (double)pixelHeight / sampleSizeDip.Height - 0.5;
		double faceStartDip = normalTransitionDip * 1.5;
		double faceEndDip = normalTransitionDip * 3.0;
		samplingParameters = samplingParameters.Normalize();
		weightParameters = weightParameters.Normalize();
		byte[] array = new byte[pixelWidth * pixelHeight * 4];
		int y;
		for (y = 0; y < pixelHeight; y++)
		{
			int x;
			for (x = 0; x < pixelWidth; x++)
			{
				int num3 = y * pixelWidth + x;
				LiquidGlassOptics.Sample sample = samples[num3];
				LiquidGlassWeights weights;
				if (sample.Inside)
				{
					weights = LiquidGlassWeights.FromSdf(in sample, num, num2, weightParameters.Normalize());
					Point point = PointFromRegions(channel);
					Point point2 = ((channel == LiquidGlassDispersionChannel.Green) ? default(Point) : PointFromRegions(LiquidGlassDispersionChannel.Green));
					bool flag = channel != LiquidGlassDispersionChannel.Green;
					double x2 = (flag ? ((point.X - point2.X) * num) : ((point.X - (double)x) * num));
					double y2 = (flag ? ((point.Y - point2.Y) * num2) : ((point.Y - (double)y) * num2));
					int num4 = num3 * 4;
					(byte, byte, byte, byte) tuple = EncodeRayVector(x2, y2, flag ? 0.04 : 4.0);
					array[num4] = tuple.Item1;
					array[num4 + 1] = tuple.Item2;
					array[num4 + 2] = tuple.Item3;
					array[num4 + 3] = tuple.Item4;
				}
				Point PointFromRegions(LiquidGlassDispersionChannel rayChannel)
				{
					double dispersion = DispersionForRegion(in sample, weights, LiquidGlassDispersionRegion.Face, strength, dispersionParameters, forceInnerOnly: false, forceOuterOnly: false);
					double dispersion2 = DispersionForRegion(in sample, weights, LiquidGlassDispersionRegion.Inner, strength, dispersionParameters, forceInnerOnly: false, forceOuterOnly: false);
					double dispersion3 = DispersionForRegion(in sample, weights, LiquidGlassDispersionRegion.Outer, strength, dispersionParameters, forceInnerOnly: false, forceOuterOnly: false);
					Point point3 = RegionUv(in sample, x, y, LiquidGlassDispersionRegion.Face, pixelWidth, pixelHeight, rayChannel, dispersion, centerPixelX, centerPixelY, faceStartDip, faceEndDip, samplingParameters, indexOfRefraction, glassThicknessDip, backgroundDistanceDip, physicalPerDipY, refractionStrength);
					Point point4 = RegionUv(in sample, x, y, LiquidGlassDispersionRegion.Inner, pixelWidth, pixelHeight, rayChannel, dispersion2, centerPixelX, centerPixelY, faceStartDip, faceEndDip, samplingParameters, indexOfRefraction, glassThicknessDip, backgroundDistanceDip, physicalPerDipY, refractionStrength);
					Point point5 = RegionUv(in sample, x, y, LiquidGlassDispersionRegion.Outer, pixelWidth, pixelHeight, rayChannel, dispersion3, centerPixelX, centerPixelY, faceStartDip, faceEndDip, samplingParameters, indexOfRefraction, glassThicknessDip, backgroundDistanceDip, physicalPerDipY, refractionStrength);
					return new Point(point3.X * weights.Face + point4.X * weights.Inner + point5.X * weights.Outer, point3.Y * weights.Face + point4.Y * weights.Inner + point5.Y * weights.Outer);
				}
			}
		}
		return array;
	}

	private static double DispersionForRegion(in LiquidGlassOptics.Sample sample, LiquidGlassWeights weights, LiquidGlassDispersionRegion region, double strength, LiquidGlassDispersionParameters parameters, bool forceInnerOnly, bool forceOuterOnly)
	{
		if ((forceInnerOnly && region != LiquidGlassDispersionRegion.Inner) || (forceOuterOnly && region != LiquidGlassDispersionRegion.Outer))
		{
			return 0.0;
		}
		double num = region switch
		{
			LiquidGlassDispersionRegion.Face => parameters.FaceDispersionGain, 
			LiquidGlassDispersionRegion.Inner => parameters.InnerDispersionGain, 
			LiquidGlassDispersionRegion.Outer => parameters.OuterDispersionGain, 
			_ => 0.0, 
		};
		double num2 = region switch
		{
			LiquidGlassDispersionRegion.Face => weights.Face, 
			LiquidGlassDispersionRegion.Inner => weights.Inner, 
			LiquidGlassDispersionRegion.Outer => weights.Outer, 
			_ => 0.0, 
		};
		float num3 = Math.Clamp(float.IsFinite(sample.SurfaceNormal.Z) ? sample.SurfaceNormal.Z : 1f, 0f, 1f);
		float num4 = Math.Clamp(Math.Max(1f - Math.Clamp(sample.HeightFraction, 0f, 1f), 1f - num3), 0f, 1f);
		float num5 = Math.Clamp(sample.SurfaceCurvature * 5f, 0f, 1f);
		double num6 = (double)num4 * (0.25 + 0.75 * (double)num5);
		return Math.Clamp(strength * num * num2 * num6, 0.0, 0.12);
	}

	private static Point RegionUv(in LiquidGlassOptics.Sample sample, int x, int y, LiquidGlassDispersionRegion region, int pixelWidth, int pixelHeight, LiquidGlassDispersionChannel channel, double dispersion, double centerPixelX, double centerPixelY, double faceStartDip, double faceEndDip, LiquidGlassSamplingParameters sampling, double indexOfRefraction, double glassThicknessDip, double backgroundDistanceDip, double physicalPerDipY, double refractionStrength)
	{
		Point ray = RefractedUv(in sample, x, y, pixelWidth, pixelHeight, channel, dispersion, indexOfRefraction, glassThicknessDip, backgroundDistanceDip, physicalPerDipY, refractionStrength);
		double refractionX = ray.X - (double)x;
		double refractionY = ray.Y - (double)y;
		return region switch
		{
			LiquidGlassDispersionRegion.Face => FaceUv(ray, sample.Depth, centerPixelX, centerPixelY, faceStartDip, faceEndDip, sampling.FaceOpticalCenterScale), 
			LiquidGlassDispersionRegion.Inner => InnerUv(ray, in sample, refractionX, refractionY, sampling.InnerRimBoost, sampling.InnerRimWidthDip), 
			LiquidGlassDispersionRegion.Outer => OuterUv(in sample, x, y, refractionX, refractionY, sampling.OuterRimStrength, sampling.OuterRimWidthDip), 
			_ => throw new ArgumentOutOfRangeException("region"), 
		};
	}

	private static Point RefractedUv(in LiquidGlassOptics.Sample sample, int x, int y, int pixelWidth, int pixelHeight, LiquidGlassDispersionChannel channel, double dispersion, double indexOfRefraction, double glassThicknessDip, double backgroundDistanceDip, double physicalPerDipY, double refractionStrength)
	{
		if (dispersion <= 0.0)
		{
			return new Point(sample.X, sample.Y);
		}
		double num = ChannelIor(indexOfRefraction, dispersion, channel);
		if (num == indexOfRefraction)
		{
			return new Point(sample.X, sample.Y);
		}
		double height = (double)Math.Max(0f, sample.HeightFraction) * glassThicknessDip * physicalPerDipY;
		if (!GlassRayTracer.TryTrace(new Point((double)x + 0.5, (double)y + 0.5), sample.SurfaceNormal, height, backgroundDistanceDip * physicalPerDipY, num, out var backgroundPoint))
		{
			return new Point(sample.X, sample.Y);
		}
		double num2 = backgroundPoint.X - 0.5;
		double num3 = backgroundPoint.Y - 0.5;
		if (num2 < 0.0 || num2 > (double)(pixelWidth - 1) || num3 < 0.0 || num3 > (double)(pixelHeight - 1))
		{
			return new Point(sample.X, sample.Y);
		}
		return new Point((double)x + (num2 - (double)x) * refractionStrength, (double)y + (num3 - (double)y) * refractionStrength);
	}

	private static Point FaceUv(Point ray, double depth, double centerPixelX, double centerPixelY, double faceStartDip, double faceEndDip, double centerScale)
	{
		double num = 1.0 + centerScale * (double)SmoothStep(faceStartDip, faceEndDip, depth);
		return new Point(centerPixelX + (ray.X - centerPixelX) * num, centerPixelY + (ray.Y - centerPixelY) * num);
	}

	private static Point InnerUv(Point ray, in LiquidGlassOptics.Sample sample, double refractionX, double refractionY, double boost, double widthDip)
	{
		float num = RimWeight(sample.Depth, widthDip);
		double num2 = refractionX * (double)sample.InwardX + refractionY * (double)sample.InwardY;
		return new Point(ray.X + (double)sample.InwardX * num2 * boost * (double)num, ray.Y + (double)sample.InwardY * num2 * boost * (double)num);
	}

	private static Point OuterUv(in LiquidGlassOptics.Sample sample, int x, int y, double refractionX, double refractionY, double strength, double widthDip)
	{
		float num = RimWeight(sample.Depth, widthDip);
		double num2 = Math.Abs(refractionX * (double)sample.InwardX + refractionY * (double)sample.InwardY);
		return new Point((double)x - (double)sample.InwardX * num2 * strength * (double)num, (double)y - (double)sample.InwardY * num2 * strength * (double)num);
	}

	private static byte ReadFootprint(byte[] source, double sampleX, double sampleY, int channel, int width, int height, float offsetX, float offsetY, double footprintDip)
	{
		if (footprintDip <= 0.0)
		{
			return Read(source, width, height, sampleX, sampleY, channel);
		}
		return (byte)Math.Clamp((int)Math.Round((double)(int)Read(source, width, height, sampleX, sampleY, channel) * 0.5 + (double)(Read(source, width, height, sampleX - (double)offsetX, sampleY, channel) + Read(source, width, height, sampleX + (double)offsetX, sampleY, channel) + Read(source, width, height, sampleX, sampleY - (double)offsetY, channel) + Read(source, width, height, sampleX, sampleY + (double)offsetY, channel)) * 0.125), 0, 255);
	}

	private static byte Read(byte[] source, int width, int height, double sampleX, double sampleY, int channel)
	{
		sampleX = Math.Clamp(sampleX, 0.0, width - 1);
		sampleY = Math.Clamp(sampleY, 0.0, height - 1);
		int num = (int)sampleX;
		int num2 = (int)sampleY;
		int num3 = Math.Min(num + 1, width - 1);
		int num4 = Math.Min(num2 + 1, height - 1);
		double num5 = sampleX - (double)num;
		double num6 = sampleY - (double)num2;
		double num7 = (double)(int)source[(num2 * width + num) * 4 + channel] * (1.0 - num5) + (double)(int)source[(num2 * width + num3) * 4 + channel] * num5;
		double num8 = (double)(int)source[(num4 * width + num) * 4 + channel] * (1.0 - num5) + (double)(int)source[(num4 * width + num3) * 4 + channel] * num5;
		return (byte)Math.Clamp((int)Math.Round(num7 * (1.0 - num6) + num8 * num6), 0, 255);
	}

	private static float RimWeight(float depthDip, double widthDip)
	{
		return SmoothStep(0.0, widthDip, (float)(widthDip - (double)depthDip));
	}

	private static float SmoothStep(double minimum, double maximum, double value)
	{
		float num = (float)Math.Clamp((value - minimum) / Math.Max(0.0001, maximum - minimum), 0.0, 1.0);
		return num * num * (3f - 2f * num);
	}

	private static (byte Blue, byte Green, byte Red, byte Alpha) EncodeRayVector(double x, double y, double range)
	{
		double num = Math.Clamp(Math.Sqrt(x * x + y * y) / range, 0.0, 1.0);
		if (num <= 0.0)
		{
			return default;
		}
		double num2 = Math.Atan2(y, x) * 3.0 / Math.PI;
		if (num2 < 0.0)
		{
			num2 += 6.0;
		}
		int num3 = (int)Math.Floor(num2) % 6;
		double num4 = num2 - Math.Floor(num2);
		double num5 = num * (1.0 - num4);
		double num6 = num * num4;
		(double, double, double) tuple = num3 switch
		{
			0 => (num, num6, 0.0), 
			1 => (num5, num, 0.0), 
			2 => (0.0, num, num6), 
			3 => (0.0, num5, num), 
			4 => (num6, 0.0, num), 
			_ => (num, 0.0, num5), 
		};
		var (component, _, _) = tuple;
		return new ValueTuple<byte, byte, byte, byte>(item2: ToByte(tuple.Item2), item1: ToByte(tuple.Item3), item3: ToByte(component), item4: ToByte(num * 220.0));
		static byte ToByte(double num7)
		{
			return (byte)Math.Clamp((int)Math.Round(num7 * 255.0), 0, 255);
		}
	}
}
