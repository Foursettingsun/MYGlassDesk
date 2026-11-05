using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Security.Cryptography;
using System.Text.Json;

if (args.Length != 2) throw new ArgumentException("Usage: HoverRepair input.dll staged-output.dll");
var input = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
if (input == output || File.Exists(output)) throw new InvalidOperationException("A new staging path is required.");
using var assembly = AssemblyDefinition.ReadAssembly(input, new ReaderParameters { InMemory = true });
var module = assembly.MainModule;
if (assembly.Name.HasPublicKey || (module.Attributes & ModuleAttributes.ILOnly) == 0)
    throw new InvalidOperationException("Expected the existing unsigned managed Dock component.");
var window = module.GetType("GlassDesk.Dock.DockWindow");
var model = module.GetType("GlassDesk.Dock.DockTileModel");
if (!model.Methods.Any(m => m.Name == "ApplyRunningIndicatorSetting"))
    throw new InvalidOperationException("Existing running-indicator repair must be preserved.");
const double oldPeak = 1.15;
const double newPeak = 1.38;
var expected = new Dictionary<string, int> { ["AnimateEntryHoverFrame"] = 3, ["ApplyStripMetrics"] = 1 };
var changed = new List<object>();
foreach (var (methodName, expectedCount) in expected) {
    var method = window.Methods.Single(m => m.Name == methodName);
    var constants = method.Body.Instructions.Where(i => i.OpCode == OpCodes.Ldc_R8 && i.Operand is double d && d == oldPeak).ToArray();
    if (constants.Length != expectedCount) throw new InvalidOperationException($"Unexpected peak count in {methodName}.");
    foreach (var instruction in constants) instruction.Operand = newPeak;
    changed.Add(new { method = methodName, count = constants.Length });
}
assembly.Write(output);
using var reread = AssemblyDefinition.ReadAssembly(output);
var afterModule = reread.MainModule;
foreach (var before in module.Resources.OfType<EmbeddedResource>()) {
    var after = afterModule.Resources.OfType<EmbeddedResource>().Single(r => r.Name == before.Name);
    if (!SHA256.HashData(before.GetResourceData()).SequenceEqual(SHA256.HashData(after.GetResourceData())))
        throw new InvalidOperationException("Embedded resources changed.");
}
foreach (var (methodName, expectedCount) in expected) {
    var method = afterModule.GetType(window.FullName).Methods.Single(m => m.Name == methodName);
    if (method.Body.Instructions.Count(i => i.OpCode == OpCodes.Ldc_R8 && i.Operand is double d && d == newPeak) != expectedCount)
        throw new InvalidOperationException("Peak verification failed.");
}
Console.WriteLine(JsonSerializer.Serialize(new { oldPeak, newPeak, relativeIncrease = newPeak / oldPeak - 1, changed, runningIndicatorRepairPreserved = true,
    outputSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(output))) }));
