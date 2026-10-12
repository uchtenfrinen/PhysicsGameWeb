using System;
using System.Windows;
using System.Windows.Interop;
using SharpDX.Direct3D9;

namespace WPFPhisics1._1.Interop;

/// <summary>
/// Подключает DXGI shared handle (D3D11 / MonoGame) к D3D9-текстуре и поверхности для <see cref="D3DImage"/>.
/// Используется конструктор <see cref="Texture"/> с <c>ref sharedHandle</c> (совместимость D3D9↔D3D11).
/// </summary>
public sealed class WpfD3D9SharedSurface : IDisposable
{
    private Direct3DEx? _d3d;
    private Device? _device;
    private Texture? _sharedTexture;
    private SharpDX.Direct3D9.Surface? _surface;

    public void Bind(D3DImage d3dImage, IntPtr sharedHandle, int pixelWidth, int pixelHeight)
    {
        if (sharedHandle == IntPtr.Zero || pixelWidth <= 0 || pixelHeight <= 0)
        {
            return;
        }

        if (!d3dImage.IsFrontBufferAvailable)
        {
            return;
        }

        DisposeBinding();

        _d3d ??= new Direct3DEx();

        if (_device == null)
        {
            var pp = new PresentParameters
            {
                Windowed = true,
                SwapEffect = SwapEffect.Discard,
                DeviceWindowHandle = IntPtr.Zero,
                PresentationInterval = PresentInterval.Default,
                BackBufferFormat = Format.Unknown,
                BackBufferWidth = 1,
                BackBufferHeight = 1,
                BackBufferCount = 1,
                EnableAutoDepthStencil = false
            };

            const CreateFlags flags =
                CreateFlags.Multithreaded |
                CreateFlags.FpuPreserve |
                CreateFlags.HardwareVertexProcessing;

            // Device (не DeviceEx): так надёжнее открывает shared D3D11-текстуру (см. SharpDX + D3DImage примеры).
            _device = new Device(_d3d, 0, DeviceType.Hardware, IntPtr.Zero, flags, pp);
        }

        var h = sharedHandle;
        _sharedTexture = new Texture(
            _device,
            pixelWidth,
            pixelHeight,
            1,
            Usage.RenderTarget,
            Format.A8R8G8B8,
            Pool.Default,
            ref h);
        _surface = _sharedTexture.GetSurfaceLevel(0);

        try
        {
            d3dImage.Lock();
            d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _surface.NativePointer);
        }
        finally
        {
            d3dImage.Unlock();
        }
    }

    public void InvalidateFull(D3DImage d3dImage, int pixelWidth, int pixelHeight)
    {
        if (pixelWidth <= 0 || pixelHeight <= 0)
        {
            return;
        }

        if (!d3dImage.IsFrontBufferAvailable)
        {
            return;
        }

        try
        {
            d3dImage.Lock();
            d3dImage.AddDirtyRect(new Int32Rect(0, 0, pixelWidth, pixelHeight));
        }
        finally
        {
            d3dImage.Unlock();
        }
    }

    public void DisposeBinding()
    {
        _surface?.Dispose();
        _surface = null;
        _sharedTexture?.Dispose();
        _sharedTexture = null;
    }

    public void Dispose()
    {
        DisposeBinding();
        _device?.Dispose();
        _device = null;
        _d3d?.Dispose();
        _d3d = null;
    }
}
