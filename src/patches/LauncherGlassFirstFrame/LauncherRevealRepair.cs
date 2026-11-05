using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

[assembly: AssemblyVersion("1.0.0.0")]

namespace GlassDesk.UI.Repair;

// Merged into GlassDesk.UI. It operates only on launcher glass surfaces.
public static class LauncherRevealRepair
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
    private static readonly ConditionalWeakTable<Window, RevealState> Windows = new();
    private static readonly ConditionalWeakTable<object, Snapshot> Snapshots = new();

    private sealed class Snapshot
    {
        public BitmapSource? Bitmap;
        public object? Stamp;
        public object? Scene;
        public object? Bounds;
        public long CapturedAt;
        public bool Restoring;
    }

    private sealed class RevealState
    {
        public Window? Window;
        public object? Surface;
        public DispatcherTimer? Timer;
        public bool Preparing;
        public bool BeginRequested;
        public bool OriginalHitTest;
        public bool OriginalTransparent;
        public bool InitialReady;
        public bool ReadyAtReveal;
        public bool SnapshotAtReveal;
        public bool Degraded;
        public bool LiveObserved;
        public double OriginalOpacity;
        public double Target;
        public long Started;
        public long LastAttempt;
        public long RevealedAt;
        public long LastObservedRender;
        public int Attempts;
        public int ReadyTicks;
        public int VisibleRenderCount;
        public int MissingMaterialRenders;
        public int SnapshotRestores;
        public string? Error;
        public void Tick(object? sender, EventArgs args) { Poll(this); }
        public void VisibilityChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            if (Window != null && !Window.IsVisible) Cancel(this);
        }
        public void Closed(object? sender, EventArgs args)
        {
            Cancel(this);
            if (Window != null) Windows.Remove(Window);
        }
    }

    public static void ShowPrepared(Window window)
    {
        object? surface = Read(window, "GlassSurface");
        if (surface == null || !IsLauncherSurface(surface) || Read(window, "SurfaceMode")?.ToString() != "OpticalGlass")
        {
            window.Show();
            return;
        }
        RevealState state = Windows.GetOrCreateValue(window);
        if (state.Window == null)
        {
            state.Window = window;
            window.IsVisibleChanged += state.VisibilityChanged;
            window.Closed += state.Closed;
        }
        state.Timer?.Stop();
        state.Surface = surface;
        state.OriginalOpacity = window.Opacity;
        state.OriginalHitTest = window.IsHitTestVisible;
        state.OriginalTransparent = Flag(surface, "TransparentWhileLoading");
        state.Started = Stopwatch.GetTimestamp();
        state.RevealedAt = state.LastAttempt = state.LastObservedRender = 0;
        state.Attempts = state.ReadyTicks = state.VisibleRenderCount = state.MissingMaterialRenders = state.SnapshotRestores = 0;
        state.BeginRequested = state.Degraded = state.LiveObserved = state.ReadyAtReveal = state.SnapshotAtReveal = false;
        state.Error = null;
        state.InitialReady = Ready(surface);
        state.Preparing = true;
        // Keep the real window loaded so capture can advance, but never expose a pending frame.
        window.Opacity = 0;
        window.IsHitTestVisible = false;
        window.Show();
        state.Timer ??= new DispatcherTimer(DispatcherPriority.Render, window.Dispatcher);
        state.Timer.Tick -= state.Tick;
        state.Timer.Tick += state.Tick;
        state.Timer.Interval = TimeSpan.FromMilliseconds(16);
        state.Timer.Start();
    }

    public static void BeginWhenReady(Window window, double timeline)
    {
        if (Windows.TryGetValue(window, out RevealState? state) && state.Preparing && timeline > 0)
        {
            state.BeginRequested = true;
            state.Target = timeline;
            return;
        }
        Call(window, "BeginContainerTransform", timeline);
    }

    public static void AfterPrepare(object surface)
    {
        if (!IsLauncherSurface(surface)) return;
        RememberSnapshot(surface);
        RestoreSnapshot(surface);
    }

    public static void AfterEnsure(object surface)
    {
        if (!IsLauncherSurface(surface)) return;
        RestoreSnapshot(surface);
    }

    public static void AfterSleep(object surface, bool sleeping)
    {
        if (sleeping) Snapshots.Remove(surface);
    }

    private static void RememberSnapshot(object surface)
    {
        if (!Flag(surface, "_compatibilityIsDesktopSnapshot") || Read(surface, "_compatibilityBackdrop") is not BitmapSource bitmap) return;
        // Keep at most one already processed image, without capture devices or rendering workers.
        if ((long)bitmap.PixelWidth * bitmap.PixelHeight > 2000000) return;
        Snapshot saved = Snapshots.GetOrCreateValue(surface);
        if (!ReferenceEquals(saved.Bitmap, bitmap)) saved.CapturedAt = Stopwatch.GetTimestamp();
        saved.Bitmap = bitmap;
        saved.Stamp = Read(surface, "_compatibilityStamp");
        saved.Scene = Read(surface, "_compatibilityScene");
        saved.Bounds = Read(surface, "_compatibilityBounds");
    }

    private static void RestoreSnapshot(object surface)
    {
        if (!Snapshots.TryGetValue(surface, out Snapshot? saved) || saved.Restoring || saved.Bitmap == null || Flag(surface, "_performanceSleeping")) return;
        if (Stopwatch.GetElapsedTime(saved.CapturedAt).TotalMilliseconds > 1500) return;
        Window? window = surface is DependencyObject element ? Window.GetWindow(element) : null;
        if (window == null) return;
        bool opening = Windows.TryGetValue(window, out RevealState? state) && (state.Preparing || Flag(window, "_transformActive"));
        object? stamp = Call(surface, "ReadMaterialStamp");
        object? scene = Read(surface, "_backdropScene");
        if (stamp == null || !stamp.Equals(saved.Stamp) || scene == null || saved.Scene == null) return;
        bool sameScene = scene.Equals(saved.Scene);
        if (!sameScene && (!opening || Call(scene, "CanRetainMaterialFrom", saved.Scene) is not true)) return;
        if (HasLive(surface)) return;
        if (Read(surface, "_compatibilityBackdrop") != null && Flag(surface, "_compatibilityIsDesktopSnapshot")) return;
        saved.Restoring = true;
        try
        {
            Write(surface, "_compatibilityBackdrop", saved.Bitmap);
            Write(surface, "_compatibilityStamp", stamp);
            Write(surface, "_compatibilityBounds", saved.Bounds);
            Write(surface, "_compatibilityScene", scene);
            Write(surface, "_compatibilityIsDesktopSnapshot", true);
            Write(surface, "_handoffStarted", 0L);
            Call(surface, "UpdateTransitionLayer", (object?)null);
            if (state != null) state.SnapshotRestores++;
        }
        finally { saved.Restoring = false; }
    }

    private static void Poll(RevealState state)
    {
        Window window = state.Window!;
        object surface = state.Surface!;
        if (!window.IsVisible || window.Dispatcher.HasShutdownStarted) { Cancel(state); return; }
        try
        {
            if (!state.Preparing)
            {
                Observe(state);
                if (Stopwatch.GetElapsedTime(state.RevealedAt).TotalMilliseconds >= 1300)
                {
                    state.Timer!.Stop();
                    TraceResult(state, "complete");
                }
                return;
            }
            bool ready = Ready(surface);
            if (!ready && state.Attempts < 4 && (state.LastAttempt == 0 || Stopwatch.GetElapsedTime(state.LastAttempt).TotalMilliseconds >= 60))
            {
                state.LastAttempt = Stopwatch.GetTimestamp();
                state.Attempts++;
                object? original = Read(surface, "IsBackdropActive");
                try
                {
                    SetProperty(surface, "IsBackdropActive", new Func<bool>(Inactive));
                    Call(window, "PrepareRevealMaterial");
                }
                finally { SetProperty(surface, "IsBackdropActive", original); }
                Call(surface, "RequestBackdropRefresh", true);
                ready = Ready(surface);
            }
            state.ReadyTicks = ready ? state.ReadyTicks + 1 : 0;
            if (state.ReadyTicks >= 2) { Reveal(state, false); return; }
            if (Stopwatch.GetElapsedTime(state.Started).TotalMilliseconds >= 3000)
            {
                // Capture failure must still leave the launcher usable with one stable fallback.
                state.Degraded = true;
                SetProperty(surface, "TransparentWhileLoading", false);
                Call(surface, "PrepareCompatibleMaterial", ((FrameworkElement)surface).RenderSize);
                Call(surface, "UpdateTransitionLayer", (object?)null);
                Reveal(state, true);
            }
        }
        catch (Exception error)
        {
            state.Error = error.GetBaseException().GetType().Name + ": " + error.GetBaseException().Message;
            state.Degraded = true;
            Reveal(state, true);
        }
    }

    private static bool Inactive() { return false; }

    private static bool IsLauncherSurface(object surface)
    {
        return surface is DependencyObject element && Window.GetWindow(element)?.GetType().FullName == "GlassDesk.Launcher.LauncherWindow";
    }

    private static void Reveal(RevealState state, bool degraded)
    {
        object surface = state.Surface!;
        Window window = state.Window!;
        state.ReadyAtReveal = !degraded && Ready(surface);
        state.SnapshotAtReveal = Flag(surface, "_compatibilityIsDesktopSnapshot");
        Write(surface, "_firstVisibleRenderMaterialReady", null);
        Write(surface, "_firstVisibleMaterialSource", null);
        state.Preparing = false;
        state.RevealedAt = Stopwatch.GetTimestamp();
        if (state.BeginRequested) Call(window, "BeginContainerTransform", state.Target);
        window.IsHitTestVisible = state.OriginalHitTest;
        window.Opacity = state.OriginalOpacity;
        TraceResult(state, "presented");
    }

    private static void Observe(RevealState state)
    {
        object surface = state.Surface!;
        long render = Read(surface, "_lastRenderTicks") is long ticks ? ticks : 0;
        if (render > state.RevealedAt && render != state.LastObservedRender)
        {
            state.LastObservedRender = render;
            state.VisibleRenderCount++;
            if (!Flag(surface, "_lastRenderMaterialReady")) state.MissingMaterialRenders++;
        }
        state.LiveObserved |= HasLive(surface);
        if (state.LiveObserved && state.Degraded) SetProperty(surface, "TransparentWhileLoading", state.OriginalTransparent);
    }

    private static bool HasLive(object surface)
    {
        object? scene = Read(surface, "_backdropScene");
        object? stamp = Read(surface, "_materialStamp");
        if (scene == null || stamp == null || !stamp.Equals(Call(surface, "ReadMaterialStamp"))) return false;
        return (Read(surface, "_frame") != null && scene.Equals(Read(surface, "_frameScene")))
            || (Read(surface, "_transitionBackdrop") != null && scene.Equals(Read(surface, "_transitionBackdropScene")));
    }

    private static bool Ready(object surface) { return Read(surface, "IsCurrentRevealMaterialReady") is true; }

    private static void Cancel(RevealState state)
    {
        state.Timer?.Stop();
        if (state.Window == null) return;
        if (state.Preparing) TraceResult(state, "cancelled");
        state.Preparing = false;
        state.Window.Opacity = state.OriginalOpacity;
        state.Window.IsHitTestVisible = state.OriginalHitTest;
        if (state.Surface != null) SetProperty(state.Surface, "TransparentWhileLoading", state.OriginalTransparent);
    }

    private static void TraceResult(RevealState state, string stage)
    {
        string? directory = Environment.GetEnvironmentVariable("GLASSDESK_REVEAL_TRACE_DIR");
        if (string.IsNullOrWhiteSpace(directory)) return;
        try
        {
            var metrics = new Dictionary<string, object?>
            {
                ["stage"] = stage, ["processId"] = Environment.ProcessId, ["requestTicks"] = state.Started,
                ["initialReady"] = state.InitialReady, ["readyAtReveal"] = state.ReadyAtReveal,
                ["snapshotAtReveal"] = state.SnapshotAtReveal, ["degraded"] = state.Degraded,
                ["waitMilliseconds"] = state.RevealedAt == 0 ? null : Stopwatch.GetElapsedTime(state.Started, state.RevealedAt).TotalMilliseconds,
                ["attempts"] = state.Attempts, ["snapshotRestores"] = state.SnapshotRestores,
                ["visibleRenderCount"] = state.VisibleRenderCount, ["missingMaterialRenders"] = state.MissingMaterialRenders,
                ["liveObserved"] = state.LiveObserved, ["error"] = state.Error,
                ["surface"] = state.Surface == null ? null : Call(state.Surface, "InspectPerformance")
            };
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, state.Started.ToString() + "-" + stage + ".json"), JsonSerializer.Serialize(metrics));
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static FieldInfo? FindField(Type type, string name)
    {
        for (Type? current = type; current != null; current = current.BaseType)
        {
            FieldInfo? found = current.GetField(name, Members);
            if (found != null) return found;
        }
        return null;
    }

    private static PropertyInfo? FindProperty(Type type, string name)
    {
        for (Type? current = type; current != null; current = current.BaseType)
        {
            PropertyInfo? found = current.GetProperty(name, Members);
            if (found != null) return found;
        }
        return null;
    }

    private static object? Read(object target, string name)
    {
        FieldInfo? field = FindField(target.GetType(), name);
        if (field != null) return field.GetValue(target);
        PropertyInfo? property = FindProperty(target.GetType(), name);
        if (property != null) return property.GetValue(target);
        throw new MissingMemberException(target.GetType().FullName, name);
    }

    private static bool Flag(object target, string name) { return Read(target, name) is true; }

    private static void Write(object target, string name, object? value)
    {
        (FindField(target.GetType(), name) ?? throw new MissingFieldException(target.GetType().FullName, name)).SetValue(target, value);
    }

    private static void SetProperty(object target, string name, object? value)
    {
        (FindProperty(target.GetType(), name) ?? throw new MissingMemberException(target.GetType().FullName, name)).SetValue(target, value);
    }

    private static object? Call(object target, string name, params object?[] arguments)
    {
        for (Type? type = target.GetType(); type != null; type = type.BaseType)
        {
            foreach (MethodInfo method in type.GetMethods(Members))
                if (method.Name == name && method.GetParameters().Length == arguments.Length) return method.Invoke(target, arguments);
        }
        throw new MissingMethodException(target.GetType().FullName, name);
    }
}
