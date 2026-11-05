using System;
using System.Windows;
using GlassDesk.Platform.Displays;

namespace GlassDesk.UI.Controls;

public sealed class ContainerMorphMotion
{
	private struct Spring(double initial)
	{
		public double Value = initial;

		private double _velocity = 0.0;

		public void Advance(double seconds, double target, double frequency, double damping)
		{
			if (seconds <= 0.0)
			{
				return;
			}
			double num = Value - target;
			if (num != 0.0 || _velocity != 0.0)
			{
				double num2 = Math.Exp((0.0 - damping) * frequency * seconds);
				if (damping == 1.0)
				{
					double num3 = _velocity + frequency * num;
					Value = target + num2 * (num + num3 * seconds);
					_velocity = num2 * (_velocity - frequency * num3 * seconds);
					return;
				}
				double num4 = frequency * Math.Sqrt(1.0 - damping * damping);
				double num5 = Math.Sin(num4 * seconds);
				double num6 = Math.Cos(num4 * seconds);
				double num7 = (_velocity + damping * frequency * num) / num4;
				double num8 = num * num6 + num7 * num5;
				Value = target + num2 * num8;
				_velocity = num2 * (num4 * ((0.0 - num) * num5 + num7 * num6) - damping * frequency * num8);
			}
		}

		public readonly bool AtRest(double target, double span)
		{
			if (Math.Abs(Value - target) <= Math.Max(0.001, Math.Abs(span) * 0.001))
			{
				return Math.Abs(_velocity) <= Math.Max(0.01, Math.Abs(span) * 0.01);
			}
			return false;
		}
	}

	private const double WidthLeadSeconds = 0.018;

	private const double RevealThreshold = 0.65;

	private const double RevealSeconds = 0.1;

	private const double ConcealSeconds = 0.07;

	private PixelRect _source;

	private readonly PixelRect _destination;

	private readonly double _sourceRoundness;

	private readonly double _destinationRadius;

	private Spring _centerX;

	private Spring _centerY;

	private Spring _width;

	private Spring _height;

	private Spring _roundness;

	private Spring _sourceWidthForProgress;

	private Spring _sourceHeightForProgress;

	private bool _shapeExpanded;

	private double _shapeAge;

	private double _widthDelay;

	private double _closeHold;

	private double _contentOpacity;

	private double _contentFrom;

	private double _contentTime;

	private bool _contentRevealing;

	public ContainerMorphFrame CurrentFrame
	{
		get
		{
			double num = Math.Max(1.0, _width.Value);
			double num2 = Math.Max(1.0, _height.Value);
			double openness = Openness;
			double cornerRadius = Math.Clamp(_roundness.Value, 0.0, 1.0) * Math.Min(num, num2) / 2.0;
			return new ContainerMorphFrame(new Rect(_centerX.Value - num / 2.0, _centerY.Value - num2 / 2.0, num, num2), cornerRadius, openness, 0.8 * (1.0 - openness), _contentOpacity, 0.96 + 0.04 * _contentOpacity, 1.0 - SmoothStep(Math.Clamp(openness / 0.55, 0.0, 1.0)));
		}
	}

	public bool IsSettled { get; private set; }

	public bool IsExpandedTarget { get; private set; }

	public double Openness
	{
		get
		{
			if (!IsSettled)
			{
				return OpennessFor(_width.Value, _height.Value, _sourceWidthForProgress.Value, _sourceHeightForProgress.Value);
			}
			return IsExpandedTarget ? 1 : 0;
		}
	}

	public ContainerMorphMotion(PixelRect source, PixelRect destination, double sourceRadius, double destinationRadius)
	{
		_source = Normalize(source);
		_destination = Normalize(destination);
		_sourceRoundness = RoundnessFor(_source, NormalizeRadius(sourceRadius));
		_destinationRadius = NormalizeRadius(destinationRadius);
		SnapToTarget();
	}

