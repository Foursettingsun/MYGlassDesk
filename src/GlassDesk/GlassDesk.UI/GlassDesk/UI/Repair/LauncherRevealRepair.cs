using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace GlassDesk.UI.Repair;

public static class LauncherRevealRepair
{
	private sealed class Snapshot
	{
		public BitmapSource Bitmap;

		public object Stamp;

		public object Scene;

		public object Bounds;

		public long CapturedAt;

		public bool Restoring;
	}

	private sealed class RevealState
	{
		public Window Window;

		public object Surface;

		public DispatcherTimer Timer;

		public bool Preparing;

		public bool BeginRequested;

		public bool OriginalHitTest;

		public bool OriginalTransparent;

		public bool InitialReady;

		public bool ReadyAtReveal;

		public bool SnapshotAtReveal;

		public bool Degraded;

		public bool LiveObserved;

		public double OriginalOpacity;

		public double Target;

		public long Started;

		public long LastAttempt;

		public long RevealedAt;

		public long LastObservedRender;

		public int Attempts;

		public int ReadyTicks;

		public int VisibleRenderCount;

		public int MissingMaterialRenders;

		public int SnapshotRestores;

		public string Error;

		public void Tick(object sender, EventArgs args)
		{
			Poll(this);
		}

		public void VisibilityChanged(object sender, DependencyPropertyChangedEventArgs args)
		{
			if (Window != null && !Window.IsVisible)
			{
				Cancel(this);
			}
		}

		public void Closed(object sender, EventArgs args)
		{
			Cancel(this);
			if (Window != null)
			{
				Windows.Remove(Window);
			}
		}
	}

	private const BindingFlags Members = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	private static readonly ConditionalWeakTable<Window, RevealState> Windows = new ConditionalWeakTable<Window, RevealState>();

	private static readonly ConditionalWeakTable<object, Snapshot> Snapshots = new ConditionalWeakTable<object, Snapshot>();

	public static void ShowPrepared(Window window)
	{
		object obj = Read(window, "GlassSurface");
		if (obj == null || !IsLauncherSurface(obj) || Read(window, "SurfaceMode")?.ToString() != "OpticalGlass")
		{
			window.Show();
			return;
		}
		RevealState orCreateValue = Windows.GetOrCreateValue(window);
		if (orCreateValue.Window == null)
		{
			orCreateValue.Window = window;
			window.IsVisibleChanged += orCreateValue.VisibilityChanged;
			window.Closed += orCreateValue.Closed;
		}
		orCreateValue.Timer?.Stop();
		orCreateValue.Surface = obj;
		orCreateValue.OriginalOpacity = window.Opacity;
		orCreateValue.OriginalHitTest = window.IsHitTestVisible;
		orCreateValue.OriginalTransparent = Flag(obj, "TransparentWhileLoading");
		orCreateValue.Started = Stopwatch.GetTimestamp();
		orCreateValue.RevealedAt = (orCreateValue.LastAttempt = (orCreateValue.LastObservedRender = 0L));
		orCreateValue.Attempts = (orCreateValue.ReadyTicks = (orCreateValue.VisibleRenderCount = (orCreateValue.MissingMaterialRenders = (orCreateValue.SnapshotRestores = 0))));
		orCreateValue.BeginRequested = (orCreateValue.Degraded = (orCreateValue.LiveObserved = (orCreateValue.ReadyAtReveal = (orCreateValue.SnapshotAtReveal = false))));
		orCreateValue.Error = null;
		orCreateValue.InitialReady = Ready(obj);
		orCreateValue.Preparing = true;
		window.Opacity = 0.0;
		window.IsHitTestVisible = false;
		window.Show();
		RevealState revealState = orCreateValue;
		if (revealState.Timer == null)
		{
			revealState.Timer = new DispatcherTimer(DispatcherPriority.Render, window.Dispatcher);
		}
		orCreateValue.Timer.Tick -= orCreateValue.Tick;
		orCreateValue.Timer.Tick += orCreateValue.Tick;
		orCreateValue.Timer.Interval = TimeSpan.FromMilliseconds(16L);
		orCreateValue.Timer.Start();
	}

