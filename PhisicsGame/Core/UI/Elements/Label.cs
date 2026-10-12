using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PhisicsGame.Core.Text;

namespace PhisicsGame.Core.UI.Elements;

public sealed class Label : UIElement
{
    public string Text { get; set; } = string.Empty;
    public Color Color { get; set; } = UITheme.TextPrimary;
    public float Scale { get; set; } = 1f;

    public override void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        if (!Visible)
        {
            return;
        }

        spriteBatch.DrawString(font, SpriteFontText.Sanitize(Text), Bounds.Location.ToVector2(), Color * Alpha, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
    }
}
