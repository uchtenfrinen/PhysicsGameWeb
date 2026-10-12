using PhisicsGame.Core.Tasks;

namespace PhisicsGame.Tasks;

public abstract class PhysicsTask
{
    public abstract string Id { get; }
    public abstract string TitleKey { get; }
    public abstract string DescriptionKey { get; }
    public abstract string FormulaKey { get; }
    public abstract string ImagePath { get; }
    public abstract TaskKind Kind { get; }
    public abstract string[] Choices { get; }
    public abstract float SimulationMin { get; }
    public abstract float SimulationMax { get; }
    public abstract float SimulationStep { get; }
    public abstract bool Validate(TaskRuntimeState state);

    /// <summary>
    /// Предпочтение лаборатории WPF: <see langword="null"/> = авто; <c>none</c> = отключить; иначе id установки.
    /// </summary>
    public abstract string? GetLabWpfSetupPreference();

    public static PhysicsTask FromDefinition(TaskDefinition definition)
    {
        return definition.Type.ToLowerInvariant() switch
        {
            "numeric" => new NumericPhysicsTask(definition),
            "choice" => new ChoicePhysicsTask(definition),
            "simulation" => new SimulationPhysicsTask(definition),
            _ => throw new InvalidOperationException($"Unknown task type: {definition.Type}")
        };
    }
}
