using System;
using Expression = System.Linq.Expressions.Expression;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.Json;
using System.Windows;
internal static class Program
{
    static int Collapses;
    static int DesktopActions;
    static void ObserveCollapse(){Collapses++;}
    static void ObserveDesktop(){DesktopActions++;}
    [STAThread]
    static void Main(string[] args)
    {
        var context=new AssemblyLoadContext("dock-collapse-inspection");
        context.Resolving+=(_,name)=>AssemblyLoadContext.Default.LoadFromAssemblyName(name);
        var assembly=context.LoadFromAssemblyPath(System.IO.Path.GetFullPath(args[0]));
        var type=assembly.GetType("GlassDesk.Dock.DockWindow",true)!;
        const BindingFlags privateInstance=BindingFlags.NonPublic|BindingFlags.Instance;
        var collapse=type.GetMethod("CollapseLauncherForBuiltinAction",privateInstance)??throw new MissingMethodException("Collapse helper was not loaded.");
        // Exercise the action state without creating windows or performing system actions.
        var dock=RuntimeHelpers.GetUninitializedObject(type);
        var callback=type.GetField("_toggleLauncher",privateInstance)!;
        var argument=Expression.Parameter(callback.FieldType.GetMethod("Invoke")!.GetParameters()[0].ParameterType);
        var body=Expression.Call(typeof(Program).GetMethod("ObserveCollapse",BindingFlags.Static|BindingFlags.NonPublic)!);
        callback.SetValue(dock,Expression.Lambda(callback.FieldType,body,argument).Compile());
        type.GetProperty("ToggleDesktopIcons")!.SetValue(dock,(Action)ObserveDesktop);
        var setState=type.GetMethod("SetLauncherContainerState")!;
        var desktop=type.GetMethod("OnDesktopIconsButtonClick",privateInstance)!;
        RuntimeHelpers.PrepareMethod(type.GetMethod("OnThisPcButtonClick",privateInstance)!.MethodHandle);
        desktop.Invoke(dock,new object?[]{null,new RoutedEventArgs()});
        if(Collapses!=0||DesktopActions!=1)throw new InvalidOperationException("A hidden launcher should remain hidden.");
        setState.Invoke(dock,new object[]{true});
        desktop.Invoke(dock,new object?[]{null,new RoutedEventArgs()});
        if(Collapses!=1||DesktopActions!=2)throw new InvalidOperationException("Desktop action did not collapse the launcher.");
        setState.Invoke(dock,new object[]{false});
        desktop.Invoke(dock,new object?[]{null,new RoutedEventArgs()});
        if(Collapses!=1||DesktopActions!=3)throw new InvalidOperationException("A subsequent action reopened the launcher.");
        setState.Invoke(dock,new object[]{true});collapse.Invoke(dock,null);
        if(Collapses!=2)throw new InvalidOperationException("Shared collapse helper did not request closing.");
        Console.WriteLine(JsonSerializer.Serialize(new{ActiveLauncherCollapsePassed=true,ClosedLauncherRemainsClosed=true,DesktopActionPreserved=true,ThisPcHandlerJitPassed=true,CollapseCalls=Collapses,DesktopActionCalls=DesktopActions}));
    }
}
