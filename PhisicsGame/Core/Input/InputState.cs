using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PhisicsGame.Core.Graphics;

namespace PhisicsGame.Core.Input;

public sealed class InputState
{
    private KeyboardState _currentKeyboard;
    private KeyboardState _previousKeyboard;
    private MouseState _currentMouse;
    private MouseState _previousMouse;

    /// <param name="gameWindowActive">
    /// <see langword="false"/>, когда фокус у другого окна (WPF «Задачи», преподаватель и т.д.): иначе
    /// <see cref="Mouse"/> продолжает видеть нажатия, совпадающие по экрану с UI игры (например «Лаборатория» внизу справа).
    /// </param>
    public void Update(bool gameWindowActive)
    {
        _previousKeyboard = _currentKeyboard;
        _previousMouse = _currentMouse;

        if (HostedInputSource.IsActive)
        {
            _currentKeyboard = HostedInputSource.GetKeyboardState();
            _currentMouse = HostedInputSource.GetMouseState();
        }
        else
        {
            _currentKeyboard = Keyboard.GetState();
            var rawMouse = Mouse.GetState();
            if (!gameWindowActive)
            {
                _currentKeyboard = new KeyboardState();
                _currentMouse = new MouseState(
                    rawMouse.X,
                    rawMouse.Y,
                    rawMouse.ScrollWheelValue,
                    ButtonState.Released,
                    ButtonState.Released,
                    ButtonState.Released,
                    ButtonState.Released,
                    ButtonState.Released,
                    rawMouse.HorizontalScrollWheelValue);
            }
            else
            {
                _currentMouse = rawMouse;
            }
        }
    }

    public bool IsPressed(Keys key) => _currentKeyboard.IsKeyDown(key);

    public bool IsJustPressed(Keys key) =>
        _currentKeyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);

    public Point MousePosition => _currentMouse.Position;

    /// <summary>
    /// Координаты мыши в «игровом» пространстве (1280×720). Letterbox считается от физического backbuffer,
    /// как в <see cref="VirtualResolution.ApplyLetterboxViewport"/> — не от <see cref="GraphicsDevice.Viewport"/>,
    /// чтобы не ломаться после RenderTarget и композита.
    /// </summary>
    public Point GetMouseVirtual(GraphicsDevice graphicsDevice, int virtualWidth = 1280, int virtualHeight = 720)
    {
        var bw = VirtualResolution.PhysicalBackBufferWidth > 0
            ? VirtualResolution.PhysicalBackBufferWidth
            : graphicsDevice.PresentationParameters.BackBufferWidth;
        var bh = VirtualResolution.PhysicalBackBufferHeight > 0
            ? VirtualResolution.PhysicalBackBufferHeight
            : graphicsDevice.PresentationParameters.BackBufferHeight;
        if (bw <= 0 || bh <= 0)
        {
            return MousePosition;
        }

        VirtualResolution.GetLetterboxRectangle(bw, bh, out var vx, out var vy, out var vw, out var vh);
        if (vw <= 0 || vh <= 0)
        {
            return MousePosition;
        }

        var x = (MousePosition.X - vx) * (virtualWidth / (float)vw);
        var y = (MousePosition.Y - vy) * (virtualHeight / (float)vh);
        return new Point(
            (int)Math.Clamp(Math.Round(x), 0, virtualWidth - 1),
            (int)Math.Clamp(Math.Round(y), 0, virtualHeight - 1));
    }

    public bool IsLeftMouseJustPressed() =>
        _currentMouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;

    public bool IsLeftMousePressed() => _currentMouse.LeftButton == ButtonState.Pressed;
}
