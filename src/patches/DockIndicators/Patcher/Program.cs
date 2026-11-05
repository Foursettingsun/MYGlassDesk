using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Security.Cryptography;
using System.Text.Json;
using System.Reflection.PortableExecutable;

if (args.Length != 2) throw new ArgumentException("Usage: IndicatorRepair input.dll output.dll");
var input = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
if (input == output || File.Exists(output)) throw new InvalidOperationException("Output must be a new staging file.");
var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(Path.GetDirectoryName(input));
using var assembly = AssemblyDefinition.ReadAssembly(input, new ReaderParameters { AssemblyResolver = resolver, InMemory = true });
var module = assembly.MainModule;
if (assembly.Name.HasPublicKey) throw new InvalidOperationException("Refusing to invalidate a signed assembly.");
// Published assemblies contain ReadyToRun native caches alongside complete IL.
// Cecil emits the repaired managed IL; only strip caches with the RTR signature.
var strippedReadyToRun = false;
if ((module.Attributes & ModuleAttributes.ILOnly) == 0) {
    using var pe = new PEReader(File.OpenRead(input));
    var native = pe.PEHeaders.CorHeader!.ManagedNativeHeaderDirectory;
    if (native.Size < 4 || pe.GetSectionData(native.RelativeVirtualAddress).GetReader().ReadUInt32() != 0x00525452)
        throw new InvalidOperationException("Refusing a mixed-mode assembly without a ReadyToRun header.");
    module.Attributes |= ModuleAttributes.ILOnly;
    strippedReadyToRun = true;
}
var model = module.GetType("GlassDesk.Dock.DockTileModel");
var window = module.GetType("GlassDesk.Dock.DockWindow");
var setter = model.Methods.Single(m => m.Name == "set_RunningVisibility");
var visibilityType = setter.Parameters.Single().ParameterType;
var getSettings = window.Methods.Single(m => m.Name == "get_Settings");
using var core = AssemblyDefinition.ReadAssembly(Path.Combine(Path.GetDirectoryName(input)!, "GlassDesk.Core.dll"), new ReaderParameters { AssemblyResolver = resolver });
var settings = core.MainModule.GetType("GlassDesk.Core.Models.Settings.DockSettings");
var getFlag = module.ImportReference(settings.Methods.Single(m => m.Name == "get_ShowRunningIndicator"));
const string helperName = "ApplyRunningIndicatorSetting";
if (model.Methods.Any(m => m.Name == helperName)) throw new InvalidOperationException("Patch already exists.");
var helper = new MethodDefinition(helperName, MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig, module.TypeSystem.Void);
helper.Parameters.Add(new ParameterDefinition("model", ParameterAttributes.None, model));
helper.Parameters.Add(new ParameterDefinition("visibility", ParameterAttributes.None, visibilityType));
helper.Parameters.Add(new ParameterDefinition("enabled", ParameterAttributes.None, module.TypeSystem.Boolean));
model.Methods.Add(helper);
var il = helper.Body.GetILProcessor();
var enabledValue = Instruction.Create(OpCodes.Ldarg_1);
var apply = Instruction.Create(OpCodes.Callvirt, setter);
il.Append(Instruction.Create(OpCodes.Ldarg_0));
il.Append(Instruction.Create(OpCodes.Ldarg_2));
il.Append(Instruction.Create(OpCodes.Brtrue, enabledValue));
il.Append(Instruction.Create(OpCodes.Ldc_I4_2));
il.Append(Instruction.Create(OpCodes.Br, apply));
il.Append(enabledValue);
il.Append(apply);
il.Append(Instruction.Create(OpCodes.Ret));

IEnumerable<TypeDefinition> AllTypes(IEnumerable<TypeDefinition> types) => types.SelectMany(t => new[] { t }.Concat(AllTypes(t.NestedTypes)));
var sites = AllTypes(module.Types).SelectMany(t => t.Methods).Where(m => m.HasBody && m != helper)
    .SelectMany(m => m.Body.Instructions.Where(i => i.Operand is MethodReference r && r.FullName == setter.FullName).Select(i => (Method: m, Instruction: i))).ToList();
