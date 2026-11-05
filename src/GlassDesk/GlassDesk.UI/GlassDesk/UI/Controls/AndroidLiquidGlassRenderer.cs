using System;
using System.Buffers;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Windows;

namespace GlassDesk.UI.Controls;

internal sealed class AndroidLiquidGlassRenderer
{
	private readonly record struct Sample(float X, float Y, float Coverage, float Highlight, float Shade, float DispersionX, float DispersionY, float OuterBlend);

	internal const double BodyOpacity = 0.16;

	internal const double BlurDip = 3.2;

	private Sample[]? _map;

	private (int Width, int Height, Size Size, Rect Bounds, double Radius, bool Squircle, double Strength, bool Launchpad) _key;

	internal static double LensOffset(double depth, double height, double amount)
	{
		if (height <= 0.0 || amount <= 0.0 || depth >= height)
		{
			return 0.0;
		}
		double num = Math.Clamp(1.0 - Math.Max(0.0, depth) / height, 0.0, 1.0);
		return (0.0 - (1.0 - Math.Sqrt(Math.Max(0.0, 1.0 - num * num)))) * amount;
	}

	internal byte[] Render(byte[] source, int width, int height, Size size, Rect bounds, double radius, bool squircle, bool dark, double strength, double tintOpacity = 0.16, double backgroundClarity = 0.5, LiquidGlassDispersionParameters? dispersionParameters = null, bool launchpadOptics = false)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		if (width <= 0 || height <= 0 || source.Length != checked(width * height * 4) || !double.IsFinite(size.Width) || !double.IsFinite(size.Height) || size.Width <= 0.0 || size.Height <= 0.0 || bounds.IsEmpty || bounds.Width <= 0.0 || bounds.Height <= 0.0 || !new Rect(size).Contains(bounds))
		{
			throw new ArgumentOutOfRangeException("width");
		}
		strength = (double.IsFinite(strength) ? Math.Clamp(strength, 0.0, 1.0) : 0.0);
		radius = (double.IsFinite(radius) ? Math.Clamp(radius, 0.0, Math.Min(bounds.Width, bounds.Height) / 2.0) : 0.0);
		(int, int, Size, Rect, double, bool, double, bool) tuple = (width, height, size, bounds, radius, squircle, strength, launchpadOptics);
		if (_map != null)
		{
			(int, int, Size, Rect, double, bool, double, bool) key = _key;
			(int, int, Size, Rect, double, bool, double, bool) tuple2 = tuple;
			if (key.Item1 == tuple2.Item1 && key.Item2 == tuple2.Item2 && !(key.Item3 != tuple2.Item3) && !(key.Item4 != tuple2.Item4) && key.Item5 == tuple2.Item5 && key.Item6 == tuple2.Item6 && key.Item7 == tuple2.Item7 && key.Item8 == tuple2.Item8)
			{
				goto IL_095a;
			}
		}
		_key = tuple;
		_map = new Sample[width * height];
		double num = Math.Max(size.Width / (double)width, size.Height / (double)height);
		double num2 = bounds.Width / 2.0;
		double num3 = bounds.Height / 2.0;
		double height2 = Math.Min(24.0, num3);
		double num4 = Math.Min(radius * 1.5, Math.Min(num2, num3));
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				Point point = new Point(((double)j + 0.5) * size.Width / (double)width, ((double)i + 0.5) * size.Height / (double)height);
				double num5 = (squircle ? GlassSignedDistance.Squircle(bounds, radius, point) : GlassSignedDistance.Continuous(bounds, radius, point));
				double num6 = Math.Clamp(0.5 - num5 / num, 0.0, 1.0);
				if (num6 == 0.0)
				{
					continue;
				}
				double num7 = point.X - bounds.Left - num2;
				double num8 = point.Y - bounds.Top - num3;
				double num9 = Math.Abs(num7) - num2 + num4;
				double num10 = Math.Abs(num8) - num3 + num4;
				double num11;
				double num12;
				if (num9 >= 0.0 || num10 >= 0.0)
				{
					num11 = Math.Max(num9, 0.0);
					num12 = Math.Max(num10, 0.0);
					double num13 = Math.Sqrt(num11 * num11 + num12 * num12);
					if (num13 > 1E-09)
					{
						num11 /= num13;
						num12 /= num13;
					}
					else
					{
						num11 = ((num9 >= num10) ? 1 : 0);
						num12 = 1.0 - num11;
					}
					num11 *= (double)((!(num7 < 0.0)) ? 1 : (-1));
					num12 *= (double)((!(num8 < 0.0)) ? 1 : (-1));
				}
				else
				{
					num11 = ((num9 >= num10) ? ((!(num7 < 0.0)) ? 1 : (-1)) : 0);
					num12 = ((!(num9 >= num10)) ? ((!(num8 < 0.0)) ? 1 : (-1)) : 0);
				}
				double num14 = LensOffset(0.0 - num5, height2, 32.0 * strength);
				double num15 = num14 * num11;
				double num16 = num14 * num12;
				if (launchpadOptics)
				{
					double num17 = Math.Min(48.0, num2);
					double num18 = Math.Min(48.0, num3);
					num15 = LensOffset(Math.Min(point.X - bounds.Left, bounds.Right - point.X), num17, Math.Min(64.0, num17 * 4.0 / 3.0) * strength) * (double)((!(num7 < 0.0)) ? 1 : (-1));
					num16 = LensOffset(Math.Min(point.Y - bounds.Top, bounds.Bottom - point.Y), num18, Math.Min(64.0, num18 * 4.0 / 3.0) * strength) * (double)((!(num8 < 0.0)) ? 1 : (-1));
					if (num9 >= 0.0 && num10 >= 0.0)
					{
						double num19 = Math.Min(num17, num18);
						double num20 = LensOffset(0.0 - num5, num19, Math.Min(64.0, num19 * 4.0 / 3.0) * strength);
						num15 = num15 * 0.35 + num20 * num11 * 0.65;
						num16 = num16 * 0.35 + num20 * num12 * 0.65;
					}
				}
				double num21 = Math.Clamp((0.75 + num / 2.0 - Math.Max(0.0, 0.0 - num5)) / num, 0.0, 1.0);
				double num22 = Math.Abs((num11 + num12) / Math.Sqrt(2.0)) * 0.5 * num21 * strength;
				double num23 = Math.Pow(Math.Clamp(1.0 - Math.Max(0.0, 0.0 - num5) / (double)(launchpadOptics ? 18 : 10), 0.0, 1.0), 2.0);
				double num24 = -0.6 * num11 - 0.8 * num12;
				num22 += Math.Pow(Math.Max(0.0, num24), 2.0) * 0.18 * num23 * strength;
				double num25 = Math.Pow(Math.Max(0.0, 0.0 - num24), 2.0) * 0.2 * num23 * strength;
				double num26 = (launchpadOptics ? Math.Sqrt(num15 * num15 + num16 * num16) : Math.Abs(num14));
				double num27 = ((launchpadOptics && num26 > 1E-09) ? ((0.0 - num15) / num26) : num11);
				double num28 = ((launchpadOptics && num26 > 1E-09) ? ((0.0 - num16) / num26) : num12);
				double num29 = Math.Clamp(num26 / 8.0, 0.0, 1.0);
				double num30 = Math.Clamp(2.0 * Math.Abs(num11 * num12), 0.0, 1.0);
				double num31 = Math.Min(0.75, num26 * 0.035) * num29 * num29 * (0.65 + 0.35 * num30);
				_map[i * width + j] = new Sample((float)((double)j + num15 * (double)width / size.Width), (float)((double)i + num16 * (double)height / size.Height), (float)num6, (float)num22, (float)num25, (float)(num31 * num27 * (double)width / size.Width), (float)(num31 * num28 * (double)height / size.Height), (float)Math.Exp((0.0 - Math.Max(0.0, 0.0 - num5)) / 5.0));
			}
		}
		goto IL_095a;
		IL_095a:
		byte[] array = FilterBackdrop(source, width, height, size, strength, backgroundClarity);
		byte[] array2 = new byte[source.Length];
		double num32 = (dark ? 18.0 : 250.0);
		double num33 = NormalizeTintOpacity(tintOpacity) * BodyTintScale(backgroundClarity, launchpadOptics) * strength;
		LiquidGlassDispersionParameters liquidGlassDispersionParameters = (dispersionParameters ?? LiquidGlassDispersionParameters.Default).Normalize();
		double num34 = liquidGlassDispersionParameters.DispersionStrength / 0.003;
		for (int k = 0; k < _map.Length; k++)
		{
			Sample sample = _map[k];
			if (sample.Coverage != 0f)
			{
				double num35 = (double)sample.OuterBlend * liquidGlassDispersionParameters.OuterDispersionGain / 0.4 + (double)(1f - sample.OuterBlend) * liquidGlassDispersionParameters.InnerDispersionGain / 0.25;
				int num36 = k * 4;
				for (int l = 0; l < 4; l++)
				{
					double num37 = (double)((l == 0) ? (-1) : ((l == 2) ? 1 : 0)) * num34 * num35;
					double num38 = Math.Clamp((double)sample.X + (double)sample.DispersionX * num37, 0.0, width - 1);
					double num39 = Math.Clamp((double)sample.Y + (double)sample.DispersionY * num37, 0.0, height - 1);
					int num40 = (int)num38;
					int num41 = (int)num39;
					int num42 = Math.Min(num40 + 1, width - 1);
					int num43 = Math.Min(num41 + 1, height - 1);
					double num44 = num38 - (double)num40;
					double num45 = num39 - (double)num41;
					double num46 = (double)(int)array[(num41 * width + num40) * 4 + l] * (1.0 - num44) + (double)(int)array[(num41 * width + num42) * 4 + l] * num44;
					double num47 = (double)(int)array[(num43 * width + num40) * 4 + l] * (1.0 - num44) + (double)(int)array[(num43 * width + num42) * 4 + l] * num44;
					double num48 = num46 * (1.0 - num45) + num47 * num45;
					array2[num36 + l] = ((l == 3) ? Byte(num48 * (double)sample.Coverage) : Byte((num48 * (1.0 - num33) + num32 * num33) * (double)(1f - sample.Shade) + (double)(sample.Highlight * 255f)));
				}
			}
		}
		return array2;
	}

	internal static byte[] RenderTransitionBackdrop(byte[] source, int width, int height, Size size, bool dark, double strength, double tintOpacity = 0.16, double backgroundClarity = 0.5, bool launchpadOptics = false)
	{
		byte[] array = FilterBackdrop(source, width, height, size, strength, backgroundClarity);
		byte[] obj = ((array == source) ? source.ToArray() : array);
		double tint = (dark ? 18.0 : 250.0);
		double opacity = NormalizeTintOpacity(tintOpacity) * BodyTintScale(backgroundClarity, launchpadOptics) * strength;
		TintBackdrop(obj, tint, opacity);
		return obj;
	}

	internal static void RenderTransitionBackdropInto(byte[] source, byte[] result, byte[] scratch, int width, int height, Size size, bool dark, double strength, double tintOpacity = 0.16, double backgroundClarity = 0.5, bool launchpadOptics = false)
	{
		if (source.Length != checked(width * height * 4) || result.Length != source.Length || scratch.Length != source.Length)
		{
			throw new ArgumentException("Backdrop buffers must match the complete BGRA frame.");
		}
		if (source == result || source == scratch || result == scratch)
		{
			throw new ArgumentException("Capture, output and scratch buffers must be distinct.");
		}
		source.CopyTo(result, 0);
		if (strength > 0.0)
		{
			ApplyBackdropFilter(source, result, width, height, size, strength, backgroundClarity, scratch);
		}
		double tint = (dark ? 18.0 : 250.0);
		double opacity = NormalizeTintOpacity(tintOpacity) * BodyTintScale(backgroundClarity, launchpadOptics) * strength;
		TintBackdrop(result, tint, opacity);
	}

	internal static void RenderFrostedBackdropInto(byte[] source, byte[] result, byte[] scratch, int width, int height, Size size, bool dark, double strength, double clarity)
	{
		if (source.Length != checked(width * height * 4) || result.Length != source.Length || scratch.Length != source.Length || size.Width <= 0.0 || size.Height <= 0.0)
		{
			throw new ArgumentException("Backdrop buffers must match the complete BGRA frame.");
		}
		if (source == result || source == scratch || result == scratch)
		{
			throw new ArgumentException("Capture, output and scratch buffers must be distinct.");
		}
		strength = (double.IsFinite(strength) ? Math.Clamp(strength, 0.0, 1.0) : 1.0);
		clarity = NormalizeBackgroundClarity(clarity);
		source.CopyTo(result, 0);
		double num = (12.0 + 8.0 * (1.0 - clarity)) * (0.7 + 0.3 * strength);
		Blur(result, width, height, num * (double)width / size.Width, num * (double)height / size.Height, scratch);
		TintBackdrop(result, dark ? 24 : 246, 0.5 * (1.0 - clarity));
	}

	private static void TintBackdrop(byte[] pixels, double tint, double opacity)
	{
		if (pixels.Length == 0 || opacity == 0.0)
		{
			return;
		}
		if (Avx.IsSupported && Sse41.IsSupported)
		{
			Vector256<double> right = Vector256.Create(1.0 - opacity);
			Vector256<double> right2 = Vector256.Create(tint * opacity);
			ref byte source = ref pixels[0];
			for (int i = 0; i < pixels.Length; i += 4)
			{
				uint num = Unsafe.ReadUnaligned<uint>(in Unsafe.Add(ref source, i));
				Vector128<byte> vector = Sse2.PackUnsignedSaturate(Sse2.PackSignedSaturate(Avx.ConvertToVector128Int32WithTruncation(Avx.RoundToNearestInteger(Avx.Add(Avx.Multiply(Avx.ConvertToVector256Double(Sse41.ConvertToVector128Int32(Vector128.CreateScalar(num).AsByte())), right), right2))), Vector128<int>.Zero), Vector128<short>.Zero);
				Unsafe.WriteUnaligned(ref Unsafe.Add(ref source, i), (num & 0xFF000000u) | (vector.AsUInt32().GetElement(0) & 0xFFFFFF));
			}
			return;
		}
		for (int j = 0; j < pixels.Length; j += 4)
		{
			for (int k = 0; k < 3; k++)
			{
				pixels[j + k] = Byte((double)(int)pixels[j + k] * (1.0 - opacity) + tint * opacity);
			}
		}
	}

	internal static double NormalizeTintOpacity(double value)
	{
		if (!double.IsFinite(value))
		{
			return 0.16;
		}
		return Math.Clamp(value, 0.0, 1.0);
	}

	internal static double NormalizeBackgroundClarity(double value)
	{
		if (!double.IsFinite(value))
		{
			return 0.5;
		}
		return Math.Clamp(value, 0.0, 1.0);
	}

	internal static double BodyTintScale(double clarity, bool launchpadOptics)
	{
		if (!launchpadOptics)
		{
			return 1.0;
		}
		return Math.Min(1.0, 2.0 * (1.0 - NormalizeBackgroundClarity(clarity)));
	}

	private static byte[] FilterBackdrop(byte[] source, int width, int height, Size size, double strength, double backgroundClarity)
	{
		byte[] array = source;
		if (strength > 0.0)
		{
			array = source.ToArray();
			ApplyBackdropFilter(source, array, width, height, size, strength, backgroundClarity);
		}
		return array;
	}

	private static void ApplyBackdropFilter(byte[] source, byte[] filtered, int width, int height, Size size, double strength, double backgroundClarity, byte[]? scratch = null)
	{
		if (Avx.IsSupported && Sse41.IsSupported)
		{
			Vector256<double> right = Vector256.Create(1.0 + 0.5 * strength);
			for (int i = 0; i < filtered.Length; i += 4)
			{
				uint num = Unsafe.ReadUnaligned<uint>(in source[i]);
				Vector256<double> left = Avx.ConvertToVector256Double(Sse41.ConvertToVector128Int32(Vector128.CreateScalar(num).AsByte()));
				Vector256<double> vector = Vector256.Create(0.072 * (double)(int)source[i] + 0.715 * (double)(int)source[i + 1] + 0.213 * (double)(int)source[i + 2]);
				Vector128<byte> vector2 = Sse2.PackUnsignedSaturate(Sse2.PackSignedSaturate(Avx.ConvertToVector128Int32WithTruncation(Avx.RoundToNearestInteger(Avx.Add(vector, Avx.Multiply(Avx.Subtract(left, vector), right)))), Vector128<int>.Zero), Vector128<short>.Zero);
				Unsafe.WriteUnaligned(ref filtered[i], (num & 0xFF000000u) | (vector2.AsUInt32().GetElement(0) & 0xFFFFFF));
			}
		}
		else
		{
			for (int j = 0; j < filtered.Length; j += 4)
			{
				double num2 = 0.072 * (double)(int)source[j] + 0.715 * (double)(int)source[j + 1] + 0.213 * (double)(int)source[j + 2];
				for (int k = 0; k < 3; k++)
				{
					filtered[j + k] = Byte(num2 + ((double)(int)source[j + k] - num2) * (1.0 + 0.5 * strength));
				}
			}
		}
		double num3 = NormalizeBackgroundClarity(backgroundClarity);
		double num4 = ((num3 <= 0.5) ? (1.5 - num3) : (2.0 * (1.0 - num3)));
		if (num4 > 0.0)
		{
			Blur(filtered, width, height, 3.2 * strength * num4 * (double)width / size.Width, 3.2 * strength * num4 * (double)height / size.Height, scratch);
		}
	}

	private static byte[] Blur(byte[] source, int width, int height, double sigmaX, double sigmaY, byte[]? scratch = null)
	{
		byte[] array = scratch ?? ArrayPool<byte>.Shared.Rent(source.Length);
		try
		{
			for (int i = 0; i < 3; i++)
			{
				Box(source, array.AsSpan(0, source.Length), width, height, Radius(sigmaX, i), horizontal: true);
				Box(array.AsSpan(0, source.Length), source, width, height, Radius(sigmaY, i), horizontal: false);
			}
			return source;
		}
		finally
		{
			if (scratch == null)
			{
				ArrayPool<byte>.Shared.Return(array);
			}
		}
	}

	private static int Radius(double sigma, int pass)
	{
		if (sigma < 0.1)
		{
			return 0;
		}
		int num = Math.Max(1, (int)Math.Floor(Math.Sqrt(4.0 * sigma * sigma + 1.0)));
		if (num % 2 == 0)
		{
			num--;
		}
		int num2 = (int)Math.Round((12.0 * sigma * sigma - (double)(3 * num * num) - (double)(12 * num) - 9.0) / (-4.0 * (double)num - 4.0));
		return ((pass < num2) ? (num - 1) : (num + 1)) / 2;
	}

	private static void Box(ReadOnlySpan<byte> source, Span<byte> target, int width, int height, int radius, bool horizontal)
	{
		if (radius == 0)
		{
			source.CopyTo(target);
			return;
		}
		int num = radius * 2 + 1;
		int num2 = width * 4;
		if (horizontal)
		{
			if (Sse41.IsSupported && num <= 65535)
			{
				Vector128<int> right = Vector128.Create(num / 2);
				Vector128<float> right2 = Vector128.Create((float)num);
				ref byte reference = ref MemoryMarshal.GetReference(source);
				ref byte reference2 = ref MemoryMarshal.GetReference(target);
				for (int i = 0; i < height; i++)
				{
					int num3 = i * num2;
					Vector128<int> left = Vector128<int>.Zero;
					for (int j = -radius; j <= radius; j++)
					{
						uint value = Unsafe.ReadUnaligned<uint>(in Unsafe.Add(ref reference, num3 + Math.Clamp(j, 0, width - 1) * 4));
						left = Sse2.Add(left, Sse41.ConvertToVector128Int32(Vector128.CreateScalar(value).AsByte()));
					}
					for (int k = 0; k < width; k++)
					{
						Vector128<byte> vector = Sse2.PackUnsignedSaturate(Sse2.PackSignedSaturate(Sse2.ConvertToVector128Int32WithTruncation(Sse.Divide(Sse2.ConvertToVector128Single(Sse2.Add(left, right)), right2)), Vector128<int>.Zero), Vector128<short>.Zero);
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref reference2, num3 + k * 4), vector.AsUInt32().GetElement(0));
						uint value2 = Unsafe.ReadUnaligned<uint>(in Unsafe.Add(ref reference, num3 + Math.Clamp(k - radius, 0, width - 1) * 4));
						uint value3 = Unsafe.ReadUnaligned<uint>(in Unsafe.Add(ref reference, num3 + Math.Clamp(k + radius + 1, 0, width - 1) * 4));
						left = Sse2.Add(Sse2.Subtract(left, Sse41.ConvertToVector128Int32(Vector128.CreateScalar(value2).AsByte())), Sse41.ConvertToVector128Int32(Vector128.CreateScalar(value3).AsByte()));
					}
				}
				return;
			}
			Span<int> span = stackalloc int[4];
			for (int l = 0; l < height; l++)
			{
				span.Clear();
				int num4 = l * num2;
				for (int m = -radius; m <= radius; m++)
				{
					int num5 = num4 + Math.Clamp(m, 0, width - 1) * 4;
					for (int n = 0; n < 4; n++)
					{
						span[n] += source[num5 + n];
					}
				}
				for (int num6 = 0; num6 < width; num6++)
				{
					int num7 = num4 + num6 * 4;
					int num8 = num4 + Math.Clamp(num6 - radius, 0, width - 1) * 4;
					int num9 = num4 + Math.Clamp(num6 + radius + 1, 0, width - 1) * 4;
					for (int num10 = 0; num10 < 4; num10++)
					{
						target[num7 + num10] = (byte)((span[num10] + num / 2) / num);
						span[num10] += source[num9 + num10] - source[num8 + num10];
					}
				}
			}
			return;
		}
		int[] array = ArrayPool<int>.Shared.Rent(num2);
		try
		{
			Span<int> span2 = array.AsSpan(0, num2);
			span2.Clear();
			for (int num11 = -radius; num11 <= radius; num11++)
			{
				int num12 = Math.Clamp(num11, 0, height - 1) * num2;
				for (int num13 = 0; num13 < num2; num13++)
				{
					span2[num13] += source[num12 + num13];
				}
			}
			for (int num14 = 0; num14 < height; num14++)
			{
				int num15 = num14 * num2;
				int num16 = Math.Clamp(num14 - radius, 0, height - 1) * num2;
				int num17 = Math.Clamp(num14 + radius + 1, 0, height - 1) * num2;
				int num18 = 0;
				if (Avx2.IsSupported && num <= 65535)
				{
					Vector256<int> right3 = Vector256.Create(num / 2);
					Vector256<float> right4 = Vector256.Create((float)num);
					ref byte reference3 = ref MemoryMarshal.GetReference(source);
					ref byte reference4 = ref MemoryMarshal.GetReference(target);
					ref int reference5 = ref MemoryMarshal.GetReference(span2);
					for (; num18 <= num2 - 16; num18 += 8)
					{
						Vector256<int> left2 = Vector256.LoadUnsafe(in reference5, (nuint)num18);
						Vector256<int> vector2 = Avx.ConvertToVector256Int32WithTruncation(Avx.Divide(Avx.ConvertToVector256Single(Avx2.Add(left2, right3)), right4));
						Unsafe.WriteUnaligned(value: Sse2.PackUnsignedSaturate(Sse2.PackSignedSaturate(vector2.GetLower(), vector2.GetUpper()), Vector128<short>.Zero).AsUInt64().GetElement(0), destination: ref Unsafe.Add(ref reference4, num15 + num18));
						Vector256<int> right5 = Avx2.ConvertToVector256Int32(Vector128.LoadUnsafe(in reference3, (nuint)(num16 + num18)));
						Avx2.Add(right: Avx2.ConvertToVector256Int32(Vector128.LoadUnsafe(in reference3, (nuint)(num17 + num18))), left: Avx2.Subtract(left2, right5)).StoreUnsafe(ref reference5, (nuint)num18);
					}
				}
				for (; num18 < num2; num18++)
				{
					target[num15 + num18] = (byte)((span2[num18] + num / 2) / num);
					span2[num18] += source[num17 + num18] - source[num16 + num18];
				}
			}
		}
		finally
		{
			ArrayPool<int>.Shared.Return(array);
		}
	}

	private static byte Byte(double value)
	{
		return (byte)Math.Clamp((int)Math.Round(value), 0, 255);
	}
}
