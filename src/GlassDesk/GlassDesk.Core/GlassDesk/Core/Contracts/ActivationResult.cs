using System;
using System.Collections.Generic;

namespace GlassDesk.Core.Contracts;

public sealed class ActivationResult
{
	public ActivationOutcome Outcome { get; init; }

	public IReadOnlyList<WindowCandidate> Candidates { get; init; } = Array.Empty<WindowCandidate>();

	public string? Message { get; init; }
}
