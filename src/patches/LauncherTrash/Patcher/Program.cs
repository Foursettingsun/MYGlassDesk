using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Security.Cryptography;
using System.Text.Json;
if(args.Length!=3)throw new ArgumentException("Usage: Patcher launcher.dll helper.dll new-output.dll");
if(File.Exists(args[2]))throw new InvalidOperationException("Staging output must be new.");
using var assembly=AssemblyDefinition.ReadAssembly(args[0],new ReaderParameters{InMemory=true});
using var helper=AssemblyDefinition.ReadAssembly(args[1]);
var module=assembly.MainModule;
if(assembly.Name.HasPublicKey||(module.Attributes&ModuleAttributes.ILOnly)==0)throw new InvalidOperationException("Expected the existing managed launcher component.");
if(module.GetType("GlassDesk.Launcher.Repair.LauncherTrashRepair")!=null)throw new InvalidOperationException("Trash repair already exists.");
new TypeCopier(helper.MainModule,module).Copy();
var policy=module.GetType("GlassDesk.Launcher.Repair.LauncherTrashRepair");
var launcher=module.GetType("GlassDesk.Launcher.LauncherWindow");
var constructor=launcher.Methods.Single(m=>m.IsConstructor&&!m.IsStatic&&m.Parameters.Count==0);
var initialize=constructor.Body.Instructions.Single(i=>i.Operand is MethodReference m&&m.Name=="InitializeComponent");
var il=constructor.Body.GetILProcessor();var owner=Instruction.Create(OpCodes.Ldarg_0);il.InsertAfter(initialize,owner);
il.InsertAfter(owner,Instruction.Create(OpCodes.Call,policy.Methods.Single(m=>m.Name=="Configure")));
foreach(var reference in module.AssemblyReferences.Where(r=>r.Name==helper.Name.Name||r.Name==assembly.Name.Name).ToArray())module.AssemblyReferences.Remove(reference);
assembly.Write(args[2]);
using var verified=AssemblyDefinition.ReadAssembly(args[2]);
if(verified.MainModule.GetTypeReferences().Any(t=>t.Scope?.Name==helper.Name.Name))throw new InvalidOperationException("Unmerged helper reference.");
foreach(var resource in module.Resources.OfType<EmbeddedResource>()){
    var after=verified.MainModule.Resources.OfType<EmbeddedResource>().Single(r=>r.Name==resource.Name);
    if(!SHA256.HashData(resource.GetResourceData()).SequenceEqual(SHA256.HashData(after.GetResourceData())))throw new InvalidOperationException("Embedded resources changed.");
}
Console.WriteLine(JsonSerializer.Serialize(new{Output=args[2],Width=38,Height=29,IconSize=14,Position="TopLeft",ExistingResourcesPreserved=true,Sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[2])))}));