	public static void BeginWhenReady(Window window, double timeline)
	{
		if (Windows.TryGetValue(window, out var value) && value.Preparing && timeline > 0.0)
		{
			value.BeginRequested = true;
			value.Target = timeline;
			if (value.InitialReady && Ready(value.Surface))
			{
				Reveal(value, degraded: false);
			}
		}
		else
		{
			Call(window, "BeginContainerTransform", new object[1] { timeline });
		}
	}

	public static void AfterPrepare(object surface)
	{
		if (IsLauncherSurface(surface))
		{
			RememberSnapshot(surface);
			RestoreSnapshot(surface);
		}
	}

	public static void AfterEnsure(object surface)
	{
		if (IsLauncherSurface(surface))
		{
			RestoreSnapshot(surface);
		}
	}

	public static void AfterSleep(object surface, bool sleeping)
	{
		if (sleeping)
		{
			Snapshots.Remove(surface);
		}
	}

	private static void RememberSnapshot(object surface)
	{
		if (Flag(surface, "_compatibilityIsDesktopSnapshot") && Read(surface, "_compatibilityBackdrop") is BitmapSource bitmapSource && (long)bitmapSource.PixelWidth * (long)bitmapSource.PixelHeight <= 2000000)
		{
			Snapshot orCreateValue = Snapshots.GetOrCreateValue(surface);
			if (orCreateValue.Bitmap != bitmapSource)
			{
				orCreateValue.CapturedAt = Stopwatch.GetTimestamp();
			}
			orCreateValue.Bitmap = bitmapSource;
			orCreateValue.Stamp = Read(surface, "_compatibilityStamp");
			orCreateValue.Scene = Read(surface, "_compatibilityScene");
			orCreateValue.Bounds = Read(surface, "_compatibilityBounds");
		}
	}

	private static void RestoreSnapshot(object surface)
	{
		if (!Snapshots.TryGetValue(surface, out var value) || value.Restoring || value.Bitmap == null || Flag(surface, "_performanceSleeping") || Stopwatch.GetElapsedTime(value.CapturedAt).TotalMilliseconds > 1500.0)
		{
			return;
		}
		Window window = ((surface is DependencyObject dependencyObject) ? Window.GetWindow(dependencyObject) : null);
		if (window == null)
		{
			return;
		}
		bool flag = Windows.TryGetValue(window, out var value2) && (value2.Preparing || Flag(window, "_transformActive"));
		object obj = Call(surface, "ReadMaterialStamp", Array.Empty<object>());
		object obj2 = Read(surface, "_backdropScene");
		if (obj == null || !obj.Equals(value.Stamp) || obj2 == null || value.Scene == null)
		{
			return;
		}
		if (!obj2.Equals(value.Scene))
		{
			if (!flag)
			{
				return;
			}
			object obj3 = Call(obj2, "CanRetainMaterialFrom", new object[1] { value.Scene });
			if (!(obj3 is bool) || !(bool)obj3)
			{
				return;
			}
		}
		if (HasLive(surface) || (Read(surface, "_compatibilityBackdrop") != null && Flag(surface, "_compatibilityIsDesktopSnapshot")))
		{
			return;
		}
		value.Restoring = true;
		try
		{
			Write(surface, "_compatibilityBackdrop", value.Bitmap);
			Write(surface, "_compatibilityStamp", obj);
			Write(surface, "_compatibilityBounds", value.Bounds);
			Write(surface, "_compatibilityScene", obj2);
			Write(surface, "_compatibilityIsDesktopSnapshot", true);
			Write(surface, "_handoffStarted", 0L);
			Call(surface, "UpdateTransitionLayer", new object[1]);
			if (value2 != null)
			{
				value2.SnapshotRestores++;
			}
		}
		finally
		{
			value.Restoring = false;
		}
	}

