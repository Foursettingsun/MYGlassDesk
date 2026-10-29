using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using GlassDesk.UI.Theming;

namespace GlassDesk.Launcher;

internal sealed class LauncherAppTileButton : Button
{
	private readonly ScaleTransform _iconScale = new ScaleTransform(1.0, 1.0);

	private readonly FrameworkElement _iconHost;

	private bool _folderHovered;

	internal FrameworkElement IconHost => _iconHost;

	internal void AnimateFolderArrival()
	{
		ThemeService? current = ThemeService.Current;
		if (current == null || current.AnimationsEnabled)
		{
			DoubleAnimation animation = new DoubleAnimation(0.93, 1.0, TimeSpan.FromMilliseconds(320L))
			{
				EasingFunction = new BackEase
				{
					Amplitude = 0.5,
					EasingMode = EasingMode.EaseOut
				}
			};
			_iconScale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
			_iconScale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
		}
	}

	internal void SetFolderHovered(bool hovered)
	{
		_folderHovered = hovered;
		double toValue = (hovered ? 1.045 : 1.0);
		ThemeService? current = ThemeService.Current;
		if (current != null && !current.AnimationsEnabled)
		{
			ResetIconScale();
			return;
		}
		DoubleAnimation animation = new DoubleAnimation(toValue, TimeSpan.FromMilliseconds(240L))
		{
			EasingFunction = new BackEase
			{
				Amplitude = 0.22,
				EasingMode = EasingMode.EaseOut
			}
		};
		_iconScale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
		_iconScale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
	}

	public LauncherAppTileButton(FrameworkElement iconHost)
	{
		_iconHost = iconHost;
		iconHost.RenderTransformOrigin = new Point(0.5, 0.5);
		iconHost.RenderTransform = _iconScale;
		base.Unloaded += delegate
		{
			ResetIconScale();
		};
	}

	protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);
		if (_iconHost == null)
		{
			return;
		}
		if (e.Property == UIElement.IsVisibleProperty)
		{
			object newValue = e.NewValue;
			if (newValue is bool && !(bool)newValue)
			{
				ResetIconScale();
				return;
			}
		}
		if (e.Property != ButtonBase.IsPressedProperty)
		{
			return;
		}
		ThemeService? current = ThemeService.Current;
		if (current != null && !current.AnimationsEnabled)
		{
			ResetIconScale();
			return;
		}
		DoubleAnimation doubleAnimation = new DoubleAnimation(base.IsPressed ? 0.97 : 1.0, TimeSpan.FromMilliseconds(base.IsPressed ? 60 : 180))
		{
			EasingFunction = new CubicEase
			{
				EasingMode = EasingMode.EaseOut
			}
		};
		if (!base.IsPressed)
		{
			doubleAnimation.Completed += delegate
			{
				if (!base.IsPressed)
				{
					ResetIconScale();
				}
			};
		}
		_iconScale.BeginAnimation(ScaleTransform.ScaleXProperty, doubleAnimation);
		_iconScale.BeginAnimation(ScaleTransform.ScaleYProperty, doubleAnimation);
	}

	private void ResetIconScale()
	{
		_iconScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
		_iconScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
		ScaleTransform iconScale = _iconScale;
		double scaleX = (_iconScale.ScaleY = (_folderHovered ? 1.045 : 1.0));
		iconScale.ScaleX = scaleX;
	}
}
