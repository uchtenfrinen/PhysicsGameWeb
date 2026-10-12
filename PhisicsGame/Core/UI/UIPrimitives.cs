using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PhisicsGame.Core.Text;

namespace PhisicsGame.Core.UI;

public static class UIPrimitives
{
    public static void DrawRoundedPanel(SpriteBatch sb, Texture2D pixel, Rectangle rect, int radius, Color color)
    {
        var center = new Rectangle(rect.X + radius, rect.Y, rect.Width - radius * 2, rect.Height);
        var left = new Rectangle(rect.X, rect.Y + radius, radius, rect.Height - radius * 2);
        var right = new Rectangle(rect.Right - radius, rect.Y + radius, radius, rect.Height - radius * 2);
        sb.Draw(pixel, center, color);
        sb.Draw(pixel, left, color);
        sb.Draw(pixel, right, color);

        // Rounded corner illusion via stepped quarter circles.
        for (var i = 0; i < radius; i++)
        {
            var width = radius - i;
            sb.Draw(pixel, new Rectangle(rect.X + i, rect.Y + i, width, 1), color);
            sb.Draw(pixel, new Rectangle(rect.Right - width - i, rect.Y + i, width, 1), color);
            sb.Draw(pixel, new Rectangle(rect.X + i, rect.Bottom - i - 1, width, 1), color);
            sb.Draw(pixel, new Rectangle(rect.Right - width - i, rect.Bottom - i - 1, width, 1), color);
        }
    }

    public static void DrawShadow(SpriteBatch sb, Texture2D pixel, Rectangle rect, int size, Color shadowColor)
    {
        for (var i = 1; i <= size; i++)
        {
            var fade = 1f - i / (float)(size + 1);
            var c = shadowColor * fade;
            sb.Draw(pixel, new Rectangle(rect.X - i, rect.Y - i, rect.Width + i * 2, 1), c);
            sb.Draw(pixel, new Rectangle(rect.X - i, rect.Bottom + i - 1, rect.Width + i * 2, 1), c);
            sb.Draw(pixel, new Rectangle(rect.X - i, rect.Y, 1, rect.Height), c);
            sb.Draw(pixel, new Rectangle(rect.Right + i - 1, rect.Y, 1, rect.Height), c);
        }
    }

    /// <summary>Тёмная панель с лёгким «техно» шумом (имитация карбона).</summary>
    public static void DrawCarbonPanel(SpriteBatch sb, Texture2D pixel, Rectangle rect, Color baseColor)
    {
        sb.Draw(pixel, rect, baseColor);
        for (var y = 0; y < rect.Height; y += 3)
        {
            sb.Draw(pixel, new Rectangle(rect.X, rect.Y + y, rect.Width, 1), Color.White * 0.035f);
        }

        for (var x = 0; x < rect.Width; x += 48)
        {
            sb.Draw(pixel, new Rectangle(rect.X + x, rect.Y, 1, rect.Height), Color.White * 0.025f);
        }
    }

    /// <summary>Внешняя «металлическая» рамка и внутренняя панель с карбоном.</summary>
    public static void DrawMetalFrame(SpriteBatch sb, Texture2D pixel, Rectangle outer, Color metal, Color innerBase)
    {
        sb.Draw(pixel, outer, metal);
        const int inset = 6;
        var inner = new Rectangle(outer.X + inset, outer.Y + inset, outer.Width - inset * 2, outer.Height - inset * 2);
        DrawCarbonPanel(sb, pixel, inner, innerBase);
    }

    /// <summary>Угловые «болты» для рамки.</summary>
    public static void DrawCornerScrews(SpriteBatch sb, Texture2D pixel, Rectangle outer, Color screwColor)
    {
        const int s = 6;
        var corners = new[]
        {
            new Point(outer.X + 4, outer.Y + 4),
            new Point(outer.Right - s - 4, outer.Y + 4),
            new Point(outer.X + 4, outer.Bottom - s - 4),
            new Point(outer.Right - s - 4, outer.Bottom - s - 4)
        };
        foreach (var c in corners)
        {
            sb.Draw(pixel, new Rectangle(c.X, c.Y, s, s), screwColor);
            sb.Draw(pixel, new Rectangle(c.X + 1, c.Y + 1, s - 2, s - 2), Color.Black * 0.35f);
        }
    }

    /// <summary>Линия произвольной толщины (для «доски» и т.п.).</summary>
    public static void DrawLine(SpriteBatch sb, Texture2D pixel, Vector2 start, Vector2 end, float thickness, Color color)
    {
        var edge = end - start;
        var length = edge.Length();
        if (length < 0.001f)
        {
            return;
        }

        var angle = MathF.Atan2(edge.Y, edge.X);
        sb.Draw(
            pixel,
            start,
            null,
            color,
            angle,
            new Vector2(0f, 0.5f),
            new Vector2(length, thickness),
            SpriteEffects.None,
            0f);
    }

