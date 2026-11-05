using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace GlassDesk.AppServices;

public static class OfflinePinyin
{
	private static readonly Lazy<Dictionary<int, string>> Readings = new Lazy<Dictionary<int, string>>(LoadReadings);

	public static (string Full, string Initials) Index(string? text)
	{
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = new StringBuilder();
		foreach (Rune item in (text ?? string.Empty).Normalize(NormalizationForm.FormKC).EnumerateRunes())
		{
			if (Readings.Value.TryGetValue(item.Value, out string value))
			{
				stringBuilder.Append(value);
				stringBuilder2.Append(value[0]);
			}
			else if (Rune.IsLetterOrDigit(item))
			{
				string value2 = item.ToString().ToLowerInvariant();
				stringBuilder.Append(value2);
				stringBuilder2.Append(value2);
			}
		}
		return (Full: stringBuilder.ToString(), Initials: stringBuilder2.ToString());
	}

	private static Dictionary<int, string> LoadReadings()
	{
		Dictionary<int, string> dictionary = new Dictionary<int, string>();
		using Stream stream = typeof(OfflinePinyin).Assembly.GetManifestResourceStream("GlassDesk.AppServices.Resources.unihan-mandarin-17.txt") ?? throw new InvalidOperationException("Offline pinyin data is missing from the application.");
		using StreamReader streamReader = new StreamReader(stream, Encoding.UTF8);
		while (true)
		{
			string text = streamReader.ReadLine();
			if (text == null)
			{
				break;
			}
			string[] array = text.Split('\t', 2);
			if (array.Length == 2 && array[0].StartsWith("U+", StringComparison.Ordinal) && int.TryParse(array[0].AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result) && array[1].Length > 0)
			{
				dictionary[result] = array[1];
			}
		}
		return dictionary;
	}
}
