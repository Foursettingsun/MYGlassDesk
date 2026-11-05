using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace GlassDesk.Dock;

public sealed class DockTileModel : INotifyPropertyChanged
{
	private Visibility _runningVisibility = Visibility.Collapsed;

	private bool _isActive;

	public string EntryId { get; init; } = string.Empty;

	public string? Glyph { get; init; }

	public bool IsMissing { get; init; }

	public string DisplayName { get; init; } = string.Empty;

	public string ToolTipText { get; init; } = string.Empty;

	public ImageSource? Icon { get; init; }

	public Brush? DotBrush { get; init; }

	public Brush? ActiveBrush { get; init; }

	public Brush? IndicatorBrush
	{
		get
		{
			if (!IsActive)
			{
				return DotBrush;
			}
			return ActiveBrush ?? DotBrush;
		}
	}

	public double IndicatorWidth => IsActive ? 15 : 5;

	public double IconSize { get; init; } = 48.0;

	public Thickness TileMargin { get; init; } = new Thickness(4.0, 0.0, 4.0, 0.0);

	public Visibility RunningVisibility
	{
		get
		{
			return _runningVisibility;
		}
		set
		{
			if (_runningVisibility != value)
			{
				_runningVisibility = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RunningVisibility"));
			}
		}
	}

	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (_isActive != value)
			{
				_isActive = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsActive"));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IndicatorBrush"));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IndicatorWidth"));
			}
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	internal static void ApplyRunningIndicatorSetting(DockTileModel? model, Visibility visibility, bool enabled)
	{
		model.RunningVisibility = (enabled ? visibility : Visibility.Collapsed);
	}
}
