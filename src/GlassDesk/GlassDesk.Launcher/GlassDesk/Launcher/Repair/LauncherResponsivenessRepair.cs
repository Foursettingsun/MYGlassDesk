using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using GlassDesk.Platform.Displays;

namespace GlassDesk.Launcher.Repair;

public static class LauncherResponsivenessRepair
{
	private sealed class WarmState
	{
		public object Host = null;

		public Window Launcher = null;

		public FrameworkElement Button = null;

		public object Dock = null;

		public bool Queued;

		public long LastWarm;

		public bool WindowPrimed;

		public DispatcherTimer RetryTimer;

		public int Retries;

		public void Hover(object sender, MouseEventArgs e)
		{
			Queue();
		}

		public void Press(object sender, MouseButtonEventArgs e)
		{
			Queue();
		}

		public void Navigation(object sender, EventArgs e)
		{
			bool flag = Launcher.IsVisible && !Flag(Launcher, "IsClosingContainer");
			Dock.GetType().GetMethod("SetLauncherContainerState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(Dock, new object[1] { flag });
		}

		public void Queue()
		{
			if (!Queued && !Launcher.IsVisible && !Launcher.Dispatcher.HasShutdownStarted)
			{
				Queued = true;
				Launcher.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(Run));
			}
		}

		public void Run()
		{
			Queued = false;
			if (Launcher.IsVisible || Launcher.Dispatcher.HasShutdownStarted || Flag(Host, "_disposed") || Flag(Host, "_shuttingDown"))
			{
				return;
			}
			object obj = Read(Host, "_performance");
			if (obj == null || Flag(obj, "IsSuppressionRequested") || (LastWarm != 0L && Stopwatch.GetElapsedTime(LastWarm).TotalMilliseconds < 750.0))
			{
				return;
			}
			long timestamp = Stopwatch.GetTimestamp();
			try
			{
				Write(Host, "_launcherHiddenSince", DateTimeOffset.UtcNow);
				Call(Host, "ReconcilePerformance");
				Call(Launcher, "PositionOnWorkingArea");
				Call(Launcher, "PrepareToShow");
				if (!WindowPrimed && Launcher.GetType().FullName == "GlassDesk.Launcher.LauncherWindow")
				{
					PrimeMethods(Launcher.GetType(), new string[5] { "ShowWithTransition", "CloseWithTransition", "BeginContainerTransform", "ApplyContainerTransformFrame", "OnContainerTransformRendering" });
					Type type = Type.GetType("GlassDesk.UI.Repair.LauncherRevealRepair, GlassDesk.UI");
					if (type != null)
					{
						PrimeMethods(type, new string[5] { "ShowPrepared", "BeginWhenReady", "Reveal", "Poll", "Observe" });
					}
					double opacity = Launcher.Opacity;
					bool isHitTestVisible = Launcher.IsHitTestVisible;
					bool showActivated = Launcher.ShowActivated;
					try
					{
						Launcher.Opacity = 0.0;
						Launcher.IsHitTestVisible = false;
						Launcher.ShowActivated = false;
						Launcher.Show();
						Launcher.Hide();
					}
					finally
					{
						if (Launcher.IsVisible)
						{
							Launcher.Hide();
						}
						Launcher.Opacity = opacity;
						Launcher.IsHitTestVisible = isHitTestVisible;
						Launcher.ShowActivated = showActivated;
					}
					WindowPrimed = true;
					Call(Launcher, "PositionOnWorkingArea");
					Call(Launcher, "PrepareToShow");
				}
				Call(Launcher, "PrewarmNextRevealMaterial");
				LastWarm = Stopwatch.GetTimestamp();
				if (Launcher.GetType().FullName == "GlassDesk.Launcher.LauncherWindow")
				{
					RetryTimer?.Stop();
					if (RetryTimer == null)
					{
						RetryTimer = new DispatcherTimer(DispatcherPriority.Background, Launcher.Dispatcher);
					}
					RetryTimer.Tick -= Retry;
					RetryTimer.Tick += Retry;
					RetryTimer.Interval = TimeSpan.FromMilliseconds(60L);
					Retries = 0;
					RetryTimer.Start();
				}
				Trace("prewarm", Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds, null);
			}
			catch (Exception ex)
			{
				Trace("prewarm-error", Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds, ex.GetBaseException().Message);
			}
		}

