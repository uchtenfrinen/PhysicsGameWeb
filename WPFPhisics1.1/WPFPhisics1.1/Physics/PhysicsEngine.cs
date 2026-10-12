using System;

namespace WPFPhisics1._1.Physics;

/// <summary>
/// Движок симуляции: плоскость, груз, трение. Обновляет позицию и скорость по таймеру.
/// </summary>
public class PhysicsEngine
{
    public InclinedPlane Plane { get; }
    public Block Block { get; }
    public double FrictionMu { get; set; }

    public PhysicsEngine(InclinedPlane plane, Block block, double frictionMu)
    {
        Plane = plane;
        Block = block;
        FrictionMu = Math.Clamp(frictionMu, 0, 1);
    }

    /// <summary>Текущее ускорение вдоль плоскости (м/с²).</summary>
    public double Acceleration =>
        ForceCalculator.Acceleration(Block.MassKg, Plane.AngleDegrees, FrictionMu);

    /// <summary>Текущие силы и состояние (для отображения).</summary>
    public ForceCalculator.ForceResult CurrentForces =>
        ForceCalculator.Compute(Block.MassKg, Plane.AngleDegrees, FrictionMu);

    /// <summary>
    /// Минимальная позиция t (низ плоскости), при достижении симуляция останавливается.
    /// </summary>
    public const double MinT = 0.14;

    /// <summary>
    /// Один шаг симуляции за время dt (секунды).
    /// Обновляет скорость и позицию; вниз — уменьшение t.
    /// </summary>
    public void Step(double dt)
    {
        double a = Acceleration;
        Block.VelocityAlongPlane += a * dt;
        double t = Block.PositionAlongPlane;
        t -= Block.VelocityAlongPlane * dt; // вниз — уменьшение t
        t = Math.Max(MinT, t);
        Block.PositionAlongPlane = t;
    }

    /// <summary>Груз достиг низа (симуляцию можно остановить).</summary>
    public bool ReachedBottom => Block.PositionAlongPlane <= MinT;
}
