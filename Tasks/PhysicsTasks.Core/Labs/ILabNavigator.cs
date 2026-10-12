namespace PhysicsTasks.Labs;

/// <summary>
/// Opens a WPF lab page when the host wires real navigation; default stub does nothing.
/// </summary>
public interface ILabNavigator
{
    /// <summary>Returns <c>true</c> if a lab window or page was opened.</summary>
    bool TryOpenLab(string? labWpfSetupId);
}

public sealed class StubLabNavigator : ILabNavigator
{
    public bool TryOpenLab(string? labWpfSetupId) => false;
}
