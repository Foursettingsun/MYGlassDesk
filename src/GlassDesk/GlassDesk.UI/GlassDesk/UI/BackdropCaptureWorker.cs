using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GlassDesk.Platform;

namespace GlassDesk.UI;

public static class BackdropCaptureWorker
{
	private struct StartupMessage
	{
		public nint Window;

		public uint Message;

		public nuint WParam;

		public nint LParam;

		public uint Time;

		public int X;

		public int Y;

		public uint Private;
	}

	internal const int ProtocolMagic = 1195655987;

	internal const int ProtocolVersion = 5;

	public static bool RunIfRequested(string[] arguments, out int exitCode)
	{
		exitCode = 0;
		if (arguments.Length != 2 || arguments[0] != "--glass-backdrop-worker")
		{
			return false;
		}
		if (!int.TryParse(arguments[1], out var result) || result <= 0)
		{
			exitCode = 2;
			return true;
		}
		CompleteStartupFeedback();
		using BinaryReader binaryReader = new BinaryReader(Console.OpenStandardInput(), Encoding.UTF8);
		using BinaryWriter binaryWriter = new BinaryWriter(Console.OpenStandardOutput(), Encoding.UTF8);
		try
		{
			Process parent = Process.GetProcessById(result);
			try
			{
				CancellationTokenSource lifetime = new CancellationTokenSource();
				try
				{
					Task.Run(async () =>
					{
						while (!lifetime.IsCancellationRequested)
						{
							if (parent.HasExited)
							{
								Environment.Exit(0);
							}
							try
							{
								await Task.Delay(500, lifetime.Token);
							}
							catch (OperationCanceledException)
							{
								break;
							}
						}
					});
					SetThreadDpiAwarenessContext(-4);
					if (binaryReader.ReadInt32() != 1195655987 || binaryReader.ReadInt32() != 5)
					{
						throw new InvalidDataException("Invalid capture protocol.");
					}
					byte b = binaryReader.ReadByte();
					if (b > 4)
					{
						throw new InvalidDataException("Invalid capture provider.");
					}
					ScreenRect region = new ScreenRect(binaryReader.ReadInt32(), binaryReader.ReadInt32(), binaryReader.ReadInt32(), binaryReader.ReadInt32());
					if (region.Width <= 0 || region.Height <= 0 || (long)region.Width * (long)region.Height > 32000000)
					{
						throw new InvalidDataException("Invalid capture extent.");
					}
					using SharedCapturePixels sharedCapturePixels = SharedCapturePixels.Open(binaryReader.ReadString(), binaryReader.ReadInt32());
					using NativeFrameResize nativeFrameResize = new NativeFrameResize();
					byte[] array = null;
					CapturePixels capturePixels = null;
					long num = 0L;
					int[] excluded = ReadFamily(binaryReader, result);
					NativeCaptureDiagnostics nativeCaptureDiagnostics = new NativeCaptureDiagnostics();
					IDesktopSceneCapture desktopSceneCapture;
					switch (b)
					{
					case 0:
						desktopSceneCapture = new SharedDesktopSceneCapture(region, diagnosticOnScreen: false, nativeCaptureDiagnostics.Observe, result, () => excluded);
						break;
					case 1:
						desktopSceneCapture = new DocumentedDesktopSceneCapture(region, diagnosticOnScreen: false, nativeCaptureDiagnostics.Observe, () => excluded);
						break;
					case 3:
						if (WindowsCapabilities.BuildNumber >= 22000)
						{
							desktopSceneCapture = new SharedDesktopSceneCapture(region, diagnosticOnScreen: false, nativeCaptureDiagnostics.Observe, result, () => excluded, desktopOnly: true);
							break;
						}
						goto case 4;
					case 4:
						desktopSceneCapture = new DocumentedDesktopSceneCapture(region, diagnosticOnScreen: false, nativeCaptureDiagnostics.Observe, () => excluded, desktopOnly: true);
						break;
					default:
						desktopSceneCapture = new WallpaperSceneCapture(region);
						break;
					}
					using IDesktopSceneCapture desktopSceneCapture2 = desktopSceneCapture;
					binaryWriter.Write((byte)1);
					binaryWriter.Write(5);
					binaryWriter.Write(((IntPtr)desktopSceneCapture2.CarrierHandle).ToInt64());
					binaryWriter.Flush();
					while (!parent.HasExited)
					{
						byte b2 = binaryReader.ReadByte();
						if (b2 == 0)
						{
							break;
						}
						long num2 = binaryReader.ReadInt64();
						long num3 = binaryReader.ReadInt64();
						if (num2 <= 0 || num3 <= 0)
						{
							throw new InvalidDataException("Invalid capture request identity.");
						}
						excluded = ReadFamily(binaryReader, result);
						bool flag;
						switch (b2)
						{
						case 5:
						{
							byte[] array2 = JsonSerializer.SerializeToUtf8Bytes(nativeCaptureDiagnostics.Drain());
							if (array2.Length > 65536)
							{
								throw new InvalidDataException("Native trace envelope exceeds its budget.");
							}
							binaryWriter.Write((byte)0);
							binaryWriter.Write(num2);
							binaryWriter.Write(num3);
							binaryWriter.Write(desktopSceneCapture2.SceneRevision);
							binaryWriter.Write(array2.Length);
							binaryWriter.Write(array2);
							binaryWriter.Flush();
							continue;
						}
						case 3:
						{
							bool flag2 = desktopSceneCapture2.UpdateExclusions();
							if (flag2)
							{
								capturePixels = null;
							}
							binaryWriter.Write(flag2 ? ((byte)1) : ((byte)0));
							binaryWriter.Write(num2);
							binaryWriter.Write(num3);
							binaryWriter.Write(desktopSceneCapture2.SceneRevision);
							binaryWriter.Flush();
							continue;
						}
						case 2:
							desktopSceneCapture2.ReleaseReadbackBuffers();
							nativeFrameResize.Dispose();
							array = null;
							capturePixels = null;
							binaryWriter.Write((byte)0);
							binaryWriter.Write(num2);
							binaryWriter.Write(num3);
							binaryWriter.Write(desktopSceneCapture2.SceneRevision);
							binaryWriter.Flush();
							continue;
						case 1:
						case 4:
							flag = true;
							break;
						default:
							flag = false;
							break;
						}
						if (!flag)
						{
							throw new InvalidDataException("Unknown capture command.");
						}
						long num4 = binaryReader.ReadInt64();
						int num5 = Math.Clamp(binaryReader.ReadInt32(), 0, 100);
						PixelBounds crop = new PixelBounds(binaryReader.ReadInt32(), binaryReader.ReadInt32(), binaryReader.ReadInt32(), binaryReader.ReadInt32());
						int num6 = binaryReader.ReadInt32();
						int num7 = binaryReader.ReadInt32();
						if (crop.Left < 0 || crop.Top < 0 || crop.Right > region.Width || crop.Bottom > region.Height || crop.Width <= 0 || crop.Height <= 0 || num6 <= 0 || num7 <= 0 || (long)num6 * (long)num7 > 1000000 || (long)num6 * (long)num7 * 4 > sharedCapturePixels.Capacity)
						{
							throw new InvalidDataException("Invalid sampled capture geometry.");
						}
						long num8 = ((b2 == 4) ? binaryReader.ReadInt64() : 0);
						object obj;
						if (b2 == 4)
						{
							obj = (((object)capturePixels != null && num == num3 && capturePixels.SourceQpcTicks == num8 && num8 >= num4) ? capturePixels : null);
						}
						else
						{
							TimeSpan waitForFrame = TimeSpan.FromMilliseconds(num5);
							long sourceNotBeforeTicks = num4;
							obj = desktopSceneCapture2.TryRead(waitForFrame, default, sourceNotBeforeTicks, reusePixels: true);
						}
						CapturePixels capturePixels2 = (CapturePixels)obj;
						if (b2 == 1 && (object)capturePixels2 != null)
						{
							capturePixels = capturePixels2;
							num = num3;
						}
						if ((object)capturePixels2 == null)
						{
							binaryWriter.Write((byte)0);
							binaryWriter.Write(num2);
							binaryWriter.Write(num3);
							binaryWriter.Write(desktopSceneCapture2.SceneRevision);
						}
						else
						{
							nativeCaptureDiagnostics.ObservePixels(capturePixels2, num3);
							int num9 = checked(num6 * num7 * 4);
							if (array == null || array.Length != num9)
							{
								array = new byte[num9];
							}
							nativeFrameResize.ResizeInto(capturePixels2, num6, num7, crop, array);
							sharedCapturePixels.Write(array);
							binaryWriter.Write((byte)1);
							binaryWriter.Write(num2);
							binaryWriter.Write(num3);
							binaryWriter.Write(desktopSceneCapture2.SceneRevision);
							binaryWriter.Write(num6);
							binaryWriter.Write(num7);
							binaryWriter.Write(capturePixels2.SourceQpcTicks);
							binaryWriter.Write(num9);
							binaryWriter.Write(crop.Left);
							binaryWriter.Write(crop.Top);
							binaryWriter.Write(crop.Right);
							binaryWriter.Write(crop.Bottom);
						}
						binaryWriter.Flush();
						Program.Pump(TimeSpan.FromMilliseconds(1L));
					}
					lifetime.Cancel();
				}
				finally
				{
					if (lifetime != null)
					{
						((IDisposable)lifetime).Dispose();
					}
				}
			}
			finally
			{
				if (parent != null)
				{
					((IDisposable)parent).Dispose();
				}
			}
		}
		catch (EndOfStreamException)
		{
		}
		catch (IOException)
		{
			exitCode = 1;
		}
		catch (Exception ex3)
		{
			exitCode = 1;
			try
			{
				byte[] array3 = Encoding.UTF8.GetBytes(ex3.Message);
				if (array3.Length > 8192)
				{
					array3 = array3[..8192];
				}
				binaryWriter.Write((byte)2);
				binaryWriter.Write(array3.Length);
				binaryWriter.Write(array3);
				binaryWriter.Flush();
			}
			catch (IOException)
			{
			}
		}
		return true;
	}