    /// <summary>Неоновая подсветка текста (обводка + основной цвет).</summary>
    public static void DrawNeonTitle(SpriteBatch sb, SpriteFont font, string text, Vector2 position, Color glow, Color main, float scale, float alpha)
    {
        text = SpriteFontText.Sanitize(text);
        for (var dx = -2; dx <= 2; dx++)
        {
            for (var dy = -2; dy <= 2; dy++)
            {
                if (dx == 0 && dy == 0)
                {
                    continue;
                }

                sb.DrawString(font, text, position + new Vector2(dx, dy), glow * (alpha * 0.45f), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }

        sb.DrawString(font, text, position, main * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    /// <summary>
    /// Определённый интеграл: пределы ставятся у правой «ножки» ∫ (а не слева от символа), подынтегральное выражение — справа, по центру по вертикали.
    /// </summary>
    public static void DrawDefiniteIntegral(
        SpriteBatch sb,
        SpriteFont font,
        string lowerLimit,
        string upperLimit,
        string integrand,
        Vector2 origin,
        Color integralColor,
        Color limitColor,
        Color integrandColor,
        float alpha,
        float integralScale = 2.05f,
        float limitScale = 0.58f,
        float integrandScale = 1.02f)
    {
        var intChar = SpriteFontText.Sanitize("\u222B");
        var lo = SpriteFontText.Sanitize(lowerLimit);
        var hi = SpriteFontText.Sanitize(upperLimit);
        var arg = SpriteFontText.Sanitize(integrand);

        var intSize = font.MeasureString(intChar);
        var intW = intSize.X * integralScale;
        var intH = intSize.Y * integralScale;

        // Правая часть глифа ∫ — где в типографике ставят a и b.
        var stemX = origin.X + intW * 0.78f;

        var hiSize = font.MeasureString(hi);
        var loSize = font.MeasureString(lo);
        var hiH = hiSize.Y * limitScale;
        var loH = loSize.Y * limitScale;

        sb.DrawString(font, intChar, origin, integralColor * alpha, 0f, Vector2.Zero, integralScale, SpriteEffects.None, 0f);

        var upperPos = new Vector2(stemX, origin.Y + intH * 0.02f - hiH * 0.15f);
        sb.DrawString(font, hi, upperPos, limitColor * alpha, 0f, Vector2.Zero, limitScale, SpriteEffects.None, 0f);

        var lowerPos = new Vector2(stemX, origin.Y + intH - loH * 0.92f);
        sb.DrawString(font, lo, lowerPos, limitColor * alpha, 0f, Vector2.Zero, limitScale, SpriteEffects.None, 0f);

        var argSize = font.MeasureString(arg);
        var argH = argSize.Y * integrandScale;
        var afterLimits = stemX + MathF.Max(hiSize.X * limitScale, loSize.X * limitScale) + 10f;
        var argPos = new Vector2(afterLimits, origin.Y + (intH - argH) * 0.5f + intH * 0.04f);
        sb.DrawString(font, arg, argPos, integrandColor * alpha, 0f, Vector2.Zero, integrandScale, SpriteEffects.None, 0f);
    }

    /// <summary>Сетка «blueprint» для фона терминала задачи.</summary>
    public static void DrawBlueprintGrid(SpriteBatch sb, Texture2D pixel, Rectangle rect, Color lineColor, float fade)
    {
        const int step = 32;
        var c = lineColor * fade;
        for (var x = rect.Left; x <= rect.Right; x += step)
        {
            sb.Draw(pixel, new Rectangle(x, rect.Y, 1, rect.Height), c * 0.35f);
        }

        for (var y = rect.Top; y <= rect.Bottom; y += step)
        {
            sb.Draw(pixel, new Rectangle(rect.X, y, rect.Width, 1), c * 0.35f);
        }

        sb.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), c * 0.55f);
        sb.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), c * 0.45f);
    }

    /// <summary>Лёгкое «стекло»: полупрозрачная панель + блик сверху.</summary>
    public static void DrawGlassPanel(SpriteBatch sb, Texture2D pixel, Rectangle rect, Color border, Color fill, float fade)
    {
        DrawRoundedPanel(sb, pixel, rect, 12, fill * fade);
        sb.Draw(pixel, new Rectangle(rect.X + 8, rect.Y + 4, rect.Width - 16, 1), Color.White * 0.12f * fade);
        sb.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 2), border * 0.85f * fade);
        sb.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), border * 0.5f * fade);
        sb.Draw(pixel, new Rectangle(rect.X, rect.Y, 2, rect.Height), border * 0.45f * fade);
        sb.Draw(pixel, new Rectangle(rect.Right - 2, rect.Y, 2, rect.Height), border * 0.45f * fade);
    }

    public static void DrawVignette(SpriteBatch sb, Texture2D pixel, Rectangle viewport)
    {
        const int layers = 28;
        for (var i = 0; i < layers; i++)
        {
            var t = i / (float)layers;
            var alpha = 0.015f + t * 0.035f;
            var c = Color.Black * alpha;
            sb.Draw(pixel, new Rectangle(i, i, viewport.Width - i * 2, 1), c);
            sb.Draw(pixel, new Rectangle(i, viewport.Bottom - i - 1, viewport.Width - i * 2, 1), c);
            sb.Draw(pixel, new Rectangle(i, i, 1, viewport.Height - i * 2), c);
            sb.Draw(pixel, new Rectangle(viewport.Right - i - 1, i, 1, viewport.Height - i * 2), c);
        }
    }
}