	private static void Poll(RevealState state)
	{
		Window window = state.Window;
		object surface = state.Surface;
		if (!window.IsVisible || window.Dispatcher.HasShutdownStarted)
		{
			Cancel(state);
			return;
		}
		try
		{
			if (!state.Preparing)
			{
				Observe(state);
				if (Stopwatch.GetElapsedTime(state.RevealedAt).TotalMilliseconds >= 1300.0)
				{
					state.Timer.Stop();
					TraceResult(state, "complete");
				}
				return;
			}
			bool flag = Ready(surface);
			if (!flag && state.Attempts < 4 && (state.LastAttempt == 0L || Stopwatch.GetElapsedTime(state.LastAttempt).TotalMilliseconds >= 60.0))
			{
				state.LastAttempt = Stopwatch.GetTimestamp();
				state.Attempts++;
				object value = Read(surface, "IsBackdropActive");
				try
				{
					SetProperty(surface, "IsBackdropActive", new Func<bool>(Inactive));
					Call(window, "PrepareRevealMaterial", Array.Empty<object>());
				}
				finally
				{
					SetProperty(surface, "IsBackdropActive", value);
				}
				Call(surface, "RequestBackdropRefresh", new object[1] { true });
				flag = Ready(surface);
			}
			state.ReadyTicks = (flag ? (state.ReadyTicks + 1) : 0);
			if (state.ReadyTicks >= 2)
			{
				Reveal(state, degraded: false);
			}
			else if (Stopwatch.GetElapsedTime(state.Started).TotalMilliseconds >= 3000.0)
			{
				state.Degraded = true;
				SetProperty(surface, "TransparentWhileLoading", false);
				Call(surface, "PrepareCompatibleMaterial", new object[1] { ((FrameworkElement)surface).RenderSize });
				Call(surface, "UpdateTransitionLayer", new object[1]);
				Reveal(state, degraded: true);
			}
		}
		catch (Exception ex)
		{
			state.Error = ex.GetBaseException().GetType().Name + ": " + ex.GetBaseException().Message;
			state.Degraded = true;
			Reveal(state, degraded: true);
		}
	}

	private static bool Inactive()
	{
		return false;
	}

	private static bool IsLauncherSurface(object surface)
	{
		if (surface is DependencyObject dependencyObject)
		{
			return Window.GetWindow(dependencyObject)?.GetType().FullName == "GlassDesk.Launcher.LauncherWindow";
		}
		return false;
	}

	private static void Reveal(RevealState state, bool degraded)
	{
		object surface = state.Surface;
		Window window = state.Window;
		state.ReadyAtReveal = !degraded && Ready(surface);
		state.SnapshotAtReveal = Flag(surface, "_compatibilityIsDesktopSnapshot");
		Write(surface, "_firstVisibleRenderMaterialReady", null);
		Write(surface, "_firstVisibleMaterialSource", null);
		state.Preparing = false;
		state.RevealedAt = Stopwatch.GetTimestamp();
		if (state.BeginRequested)
		{
			Call(window, "BeginContainerTransform", new object[1] { state.Target });
		}
		window.IsHitTestVisible = state.OriginalHitTest;
		window.Opacity = state.OriginalOpacity;
		TraceResult(state, "presented");
	}

	private static void Observe(RevealState state)
	{
		object surface = state.Surface;
		long num = ((Read(surface, "_lastRenderTicks") is long num2) ? num2 : 0);
		if (num > state.RevealedAt && num != state.LastObservedRender)
		{
			state.LastObservedRender = num;
			state.VisibleRenderCount++;
			if (!Flag(surface, "_lastRenderMaterialReady"))
			{
				state.MissingMaterialRenders++;
			}
		}
		state.LiveObserved |= HasLive(surface);
		if (state.LiveObserved && state.Degraded)
		{
			SetProperty(surface, "TransparentWhileLoading", state.OriginalTransparent);
		}
	}

	private static bool HasLive(object surface)
	{
		object obj = Read(surface, "_backdropScene");
		object obj2 = Read(surface, "_materialStamp");
		if (obj == null || obj2 == null || !obj2.Equals(Call(surface, "ReadMaterialStamp", Array.Empty<object>())))
		{
			return false;
		}
		if (Read(surface, "_frame") == null || !obj.Equals(Read(surface, "_frameScene")))
		{
			if (Read(surface, "_transitionBackdrop") != null)
			{
				return obj.Equals(Read(surface, "_transitionBackdropScene"));
			}
			return false;
		}
		return true;
	}

	private static bool Ready(object surface)
	{
		object obj = Read(surface, "IsCurrentRevealMaterialReady");
		if (obj is bool)
		{
			return (bool)obj;
		}
		return false;
	}

