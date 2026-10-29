using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
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
        bool create=args[0]=="create";
        string directory=Path.GetFullPath(args[1]);
        var data=JsonSerializer.Deserialize(File.ReadAllText(args[2]),GlassDeskJsonContext.Default.AppLibraryData)!;
        if(create)Directory.CreateDirectory(directory);
        var results=new List<object>();
        foreach(string name in new[]{"ChatGPT","Claude"})
        {
            var entry=data.Items.Single(e=>e.DisplayName==name);
            string identity=entry.AppUserModelId??throw new InvalidOperationException("Application identity is missing.");
            string path=Path.Combine(directory,name+".lnk");
            if(create)
            {
                if(File.Exists(path))throw new IOException("Shortcut already exists: "+path);
                Create(path,name,identity,entry.IconSource);
            }
            string? actual=WindowIdentity.TryGetShortcutAumid(path);
            if(actual!=identity)throw new InvalidOperationException("Shortcut application identity mismatch.");
            var shortcut=ShortcutResolver.Resolve(path)??throw new InvalidOperationException("Shortcut cannot be resolved by GlassDesk.");
            var probeEntry=new AppEntry{Id="shortcut-probe",DisplayName=name,Kind=AppItemKind.Program,Target=path};
            if(WindowMatcher.ResolveAumid(probeEntry)!=identity)throw new InvalidOperationException("GlassDesk does not recognize the shortcut identity.");
            string parsingName=ReadTarget(path);
            if(!parsingName.Contains(identity,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Shortcut points to a different shell application.");
            results.Add(new{App=name,Path=path,Identity=actual,ShellTarget=parsingName,RecognizedByGlassDesk=true});
        }
        Console.WriteLine(JsonSerializer.Serialize(new{Shortcuts=results,ApplicationsLaunched=false}));
    }

    static void Create(string path,string name,string identity,string? icon)
    {
        object instance=new ShellLink();IntPtr item=IntPtr.Zero;
        try
        {
            Marshal.ThrowExceptionForHR(SHParseDisplayName("shell:AppsFolder\\"+identity,IntPtr.Zero,out item,0,out _));
            var link=(IShellLinkW)instance;link.SetIDList(item);link.SetDescription(name);link.SetShowCmd(1);
            if(!string.IsNullOrWhiteSpace(icon)&&File.Exists(icon))link.SetIconLocation(icon,0);
            var key=new PropertyKey{FormatId=new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),PropertyId=5};
            var value=new PropVariant{Type=31,Pointer=Marshal.StringToCoTaskMemUni(identity)};
            try{var store=(IPropertyStore)instance;Marshal.ThrowExceptionForHR(store.SetValue(ref key,ref value));Marshal.ThrowExceptionForHR(store.Commit());}
            finally{Marshal.FreeCoTaskMem(value.Pointer);}
            ((IPersistFile)instance).Save(path,true);
        }
        finally{if(item!=IntPtr.Zero)Marshal.FreeCoTaskMem(item);Marshal.FinalReleaseComObject(instance);}
    }

    static string ReadTarget(string path)
    {
        object instance=new ShellLink();IntPtr item=IntPtr.Zero;IntPtr name=IntPtr.Zero;
        try
        {
            ((IPersistFile)instance).Load(path,0);((IShellLinkW)instance).GetIDList(out item);
            Marshal.ThrowExceptionForHR(SHGetNameFromIDList(item,0x80028000,out name));
            return Marshal.PtrToStringUni(name)??string.Empty;
        }
        finally{if(name!=IntPtr.Zero)Marshal.FreeCoTaskMem(name);if(item!=IntPtr.Zero)Marshal.FreeCoTaskMem(item);Marshal.FinalReleaseComObject(instance);}
    }

    [DllImport("shell32.dll",CharSet=CharSet.Unicode)]static extern int SHParseDisplayName(string name,IntPtr context,out IntPtr item,uint attributes,out uint resultAttributes);
    [DllImport("shell32.dll")]static extern int SHGetNameFromIDList(IntPtr item,uint form,out IntPtr name);
    [ComImport,Guid("00021401-0000-0000-C000-000000000046")]private class ShellLink{}
    [ComImport,Guid("000214F9-0000-0000-C000-000000000046"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]private interface IShellLinkW
    {
        void GetPath([Out,MarshalAs(UnmanagedType.LPWStr)]StringBuilder path,int count,IntPtr data,uint flags);
        void GetIDList(out IntPtr item);void SetIDList(IntPtr item);
        void GetDescription([Out,MarshalAs(UnmanagedType.LPWStr)]StringBuilder text,int count);void SetDescription([MarshalAs(UnmanagedType.LPWStr)]string text);
        void GetWorkingDirectory([Out,MarshalAs(UnmanagedType.LPWStr)]StringBuilder text,int count);void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)]string text);
        void GetArguments([Out,MarshalAs(UnmanagedType.LPWStr)]StringBuilder text,int count);void SetArguments([MarshalAs(UnmanagedType.LPWStr)]string text);
        void GetHotkey(out short key);void SetHotkey(short key);void GetShowCmd(out int command);void SetShowCmd(int command);
        void GetIconLocation([Out,MarshalAs(UnmanagedType.LPWStr)]StringBuilder text,int count,out int index);void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)]string text,int index);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)]string path,uint reserved);void Resolve(IntPtr owner,uint flags);void SetPath([MarshalAs(UnmanagedType.LPWStr)]string path);
    }
    [StructLayout(LayoutKind.Sequential)]private struct PropertyKey{public Guid FormatId;public uint PropertyId;}
    [StructLayout(LayoutKind.Explicit,Size=24)]private struct PropVariant{[FieldOffset(0)]public ushort Type;[FieldOffset(8)]public IntPtr Pointer;}
    [ComImport,Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]private interface IPropertyStore
    {
        [PreserveSig]int GetCount(out uint count);[PreserveSig]int GetAt(uint index,out PropertyKey key);[PreserveSig]int GetValue(ref PropertyKey key,out PropVariant value);
        [PreserveSig]int SetValue(ref PropertyKey key,ref PropVariant value);[PreserveSig]int Commit();
    }
}
