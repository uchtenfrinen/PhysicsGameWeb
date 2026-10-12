using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PhisicsGame.Core.Text;

namespace PhisicsGame.Core.UI.Elements;

public enum UIButtonVisualState
{
    Normal,
    Hover,
    Selected,
    Correct,
    Wrong
}

public sealed class Button : UIElement
{
    public string Text { get; set; } = string.Empty;
    public UIButtonVisualState VisualState { get; set; } = UIButtonVisualState.Normal;
    public float Scale { get; private set; } = 1f;

    public override void Update(GameTime gameTime, Core.Input.InputState input)
    {
        var targetScale = VisualState is UIButtonVisualState.Hover or UIButtonVisualState.Selected ? 1.03f : 1f;
        var t = MathHelper.Clamp((float)gameTime.ElapsedGameTime.TotalSeconds * 9f, 0f, 1f);
        Scale = MathHelper.Lerp(Scale, targetScale, t);
    }

    public override void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        if (!Visible)
        {
            return;
        }

        var center = new Vector2(Bounds.Center.X, Bounds.Center.Y);
        var size = new Vector2(Bounds.Width * Scale, Bounds.Height * Scale);
        var drawRect = new Rectangle(
            (int)(center.X - size.X * 0.5f),
            (int)(center.Y - size.Y * 0.5f),
            (int)size.X,
            (int)size.Y);

        var (bg, border) = ResolveColors();
        UIPrimitives.DrawRoundedPanel(spriteBatch, pixel, drawRect, 9, bg * Alpha);
        spriteBatch.Draw(pixel, new Rectangle(drawRect.X, drawRect.Y, drawRect.Width, 2), border * Alpha);
        spriteBatch.Draw(pixel, new Rectangle(drawRect.X, drawRect.Bottom - 2, drawRect.Width, 2), border * Alpha);

        var safe = SpriteFontText.Sanitize(Text);
        var textSize = font.MeasureString(safe);
        var textPos = new Vector2(drawRect.Center.X - textSize.X * 0.5f, drawRect.Center.Y - textSize.Y * 0.5f);
        spriteBatch.DrawString(font, safe, textPos, UITheme.TextPrimary * Alpha);
    }

    private (Color bg, Color border) ResolveColors()
    {
        return VisualState switch
        {
            UIButtonVisualState.Normal => (new Color(34, 42, 65, 220), UITheme.Accent * 0.45f),
            UIButtonVisualState.Hover => (new Color(47, 58, 84, 230), UITheme.Accent),
            UIButtonVisualState.Selected => (new Color(62, 74, 102, 235), UITheme.AccentAlt),
            UIButtonVisualState.Correct => (new Color(30, 72, 46, 235), UITheme.Success),
            UIButtonVisualState.Wrong => (new Color(90, 32, 45, 235), UITheme.Error),
            _ => (new Color(34, 42, 65, 220), UITheme.Accent * 0.45f)
        };
    }
}
