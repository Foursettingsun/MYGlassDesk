using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Text.Json;

static class ToggleRegression
{
    sealed class Target { public bool Visible;public bool Closing;public int Shows;public int Closes; }
    public static void Run(string original,string patched)
    {
        using var before=AssemblyDefinition.ReadAssembly(original);
        using var after=AssemblyDefinition.ReadAssembly(patched);
        var old=before.MainModule.GetType("GlassDesk.App.AppHost").Methods.Single(m=>m.Name=="ToggleLauncherFromDock");
        var current=after.MainModule.GetType("GlassDesk.App.AppHost").Methods.Single(m=>m.Name=="ToggleLauncherFromDock");
        var baseline=new Target{Visible=true,Closing=true};Execute(old,baseline);
        if(baseline.Shows!=0||baseline.Closes!=1)throw new Exception("The original dead-click condition was not reproduced.");
        int checks=0;
        foreach(var visible in new[]{false,true})foreach(var closing in new[]{false,true}){
            var target=new Target{Visible=visible,Closing=closing};Execute(current,target);
            bool shouldShow=!visible||closing;
            if(target.Shows!=(shouldShow?1:0)||target.Closes!=(shouldShow?0:1))throw new Exception("Wrong target-state toggle.");checks++;
        }
        Execute(current,null);checks++;
        var rapid=new Target();
        for(int i=0;i<1000;i++){
            Execute(current,rapid);
            if(rapid.Closing!=(i%2==1))throw new Exception("A rapid toggle was ignored.");
        }
        if(rapid.Shows!=500||rapid.Closes!=500)throw new Exception("Rapid actions were lost.");
        Console.WriteLine(JsonSerializer.Serialize(new{OriginalClosingClickIgnored=true,StateCases=checks,RapidActions=1000,Opens=rapid.Shows,Closes=rapid.Closes}));
    }
    // Evaluate the actual patched method's control-flow with only its external
    // window and callback calls substituted; no application windows are opened.
    static void Execute(MethodDefinition method,Target? target)
    {
        var stack=new Stack<object?>();var cursor=method.Body.Instructions[0];int steps=0;
        var show=(Action<int?>)(bounds=>{if(bounds!=42)throw new Exception("Anchor not preserved.");target!.Shows++;target.Visible=true;target.Closing=false;});
        while(cursor!=null){
            if(++steps>200)throw new Exception("Unexpected loop in toggle method.");
            var next=cursor.Next;
            switch(cursor.OpCode.Code){
                case Code.Nop:break;
                case Code.Ldarg_0:stack.Push(target);break;
                case Code.Ldarg_1:stack.Push((int?)42);break;
                case Code.Ldarg_2:stack.Push(show);break;
                case Code.Ldstr:stack.Push(cursor.Operand);break;
                case Code.Br:case Code.Br_S:next=(Instruction)cursor.Operand;break;
                case Code.Brfalse:case Code.Brfalse_S:if(!Truth(stack.Pop()))next=(Instruction)cursor.Operand;break;
                case Code.Brtrue:case Code.Brtrue_S:if(Truth(stack.Pop()))next=(Instruction)cursor.Operand;break;
                case Code.Call:case Code.Callvirt:
                    var call=(MethodReference)cursor.Operand;
                    switch(call.Name){
                        case "ThrowIfNull":stack.Pop();if(stack.Pop()==null)throw new ArgumentNullException();break;
                        case "get_IsVisible":stack.Push(((Target)stack.Pop()!).Visible);break;
                        case "get_IsClosingContainer":stack.Push(((Target)stack.Pop()!).Closing);break;
                        case "CloseWithTransition":var closing=(Target)stack.Pop()!;closing.Closes++;closing.Closing=true;break;
                        case "Invoke":var bounds=(int?)stack.Pop();((Action<int?>)stack.Pop()!)(bounds);break;
                        default:throw new Exception("Unexpected call: "+call.FullName);
                    }break;
                case Code.Ret:return;
                default:throw new Exception("Unexpected instruction: "+cursor);
            }
            cursor=next;
        }
    }
    static bool Truth(object? value)=>value is bool boolean?boolean:value!=null;
}
