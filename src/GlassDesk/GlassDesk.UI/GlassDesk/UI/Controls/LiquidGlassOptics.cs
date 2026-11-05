using System;
using System.Numerics;
using System.Windows;

namespace GlassDesk.UI.Controls;

public sealed class LiquidGlassOptics
{
	internal readonly record struct Sample(float X, float Y, bool Inside, float Edge, float Depth, float HeightFraction, float InwardX, float InwardY, Vector3 SurfaceNormal, float SurfaceCurvature);

	private Sample[]? _map;

	private byte[]? _debugFrame;

	private LiquidGlassDebugView _debugFrameView;

	private (int Width, int Height, Size Size, Rect GlassBounds, double Radius, bool Squircle, double Strength, double Ior, double Thickness, double Transition, double BackgroundDistance) _key;

	public const double DefaultIndexOfRefraction = 1.46;

	public byte[] Render(byte[] sourceBgra, int pixelWidth, int pixelHeight, Size sizeDip, double cornerRadiusDip, bool squircle = false, double refractionStrength = 1.0, double detailSoftnessDip = 0.0, double indexOfRefraction = 1.46, double glassThicknessDip = 4.0, double normalTransitionDip = 12.0, double backgroundDistanceDip = 8.0, LiquidGlassDebugView debugView = LiquidGlassDebugView.None, LiquidGlassSamplingParameters? samplingParameters = null, LiquidGlassWeightParameters? weightParameters = null, Rect? glassBoundsDip = null, LiquidGlassLodParameters? lodParameters = null, LiquidGlassDispersionParameters? dispersionParameters = null, bool deferDispersion = false)
	{
		ArgumentNullException.ThrowIfNull(sourceBgra, "sourceBgra");
		if (pixelWidth <= 0 || pixelHeight <= 0 || (long)pixelWidth * (long)pixelHeight > 2000000 || !double.IsFinite(sizeDip.Width) || !double.IsFinite(sizeDip.Height) || sizeDip.Width <= 0.0 || sizeDip.Height <= 0.0 || !double.IsFinite(cornerRadiusDip))
		{
			throw new ArgumentOutOfRangeException("pixelWidth");
		}
		if (sourceBgra.Length != pixelWidth * pixelHeight * 4)
		{
			throw new ArgumentException("Invalid BGRA frame.", "sourceBgra");
		}
		if (!double.IsFinite(indexOfRefraction) || indexOfRefraction < 1.0 || !double.IsFinite(glassThicknessDip) || glassThicknessDip < 0.0 || !double.IsFinite(normalTransitionDip) || normalTransitionDip <= 0.0 || !double.IsFinite(backgroundDistanceDip) || backgroundDistanceDip < 0.0)
		{
			throw new ArgumentOutOfRangeException("indexOfRefraction");
		}
		if (!Enum.IsDefined(debugView))
		{
			throw new ArgumentOutOfRangeException("debugView");
		}
		debugView = LiquidGlassFinishComposer.OpticsView(debugView);
		refractionStrength = (double.IsFinite(refractionStrength) ? Math.Clamp(refractionStrength, 0.0, 1.0) : 1.0);
		Rect rect = glassBoundsDip ?? new Rect(sizeDip);
		if (rect.IsEmpty || !double.IsFinite(rect.X) || !double.IsFinite(rect.Y) || !double.IsFinite(rect.Width) || !double.IsFinite(rect.Height) || rect.Width <= 0.0 || rect.Height <= 0.0 || !new Rect(sizeDip).Contains(rect))
		{
			throw new ArgumentOutOfRangeException("glassBoundsDip");
		}
		LiquidGlassSamplingParameters liquidGlassSamplingParameters = (samplingParameters ?? LiquidGlassSamplingParameters.Default).Normalize();
		LiquidGlassWeightParameters liquidGlassWeightParameters = (weightParameters ?? LiquidGlassWeightParameters.Default).Normalize();
		LiquidGlassLodParameters liquidGlassLodParameters = (lodParameters ?? LiquidGlassLodParameters.Default).Normalize();
		LiquidGlassDispersionParameters dispersionParameters2 = (dispersionParameters ?? LiquidGlassDispersionParameters.Default).Normalize();
		(int, int, Size, Rect, double, bool, double, double, double, double, double) tuple = (pixelWidth, pixelHeight, sizeDip, rect, cornerRadiusDip, squircle, refractionStrength, indexOfRefraction, glassThicknessDip, normalTransitionDip, backgroundDistanceDip);
		if (_map != null)
		{
			(int, int, Size, Rect, double, bool, double, double, double, double, double) key = _key;
			(int, int, Size, Rect, double, bool, double, double, double, double, double) tuple2 = tuple;
			if (key.Item1 == tuple2.Item1 && key.Item2 == tuple2.Item2 && !(key.Item3 != tuple2.Item3) && !(key.Item4 != tuple2.Item4) && key.Item5 == tuple2.Item5 && key.Item6 == tuple2.Item6 && key.Item7 == tuple2.Item7 && key.Item8 == tuple2.Item8 && key.Item9 == tuple2.Item9 && key.Item10 == tuple2.Item10 && key.Item11 == tuple2.Item11)
			{
				goto IL_07c2;
			}
		}
		_key = tuple;
		_debugFrame = null;
		int num = pixelWidth * pixelHeight;
		if (_map == null || _map.Length < num)
		{
			_map = new Sample[num];
		}
		Rect rect2 = rect;
		double val = Math.Min(15.0, Math.Min(sizeDip.Width, sizeDip.Height) * 0.24);
		Size physicalPixelSize = new Size(pixelWidth, pixelHeight);
		double num2 = (double)pixelHeight / sizeDip.Height;
		Rect sampleBoundsDip = new Rect(sizeDip);
		double num3 = Math.Clamp(normalTransitionDip / sizeDip.Height * 0.13, 0.003, 0.012) * sizeDip.Height;
		double num4 = normalTransitionDip * 0.82 + num3;
		for (int i = 0; i < pixelHeight; i++)
		{
			for (int j = 0; j < pixelWidth; j++)
			{
				Point point = new Point(((double)j + 0.5) * sizeDip.Width / (double)pixelWidth, ((double)i + 0.5) * sizeDip.Height / (double)pixelHeight);
				bool flag = (squircle ? SquircleGlassGeometry.Contains(rect2, cornerRadiusDip, point) : ContinuousGlassGeometry.Contains(rect2, cornerRadiusDip, point));
				double depth = 0.0;
				float inwardX = 0f;
				float inwardY = 0f;
				if (flag)
				{
					System.Windows.Vector inwardNormal2;
					if (squircle)
					{
						depth = 0.0 - GlassSignedDistance.Squircle(rect2, cornerRadiusDip, point);
						if (SquircleGlassGeometry.TryGetEdge(rect2, cornerRadiusDip, point, out var _, out var inwardNormal))
						{
							inwardX = (float)inwardNormal.X;
							inwardY = (float)inwardNormal.Y;
						}
					}
					else if (ContinuousGlassGeometry.TryGetEdge(rect2, cornerRadiusDip, point, out depth, out inwardNormal2))
					{
						inwardX = (float)inwardNormal2.X;
						inwardY = (float)inwardNormal2.Y;
					}
				}
				double num5 = (flag ? Math.Clamp(1.0 - depth / Math.Max(0.01, val), 0.0, 1.0) : 0.0);
				float heightFraction = (flag ? ((float)GlassHeightField.HeightFractionFromInsideDistance(depth, normalTransitionDip)) : 0f);
				float x = j;
				float y = i;
				Vector3 surfaceNormal = Vector3.UnitZ;
				if (flag && depth <= num4)
				{
					Point point2 = new Point((double)j + 0.5, (double)i + 0.5);
					GlassSurfaceSample glassSurfaceSample = GlassHeightField.SamplePhysical(rect2, cornerRadiusDip, squircle ? GlassFieldShape.Squircle : GlassFieldShape.Continuous, point2, physicalPixelSize, sampleBoundsDip, normalTransitionDip, glassThicknessDip);
					surfaceNormal = glassSurfaceSample.Normal;
					if (refractionStrength > 0.0 && GlassRayTracer.TryTrace(point2, glassSurfaceSample.Normal, glassSurfaceSample.HeightDip * num2, backgroundDistanceDip * num2, indexOfRefraction, out var backgroundPoint))
					{
						double num6 = backgroundPoint.X - 0.5;
						double num7 = backgroundPoint.Y - 0.5;
						if (num6 >= 0.0 && num6 <= (double)(pixelWidth - 1) && num7 >= 0.0 && num7 <= (double)(pixelHeight - 1))
						{
							x = (float)((double)j + (num6 - (double)j) * refractionStrength);
							y = (float)((double)i + (num7 - (double)i) * refractionStrength);
						}
					}
				}
				_map[i * pixelWidth + j] = new Sample(x, y, flag, (float)num5, (float)depth, heightFraction, inwardX, inwardY, surfaceNormal, 0f);
			}
		}
		AddSurfaceCurvature(pixelWidth, pixelHeight, sizeDip);
		goto IL_07c2;
		IL_07c2:
		if ((uint)(debugView - 29) <= 2u)
		{
			return LiquidGlassDispersion.RenderRayUv(_map, pixelWidth, pixelHeight, sizeDip, rect, normalTransitionDip, glassThicknessDip, backgroundDistanceDip, refractionStrength, liquidGlassSamplingParameters, liquidGlassWeightParameters, dispersionParameters2, indexOfRefraction, debugView switch
			{
				LiquidGlassDebugView.RRayUv => LiquidGlassDispersionChannel.Red, 
				LiquidGlassDebugView.GRayUv => LiquidGlassDispersionChannel.Green, 
				_ => LiquidGlassDispersionChannel.Blue, 
			});
		}
		bool flag2 = ((debugView == LiquidGlassDebugView.DispersionOff || (uint)(debugView - 32) <= 3u) ? true : false);
		bool flag3 = flag2;
		if ((uint)(debugView - 5) <= 2u)
		{
			return RenderSampledColor(sourceBgra, pixelWidth, pixelHeight, sizeDip, rect, normalTransitionDip, liquidGlassSamplingParameters, debugView, 0.0);
		}
		if ((uint)(debugView - 8) <= 3u)
		{
			return RenderWeightDebug(pixelWidth, pixelHeight, sizeDip, liquidGlassWeightParameters, debugView);
		}
		if (debugView != LiquidGlassDebugView.None && debugView != LiquidGlassDebugView.FinalMixedSampling && debugView != LiquidGlassDebugView.LodOff && debugView != LiquidGlassDebugView.FaceLodOnly && debugView != LiquidGlassDebugView.InnerLodOnly && debugView != LiquidGlassDebugView.OuterLodOnly && debugView != LiquidGlassDebugView.FinalMixedWithLod && !flag3)
		{
			return RenderDebugView(pixelWidth, pixelHeight, sizeDip, rect, cornerRadiusDip, squircle, glassThicknessDip, normalTransitionDip, debugView);
		}
		if (refractionStrength > 0.0)
		{
			detailSoftnessDip = (double.IsFinite(detailSoftnessDip) ? Math.Clamp(detailSoftnessDip, 0.0, 6.0) : 0.0);
			byte[] array = Filter(sourceBgra, pixelWidth, pixelHeight, Math.Max(1, (int)Math.Round((4.2 + detailSoftnessDip) * (double)pixelWidth / sizeDip.Width)), Math.Max(1, (int)Math.Round((4.2 + detailSoftnessDip) * (double)pixelHeight / sizeDip.Height)));
			byte[] array2 = Filter(array, pixelWidth, pixelHeight, Math.Max(1, (int)Math.Round(9.0 * (double)pixelWidth / sizeDip.Width)), Math.Max(1, (int)Math.Round(9.0 * (double)pixelHeight / sizeDip.Height)));
			for (int k = 0; k < array.Length; k++)
			{
				double num8 = 0.18 + 0.3 * (double)_map[k / 4].Edge;
				array[k] = (byte)Math.Round((double)(int)sourceBgra[k] * (1.0 - refractionStrength) + ((double)(int)array[k] * (1.0 - num8) + (double)(int)array2[k] * num8) * refractionStrength);
			}
			sourceBgra = array;
		}
		LiquidGlassDebugView debugView2 = ((!flag3) ? debugView : LiquidGlassDebugView.None);
		byte[] array3 = RenderMixedColor(sourceBgra, pixelWidth, pixelHeight, sizeDip, rect, normalTransitionDip, liquidGlassSamplingParameters, liquidGlassWeightParameters, liquidGlassLodParameters, debugView2);
		if (deferDispersion || debugView == LiquidGlassDebugView.DispersionOff || (debugView != LiquidGlassDebugView.None && !flag3))
		{
			return array3;
		}
		return LiquidGlassDispersion.Apply(sourceBgra, array3, _map, pixelWidth, pixelHeight, sizeDip, rect, normalTransitionDip, glassThicknessDip, backgroundDistanceDip, refractionStrength, liquidGlassSamplingParameters, liquidGlassWeightParameters, liquidGlassLodParameters, dispersionParameters2, debugView, indexOfRefraction);
	}

