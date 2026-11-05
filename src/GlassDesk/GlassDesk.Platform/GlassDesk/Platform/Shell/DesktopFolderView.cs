using System;
using System.Runtime.InteropServices;

namespace GlassDesk.Platform.Shell;

internal static class DesktopFolderView
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int QueryView(nint self, out nint view);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int GetFlags(nint self, out uint flags);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int SetFlags(nint self, uint mask, uint flags);

	[ComImport]
	[Guid("6D5140C1-7436-11CE-8034-00AA006009FA")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IServiceProvider
	{
		[PreserveSig]
		int QueryService(ref Guid service, ref Guid iid, out nint result);
	}

	private static readonly Guid BrowserService = new Guid("4C96BE40-915C-11CF-99D3-00AA004AE837");

	private static readonly Guid ShellBrowser = new Guid("000214E2-0000-0000-C000-000000000046");

	private static readonly Guid FolderView2 = new Guid("1AF3A467-214F-4298-908E-06B03E0B39F9");

	internal static bool Read()
	{
		return WithView((nint view) =>
		{
			Check(Slot<GetFlags>(view, 25)(view, out var flags));
			return (flags & 0x1000) == 0;
		});
	}

	internal static void Write(bool visible)
	{
		WithView((nint view) =>
		{
			Check(Slot<SetFlags>(view, 24)(view, 4096u, (!visible) ? 4096u : 0u));
			return true;
		});
	}

	private static T WithView<T>(Func<nint, T> operation)
	{
		dynamic val = null;
		object obj = null;
		nint result = 0;
		nint view = 0;
		nint ppv = 0;
		try
		{
			val = Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("9BA05972-F6A8-11CF-A442-00A0C90A8F39"), throwOnError: true));
			object obj2 = 0;
			object obj3 = 0;
			int num = default;
			obj = val.FindWindowSW(ref obj2, ref obj3, 8, out num, 1);
			if (obj == null)
			{
				throw new InvalidOperationException("Explorer 未提供桌面视图");
			}
			IServiceProvider serviceProvider = (IServiceProvider)obj;
			Guid service = BrowserService;
			Guid iid = ShellBrowser;
			Check(serviceProvider.QueryService(ref service, ref iid, out result));
			Check(Slot<QueryView>(result, 15)(result, out view));
			iid = FolderView2;
			Check(Marshal.QueryInterface(view, in iid, out ppv));
			return operation(ppv);
		}
		finally
		{
			if (ppv != 0)
			{
				Marshal.Release(ppv);
			}
			if (view != 0)
			{
				Marshal.Release(view);
			}
			if (result != 0)
			{
				Marshal.Release(result);
			}
			if (obj != null && Marshal.IsComObject(obj))
			{
				Marshal.ReleaseComObject(obj);
			}
			if ((object)val != null && Marshal.IsComObject((object)val))
			{
				Marshal.ReleaseComObject((object)val);
			}
		}
	}

	private static T Slot<T>(nint instance, int slot) where T : Delegate
	{
		return Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(instance), slot * IntPtr.Size));
	}

	private static void Check(int hr)
	{
		if (hr < 0)
		{
			Marshal.ThrowExceptionForHR(hr);
		}
	}
}
