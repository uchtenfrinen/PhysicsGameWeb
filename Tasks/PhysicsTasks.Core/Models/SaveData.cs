namespace PhysicsTasks.Models;

public sealed class SaveData
{
    public HashSet<string> CompletedTaskIds { get; } = new(StringComparer.Ordinal);
}
