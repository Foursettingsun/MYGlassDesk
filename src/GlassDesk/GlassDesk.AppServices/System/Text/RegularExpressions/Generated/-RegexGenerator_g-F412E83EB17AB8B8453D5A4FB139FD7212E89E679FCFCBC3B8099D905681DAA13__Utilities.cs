using System.Buffers;
using System.CodeDom.Compiler;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.42308")]
internal static class _003CRegexGenerator_g_003EF412E83EB17AB8B8453D5A4FB139FD7212E89E679FCFCBC3B8099D905681DAA13__Utilities
{
	internal static readonly TimeSpan s_defaultTimeout = ((AppContext.GetData("REGEX_DEFAULT_MATCH_TIMEOUT") is TimeSpan timeSpan) ? timeSpan : Regex.InfiniteMatchTimeout);

	internal static readonly bool s_hasTimeout = s_defaultTimeout != Regex.InfiniteMatchTimeout;

	internal static readonly SearchValues<char> s_asciiHexDigits = SearchValues.Create("0123456789ABCDEFabcdef");

	internal static readonly SearchValues<char> s_asciiLettersAndDigitsAndDashDotUnderscore = SearchValues.Create("-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz");
}
