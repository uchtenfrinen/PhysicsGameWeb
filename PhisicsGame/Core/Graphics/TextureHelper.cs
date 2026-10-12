using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PhisicsGame.Core.Graphics;

/// <summary>
/// PNG из Texture2D.FromStream обычно в straight alpha, а SpriteBatch по умолчанию ожидает premultiplied —
/// из‑за этого на краях спрайта часто появляются ореолы / «пятна».
/// Заливку фона по цвету не используем: у персонажа чёрные штаны/обувь совпадают с чёрным фоном и тоже удалялись.
/// </summary>
public static class TextureHelper
{
    public static void PremultiplyAlpha(Texture2D texture)
    {
        if (texture.Format != SurfaceFormat.Color)
        {
            return;
        }

        var data = new Color[texture.Width * texture.Height];
        texture.GetData(data);
        for (var i = 0; i < data.Length; i++)
        {
            ref var c = ref data[i];
            // Убрать полупрозрачный мусор на краях после вырезки.
            if (c.A <= 18)
            {
                c = Color.Transparent;
                continue;
            }

            var a = c.A / 255f;
            c.R = (byte)(c.R * a);
            c.G = (byte)(c.G * a);
            c.B = (byte)(c.B * a);
        }

        texture.SetData(data);
    }
}
