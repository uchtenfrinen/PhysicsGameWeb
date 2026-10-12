using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PhisicsGame.Core.Text;

namespace PhisicsGame.Core.UI;

/// <summary>
/// Плашка с профессором и «облаком» текста при подсказке E (см. Assets/Images/interaction_hint.png).
/// Текст рисуется только внутри белой области облака (scissor + отступы).
/// </summary>
public static class InteractionHintPanel
{
    private static readonly RasterizerState ScissorRasterizer = new() { ScissorTestEnable = true };

    /// <summary>
    /// Вызывать при неактивном <see cref="SpriteBatch"/> (после <c>End()</c> у вызывающего кода).
    /// </summary>
    /// <param name="fadeAlpha">Множитель прозрачности (например из сцены с затуханием).</param>
    public static void Draw(
        SpriteBatch spriteBatch,
        Texture2D panelTexture,
        SpriteFont font,
        string text,
        float fadeAlpha = 1f)
    {
        if (panelTexture.Width <= 1 || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var gd = spriteBatch.GraphicsDevice;
        var vp = gd.Viewport;

        const int maxH = 260;
        const int maxW = 920;
        var scaleW = maxW / (float)panelTexture.Width;
        var scaleH = maxH / (float)panelTexture.Height;
        var scale = Math.Min(scaleW, scaleH);
        var destW = (int)(panelTexture.Width * scale);
        var destH = (int)(panelTexture.Height * scale);
        var x = vp.X + (vp.Width - destW) / 2;
        var y = vp.Y + vp.Height - destH - 8;
        var destRect = new Rectangle(x, y, destW, destH);

        // Спрайт целиком (персонаж слева, облако справа).
        spriteBatch.Begin();
        spriteBatch.Draw(panelTexture, destRect, Color.White * fadeAlpha);
        spriteBatch.End();

        // Белая область облака (доли от текстуры — персонаж ~0–38%, облако ~40%–95%).
        var bubbleOuter = new Rectangle(
            (int)(x + destW * 0.415f),
            (int)(y + destH * 0.095f),
            (int)(destW * 0.515f),
            (int)(destH * 0.545f));
        const int pad = 12;
        var textBounds = new Rectangle(
            bubbleOuter.X + pad,
            bubbleOuter.Y + pad,
            Math.Max(0, bubbleOuter.Width - pad * 2),
            Math.Max(0, bubbleOuter.Height - pad * 2));

        var scissor = ClipToViewport(gd, textBounds);
        if (scissor.Width <= 0 || scissor.Height <= 0)
        {
            return;
        }

        var safe = SpriteFontText.Sanitize(text);
        var prevScissor = gd.ScissorRectangle;
        gd.ScissorRectangle = scissor;

        try
        {
            // Immediate — один проход текста, без артефактов отложенной сортировки со scissor.
            spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                ScissorRasterizer);

            DrawWrappedStringCentered(
                spriteBatch,
                font,
                safe,
                textBounds,
                new Color(38, 46, 62) * fadeAlpha,
                0.52f);
        }
        finally
        {
            spriteBatch.End();
            gd.ScissorRectangle = prevScissor;
        }
    }

    private static Rectangle ClipToViewport(GraphicsDevice gd, Rectangle r)
    {
        var vp = gd.Viewport;
        var left = Math.Max(vp.X, r.Left);
        var top = Math.Max(vp.Y, r.Top);
        var right = Math.Min(vp.X + vp.Width, r.Right);
        var bottom = Math.Min(vp.Y + vp.Height, r.Bottom);
        var w = right - left;
        var h = bottom - top;
        if (w <= 0 || h <= 0)
        {
            return Rectangle.Empty;
        }

        return new Rectangle(left, top, w, h);
    }

    /// <summary>Перенос по словам, каждая строка по центру области (внутри scissor).</summary>
    private static void DrawWrappedStringCentered(
        SpriteBatch spriteBatch,
        SpriteFont font,
        string text,
        Rectangle bounds,
        Color color,
        float scale)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return;
        }

        var lineHeight = font.LineSpacing * scale * 0.92f;
        var lines = new List<string>();
        var line = string.Empty;

        foreach (var word in words)
        {
            var trial = string.IsNullOrEmpty(line) ? word : line + " " + word;
            if (font.MeasureString(trial).X * scale <= bounds.Width + 0.5f)
            {
                line = trial;
            }
            else
            {
                if (!string.IsNullOrEmpty(line))
                {
                    lines.Add(line);
                }

                line = word;
            }
        }

        if (!string.IsNullOrEmpty(line))
        {
            lines.Add(line);
        }

        if (lines.Count == 0)
        {
            return;
        }

        var totalTextHeight = lines.Count * lineHeight;
        var py = bounds.Y + Math.Max(0f, (bounds.Height - totalTextHeight) * 0.5f);
        foreach (var ln in lines)
        {
            if (py + lineHeight > bounds.Bottom + 0.5f)
            {
                break;
            }

            var lineW = font.MeasureString(ln).X * scale;
            var px = bounds.X + (bounds.Width - lineW) * 0.5f;
            if (px < bounds.X)
            {
                px = bounds.X;
            }

            spriteBatch.DrawString(
                font,
                ln,
                new Vector2(px, py),
                color,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f);
            py += lineHeight;
        }
    }
}
