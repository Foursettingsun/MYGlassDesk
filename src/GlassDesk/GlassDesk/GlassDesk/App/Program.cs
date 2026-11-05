using System;
using System.Runtime.CompilerServices;
using GlassDesk.UI;

namespace GlassDesk.App;

internal static class Program
{
	[STAThread]
	private static int Main(string[] args)
	{
		if (BackdropCaptureWorker.RunIfRequested(args, out var exitCode))
		{
			return exitCode;
		}
		return RunDesktop();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static int RunDesktop()
	{
		App app = new App();
		app.InitializeComponent();
		return app.Run();
	}
}
