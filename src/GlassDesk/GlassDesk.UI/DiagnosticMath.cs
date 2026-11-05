using System;
using System.Collections.Generic;
using System.Linq;

internal static class DiagnosticMath
{
	internal static double ReadbackMiBPerSecond(int bytesPerFrame, int frames, double elapsedSeconds)
	{
		return (double)bytesPerFrame * (double)frames / elapsedSeconds / 1024.0 / 1024.0;
	}

	internal static object Distribution(IEnumerable<double> samples)
	{
		double[] values = samples.Order().ToArray();
		if (values.Length == 0)
		{
			return new
			{
				count = 0,
				meanMs = (double?)null,
				p50Ms = (double?)null,
				p95Ms = (double?)null,
				p99Ms = (double?)null,
				maxMs = (double?)null
			};
		}
		return new
		{
			count = values.Length,
			meanMs = (double?)values.Average(),
			p50Ms = (double?)P(0.5),
			p95Ms = (double?)P(0.95),
			p99Ms = (double?)P(0.99),
			maxMs = (double?)values[^1]
		};
		double P(double p)
		{
			return values[Math.Clamp((int)Math.Ceiling((double)values.Length * p) - 1, 0, values.Length - 1)];
		}
	}
}