		private void Retry(object sender, EventArgs e)
		{
			RetryTimer.Stop();
			if (Launcher.IsVisible || Launcher.Dispatcher.HasShutdownStarted || Flag(Host, "_disposed") || Flag(Host, "_shuttingDown"))
			{
				return;
			}
			object obj = Read(Host, "_performance");
			if (obj == null || Flag(obj, "IsSuppressionRequested"))
			{
				return;
			}
			try
			{
				Retries++;
				Call(Launcher, "PrewarmNextRevealMaterial");
				object obj2 = Read(Launcher, "GlassSurface");
				if (obj2 != null && Flag(obj2, "IsCurrentRevealMaterialReady"))
				{
					Trace("prewarm-ready", Stopwatch.GetElapsedTime(LastWarm).TotalMilliseconds, null);
				}
				else if (Retries < 3)
				{
					RetryTimer.Start();
				}
			}
			catch (Exception ex)
			{
				Trace("prewarm-retry-error", 0.0, ex.GetBaseException().Message);
			}
		}
	}

	private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private static readonly ConditionalWeakTable<Window, WarmState> States = new ConditionalWeakTable<Window, WarmState>();

	public static void Install(object host)
	{
		if (Read(host, "_launcher") is Window window)
		{
			object obj = Read(host, "_dock");
			if (obj != null && !States.TryGetValue(window, out var _) && Read(obj, "LauncherButton") is FrameworkElement frameworkElement)
			{
				WarmState warmState = new WarmState
				{
					Host = host,
					Launcher = window,
					Button = frameworkElement,
					Dock = obj
				};
				States.Add(window, warmState);
				frameworkElement.MouseEnter += warmState.Hover;
				frameworkElement.PreviewMouseLeftButtonDown += warmState.Press;
				window.GetType().GetEvent("NavigationAvailabilityChanged", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.AddEventHandler(window, new EventHandler(warmState.Navigation));
			}
		}
	}

	public static void WarmPreparedLauncher(Window launcher)
	{
		if (States.TryGetValue(launcher, out var value))
		{
			value.Run();
		}
	}

	public static bool IsDockAnchor(Point point, PixelRect source, PixelRect host, double dipPerPixel, Thickness chrome, bool hasPath, bool animating, bool hitTestVisible)
	{
		if (!hasPath || (!animating & hitTestVisible) || source.Width <= 0 || source.Height <= 0 || host.Width <= 0 || host.Height <= 0 || !double.IsFinite(dipPerPixel) || dipPerPixel <= 0.0)
		{
			return false;
		}
		return new Rect((double)(source.X - host.X) * dipPerPixel - chrome.Left, (double)(source.Y - host.Y) * dipPerPixel - chrome.Top, (double)source.Width * dipPerPixel, (double)source.Height * dipPerPixel).Contains(point);
	}

	private static void PrimeMethods(Type type, string[] names)
	{
		foreach (string text in names)
		{
			MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo methodInfo in methods)
			{
				if (methodInfo.Name == text && !methodInfo.ContainsGenericParameters)
				{
					RuntimeHelpers.PrepareMethod(methodInfo.MethodHandle);
				}
			}
		}
	}

	private static object Read(object target, string name)
	{
		Type type = target.GetType();
		while (type != null)
		{
			FieldInfo field = type.GetField(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field != null)
			{
				return field.GetValue(target);
			}
			PropertyInfo property = type.GetProperty(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null)
			{
				return property.GetValue(target);
			}
			type = type.BaseType;
		}
		throw new MissingMemberException(target.GetType().FullName, name);
	}

	private static bool Flag(object target, string name)
	{
		object obj = Read(target, name);
		return obj is bool && (bool)obj;
	}

	private static void Write(object target, string name, object value)
	{
		(target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? throw new MissingFieldException(name)).SetValue(target, value);
	}

	private static object Call(object target, string name)
	{
		return (target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) ?? throw new MissingMethodException(name)).Invoke(target, null);
	}

	private static void Trace(string stage, double milliseconds, string error)
	{
		string environmentVariable = Environment.GetEnvironmentVariable("GLASSDESK_INTERACTION_TRACE_DIR");
		if (string.IsNullOrWhiteSpace(environmentVariable))
		{
			return;
		}
		try
		{
			Directory.CreateDirectory(environmentVariable);
			File.WriteAllText(Path.Combine(environmentVariable, Stopwatch.GetTimestamp() + "-" + stage + ".json"), JsonSerializer.Serialize(new Dictionary<string, object>
			{
				["stage"] = stage,
				["milliseconds"] = milliseconds,
				["error"] = error
			}));
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
	}
}
