using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;

if (args.Length != 3) throw new ArgumentException("Usage: Patcher app-directory helper.dll new-output-directory");
var root = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[2]);
if (Directory.Exists(output)) throw new InvalidOperationException("Staging directory must be new.");
Directory.CreateDirectory(output);
var resolver = new DefaultAssemblyResolver(); resolver.AddSearchDirectory(root);
using var ui = AssemblyDefinition.ReadAssembly(Path.Combine(root, "GlassDesk.UI.dll"), new ReaderParameters { InMemory = true, AssemblyResolver = resolver });
using var launcher = AssemblyDefinition.ReadAssembly(Path.Combine(root, "GlassDesk.Launcher.dll"), new ReaderParameters { InMemory = true, AssemblyResolver = resolver });
using var bridge = AssemblyDefinition.ReadAssembly(args[1], new ReaderParameters { InMemory = true });
MakeManaged(ui, Path.Combine(root, "GlassDesk.UI.dll"));
MakeManaged(launcher, Path.Combine(root, "GlassDesk.Launcher.dll"));
var target = ui.MainModule;
var copier = new TypeCopier(bridge.MainModule, target);
copier.Copy();
var policy = target.GetType("GlassDesk.UI.Repair.LauncherRevealRepair");
if (policy == null) throw new InvalidOperationException("Repair helper was not imported.");
var surface = target.GetType("GlassDesk.UI.Controls.LiquidGlassSurface");
var hooks = new Dictionary<string, string> { ["PrepareRevealMaterial"] = "AfterPrepare", ["EnsureBackdropScene"] = "AfterEnsure", ["SetPerformanceSleeping"] = "AfterSleep" };
foreach (var (methodName, hookName) in hooks) {
    var method = surface.Methods.Single(m => m.Name == methodName);
    var hook = policy.Methods.Single(m => m.Name == hookName);
    var il = method.Body.GetILProcessor();
    foreach (var ret in method.Body.Instructions.Where(i => i.OpCode == OpCodes.Ret).ToArray()) {
        ret.OpCode = OpCodes.Ldarg_0; ret.Operand = null;
        var tail = ret;
        if (hookName == "AfterSleep") { var argument = Instruction.Create(OpCodes.Ldarg_1); il.InsertAfter(tail, argument); tail = argument; }
        var call = Instruction.Create(OpCodes.Call, hook); il.InsertAfter(tail, call); il.InsertAfter(call, Instruction.Create(OpCodes.Ret));
    }
    Widen(method);
}
var window = launcher.MainModule.GetType("GlassDesk.Launcher.LauncherWindow");
var show = window.Methods.Single(m => m.Name == "ShowWithTransition");
int showSites = 0, beginSites = 0;
foreach (var instruction in show.Body.Instructions) {
    if (instruction.Operand is not MethodReference method) continue;
    if (method.Name == "Show" && method.DeclaringType.FullName == "System.Windows.Window") {
        instruction.OpCode = OpCodes.Call;
        instruction.Operand = launcher.MainModule.ImportReference(policy.Methods.Single(m => m.Name == "ShowPrepared"));
        showSites++;
    } else if (method.Name == "BeginContainerTransform") {
        instruction.OpCode = OpCodes.Call;
        instruction.Operand = launcher.MainModule.ImportReference(policy.Methods.Single(m => m.Name == "BeginWhenReady"));
        beginSites++;
    }
}
if (showSites != 2 || beginSites != 2) throw new InvalidOperationException($"Unexpected show paths: {showSites}, {beginSites}.");
Widen(show);
// Importing a closed generic member can leave an unused source AssemblyRef.
foreach (var unused in target.AssemblyReferences.Where(r => r.Name == bridge.Name.Name).ToArray()) target.AssemblyReferences.Remove(unused);
foreach (var assembly in new[] { ui, launcher }) {
    var path = Path.Combine(output, assembly.Name.Name + ".dll");
    assembly.Write(path);
    using var check = AssemblyDefinition.ReadAssembly(path);
    if (check.MainModule.GetTypeReferences().Any(t => t.Scope?.Name == bridge.Name.Name)) throw new InvalidOperationException("Unmerged helper reference remains.");
    foreach (var resource in assembly.MainModule.Resources.OfType<EmbeddedResource>()) {
        var saved = check.MainModule.Resources.OfType<EmbeddedResource>().Single(r => r.Name == resource.Name);
        if (!SHA256.HashData(resource.GetResourceData()).SequenceEqual(SHA256.HashData(saved.GetResourceData()))) throw new InvalidOperationException("Embedded resource changed.");
    }
}
Console.WriteLine(JsonSerializer.Serialize(new { Output = output, ShowPaths = showSites, AnimationPaths = beginSites, SurfaceHooks = hooks, ExtraRuntimeDependencies = 0,
    Files = Directory.GetFiles(output).Select(p => new { Name = Path.GetFileName(p), Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))) }) }));

