using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PhisicsGame.Core.Graphics;

/// <summary>
/// Логическое разрешение сцены. Сцена рисуется в RT 1280×720, затем масштабируется на физический экран с полосами.
/// </summary>
public static class VirtualResolution
{
    public const int Width = 1280;
    public const int Height = 720;

    /// <summary>
    /// Размер окна/экрана при <see cref="GraphicsDevice.SetRenderTarget"/> (null). Не меняется при рендере в RT 1280×720.
    /// Нужен для мыши: SDL даёт координаты в этом пространстве, а PP во время сцены указывает на RT.
    /// </summary>
    public static int PhysicalBackBufferWidth { get; private set; }

    public static int PhysicalBackBufferHeight { get; private set; }

    private static int _lastViewportVx;
    private static int _lastViewportVy;
    private static int _lastViewportVw = -1;
    private static int _lastViewportVh = -1;

    public static void SetPhysicalBackBufferSize(int w, int h)
    {
        if (w <= 0 || h <= 0)
        {
            return;
        }

        if (PhysicalBackBufferWidth == w && PhysicalBackBufferHeight == h)
        {
            return;
        }

        PhysicalBackBufferWidth = w;
        PhysicalBackBufferHeight = h;
        _lastViewportVw = -1;
    }

    public static void GetLetterboxRectangle(int backBufferWidth, int backBufferHeight, out int vx, out int vy, out int vw, out int vh)
    {
        var scale = Math.Min(backBufferWidth / (float)Width, backBufferHeight / (float)Height);
        vw = Math.Max(1, (int)Math.Round(Width * scale));
        vh = Math.Max(1, (int)Math.Round(Height * scale));
        vx = (backBufferWidth - vw) / 2;
        vy = (backBufferHeight - vh) / 2;
    }

    /// <summary>Viewport на физическом backbuffer для корректной мыши (виртуальные 1280×720).</summary>
    public static void ApplyLetterboxViewport(GraphicsDevice graphicsDevice)
    {
        var bw = PhysicalBackBufferWidth > 0
            ? PhysicalBackBufferWidth
            : graphicsDevice.PresentationParameters.BackBufferWidth;
        var bh = PhysicalBackBufferHeight > 0
            ? PhysicalBackBufferHeight
            : graphicsDevice.PresentationParameters.BackBufferHeight;
        if (bw <= 0 || bh <= 0)
        {
            return;
        }

        GetLetterboxRectangle(bw, bh, out var vx, out var vy, out var vw, out var vh);
        if (vx == _lastViewportVx && vy == _lastViewportVy && vw == _lastViewportVw && vh == _lastViewportVh)
        {
            return;
        }

        _lastViewportVx = vx;
        _lastViewportVy = vy;
        _lastViewportVw = vw;
        _lastViewportVh = vh;
        graphicsDevice.Viewport = new Viewport(vx, vy, vw, vh);
    }

    /// <summary>Прямоугольник на экране для финального блита виртуальной сцены.</summary>
    public static Rectangle GetLetterboxDestinationRect(GraphicsDevice graphicsDevice)
    {
        var bw = PhysicalBackBufferWidth > 0
            ? PhysicalBackBufferWidth
            : graphicsDevice.PresentationParameters.BackBufferWidth;
        var bh = PhysicalBackBufferHeight > 0
            ? PhysicalBackBufferHeight
            : graphicsDevice.PresentationParameters.BackBufferHeight;
        GetLetterboxRectangle(bw, bh, out var vx, out var vy, out var vw, out var vh);
        return new Rectangle(vx, vy, vw, vh);
    }

    /// <summary>Прямоугольник в координатах виртуальной сцены (1280×720) в пиксели физического backbuffer с учётом letterbox.</summary>
    public static Rectangle VirtualRectToPhysical(int virtualX, int virtualY, int virtualW, int virtualH)
    {
        var bw = PhysicalBackBufferWidth > 0 ? PhysicalBackBufferWidth : Width;
        var bh = PhysicalBackBufferHeight > 0 ? PhysicalBackBufferHeight : Height;
        if (bw <= 0 || bh <= 0)
        {
            return new Rectangle(virtualX, virtualY, virtualW, virtualH);
        }

        GetLetterboxRectangle(bw, bh, out var vx, out var vy, out var vw, out var vh);
        var scaleX = vw / (float)Width;
        var scaleY = vh / (float)Height;
        return new Rectangle(
            vx + (int)Math.Round(virtualX * scaleX),
            vy + (int)Math.Round(virtualY * scaleY),
            Math.Max(1, (int)Math.Round(virtualW * scaleX)),
            Math.Max(1, (int)Math.Round(virtualH * scaleY)));
    }

    /// <summary>Точка виртуальной сцены (1280×720) в координаты физического backbuffer.</summary>
    public static Vector2 VirtualPointToPhysical(float virtualX, float virtualY)
    {
        var bw = PhysicalBackBufferWidth > 0 ? PhysicalBackBufferWidth : Width;
        var bh = PhysicalBackBufferHeight > 0 ? PhysicalBackBufferHeight : Height;
        GetLetterboxRectangle(bw, bh, out var vx, out var vy, out var vw, out var vh);
        return new Vector2(
            vx + virtualX * (vw / (float)Width),
            vy + virtualY * (vh / (float)Height));
    }
}
