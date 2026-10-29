using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;

if(args.Length!=3)throw new ArgumentException("Usage: Patcher original-core.dll helper.dll new-core.dll");
if(File.Exists(args[2]))throw new InvalidOperationException("Staging output already exists.");
using var assembly=AssemblyDefinition.ReadAssembly(args[0],new ReaderParameters{InMemory=true});
using var helper=AssemblyDefinition.ReadAssembly(args[1]);
var module=assembly.MainModule;
if(assembly.Name.HasPublicKey)throw new InvalidOperationException("Cannot modify a signed assembly.");
if((module.Attributes&ModuleAttributes.ILOnly)==0){
    using var pe=new PEReader(File.OpenRead(args[0]));var native=pe.PEHeaders.CorHeader!.ManagedNativeHeaderDirectory;
    if(native.Size<4||pe.GetSectionData(native.RelativeVirtualAddress).GetReader().ReadUInt32()!=0x00525452)throw new InvalidOperationException("Expected managed ReadyToRun.");
    module.Attributes|=ModuleAttributes.ILOnly;
}
AddSizes(module.GetType("GlassDesk.Core.Models.LauncherLayout"));
AddSizes(module.GetType("GlassDesk.Core.Models.LauncherGroup"));
new TypeCopier(helper.MainModule,module).Copy();
var policy=module.GetType("GlassDesk.Core.Repair.LauncherPagingRepair");
var rules=module.GetType("GlassDesk.Core.Models.LauncherFilterRules");
foreach(var name in new[]{"Project","PlaceAtSlot","ApplyProjection"})Forward(rules.Methods.Single(m=>m.Name==name),policy.Methods.Single(m=>m.Name==name));
var normalize=rules.Methods.Single(m=>m.Name=="Normalize");var normalizeIl=normalize.Body.GetILProcessor();var normalizeEntry=normalize.Body.Instructions[0];
normalizeIl.InsertBefore(normalizeEntry,Instruction.Create(OpCodes.Ldarg_0));normalizeIl.InsertBefore(normalizeEntry,Instruction.Create(OpCodes.Call,policy.Methods.Single(m=>m.Name=="NormalizePaging")));Widen(normalize);
var layoutRules=module.GetType("GlassDesk.Core.Models.LauncherLayoutRules");
AddReturnHook(layoutRules.Methods.Single(m=>m.Name=="DeepClone"),policy.Methods.Single(m=>m.Name=="CopyPaging"),false);
AddReturnHook(layoutRules.Methods.Single(m=>m.Name=="Reconcile"),policy.Methods.Single(m=>m.Name=="CopyAfterReconcile"),true);
var equivalent=layoutRules.Methods.Single(m=>m.Name=="Equivalent");
var eql=equivalent.Body.GetILProcessor();var entry=equivalent.Body.Instructions[0];
foreach(var i in new[]{Instruction.Create(OpCodes.Ldarg_0),Instruction.Create(OpCodes.Ldarg_1),Instruction.Create(OpCodes.Call,policy.Methods.Single(m=>m.Name=="PagingEquivalent")),Instruction.Create(OpCodes.Brtrue,entry),Instruction.Create(OpCodes.Ldc_I4_0),Instruction.Create(OpCodes.Ret)})eql.InsertBefore(entry,i);
Widen(equivalent);
var store=module.GetType("GlassDesk.Core.Storage.LayoutStore");
var copyStore=policy.Methods.Single(m=>m.Name=="CopyStorePaging");
var commit=store.Methods.Single(m=>m.Name=="CommitLauncher");
var commitClone=commit.Body.Variables.Single(v=>v.VariableType.FullName=="GlassDesk.Core.Models.LauncherLayout");
var saveCall=commit.Body.Instructions.Single(i=>i.Operand is MethodReference r&&r.Name=="SaveLauncher");
var originalCall=(MethodReference)saveCall.Operand;saveCall.OpCode=OpCodes.Dup;saveCall.Operand=null;
var commitIl=commit.Body.GetILProcessor();var loadClone=Instruction.Create(OpCodes.Ldloc,commitClone);commitIl.InsertAfter(saveCall,loadClone);
var copyCall=Instruction.Create(OpCodes.Call,copyStore);commitIl.InsertAfter(loadClone,copyCall);commitIl.InsertAfter(copyCall,Instruction.Create(OpCodes.Call,originalCall));Widen(commit);
var save=store.Methods.Single(m=>m.Name=="SaveLauncher");
var rollbackClone=save.Body.Variables.Single(v=>v.VariableType.FullName=="GlassDesk.Core.Models.LauncherLayout");
var rollback=save.Body.Instructions.Single(i=>i.Operand is MethodReference r&&r.Name=="set_FilterGroups");
var saveIl=save.Body.GetILProcessor();var owner=Instruction.Create(OpCodes.Ldarg_0);saveIl.InsertAfter(rollback,owner);
var value=Instruction.Create(OpCodes.Ldloc,rollbackClone);saveIl.InsertAfter(owner,value);saveIl.InsertAfter(value,Instruction.Create(OpCodes.Call,copyStore));Widen(save);
var context=module.GetType("GlassDesk.Core.Serialization.GlassDeskJsonContext");
var typeHandle=context.Methods.Where(m=>m.HasBody).SelectMany(m=>m.Body.Instructions).Select(i=>i.Operand).OfType<MethodReference>().First(r=>r.Name=="GetTypeFromHandle");
foreach(var name in new[]{"Create_LauncherLayout","Create_LauncherGroup","Create_LauncherPage","Create_LauncherSlot","Create_ListLauncherGroup","Create_ListLauncherPage","Create_ListLauncherSlot"}){
    var method=context.Methods.Single(m=>m.Name==name);var type=((GenericInstanceType)method.ReturnType).GenericArguments[0];
    method.Body=new MethodBody(method);var il=method.Body.GetILProcessor();
    il.Append(Instruction.Create(OpCodes.Ldtoken,type));il.Append(Instruction.Create(OpCodes.Call,typeHandle));il.Append(Instruction.Create(OpCodes.Ldarg_1));
    il.Append(Instruction.Create(OpCodes.Call,policy.Methods.Single(m=>m.Name=="ResolveLayoutInfo")));il.Append(Instruction.Create(OpCodes.Castclass,method.ReturnType));il.Append(Instruction.Create(OpCodes.Ret));
}
var resolver=context.Methods.Single(m=>m.Name.EndsWith("IJsonTypeInfoResolver.GetTypeInfo"));
var resolveIl=resolver.Body.GetILProcessor();var oldEntry=resolver.Body.Instructions[0];var fallthrough=Instruction.Create(OpCodes.Pop);
foreach(var i in new[]{Instruction.Create(OpCodes.Ldarg_1),Instruction.Create(OpCodes.Ldarg_2),Instruction.Create(OpCodes.Call,policy.Methods.Single(m=>m.Name=="ResolveExtraInfo")),Instruction.Create(OpCodes.Dup),Instruction.Create(OpCodes.Brfalse,fallthrough),Instruction.Create(OpCodes.Ret),fallthrough})resolveIl.InsertBefore(oldEntry,i);
Widen(resolver);
foreach(var reference in module.AssemblyReferences.Where(r=>r.Name==helper.Name.Name||r.Name==assembly.Name.Name).ToArray())module.AssemblyReferences.Remove(reference);
assembly.Write(args[2]);
using var verified=AssemblyDefinition.ReadAssembly(args[2]);
if(verified.MainModule.GetTypeReferences().Any(t=>t.Scope?.Name==helper.Name.Name||t.Scope?.Name==assembly.Name.Name))throw new InvalidOperationException("Unmerged helper reference.");
foreach(var resource in module.Resources.OfType<EmbeddedResource>()){
    var after=verified.MainModule.Resources.OfType<EmbeddedResource>().Single(r=>r.Name==resource.Name);
    if(!SHA256.HashData(resource.GetResourceData()).SequenceEqual(SHA256.HashData(after.GetResourceData())))throw new InvalidOperationException("Resource changed.");
}
Console.WriteLine(JsonSerializer.Serialize(new{Output=args[2],NewMetadata="flatPageSizes",CoreOnly=true,Sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[2])))}));