static void MakeManaged(AssemblyDefinition assembly, string file) {
    if (assembly.Name.HasPublicKey) throw new InvalidOperationException("Refusing a signed component.");
    if ((assembly.MainModule.Attributes & ModuleAttributes.ILOnly) != 0) return;
    using var pe = new PEReader(File.OpenRead(file));
    var native = pe.PEHeaders.CorHeader!.ManagedNativeHeaderDirectory;
    if (native.Size < 4 || pe.GetSectionData(native.RelativeVirtualAddress).GetReader().ReadUInt32() != 0x00525452) throw new InvalidOperationException("Expected a ReadyToRun managed component.");
    assembly.MainModule.Attributes |= ModuleAttributes.ILOnly;
}

static void Widen(MethodDefinition method) {
    var map = new Dictionary<OpCode, OpCode> { [OpCodes.Br_S]=OpCodes.Br, [OpCodes.Brtrue_S]=OpCodes.Brtrue, [OpCodes.Brfalse_S]=OpCodes.Brfalse, [OpCodes.Beq_S]=OpCodes.Beq,
        [OpCodes.Bge_S]=OpCodes.Bge, [OpCodes.Bgt_S]=OpCodes.Bgt, [OpCodes.Ble_S]=OpCodes.Ble, [OpCodes.Blt_S]=OpCodes.Blt, [OpCodes.Bne_Un_S]=OpCodes.Bne_Un,
        [OpCodes.Bge_Un_S]=OpCodes.Bge_Un, [OpCodes.Bgt_Un_S]=OpCodes.Bgt_Un, [OpCodes.Ble_Un_S]=OpCodes.Ble_Un, [OpCodes.Blt_Un_S]=OpCodes.Blt_Un, [OpCodes.Leave_S]=OpCodes.Leave };
    foreach (var instruction in method.Body.Instructions) if (map.TryGetValue(instruction.OpCode, out var wide)) instruction.OpCode = wide;
}

sealed class TypeCopier(ModuleDefinition source, ModuleDefinition target)
{
    readonly Dictionary<string, TypeDefinition> types = new();
    readonly Dictionary<string, MethodDefinition> methods = new();
    readonly Dictionary<string, FieldDefinition> fields = new();
    IEnumerable<TypeDefinition> All(IEnumerable<TypeDefinition> list) => list.SelectMany(t => new[] { t }.Concat(All(t.NestedTypes)));

    public void Copy() {
        var originals = All(source.Types.Where(t => t.Name != "<Module>")).ToArray();
        foreach (var type in originals) {
            var clone = new TypeDefinition(type.Namespace, type.Name, type.Attributes) { ClassSize=type.ClassSize, PackingSize=type.PackingSize };
            types.Add(type.FullName, clone);
            if (type.DeclaringType == null) target.Types.Add(clone); else types[type.DeclaringType.FullName].NestedTypes.Add(clone);
            if (type.HasGenericParameters) throw new InvalidOperationException("Repair helper declares unexpected generics.");
        }
        foreach (var type in originals) {
            var clone = types[type.FullName]; clone.BaseType = MapType(type.BaseType);
            foreach (var face in type.Interfaces) clone.Interfaces.Add(new InterfaceImplementation(MapType(face.InterfaceType)));
            foreach (var field in type.Fields) {
                var copy = new FieldDefinition(field.Name, field.Attributes, MapType(field.FieldType));
                if (field.HasConstant) copy.Constant=field.Constant;
                clone.Fields.Add(copy); fields.Add(field.FullName, copy);
            }
            foreach (var method in type.Methods) {
                if (method.HasGenericParameters) throw new InvalidOperationException("Repair helper declares unexpected generic methods.");
                var copy = new MethodDefinition(method.Name, method.Attributes, MapType(method.ReturnType)) { ImplAttributes=method.ImplAttributes, CallingConvention=method.CallingConvention };
                foreach (var param in method.Parameters) copy.Parameters.Add(new ParameterDefinition(param.Name, param.Attributes, MapType(param.ParameterType)));
                clone.Methods.Add(copy); methods.Add(method.FullName, copy);
            }
        }
        foreach (var type in originals) foreach (var method in type.Methods) if (method.HasBody) CopyBody(method, methods[method.FullName]);
    }