	internal byte[] ApplyBrdf(byte[] opticalBgra, Size sampleSizeDip, LiquidGlassWeightParameters weightParameters, LiquidGlassBrdfParameters brdfParameters, LiquidGlassDebugView debugView)
	{
		if (_map == null)
		{
			throw new InvalidOperationException("Optical samples must be built before BRDF composition.");
		}
		return LiquidGlassBrdf.Apply(opticalBgra, _map, _key.Width, _key.Height, sampleSizeDip, weightParameters, brdfParameters, 1.46, debugView);
	}

	internal byte[] ApplyDispersion(byte[] sourceBgra, byte[] finishedBgra, LiquidGlassSamplingParameters samplingParameters, LiquidGlassWeightParameters weightParameters, LiquidGlassLodParameters lodParameters, LiquidGlassDispersionParameters dispersionParameters, LiquidGlassDebugView debugView)
	{
		if (_map == null)
		{
			throw new InvalidOperationException("Optical samples must be built before dispersion composition.");
		}
		ArgumentNullException.ThrowIfNull(sourceBgra, "sourceBgra");
		ArgumentNullException.ThrowIfNull(finishedBgra, "finishedBgra");
		int num = checked(_key.Width * _key.Height * 4);
		if (sourceBgra.Length != num || finishedBgra.Length != num)
		{
			throw new ArgumentException("Dispersion buffers must match the most recently rendered optical frame.");
		}
		return LiquidGlassDispersion.Apply(sourceBgra, finishedBgra, _map, _key.Width, _key.Height, _key.Size, _key.GlassBounds, _key.Transition, _key.Thickness, _key.BackgroundDistance, _key.Strength, samplingParameters, weightParameters, lodParameters, dispersionParameters, debugView, _key.Ior);
	}

