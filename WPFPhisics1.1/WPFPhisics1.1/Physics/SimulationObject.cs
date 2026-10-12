namespace WPFPhisics1._1.Physics;

/// <summary>
/// Базовый контракт для объекта симуляции (позиция вдоль плоскости).
/// </summary>
public interface ISimulationObject
{
    double PositionAlongPlane { get; set; }
    double VelocityAlongPlane { get; set; }
}
