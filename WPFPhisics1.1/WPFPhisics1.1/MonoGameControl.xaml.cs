using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PhisicsGame;
using PhisicsGame.Core.Input;
using WPFPhisics1._1.Interop;

namespace WPFPhisics1._1;

/// <summary>
/// Хост MonoGame (WindowsDX): общая текстура D3D11 → D3D9 и <see cref="D3DImage"/> внутри <see cref="Image"/>.
/// </summary>
public partial class MonoGameControl : IDisposable
{
    private const int GwlExStyle = -20;
    private const int WsExAppWindow = 0x00040000;
    private const int WsExToolWindow = 0x00000080;
    private const int SwHide = 0;

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll")]
    private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool bRepaint);

    private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        return IntPtr.Size == 8
            ? GetWindowLongPtr64(hWnd, nIndex)
            : new IntPtr(GetWindowLong32(hWnd, nIndex));
    }

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        return IntPtr.Size == 8
            ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
            : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
    }

    private readonly D3DImage _d3dImage = new();
    private readonly WpfD3D9SharedSurface _d3d9Shared = new();
    private PhisicsGame.Game1? _game;
    private bool _compositionHooked;
    private bool _inRender;
    private bool _firstFrameDone;
    private bool _surfaceDirty = true;
    private IntPtr _lastSharedHandle;
    private TimeSpan _lastRenderingTime = TimeSpan.MinValue;
    private readonly Stopwatch _frameTimer = Stopwatch.StartNew();
    private long _lastFrameTick;

    public MonoGameControl()
    {
        InitializeComponent();
        HostImage.Source = _d3dImage;
        _d3dImage.IsFrontBufferAvailableChanged += OnD3DImageFrontBufferChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += (_, _) => RequestResize();
        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible)
            {
                Focus();
                _surfaceDirty = true;
                _lastSharedHandle = IntPtr.Zero;
            }
        };
    }

    /// <summary>Не гоняем MonoGame, пока хост свёрнут, скрыт или без фокуса — иначе D3DImage+shared texture часто вешают UI при работе в других окнах (лаборатория, задачи, ForTeacher).</summary>
    private bool ShouldAdvanceGameFrame()
    {
        if (!IsLoaded || _game == null)
        {
            return false;
        }

        if (!IsVisible)
        {
            return false;
        }

        var host = Window.GetWindow(this);
        if (host != null)
        {
            if (host.WindowState == WindowState.Minimized)
            {
                return false;
            }

            if (!host.IsActive)
            {
                return false;
            }
        }

        if (!_d3dImage.IsFrontBufferAvailable)
        {
            return false;
        }

        return true;
    }

    private void OnD3DImageFrontBufferChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_d3dImage.IsFrontBufferAvailable)
        {
            _surfaceDirty = true;
            _lastSharedHandle = IntPtr.Zero;
            return;
        }

        try
        {
            _d3dImage.Lock();
            _d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
        }
        catch
        {
            // потеря устройства / гонка с композитором
        }
        finally
        {
            try
            {
                _d3dImage.Unlock();
            }
            catch
            {
                // ignore
            }
        }

        _d3d9Shared.DisposeBinding();
        _lastSharedHandle = IntPtr.Zero;
        _surfaceDirty = true;
    }

    public void StartGame()
    {
        if (_game != null)
        {
            return;
        }

        var (w, h) = GetPresentationPixelSize();
        _game = new PhisicsGame.Game1(hostForWpf: true);
        _game.ResizePresentation(w, h);
        _firstFrameDone = false;
        _surfaceDirty = true;
        _lastSharedHandle = IntPtr.Zero;
        HookComposition();
        Focus();
    }

    public void StopGame()
    {
        UnhookComposition();
        _d3d9Shared.DisposeBinding();
        _lastSharedHandle = IntPtr.Zero;
        _game?.Dispose();
        _game = null;
        _firstFrameDone = false;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Focus();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _d3dImage.IsFrontBufferAvailableChanged -= OnD3DImageFrontBufferChanged;
        StopGame();
    }

    private void HookComposition()
    {
        if (_compositionHooked)
        {
            return;
        }

        CompositionTarget.Rendering += OnCompositionRendering;
        _compositionHooked = true;
    }

    private void UnhookComposition()
    {
        if (!_compositionHooked)
        {
            return;
        }

        CompositionTarget.Rendering -= OnCompositionRendering;
        _compositionHooked = false;
    }

    private void OnCompositionRendering(object? sender, EventArgs e)
    {
        if (_inRender || _game == null || !IsLoaded)
        {
            return;
        }

        if (!ShouldAdvanceGameFrame())
        {
            return;
        }

        // CompositionTarget.Rendering может вызываться несколько раз за один кадр — не гоняем Tick повторно.
        if (e is RenderingEventArgs re)
        {
            if (re.RenderingTime == _lastRenderingTime)
            {
                return;
            }
        }

        // Ограничение ~60 Гц (не конфликтует с дедупликацией: время кадра обновляем только после успешного Tick).
        var nowTick = _frameTimer.ElapsedTicks;
        var minTicks = Stopwatch.Frequency / 60;
        if (nowTick - _lastFrameTick < minTicks)
        {
            return;
        }

        _inRender = true;
        try
        {
            var (w, h) = GetPresentationPixelSize();
            PushInput(w, h);
            _game.ResizePresentation(w, h);

            if (!_firstFrameDone)
            {
                _game.RunOneFrame();
                _firstFrameDone = true;
                ApplyMonoGameHostWindowHidden();
            }
            else
            {
                _game.Tick();
            }

            TryUpdateD3DImageSurface();

            _lastFrameTick = nowTick;
            if (e is RenderingEventArgs reOk)
            {
                _lastRenderingTime = reOk.RenderingTime;
            }
        }
        finally
        {
            _inRender = false;
        }
    }

    private void TryUpdateD3DImageSurface()
    {
        if (_game == null)
        {
            return;
        }

        if (!_d3dImage.IsFrontBufferAvailable)
        {
            return;
        }

        var handle = _game.WpfSharedTextureHandle;
        var pw = _game.PresentationPixelWidth;
        var ph = _game.PresentationPixelHeight;
        if (handle == IntPtr.Zero || pw <= 0 || ph <= 0)
        {
            return;
        }

        try
        {
            if (_surfaceDirty || handle != _lastSharedHandle)
            {
                _d3d9Shared.Bind(_d3dImage, handle, pw, ph);
                _lastSharedHandle = handle;
                _surfaceDirty = false;
            }

            _d3d9Shared.InvalidateFull(_d3dImage, pw, ph);
        }
        catch
        {
            _d3d9Shared.DisposeBinding();
            _lastSharedHandle = IntPtr.Zero;
            _surfaceDirty = true;
        }
    }

    private void RequestResize()
    {
        _surfaceDirty = true;
        _lastSharedHandle = IntPtr.Zero;
    }

    private (int W, int H) GetPresentationPixelSize()
    {
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget == null)
        {
            return (1280, 720);
        }

        var t = source.CompositionTarget.TransformToDevice;
        var w = Math.Max(1, (int)Math.Ceiling(ActualWidth * t.M11));
        var h = Math.Max(1, (int)Math.Ceiling(ActualHeight * t.M22));
        return (w, h);
    }

    private void PushInput(int w, int h)
    {
        if (!IsLoaded || _game == null)
        {
            return;
        }

        var pos = System.Windows.Input.Mouse.GetPosition(this);
        var x = (int)Math.Clamp(Math.Round(pos.X * w / Math.Max(ActualWidth, 1.0)), 0, w - 1);
        var y = (int)Math.Clamp(Math.Round(pos.Y * h / Math.Max(ActualHeight, 1.0)), 0, h - 1);

        HostedInputSource.SetMousePosition(x, y);
        HostedInputSource.SetMouseButtons(
            System.Windows.Input.Mouse.LeftButton == MouseButtonState.Pressed,
            System.Windows.Input.Mouse.RightButton == MouseButtonState.Pressed,
            System.Windows.Input.Mouse.MiddleButton == MouseButtonState.Pressed,
            System.Windows.Input.Mouse.XButton1 == MouseButtonState.Pressed,
            System.Windows.Input.Mouse.XButton2 == MouseButtonState.Pressed);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        HostedInputSource.AddWheelDelta(e.Delta);
    }

    protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        // Кнопки отпускаются системой при уходе курсора; колёсико не сбрасываем (как у WinForms).
    }

    protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        base.OnKeyDown(e);
        var k = KeyToXna(e.Key);
        if (k != Microsoft.Xna.Framework.Input.Keys.None)
        {
            HostedInputSource.SetKeyDown(k);
        }
    }

    protected override void OnKeyUp(System.Windows.Input.KeyEventArgs e)
    {
        base.OnKeyUp(e);
        var k = KeyToXna(e.Key);
        if (k != Microsoft.Xna.Framework.Input.Keys.None)
        {
            HostedInputSource.SetKeyUp(k);
        }
    }

    private static Microsoft.Xna.Framework.Input.Keys KeyToXna(System.Windows.Input.Key key)
    {
        var vk = KeyInterop.VirtualKeyFromKey(key);
        return Enum.IsDefined(typeof(Microsoft.Xna.Framework.Input.Keys), vk)
            ? (Microsoft.Xna.Framework.Input.Keys)vk
            : Microsoft.Xna.Framework.Input.Keys.None;
    }

    /// <summary>
    /// Окно WinForms MonoGame должно не быть отдельным top-level (иначе «игра на весь экран» за WPF).
    /// Скрываем, делаем tool-window, при необходимости встраиваем как дочернее HWND главного окна WPF.
    /// </summary>
    private void ApplyMonoGameHostWindowHidden()
    {
        if (_game == null)
        {
            return;
        }

        try
        {
            var hwnd = _game.Window.Handle;
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            var ex = unchecked((uint)GetWindowLongPtr(hwnd, GwlExStyle).ToInt32());
            ex |= (uint)WsExToolWindow;
            ex &= ~(uint)WsExAppWindow;
            SetWindowLongPtr(hwnd, GwlExStyle, new IntPtr(ex));

            var w = Window.GetWindow(this);
            if (w != null)
            {
                var parent = new WindowInteropHelper(w).Handle;
                if (parent != IntPtr.Zero)
                {
                    SetParent(hwnd, parent);
                    MoveWindow(hwnd, 0, 0, 1, 1, true);
                }
            }

            ShowWindow(hwnd, SwHide);

            var formField = _game.Window.GetType().GetField("Form", BindingFlags.Instance | BindingFlags.NonPublic);
            var formObj = formField?.GetValue(_game.Window);
            var t = formObj?.GetType();
            t?.GetProperty("ShowInTaskbar")?.SetValue(formObj, false);
            t?.GetProperty("Visible")?.SetValue(formObj, false);
        }
        catch
        {
            // Окно может быть не WinForms в других сборках MonoGame.
        }
    }

    public void Dispose()
    {
        _d3dImage.IsFrontBufferAvailableChanged -= OnD3DImageFrontBufferChanged;
        StopGame();
        _d3d9Shared.Dispose();
        GC.SuppressFinalize(this);
    }
}
