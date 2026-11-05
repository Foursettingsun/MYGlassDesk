using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;

if(args.Length==3&&args[0]=="test"){ToggleRegression.Run(args[1],args[2]);return;}
if(args.Length!=3&&args.Length!=4)throw new ArgumentException("Usage: Patcher installed-directory helper.dll new-staging-directory [original-directory]");
var root=Path.GetFullPath(args[0]);var output=Path.GetFullPath(args[2]);
var sourceRoot=args.Length==4?Path.GetFullPath(args[3]):root;
if(Directory.Exists(output))throw new IOException("Staging directory must be new.");
Directory.CreateDirectory(output);
var resolver=new DefaultAssemblyResolver();resolver.AddSearchDirectory(root);
var reader=new ReaderParameters{InMemory=true,AssemblyResolver=resolver};
using var host=AssemblyDefinition.ReadAssembly(Path.Combine(sourceRoot,"GlassDesk.dll"),reader);
using var launcher=AssemblyDefinition.ReadAssembly(Path.Combine(sourceRoot,"GlassDesk.Launcher.dll"),reader);
using var ui=AssemblyDefinition.ReadAssembly(Path.Combine(sourceRoot,"GlassDesk.UI.dll"),reader);
using var helper=AssemblyDefinition.ReadAssembly(args[1],reader);
foreach(var assembly in new[]{host,launcher,ui})MakeManaged(assembly,Path.Combine(sourceRoot,assembly.Name.Name+".dll"));
new TypeCopier(helper.MainModule,launcher.MainModule).Copy();
var policy=launcher.MainModule.GetType("GlassDesk.Launcher.Repair.LauncherResponsivenessRepair");
var window=launcher.MainModule.GetType("GlassDesk.Launcher.LauncherWindow");
var app=host.MainModule.GetType("GlassDesk.App.AppHost");

// A closing window remains IsVisible until the animation ends. Toggle against
// its target state so a second click reverses it rather than calling Close twice.
var toggle=app.Methods.Single(m=>m.Name=="ToggleLauncherFromDock");
if(toggle.Body.Instructions.Any(i=>i.Operand is MethodReference m&&m.Name=="get_IsClosingContainer"))throw new InvalidOperationException("Toggle repair already exists.");
var visible=toggle.Body.Instructions.Single(i=>i.Operand is MethodReference m&&m.Name=="get_IsVisible");
var branch=visible.Next;
if(branch.OpCode!=OpCodes.Brfalse_S&&branch.OpCode!=OpCodes.Brfalse)throw new InvalidOperationException("Unexpected toggle visibility branch.");
var closingGetter=window.Methods.Single(m=>m.Name=="get_IsClosingContainer");
After(toggle,branch,Instruction.Create(OpCodes.Ldarg_0),Instruction.Create(OpCodes.Callvirt,host.MainModule.ImportReference(closingGetter)),Instruction.Create(OpCodes.Brtrue,(Instruction)branch.Operand));

// Connect hover/press intent once, without replacing Dock input handlers.
var create=app.Methods.Single(m=>m.Name=="CreateWindows");
var createIl=create.Body.GetILProcessor();
foreach(var ret in create.Body.Instructions.Where(i=>i.OpCode==OpCodes.Ret).ToArray()){
    ret.OpCode=OpCodes.Ldarg_0;ret.Operand=null;
    After(create,ret,Instruction.Create(OpCodes.Call,host.MainModule.ImportReference(policy.Methods.Single(m=>m.Name=="Install"))),Instruction.Create(OpCodes.Ret));
}
int startup=0;
foreach(var method in app.Methods.Where(m=>m.HasBody))foreach(var instruction in method.Body.Instructions){
    if(instruction.Operand is MethodReference m&&m.Name=="PrewarmNextRevealMaterial"){
        instruction.OpCode=OpCodes.Call;instruction.Operand=host.MainModule.ImportReference(policy.Methods.Single(m=>m.Name=="WarmPreparedLauncher"));startup++;
    }
}
if(startup!=1)throw new InvalidOperationException("Expected exactly one startup prewarm callback.");

