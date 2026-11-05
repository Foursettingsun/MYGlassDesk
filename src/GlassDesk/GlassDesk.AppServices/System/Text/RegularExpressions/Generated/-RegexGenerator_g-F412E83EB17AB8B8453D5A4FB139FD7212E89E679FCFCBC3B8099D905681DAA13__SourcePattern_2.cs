using System.CodeDom.Compiler;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.42308")]
internal sealed class _003CRegexGenerator_g_003EF412E83EB17AB8B8453D5A4FB139FD7212E89E679FCFCBC3B8099D905681DAA13__SourcePattern_2 : Regex
{
	private sealed class RunnerFactory : RegexRunnerFactory
	{
		private sealed class Runner : RegexRunner
		{
			protected override void Scan(ReadOnlySpan<char> inputSpan)
			{
				if (TryFindNextPossibleStartingPosition(inputSpan) && !TryMatchAtCurrentPosition(inputSpan))
				{
					runtextpos = inputSpan.Length;
				}
			}

			private bool TryFindNextPossibleStartingPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				if (num <= inputSpan.Length - 26 && num == 0)
				{
					return true;
				}
				runtextpos = inputSpan.Length;
				return false;
			}

			private bool TryMatchAtCurrentPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				int start = num;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				if (num != 0)
				{
					return false;
				}
				if (!span.StartsWith("/repos/"))
				{
					return false;
				}
				int num2 = span.Slice(7).IndexOfAnyExcept(_003CRegexGenerator_g_003EF412E83EB17AB8B8453D5A4FB139FD7212E89E679FCFCBC3B8099D905681DAA13__Utilities.s_asciiLettersAndDigitsAndDashDotUnderscore);
				if (num2 < 0)
				{
					num2 = span.Length - 7;
				}
				if (num2 == 0)
				{
					return false;
				}
				span = span.Slice(num2);
				num += num2;
				if ((uint)span.Length < 8u || span[7] != '/')
				{
					return false;
				}
				int num3 = span.Slice(8).IndexOfAnyExcept(_003CRegexGenerator_g_003EF412E83EB17AB8B8453D5A4FB139FD7212E89E679FCFCBC3B8099D905681DAA13__Utilities.s_asciiLettersAndDigitsAndDashDotUnderscore);
				if (num3 < 0)
				{
					num3 = span.Length - 8;
				}
				if (num3 == 0)
				{
					return false;
				}
				span = span.Slice(num3);
				num += num3;
				if (!span.Slice(8).StartsWith("/releases/latest"))
				{
					return false;
				}
				if (25 < span.Length || (24 < span.Length && span[24] != '\n'))
				{
					return false;
				}
				Capture(0, start, runtextpos = num + 24);
				return true;
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EF412E83EB17AB8B8453D5A4FB139FD7212E89E679FCFCBC3B8099D905681DAA13__SourcePattern_2 Instance = new _003CRegexGenerator_g_003EF412E83EB17AB8B8453D5A4FB139FD7212E89E679FCFCBC3B8099D905681DAA13__SourcePattern_2();

	private _003CRegexGenerator_g_003EF412E83EB17AB8B8453D5A4FB139FD7212E89E679FCFCBC3B8099D905681DAA13__SourcePattern_2()
	{
		pattern = "^/repos/[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+/releases/latest$";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF412E83EB17AB8B8453D5A4FB139FD7212E89E679FCFCBC3B8099D905681DAA13__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF412E83EB17AB8B8453D5A4FB139FD7212E89E679FCFCBC3B8099D905681DAA13__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 1;
	}
}
