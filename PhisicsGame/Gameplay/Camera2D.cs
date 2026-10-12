using Microsoft.Xna.Framework;
using PhisicsGame.Core.Graphics;

namespace PhisicsGame.Gameplay;

/// <summary>Статичный вид сверху: весь уровень в кадре без следования за персонажем.</summary>
public static class Camera2D
{
    /// <summary>Центрирует мир в виртуальном кадре 1280×720 (если размеры совпадают — без сдвига).</summary>
    public static Matrix GetStaticWorldMatrix(int worldWidth, int worldHeight)
    {
        var tx = (VirtualResolution.Width - worldWidth) * 0.5f;
        var ty = (VirtualResolution.Height - worldHeight) * 0.5f;
        return Matrix.CreateTranslation(tx, ty, 0f);
    }
}
