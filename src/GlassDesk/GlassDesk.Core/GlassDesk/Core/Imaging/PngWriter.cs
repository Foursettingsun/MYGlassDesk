using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace GlassDesk.Core.Imaging;

public static class PngWriter
{
	internal static class Crc32
	{
		private static readonly uint[] Table = BuildTable();

		internal static uint Compute(ReadOnlySpan<byte> first, ReadOnlySpan<byte> second)
		{
			return Update(Update(uint.MaxValue, first), second) ^ 0xFFFFFFFFu;
		}

		private static uint Update(uint crc, ReadOnlySpan<byte> data)
		{
			ReadOnlySpan<byte> readOnlySpan = data;
			for (int i = 0; i < readOnlySpan.Length; i++)
			{
				byte b = readOnlySpan[i];
				crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
			}
			return crc;
		}

		private static uint[] BuildTable()
		{
			uint[] array = new uint[256];
			for (uint num = 0u; num < array.Length; num++)
			{
				uint num2 = num;
				for (int i = 0; i < 8; i++)
				{
					num2 = (((num2 & 1) != 0) ? (0xEDB88320u ^ (num2 >> 1)) : (num2 >> 1));
				}
				array[num] = num2;
			}
			return array;
		}
	}

	private static readonly byte[] Signature = new byte[8] { 137, 80, 78, 71, 13, 10, 26, 10 };

	private const byte ColorTypeRgba = 6;

	private const byte FilterNone = 0;

	public static byte[] EncodeBgra(ReadOnlySpan<byte> bgra, int width, int height)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(width, 1, "width");
		ArgumentOutOfRangeException.ThrowIfLessThan(height, 1, "height");
		int num = checked(width * height * 4);
		if (bgra.Length != num)
		{
			throw new ArgumentException($"像素数据长度应为 {num} 字节，实际 {bgra.Length} 字节。", "bgra");
		}
		using MemoryStream memoryStream = new MemoryStream(Signature.Length + num + 128);
		memoryStream.Write(Signature);
		WriteHeader(memoryStream, width, height);
		WriteImageData(memoryStream, bgra, width, height);
		WriteChunk(memoryStream, "IEND", ReadOnlySpan<byte>.Empty);
		return memoryStream.ToArray();
	}

	private static void WriteHeader(Stream output, int width, int height)
	{
		Span<byte> span = stackalloc byte[13];
		BinaryPrimitives.WriteInt32BigEndian(span, width);
		BinaryPrimitives.WriteInt32BigEndian(span.Slice(4), height);
		span[8] = 8;
		span[9] = 6;
		span[10] = 0;
		span[11] = 0;
		span[12] = 0;
		WriteChunk(output, "IHDR", span);
	}

	private static void WriteImageData(Stream output, ReadOnlySpan<byte> bgra, int width, int height)
	{
		int num = width * 4 + 1;
		byte[] array = new byte[num * height];
		for (int i = 0; i < height; i++)
		{
			int num2 = i * num;
			array[num2] = 0;
			int num3 = i * width * 4;
			for (int j = 0; j < width; j++)
			{
				int num4 = num3 + j * 4;
				int num5 = num2 + 1 + j * 4;
				array[num5] = bgra[num4 + 2];
				array[num5 + 1] = bgra[num4 + 1];
				array[num5 + 2] = bgra[num4];
				array[num5 + 3] = bgra[num4 + 3];
			}
		}
		using MemoryStream memoryStream = new MemoryStream(array.Length / 4 + 64);
		using (ZLibStream zLibStream = new ZLibStream(memoryStream, CompressionLevel.Optimal, leaveOpen: true))
		{
			zLibStream.Write(array);
		}
		WriteChunk(output, "IDAT", memoryStream.GetBuffer().AsSpan(0, (int)memoryStream.Length));
	}

	private static void WriteChunk(Stream output, string type, ReadOnlySpan<byte> data)
	{
		Span<byte> span = stackalloc byte[4];
		BinaryPrimitives.WriteInt32BigEndian(span, data.Length);
		output.Write(span);
		byte[] bytes = Encoding.ASCII.GetBytes(type);
		output.Write(bytes);
		output.Write(data);
		uint value = Crc32.Compute(bytes, data);
		Span<byte> span2 = stackalloc byte[4];
		BinaryPrimitives.WriteUInt32BigEndian(span2, value);
		output.Write(span2);
	}
}
