using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;

if (args.Length != 4 && args.Length != 5)
{
    throw new ArgumentException("Usage: Patcher app-directory placement-helper.dll controls-helper.dll new-output-directory [original-directory]");
}

string root = Path.GetFullPath(args[0]);
string output = Path.GetFullPath(args[3]);
string sourceRoot = args.Length == 5 ? Path.GetFullPath(args[4]) : root;
if (Directory.Exists(output))
{
    throw new IOException("The staging directory must be new.");
}
Directory.CreateDirectory(output);

var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(root);
var reader = new ReaderParameters { InMemory = true, AssemblyResolver = resolver };
using var core = AssemblyDefinition.ReadAssembly(Path.Combine(sourceRoot, "GlassDesk.Core.dll"), reader);
using var launcher = AssemblyDefinition.ReadAssembly(Path.Combine(sourceRoot, "GlassDesk.Launcher.dll"), reader);
using var settings = AssemblyDefinition.ReadAssembly(Path.Combine(sourceRoot, "GlassDesk.Settings.dll"), reader);
using var ui = AssemblyDefinition.ReadAssembly(Path.Combine(sourceRoot, "GlassDesk.UI.dll"), reader);
using var placementHelper = AssemblyDefinition.ReadAssembly(args[1], reader);
using var controlsHelper = AssemblyDefinition.ReadAssembly(args[2], reader);

var changedMethods = new List<MethodDefinition>();
var model = core.MainModule.GetType("GlassDesk.Core.Models.Settings.LauncherSettings");
if (model.Properties.Any(property => property.Name == "OffsetY"))
{
    throw new InvalidOperationException("The vertical offset property already exists.");
}
var backingField = new FieldDefinition("<OffsetY>k__BackingField", FieldAttributes.Private, core.MainModule.TypeSystem.Double);
model.Fields.Add(backingField);
var getter = new MethodDefinition("get_OffsetY", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName, core.MainModule.TypeSystem.Double);
Append(getter, Instruction.Create(OpCodes.Ldarg_0), Instruction.Create(OpCodes.Ldfld, backingField), Instruction.Create(OpCodes.Ret));
var setter = new MethodDefinition("set_OffsetY", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName, core.MainModule.TypeSystem.Void);
setter.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, core.MainModule.TypeSystem.Double));
var coreCalls = All(core.MainModule.Types).SelectMany(type => type.Methods).Where(method => method.HasBody)
    .SelectMany(method => method.Body.Instructions).Select(instruction => instruction.Operand).OfType<MethodReference>().ToArray();
var finite = coreCalls.First(method => method.DeclaringType.FullName == "System.Double" && method.Name == "IsFinite");
var clamp = coreCalls.First(method => method.DeclaringType.FullName == "System.Math" && method.Name == "Clamp" && method.Parameters[0].ParameterType.FullName == "System.Double");
var fallback = Instruction.Create(OpCodes.Ldc_R8, 0.0);
var saveValue = Instruction.Create(OpCodes.Stfld, backingField);
Append(setter,
    Instruction.Create(OpCodes.Ldarg_0), Instruction.Create(OpCodes.Ldarg_1), Instruction.Create(OpCodes.Call, finite), Instruction.Create(OpCodes.Brfalse, fallback),
    Instruction.Create(OpCodes.Ldarg_1), Instruction.Create(OpCodes.Ldc_R8, -10000.0), Instruction.Create(OpCodes.Ldc_R8, 10000.0), Instruction.Create(OpCodes.Call, clamp), Instruction.Create(OpCodes.Br, saveValue),
    fallback, saveValue, Instruction.Create(OpCodes.Ret));
model.Methods.Add(getter);
model.Methods.Add(setter);
model.Properties.Add(new PropertyDefinition("OffsetY", PropertyAttributes.None, core.MainModule.TypeSystem.Double) { GetMethod = getter, SetMethod = setter });

// The root settings fast writer invokes this handler directly. Extend both its
// writer and the nested type's metadata so save, load and cloning all agree.
var jsonContext = core.MainModule.GetType("GlassDesk.Core.Serialization.GlassDeskJsonContext");
var metadata = jsonContext.Methods.Single(method => method.Name == "Create_LauncherSettings");
var getType = coreCalls.First(method => method.Name == "GetTypeFromHandle");
var resolveInfo = core.MainModule.GetType("GlassDesk.Core.Repair.LauncherPagingRepair").Methods.Single(method => method.Name == "ResolveLayoutInfo");
metadata.Body = new MethodBody(metadata);
Append(metadata,
    Instruction.Create(OpCodes.Ldtoken, model), Instruction.Create(OpCodes.Call, getType), Instruction.Create(OpCodes.Ldarg_1),
    Instruction.Create(OpCodes.Call, resolveInfo), Instruction.Create(OpCodes.Castclass, metadata.ReturnType), Instruction.Create(OpCodes.Ret));
