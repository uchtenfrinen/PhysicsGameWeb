using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PhisicsGame.Core.Input;

namespace PhisicsGame.Core.UI.Elements;

public abstract class UIElement
{
    public Rectangle Bounds { get; set; }
    public bool Visible { get; set; } = true;
    public float Alpha { get; set; } = 1f;

    public virtual void Update(GameTime gameTime, InputState input)
    {
    }

    public abstract void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font);
}
