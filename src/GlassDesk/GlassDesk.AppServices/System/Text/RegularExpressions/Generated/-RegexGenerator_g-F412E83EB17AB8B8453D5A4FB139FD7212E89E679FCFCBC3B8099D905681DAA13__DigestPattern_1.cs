using System.CodeDom.Compiler;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.42308")]
internal sealed class _003CRegexGenerator_g_003EF412E83EB17AB8B8453D5A4FB139FD7212E89E679FCFCBC3B8099D905681DAA13__DigestPattern_1 : Regex
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
				if (num <= inputSpan.Length - 71 && num == 0)
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
				if ((uint)span.Length < 71u || !span.StartsWith("sha256:"))
				{
					return false;
				}
				if (span.Slice(7, 64).ContainsAnyExcept(_003CRegexGenerator_g_003EF412E83EB17AB8B8453D5A4FB139FD7212E89E679FCFCBC3B8099D905681DAA13__Utilities.s_asciiHexDigits))
				{
					return false;
				}
				if (72 < span.Length || (71 < span.Length && span[71] != '\n'))
				{
					return false;
				}
				Capture(0, start, runtextpos = num + 71);
				return true;
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EF412E83EB17AB8B8453D5A4FB139FD7212E89E679FCFCBC3B8099D905681DAA13__DigestPattern_1 Instance = new _003CRegexGenerator_g_003EF412E83EB17AB8B8453D5A4FB139FD7212E89E679FCFCBC3B8099D905681DAA13__DigestPattern_1();

	private _003CRegexGenerator_g_003EF412E83EB17AB8B8453D5A4FB139FD7212E89E679FCFCBC3B8099D905681DAA13__DigestPattern_1()
	{
		pattern = "^sha256:[0-9a-fA-F]{64}$";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF412E83EB17AB8B8453D5A4FB139FD7212E89E679FCFCBC3B8099D905681DAA13__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF412E83EB17AB8B8453D5A4FB139FD7212E89E679FCFCBC3B8099D905681DAA13__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 1;
	}
}
