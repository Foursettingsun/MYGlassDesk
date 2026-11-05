using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.42308")]
internal sealed class _003CRegexGenerator_g_003EF412E83EB17AB8B8453D5A4FB139FD7212E89E679FCFCBC3B8099D905681DAA13__StableVersionPattern_0 : Regex
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
				if (num <= inputSpan.Length - 5 && num == 0)
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
				int num2 = 0;
				int num3 = 0;
				int num4 = 0;
				ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
				if (num != 0)
				{
					UncaptureUntil(0);
					return false;
				}
				num2 = num;
				if (readOnlySpan.IsEmpty)
				{
					UncaptureUntil(0);
					return false;
				}
				switch (readOnlySpan[0])
				{
				case '0':
					num++;
					readOnlySpan = inputSpan.Slice(num);
					break;
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
				{
					int num5 = readOnlySpan.Slice(1).IndexOfAnyExceptInRange('0', '9');
					if (num5 < 0)
					{
						num5 = readOnlySpan.Length - 1;
					}
					readOnlySpan = readOnlySpan.Slice(num5);
					num += num5;
					num++;
					readOnlySpan = inputSpan.Slice(num);
					break;
				}
				default:
					UncaptureUntil(0);
					return false;
				}
				Capture(1, num2, num);
				if (readOnlySpan.IsEmpty || readOnlySpan[0] != '.')
				{
					UncaptureUntil(0);
					return false;
				}
				num++;
				readOnlySpan = inputSpan.Slice(num);
				num3 = num;
				if (readOnlySpan.IsEmpty)
				{
					UncaptureUntil(0);
					return false;
				}
				switch (readOnlySpan[0])
				{
				case '0':
					num++;
					readOnlySpan = inputSpan.Slice(num);
					break;
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
				{
					int num6 = readOnlySpan.Slice(1).IndexOfAnyExceptInRange('0', '9');
					if (num6 < 0)
					{
						num6 = readOnlySpan.Length - 1;
					}
					readOnlySpan = readOnlySpan.Slice(num6);
					num += num6;
					num++;
					readOnlySpan = inputSpan.Slice(num);
					break;
				}
				default:
					UncaptureUntil(0);
					return false;
				}
				Capture(2, num3, num);
				if (readOnlySpan.IsEmpty || readOnlySpan[0] != '.')
				{
					UncaptureUntil(0);
					return false;
				}
				num++;
				readOnlySpan = inputSpan.Slice(num);
				num4 = num;
				if (readOnlySpan.IsEmpty)
				{
					UncaptureUntil(0);
					return false;
				}
				switch (readOnlySpan[0])
				{
				case '0':
					num++;
					readOnlySpan = inputSpan.Slice(num);
					break;
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
				{
					int num7 = readOnlySpan.Slice(1).IndexOfAnyExceptInRange('0', '9');
					if (num7 < 0)
					{
						num7 = readOnlySpan.Length - 1;
					}
					readOnlySpan = readOnlySpan.Slice(num7);
					num += num7;
					num++;
					readOnlySpan = inputSpan.Slice(num);
					break;
				}
				default:
					UncaptureUntil(0);
					return false;
				}
				Capture(3, num4, num);
				if (num < inputSpan.Length - 1 || ((uint)num < (uint)inputSpan.Length && inputSpan[num] != '\n'))
				{
					UncaptureUntil(0);
					return false;
				}
				runtextpos = num;
				Capture(0, start, num);
				return true;
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				void UncaptureUntil(int capturePosition)
				{
					while (Crawlpos() > capturePosition)
					{
						Uncapture();
					}
				}
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EF412E83EB17AB8B8453D5A4FB139FD7212E89E679FCFCBC3B8099D905681DAA13__StableVersionPattern_0 Instance = new _003CRegexGenerator_g_003EF412E83EB17AB8B8453D5A4FB139FD7212E89E679FCFCBC3B8099D905681DAA13__StableVersionPattern_0();

	private _003CRegexGenerator_g_003EF412E83EB17AB8B8453D5A4FB139FD7212E89E679FCFCBC3B8099D905681DAA13__StableVersionPattern_0()
	{
		pattern = "^(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)\\.(0|[1-9][0-9]*)$";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF412E83EB17AB8B8453D5A4FB139FD7212E89E679FCFCBC3B8099D905681DAA13__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF412E83EB17AB8B8453D5A4FB139FD7212E89E679FCFCBC3B8099D905681DAA13__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 4;
	}
}
