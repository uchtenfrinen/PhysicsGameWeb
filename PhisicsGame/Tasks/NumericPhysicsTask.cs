using PhisicsGame.Core.Tasks;

namespace PhisicsGame.Tasks;

public sealed class NumericPhysicsTask(TaskDefinition definition) : PhysicsTask
{
    public override string Id => definition.Id;
    public override string TitleKey => definition.TitleKey;
    public override string DescriptionKey => definition.DescriptionKey;
    public override string FormulaKey => definition.FormulaKey;
    public override string ImagePath => definition.ImagePath;
    public override TaskKind Kind => TaskKind.Numeric;
    public override string[] Choices => [];
    public override float SimulationMin => 0f;
    public override float SimulationMax => 0f;
    public override float SimulationStep => 0f;

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
        if (!definition.ExpectedNumeric.HasValue)
        {
            return false;
        }

        return double.TryParse(state.NumericInput.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture, out var value)
               && Math.Abs(value - definition.ExpectedNumeric.Value) <= definition.NumericTolerance;
    }
}
