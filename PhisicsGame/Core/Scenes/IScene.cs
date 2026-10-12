using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PhisicsGame.Core.Scenes;

public interface IScene
{
    void OnEnter();
    void OnExit();
    void Update(GameTime gameTime);
    void Draw(GameTime gameTime, SpriteBatch spriteBatch);
}
