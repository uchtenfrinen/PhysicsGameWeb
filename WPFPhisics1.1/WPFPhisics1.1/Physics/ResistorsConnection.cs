namespace WPFPhisics1._1.Physics;

/// <summary>
/// Соединение резисторов: последовательное и параллельное.
/// Последовательно: R_общ = R₁ + R₂ + R₃, I = U/R_общ, Uₖ = I·Rₖ.
/// Параллельно: 1/R_общ = 1/R₁ + 1/R₂ + 1/R₃, I = U/R_общ, Iₖ = U/Rₖ.
/// </summary>
public static class ResistorsConnection
{
    /// <summary>Эквивалентное сопротивление при последовательном соединении (Ом).</summary>
    public static double SeriesEquivalent(double r1, double r2, double r3)
    {
        return Math.Max(0, r1) + Math.Max(0, r2) + Math.Max(0, r3);
    }

    /// <summary>Эквивалентное сопротивление при параллельном соединении (Ом).</summary>
    public static double ParallelEquivalent(double r1, double r2, double r3)
    {
        double s = 0;
        if (r1 > 0) s += 1 / r1;
        if (r2 > 0) s += 1 / r2;
        if (r3 > 0) s += 1 / r3;
        return s > 0 ? 1 / s : 0;
    }

    /// <summary>Сила тока при заданном напряжении (А). I = U / R.</summary>
    public static double Current(double voltageV, double resistanceOhm)
    {
        if (resistanceOhm <= 0) return 0;
        return voltageV / resistanceOhm;
    }

    /// <summary>Мощность (Вт). P = U·I = U²/R.</summary>
    public static double Power(double voltageV, double currentA)
    {
        return voltageV * currentA;
    }

    /// <summary>Напряжение на резисторе при последовательном соединении (В). Uₖ = I·Rₖ.</summary>
    public static double VoltageDropSeries(double currentA, double resistanceOhm)
    {
        return currentA * Math.Max(0, resistanceOhm);
    }

    /// <summary>Ток через ветвь при параллельном соединении (А). Iₖ = U / Rₖ.</summary>
    public static double BranchCurrentParallel(double voltageV, double resistanceOhm)
    {
        if (resistanceOhm <= 0) return 0;
        return voltageV / resistanceOhm;
    }

    // Два резистора (для обратной совместимости)
    public static double SeriesEquivalent(double r1, double r2) => Math.Max(0, r1) + Math.Max(0, r2);
    public static double ParallelEquivalent(double r1, double r2)
    {
        if (r1 <= 0 && r2 <= 0) return 0;
        if (r1 <= 0) return Math.Max(0, r2);
        if (r2 <= 0) return Math.Max(0, r1);
        return r1 * r2 / (r1 + r2);
    }
}
