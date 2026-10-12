namespace WPFPhisics1._1.Physics;

/// <summary>
/// Заряд в однородном электрическом поле (аналог брошенного тела).
/// Поле направлено вниз (как g): a_y = −q·E/m. x(t) = v₀·cos α·t, y(t) = v₀·sin α·t + a_y·t²/2.
/// </summary>
public static class ChargeInField
{
    /// <summary>Ускорение по модулю (м/с²): a = |q|·E/m.</summary>
    public static double AccelerationMagnitude(double q, double E, double m)
    {
        if (m <= 0 || E < 0) return 0;
        return Math.Abs(q) * E / m;
    }

    /// <summary>Вертикальная составляющая ускорения (м/с²): a_y = −q·E/m (поле вниз).</summary>
    public static double AccelerationY(double q, double E, double m)
    {
        if (m <= 0 || E < 0) return 0;
        return -q * E / m;
    }

    /// <summary>Время полёта до возврата на уровень y = 0. T = −2·v₀·sin α / a_y.</summary>
    public static double TimeOfFlight(double v0, double angleDegrees, double q, double E, double m)
    {
        if (v0 <= 0 || m <= 0 || E <= 0) return 0;
        double aY = AccelerationY(q, E, m);
        if (Math.Abs(aY) < 1e-12) return 0;
        double rad = angleDegrees * Math.PI / 180.0;
        return -2 * v0 * Math.Sin(rad) / aY;
    }

    /// <summary>Дальность по горизонтали (м).</summary>
    public static double Range(double v0, double angleDegrees, double q, double E, double m)
    {
        double rad = angleDegrees * Math.PI / 180.0;
        double t = TimeOfFlight(v0, angleDegrees, q, E, m);
        return v0 * Math.Cos(rad) * t;
    }

    /// <summary>Максимальная высота над уровнем y = 0 (м).</summary>
    public static double MaxHeight(double v0, double angleDegrees, double q, double E, double m)
    {
        if (m <= 0 || E <= 0) return 0;
        double aY = AccelerationY(q, E, m);
        double rad = angleDegrees * Math.PI / 180.0;
        double v0y = v0 * Math.Sin(rad);
        return (v0y * v0y) / (2 * Math.Abs(aY));
    }

    /// <summary>Позиция в момент t (x, y) в метрах. Старт в (0, 0).</summary>
    public static (double X, double Y) PositionAt(double v0, double angleDegrees, double q, double E, double m, double t)
    {
        double rad = angleDegrees * Math.PI / 180.0;
        double aY = AccelerationY(q, E, m);
        double x = v0 * Math.Cos(rad) * t;
        double y = v0 * Math.Sin(rad) * t + 0.5 * aY * t * t;
        return (x, y);
    }

    /// <summary>Точки траектории от 0 до времени полёта.</summary>
    public static List<(double X, double Y)> GetTrajectoryPoints(double v0, double angleDegrees, double q, double E, double m, int pointCount = 80)
    {
        var list = new List<(double X, double Y)>();
        if (v0 <= 0 || m <= 0 || E <= 0 || pointCount < 2) return list;
        double tMax = TimeOfFlight(v0, angleDegrees, q, E, m);
        if (tMax <= 0) return list;
        for (int i = 0; i < pointCount; i++)
        {
            double t = tMax * i / (pointCount - 1);
            list.Add(PositionAt(v0, angleDegrees, q, E, m, t));
        }
        return list;
    }
}
