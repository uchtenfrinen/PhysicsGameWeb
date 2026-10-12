namespace WPFPhisics1._1.Physics;

/// <summary>
/// Движение тела, брошенного под углом к горизонту.
/// Без сопротивления воздуха: x(t) = v0·cos(α)·t, y(t) = y0 + v0·sin(α)·t − g·t²/2.
/// </summary>
public static class ProjectileMotion
{
    public const double G = 9.81;

    /// <summary>Время полёта до падения на землю y=0 (с). При y0=0: T = 2v₀·sin α/g; при y0≠0: T = (v₀y + √(v₀y² + 2gy₀))/g.</summary>
    public static double TimeOfFlight(double v0, double angleDegrees, double y0 = 0)
    {
        if (v0 <= 0) return 0;
        double rad = angleDegrees * Math.PI / 180.0;
        double v0y = v0 * Math.Sin(rad);
        if (y0 <= 0)
            return 2 * v0y / G;
        // y(t) = y0 + v0y*t - g*t²/2 = 0 => t = (v0y + √(v0y² + 2*g*y0)) / g (положительный корень — момент приземления)
        double disc = v0y * v0y + 2 * G * y0;
        if (disc < 0) return 0;
        return (v0y + Math.Sqrt(disc)) / G;
    }

    /// <summary>Дальность полёта (м).</summary>
    public static double Range(double v0, double angleDegrees, double y0 = 0)
    {
        double rad = angleDegrees * Math.PI / 180.0;
        double t = TimeOfFlight(v0, angleDegrees, y0);
        return v0 * Math.Cos(rad) * t;
    }

    /// <summary>Максимальная высота (м).</summary>
    public static double MaxHeight(double v0, double angleDegrees, double y0 = 0)
    {
        double rad = angleDegrees * Math.PI / 180.0;
        double v0y = v0 * Math.Sin(rad);
        return y0 + (v0y * v0y) / (2 * G);
    }

    /// <summary>Позиция в момент t (x, y) в метрах.</summary>
    public static (double X, double Y) PositionAt(double v0, double angleDegrees, double t, double y0 = 0)
    {
        double rad = angleDegrees * Math.PI / 180.0;
        double x = v0 * Math.Cos(rad) * t;
        double y = y0 + v0 * Math.Sin(rad) * t - G * t * t / 2;
        return (x, y);
    }

    /// <summary>Скорость в момент t: vx = v₀·cos α, vy = v₀·sin α − g·t. Возвращает (vx, vy) в м/с.</summary>
    public static (double Vx, double Vy) VelocityAt(double v0, double angleDegrees, double t)
    {
        double rad = angleDegrees * Math.PI / 180.0;
        double vx = v0 * Math.Cos(rad);
        double vy = v0 * Math.Sin(rad) - G * t;
        return (vx, vy);
    }

    /// <summary>Точки траектории от 0 до времени полёта (для построения графика).</summary>
    public static List<(double X, double Y)> GetTrajectoryPoints(double v0, double angleDegrees, double y0, int pointCount = 80)
    {
        var list = new List<(double X, double Y)>();
        if (v0 <= 0 || pointCount < 2) return list;
        double tMax = TimeOfFlight(v0, angleDegrees, y0);
        for (int i = 0; i < pointCount; i++)
        {
            double t = tMax * i / (pointCount - 1);
            list.Add(PositionAt(v0, angleDegrees, t, y0));
        }
        return list;
    }
}
