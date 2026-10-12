using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PhisicsGame.Core.Text;

namespace PhisicsGame.Core.UI;

/// <summary>Сложение переносов и подсветка изолированных латинских букв (переменные вроде c, v, n).</summary>
public static class TaskInstructionText
{
    public static List<string> WordWrap(string text, SpriteFont font, float scale, int maxWidth)
    {
        text = SpriteFontText.Sanitize(text);
        var lines = new List<string>();
        if (string.IsNullOrEmpty(text))
        {
            lines.Add(string.Empty);
            return lines;
        }

        var words = text.Split(' ', StringSplitOptions.None);
        var line = string.Empty;
        foreach (var word in words)
        {
            var candidate = string.IsNullOrEmpty(line) ? word : line + " " + word;
            var w = font.MeasureString(SpriteFontText.Sanitize(candidate)).X * scale;
            if (w <= maxWidth || string.IsNullOrEmpty(line))
            {
                line = candidate;
            }
            else
            {
                lines.Add(line);
                line = word;
            }
        }

        if (!string.IsNullOrEmpty(line))
        {
            lines.Add(line);
        }

        return lines;
    }

    public static bool IsIsolatedLatinLetter(string line, int i)
    {
        var c = line[i];
        if (c is < 'A' or > 'z' || (c > 'Z' && c < 'a'))
        {
            return false;
        }

        if (i > 0 && IsLatinLetter(line[i - 1]))
        {
            return false;
        }

        if (i + 1 < line.Length && IsLatinLetter(line[i + 1]))
        {
            return false;
        }

        return true;
    }

    private static bool IsLatinLetter(char c) => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z';

    public static void DrawWrappedWithVariables(
        SpriteBatch sb,
        SpriteFont font,
        string text,
        Rectangle bounds,
        float scale,
        Color normal,
        Color variable,
        float fade)
    {
        text = SpriteFontText.Sanitize(text);
        var lines = WordWrap(text, font, scale, bounds.Width);
        var y = (float)bounds.Y;
        var lineHeight = font.LineSpacing * scale;
        foreach (var line in lines)
        {
            var x = (float)bounds.X;
            for (var i = 0; i < line.Length; i++)
            {
                var ch = line[i].ToString();
                var safe = SpriteFontText.Sanitize(ch);
                var col = IsIsolatedLatinLetter(line, i) ? variable : normal;
                sb.DrawString(font, safe, new Vector2(x, y), col * fade, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                x += font.MeasureString(safe).X * scale;
            }

            y += lineHeight;
            if (y > bounds.Bottom)
            {
                break;
            }
        }
    }
}