	private void AddSurfaceCurvature(int pixelWidth, int pixelHeight, Size sizeDip)
	{
		double num = sizeDip.Width / (double)pixelWidth;
		double num2 = sizeDip.Height / (double)pixelHeight;
		for (int i = 0; i < pixelHeight; i++)
		{
			for (int j = 0; j < pixelWidth; j++)
			{
				int num3 = i * pixelWidth + j;
				Sample sample = _map[num3];
				if (!sample.Inside || sample.Depth <= 0f || (double)(Math.Abs(sample.InwardX) + Math.Abs(sample.InwardY)) < 1E-05)
				{
					continue;
				}
				int num4 = (int)Math.Round((double)sample.InwardX / num);
				int num5 = (int)Math.Round((double)sample.InwardY / num2);
				if (num4 == 0 && num5 == 0)
				{
					if ((double)Math.Abs(sample.InwardX) / num >= (double)Math.Abs(sample.InwardY) / num2)
					{
						num4 = Math.Sign(sample.InwardX);
					}
					else
					{
						num5 = Math.Sign(sample.InwardY);
					}
				}
				double num6 = Math.Sqrt((double)(num4 * num4) * num * num + (double)(num5 * num5) * num2 * num2);
				if (!(num6 <= 1E-06))
				{
					int num7 = Math.Clamp(j - num4, 0, pixelWidth - 1);
					int num8 = Math.Clamp(i - num5, 0, pixelHeight - 1);
					int num9 = Math.Clamp(j + num4, 0, pixelWidth - 1);
					int num10 = Math.Clamp(i + num5, 0, pixelHeight - 1);
					Vector3 surfaceNormal = _map[num8 * pixelWidth + num7].SurfaceNormal;
					Vector3 surfaceNormal2 = _map[num10 * pixelWidth + num9].SurfaceNormal;
					double num11 = Math.Clamp((double)Vector3.Distance(surfaceNormal, surfaceNormal2) / (2.0 * num6) * 4.0, 0.0, 1.0);
					_map[num3] = sample with
					{
						SurfaceCurvature = (float)num11
					};
				}
			}
		}
	}

