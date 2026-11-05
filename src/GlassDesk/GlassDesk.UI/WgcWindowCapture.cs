using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WinRT;
using Windows.Foundation.Metadata;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Security.Authorization.AppCapabilityAccess;

internal static class WgcWindowCapture
{
	internal sealed class PixelReadback : IDisposable
	{
		private nint _staging;

		private nint _device;

		private TextureDesc _description;

		private byte[]? _reusablePixels;

		internal unsafe CapturePixels ReadPixels(nint device, nint context, Direct3D11CaptureFrame frame, Action<CaptureTrace>? trace = null, bool reusePixels = false)
		{
			nint num = 0;
			nint ppv = 0;
			nint value = 0;
			try
			{
				num = MarshalInterface<IDirect3DSurface>.FromManaged(frame.Surface);
				Check(Marshal.QueryInterface(num, new Guid("a9b3d012-3df2-4ee3-b8d1-8695f457d3c1"), out ppv));
				Guid guid = new Guid("6f15aaf2-d208-4e89-9ab4-489535d34f9c");
				Check(((delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>)(*(IntPtr*)((nint)(*(IntPtr*)ppv) + (nint)3 * (nint)sizeof(nint))))(ppv, &guid, &value));
				TextureDesc textureDesc = default;
				((delegate* unmanaged[Stdcall]<nint, TextureDesc*, void>)(*(IntPtr*)((nint)(*(IntPtr*)value) + (nint)10 * (nint)sizeof(nint))))(value, &textureDesc);
				int width = frame.ContentSize.Width;
				int height = frame.ContentSize.Height;
				if (width <= 0 || height <= 0 || width > textureDesc.Width || height > textureDesc.Height || (long)width * (long)height > 16000000)
				{
					throw new InvalidOperationException("Controlled capture extent is invalid or exceeds the probe budget.");
				}
				textureDesc.Usage = 3u;
				textureDesc.BindFlags = 0u;
				textureDesc.CpuAccessFlags = 131072u;
				textureDesc.MiscFlags = 0u;
				if (_staging == 0 || _device != device || !SameTexture(_description, textureDesc))
				{
					Release(ref _staging);
					nint staging = 0;
					Check(((delegate* unmanaged[Stdcall]<nint, TextureDesc*, nint, nint*, int>)(*(IntPtr*)((nint)(*(IntPtr*)device) + (nint)5 * (nint)sizeof(nint))))(device, &textureDesc, 0, &staging));
					_staging = staging;
					_device = device;
					_description = textureDesc;
					trace?.Invoke(new CaptureTrace("ReadbackResource", "Created", Stopwatch.GetTimestamp(), 0L, 0L, 0L, 0L, 0, 0L));
				}
				else
				{
					trace?.Invoke(new CaptureTrace("ReadbackResource", "Reused", Stopwatch.GetTimestamp(), 0L, 0L, 0L, 0L, 0, 0L));
				}
				((delegate* unmanaged[Stdcall]<nint, nint, nint, void>)(*(IntPtr*)((nint)(*(IntPtr*)context) + (nint)47 * (nint)sizeof(nint))))(context, _staging, value);
				MappedResource mappedResource = default;
				Check(((delegate* unmanaged[Stdcall]<nint, nint, uint, uint, uint, MappedResource*, int>)(*(IntPtr*)((nint)(*(IntPtr*)context) + (nint)14 * (nint)sizeof(nint))))(context, _staging, 0u, 1u, 0u, &mappedResource));
				try
				{
					int num2 = checked(width * height * 4);
					byte[] array;
					if (reusePixels)
					{
						byte[]? reusablePixels = _reusablePixels;
						if (reusablePixels != null && reusablePixels.Length == num2)
						{
							array = _reusablePixels;
							goto IL_02e6;
						}
					}
					array = new byte[num2];
					goto IL_02e6;
					IL_02e6:
					byte[] array2 = array;
					if (reusePixels)
					{
						_reusablePixels = array2;
					}
					for (int i = 0; i < height; i++)
					{
						Marshal.Copy(mappedResource.Data + checked((nint)(i * mappedResource.RowPitch)), array2, i * width * 4, width * 4);
					}
					return new CapturePixels(width, height, array2)
					{
						SourceQpcTicks = (long)Math.Round(frame.SystemRelativeTime.TotalSeconds * (double)Stopwatch.Frequency)
					};
				}
				finally
				{
					((delegate* unmanaged[Stdcall]<nint, nint, uint, void>)(*(IntPtr*)((nint)(*(IntPtr*)context) + (nint)15 * (nint)sizeof(nint))))(context, _staging, 0u);
				}
			}
			finally
			{
				Release(ref value);
				Release(ref ppv);
				if (num != 0)
				{
					MarshalInterface<IDirect3DSurface>.DisposeAbi(num);
				}
			}
		}

		private static bool SameTexture(TextureDesc a, TextureDesc b)
		{
			if (a.Width == b.Width && a.Height == b.Height && a.MipLevels == b.MipLevels && a.ArraySize == b.ArraySize && a.Format == b.Format && a.SampleCount == b.SampleCount)
			{
				return a.SampleQuality == b.SampleQuality;
			}
			return false;
		}