	private static bool CompleteStartupFeedback()
	{
		PeekMessage(out var message, 0, 33556u, 33556u, 0u);
		if (PostThreadMessage(GetCurrentThreadId(), 33556u, 0u, 0))
		{
			return GetMessage(out message, 0, 33556u, 33556u) > 0;
		}
		return false;
	}

	[DllImport("kernel32.dll")]
	private static extern uint GetCurrentThreadId();

	[DllImport("user32.dll", EntryPoint = "PeekMessageW")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool PeekMessage(out StartupMessage message, nint window, uint min, uint max, uint remove);

	[DllImport("user32.dll", EntryPoint = "PostThreadMessageW")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool PostThreadMessage(uint thread, uint message, nuint wParam, nint lParam);

	[DllImport("user32.dll", EntryPoint = "GetMessageW")]
	private static extern int GetMessage(out StartupMessage message, nint window, uint min, uint max);

	private static int[] ReadFamily(BinaryReader input, int parent)
	{
		int num = input.ReadInt32();
		if (num < 1 || num > 256)
		{
			throw new InvalidDataException("Invalid exclusion family.");
		}
		HashSet<int> hashSet = new HashSet<int>
		{
			Environment.ProcessId,
			parent
		};
		for (int i = 0; i < num; i++)
		{
			int num2 = input.ReadInt32();
			if (num2 <= 0)
			{
				throw new InvalidDataException("Invalid exclusion process.");
			}
			hashSet.Add(num2);
		}
		return hashSet.ToArray();
	}

	[DllImport("user32.dll")]
	private static extern nint SetThreadDpiAwarenessContext(nint context);
}
