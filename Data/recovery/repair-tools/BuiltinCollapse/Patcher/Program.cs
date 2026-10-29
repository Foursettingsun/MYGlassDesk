using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Security.Cryptography;
using System.Text.Json;
if(args.Length!=2)throw new ArgumentException("Usage: Patcher original-dock.dll new-output.dll");
if(File.Exists(args[1]))throw new InvalidOperationException("Output must be a new staging file.");
using var assembly=AssemblyDefinition.ReadAssembly(args[0],new ReaderParameters{InMemory=true});
var module=assembly.MainModule;
if(assembly.Name.HasPublicKey||(module.Attributes&ModuleAttributes.ILOnly)==0)throw new InvalidOperationException("Expected the existing unsigned managed Dock component.");
var dock=module.GetType("GlassDesk.Dock.DockWindow");
const string helperName="CollapseLauncherForBuiltinAction";
if(dock.Methods.Any(m=>m.Name==helperName))throw new InvalidOperationException("Collapse repair already exists.");
var active=dock.Fields.Single(f=>f.Name=="_launcherContainerActive");
var toggle=dock.Methods.Single(m=>m.Name=="RequestLauncherToggle");
var helper=new MethodDefinition(helperName,MethodAttributes.Private|MethodAttributes.HideBySig,module.TypeSystem.Void);
dock.Methods.Add(helper);
helper.Body.InitLocals=true;
var emptyBounds=new VariableDefinition(toggle.Parameters.Single().ParameterType);helper.Body.Variables.Add(emptyBounds);
var il=helper.Body.GetILProcessor();var done=Instruction.Create(OpCodes.Ret);
il.Append(Instruction.Create(OpCodes.Ldarg_0));il.Append(Instruction.Create(OpCodes.Ldfld,active));il.Append(Instruction.Create(OpCodes.Brfalse,done));
il.Append(Instruction.Create(OpCodes.Ldarg_0));il.Append(Instruction.Create(OpCodes.Ldloca,emptyBounds));il.Append(Instruction.Create(OpCodes.Initobj,emptyBounds.VariableType));
il.Append(Instruction.Create(OpCodes.Ldloc,emptyBounds));il.Append(Instruction.Create(OpCodes.Call,toggle));il.Append(done);
var handlers=new[]{"OnThisPcButtonClick","OnDesktopIconsButtonClick"};
foreach(var name in handlers){var method=dock.Methods.Single(m=>m.Name==name);var entry=method.Body.Instructions[0];var editor=method.Body.GetILProcessor();editor.InsertBefore(entry,Instruction.Create(OpCodes.Ldarg_0));editor.InsertBefore(entry,Instruction.Create(OpCodes.Call,helper));}
assembly.Write(args[1]);
using var verified=AssemblyDefinition.ReadAssembly(args[1]);
foreach(var name in handlers){var method=verified.MainModule.GetType(dock.FullName).Methods.Single(m=>m.Name==name);if(method.Body.Instructions[1].Operand is not MethodReference call||call.Name!=helperName)throw new InvalidOperationException("Missing action collapse hook.");}
foreach(var resource in module.Resources.OfType<EmbeddedResource>()){var after=verified.MainModule.Resources.OfType<EmbeddedResource>().Single(r=>r.Name==resource.Name);if(!SHA256.HashData(resource.GetResourceData()).SequenceEqual(SHA256.HashData(after.GetResourceData())))throw new InvalidOperationException("Dock resources changed.");}
Console.WriteLine(JsonSerializer.Serialize(new{Output=args[1],PatchedHandlers=handlers,StateGuard="Only collapse an active launcher",OriginalActionsPreserved=true,Sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[1])))}));