	private byte[] RenderWeightDebug(int pixelWidth, int pixelHeight, Size sizeDip, LiquidGlassWeightParameters tuning, LiquidGlassDebugView debugView)
	{
		byte[] array = new byte[pixelWidth * pixelHeight * 4];
		double pixelDipX = sizeDip.Width / (double)pixelWidth;
		double pixelDipY = sizeDip.Height / (double)pixelHeight;
		for (int i = 0; i < pixelWidth * pixelHeight; i++)
		{
			Sample sample = _map[i];
			if (sample.Inside)
			{
				LiquidGlassWeights liquidGlassWeights = LiquidGlassWeights.FromSdf(in sample, pixelDipX, pixelDipY, tuning);
				int num = i * 4;
				byte b = EncodeWeight(liquidGlassWeights.Face);
				byte b2 = EncodeWeight(liquidGlassWeights.Inner);
				byte b3 = EncodeWeight(liquidGlassWeights.Outer);
				switch (debugView)
				{
				case LiquidGlassDebugView.FaceWeightOnly:
					array[num] = (array[num + 1] = (array[num + 2] = b));
					break;
				case LiquidGlassDebugView.InnerWeightOnly:
					array[num] = (array[num + 1] = (array[num + 2] = b2));
					break;
				case LiquidGlassDebugView.OuterWeightOnly:
					array[num] = (array[num + 1] = (array[num + 2] = b3));
					break;
				case LiquidGlassDebugView.CombinedWeightsPreview:
					array[num] = b3;
					array[num + 1] = b2;
					array[num + 2] = b;
					break;
				default:
					throw new ArgumentOutOfRangeException("debugView");
				}
				array[num + 3] = byte.MaxValue;
			}
		}
		return array;
	}

