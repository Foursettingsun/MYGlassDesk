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
    private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly ConditionalWeakTable<Window, Controls> Windows = new();
    private static readonly PropertyInfo OffsetProperty = typeof(LauncherSettings).GetProperty("OffsetY")!;
    private static readonly MethodInfo CopyObject = typeof(object).GetMethod("MemberwiseClone", InstanceMembers)!;

    private sealed class Controls
    {
        public Window Window = null!;
        public StackPanel Panel = null!;
        public Slider Slider = null!;
        public TextBlock Readout = null!;
        public TextBlock Status = null!;
        public FieldInfo StoreField = null!;
        public FieldInfo InvalidValuesField = null!;
        public MethodInfo SavePage = null!;
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
            SaveOffset(0);
        }

        public void ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> args)
        {
            SaveOffset(Slider.Value);
        }

        private void SaveOffset(double value)
        {
            if (Loading || StoreField.GetValue(Window) is not GlassDeskDataStore store)
            {
                return;
            }

            GlassDeskSettings current = store.Settings.Current;
            if ((double)OffsetProperty.GetValue(current.Launcher)! == value)
            {
                return;
            }

            GlassDeskSettings snapshot = SettingsPageEditor.Clone(current);
            var next = (LauncherSettings)CopyObject.Invoke(current.Launcher, null)!;
            OffsetProperty.SetValue(next, value);
            current.Launcher = next;
            try
            {
                SavePage.Invoke(Window, new object[] { snapshot, "启动台", Status });
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
                if (StoreField.GetValue(Window) is not GlassDeskDataStore store)
                {
                    Panel.IsEnabled = false;
                    return;
                }

                LauncherSettings settings = store.Settings.Current.Launcher;
                var glassWindow = (GlassWindow)Window;
                var session = glassWindow.PrimaryDisplaySession;
                DisplayInfo? display = session != null && session.IsAvailable ? session.Display : DisplayService.Primary();
                double minimum = 0;
                double maximum = 0;
                if (display != null)
                {
                    PixelRect centered = WindowPlacementPolicy.Centered(display.WorkArea, settings.WindowWorkAreaRatio, false);
                    double dipPerPixel = 96.0 / (display.Dpi == 0 ? 96 : display.Dpi);
                    minimum = -Math.Floor(Math.Max(0, centered.Y - display.WorkArea.Y) * dipPerPixel);
                    maximum = Math.Floor(Math.Max(0, display.WorkArea.Bottom - centered.Bottom) * dipPerPixel);
                }

                Slider.Minimum = Math.Min(minimum, Slider.Maximum);
                Slider.Maximum = maximum;
                Slider.Minimum = minimum;
                double saved = (double)OffsetProperty.GetValue(settings)!;
                double effective = Math.Clamp(saved, minimum, maximum);
                Slider.Value = effective;
                Readout.Text = saved == effective
                    ? $"{saved:0} DIP"
                    : $"{saved:0} DIP，当前屏幕限制为 {effective:0} DIP";
                Panel.IsEnabled = !(bool)InvalidValuesField.GetValue(Window)!;
                Slider.IsEnabled = maximum > minimum;
            }
            finally
            {
                Loading = false;
            }
        }
    }

    public static void Configure(object instance)
    {
        var window = (Window)instance;
        if (Windows.TryGetValue(window, out _))
        {
            return;
        }

        Type type = window.GetType();
        var columns = (Slider)type.GetField("LauncherColumnsSlider", InstanceMembers)!.GetValue(window)!;
        var status = (TextBlock)type.GetField("LauncherSettingsStatus", InstanceMembers)!.GetValue(window)!;
        var parent = (Panel)columns.Parent;
        var controls = new Controls
        {
            Window = window,
            Panel = new StackPanel { Name = "LauncherVerticalOffsetPanel", Margin = new Thickness(0, 12, 0, 8) },
            Slider = new Slider
            {
                Name = "LauncherVerticalOffsetSlider",
                Minimum = -300,
                Maximum = 300,
                TickFrequency = 1,
                IsSnapToTickEnabled = true,
                SmallChange = 1,
                LargeChange = 10,
                Style = columns.Style,
                Margin = columns.Margin
            },
            Readout = new TextBlock { Name = "LauncherVerticalOffsetReadout", Foreground = status.Foreground, FontSize = 12 },
            Status = status,
            StoreField = type.GetField("_dataStore", InstanceMembers)!,
            InvalidValuesField = type.GetField("_launcherValuesOutOfRange", InstanceMembers)!,
            SavePage = type.GetMethod("SavePageSnapshot", InstanceMembers)!
        };

        var title = new DockPanel { LastChildFill = true, Margin = new Thickness(0, 0, 0, 6) };
        var reset = new Button { Name = "LauncherVerticalOffsetReset", Content = "恢复居中", Padding = new Thickness(10, 3, 10, 3) };
        DockPanel.SetDock(reset, System.Windows.Controls.Dock.Right);
        title.Children.Add(reset);
        var heading = (TextBlock)type.GetField("LauncherSectionHeading", InstanceMembers)!.GetValue(window)!;
        title.Children.Add(new TextBlock { Text = "垂直偏移", Foreground = heading.Foreground, VerticalAlignment = VerticalAlignment.Center });
        controls.Panel.Children.Add(title);
        controls.Panel.Children.Add(controls.Slider);
        controls.Panel.Children.Add(controls.Readout);
        controls.Panel.Children.Add(new TextBlock
        {
            Text = "0 为居中，负值向上，正值向下。调节后自动保存，范围随当前屏幕可用空间调整。",
            TextWrapping = TextWrapping.Wrap,
            Foreground = status.Foreground,
            FontSize = 12,
            Margin = new Thickness(0, 4, 0, 0)
        });
        parent.Children.Insert(parent.Children.IndexOf(columns) + 1, controls.Panel);
        Windows.Add(window, controls);
        reset.Click += controls.Center;
        controls.Slider.ValueChanged += controls.ValueChanged;
        window.IsVisibleChanged += controls.VisibilityChanged;
        controls.Load();
    }

    public static void Reload(object instance)
    {
        if (Windows.TryGetValue((Window)instance, out Controls? controls))
        {
            controls.Load();
        }
    }
}
