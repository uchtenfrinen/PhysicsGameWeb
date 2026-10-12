namespace WPFPhisics1._1.Physics;

/// <summary>
/// Уравнение состояния идеального газа (Менделеева — Клапейрона): PV = νRT.
/// R = 8,314 Дж/(моль·К). Изотерма (T = const): закон Бойля — Мариотта PV = const.
/// </summary>
public static class IdealGas
{
    /// <summary>Универсальная газовая постоянная R (Дж/(моль·К)).</summary>
    public const double R = 8.314;

    /// <summary>Давление (Па): P = nRT/V.</summary>
    public static double Pressure(double n, double T, double V)
    {
        if (V <= 0 || n < 0 || T < 0) return 0;
        return n * R * T / V;
    }

    /// <summary>Объём (м³): V = nRT/P.</summary>
    public static double Volume(double n, double T, double P)
    {
        if (P <= 0 || n < 0 || T < 0) return 0;
        return n * R * T / P;
    }

    /// <summary>Температура (К): T = PV/(nR).</summary>
    public static double Temperature(double P, double V, double n)
    {
        if (n <= 0 || V <= 0) return 0;
        return P * V / (n * R);
    }

    /// <summary>При изотермическом процессе (T = const): P(V) = nRT/V — гипербола. Точки для графика P-V.</summary>
    public static List<(double V, double P)> IsothermPoints(double n, double T, double VMin, double VMax, int count = 80)
    {
        var list = new List<(double V, double P)>();
        if (VMin <= 0 || VMax <= VMin || count < 2) return list;
        for (int i = 0; i < count; i++)
        {
            double V = VMin + (VMax - VMin) * i / (count - 1);
            list.Add((V, Pressure(n, T, V)));
        }
        return list;
    }
}
