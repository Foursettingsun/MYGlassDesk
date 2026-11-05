using System;

namespace GlassDesk.Platform.Interop;

internal static class ShellComIds
{
	internal static readonly Guid ShellLinkClass = new Guid("00021401-0000-0000-C000-000000000046");

	internal static readonly Guid ShellLinkW = new Guid("000214F9-0000-0000-C000-000000000046");

	internal static readonly Guid PersistFile = new Guid("0000010B-0000-0000-C000-000000000046");

	internal static readonly Guid ShellItemImageFactory = new Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b");

	internal static readonly Guid PropertyStore = new Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99");
}
