namespace WPFPhisics1._1.Physics;

/// <summary>
/// Расчёт сил и кинематики для груза на наклонной плоскости.
/// g = 9.81 м/с². Угол α в градусах.
/// </summary>
public static class ForceCalculator
{
    public const double G = 9.81;

    /// <summary>Сила тяжести F = m·g (Н).</summary>
    public static double Weight(double massKg) => massKg * G;

    /// <summary>Составляющая вдоль плоскости вниз F‖ = m·g·sin(α) (Н).</summary>
    public static double ForceAlongPlane(double massKg, double angleDegrees)
    {
        double rad = angleDegrees * Math.PI / 180.0;
        return massKg * G * Math.Sin(rad);
    }

    /// <summary>Нормальная реакция N = m·g·cos(α) (Н).</summary>
    public static double NormalForce(double massKg, double angleDegrees)
    {
        double rad = angleDegrees * Math.PI / 180.0;
        return massKg * G * Math.Cos(rad);
    }

    /// <summary>Сила трения F_тр = μ·N (Н).</summary>
    public static double FrictionForce(double frictionMu, double normalForce) => frictionMu * normalForce;

    /// <summary>Проверка покоя: F_тр ≥ F‖.</summary>
    public static bool IsAtRest(double forceAlongPlane, double frictionForce) => frictionForce >= forceAlongPlane;

    /// <summary>
    /// Ускорение вдоль плоскости (м/с²).
    /// При покое — 0; при скольжении вниз: a = g·(sin α − μ·cos α).
    /// </summary>
    public static double Acceleration(double massKg, double angleDegrees, double frictionMu)
    {
        double fAlong = ForceAlongPlane(massKg, angleDegrees);
        double n = NormalForce(massKg, angleDegrees);
        double fTr = FrictionForce(frictionMu, n);
        if (IsAtRest(fAlong, fTr)) return 0;
        return (fAlong - fTr) / massKg;
    }

    /// <summary>Все величины для отображения (Н, Н, Н, Н, м/с², покой/скольжение).</summary>
    public static ForceResult Compute(double massKg, double angleDegrees, double frictionMu)
    {
        double f = Weight(massKg);
        double fAlong = ForceAlongPlane(massKg, angleDegrees);
        double n = NormalForce(massKg, angleDegrees);
        double fTr = FrictionForce(frictionMu, n);
        bool atRest = IsAtRest(fAlong, fTr);
        double a = atRest ? 0 : (fAlong - fTr) / massKg;
        return new ForceResult(f, n, fAlong, fTr, a, atRest);
    }

    public readonly record struct ForceResult(
        double WeightN,
        double NormalN,
        double ForceAlongPlaneN,
        double FrictionN,
        double AccelerationMps2,
        bool IsAtRest
    );
}