	private byte[] RenderMixedColor(byte[] sourceBgra, int pixelWidth, int pixelHeight, Size sampleSizeDip, Rect glassBoundsDip, double normalTransitionDip, LiquidGlassSamplingParameters sampling, LiquidGlassWeightParameters weightsTuning, LiquidGlassLodParameters lodTuning, LiquidGlassDebugView debugView)
	{
		bool flag = ((debugView == LiquidGlassDebugView.None || debugView == LiquidGlassDebugView.FinalMixedWithLod) ? true : false);
		bool flag2 = flag;
		double lodFootprintDip = ((flag2 || debugView == LiquidGlassDebugView.FaceLodOnly) ? lodTuning.FaceFootprintDip : 0.0);
		double lodFootprintDip2 = ((flag2 || debugView == LiquidGlassDebugView.InnerLodOnly) ? lodTuning.InnerFootprintDip : 0.0);
		double lodFootprintDip3 = ((flag2 || debugView == LiquidGlassDebugView.OuterLodOnly) ? lodTuning.OuterFootprintDip : 0.0);
		byte[] array = RenderSampledColor(sourceBgra, pixelWidth, pixelHeight, sampleSizeDip, glassBoundsDip, normalTransitionDip, sampling, LiquidGlassDebugView.FaceOnly, lodFootprintDip);
		byte[] array2 = RenderSampledColor(sourceBgra, pixelWidth, pixelHeight, sampleSizeDip, glassBoundsDip, normalTransitionDip, sampling, LiquidGlassDebugView.InnerOnly, lodFootprintDip2);
		byte[] array3 = RenderSampledColor(sourceBgra, pixelWidth, pixelHeight, sampleSizeDip, glassBoundsDip, normalTransitionDip, sampling, LiquidGlassDebugView.OuterOnly, lodFootprintDip3);
		byte[] array4 = new byte[sourceBgra.Length];
		double pixelDipX = sampleSizeDip.Width / (double)pixelWidth;
		double pixelDipY = sampleSizeDip.Height / (double)pixelHeight;
		for (int i = 0; i < pixelWidth * pixelHeight; i++)
		{
			Sample sample = _map[i];
			if (sample.Inside)
			{
				LiquidGlassWeights liquidGlassWeights = LiquidGlassWeights.FromSdf(in sample, pixelDipX, pixelDipY, weightsTuning);
				int num = i * 4;
				for (int j = 0; j < 3; j++)
				{
					array4[num + j] = (byte)Math.Clamp((int)Math.Round((double)(int)array[num + j] * liquidGlassWeights.Face + (double)(int)array2[num + j] * liquidGlassWeights.Inner + (double)(int)array3[num + j] * liquidGlassWeights.Outer), 0, 255);
				}
				array4[num + 3] = (byte)Math.Clamp((int)Math.Round((double)(int)array[num + 3] * liquidGlassWeights.Face + (double)(int)array2[num + 3] * liquidGlassWeights.Inner + (double)(int)array3[num + 3] * liquidGlassWeights.Outer), 0, 255);
			}
		}
		return array4;
	}