    TypeReference MapType(TypeReference type) {
        if (type == null) return null!;
        if (types.TryGetValue(type.FullName, out var own)) return own;
        if (type is GenericInstanceType generic) {
            var result = new GenericInstanceType(MapType(generic.ElementType));
            foreach (var argument in generic.GenericArguments) result.GenericArguments.Add(MapType(argument)); return result;
        }
        if (type is ArrayType array) return new ArrayType(MapType(array.ElementType), array.Rank);
        if (type is ByReferenceType reference) return new ByReferenceType(MapType(reference.ElementType));
        if (type is PointerType pointer) return new PointerType(MapType(pointer.ElementType));
        if (type is OptionalModifierType optional) return new OptionalModifierType(MapType(optional.ModifierType), MapType(optional.ElementType));
        if (type is RequiredModifierType required) return new RequiredModifierType(MapType(required.ModifierType), MapType(required.ElementType));
        if (type is GenericParameter) return type;
        return target.ImportReference(type);
    }

    MethodReference MapMethod(MethodReference method) {
        if (methods.TryGetValue(method.FullName, out var own)) return own;
        if (method is GenericInstanceMethod generic) {
            var result = new GenericInstanceMethod(MapMethod(generic.ElementMethod));
            foreach (var argument in generic.GenericArguments) result.GenericArguments.Add(MapType(argument)); return result;
        }
        var imported = target.ImportReference(method);
        imported.DeclaringType=MapType(imported.DeclaringType);
        imported.ReturnType=MapType(imported.ReturnType);
        foreach (var param in imported.Parameters) param.ParameterType=MapType(param.ParameterType);
        return imported;
    }

    FieldReference MapField(FieldReference field) {
        if (fields.TryGetValue(field.FullName, out var own)) return own;
        var imported=target.ImportReference(field);
        imported.DeclaringType=MapType(imported.DeclaringType); imported.FieldType=MapType(imported.FieldType); return imported;
    }

    void CopyBody(MethodDefinition original, MethodDefinition copy) {
        copy.Body = new MethodBody(copy) { InitLocals=original.Body.InitLocals, MaxStackSize=original.Body.MaxStackSize };
        var variables = new Dictionary<VariableDefinition, VariableDefinition>();
        foreach (var variable in original.Body.Variables) { var clone=new VariableDefinition(MapType(variable.VariableType)); copy.Body.Variables.Add(clone); variables.Add(variable,clone); }
        var instructions = new Dictionary<Instruction, Instruction>();
        foreach (var instruction in original.Body.Instructions) { var clone=Instruction.Create(OpCodes.Nop); clone.OpCode=instruction.OpCode; instructions.Add(instruction, clone); copy.Body.Instructions.Add(clone); }
        foreach (var instruction in original.Body.Instructions) {
            instructions[instruction].Operand = instruction.Operand switch {
                null => null, Instruction targetInstruction => instructions[targetInstruction], Instruction[] targets => targets.Select(i=>instructions[i]).ToArray(),
                MethodReference method => MapMethod(method), FieldReference field => MapField(field), TypeReference type => MapType(type),
                VariableDefinition variable => variables[variable], ParameterDefinition param => copy.Parameters[param.Index], _ => instruction.Operand
            };
        }
        foreach (var handler in original.Body.ExceptionHandlers) copy.Body.ExceptionHandlers.Add(new ExceptionHandler(handler.HandlerType) {
            CatchType=handler.CatchType == null ? null : MapType(handler.CatchType),
            TryStart=instructions[handler.TryStart], TryEnd=handler.TryEnd == null ? null : instructions[handler.TryEnd],
            HandlerStart=instructions[handler.HandlerStart], HandlerEnd=handler.HandlerEnd == null ? null : instructions[handler.HandlerEnd],
            FilterStart=handler.FilterStart == null ? null : instructions[handler.FilterStart]
        });
    }
}