	private static void Cancel(RevealState state)
	{
		state.Timer?.Stop();
		if (state.Window != null)
		{
			if (state.Preparing)
			{
				TraceResult(state, "cancelled");
			}
			state.Preparing = false;
			state.Window.Opacity = state.OriginalOpacity;
			state.Window.IsHitTestVisible = state.OriginalHitTest;
			if (state.Surface != null)
			{
				SetProperty(state.Surface, "TransparentWhileLoading", state.OriginalTransparent);
			}
		}
	}

	private static void TraceResult(RevealState state, string stage)
	{
		string environmentVariable = Environment.GetEnvironmentVariable("GLASSDESK_REVEAL_TRACE_DIR");
		if (string.IsNullOrWhiteSpace(environmentVariable))
		{
			return;
		}
		try
		{
			Dictionary<string, object> value = new Dictionary<string, object>
			{
				["stage"] = stage,
				["processId"] = Environment.ProcessId,
				["requestTicks"] = state.Started,
				["initialReady"] = state.InitialReady,
				["readyAtReveal"] = state.ReadyAtReveal,
				["snapshotAtReveal"] = state.SnapshotAtReveal,
				["degraded"] = state.Degraded,
				["waitMilliseconds"] = ((state.RevealedAt == 0L) ? null : ((object)Stopwatch.GetElapsedTime(state.Started, state.RevealedAt).TotalMilliseconds)),
				["attempts"] = state.Attempts,
				["snapshotRestores"] = state.SnapshotRestores,
				["visibleRenderCount"] = state.VisibleRenderCount,
				["missingMaterialRenders"] = state.MissingMaterialRenders,
				["liveObserved"] = state.LiveObserved,
				["error"] = state.Error,
				["surface"] = ((state.Surface == null) ? null : Call(state.Surface, "InspectPerformance", Array.Empty<object>()))
			};
			Directory.CreateDirectory(environmentVariable);
			File.WriteAllText(Path.Combine(environmentVariable, state.Started + "-" + stage + ".json"), JsonSerializer.Serialize(value));
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
	}

	private static FieldInfo FindField(Type type, string name)
	{
		Type type2 = type;
		while (type2 != null)
		{
			FieldInfo field = type2.GetField(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field != null)
			{
				return field;
			}
			type2 = type2.BaseType;
		}
		return null;
	}

	private static PropertyInfo FindProperty(Type type, string name)
	{
		Type type2 = type;
		while (type2 != null)
		{
			PropertyInfo property = type2.GetProperty(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null)
			{
				return property;
			}
			type2 = type2.BaseType;
		}
		return null;
	}

	private static object Read(object target, string name)
	{
		FieldInfo fieldInfo = FindField(target.GetType(), name);
		if (fieldInfo != null)
		{
			return fieldInfo.GetValue(target);
		}
		PropertyInfo propertyInfo = FindProperty(target.GetType(), name);
		if (propertyInfo != null)
		{
			return propertyInfo.GetValue(target);
		}
		throw new MissingMemberException(target.GetType().FullName, name);
	}

	private static bool Flag(object target, string name)
	{
		object obj = Read(target, name);
		if (obj is bool)
		{
			return (bool)obj;
		}
		return false;
	}

	private static void Write(object target, string name, object value)
	{
		(FindField(target.GetType(), name) ?? throw new MissingFieldException(target.GetType().FullName, name)).SetValue(target, value);
	}

	private static void SetProperty(object target, string name, object value)
	{
		(FindProperty(target.GetType(), name) ?? throw new MissingMemberException(target.GetType().FullName, name)).SetValue(target, value);
	}

	private static object Call(object target, string name, object[] arguments)
	{
		Type type = target.GetType();
		while (type != null)
		{
			MethodInfo[] methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo methodInfo in methods)
			{
				if (methodInfo.Name == name && methodInfo.GetParameters().Length == arguments.Length)
				{
					return methodInfo.Invoke(target, arguments);
				}
			}
			type = type.BaseType;
		}
		throw new MissingMethodException(target.GetType().FullName, name);
	}

	public static bool CancelPendingReveal(Window window)
	{
		if (Windows.TryGetValue(window, out var value) && value.Preparing)
		{
			value.BeginRequested = false;
			Cancel(value);
			window.Hide();
			return true;
		}
		return false;
	}
}
