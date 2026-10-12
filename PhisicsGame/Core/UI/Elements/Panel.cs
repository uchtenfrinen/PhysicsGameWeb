using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PhisicsGame.Core.UI.Elements;

public sealed class Panel : UIElement
{
    public int CornerRadius { get; set; } = 12;
    public int ShadowSize { get; set; } = 14;
    public Color BackgroundColor { get; set; } = UITheme.Panel;
    public Color BorderColor { get; set; } = UITheme.Accent * 0.5f;

    public override void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        if (!Visible)
        {
            return;
        }

        UIPrimitives.DrawShadow(spriteBatch, pixel, Bounds, ShadowSize, Color.Black * 0.33f * Alpha);
        UIPrimitives.DrawRoundedPanel(spriteBatch, pixel, Bounds, CornerRadius, BackgroundColor * Alpha);

        var border = new Rectangle(Bounds.X - 1, Bounds.Y - 1, Bounds.Width + 2, Bounds.Height + 2);
        UIPrimitives.DrawRoundedPanel(spriteBatch, pixel, border, CornerRadius + 1, Color.Transparent);
        spriteBatch.Draw(pixel, new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, 1), BorderColor * Alpha);
        spriteBatch.Draw(pixel, new Rectangle(Bounds.X, Bounds.Bottom - 1, Bounds.Width, 1), BorderColor * Alpha);
    }
}