	public void Retarget(bool expanded, PixelRect? sourceBounds = null)
	{
		int num;
		if (sourceBounds.HasValue)
		{
			PixelRect valueOrDefault = sourceBounds.GetValueOrDefault();
			num = ((Normalize(valueOrDefault) != _source) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		bool flag = (byte)num != 0;
		if (flag)
		{
			_source = Normalize(sourceBounds.Value);
		}
		if (expanded == IsExpandedTarget)
		{
			if (flag && !expanded && IsSettled)
			{
				IsSettled = false;
				_shapeAge = 0.0;
			}
			return;
		}
		bool flag2 = IsSettled && !IsExpandedTarget;
		IsExpandedTarget = expanded;
		IsSettled = false;
		_contentFrom = _contentOpacity;
		_contentTime = 0.0;
		_contentRevealing = expanded && Openness >= 0.65;
		_closeHold = ((!expanded && _contentOpacity > 0.0) ? 0.07 : 0.0);
		_widthDelay = ((expanded & flag2) ? 0.018 : 0.0);
		if (expanded || _closeHold == 0.0)
		{
			_shapeExpanded = expanded;
			_shapeAge = 0.0;
		}
	}

	public void Advance(double deltaSeconds)
	{
		if (IsSettled || !double.IsFinite(deltaSeconds) || deltaSeconds <= 0.0)
		{
			return;
		}
		if (IsExpandedTarget)
		{
			Spring width = _width;
			Spring height = _height;
			Spring sourceWidthForProgress = _sourceWidthForProgress;
			Spring sourceHeightForProgress = _sourceHeightForProgress;
			double widthDelay = _widthDelay;
			AdvanceProgressSource(deltaSeconds);
			AdvanceGeometry(deltaSeconds);
			if (!_contentRevealing && Openness >= 0.65)
			{
				double num = 0.0;
				double num2 = deltaSeconds;
				for (int i = 0; i < 24; i++)
				{
					double num3 = (num + num2) / 2.0;
					Spring spring = width;
					Spring spring2 = height;
					Spring spring3 = sourceWidthForProgress;
					Spring spring4 = sourceHeightForProgress;
					spring.Advance(Math.Max(0.0, num3 - widthDelay), _destination.Width, 25.0, 0.76);
					spring2.Advance(num3, _destination.Height, 28.0, 0.77);
					spring3.Advance(num3, _source.Width, 48.0, 1.0);
					spring4.Advance(num3, _source.Height, 48.0, 1.0);
					if (OpennessFor(spring.Value, spring2.Value, spring3.Value, spring4.Value) >= 0.65)
					{
						num2 = num3;
					}
					else
					{
						num = num3;
					}
				}
				_contentRevealing = true;
				_contentTime = deltaSeconds - num2;
			}
			else if (_contentRevealing)
			{
				_contentTime += deltaSeconds;
			}
			if (_contentRevealing)
			{
				_contentOpacity = _contentFrom + (1.0 - _contentFrom) * SmoothStep(_contentTime / 0.1);
			}
		}
		else
		{
			AdvanceProgressSource(deltaSeconds);
			_contentTime += deltaSeconds;
			_contentOpacity = _contentFrom * (1.0 - SmoothStep(_contentTime / 0.07));
			double num4 = deltaSeconds;
			if (_closeHold > 0.0)
			{
				double num5 = Math.Min(num4, _closeHold);
				AdvanceGeometry(num5);
				num4 -= num5;
				_closeHold -= num5;
				if (_closeHold <= 1E-12)
				{
					_closeHold = 0.0;
					_shapeExpanded = false;
					_shapeAge = 0.0;
					_widthDelay = 0.0;
				}
			}
			if (num4 > 0.0)
			{
				AdvanceGeometry(num4);
			}
		}
		if (_shapeExpanded == IsExpandedTarget && _closeHold == 0.0 && _shapeAge >= (IsExpandedTarget ? 0.38 : 0.2) && GeometryAtRest() && _contentOpacity == (double)(IsExpandedTarget ? 1 : 0))
		{
			SnapToTarget();
		}
	}

	public void SnapToTarget()
	{
		PixelRect bounds = (IsExpandedTarget ? _destination : _source);
		_centerX = new Spring((double)bounds.X + (double)bounds.Width / 2.0);
		_centerY = new Spring((double)bounds.Y + (double)bounds.Height / 2.0);
		_width = new Spring(bounds.Width);
		_height = new Spring(bounds.Height);
		_roundness = new Spring(IsExpandedTarget ? RoundnessFor(bounds, _destinationRadius) : _sourceRoundness);
		_sourceWidthForProgress = new Spring(_source.Width);
		_sourceHeightForProgress = new Spring(_source.Height);
		_shapeExpanded = IsExpandedTarget;
		_widthDelay = 0.0;
		_closeHold = 0.0;
		_contentOpacity = (IsExpandedTarget ? 1 : 0);
		_contentFrom = _contentOpacity;
		_contentTime = 0.0;
		_contentRevealing = IsExpandedTarget;
		IsSettled = true;
	}

	private void AdvanceGeometry(double seconds)
	{
		_shapeAge += seconds;
		PixelRect bounds = (_shapeExpanded ? _destination : _source);
		if (_shapeExpanded)
		{
			_centerX.Advance(seconds, (double)bounds.X + (double)bounds.Width / 2.0, 26.0, 0.84);
			_centerY.Advance(seconds, (double)bounds.Y + (double)bounds.Height / 2.0, 29.0, 0.82);
			double seconds2 = Math.Max(0.0, seconds - _widthDelay);
			_widthDelay = Math.Max(0.0, _widthDelay - seconds);
			_width.Advance(seconds2, bounds.Width, 25.0, 0.76);
			_height.Advance(seconds, bounds.Height, 28.0, 0.77);
			_roundness.Advance(seconds, RoundnessFor(bounds, _destinationRadius), 27.0, 1.0);
		}
		else
		{
			_centerX.Advance(seconds, (double)bounds.X + (double)bounds.Width / 2.0, 48.0, 1.0);
			_centerY.Advance(seconds, (double)bounds.Y + (double)bounds.Height / 2.0, 48.0, 1.0);
			_width.Advance(seconds, bounds.Width, 48.0, 1.0);
			_height.Advance(seconds, bounds.Height, 48.0, 1.0);
			_roundness.Advance(seconds, _sourceRoundness, 48.0, 1.0);
		}
	}

	private void AdvanceProgressSource(double seconds)
	{
		_sourceWidthForProgress.Advance(seconds, _source.Width, 48.0, 1.0);
		_sourceHeightForProgress.Advance(seconds, _source.Height, 48.0, 1.0);
	}

	private bool GeometryAtRest()
	{
		PixelRect bounds = (IsExpandedTarget ? _destination : _source);
		if (_centerX.AtRest((double)bounds.X + (double)bounds.Width / 2.0, (double)_destination.X + (double)_destination.Width / 2.0 - (double)_source.X - (double)_source.Width / 2.0) && _centerY.AtRest((double)bounds.Y + (double)bounds.Height / 2.0, (double)_destination.Y + (double)_destination.Height / 2.0 - (double)_source.Y - (double)_source.Height / 2.0) && _width.AtRest(bounds.Width, _destination.Width - _source.Width) && _height.AtRest(bounds.Height, _destination.Height - _source.Height) && _roundness.AtRest(IsExpandedTarget ? RoundnessFor(bounds, _destinationRadius) : _sourceRoundness, 1.0) && _sourceWidthForProgress.AtRest(_source.Width, _destination.Width - _source.Width))
		{
			return _sourceHeightForProgress.AtRest(_source.Height, _destination.Height - _source.Height);
		}
		return false;
	}

	private double OpennessFor(double width, double height, double sourceWidth, double sourceHeight)
	{
		double num = (double)_destination.Width - sourceWidth;
		double num2 = (double)_destination.Height - sourceHeight;
		double val = ((Math.Abs(num) < 1E-09) ? 1.0 : ((width - sourceWidth) / num));
		double val2 = ((Math.Abs(num2) < 1E-09) ? 1.0 : ((height - sourceHeight) / num2));
		return Math.Clamp(Math.Min(val, val2), 0.0, 1.0);
	}

	private static PixelRect Normalize(PixelRect bounds)
	{
		return bounds with
		{
			Width = Math.Max(1, bounds.Width),
			Height = Math.Max(1, bounds.Height)
		};
	}

	private static double NormalizeRadius(double radius)
	{
		if (!double.IsFinite(radius))
		{
			return 0.0;
		}
		return Math.Max(0.0, radius);
	}

	private static double RoundnessFor(PixelRect bounds, double radius)
	{
		return Math.Clamp(radius * 2.0 / (double)Math.Min(bounds.Width, bounds.Height), 0.0, 1.0);
	}

	private static double SmoothStep(double value)
	{
		double num = Math.Clamp(value, 0.0, 1.0);
		return num * num * (3.0 - 2.0 * num);
	}
}
