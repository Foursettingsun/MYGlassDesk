using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;

internal sealed class SharedCapturePixels : IDisposable
{
	private readonly MemoryMappedFile _mapping;

	private readonly MemoryMappedViewAccessor _view;

	private unsafe byte* _pointer;

	private bool _disposed;

	internal string Name { get; }

	internal int Capacity { get; }

	private unsafe SharedCapturePixels(string name, int capacity, bool create)
	{
		if (capacity <= 0 || capacity > 4000000)
		{
			throw new ArgumentOutOfRangeException("capacity");
		}
		Name = name;
		Capacity = capacity;
		_mapping = (create ? MemoryMappedFile.CreateNew(name, capacity, MemoryMappedFileAccess.ReadWrite) : MemoryMappedFile.OpenExisting(name, MemoryMappedFileRights.ReadWrite));
		try
		{
			_view = _mapping.CreateViewAccessor(0L, capacity, MemoryMappedFileAccess.ReadWrite);
			_view.SafeMemoryMappedViewHandle.AcquirePointer(ref _pointer);
			_pointer += _view.PointerOffset;
		}
		catch
		{
			_view?.Dispose();
			_mapping.Dispose();
			throw;
		}
	}

	internal static SharedCapturePixels Create(int capacity)
	{
		return new SharedCapturePixels("Local\\GlassDeskBackdrop-" + Environment.ProcessId + "-" + Guid.NewGuid().ToString("N"), capacity, create: true);
	}

	internal static SharedCapturePixels Open(string name, int capacity)
	{
		return new SharedCapturePixels(name, capacity, create: false);
	}

	internal unsafe void Write(byte[] pixels)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (pixels.Length == 0 || pixels.Length > Capacity)
		{
			throw new ArgumentException("Invalid shared capture size.");
		}
		Marshal.Copy(pixels, 0, (nint)_pointer, pixels.Length);
		Thread.MemoryBarrier();
	}

	internal unsafe void Read(byte[] pixels)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (pixels.Length == 0 || pixels.Length > Capacity)
		{
			throw new ArgumentException("Invalid shared capture size.");
		}
		Thread.MemoryBarrier();
		Marshal.Copy((nint)_pointer, pixels, 0, pixels.Length);
	}

	public unsafe void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_view.SafeMemoryMappedViewHandle.ReleasePointer();
			_pointer = null;
			_view.Dispose();
			_mapping.Dispose();
		}
	}
}
