namespace PhysicsTasks.Models;

/// <summary>
/// Mutable answer snapshot passed to <see cref="PhysicsTask.Validate"/>.
/// </summary>
public sealed class TaskRuntimeState
{
    public double? NumericAnswer { get; set; }

    public int? SelectedChoiceIndex { get; set; }

    public double? SimulationValue { get; set; }
}
