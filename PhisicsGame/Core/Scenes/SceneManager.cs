using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PhisicsGame.Core.Scenes;

public sealed class SceneManager(GameServices services)
{
    public IScene? CurrentScene { get; private set; }
    public GameServices Services { get; } = services;

    public void ChangeScene(IScene nextScene)
    {
        CurrentScene?.OnExit();
        CurrentScene = nextScene;
        CurrentScene.OnEnter();
    }

    public void Update(GameTime gameTime)
    {
        CurrentScene?.Update(gameTime);
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        CurrentScene?.Draw(gameTime, spriteBatch);
    }
}
