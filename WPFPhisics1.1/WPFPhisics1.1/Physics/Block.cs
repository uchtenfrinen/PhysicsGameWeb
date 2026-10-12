namespace WPFPhisics1._1.Physics;

/// <summary>
/// Груз (блок) на плоскости: масса и позиция вдоль плоскости t ∈ [0, 1] (0 — низ, 1 — верх).
/// </summary>
public class Block : ISimulationObject
{
    private double _massKg = 2;
    private double _positionAlongPlane = 0.5;

    public double MassKg
    {
        get => _massKg;
        set => _massKg = Math.Clamp(value, 0.5, 50);
    }

    /// <summary>Позиция вдоль плоскости: 0 — низ, 1 — верх. Для перетаскивания допустимо ограничить, например 0.14–0.86.</summary>
    public double PositionAlongPlane
    {
        get => _positionAlongPlane;
        set => _positionAlongPlane = Math.Clamp(value, 0, 1);
    }

    /// <summary>Скорость вдоль плоскости (положительная — вниз). м/с (условные единицы вдоль оси).</summary>
    public double VelocityAlongPlane { get; set; }
}
