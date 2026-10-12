namespace WPFPhisics1._1.Physics;

/// <summary>
/// Гармонические колебания груза на пружине (горизонтально, без трения).
/// Закон Гука: F = −kx. Уравнение движения: ma = −kx ⇒ ω² = k/m, T = 2π√(m/k).
/// x(t) = x₀·cos(ωt) + (v₀/ω)·sin(ωt), v(t) = −x₀·ω·sin(ωt) + v₀·cos(ωt).
/// </summary>
public static class SpringOscillator
{
    /// <summary>Циклическая частота (рад/с): ω = √(k/m).</summary>
    public static double Omega(double k, double m)
    {
        if (m <= 0 || k < 0) return 0;
        return Math.Sqrt(k / m);
    }

    /// <summary>Период колебаний (с): T = 2π√(m/k).</summary>
    public static double Period(double k, double m)
    {
        double w = Omega(k, m);
        return w <= 0 ? 0 : 2 * Math.PI / w;
    }

    /// <summary>Частота (Гц): f = 1/T.</summary>
    public static double Frequency(double k, double m)
    {
        double t = Period(k, m);
        return t <= 0 ? 0 : 1 / t;
    }

    /// <summary>Смещение от положения равновесия в момент t (м).</summary>
    public static double PositionAt(double k, double m, double x0, double v0, double t)
    {
        double w = Omega(k, m);
        if (w <= 0) return x0;
        return x0 * Math.Cos(w * t) + (v0 / w) * Math.Sin(w * t);
    }

    /// <summary>Скорость в момент t (м/с).</summary>
    public static double VelocityAt(double k, double m, double x0, double v0, double t)
    {
        double w = Omega(k, m);
        if (w <= 0) return v0;
        return -x0 * w * Math.Sin(w * t) + v0 * Math.Cos(w * t);
    }

    /// <summary>Ускорение a = −(k/m)·x = −ω²·x (м/с²).</summary>
    public static double AccelerationAt(double k, double m, double x) => -(k / m) * x;

    /// <summary>Сила упругости F = −k·x (Н).</summary>
    public static double ForceAt(double k, double x) => -k * x;

    /// <summary>Точки графика x(t) за несколько периодов.</summary>
    public static List<(double T, double X)> GetPositionPoints(double k, double m, double x0, double v0, int periods = 2, int pointCount = 120)
    {
        var list = new List<(double T, double X)>();
        double period = Period(k, m);
        if (period <= 0 || pointCount < 2) return list;
        double tMax = periods * period;
        for (int i = 0; i < pointCount; i++)
        {
            double t = tMax * i / (pointCount - 1);
            list.Add((t, PositionAt(k, m, x0, v0, t)));
        }
        return list;
    }

    /// <summary>Точки графика v(t).</summary>
    public static List<(double T, double V)> GetVelocityPoints(double k, double m, double x0, double v0, int periods = 2, int pointCount = 120)
    {
        var list = new List<(double T, double V)>();
        double period = Period(k, m);
        if (period <= 0 || pointCount < 2) return list;
        double tMax = periods * period;
        for (int i = 0; i < pointCount; i++)
        {
            double t = tMax * i / (pointCount - 1);
            list.Add((t, VelocityAt(k, m, x0, v0, t)));
        }
        return list;
    }

    /// <summary>Точки графика a(t) = −ω²·x(t).</summary>
    public static List<(double T, double A)> GetAccelerationPoints(double k, double m, double x0, double v0, int periods = 2, int pointCount = 120)
    {
        var list = new List<(double T, double A)>();
        double period = Period(k, m);
        if (period <= 0 || pointCount < 2) return list;
        double tMax = periods * period;
        for (int i = 0; i < pointCount; i++)
        {
            double t = tMax * i / (pointCount - 1);
            double x = PositionAt(k, m, x0, v0, t);
            list.Add((t, AccelerationAt(k, m, x)));
        }
        return list;
    }
}