// Make the original Dock anchor input-transparent throughout loading/morphing.
var contains=window.Methods.Single(m=>m.Name=="ContainsPanelGeometryCore");
var entry=contains.Body.Instructions[0];var il=contains.Body.GetILProcessor();
var prefix=new List<Instruction>{Instruction.Create(OpCodes.Ldarg_3)};
foreach(var name in new[]{"_transformSourceBounds","_transformHostBounds","_transformDipPerPixel"}){
    prefix.Add(Instruction.Create(OpCodes.Ldarg_0));prefix.Add(Instruction.Create(OpCodes.Ldfld,window.Fields.Single(f=>f.Name==name)));
}
// Resolve WPF members from existing metadata, never the SDK runtime.
var glass=resolver.Resolve(launcher.MainModule.AssemblyReferences.Single(a=>a.Name=="GlassDesk.UI")).MainModule.GetType("GlassDesk.UI.Controls.GlassWindow");
prefix.Add(Instruction.Create(OpCodes.Ldarg_0));prefix.Add(Instruction.Create(OpCodes.Call,launcher.MainModule.ImportReference(glass.Methods.Single(m=>m.Name=="get_ChromeInset"))));
foreach(var name in new[]{"_hasTransformPath","_transformActive"}){
    prefix.Add(Instruction.Create(OpCodes.Ldarg_0));prefix.Add(Instruction.Create(OpCodes.Ldfld,window.Fields.Single(f=>f.Name==name)));
}
var presentation=resolver.Resolve(launcher.MainModule.AssemblyReferences.Single(a=>a.Name=="PresentationCore"));
var uiElement=presentation.MainModule.GetType("System.Windows.UIElement");
prefix.Add(Instruction.Create(OpCodes.Ldarg_0));prefix.Add(Instruction.Create(OpCodes.Call,launcher.MainModule.ImportReference(uiElement.Methods.Single(m=>m.Name=="get_IsHitTestVisible"))));
prefix.Add(Instruction.Create(OpCodes.Call,policy.Methods.Single(m=>m.Name=="IsDockAnchor")));
prefix.Add(Instruction.Create(OpCodes.Brfalse,entry));prefix.Add(Instruction.Create(OpCodes.Ldc_I4_0));prefix.Add(Instruction.Create(OpCodes.Ret));
foreach(var item in prefix)il.InsertBefore(entry,item);

var reveal=ui.MainModule.GetType("GlassDesk.UI.Repair.LauncherRevealRepair");
var state=reveal.NestedTypes.Single(t=>t.Name=="RevealState");
var table=reveal.Fields.Single(f=>f.Name=="Windows");
var begin=reveal.Methods.Single(m=>m.Name=="BeginWhenReady");
var stateVariable=begin.Body.Variables.Single(v=>v.VariableType.FullName==state.FullName);
var targetStore=begin.Body.Instructions.Single(i=>i.OpCode==OpCodes.Stfld&&i.Operand is FieldReference f&&f.Name=="Target");
var ready=reveal.Methods.Single(m=>m.Name=="Ready");
var present=reveal.Methods.Single(m=>m.Name=="Reveal");
var end=targetStore.Next;
if(end.OpCode!=OpCodes.Ret)throw new InvalidOperationException("Unexpected preparation state exit.");
After(begin,targetStore,
    Instruction.Create(OpCodes.Ldloc,stateVariable),Instruction.Create(OpCodes.Ldfld,state.Fields.Single(f=>f.Name=="InitialReady")),Instruction.Create(OpCodes.Brfalse,end),
    Instruction.Create(OpCodes.Ldloc,stateVariable),Instruction.Create(OpCodes.Ldfld,state.Fields.Single(f=>f.Name=="Surface")),Instruction.Create(OpCodes.Call,ready),Instruction.Create(OpCodes.Brfalse,end),
    Instruction.Create(OpCodes.Ldloc,stateVariable),Instruction.Create(OpCodes.Ldc_I4_0),Instruction.Create(OpCodes.Call,present));

// A click during hidden material preparation must cancel that pending open;
// otherwise its render timer can resurrect the launcher after a close request.
var cancel=new MethodDefinition("CancelPendingReveal",MethodAttributes.Public|MethodAttributes.Static|MethodAttributes.HideBySig,ui.MainModule.TypeSystem.Boolean);
cancel.Parameters.Add(new ParameterDefinition("window",ParameterAttributes.None,begin.Parameters[0].ParameterType));
cancel.Body.InitLocals=true;var saved=new VariableDefinition(state);cancel.Body.Variables.Add(saved);
var tryGet=(MethodReference)begin.Body.Instructions.Single(i=>i.Operand is MethodReference m&&m.Name=="TryGetValue").Operand;
var cancelIl=cancel.Body.GetILProcessor();var no=Instruction.Create(OpCodes.Ldc_I4_0);
cancelIl.Append(Instruction.Create(OpCodes.Ldsfld,table));cancelIl.Append(Instruction.Create(OpCodes.Ldarg_0));cancelIl.Append(Instruction.Create(OpCodes.Ldloca,saved));cancelIl.Append(Instruction.Create(OpCodes.Callvirt,tryGet));cancelIl.Append(Instruction.Create(OpCodes.Brfalse,no));
cancelIl.Append(Instruction.Create(OpCodes.Ldloc,saved));cancelIl.Append(Instruction.Create(OpCodes.Ldfld,state.Fields.Single(f=>f.Name=="Preparing")));cancelIl.Append(Instruction.Create(OpCodes.Brfalse,no));
cancelIl.Append(Instruction.Create(OpCodes.Ldloc,saved));cancelIl.Append(Instruction.Create(OpCodes.Ldc_I4_0));cancelIl.Append(Instruction.Create(OpCodes.Stfld,state.Fields.Single(f=>f.Name=="BeginRequested")));
cancelIl.Append(Instruction.Create(OpCodes.Ldloc,saved));cancelIl.Append(Instruction.Create(OpCodes.Call,reveal.Methods.Single(m=>m.Name=="Cancel")));
var windowType=resolver.Resolve(ui.MainModule.AssemblyReferences.Single(a=>a.Name=="PresentationFramework")).MainModule.GetType("System.Windows.Window");
cancelIl.Append(Instruction.Create(OpCodes.Ldarg_0));cancelIl.Append(Instruction.Create(OpCodes.Callvirt,ui.MainModule.ImportReference(windowType.Methods.Single(m=>m.Name=="Hide"))));
cancelIl.Append(Instruction.Create(OpCodes.Ldc_I4_1));cancelIl.Append(Instruction.Create(OpCodes.Ret));cancelIl.Append(no);cancelIl.Append(Instruction.Create(OpCodes.Ret));reveal.Methods.Add(cancel);
var close=window.Methods.Single(m=>m.Name=="CloseWithTransition");var closeEntry=close.Body.Instructions[0];var closeIl=close.Body.GetILProcessor();
foreach(var instruction in new[]{Instruction.Create(OpCodes.Ldarg_0),Instruction.Create(OpCodes.Call,launcher.MainModule.ImportReference(cancel)),Instruction.Create(OpCodes.Brfalse,closeEntry),Instruction.Create(OpCodes.Ldarg_0),Instruction.Create(OpCodes.Call,window.Methods.Single(m=>m.Name=="AbortContainerTransform")),Instruction.Create(OpCodes.Ret)})closeIl.InsertBefore(closeEntry,instruction);

