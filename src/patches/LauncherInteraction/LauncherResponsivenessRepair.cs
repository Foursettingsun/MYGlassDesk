using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using GlassDesk.Platform.Displays;

namespace GlassDesk.Launcher.Repair;

public static class LauncherResponsivenessRepair
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly ConditionalWeakTable<Window, WarmState> States = new();

    private sealed class WarmState
    {
        public object Host = null!;
        public Window Launcher = null!;
        public FrameworkElement Button = null!;
        public object Dock = null!;
        public bool Queued;
        public long LastWarm;
        public bool WindowPrimed;
        public DispatcherTimer? RetryTimer;
        public int Retries;
        public void Hover(object sender, MouseEventArgs e) { Queue(); }
        public void Press(object sender, MouseButtonEventArgs e) { Queue(); }
        public void Navigation(object? sender, EventArgs e)
        {
            bool expanded = Launcher.IsVisible && !Flag(Launcher, "IsClosingContainer");
            Dock.GetType().GetMethod("SetLauncherContainerState", Members)!.Invoke(Dock, new object[] { expanded });
        }
        public void Queue()
        {
            if (Queued || Launcher.IsVisible || Launcher.Dispatcher.HasShutdownStarted) return;
            Queued = true;
            Launcher.Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)Run);
        }
        public void Run()
        {
            Queued = false;
            if (Launcher.IsVisible || Launcher.Dispatcher.HasShutdownStarted || Flag(Host, "_disposed") || Flag(Host, "_shuttingDown")) return;
            object? performance = Read(Host, "_performance");
            if (performance == null || Flag(performance, "IsSuppressionRequested")) return;
            if (LastWarm != 0 && Stopwatch.GetElapsedTime(LastWarm).TotalMilliseconds < 750) return;
            long started = Stopwatch.GetTimestamp();
            try
            {
                // Reuse the existing ten-second hidden-resource deadline instead of
                // maintaining a permanently active capture loop or extending cache life.
                Write(Host, "_launcherHiddenSince", (DateTimeOffset?)DateTimeOffset.UtcNow);
                Call(Host, "ReconcilePerformance");
                Call(Launcher, "PositionOnWorkingArea");
                Call(Launcher, "PrepareToShow");
                if (!WindowPrimed && Launcher.GetType().FullName == "GlassDesk.Launcher.LauncherWindow")
                {
                    PrimeMethods(Launcher.GetType(), new[] { "ShowWithTransition", "CloseWithTransition", "BeginContainerTransform", "ApplyContainerTransformFrame", "OnContainerTransformRendering" });
                    Type? reveal = Type.GetType("GlassDesk.UI.Repair.LauncherRevealRepair, GlassDesk.UI");
                    if (reveal != null) PrimeMethods(reveal, new[] { "ShowPrepared", "BeginWhenReady", "Reveal", "Poll", "Observe" });
                    // Force first-use WPF template/layout loading off the click path.
                    // The window never becomes opaque, interactive or activated.
                    double opacity = Launcher.Opacity;
                    bool input = Launcher.IsHitTestVisible;
                    bool activated = Launcher.ShowActivated;
                    try
                    {
                        Launcher.Opacity = 0;
                        Launcher.IsHitTestVisible = false;
                        Launcher.ShowActivated = false;
                        Launcher.Show();
                        Launcher.Hide();
                    }
                    finally
                    {
                        if (Launcher.IsVisible) Launcher.Hide();
                        Launcher.Opacity = opacity;
                        Launcher.IsHitTestVisible = input;
                        Launcher.ShowActivated = activated;
                    }
                    WindowPrimed = true;
                    Call(Launcher, "PositionOnWorkingArea");
                    Call(Launcher, "PrepareToShow");
                }
                Call(Launcher, "PrewarmNextRevealMaterial");
                LastWarm = Stopwatch.GetTimestamp();
                if (Launcher.GetType().FullName == "GlassDesk.Launcher.LauncherWindow")
                {
                    RetryTimer?.Stop();
                    RetryTimer ??= new DispatcherTimer(DispatcherPriority.Background, Launcher.Dispatcher);
                    RetryTimer.Tick -= Retry;
                    RetryTimer.Tick += Retry;
                    RetryTimer.Interval = TimeSpan.FromMilliseconds(60);
                    Retries = 0;
                    RetryTimer.Start();
                }
                Trace("prewarm", Stopwatch.GetElapsedTime(started).TotalMilliseconds, null);
            }
            catch (Exception error)
            {
                // A failed speculative preparation must not break normal opening.
                Trace("prewarm-error", Stopwatch.GetElapsedTime(started).TotalMilliseconds, error.GetBaseException().Message);
            }
        }
        private void Retry(object? sender, EventArgs e)
        {
            RetryTimer!.Stop();
            if (Launcher.IsVisible || Launcher.Dispatcher.HasShutdownStarted || Flag(Host, "_disposed") || Flag(Host, "_shuttingDown")) return;
            object? performance = Read(Host, "_performance");
            if (performance == null || Flag(performance, "IsSuppressionRequested")) return;
            try
            {
                Retries++;
                Call(Launcher, "PrewarmNextRevealMaterial");
                object? surface = Read(Launcher, "GlassSurface");
                if (surface != null && Flag(surface, "IsCurrentRevealMaterialReady"))
                {
                    Trace("prewarm-ready", Stopwatch.GetElapsedTime(LastWarm).TotalMilliseconds, null);
                    return;
                }
                if (Retries < 3) RetryTimer.Start();
            }
            catch (Exception error) { Trace("prewarm-retry-error", 0, error.GetBaseException().Message); }
        }
    }

    public static void Install(object host)
    {
        if (Read(host, "_launcher") is not Window launcher || Read(host, "_dock") is not object dock) return;
        if (States.TryGetValue(launcher, out _)) return;
        if (Read(dock, "LauncherButton") is not FrameworkElement button) return;
        var state = new WarmState { Host = host, Launcher = launcher, Button = button, Dock = dock };
        States.Add(launcher, state);
        button.MouseEnter += state.Hover;
        button.PreviewMouseLeftButtonDown += state.Press;
        // Builtin Dock actions request collapse through the same legacy toggle.
        // Keep their existing active guard aligned with the animation target so
        // clicking This PC during collapse cannot reverse it into an open.
        launcher.GetType().GetEvent("NavigationAvailabilityChanged", Members)?.AddEventHandler(launcher, (EventHandler)state.Navigation);
        // The existing startup-idle callback invokes WarmPreparedLauncher below.
    }

    public static void WarmPreparedLauncher(Window launcher)
    {
        if (States.TryGetValue(launcher, out WarmState? state)) state.Run();
    }

    public static bool IsDockAnchor(Point point, PixelRect source, PixelRect host, double dipPerPixel,
        Thickness chrome, bool hasPath, bool animating, bool hitTestVisible)
    {
        if (!hasPath || (!animating && hitTestVisible) || source.Width <= 0 || source.Height <= 0 ||
            host.Width <= 0 || host.Height <= 0 || !double.IsFinite(dipPerPixel) || dipPerPixel <= 0) return false;
        var anchor = new Rect((source.X - host.X) * dipPerPixel - chrome.Left,
            (source.Y - host.Y) * dipPerPixel - chrome.Top, source.Width * dipPerPixel, source.Height * dipPerPixel);
        // WM_NCHITTEST will return HTTRANSPARENT for this area, so an animation
        // covering its own Dock source cannot intercept the next physical click.
        return anchor.Contains(point);
    }

    private static void PrimeMethods(Type type, string[] names)
    {
        foreach (string name in names)
        {
            foreach (MethodInfo method in type.GetMethods(Members | BindingFlags.Static))
                if (method.Name == name && !method.ContainsGenericParameters) RuntimeHelpers.PrepareMethod(method.MethodHandle);
        }
    }

    private static object? Read(object target, string name)
    {
        for (Type? type = target.GetType(); type != null; type = type.BaseType)
        {
            FieldInfo? field = type.GetField(name, Members | BindingFlags.DeclaredOnly);
            if (field != null) return field.GetValue(target);
            PropertyInfo? property = type.GetProperty(name, Members | BindingFlags.DeclaredOnly);
            if (property != null) return property.GetValue(target);
        }
        throw new MissingMemberException(target.GetType().FullName, name);
    }
    private static bool Flag(object target, string name) { return Read(target, name) is true; }
    private static void Write(object target, string name, object? value)
    {
        (target.GetType().GetField(name, Members) ?? throw new MissingFieldException(name)).SetValue(target, value);
    }
    private static object? Call(object target, string name)
    {
        return (target.GetType().GetMethod(name, Members, null, Type.EmptyTypes, null) ?? throw new MissingMethodException(name)).Invoke(target, null);
    }
    private static void Trace(string stage, double milliseconds, string? error)
    {
        string? directory = Environment.GetEnvironmentVariable("GLASSDESK_INTERACTION_TRACE_DIR");
        if (string.IsNullOrWhiteSpace(directory)) return;
        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, Stopwatch.GetTimestamp() + "-" + stage + ".json"),
                JsonSerializer.Serialize(new Dictionary<string, object?> { ["stage"] = stage, ["milliseconds"] = milliseconds, ["error"] = error }));
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
