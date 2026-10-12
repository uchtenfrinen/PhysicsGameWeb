namespace WPFPhisics1._1.Physics;

/// <summary>
/// Гармонические колебания в общем виде: x(t) = A·cos(ωt + φ₀).
/// v(t) = −Aω·sin(ωt + φ₀), a(t) = −ω²·x(t).
/// </summary>
public static class HarmonicOscillations
{
    /// <summary>Смещение x(t) = A·cos(ωt + φ₀).</summary>
    public static double PositionAt(double A, double omega, double phi0Degrees, double t)
    {
        double phi0 = phi0Degrees * Math.PI / 180.0;
        return A * Math.Cos(omega * t + phi0);
    }

    /// <summary>Скорость v(t) = −Aω·sin(ωt + φ₀).</summary>
    public static double VelocityAt(double A, double omega, double phi0Degrees, double t)
    {
        double phi0 = phi0Degrees * Math.PI / 180.0;
        return -A * omega * Math.Sin(omega * t + phi0);
    }

    /// <summary>Ускорение a(t) = −ω²·x(t).</summary>
    public static double AccelerationAt(double A, double omega, double phi0Degrees, double t)
    {
        double x = PositionAt(A, omega, phi0Degrees, t);
        return -omega * omega * x;
    }

    /// <summary>Период T = 2π/ω.</summary>
    public static double Period(double omega) => omega <= 0 ? 0 : 2 * Math.PI / omega;

    /// <summary>Частота f = ω/(2π) (Гц).</summary>
    public static double Frequency(double omega) => omega <= 0 ? 0 : omega / (2 * Math.PI);

    /// <summary>Точки графика x(t) за несколько периодов для построения.</summary>
    public static List<(double T, double X)> GetChartPoints(double A, double omega, double phi0Degrees, int periods = 2, int pointCount = 150)
    {
        var list = new List<(double T, double X)>();
        double period = Period(omega);
        if (period <= 0 || pointCount < 2) return list;
        double tMax = periods * period;
        for (int i = 0; i < pointCount; i++)
        {
            double t = tMax * i / (pointCount - 1);
            list.Add((t, PositionAt(A, omega, phi0Degrees, t)));
        }
        return list;
    }
}
