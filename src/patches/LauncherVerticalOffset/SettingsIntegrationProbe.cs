using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using GlassDesk.AppServices;
using GlassDesk.Core.Models;
using GlassDesk.Core.Models.Settings;
using GlassDesk.Core.Storage;
using GlassDesk.Launcher;
using GlassDesk.Launcher.Repair;
using GlassDesk.Platform.Displays;
using GlassDesk.Settings;
using GlassDesk.Settings.Repair;
using GlassDesk.UI.Theming;

internal static class Program
{
    private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    [STAThread]
    private static void Main(string[] args)
    {
        string directory = Path.GetFullPath(args[0]);
        Directory.CreateDirectory(directory);
        var data = new GlassDeskDataStore(new AppPaths(directory));
        var report = data.LoadAll();
        data.Library.Add(new AppEntry { Id = "fixture-one", DisplayName = "One", Kind = AppItemKind.Url, Target = "https://example.invalid/one" });
        data.Library.Add(new AppEntry { Id = "fixture-two", DisplayName = "Two", Kind = AppItemKind.Url, Target = "https://example.invalid/two" });
        using var services = new AppServiceHost(data);
        data.Settings.Current.Appearance.GlassMaterial = GlassMaterialMode.PlainGlass;
        using var theme = ThemeService.Initialize(Dispatcher.CurrentDispatcher);
        theme.Update(data.Settings.Current.Appearance);
        var launcher = new LauncherWindow();
        launcher.AttachServices(services, true);
        launcher.ApplySettings(data.Settings.Current.Launcher);
        launcher.Opacity = 0;
        launcher.IsHitTestVisible = false;
        launcher.ShowActivated = false;
        launcher.Show();
        Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(() => { }));
        launcher.Hide();
        launcher.Opacity = 1;
        launcher.IsHitTestVisible = true;
        Check(launcher.IsLoaded, "The isolated launcher visual tree did not finish loading.");
        var settings = new SettingsWindow();
        settings.AttachDataStore(data, report);
        int updates = 0;
        settings.LauncherSettingsChanged += (_, value) => { updates++; launcher.ApplySettings(value); };

        var slider = Find<Slider>(settings, "LauncherVerticalOffsetSlider");
        var center = Find<Button>(settings, "LauncherVerticalOffsetReset");
        var rows = Get<Slider>(settings, "LauncherRowsSlider");
        var grid = Get<Panel>(launcher, "TileGrid");
        settings.Measure(new Size(1100, 760));
        settings.Arrange(new Rect(0, 0, 1100, 760));
        settings.UpdateLayout();
        Check(slider.IsEnabled && slider.Minimum < 0 && slider.Maximum > 0, "The settings slider is unavailable.");
        Check(grid.Children.Count == 2, "The fixture application tiles were not constructed.");
        double value = Math.Min(20, Math.Floor(slider.Maximum / 2));
        Check(value > 0, "The display has no vertical adjustment room.");
        var tiles = grid.Children.Cast<UIElement>().ToArray();
        slider.Value = value;
        Check(data.Settings.Current.Launcher.OffsetY == value && updates == 1, "The slider did not save and broadcast its value.");
        Check(tiles.SequenceEqual(grid.Children.Cast<UIElement>()), "Changing only position rebuilt application tiles.");
        var loaded = new SettingsStore(data.Paths.SettingsFile);
        loaded.Load();
        Check(loaded.Current.Launcher.OffsetY == value, "The saved offset did not survive a new SettingsStore.");

        rows.Value = 6;
        Check(data.Settings.Current.Launcher.Rows == 6 && data.Settings.Current.Launcher.OffsetY == value, "Changing grid rows reset the vertical offset.");
        loaded.Load();
        Check(loaded.Current.Launcher.OffsetY == value, "A grid update dropped the persisted offset.");
        Check(!tiles.SequenceEqual(grid.Children.Cast<UIElement>()), "A grid change skipped the required tile refresh.");

        string beforeFailure = File.ReadAllText(data.Paths.SettingsFile);
        int beforeUpdates = updates;
        using (var lockedFile = new FileStream(data.Paths.SettingsFile, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            slider.Value = -value;
            Check(data.Settings.Current.Launcher.OffsetY == value && slider.Value == value, "A failed save did not restore the model and slider.");
        }
        Check(updates == beforeUpdates && File.ReadAllText(data.Paths.SettingsFile) == beforeFailure, "A failed save changed the active placement or disk data.");
        center.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(data.Settings.Current.Launcher.OffsetY == 0 && slider.Value == 0, "Restore center did not save zero.");

        data.Settings.Current.Launcher.WindowWorkAreaRatio = 1;
        data.Settings.Current.Launcher.OffsetY = value;
        LauncherOffsetControls.Reload(settings);
        Check(slider.Minimum == 0 && slider.Maximum == 0, "A full-height window has a nonzero adjustment range.");
        center.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(data.Settings.Current.Launcher.OffsetY == 0, "Restore center failed when the slider had no movement range.");

        var pageList = Get<ListBox>(settings, "SettingsPageList");
        pageList.SelectedIndex = 3;
        typeof(SettingsWindow).GetMethod("OnResetPageClick", PrivateInstance)!.Invoke(settings, new object?[] { null, new RoutedEventArgs() });
        Check(data.Settings.Current.Launcher.OffsetY == 0 && data.Settings.Current.Launcher.Rows == 5, "Resetting the launcher page did not restore its defaults.");

        IntPtr handle = new WindowInteropHelper(launcher).EnsureHandle();
        launcher.PositionOnWorkingArea();
        PixelRect bounds = DisplayService.GetWindowPixelBounds(handle)!.Value;
        int messages = 0;
        HwndSourceHook hook = (IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled) =>
        {
            if (message == 70) messages++;
            return IntPtr.Zero;
        };
        var source = HwndSource.FromHwnd(handle)!;
        source.AddHook(hook);
        DisplayService.MoveWindowToPixelRect(handle, bounds);
        int originalMessages = messages;
        messages = 0;
        Check(LauncherPlacementRepair.MoveWindowIfNeeded(handle, bounds), "An unchanged native rectangle did not succeed.");
        Check(messages == 0, "An unchanged native rectangle still caused positioning messages.");
        source.RemoveHook(hook);

        Console.WriteLine(JsonSerializer.Serialize(new
        {
            SliderRange = new[] { slider.Minimum, slider.Maximum },
            SliderSaveAndReload = true,
            GridEditRetainsOffset = true,
            OffsetOnlyRetainsTileInstances = true,
            SaveFailureRollback = true,
            RestoreCenterAndPageReset = true,
            OriginalUnchangedMoveMessages = originalMessages,
            OptimizedUnchangedMoveMessages = messages,
            VisibleWindowContent = false
        }));
        settings.Close();
        launcher.Close();
    }

    private static T Get<T>(object owner, string name)
    {
        return (T)owner.GetType().GetField(name, PrivateInstance)!.GetValue(owner)!;
    }

    private static T Find<T>(DependencyObject root, string name) where T : FrameworkElement
    {
        if (root is T element && element.Name == name) return element;
        foreach (object child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is not DependencyObject dependency) continue;
            T? found = TryFind<T>(dependency, name);
            if (found != null) return found;
        }
        throw new InvalidOperationException("Missing control " + name);
    }

    private static T? TryFind<T>(DependencyObject root, string name) where T : FrameworkElement
    {
        if (root is T element && element.Name == name) return element;
        foreach (object child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is DependencyObject dependency)
            {
                T? found = TryFind<T>(dependency, name);
                if (found != null) return found;
            }
        }
        return null;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
