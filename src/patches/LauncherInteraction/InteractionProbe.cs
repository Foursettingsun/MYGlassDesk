using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using GlassDesk.Platform.Displays;

internal static class Program
{
    const BindingFlags Any=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    static void Set(object instance,string field,object? value){instance.GetType().GetField(field,Any)!.SetValue(instance,value);}
    static object? Get(object instance,string field)=>instance.GetType().GetField(field,Any)!.GetValue(instance);
    [STAThread]
    static void Main(string[] args)
    {
        string stage=Path.GetFullPath(args[0]);
        var context=new AssemblyLoadContext("launcher-interaction-probe");
        context.Resolving+=(_,name)=>File.Exists(Path.Combine(stage,name.Name+".dll"))?context.LoadFromAssemblyPath(Path.Combine(stage,name.Name+".dll")):AssemblyLoadContext.Default.LoadFromAssemblyName(name);
        var ui=context.LoadFromAssemblyPath(Path.Combine(stage,"GlassDesk.UI.dll"));
        var launcher=context.LoadFromAssemblyPath(Path.Combine(stage,"GlassDesk.Launcher.dll"));
        var host=context.LoadFromAssemblyPath(Path.Combine(stage,"GlassDesk.dll"));
        var policy=launcher.GetType("GlassDesk.Launcher.Repair.LauncherResponsivenessRepair",true)!;
        var reveal=ui.GetType("GlassDesk.UI.Repair.LauncherRevealRepair",true)!;
        int anchorChecks=0;
        var source=new PixelRect(-1800,800,72,72);var hostRect=new PixelRect(-1900,0,1600,1000);
        foreach(double scale in new[]{1.0,96.0/144,0.5}){
            var chrome=new Thickness(7,9,0,0);
            var center=new Point((source.X-hostRect.X+36)*scale-chrome.Left,(source.Y-hostRect.Y+36)*scale-chrome.Top);
            bool Hit(Point p,bool path,bool active,bool input)=>(bool)policy.GetMethod("IsDockAnchor")!.Invoke(null,new object[]{p,source,hostRect,scale,chrome,path,active,input})!;
            Check(Hit(center,true,true,true),"Animation intercepted its source button.");
            Check(Hit(center,true,false,false),"Hidden preparation intercepted its source button.");
            Check(!Hit(center,true,false,true),"Idle panel input was changed.");
            Check(!Hit(center,false,true,true),"A missing transform path reserved input.");
            Check(!Hit(new Point(0,0),true,true,true),"Unrelated panel area became transparent.");anchorChecks+=5;
        }
        var stateType=reveal.GetNestedType("RevealState",BindingFlags.NonPublic)!;
        var table=reveal.GetField("Windows",Any)!.GetValue(null)!;
        var add=table.GetType().GetMethod("Add")!;
        object State(FakeWindow window,bool initial,bool ready){
            var state=Activator.CreateInstance(stateType,true)!;
            Set(state,"Window",window);Set(state,"Surface",new FakeSurface{IsCurrentRevealMaterialReady=ready});
            Set(state,"Preparing",true);Set(state,"InitialReady",initial);Set(state,"OriginalOpacity",1.0);Set(state,"OriginalHitTest",true);
            add.Invoke(table,new object[]{window,state});return state;
        }
        var warm=new FakeWindow();var warmState=State(warm,true,true);
        reveal.GetMethod("BeginWhenReady")!.Invoke(null,new object[]{warm,1.0});
        Check(!(bool)Get(warmState,"Preparing")!&&warm.Begins==1,"A validated warm material still waits for timer ticks.");
        var stale=new FakeWindow();var staleState=State(stale,true,false);
        reveal.GetMethod("BeginWhenReady")!.Invoke(null,new object[]{stale,1.0});
        Check((bool)Get(staleState,"Preparing")!&&stale.Begins==0,"An invalidated material bypassed readiness checks.");
        var cold=new FakeWindow();var coldState=State(cold,false,true);
        reveal.GetMethod("BeginWhenReady")!.Invoke(null,new object[]{cold,1.0});
        Check((bool)Get(coldState,"Preparing")!&&cold.Begins==0,"An unvalidated cold material bypassed the first-frame gate.");
        Check((bool)reveal.GetMethod("CancelPendingReveal")!.Invoke(null,new object[]{cold})!,"Pending open was not cancelled.");
        Check(!(bool)Get(coldState,"Preparing")!&&!(bool)Get(coldState,"BeginRequested")!&&cold.Begins==0,"Cancellation retained a pending open request.");
        Check(!(bool)reveal.GetMethod("CancelPendingReveal")!.Invoke(null,new object[]{warm})!,"A completed open was reported as pending.");
        var install=policy.GetMethod("Install")!;var prewarm=policy.GetMethod("WarmPreparedLauncher")!;
        var fake=new FakeHost();install.Invoke(null,new object[]{fake});prewarm.Invoke(null,new object[]{fake._launcher});
        Check(fake.Reconciles==1&&fake._launcher.Positions==1&&fake._launcher.Preparations==1&&fake._launcher.Warms==1&&fake._launcherHiddenSince.HasValue,"Hidden prewarm did not initialize size, models and material.");
        prewarm.Invoke(null,new object[]{fake._launcher});Check(fake.Reconciles==1,"Repeated input was not coalesced.");
        fake._launcher.NotifyNavigation();Check(fake._dock.StateUpdates==1&&!fake._dock.Expanded,"Dock target-state synchronization failed.");
        var suppressed=new FakeHost();suppressed._performance.IsSuppressionRequested=true;install.Invoke(null,new object[]{suppressed});prewarm.Invoke(null,new object[]{suppressed._launcher});
        Check(suppressed.Reconciles==0,"Prewarm bypassed performance suppression.");
        var disposed=new FakeHost();disposed._disposed=true;install.Invoke(null,new object[]{disposed});prewarm.Invoke(null,new object[]{disposed._launcher});
        Check(disposed.Reconciles==0,"Prewarm ran during disposal.");
        var realWindow=launcher.GetType("GlassDesk.Launcher.LauncherWindow",true)!;
        foreach(string name in new[]{"ContainsPanelGeometryCore","CloseWithTransition"})RuntimeHelpers.PrepareMethod(realWindow.GetMethod(name,Any)!.MethodHandle);
        RuntimeHelpers.PrepareMethod(host.GetType("GlassDesk.App.AppHost",true)!.GetMethod("ToggleLauncherFromDock",Any)!.MethodHandle);
        Console.WriteLine(JsonSerializer.Serialize(new{AnchorCases=anchorChecks,WarmRevealImmediate=true,StaleAndColdMaterialGatePreserved=true,PendingOpenCancellation=true,HiddenPrewarm=true,RepeatedWarmCoalesced=true,DockTargetStateSynchronized=true,PerformanceSuppressionPreserved=true,PatchedMethodsJitPassed=true,WindowsShown=0}));
    }
    public class FakeWindow:Window
    {
        public bool IsClosingContainer=>false;
        public event EventHandler? NavigationAvailabilityChanged;
        public void NotifyNavigation(){NavigationAvailabilityChanged?.Invoke(this,EventArgs.Empty);}
        public int Begins;public int Positions;public int Preparations;public int Warms;
        private void BeginContainerTransform(double target){Check(target==1,"Unexpected animation direction.");Begins++;}
        public void PositionOnWorkingArea(){Positions++;}
        public void PrepareToShow(){Preparations++;}
        public void PrewarmNextRevealMaterial(){Warms++;}
    }
    public class FakeSurface
    {
        public bool IsCurrentRevealMaterialReady{get;set;}
        public bool TransparentWhileLoading{get;set;}
        public bool _compatibilityIsDesktopSnapshot=true;
        public object? _firstVisibleRenderMaterialReady;
        public object? _firstVisibleMaterialSource;
    }
    public class FakePerformance{public bool IsSuppressionRequested{get;set;}}
    public class FakeDock{public FrameworkElement LauncherButton=new Button();public bool Expanded;public int StateUpdates;public void SetLauncherContainerState(bool expanded){Expanded=expanded;StateUpdates++;}}
    public class FakeHost
    {
        public FakeWindow _launcher=new();public FakeDock _dock=new();public FakePerformance _performance=new();
        public bool _disposed;public bool _shuttingDown;public DateTimeOffset? _launcherHiddenSince;public int Reconciles;
        private void ReconcilePerformance(){Reconciles++;}
    }
}