		public void Dispose()
		{
			Release(ref _staging);
			_device = 0;
			_description = default;
			_reusablePixels = null;
		}
	}

	private struct TextureDesc
	{
		public uint Width;

		public uint Height;

		public uint MipLevels;

		public uint ArraySize;

		public uint Format;

		public uint SampleCount;

		public uint SampleQuality;

		public uint Usage;

		public uint BindFlags;

		public uint CpuAccessFlags;

		public uint MiscFlags;
	}

	private struct MappedResource
	{
		public nint Data;

		public uint RowPitch;

		public uint DepthPitch;
	}

	private struct NativeRect
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	public static CapturePixels? Capture(nint handle, TimeSpan timeout, bool borderless = false, bool includeSecondaryWindows = false)
	{
		if (!GraphicsCaptureSession.IsSupported())
		{
			throw new NotSupportedException("WGC is not supported.");
		}
		nint device = 0;
		nint context = 0;
		nint ppv = 0;
		nint inspectable = 0;
		IDirect3DDevice direct3DDevice = null;
		try
		{
			Check(D3D11CreateDevice(0, 1u, 0, 32u, 0, 0u, 7u, out device, out var _, out context));
			Guid iid = new Guid("54ec77fa-1377-44e6-8c32-88fd5f44c84c");
			Check(Marshal.QueryInterface(device, in iid, out ppv));
			Check(CreateDirect3D11DeviceFromDXGIDevice(ppv, out inspectable));
			direct3DDevice = MarshalInterface<IDirect3DDevice>.FromAbi(inspectable);
			GraphicsCaptureItem graphicsCaptureItem = CreateItem(handle);
			using Direct3D11CaptureFramePool direct3D11CaptureFramePool = Direct3D11CaptureFramePool.CreateFreeThreaded(direct3DDevice, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, graphicsCaptureItem.Size);
			using GraphicsCaptureSession graphicsCaptureSession = direct3D11CaptureFramePool.CreateCaptureSession(graphicsCaptureItem);
			graphicsCaptureSession.IsCursorCaptureEnabled = false;
			if (includeSecondaryWindows)
			{
				if (!ApiInformation.IsPropertyPresent("Windows.Graphics.Capture.GraphicsCaptureSession", "IncludeSecondaryWindows"))
				{
					throw new NotSupportedException("Secondary-window capture is unavailable on this OS.");
				}
				graphicsCaptureSession.IncludeSecondaryWindows = true;
			}
			if (borderless)
			{
				Task<AppCapabilityAccessStatus> task = GraphicsCaptureAccess.RequestAccessAsync(GraphicsCaptureAccessKind.Borderless).AsTask();
				Stopwatch stopwatch = Stopwatch.StartNew();
				while (!task.IsCompleted && stopwatch.Elapsed < TimeSpan.FromSeconds(10L))
				{
					Program.Pump(TimeSpan.FromMilliseconds(20L));
				}
				if (!task.IsCompleted || task.GetAwaiter().GetResult() != AppCapabilityAccessStatus.Allowed)
				{
					throw new InvalidOperationException("Borderless permission is absent; do not silently show a capture indicator.");
				}
				graphicsCaptureSession.IsBorderRequired = false;
			}
			graphicsCaptureSession.StartCapture();
			Stopwatch stopwatch2 = Stopwatch.StartNew();
			while (stopwatch2.Elapsed < timeout)
			{
				using Direct3D11CaptureFrame direct3D11CaptureFrame = direct3D11CaptureFramePool.TryGetNextFrame();
				if ((object)direct3D11CaptureFrame != null && direct3D11CaptureFrame.ContentSize.Width > 0 && direct3D11CaptureFrame.ContentSize.Height > 0)
				{
					return ReadPixels(device, context, direct3D11CaptureFrame);
				}
				Program.Pump(TimeSpan.FromMilliseconds(20L));
			}
			return null;
		}
		finally
		{
			direct3DDevice?.Dispose();
			Release(ref inspectable);
			Release(ref ppv);
			Release(ref context);
			Release(ref device);
		}
	}

