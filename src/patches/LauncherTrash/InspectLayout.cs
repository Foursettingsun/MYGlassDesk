using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using GlassDesk.Launcher;
internal static class Program
{
    private static Type? Policy;
    [STAThread]
    static void Main(string[] args)
    {
        if(args.Length>0)Policy=AssemblyLoadContext.Default.LoadFromAssemblyPath(System.IO.Path.GetFullPath(args[0])).GetType("GlassDesk.Launcher.Repair.LauncherTrashRepair",true);
        Inspect();
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Inspect()
    {
        var application=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
        var launcher=new LauncherWindow();
        var trash=(FrameworkElement)Read(launcher,"TrashDropTarget");
        var root=(Grid)Read(launcher,"LauncherPanelRoot");
        var viewport=(FrameworkElement)Read(launcher,"PageViewport");
        bool fromConstructor=trash.Width==38&&trash.Height==29;
        if(!fromConstructor&&Policy!=null)Policy.GetMethod("Configure")!.Invoke(null,new object[]{launcher});
        trash.Visibility=Visibility.Visible;
        root.Width=1050;root.Height=700;
        launcher.Width=1200;launcher.Height=850;
        launcher.Measure(new Size(1200,850));launcher.Arrange(new Rect(0,0,1200,850));launcher.UpdateLayout();
        root.Measure(new Size(1050,700));root.Arrange(new Rect(0,0,1050,700));root.UpdateLayout();
        var configurations=new List<object>();
        var header=(FrameworkElement)Read(launcher,"SearchHeader");
        foreach(double width in new[]{1050.0,720.0,600.0})
        {
            root.Width=width;trash.Visibility=Visibility.Visible;
            root.Measure(new Size(width,700));root.Arrange(new Rect(0,0,width,700));root.UpdateLayout();
            var t=trash.TranslatePoint(new Point(),root);var v=viewport.TranslatePoint(new Point(),root);var h=header.TranslatePoint(new Point(),root);
            var trashRect=new Rect(t.X,t.Y,trash.ActualWidth,trash.ActualHeight);var viewportRect=new Rect(v.X,v.Y,viewport.ActualWidth,viewport.ActualHeight);var headerRect=new Rect(h.X,h.Y,header.ActualWidth,header.ActualHeight);
            if(trash.Width!=38||trash.Height!=29||trashRect.IntersectsWith(viewportRect)||trashRect.IntersectsWith(headerRect))throw new InvalidOperationException("Trash layout mismatch: root="+width+"; configured="+trash.Width+"x"+trash.Height+"; trash="+trashRect+"; grid="+viewportRect+"; search="+headerRect+"; header-margin="+header.Margin);
            configurations.Add(new{RootWidth=width,TrashX=t.X,TrashY=t.Y,TrashWidth=trash.ActualWidth,TrashHeight=trash.ActualHeight,ViewportY=v.Y,SearchHeaderX=h.X,HeaderMarginDuringDrag=header.Margin.ToString(),AppGridOverlap=false,SearchOverlap=false});
            trash.Visibility=Visibility.Collapsed;
            if(header.Margin.Left!=0)throw new InvalidOperationException("Header did not restore after the drag target was hidden.");
        }
        trash.Visibility=Visibility.Visible;
        root.Measure(new Size(600,700));root.Arrange(new Rect(0,0,600,700));root.UpdateLayout();
        var chain=new List<object>();
        for(DependencyObject? current=trash;current is FrameworkElement element;current=element.Parent)
            chain.Add(new{Name=element.Name,Type=element.GetType().Name,Row=Grid.GetRow(element),RowSpan=Grid.GetRowSpan(element),Column=Grid.GetColumn(element),ColumnSpan=Grid.GetColumnSpan(element),Width=Finite(element.Width),Height=Finite(element.Height),ActualWidth=element.ActualWidth,ActualHeight=element.ActualHeight,Margin=element.Margin.ToString(),Horizontal=element.HorizontalAlignment.ToString(),Vertical=element.VerticalAlignment.ToString()});
        var children=new List<object>();
        if(trash is Border border&&border.Child is FrameworkElement child)Describe(child,children);
        var position=trash.TranslatePoint(new Point(),root);var apps=viewport.TranslatePoint(new Point(),root);
        var searchChain=new List<object>();var search=(FrameworkElement)Read(launcher,"SearchBox");
        for(DependencyObject? current=search;current is FrameworkElement element;current=element.Parent)
            searchChain.Add(new{Name=element.Name,Type=element.GetType().Name,Row=Grid.GetRow(element),Column=Grid.GetColumn(element),Width=Finite(element.Width),Height=Finite(element.Height),ActualWidth=element.ActualWidth,ActualHeight=element.ActualHeight,Margin=element.Margin.ToString()});
        Console.WriteLine(JsonSerializer.Serialize(new{ConfiguredByConstructor=fromConstructor,Configurations=configurations,Chain=chain,Children=children,SearchChain=searchChain,TrashX=position.X,TrashY=position.Y,ViewportX=apps.X,ViewportY=apps.Y,ViewportWidth=viewport.ActualWidth,ViewportHeight=viewport.ActualHeight}));
        launcher.Close();application.Shutdown();
    }
    static double? Finite(double value)=>double.IsFinite(value)?value:null;
    static object Read(object target,string name)=>target.GetType().GetField(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(target)!;
    static void Describe(FrameworkElement element,List<object> result)
    {
        result.Add(new{Type=element.GetType().Name,element.Name,Width=Finite(element.Width),Height=Finite(element.Height),ActualWidth=element.ActualWidth,ActualHeight=element.ActualHeight,Text=(element as TextBlock)?.Text,FontSize=(element as TextBlock)?.FontSize});
        if(element is Panel panel)foreach(UIElement child in panel.Children)if(child is FrameworkElement nested)Describe(nested,result);
        if(element is Border border&&border.Child is FrameworkElement content)Describe(content,result);
    }
}
