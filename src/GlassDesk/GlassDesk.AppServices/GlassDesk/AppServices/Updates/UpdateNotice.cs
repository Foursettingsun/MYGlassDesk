using System;

namespace GlassDesk.AppServices.Updates;

public sealed record UpdateNotice(Version Version, string Notes);