if (sites.Count != 2 || !sites.Any(s => s.Method.Name == "CreateTile") || !sites.Any(s => s.Method.Name.Contains("OnRunningChanged")))
    throw new InvalidOperationException("Unexpected indicator call sites; refusing patch.");
var shortBranches = new Dictionary<OpCode, OpCode> {
    [OpCodes.Br_S] = OpCodes.Br, [OpCodes.Brtrue_S] = OpCodes.Brtrue, [OpCodes.Brfalse_S] = OpCodes.Brfalse,
    [OpCodes.Beq_S] = OpCodes.Beq, [OpCodes.Bge_S] = OpCodes.Bge, [OpCodes.Bgt_S] = OpCodes.Bgt,
    [OpCodes.Ble_S] = OpCodes.Ble, [OpCodes.Blt_S] = OpCodes.Blt, [OpCodes.Bne_Un_S] = OpCodes.Bne_Un,
    [OpCodes.Bge_Un_S] = OpCodes.Bge_Un, [OpCodes.Bgt_Un_S] = OpCodes.Bgt_Un,
    [OpCodes.Ble_Un_S] = OpCodes.Ble_Un, [OpCodes.Blt_Un_S] = OpCodes.Blt_Un, [OpCodes.Leave_S] = OpCodes.Leave
};
foreach (var (method, instruction) in sites) {
    if (method.DeclaringType != window) throw new InvalidOperationException("Unexpected closure type.");
    var processor = method.Body.GetILProcessor();
    // Mutate the old call in place: existing branches must enter the setting guard.
    instruction.OpCode = method.Name == "CreateTile" ? OpCodes.Ldarg_2 : OpCodes.Ldarg_0;
    instruction.Operand = null;
    var tail = instruction;
    if (method.Name != "CreateTile") {
        var load = Instruction.Create(OpCodes.Call, getSettings);
        processor.InsertAfter(tail, load); tail = load;
    }
    var flag = Instruction.Create(OpCodes.Callvirt, getFlag);
    processor.InsertAfter(tail, flag); tail = flag;
    processor.InsertAfter(tail, Instruction.Create(OpCodes.Call, helper));
    foreach (var branch in method.Body.Instructions) if (shortBranches.TryGetValue(branch.OpCode, out var longBranch)) branch.OpCode = longBranch;
}
assembly.Write(output);

using var verified = AssemblyDefinition.ReadAssembly(output);
if (verified.MainModule.Resources.Count != module.Resources.Count) throw new InvalidOperationException("Resource count changed.");
foreach (var resource in module.Resources.OfType<EmbeddedResource>()) {
    var after = verified.MainModule.Resources.OfType<EmbeddedResource>().Single(r => r.Name == resource.Name);
    if (!SHA256.HashData(resource.GetResourceData()).SequenceEqual(SHA256.HashData(after.GetResourceData()))) throw new InvalidOperationException("Embedded resource changed.");
}
var checkedSites = AllTypes(verified.MainModule.Types).SelectMany(t => t.Methods).Where(m => m.HasBody)
    .Where(m => m.Body.Instructions.Any(i => i.Operand is MethodReference r && r.Name == helperName)).ToList();
if (checkedSites.Count != 2 || checkedSites.Any(m => !m.Body.Instructions.Any(i => i.Operand is MethodReference r && r.Name == "get_ShowRunningIndicator")))
    throw new InvalidOperationException("Both indicator paths must read the setting.");
Console.WriteLine(JsonSerializer.Serialize(new { Input = input, Output = output, PatchedPaths = sites.Select(s => s.Method.FullName), EmbeddedResourcesUnchanged = true, StrippedReadyToRun = strippedReadyToRun, OutputSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(output))) }));
