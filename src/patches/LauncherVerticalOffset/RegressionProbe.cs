using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Windows;
using System.Windows.Threading;
using GlassDesk.Platform.Displays;

internal static class Program
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    [STAThread]
    private static void Main(string[] args)
    {
        string directory = Path.GetFullPath(args[1]);
        var context = new AssemblyLoadContext("vertical-offset-probe");
        context.Resolving += (_, name) => File.Exists(Path.Combine(directory, name.Name + ".dll"))
            ? context.LoadFromAssemblyPath(Path.Combine(directory, name.Name + ".dll"))
            : AssemblyLoadContext.Default.LoadFromAssemblyName(name);

        if (args[0] == "allocation")
        {
            MeasureRenderAllocation(context.LoadFromAssemblyPath(Path.Combine(directory, "GlassDesk.UI.dll")));
            return;
        }

        Assembly core = context.LoadFromAssemblyPath(Path.Combine(directory, "GlassDesk.Core.dll"));
        Assembly launcher = context.LoadFromAssemblyPath(Path.Combine(directory, "GlassDesk.Launcher.dll"));
        Assembly ui = context.LoadFromAssemblyPath(Path.Combine(directory, "GlassDesk.UI.dll"));
        Assembly settings = context.LoadFromAssemblyPath(Path.Combine(directory, "GlassDesk.Settings.dll"));
        Type launcherModel = core.GetType("GlassDesk.Core.Models.Settings.LauncherSettings", true)!;
        Type rootModel = core.GetType("GlassDesk.Core.Models.Settings.GlassDeskSettings", true)!;
        Type editor = core.GetType("GlassDesk.Core.Models.Settings.SettingsPageEditor", true)!;
        Type jsonContext = core.GetType("GlassDesk.Core.Serialization.GlassDeskJsonContext", true)!;
        PropertyInfo offset = launcherModel.GetProperty("OffsetY")!;
        PropertyInfo launcherProperty = rootModel.GetProperty("Launcher")!;
        object root = Activator.CreateInstance(rootModel)!;
        object model = launcherProperty.GetValue(root)!;
        Check((double)offset.GetValue(model)! == 0, "The default offset must center the launcher.");
        offset.SetValue(model, -40.0);
        object clone = editor.GetMethod("Clone")!.Invoke(null, new[] { root })!;
        Check((double)offset.GetValue(launcherProperty.GetValue(clone))! == -40, "Settings cloning discarded the offset.");
        object resolver = jsonContext.GetProperty("Default")!.GetValue(null)!;
        var typeInfo = (JsonTypeInfo)jsonContext.GetProperty("GlassDeskSettings")!.GetValue(resolver)!;
        string json = JsonSerializer.Serialize(root, typeInfo);
        Check(json.Contains("\"offsetY\": -40"), "The root fast writer omitted the offset.");
        object loaded = JsonSerializer.Deserialize(json, typeInfo)!;
        Check((double)offset.GetValue(launcherProperty.GetValue(loaded))! == -40, "JSON loading discarded the offset.");
        object legacy = JsonSerializer.Deserialize("{\"launcher\":{\"rows\":5,\"columns\":7}}", typeInfo)!;
        Check((double)offset.GetValue(launcherProperty.GetValue(legacy))! == 0, "Legacy settings did not default to center.");
        object reset = editor.GetMethod("ResetPage")!.Invoke(null, new[] { root, "启动台" })!;
        Check((double)offset.GetValue(launcherProperty.GetValue(reset))! == 0, "Page reset did not center the launcher.");
        foreach (double invalid in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            offset.SetValue(model, invalid);
            Check((double)offset.GetValue(model)! == 0, "A non-finite offset survived normalization.");
        }
        offset.SetValue(model, 1e99);
        Check((double)offset.GetValue(model)! == 10000, "A large offset exceeded the supported setting bound.");

        Type placement = launcher.GetType("GlassDesk.Launcher.Repair.LauncherPlacementRepair", true)!;
        MethodInfo apply = placement.GetMethod("ApplyVerticalOffset")!;
        int placementCases = 0;
        foreach (uint dpi in new uint[] { 96, 120, 144, 192 })
        {
            var display = new DisplayInfo("test", true, new PixelRect(-1920, -200, 1920, 1080), new PixelRect(-1920, -200, 1920, 1040), dpi);
            PixelRect centered = WindowPlacementPolicy.Centered(display.WorkArea, 0.66, false);
            foreach (double value in new[] { -10000.0, -40.0, -1.0, -0.5, 0.0, 0.5, 1.0, 40.0, 10000.0 })
            {
                var result = (PixelRect)apply.Invoke(null, new object[] { centered, display, value })!;
                double pixelOffset = Math.Round(value * dpi / 96.0, MidpointRounding.AwayFromZero);
                int expectedY = (int)Math.Clamp(centered.Y + pixelOffset, display.WorkArea.Y, display.WorkArea.Bottom - centered.Height);
                Check(result.Y == expectedY && result.X == centered.X && result.Width == centered.Width && result.Height == centered.Height, "DPI-aware placement differs from the requested offset.");
                placementCases++;
            }
        }
        object previous = Activator.CreateInstance(launcherModel)!;
        object next = Activator.CreateInstance(launcherModel)!;
        MethodInfo positionOnly = placement.GetMethod("IsPositionOnly")!;
        Check((bool)positionOnly.Invoke(null, new[] { previous, next, (object)0.0, 20.0 })!, "An offset-only update did not use the position path.");
        launcherModel.GetProperty("Rows")!.SetValue(next, 6);
        Check(!(bool)positionOnly.Invoke(null, new[] { previous, next, (object)0.0, 20.0 })!, "A grid change skipped the full update.");
        foreach (string method in new[] { "PositionOnDisplay", "ApplySettings" })
        {
            RuntimeHelpers.PrepareMethod(launcher.GetType("GlassDesk.Launcher.LauncherWindow", true)!.GetMethod(method, Members)!.MethodHandle);
        }
        RuntimeHelpers.PrepareMethod(settings.GetType("GlassDesk.Settings.SettingsWindow", true)!.GetMethod("OnLauncherControlChanged", Members)!.MethodHandle);
        RuntimeHelpers.PrepareMethod(ui.GetType("GlassDesk.UI.Controls.LiquidGlassSurface", true)!.GetMethod("OnRenderingFrame", Members)!.MethodHandle);
        Console.WriteLine(JsonSerializer.Serialize(new { SerializationRoundTrip = true, LegacySettings = true, CloneAndReset = true, InvalidValueNormalization = true, PlacementCases = placementCases, PositionOnlyDetection = true, PatchedMethodsJitPassed = true }));
    }

    private static void MeasureRenderAllocation(Assembly assembly)
    {
        Type type = assembly.GetType("GlassDesk.UI.Controls.LiquidGlassSurface", true)!;
        object surface = Activator.CreateInstance(type)!;
        MethodInfo rendering = type.GetMethod("OnRenderingFrame", Members)!;
        FieldInfo queued = type.GetField("_renderFrameQueued", Members)!;
        FieldInfo operation = type.GetField("_renderFrameOperation", Members)!;
        object[] arguments = { null!, EventArgs.Empty };
        object notQueued = false;
        void Frame()
        {
            queued.SetValue(surface, notQueued);
            rendering.Invoke(surface, arguments);
            ((DispatcherOperation?)operation.GetValue(surface))?.Abort();
        }
        for (int index = 0; index < 200; index++) Frame();
        GC.Collect();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < 2000; index++) Frame();
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        ((IDisposable)surface).Dispose();
        Console.WriteLine(JsonSerializer.Serialize(new { Frames = 2000, AllocatedBytes = bytes, BytesPerCallback = bytes / 2000.0, CachedCallbackField = type.GetField("_cachedRenderFrameAction", Members) != null }));
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
