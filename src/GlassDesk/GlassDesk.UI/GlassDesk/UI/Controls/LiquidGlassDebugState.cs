using System;

namespace GlassDesk.UI.Controls;

public static class LiquidGlassDebugState
{
	private static LiquidGlassDebugView _view = ReadInitialView();

	private static bool _showPerformanceOverlay = ReadInitialPerformanceOverlay();

	private static LiquidGlassSamplingParameters _samplingParameters = LiquidGlassSamplingParameters.Default;

	private static LiquidGlassWeightParameters _weightParameters = LiquidGlassWeightParameters.Default;

	private static LiquidGlassLodParameters _lodParameters = LiquidGlassLodParameters.Default;

	private static LiquidGlassFinishingParameters _finishingParameters = LiquidGlassFinishingParameters.Default;

	private static LiquidGlassBrdfParameters _brdfParameters = LiquidGlassBrdfParameters.Default;

	private static LiquidGlassDispersionParameters _dispersionParameters = LiquidGlassDispersionParameters.Default;

	public static LiquidGlassDebugView View
	{
		get
		{
			return _view;
		}
		set
		{
			LiquidGlassDebugView liquidGlassDebugView = (Enum.IsDefined(value) ? value : LiquidGlassDebugView.None);
			if (_view != liquidGlassDebugView)
			{
				_view = liquidGlassDebugView;
				CompositionChanged?.Invoke(null, EventArgs.Empty);
			}
		}
	}

	public static bool ShowPerformanceOverlay
	{
		get
		{
			return _showPerformanceOverlay;
		}
		set
		{
			_showPerformanceOverlay = value;
		}
	}

	public static LiquidGlassSamplingParameters SamplingParameters
	{
		get
		{
			return _samplingParameters;
		}
		set
		{
			_samplingParameters = value.Normalize();
		}
	}

	public static LiquidGlassWeightParameters WeightParameters
	{
		get
		{
			return _weightParameters;
		}
		set
		{
			_weightParameters = value.Normalize();
		}
	}

	public static LiquidGlassLodParameters LodParameters
	{
		get
		{
			return _lodParameters;
		}
		set
		{
			_lodParameters = value.Normalize();
		}
	}

	public static LiquidGlassFinishingParameters FinishingParameters
	{
		get
		{
			return _finishingParameters;
		}
		set
		{
			LiquidGlassFinishingParameters liquidGlassFinishingParameters = value.Normalize();
			if (!(_finishingParameters == liquidGlassFinishingParameters))
			{
				_finishingParameters = liquidGlassFinishingParameters;
				CompositionChanged?.Invoke(null, EventArgs.Empty);
			}
		}
	}

	public static LiquidGlassBrdfParameters BrdfParameters
	{
		get
		{
			return _brdfParameters;
		}
		set
		{
			LiquidGlassBrdfParameters liquidGlassBrdfParameters = value.Normalize();
			if (!(_brdfParameters == liquidGlassBrdfParameters))
			{
				_brdfParameters = liquidGlassBrdfParameters;
				CompositionChanged?.Invoke(null, EventArgs.Empty);
			}
		}
	}

	public static LiquidGlassDispersionParameters DispersionParameters
	{
		get
		{
			return _dispersionParameters;
		}
		set
		{
			LiquidGlassDispersionParameters liquidGlassDispersionParameters = value.Normalize();
			if (!(_dispersionParameters == liquidGlassDispersionParameters))
			{
				_dispersionParameters = liquidGlassDispersionParameters;
				CompositionChanged?.Invoke(null, EventArgs.Empty);
			}
		}
	}

	internal static event EventHandler? CompositionChanged;

	private static LiquidGlassDebugView ReadInitialView()
	{
		if (!Enum.TryParse<LiquidGlassDebugView>(Environment.GetEnvironmentVariable("GLASSDESK_GLASS_DEBUG_VIEW"), ignoreCase: true, out var result) || !Enum.IsDefined(result))
		{
			return LiquidGlassDebugView.None;
		}
		return result;
	}

	private static bool ReadInitialPerformanceOverlay()
	{
		string environmentVariable = Environment.GetEnvironmentVariable("GLASSDESK_GLASS_DEBUG_METRICS");
		bool result;
		if (!string.Equals(environmentVariable, "1", StringComparison.Ordinal))
		{
			return bool.TryParse(environmentVariable, out result) & result;
		}
		return true;
	}
}
