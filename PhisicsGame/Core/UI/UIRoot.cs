using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PhisicsGame.Core.Input;
using PhisicsGame.Core.UI.Elements;

namespace PhisicsGame.Core.UI;

public sealed class UIRoot
{
    private readonly List<UIElement> _elements = [];

    public void Add(UIElement element) => _elements.Add(element);

    public void Clear() => _elements.Clear();

    public void Update(GameTime gameTime, InputState input)
    {
        foreach (var element in _elements)
        {
            element.Update(gameTime, input);
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        foreach (var element in _elements)
        {
            element.Draw(spriteBatch, pixel, font);
        }
    }
}