var writer = jsonContext.Methods.Single(method => method.Name == "LauncherSettingsSerializeHandler");
var writeEnd = writer.Body.Instructions.Single(instruction => instruction.Operand is MethodReference method && method.Name == "WriteEndObject");
var jsonAssembly = resolver.Resolve(core.MainModule.AssemblyReferences.Single(reference => reference.Name == "System.Text.Json"));
var writeNumber = jsonAssembly.MainModule.GetType("System.Text.Json.Utf8JsonWriter").Methods.Single(method =>
    method.Name == "WriteNumber" && method.Parameters.Count == 2 && method.Parameters[0].ParameterType.FullName == "System.String" && method.Parameters[1].ParameterType.FullName == "System.Double");
InsertBefore(writer, writeEnd.Previous,
    Instruction.Create(OpCodes.Ldarg_1), Instruction.Create(OpCodes.Ldstr, "offsetY"), Instruction.Create(OpCodes.Ldarg_2),
    Instruction.Create(OpCodes.Callvirt, getter), Instruction.Create(OpCodes.Callvirt, core.MainModule.ImportReference(writeNumber)));
changedMethods.Add(writer);

new TypeCopier(placementHelper.MainModule, launcher.MainModule).Copy();
new TypeCopier(controlsHelper.MainModule, settings.MainModule).Copy();
var placement = launcher.MainModule.GetType("GlassDesk.Launcher.Repair.LauncherPlacementRepair");
var controls = settings.MainModule.GetType("GlassDesk.Settings.Repair.LauncherOffsetControls");
var launcherWindow = launcher.MainModule.GetType("GlassDesk.Launcher.LauncherWindow");
var settingsWindow = settings.MainModule.GetType("GlassDesk.Settings.SettingsWindow");

var position = launcherWindow.Methods.Single(method => method.Name == "PositionOnDisplay");
var centered = position.Body.Instructions.Single(instruction => instruction.Operand is MethodReference method && method.DeclaringType.Name == "WindowPlacementPolicy" && method.Name == "Centered");
var launcherSettingsField = launcherWindow.Fields.Single(field => field.Name == "_settings");
InsertAfter(position, centered,
    Instruction.Create(OpCodes.Ldarg_1), Instruction.Create(OpCodes.Ldarg_0), Instruction.Create(OpCodes.Ldfld, launcherSettingsField),
    Instruction.Create(OpCodes.Callvirt, launcher.MainModule.ImportReference(getter)), Instruction.Create(OpCodes.Call, placement.Methods.Single(method => method.Name == "ApplyVerticalOffset")));
int moveCalls = 0;
foreach (var instruction in position.Body.Instructions)
{
    if (instruction.Operand is MethodReference method && method.Name == "MoveWindowToPixelRect")
    {
        instruction.Operand = placement.Methods.Single(candidate => candidate.Name == "MoveWindowIfNeeded");
        moveCalls++;
    }
}
if (moveCalls != 2)
{
    throw new InvalidOperationException("Expected the two existing launcher placement calls.");
}
changedMethods.Add(position);

var apply = launcherWindow.Methods.Single(method => method.Name == "ApplySettings");
apply.Body.InitLocals = true;
var positionOnly = new VariableDefinition(launcher.MainModule.TypeSystem.Boolean);
apply.Body.Variables.Add(positionOnly);
var nullCheck = apply.Body.Instructions.First(instruction => instruction.Operand is MethodReference method && method.Name == "ThrowIfNull");
InsertAfter(apply, nullCheck,
    Instruction.Create(OpCodes.Ldarg_0), Instruction.Create(OpCodes.Ldfld, launcherSettingsField), Instruction.Create(OpCodes.Ldarg_1),
    Instruction.Create(OpCodes.Ldarg_0), Instruction.Create(OpCodes.Ldfld, launcherSettingsField), Instruction.Create(OpCodes.Callvirt, launcher.MainModule.ImportReference(getter)),
    Instruction.Create(OpCodes.Ldarg_1), Instruction.Create(OpCodes.Callvirt, launcher.MainModule.ImportReference(getter)),
    Instruction.Create(OpCodes.Call, placement.Methods.Single(method => method.Name == "IsPositionOnly")), Instruction.Create(OpCodes.Stloc, positionOnly));
var storeSettings = apply.Body.Instructions.Single(instruction => instruction.OpCode == OpCodes.Stfld && instruction.Operand is FieldReference field && field.Name == "_settings");
var fullRefresh = storeSettings.Next;
InsertAfter(apply, storeSettings,
    Instruction.Create(OpCodes.Ldloc, positionOnly), Instruction.Create(OpCodes.Brfalse, fullRefresh), Instruction.Create(OpCodes.Ldarg_0),
    Instruction.Create(OpCodes.Call, launcherWindow.Methods.Single(method => method.Name == "PositionOnWorkingArea")), Instruction.Create(OpCodes.Ret));