void AddSizes(TypeDefinition type){
    if(type.Properties.Any(p=>p.Name=="FlatPageSizes"))throw new InvalidOperationException("Paging metadata already exists.");
    var element=((GenericInstanceType)module.GetType("GlassDesk.Core.Models.LauncherLayout").Properties.Single(p=>p.Name=="FlatOrder").PropertyType).ElementType;
    var list=new GenericInstanceType(element);list.GenericArguments.Add(module.TypeSystem.Int32);
    var field=new FieldDefinition("<FlatPageSizes>k__BackingField",FieldAttributes.Private,list);type.Fields.Add(field);
    var getter=new MethodDefinition("get_FlatPageSizes",MethodAttributes.Public|MethodAttributes.HideBySig|MethodAttributes.SpecialName,list);
    var g=getter.Body.GetILProcessor();g.Append(Instruction.Create(OpCodes.Ldarg_0));g.Append(Instruction.Create(OpCodes.Ldfld,field));g.Append(Instruction.Create(OpCodes.Ret));
    var setter=new MethodDefinition("set_FlatPageSizes",MethodAttributes.Public|MethodAttributes.HideBySig|MethodAttributes.SpecialName,module.TypeSystem.Void);setter.Parameters.Add(new ParameterDefinition("value",ParameterAttributes.None,list));
    var s=setter.Body.GetILProcessor();s.Append(Instruction.Create(OpCodes.Ldarg_0));s.Append(Instruction.Create(OpCodes.Ldarg_1));s.Append(Instruction.Create(OpCodes.Stfld,field));s.Append(Instruction.Create(OpCodes.Ret));
    type.Methods.Add(getter);type.Methods.Add(setter);type.Properties.Add(new PropertyDefinition("FlatPageSizes",PropertyAttributes.None,list){GetMethod=getter,SetMethod=setter});
}
void Forward(MethodDefinition method,MethodDefinition destination){
    method.Body=new MethodBody(method);var il=method.Body.GetILProcessor();foreach(var p in method.Parameters)il.Append(Instruction.Create(OpCodes.Ldarg,p));il.Append(Instruction.Create(OpCodes.Call,destination));il.Append(Instruction.Create(OpCodes.Ret));
}
void AddReturnHook(MethodDefinition method,MethodDefinition hook,bool capacity){
    var il=method.Body.GetILProcessor();foreach(var ret in method.Body.Instructions.Where(i=>i.OpCode==OpCodes.Ret).ToArray()){
        ret.OpCode=OpCodes.Ldarg_0;ret.Operand=null;var tail=ret;
        if(capacity){var arg=Instruction.Create(OpCodes.Ldarg_2);il.InsertAfter(tail,arg);tail=arg;}
        var call=Instruction.Create(OpCodes.Call,hook);il.InsertAfter(tail,call);il.InsertAfter(call,Instruction.Create(OpCodes.Ret));
    }Widen(method);
}
static void Widen(MethodDefinition method){
    var map=new Dictionary<OpCode,OpCode>{[OpCodes.Br_S]=OpCodes.Br,[OpCodes.Brtrue_S]=OpCodes.Brtrue,[OpCodes.Brfalse_S]=OpCodes.Brfalse,[OpCodes.Beq_S]=OpCodes.Beq,[OpCodes.Bge_S]=OpCodes.Bge,[OpCodes.Bgt_S]=OpCodes.Bgt,[OpCodes.Ble_S]=OpCodes.Ble,[OpCodes.Blt_S]=OpCodes.Blt,[OpCodes.Bne_Un_S]=OpCodes.Bne_Un,[OpCodes.Bge_Un_S]=OpCodes.Bge_Un,[OpCodes.Bgt_Un_S]=OpCodes.Bgt_Un,[OpCodes.Ble_Un_S]=OpCodes.Ble_Un,[OpCodes.Blt_Un_S]=OpCodes.Blt_Un,[OpCodes.Leave_S]=OpCodes.Leave};
    foreach(var i in method.Body.Instructions)if(map.TryGetValue(i.OpCode,out var wide))i.OpCode=wide;
}
