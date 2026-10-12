using System.Text;

namespace PhisicsGame.Core.Text;

/// <summary>
/// Подготовка строк для <see cref="Microsoft.Xna.Framework.Graphics.SpriteFont"/>:
/// в текстуре шрифта должны быть глифы для всех символов, иначе DrawString падает.
/// </summary>
public static class SpriteFontText
{
    /// <summary>
    /// Удаляет или заменяет символы вне поддерживаемых диапазонов (см. DefaultFont.spritefont).
    /// Идемпотентно для уже «безопасных» строк.
    /// </summary>
    public static string Sanitize(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        var needsWork = false;
        foreach (var r in text.EnumerateRunes())
        {
            if (!IsSupported(r))
            {
                needsWork = true;
                break;
            }
        }

        if (!needsWork)
        {
            return text;
        }

        var sb = new StringBuilder(text.Length);
        foreach (var r in text.EnumerateRunes())
        {
            if (IsSupported(r))
            {
                sb.Append(r);
            }
            else
            {
                sb.Append('?');
            }
        }

        return sb.ToString();
    }

    private static bool IsSupported(Rune r)
    {
        var v = r.Value;
        if (v is '\n' or '\r' or '\t')
        {
            return true;
        }

        if (v >= 32 && v <= 126)
        {
            return true;
        }

        if (v >= 160 && v <= 255)
        {
            return true;
        }

        // Cyrillic block U+0400–U+04FF
        if (v >= 0x0400 && v <= 0x04FF)
        {
            return true;
        }

        // General punctuation (тире «умная» пунктуация и т.п.)
        if (v >= 8192 && v <= 8303)
        {
            return true;
        }

        // Mathematical operators (∫ ∑ √ …), U+2200–U+22FF
        if (v >= 0x2200 && v <= 0x22FF)
        {
            return true;
        }

        return false;
    }
}
