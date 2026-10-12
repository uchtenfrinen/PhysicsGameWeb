namespace WPFPhisics1._1.Physics;

/// <summary>
/// Преломление на границе двух сред. Закон Снеллиуса: n₁·sin θ₁ = n₂·sin θ₂.
/// Углы — от нормали к границе. Полное внутреннее отражение при n₁ > n₂ и θ₁ ≥ θ_крит.
/// </summary>
public static class RefractionAtBoundary
{
    /// <summary>Угол преломления (градусы). Луч идёт из среды n₁ в среду n₂. Возвращает NaN при полном отражении.</summary>
    public static double RefractionAngleDegrees(double n1, double n2, double incidenceAngleDegrees)
    {
        if (n1 <= 0 || n2 <= 0) return 0;
        double rad = incidenceAngleDegrees * Math.PI / 180.0;
        double sin2 = n1 * Math.Sin(rad) / n2;
        if (sin2 >= 1 || sin2 <= -1) return double.NaN; // полное внутреннее отражение
        return 180.0 * Math.Asin(sin2) / Math.PI;
    }

    /// <summary>Предельный угол полного внутреннего отражения (градусы): sin θ_крит = n₂/n₁. Только при n₁ > n₂.</summary>
    public static double? CriticalAngleDegrees(double n1, double n2)
    {
        if (n1 <= 0 || n2 <= 0 || n1 <= n2) return null;
        double sinCrit = n2 / n1;
        if (sinCrit > 1) return null;
        return 180.0 * Math.Asin(sinCrit) / Math.PI;
    }

    /// <summary>Полное внутреннее отражение: луч из n₁ в n₂, n₁ > n₂ и угол падения ≥ θ_крит.</summary>
    public static bool IsTotalInternalReflection(double n1, double n2, double incidenceAngleDegrees)
    {
        if (n1 <= n2) return false;
        var crit = CriticalAngleDegrees(n1, n2);
        if (crit == null) return false;
        return incidenceAngleDegrees >= crit.Value;
    }
}
