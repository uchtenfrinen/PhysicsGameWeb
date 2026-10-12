namespace PhisicsGame.Gameplay.Entities.Components;

public sealed class PlayerControllerComponent(float speed) : IComponent
{
    public float Speed { get; } = speed;
}
