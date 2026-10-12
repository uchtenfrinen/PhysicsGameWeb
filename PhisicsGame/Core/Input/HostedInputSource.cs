using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace PhisicsGame.Core.Input;

/// <summary>
/// Ввод из WPF (мышь/клавиатура), когда окно MonoGame скрыто и стандартный Input не привязан к области WPF.
/// </summary>
public static class HostedInputSource
{
    /// <summary>SortedSet даёт отсортированный порядок без LINQ на каждом кадре.</summary>
    private static readonly SortedSet<Keys> PressedKeys = new();

    private static int _mouseX;
    private static int _mouseY;
    private static int _scrollCumulative;
    private static int _horizontalScrollCumulative;
    private static ButtonState _left = ButtonState.Released;
    private static ButtonState _right = ButtonState.Released;
    private static ButtonState _middle = ButtonState.Released;
    private static ButtonState _x1 = ButtonState.Released;
    private static ButtonState _x2 = ButtonState.Released;

    public static bool IsActive { get; set; }

    public static void Reset()
    {
        PressedKeys.Clear();
        _mouseX = 0;
        _mouseY = 0;
        _scrollCumulative = 0;
        _horizontalScrollCumulative = 0;
        _left = _right = _middle = _x1 = _x2 = ButtonState.Released;
    }

    public static void SetKeyDown(Keys key) => PressedKeys.Add(key);

    public static void SetKeyUp(Keys key) => PressedKeys.Remove(key);

    public static void SetMousePosition(int x, int y)
    {
        _mouseX = x;
        _mouseY = y;
    }

    public static void SetMouseButtons(bool left, bool right, bool middle, bool x1 = false, bool x2 = false)
    {
        _left = left ? ButtonState.Pressed : ButtonState.Released;
        _right = right ? ButtonState.Pressed : ButtonState.Released;
        _middle = middle ? ButtonState.Pressed : ButtonState.Released;
        _x1 = x1 ? ButtonState.Pressed : ButtonState.Released;
        _x2 = x2 ? ButtonState.Pressed : ButtonState.Released;
    }

    public static void AddWheelDelta(int delta) => _scrollCumulative += delta;

    public static void AddHorizontalWheelDelta(int delta) => _horizontalScrollCumulative += delta;

    public static KeyboardState GetKeyboardState()
    {
        var n = PressedKeys.Count;
        if (n == 0)
        {
            return new KeyboardState();
        }

        var keys = new Keys[n];
        PressedKeys.CopyTo(keys, 0);
        return new KeyboardState(keys);
    }

    public static MouseState GetMouseState() =>
        new MouseState(
            _mouseX,
            _mouseY,
            _scrollCumulative,
            _left,
            _middle,
            _right,
            _x1,
            _x2,
            _horizontalScrollCumulative);
}
