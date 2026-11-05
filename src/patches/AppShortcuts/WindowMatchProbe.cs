using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GlassDesk.Core.Models;
using GlassDesk.Core.Serialization;
using GlassDesk.AppServices;
using GlassDesk.Platform.Shell;
internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var library=JsonSerializer.Deserialize(File.ReadAllText(args[0]),GlassDeskJsonContext.Default.AppLibraryData)!;
        var windows=WindowLocator.EnumerateTopLevelWindows();
        var results=new List<object>();
        foreach(var entry in library.Items)
        {
            string? expected=entry.DisplayName=="ChatGPT"?"OpenAI.Codex_2p2nqsd0c76g0!App":entry.DisplayName=="Claude"?"Claude_pzs8sxrjxfjjc!Claude":null;
            if(expected==null)continue;
            var before=WindowMatcher.MatchWindows(entry,windows,WindowMatcher.ResolveAumid(entry));
            var previous=entry.AppUserModelId;
            entry.AppUserModelId=expected;
            var after=WindowMatcher.MatchWindows(entry,windows,WindowMatcher.ResolveAumid(entry));
            results.Add(new { entry.DisplayName,PreviousIdentity=previous,ExpectedIdentity=expected,
                Before=before.Select(w=>new{w.ProcessId,Image=Path.GetFileName(w.ImagePath),Identity=w.EffectiveAumid}).ToArray(),
                After=after.Select(w=>new{w.ProcessId,Image=Path.GetFileName(w.ImagePath),Identity=w.EffectiveAumid}).ToArray() });
        }
        var relevant=windows.Where(w=>Path.GetFileName(w.ImagePath)?.Equals("ChatGPT.exe",StringComparison.OrdinalIgnoreCase)==true||Path.GetFileName(w.ImagePath)?.Equals("Claude.exe",StringComparison.OrdinalIgnoreCase)==true)
            .Select(w=>new{w.ProcessId,Image=Path.GetFileName(w.ImagePath),Identity=w.EffectiveAumid}).ToArray();
        Console.WriteLine(JsonSerializer.Serialize(new{Matches=results,ApplicationWindows=relevant}));
    }
}
