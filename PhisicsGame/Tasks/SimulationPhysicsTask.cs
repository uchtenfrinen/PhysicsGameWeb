using PhisicsGame.Core.Tasks;

namespace PhisicsGame.Tasks;

public sealed class SimulationPhysicsTask(TaskDefinition definition) : PhysicsTask
{
    public override string Id => definition.Id;
    public override string TitleKey => definition.TitleKey;
    public override string DescriptionKey => definition.DescriptionKey;
    public override string FormulaKey => definition.FormulaKey;
    public override string ImagePath => definition.ImagePath;
    public override TaskKind Kind => TaskKind.Simulation;
    public override string[] Choices => [];
    public override float SimulationMin => 0f;
    public override float SimulationMax => 20f;
    public override float SimulationStep => 0.5f;

    public override string? GetLabWpfSetupPreference()
    {
        var v = definition.LabWpfSetupId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(v))
        {
            return null;
        }

        return string.Equals(v, "none", StringComparison.OrdinalIgnoreCase) ? "none" : v;
    }

    public override bool Validate(TaskRuntimeState state)
    {
        if (!definition.SimulationTarget.HasValue)
        {
            return false;
        }

        return Math.Abs(state.SimulationValue - definition.SimulationTarget.Value) < 0.01f;
    }
}
