using Microsoft.Xna.Framework;

namespace PhisicsGame.Gameplay.Entities.Components;

public sealed class TransformComponent(Vector2 position) : IComponent
{
    public Vector2 Position { get; set; } = position;
}
