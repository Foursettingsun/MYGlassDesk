using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using GlassDesk.Core.Models.Settings;
using GlassDesk.Core.Storage;
using GlassDesk.Platform.Displays;
using GlassDesk.UI.Controls;

namespace GlassDesk.Settings.Repair;

public static class LauncherOffsetControls
{
	private sealed class Controls
	{
		public Window Window = null;

		public StackPanel Panel = null;

		public Slider Slider = null;

		public TextBlock Readout = null;

		public TextBlock Status = null;

		public FieldInfo StoreField = null;

		public FieldInfo InvalidValuesField = null;

		public MethodInfo SavePage = null;

		public bool Loading;

		public void VisibilityChanged(object sender, DependencyPropertyChangedEventArgs args)
		{
			if (Window.IsVisible)
			{
				Load();
			}
		}

		public void Center(object sender, RoutedEventArgs args)
		{
			SaveOffset(0.0);
		}

		public void ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> args)
		{
			SaveOffset(Slider.Value);
		}

		private void SaveOffset(double value)
		{
			if (Loading || !(StoreField.GetValue(Window) is GlassDeskDataStore glassDeskDataStore))
			{
				return;
			}
			GlassDeskSettings current = glassDeskDataStore.Settings.Current;
			if ((double)OffsetProperty.GetValue(current.Launcher) == value)
			{
				return;
			}
			GlassDeskSettings glassDeskSettings = SettingsPageEditor.Clone(current);
			LauncherSettings launcherSettings = (LauncherSettings)CopyObject.Invoke(current.Launcher, null);
			OffsetProperty.SetValue(launcherSettings, value);
			current.Launcher = launcherSettings;
			try
			{
				SavePage.Invoke(Window, new object[3] { glassDeskSettings, "启动台", Status });
			}
			finally
			{
				Load();
			}
		}

		public void Load()
		{
			Loading = true;
			try
			{
				if (!(StoreField.GetValue(Window) is GlassDeskDataStore glassDeskDataStore))
				{
					Panel.IsEnabled = false;
					return;
				}
				LauncherSettings launcher = glassDeskDataStore.Settings.Current.Launcher;
				GlassWindow glassWindow = (GlassWindow)Window;
				PrimaryDisplaySession primaryDisplaySession = glassWindow.PrimaryDisplaySession;
				DisplayInfo displayInfo = ((primaryDisplaySession != null && primaryDisplaySession.IsAvailable) ? primaryDisplaySession.Display : DisplayService.Primary());
				double num = 0.0;
				double num2 = 0.0;
				if (displayInfo != null)
				{
					PixelRect pixelRect = WindowPlacementPolicy.Centered(displayInfo.WorkArea, launcher.WindowWorkAreaRatio, fullscreen: false);
					double num3 = 96.0 / (double)((displayInfo.Dpi == 0) ? 96u : displayInfo.Dpi);
					num = 0.0 - Math.Floor((double)Math.Max(0, pixelRect.Y - displayInfo.WorkArea.Y) * num3);
					num2 = Math.Floor((double)Math.Max(0, displayInfo.WorkArea.Bottom - pixelRect.Bottom) * num3);
				}
				Slider.Minimum = Math.Min(num, Slider.Maximum);
				Slider.Maximum = num2;
				Slider.Minimum = num;
				double num4 = (double)OffsetProperty.GetValue(launcher);
				double num5 = Math.Clamp(num4, num, num2);
				Slider.Value = num5;
				Readout.Text = ((num4 == num5) ? $"{num4:0} DIP" : $"{num4:0} DIP，当前屏幕限制为 {num5:0} DIP");
				Panel.IsEnabled = !(bool)InvalidValuesField.GetValue(Window);
				Slider.IsEnabled = num2 > num;
			}
			finally
			{
				Loading = false;
			}
		}
	}

	private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private static readonly ConditionalWeakTable<Window, Controls> Windows = new ConditionalWeakTable<Window, Controls>();

	private static readonly PropertyInfo OffsetProperty = typeof(LauncherSettings).GetProperty("OffsetY");

	private static readonly MethodInfo CopyObject = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

	public static void Configure(object instance)
	{
		Window window = (Window)instance;
		if (!Windows.TryGetValue(window, out var _))
		{
			Type type = window.GetType();
			Slider slider = (Slider)type.GetField("LauncherColumnsSlider", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(window);
			TextBlock textBlock = (TextBlock)type.GetField("LauncherSettingsStatus", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(window);
			Panel panel = (Panel)slider.Parent;
			Controls controls = new Controls
			{
				Window = window,
				Panel = new StackPanel
				{
					Name = "LauncherVerticalOffsetPanel",
					Margin = new Thickness(0.0, 12.0, 0.0, 8.0)
				},
				Slider = new Slider
				{
					Name = "LauncherVerticalOffsetSlider",
					Minimum = -300.0,
					Maximum = 300.0,
					TickFrequency = 1.0,
					IsSnapToTickEnabled = true,
					SmallChange = 1.0,
					LargeChange = 10.0,
					Style = slider.Style,
					Margin = slider.Margin
				},
				Readout = new TextBlock
				{
					Name = "LauncherVerticalOffsetReadout",
					Foreground = textBlock.Foreground,
					FontSize = 12.0
				},
				Status = textBlock,
				StoreField = type.GetField("_dataStore", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
				InvalidValuesField = type.GetField("_launcherValuesOutOfRange", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
				SavePage = type.GetMethod("SavePageSnapshot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			};
			DockPanel dockPanel = new DockPanel
			{
				LastChildFill = true,
				Margin = new Thickness(0.0, 0.0, 0.0, 6.0)
			};
			Button button = new Button
			{
				Name = "LauncherVerticalOffsetReset",
				Content = "恢复居中",
				Padding = new Thickness(10.0, 3.0, 10.0, 3.0)
			};
			DockPanel.SetDock(button, Dock.Right);
			dockPanel.Children.Add(button);
			TextBlock textBlock2 = (TextBlock)type.GetField("LauncherSectionHeading", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(window);
			dockPanel.Children.Add(new TextBlock
			{
				Text = "垂直偏移",
				Foreground = textBlock2.Foreground,
				VerticalAlignment = VerticalAlignment.Center
			});
			controls.Panel.Children.Add(dockPanel);
			controls.Panel.Children.Add(controls.Slider);
			controls.Panel.Children.Add(controls.Readout);
			controls.Panel.Children.Add(new TextBlock
			{
				Text = "0 为居中，负值向上，正值向下。调节后自动保存，范围随当前屏幕可用空间调整。",
				TextWrapping = TextWrapping.Wrap,
				Foreground = textBlock.Foreground,
				FontSize = 12.0,
				Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
			});
			panel.Children.Insert(panel.Children.IndexOf(slider) + 1, controls.Panel);
			Windows.Add(window, controls);
			button.Click += controls.Center;
			controls.Slider.ValueChanged += controls.ValueChanged;
			window.IsVisibleChanged += controls.VisibilityChanged;
			controls.Load();
		}
	}

	public static void Reload(object instance)
	{
		if (Windows.TryGetValue((Window)instance, out var value))
		{
			value.Load();
		}
	}
}