	internal static ShadowObservation InspectSystemShadow(nint handle, CapturePixels frame)
	{
		Check(DwmGetWindowAttribute(handle, 9, out var bounds, Marshal.SizeOf<NativeRect>()));
		int num = bounds.Right + Math.Max(0, frame.Width - (bounds.Right - bounds.Left)) + 4;
		int num2 = (bounds.Top + bounds.Bottom) / 2;
		nint dC = GetDC(0);
		if (dC == 0)
		{
			throw new InvalidOperationException("Controlled desktop comparison is unavailable.");
		}
		try
		{
			uint pixel = GetPixel(dC, num, num2);
			uint pixel2 = GetPixel(dC, num + 40, num2);
			if (pixel == uint.MaxValue || pixel2 == uint.MaxValue)
			{
				throw new InvalidOperationException("Controlled desktop pixel read failed.");
			}
			int[] array = Rgb(pixel);
			int[] array2 = Rgb(pixel2);
			bool flag = array2.All((int channel) => channel >= 253);
			bool missingShadowPixels = array.All((int channel) => channel < 250) && array.Max() - array.Min() <= 2;
			if (!flag)
			{
				throw new InvalidOperationException("Controlled white backing was occluded; shadow comparison is inconclusive.");
			}
			return new ShadowObservation(missingShadowPixels, frame.Width, frame.Height, bounds.Right - bounds.Left, bounds.Bottom - bounds.Top, array, array2, num, num2);
		}
		finally
		{
			ReleaseDC(0, dC);
		}
		static int[] Rgb(uint value)
		{
			return new int[3]
			{
				(int)(value & 0xFF),
				(int)((value >> 8) & 0xFF),
				(int)((value >> 16) & 0xFF)
			};
		}
	}

	internal static ScreenRect ReadScreenBounds(nint handle)
	{
		Check(DwmGetWindowAttribute(handle, 9, out var bounds, Marshal.SizeOf<NativeRect>()));
		return new ScreenRect(bounds.Left, bounds.Top, bounds.Right - bounds.Left, bounds.Bottom - bounds.Top);
	}

	internal static int[] ReadScreenRgb(int x, int y)
	{
		nint dC = GetDC(0);
		if (dC == 0)
		{
			throw new InvalidOperationException("Controlled desktop comparison is unavailable.");
		}
		try
		{
			uint pixel = GetPixel(dC, x, y);
			if (pixel == uint.MaxValue)
			{
				throw new InvalidOperationException("Controlled desktop pixel read failed.");
			}
			return new int[3]
			{
				(int)(pixel & 0xFF),
				(int)((pixel >> 8) & 0xFF),
				(int)((pixel >> 16) & 0xFF)
			};
		}
		finally
		{
			ReleaseDC(0, dC);
		}
	}

	internal unsafe static GraphicsCaptureItem CreateItem(nint handle)
	{
		nint value = 0;
		nint factory = 0;
		nint value2 = 0;
		try
		{
			Check(WindowsCreateString("Windows.Graphics.Capture.GraphicsCaptureItem", "Windows.Graphics.Capture.GraphicsCaptureItem".Length, out value));
			Guid iid = new Guid("3628e81b-3cac-4c60-b7f4-23ce0e0c3356");
			Check(RoGetActivationFactory(value, ref iid, out factory));
			Guid guid = new Guid("79c3f95b-31f7-4ec2-a464-632ef5d30760");
			int num = ((delegate* unmanaged[Stdcall]<nint, nint, Guid*, nint*, int>)(*(IntPtr*)((nint)(*(IntPtr*)factory) + (nint)3 * (nint)sizeof(nint))))(factory, handle, &guid, &value2);
			if (num < 0)
			{
				throw new COMException($"IGraphicsCaptureItemInterop.CreateForWindow failed (0x{num:X8}).", num);
			}
			return MarshalInterface<GraphicsCaptureItem>.FromAbi(value2);
		}
		finally
		{
			Release(ref value2);
			Release(ref factory);
			if (value != 0)
			{
				WindowsDeleteString(value);
			}
		}
	}

	internal static CapturePixels ReadPixels(nint device, nint context, Direct3D11CaptureFrame frame)
	{
		using PixelReadback pixelReadback = new PixelReadback();
		return pixelReadback.ReadPixels(device, context, frame);
	}

	private static void Check(int hr)
	{
		Marshal.ThrowExceptionForHR(hr);
	}

	private static void Release(ref nint value)
	{
		if (value != 0)
		{
			Marshal.Release(value);
			value = 0;
		}
	}

	[DllImport("d3d11.dll", ExactSpelling = true)]
	private static extern int D3D11CreateDevice(nint adapter, uint driverType, nint software, uint flags, nint levels, uint count, uint sdk, out nint device, out uint level, out nint context);

	[DllImport("d3d11.dll", ExactSpelling = true)]
	private static extern int CreateDirect3D11DeviceFromDXGIDevice(nint device, out nint inspectable);

	[DllImport("combase.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
	private static extern int WindowsCreateString(string text, int length, out nint value);

	[DllImport("combase.dll", ExactSpelling = true)]
	private static extern int WindowsDeleteString(nint value);

	[DllImport("combase.dll", ExactSpelling = true)]
	private static extern int RoGetActivationFactory(nint name, ref Guid iid, out nint factory);

	[DllImport("dwmapi.dll", ExactSpelling = true)]
	private static extern int DwmGetWindowAttribute(nint handle, int attribute, out NativeRect bounds, int size);

	[DllImport("user32.dll")]
	private static extern nint GetDC(nint window);

	[DllImport("user32.dll")]
	private static extern int ReleaseDC(nint window, nint dc);

	[DllImport("gdi32.dll")]
	private static extern uint GetPixel(nint dc, int x, int y);
}
