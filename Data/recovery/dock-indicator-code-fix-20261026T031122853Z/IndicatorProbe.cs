using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

// No application windows, services, or user data are created by this probe.
AssemblyLoadContext.Default.Resolving += (_, name) => {
    var path = Path.Combine(AppContext.BaseDirectory, name.Name + ".dll");
    return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
};
var dockPath = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.Combine(AppContext.BaseDirectory, "GlassDesk.Dock.dll");
var dock = AssemblyLoadContext.Default.LoadFromAssemblyPath(dockPath);
var modelType = dock.GetType("GlassDesk.Dock.DockTileModel", true)!;
var visibility = modelType.GetProperty("RunningVisibility")!;
var active = modelType.GetProperty("IsActive")!;
var helper = modelType.GetMethod("ApplyRunningIndicatorSetting", BindingFlags.Static | BindingFlags.NonPublic)!;
var cases = new List<object>();
foreach (var enabled in new[] { false, true })
foreach (var isActive in new[] { false, true })
foreach (var state in new[] { 0, 1, 2 }) {
    var model = Activator.CreateInstance(modelType)!;
    active.SetValue(model, isActive);
    helper.Invoke(null, new[] { model, Enum.ToObject(visibility.PropertyType, state), enabled });
    var actual = Convert.ToInt32(visibility.GetValue(model));
    var expected = enabled ? state : 2;
    if (actual != expected) throw new Exception($"Indicator mismatch: enabled={enabled}, active={isActive}, state={state}, actual={actual}");
    // Simulate repeated running-status refreshes after the first assignment.
    helper.Invoke(null, new[] { model, Enum.ToObject(visibility.PropertyType, 0), enabled });
    if (Convert.ToInt32(visibility.GetValue(model)) != (enabled ? 0 : 2)) throw new Exception("Refresh restored an unwanted indicator.");
    cases.Add(new { enabled, isActive, state, actual, refreshPassed = true });
}
Console.WriteLine(JsonSerializer.Serialize(new { passed = cases.Count, cases }));
