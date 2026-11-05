using System;

namespace GlassDesk.Platform.Interop;

internal struct PropertyKey
{
	internal Guid FormatId;

	internal int PropertyId;

	internal PropertyKey(Guid formatId, int propertyId)
	{
		FormatId = formatId;
		PropertyId = propertyId;
	}
}
