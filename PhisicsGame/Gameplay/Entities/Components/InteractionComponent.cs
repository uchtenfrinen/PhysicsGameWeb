namespace PhisicsGame.Gameplay.Entities.Components;

public enum InteractionKind
{
    Task,
    Laboratory
}

public sealed class InteractionComponent : IComponent
{
    public InteractionComponent(float radius, string taskId, string objectId, InteractionKind kind = InteractionKind.Task)
    {
        Radius = radius;
        TaskId = taskId;
        ObjectId = objectId;
        Kind = kind;
    }

    public float Radius { get; }
    public string TaskId { get; }
    public string ObjectId { get; }
    public InteractionKind Kind { get; }
    public bool IsSolved { get; set; }
}