foreach(var module in new[]{host.MainModule,launcher.MainModule,ui.MainModule}){
    foreach(var type in All(module.Types))foreach(var method in type.Methods.Where(m=>m.HasBody))Widen(method);
    foreach(var unused in module.AssemblyReferences.Where(r=>r.Name==helper.Name.Name).ToArray())module.AssemblyReferences.Remove(unused);
    var path=Path.Combine(output,module.Assembly.Name.Name+".dll");module.Assembly.Write(path);
    using var check=AssemblyDefinition.ReadAssembly(path);
    foreach(var resource in module.Resources.OfType<EmbeddedResource>()){
        var after=check.MainModule.Resources.OfType<EmbeddedResource>().Single(r=>r.Name==resource.Name);
        if(!SHA256.HashData(resource.GetResourceData()).SequenceEqual(SHA256.HashData(after.GetResourceData())))throw new InvalidOperationException("An embedded resource changed.");
    }
    if(check.MainModule.GetTypeReferences().Any(t=>t.Scope?.Name==helper.Name.Name))throw new InvalidOperationException("Unmerged helper dependency.");
}
Console.WriteLine(JsonSerializer.Serialize(new{Output=output,StartupWarmCallbacks=startup,Files=Directory.GetFiles(output).Select(p=>new{File=Path.GetFileName(p),Sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p)))})}));

static IEnumerable<TypeDefinition> All(IEnumerable<TypeDefinition> types)=>types.SelectMany(t=>new[]{t}.Concat(All(t.NestedTypes)));
static void After(MethodDefinition method,Instruction anchor,params Instruction[] items){var il=method.Body.GetILProcessor();foreach(var item in items){il.InsertAfter(anchor,item);anchor=item;}}
static void MakeManaged(AssemblyDefinition assembly,string path){
    if(assembly.Name.HasPublicKey)throw new InvalidOperationException("Signed assembly cannot be modified.");
    if((assembly.MainModule.Attributes&ModuleAttributes.ILOnly)!=0)return;
    using var pe=new PEReader(File.OpenRead(path));var native=pe.PEHeaders.CorHeader!.ManagedNativeHeaderDirectory;
    if(native.Size<4||pe.GetSectionData(native.RelativeVirtualAddress).GetReader().ReadUInt32()!=0x00525452)throw new InvalidOperationException("Expected managed ReadyToRun.");
    assembly.MainModule.Attributes|=ModuleAttributes.ILOnly;
}
static void Widen(MethodDefinition method){
    var wide=new Dictionary<OpCode,OpCode>{{OpCodes.Br_S,OpCodes.Br},{OpCodes.Brtrue_S,OpCodes.Brtrue},{OpCodes.Brfalse_S,OpCodes.Brfalse},{OpCodes.Beq_S,OpCodes.Beq},{OpCodes.Bne_Un_S,OpCodes.Bne_Un},{OpCodes.Bge_S,OpCodes.Bge},{OpCodes.Bgt_S,OpCodes.Bgt},{OpCodes.Ble_S,OpCodes.Ble},{OpCodes.Blt_S,OpCodes.Blt},{OpCodes.Bge_Un_S,OpCodes.Bge_Un},{OpCodes.Bgt_Un_S,OpCodes.Bgt_Un},{OpCodes.Ble_Un_S,OpCodes.Ble_Un},{OpCodes.Blt_Un_S,OpCodes.Blt_Un},{OpCodes.Leave_S,OpCodes.Leave}};
    foreach(var instruction in method.Body.Instructions)if(wide.TryGetValue(instruction.OpCode,out var op))instruction.OpCode=op;
}