changedMethods.Add(apply);

var initialize = settingsWindow.Methods.Single(method => method.Name == ".ctor");
var initializeComponent = initialize.Body.Instructions.Single(instruction => instruction.Operand is MethodReference method && method.Name == "InitializeComponent");
InsertAfter(initialize, initializeComponent, Instruction.Create(OpCodes.Ldarg_0), Instruction.Create(OpCodes.Call, controls.Methods.Single(method => method.Name == "Configure")));
changedMethods.Add(initialize);
var loadControls = settingsWindow.Methods.Single(method => method.Name == "LoadLauncherControls");
foreach (var ret in loadControls.Body.Instructions.Where(instruction => instruction.OpCode == OpCodes.Ret).ToArray())
{
    ret.OpCode = OpCodes.Ldarg_0;
    ret.Operand = null;
    InsertAfter(loadControls, ret, Instruction.Create(OpCodes.Call, controls.Methods.Single(method => method.Name == "Reload")), Instruction.Create(OpCodes.Ret));
}
changedMethods.Add(loadControls);

// Existing grid controls replace LauncherSettings with a new object. Carry the
// offset across that replacement to retain it when another setting is edited.
var changeControls = settingsWindow.Methods.Single(method => method.Name == "OnLauncherControlChanged");
var launcherLocals = changeControls.Body.Variables.Where(variable => variable.VariableType.FullName == model.FullName).ToArray();
if (launcherLocals.Length != 2)
{
    throw new InvalidOperationException("Unexpected launcher settings locals.");
}
var candidateStore = changeControls.Body.Instructions.First(instruction => StoreIndex(instruction) == launcherLocals[1].Index);
InsertAfter(changeControls, candidateStore,
    Instruction.Create(OpCodes.Ldloc, launcherLocals[1]), Instruction.Create(OpCodes.Ldloc, launcherLocals[0]),
    Instruction.Create(OpCodes.Callvirt, settings.MainModule.ImportReference(getter)), Instruction.Create(OpCodes.Callvirt, settings.MainModule.ImportReference(setter)));
changedMethods.Add(changeControls);

// Cache the instance-only callback used by each compositor frame. Dispatcher
// priority, rendering cadence and all readiness conditions retain their logic.
var surface = ui.MainModule.GetType("GlassDesk.UI.Controls.LiquidGlassSurface");
var rendering = surface.Methods.Single(method => method.Name == "OnRenderingFrame");
var actionConstructor = rendering.Body.Instructions.Single(instruction => instruction.OpCode == OpCodes.Newobj && instruction.Operand is MethodReference method && method.DeclaringType.FullName == "System.Action");
var lambdaPointer = actionConstructor.Previous;
var lambdaOwner = lambdaPointer.Previous;
if (lambdaPointer.OpCode != OpCodes.Ldftn || lambdaOwner.OpCode != OpCodes.Ldarg_0 || ((MethodReference)lambdaPointer.Operand).DeclaringType.FullName != surface.FullName)
{
    throw new InvalidOperationException("The render callback unexpectedly captures local state.");
}
var cachedCallback = new FieldDefinition("_cachedRenderFrameAction", FieldAttributes.Private | FieldAttributes.InitOnly, ((MethodReference)actionConstructor.Operand).DeclaringType);
surface.Fields.Add(cachedCallback);
var surfaceConstructor = surface.Methods.Single(method => method.Name == ".ctor");
var surfaceReturn = surfaceConstructor.Body.Instructions.Last(instruction => instruction.OpCode == OpCodes.Ret);
surfaceReturn.OpCode = OpCodes.Ldarg_0;
surfaceReturn.Operand = null;
InsertAfter(surfaceConstructor, surfaceReturn,
    Instruction.Create(OpCodes.Ldarg_0), Instruction.Create(OpCodes.Ldftn, (MethodReference)lambdaPointer.Operand),
    Instruction.Create(OpCodes.Newobj, (MethodReference)actionConstructor.Operand), Instruction.Create(OpCodes.Stfld, cachedCallback), Instruction.Create(OpCodes.Ret));
lambdaPointer.OpCode = OpCodes.Ldfld;
lambdaPointer.Operand = cachedCallback;
actionConstructor.OpCode = OpCodes.Nop;
actionConstructor.Operand = null;
changedMethods.Add(rendering);
changedMethods.Add(surfaceConstructor);

