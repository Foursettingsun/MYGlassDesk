using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.Json;
using GlassDesk.Core.Models;
using GlassDesk.Core.Storage;
using GlassDesk.Core.Serialization;
using GlassDesk.AppServices;

internal static class Program
{
    private static readonly List<string> Passed = new();
    static void Main(string[] args)
    {
        AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(args[0]));
        Run(args[1],args[2]);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Run(string directory,string userLayout)
    {
        Directory.CreateDirectory(directory);
        const int capacity=35;
        var original=Fixture(6);
        var first=LauncherFilterRules.Project(original,null,capacity);
        var split=LauncherFilterRules.PlaceAtSlot(first,"app-5",capacity,capacity,false);
        Counts(split,5,1); Check(LauncherLayoutRules.AppIds(original).Count==6,"source layout remains intact");
        var applied=LauncherFilterRules.ApplyProjection(original,split,null,capacity);
        Check(!LauncherLayoutRules.Equivalent(original,applied),"page-only changes are saved even when order is unchanged");
        Counts(LauncherFilterRules.Project(applied,null,capacity),5,1);
        Passed.Add("drag from a partial page to a newly created page");
        var view=new LauncherViewState(5,7);view.SetPageCounts(new[]{6,0});
        var pager=new LauncherEdgePager(TimeSpan.FromMilliseconds(600));var now=DateTimeOffset.UtcNow;
        Check(!pager.Tick(1,now)&&pager.Tick(1,now.AddMilliseconds(650)),"edge dwell triggers page navigation");
        view.MoveToPage(1);Check(view.Page==1&&view.PageCount==2,"temporary empty page remains navigable");
        view.SetPageCounts(split.Pages.Select(p=>p.Slots.Count));Check(view.Page==1,"drop retains selected page");
        Passed.Add("edge navigation reaches an empty next page before placement");
        var moved=LauncherFilterRules.PlaceAtSlot(split,"app-2",capacity,capacity,false);
        Counts(moved,4,2);Check(LauncherLayoutRules.AppIds(moved).Distinct().Count()==6,"move does not duplicate icons");
        var swapped=LauncherFilterRules.PlaceAtSlot(moved,"app-0",capacity,capacity,true);
        Counts(swapped,4,2);Check(swapped.Pages[1].Slots[0].ItemId=="app-0","cross-page exchange");
        Passed.Add("move and exchange between partially filled pages");
        var single=Fixture(1);var singleSplit=LauncherFilterRules.PlaceAtSlot(LauncherFilterRules.Project(single,null,capacity),"app-0",capacity,capacity,false);
        Counts(LauncherFilterRules.Project(LauncherFilterRules.ApplyProjection(single,singleSplit,null,capacity),null,capacity),0,1);
        Passed.Add("a single icon can be placed on the next page");
        var all=LauncherLayoutRules.AppIds(applied).ToArray();
        var reconciled=LauncherLayoutRules.Reconcile(applied,all.Where(id=>id!="app-1"),capacity);
        Counts(LauncherFilterRules.Project(reconciled,null,capacity),4,1);
        Counts(LauncherFilterRules.Project(applied,null,4),4,1,1);
        Passed.Add("library reconciliation and changed grid capacity retain page boundaries");
        var purged=LauncherLayoutRules.DeepClone(applied);foreach(var page in purged.Pages)page.Slots.RemoveAll(slot=>slot.ItemId=="app-1");
        LauncherFilterRules.Normalize(purged);Counts(LauncherFilterRules.Project(purged,null,capacity),4,1);
        Passed.Add("removing a shortcut does not pull icons back from the following page");
        var clone=LauncherLayoutRules.DeepClone(applied);
        Check(LauncherLayoutRules.Equivalent(applied,clone),"deep clone keeps metadata");
        var field=typeof(LauncherLayout).GetProperty("FlatPageSizes")!;
        ((List<int>)field.GetValue(clone)!)[0]=3;
        Check(((List<int>)field.GetValue(applied)!)[0]==5,"draft metadata is independent");
        Passed.Add("cancelled edits cannot alter the stored page boundaries");
        var grouped=Fixture(6);
        grouped.FilterGroups.Add(new LauncherGroup{Id="work",Name="Work",ItemIds=new List<string>{"app-0","app-2","app-4"}});
        var groupView=LauncherFilterRules.Project(grouped,"work",capacity);
        var groupSplit=LauncherFilterRules.PlaceAtSlot(groupView,"app-4",capacity,capacity,false);
        var groupApplied=LauncherFilterRules.ApplyProjection(grouped,groupSplit,"work",capacity);
        Counts(LauncherFilterRules.Project(groupApplied,"work",capacity),2,1);
        Counts(LauncherFilterRules.Project(groupApplied,null,capacity),6);
        var renamed=LauncherFilterRules.RenameGroup(groupApplied,"work","Renamed");
        Counts(LauncherFilterRules.Project(renamed,"work",capacity),2,1);
        Passed.Add("group pages persist independently of the all-apps view and group rename");
        var withFolder=Fixture(4);
        withFolder.Pages[0].Slots.Add(new LauncherSlot{Kind=LauncherSlotKind.Group,Group=new LauncherGroup{Id="folder",Name="Folder",ItemIds=new List<string>{"nested-1","nested-2"}}});
        var folderView=LauncherFilterRules.Project(withFolder,null,capacity);
        var folderSplit=LauncherFilterRules.PlaceAtSlot(folderView,"nested-2",capacity,capacity,false);
        var folderApplied=LauncherFilterRules.ApplyProjection(withFolder,folderSplit,null,capacity);
        Check(folderApplied.Pages[0].Slots.Exists(s=>s.Group?.Id=="folder"&&s.Group.ItemIds.Count==2),"folder data survives flat-mode page edits");
        Passed.Add("existing folders and their membership remain intact");
        var json=JsonSerializer.Serialize(groupApplied,GlassDeskJsonContext.Default.LauncherLayout);
        Check(json.Contains("\"flatPageSizes\""),"paging metadata is serialized");
        Check(json.Contains("\"kind\": \"App\""),"string enum format is unchanged");
        var roundTrip=JsonSerializer.Deserialize(json,GlassDeskJsonContext.Default.LauncherLayout)!;
        Counts(LauncherFilterRules.Project(roundTrip,"work",capacity),2,1);
        Passed.Add("JSON round trip retains group page sizes and legacy formatting");
        var path=Path.Combine(directory,"launcher.json");var dockPath=Path.Combine(directory,"dock.json");
        File.WriteAllText(path,JsonSerializer.Serialize(original,GlassDeskJsonContext.Default.LauncherLayout));
        var store=new LayoutStore(dockPath,path);store.LoadLauncher();
        Check(store.CommitLauncher(applied,store.LauncherVersion),"commit succeeds");
        var fresh=new LayoutStore(dockPath,path);fresh.LoadLauncher();
        Counts(LauncherFilterRules.Project(fresh.Launcher,null,capacity),5,1);
        Check(!fresh.CommitLauncher(original,fresh.LauncherVersion-1),"stale commit is rejected");
        Passed.Add("real LayoutStore commit and reopen preserve partial pages");
        var workspaceData=new GlassDeskDataStore(new AppPaths(Path.Combine(directory,"workspace")));
        workspaceData.LoadAll();workspaceData.Layouts.CommitLauncher(original,workspaceData.Layouts.LauncherVersion);
        using(var services=new AppServiceHost(workspaceData))
        {
            var workspace=new LauncherWorkspace(services);var edit=workspace.BeginGroupModeEdit(null,capacity);
            edit.Draft=LauncherFilterRules.PlaceAtSlot(edit.Draft,"app-5",capacity,capacity,false);
            Check(workspace.Commit(edit),"workspace commit succeeds");
            Counts(LauncherFilterRules.Project(workspace.Layout,null,capacity),5,1);
        }
        var reopenedData=new GlassDeskDataStore(new AppPaths(Path.Combine(directory,"workspace")));reopenedData.LoadAll();
        Counts(LauncherFilterRules.Project(reopenedData.Layouts.Launcher,null,capacity),5,1);
        Passed.Add("application service drag session and saved workspace reopen use the repaired paging logic");
        var failurePath=Path.Combine(directory,"directory-instead-of-file");Directory.CreateDirectory(failurePath);
        var failing=new LayoutStore(dockPath,failurePath);failing.LoadLauncher();
        bool failed=false;try{failing.CommitLauncher(applied,failing.LauncherVersion);}catch{failed=true;}
        Check(failed,"write failure was exercised");
        Check(field.GetValue(failing.Launcher)==null,"save failure rolls back page metadata");
        Passed.Add("failed saves restore the previous paging metadata");
        var user=JsonSerializer.Deserialize(File.ReadAllText(userLayout),GlassDeskJsonContext.Default.LauncherLayout)!;
        var userProjection=LauncherFilterRules.Project(user,null,capacity);
        var userIds=LauncherLayoutRules.AppIds(userProjection).ToArray();
        int destination=userProjection.Pages.Count*capacity;
        var userDraft=LauncherFilterRules.PlaceAtSlot(userProjection,userIds[userIds.Length-1],destination,capacity,false);
        var userApplied=LauncherFilterRules.ApplyProjection(user,userDraft,null,capacity);
        var userReload=JsonSerializer.Deserialize(JsonSerializer.Serialize(userApplied,GlassDeskJsonContext.Default.LauncherLayout),GlassDeskJsonContext.Default.LauncherLayout)!;
        var userResult=LauncherFilterRules.Project(userReload,null,capacity);
        Check(userResult.Pages.Count==userProjection.Pages.Count+1,"actual user layout can create the next page");
        Check(LauncherLayoutRules.AppIds(userResult).ToHashSet().SetEquals(userIds),"actual user apps remain unchanged");
        Passed.Add("a copied user layout retains every app after creating the next page");
        var report=new{Passed=Passed.Count,Cases=Passed,UserApps=userIds.Length,ResultingUserPageSizes=userResult.Pages.Select(p=>p.Slots.Count).ToArray(),UserDataModified=false};
        var output=JsonSerializer.Serialize(report);File.WriteAllText(Path.Combine(directory,"verification.json"),output);Console.WriteLine(output);
    }
    static LauncherLayout Fixture(int count)
    {
        var result=new LauncherLayout{SchemaVersion=2};var page=new LauncherPage{Id="test-page"};result.Pages.Add(page);
        for(int i=0;i<count;i++){var id="app-"+i;page.Slots.Add(new LauncherSlot{ItemId=id});result.FlatOrder.Add(id);}return result;
    }
    static void Counts(LauncherLayout layout,params int[] counts)
    {
        var actual=layout.Pages.Select(p=>p.Slots.Count).ToArray();Check(actual.SequenceEqual(counts),"page sizes: expected "+string.Join(",",counts)+"; actual "+string.Join(",",actual));
    }
    static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
