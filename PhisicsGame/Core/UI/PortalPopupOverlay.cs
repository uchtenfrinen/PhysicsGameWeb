using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PhisicsGame.Core.UI;

/// <summary>
/// Готовая картинка «профессор + облако» без наложения текста (см. Assets/Images/portal_popup_*.png).
/// </summary>
public static class PortalPopupOverlay
{
    /// <summary>Вызывать при неактивном <see cref="SpriteBatch"/> (после <c>End()</c> у вызывающего кода).</summary>
    public static void Draw(SpriteBatch spriteBatch, Texture2D image, float fadeAlpha = 1f)
    {
        if (image.Width <= 1)
        {
            return;
        }

        var gd = spriteBatch.GraphicsDevice;
        var vp = gd.Viewport;

        // ~1.25× от прежних 300×960 (предыдущий 1.625× / 1.3)
        const int prevMaxH = 300;
        const int prevMaxW = 960;
        const float scaleUp = 1.25f;
        const int offsetLeft = 200;
        var maxH = Math.Min((int)(prevMaxH * scaleUp), vp.Height - 8);
        var maxW = Math.Min((int)(prevMaxW * scaleUp), vp.Width - 24);
        maxW = Math.Max(1, maxW);
        maxH = Math.Max(1, maxH);
        var scaleW = maxW / (float)image.Width;
        var scaleH = maxH / (float)image.Height;
        var scale = Math.Min(scaleW, scaleH);
        var destW = (int)(image.Width * scale);
        var destH = (int)(image.Height * scale);
        var x = vp.X + (vp.Width - destW) / 2 - offsetLeft;
        // Нижний край содержимого (после обрезки прозрачных полей в загрузчике) — у нижней границы экрана
        var y = vp.Y + vp.Height - destH;
        var destRect = new Rectangle(x, y, destW, destH);

        // Текстура после загрузки — premultiplied (см. PortalPopupTextureLoader + TextureHelper).
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone);
        spriteBatch.Draw(image, destRect, Color.White * fadeAlpha);
        spriteBatch.End();
    }
}