foreach (var method in changedMethods)
{
    WidenBranches(method);
}
foreach (var assembly in new[] { core, launcher, settings, ui })
{
    MakeManaged(assembly, Path.Combine(sourceRoot, assembly.Name.Name + ".dll"));
    foreach (var unused in assembly.MainModule.AssemblyReferences.Where(reference => reference.Name == placementHelper.Name.Name || reference.Name == controlsHelper.Name.Name || reference.Name == assembly.Name.Name).ToArray())
    {
        assembly.MainModule.AssemblyReferences.Remove(unused);
    }
    string path = Path.Combine(output, assembly.Name.Name + ".dll");
    assembly.Write(path);
    using var check = AssemblyDefinition.ReadAssembly(path);
    foreach (var resource in assembly.MainModule.Resources.OfType<EmbeddedResource>())
    {
        var saved = check.MainModule.Resources.OfType<EmbeddedResource>().Single(candidate => candidate.Name == resource.Name);
        if (!SHA256.HashData(resource.GetResourceData()).SequenceEqual(SHA256.HashData(saved.GetResourceData())))
        {
            throw new InvalidOperationException("An embedded resource changed.");
        }
    }
}
Console.WriteLine(JsonSerializer.Serialize(new
{
    Output = output,
    NewSetting = "launcher.offsetY",
    ConditionalMoveCalls = moveCalls,
    RenderCallbacksCached = 1,
    Files = Directory.GetFiles(output).Select(path => new { File = Path.GetFileName(path), Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) })
}));

static IEnumerable<TypeDefinition> All(IEnumerable<TypeDefinition> types)
{
    return types.SelectMany(type => new[] { type }.Concat(All(type.NestedTypes)));
}

static void Append(MethodDefinition method, params Instruction[] instructions)
{
    foreach (var instruction in instructions)
    {
        method.Body.GetILProcessor().Append(instruction);
    }
}

static void InsertBefore(MethodDefinition method, Instruction anchor, params Instruction[] instructions)
{
    foreach (var instruction in instructions)
    {
        method.Body.GetILProcessor().InsertBefore(anchor, instruction);
    }
}

static void InsertAfter(MethodDefinition method, Instruction anchor, params Instruction[] instructions)
{
    foreach (var instruction in instructions)
    {
        method.Body.GetILProcessor().InsertAfter(anchor, instruction);
        anchor = instruction;
    }
}

static int StoreIndex(Instruction instruction)
{
    return instruction.OpCode.Code switch
    {
        Code.Stloc_0 => 0,
        Code.Stloc_1 => 1,
        Code.Stloc_2 => 2,
        Code.Stloc_3 => 3,
        Code.Stloc or Code.Stloc_S => ((VariableDefinition)instruction.Operand).Index,
        _ => -1
    };
}

static void MakeManaged(AssemblyDefinition assembly, string path)
{
    if (assembly.Name.HasPublicKey)
    {
        throw new InvalidOperationException("A signed assembly cannot be modified.");
    }
    if ((assembly.MainModule.Attributes & ModuleAttributes.ILOnly) != 0)
    {
        return;
    }
    using var pe = new PEReader(File.OpenRead(path));
    var native = pe.PEHeaders.CorHeader!.ManagedNativeHeaderDirectory;
    if (native.Size < 4 || pe.GetSectionData(native.RelativeVirtualAddress).GetReader().ReadUInt32() != 0x00525452)
    {
        throw new InvalidOperationException("Expected managed ReadyToRun.");
    }
    assembly.MainModule.Attributes |= ModuleAttributes.ILOnly;
}

static void WidenBranches(MethodDefinition method)
{
    var replacements = new Dictionary<OpCode, OpCode>
    {
        [OpCodes.Br_S] = OpCodes.Br, [OpCodes.Brtrue_S] = OpCodes.Brtrue, [OpCodes.Brfalse_S] = OpCodes.Brfalse,
        [OpCodes.Beq_S] = OpCodes.Beq, [OpCodes.Bne_Un_S] = OpCodes.Bne_Un,
        [OpCodes.Bge_S] = OpCodes.Bge, [OpCodes.Bgt_S] = OpCodes.Bgt, [OpCodes.Ble_S] = OpCodes.Ble, [OpCodes.Blt_S] = OpCodes.Blt,
        [OpCodes.Bge_Un_S] = OpCodes.Bge_Un, [OpCodes.Bgt_Un_S] = OpCodes.Bgt_Un, [OpCodes.Ble_Un_S] = OpCodes.Ble_Un, [OpCodes.Blt_Un_S] = OpCodes.Blt_Un,
        [OpCodes.Leave_S] = OpCodes.Leave
    };
    foreach (var instruction in method.Body.Instructions)
    {
        if (replacements.TryGetValue(instruction.OpCode, out var replacement))
        {
            instruction.OpCode = replacement;
        }
    }
}
