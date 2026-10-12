using PhisicsGame.Gameplay.Entities.Components;

namespace PhisicsGame.Gameplay.Entities;

public sealed class Entity
{
    private readonly Dictionary<Type, IComponent> _components = new();

    public void AddComponent(IComponent component)
    {
        _components[component.GetType()] = component;
    }

    public T Get<T>() where T : class, IComponent
    {
        return (T)_components[typeof(T)];
    }
}