	private static byte EncodeWeight(double weight)
	{
		return (byte)Math.Clamp((int)Math.Round(Math.Clamp(weight, 0.0, 1.0) * 255.0), 0, 255);
	}

	private byte[] RenderSampledColor(byte[] sourceBgra, int pixelWidth, int pixelHeight, Size sampleSizeDip, Rect glassBoundsDip, double normalTransitionDip, LiquidGlassSamplingParameters tuning, LiquidGlassDebugView debugView, double lodFootprintDip)
	{
		byte[] array = new byte[sourceBgra.Length];
		lodFootprintDip = (double.IsFinite(lodFootprintDip) ? Math.Clamp(lodFootprintDip, 0.0, 1.5) : 0.0);
		float lodOffsetX = (float)(lodFootprintDip * (double)pixelWidth / sampleSizeDip.Width);
		float lodOffsetY = (float)(lodFootprintDip * (double)pixelHeight / sampleSizeDip.Height);
		double num = glassBoundsDip.X + glassBoundsDip.Width / 2.0;
		double num2 = glassBoundsDip.Y + glassBoundsDip.Height / 2.0;
		double num3 = num * (double)pixelWidth / sampleSizeDip.Width - 0.5;
		double num4 = num2 * (double)pixelHeight / sampleSizeDip.Height - 0.5;
		double minimum = normalTransitionDip * 1.5;
		double maximum = normalTransitionDip * 3.0;
		for (int i = 0; i < pixelHeight; i++)
		{
			for (int j = 0; j < pixelWidth; j++)
			{
				int num5 = i * pixelWidth + j;
				Sample sample = _map[num5];
				if (sample.Inside)
				{
					float x = sample.X;
					float y = sample.Y;
					float num6 = sample.X - (float)j;
					float num7 = sample.Y - (float)i;
					switch (debugView)
					{
					case LiquidGlassDebugView.FaceOnly:
					{
						float num12 = SmoothStep(minimum, maximum, sample.Depth);
						double num13 = 1.0 + tuning.FaceOpticalCenterScale * (double)num12;
						x = (float)(num3 + ((double)x - num3) * num13);
						y = (float)(num4 + ((double)y - num4) * num13);
						break;
					}
					case LiquidGlassDebugView.InnerOnly:
					{
						float num10 = RimWeight(sample.Depth, tuning.InnerRimWidthDip);
						float num11 = num6 * sample.InwardX + num7 * sample.InwardY;
						x += (float)((double)(sample.InwardX * num11) * tuning.InnerRimBoost * (double)num10);
						y += (float)((double)(sample.InwardY * num11) * tuning.InnerRimBoost * (double)num10);
						break;
					}
					case LiquidGlassDebugView.OuterOnly:
					{
						float num8 = RimWeight(sample.Depth, tuning.OuterRimWidthDip);
						float num9 = Math.Abs(num6 * sample.InwardX + num7 * sample.InwardY);
						x = (float)((double)j - (double)(sample.InwardX * num9) * tuning.OuterRimStrength * (double)num8);
						y = (float)((double)i - (double)(sample.InwardY * num9) * tuning.OuterRimStrength * (double)num8);
						break;
					}
					default:
						throw new ArgumentOutOfRangeException("debugView");
					}
					int num14 = num5 * 4;
					array[num14] = ReadFootprint(sourceBgra, x, y, 0);
					array[num14 + 1] = ReadFootprint(sourceBgra, x, y, 1);
					array[num14 + 2] = ReadFootprint(sourceBgra, x, y, 2);
					array[num14 + 3] = Read(sourceBgra, x, y, 3);
				}
			}
		}
		return array;
		byte Read(byte[] source, float sx, float sy, int channel)
		{
			sx = Math.Clamp(sx, 0f, pixelWidth - 1);
			sy = Math.Clamp(sy, 0f, pixelHeight - 1);
			int num15 = (int)sx;
			int num16 = (int)sy;
			int num17 = Math.Min(num15 + 1, pixelWidth - 1);
			int num18 = Math.Min(num16 + 1, pixelHeight - 1);
			float num19 = sx - (float)num15;
			float num20 = sy - (float)num16;
			float num21 = (float)(int)source[(num16 * pixelWidth + num15) * 4 + channel] * (1f - num19) + (float)(int)source[(num16 * pixelWidth + num17) * 4 + channel] * num19;
			float num22 = (float)(int)source[(num18 * pixelWidth + num15) * 4 + channel] * (1f - num19) + (float)(int)source[(num18 * pixelWidth + num17) * 4 + channel] * num19;
			return (byte)Math.Clamp((int)Math.Round(num21 * (1f - num20) + num22 * num20), 0, 255);
		}
		byte ReadFootprint(byte[] source, float sx, float sy, int channel)
		{
			if (channel == 3 || lodFootprintDip <= 0.0)
			{
				return Read(source, sx, sy, channel);
			}
			return (byte)Math.Clamp((int)Math.Round((double)(int)Read(source, sx, sy, channel) * 0.5 + (double)(Read(source, sx - lodOffsetX, sy, channel) + Read(source, sx + lodOffsetX, sy, channel) + Read(source, sx, sy - lodOffsetY, channel) + Read(source, sx, sy + lodOffsetY, channel)) * 0.125), 0, 255);
		}
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

	private byte[] RenderDebugView(int pixelWidth, int pixelHeight, Size sizeDip, Rect glassBoundsDip, double cornerRadiusDip, bool squircle, double glassThicknessDip, double normalTransitionDip, LiquidGlassDebugView debugView)
	{
		if (_debugFrame != null && _debugFrameView == debugView)
		{
			return _debugFrame;
		}
		byte[] array = new byte[pixelWidth * pixelHeight * 4];
		Rect sampleBoundsDip = new Rect(sizeDip);
		Size physicalPixelSize = new Size(pixelWidth, pixelHeight);
		GlassFieldShape shape = (squircle ? GlassFieldShape.Squircle : GlassFieldShape.Continuous);
		double num = Math.Sqrt(32.0);
		for (int i = 0; i < pixelHeight; i++)
		{
			for (int j = 0; j < pixelWidth; j++)
			{
				int num2 = i * pixelWidth + j;
				int num3 = num2 * 4;
				switch (debugView)
				{
				case LiquidGlassDebugView.SignedDistance:
				{
					Point point = new Point(((double)j + 0.5) * sizeDip.Width / (double)pixelWidth, ((double)i + 0.5) * sizeDip.Height / (double)pixelHeight);
					double num6 = (squircle ? GlassSignedDistance.Squircle(glassBoundsDip, cornerRadiusDip, point) : GlassSignedDistance.Continuous(glassBoundsDip, cornerRadiusDip, point));
					byte b2 = (byte)Math.Clamp((int)Math.Round((0.5 + 0.5 * Math.Clamp(num6 / 16.0, -1.0, 1.0)) * 255.0), 0, 255);
					array[num3] = (array[num3 + 1] = (array[num3 + 2] = b2));
					break;
				}
				case LiquidGlassDebugView.Height:
				case LiquidGlassDebugView.Normal:
				{
					GlassSurfaceSample glassSurfaceSample = GlassHeightField.SamplePhysical(glassBoundsDip, cornerRadiusDip, shape, new Point((double)j + 0.5, (double)i + 0.5), physicalPixelSize, sampleBoundsDip, normalTransitionDip, glassThicknessDip);
					if (debugView == LiquidGlassDebugView.Height)
					{
						byte b = (byte)Math.Clamp((int)Math.Round(Math.Clamp((glassThicknessDip > 0.0) ? (glassSurfaceSample.HeightDip / glassThicknessDip) : 0.0, 0.0, 1.0) * 255.0), 0, 255);
						array[num3] = (array[num3 + 1] = (array[num3 + 2] = b));
					}
					else
					{
						array[num3] = EncodeNormal(glassSurfaceSample.Normal.Z);
						array[num3 + 1] = EncodeNormal(glassSurfaceSample.Normal.Y);
						array[num3 + 2] = EncodeNormal(glassSurfaceSample.Normal.X);
					}
					break;
				}
				case LiquidGlassDebugView.RefractionUvOffset:
				{
					Sample sample = _map[num2];
					double num4 = (double)(sample.X - (float)j) * sizeDip.Width / (double)pixelWidth;
					double num5 = (double)(sample.Y - (float)i) * sizeDip.Height / (double)pixelHeight;
					array[num3] = (byte)Math.Clamp((int)Math.Round(Math.Min(1.0, Math.Sqrt(num4 * num4 + num5 * num5) / num) * 255.0), 0, 255);
					array[num3 + 1] = EncodeSignedOffset(num5, 4.0);
					array[num3 + 2] = EncodeSignedOffset(num4, 4.0);
					break;
				}
				default:
					throw new ArgumentOutOfRangeException("debugView");
				}
				array[num3 + 3] = byte.MaxValue;
			}
		}
		_debugFrameView = debugView;
		_debugFrame = array;
		return array;
		static byte EncodeNormal(float value)
		{
			return (byte)Math.Clamp((int)Math.Round((double)(Math.Clamp(value, -1f, 1f) + 1f) * 127.5), 0, 255);
		}
		static byte EncodeSignedOffset(double value, double range)
		{
			return (byte)Math.Clamp((int)Math.Round((0.5 + 0.5 * Math.Clamp(value / range, -1.0, 1.0)) * 255.0), 0, 255);
		}
	}

	private static byte[] Filter(byte[] source, int width, int height, int radiusX, int radiusY)
	{
		radiusX = Math.Clamp(radiusX, 1, Math.Min(width, 64));
		radiusY = Math.Clamp(radiusY, 1, Math.Min(height, 64));
		byte[] array = new byte[source.Length];
		byte[] array2 = new byte[source.Length];
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < 4; j++)
			{
				int num = 0;
				for (int k = -radiusX; k <= radiusX; k++)
				{
					num += source[(i * width + Math.Clamp(k, 0, width - 1)) * 4 + j];
				}
				for (int l = 0; l < width; l++)
				{
					array[(i * width + l) * 4 + j] = (byte)((num + radiusX) / (radiusX * 2 + 1));
					num += source[(i * width + Math.Min(l + radiusX + 1, width - 1)) * 4 + j] - source[(i * width + Math.Max(l - radiusX, 0)) * 4 + j];
				}
			}
		}
		for (int m = 0; m < width; m++)
		{
			for (int n = 0; n < 4; n++)
			{
				int num2 = 0;
				for (int num3 = -radiusY; num3 <= radiusY; num3++)
				{
					num2 += array[(Math.Clamp(num3, 0, height - 1) * width + m) * 4 + n];
				}
				for (int num4 = 0; num4 < height; num4++)
				{
					array2[(num4 * width + m) * 4 + n] = (byte)((num2 + radiusY) / (radiusY * 2 + 1));
					num2 += array[(Math.Min(num4 + radiusY + 1, height - 1) * width + m) * 4 + n] - array[(Math.Max(num4 - radiusY, 0) * width + m) * 4 + n];
				}
			}
		}
		return array2;
	}
}
