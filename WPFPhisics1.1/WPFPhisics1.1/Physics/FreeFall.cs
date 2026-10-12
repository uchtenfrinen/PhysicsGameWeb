namespace WPFPhisics1._1.Physics;

/// <summary>
/// Свободное падение тела без начальной скорости (v₀ = 0).
/// Ось Y направлена вверх: h(t) = h₀ − g·t²/2, v(t) = −g·t.
/// </summary>
public static class FreeFall
{
    public const double G = 9.81;

    /// <summary>Время падения до земли (с): T = √(2h₀/g).</summary>
    public static double TimeOfFall(double height0)
    {
        if (height0 <= 0) return 0;
        return Math.Sqrt(2 * height0 / G);
    }

    /// <summary>Скорость в момент приземления (м/с), направлена вниз: v = √(2g·h₀) = g·T.</summary>
    public static double FinalSpeed(double height0)
    {
        if (height0 <= 0) return 0;
        return Math.Sqrt(2 * G * height0);
    }

    /// <summary>Высота в момент t (м): h(t) = h₀ − g·t²/2.</summary>
    public static double HeightAt(double t, double height0)
    {
        if (t <= 0) return height0;
        double h = height0 - G * t * t / 2;
        return Math.Max(0, h);
    }

    /// <summary>Скорость в момент t (м/с), вниз положительна: v(t) = g·t.</summary>
    public static double VelocityAt(double t)
    {
        if (t < 0) return 0;
        return G * t;
    }

    /// <summary>Точки для графика h(t) от 0 до T.</summary>
    public static List<(double T, double H)> GetHeightPoints(double height0, int pointCount = 60)
    {
        var list = new List<(double T, double H)>();
        if (height0 <= 0 || pointCount < 2) return list;
        double tMax = TimeOfFall(height0);
        for (int i = 0; i < pointCount; i++)
        {
            double t = tMax * i / (pointCount - 1);
            list.Add((t, HeightAt(t, height0)));
        }
        list.Add((tMax, 0));
        return list;
    }

    /// <summary>Точки для графика v(t) от 0 до T.</summary>
    public static List<(double T, double V)> GetVelocityPoints(double height0, int pointCount = 60)
    {
        var list = new List<(double T, double V)>();
        if (height0 <= 0 || pointCount < 2) return list;
        double tMax = TimeOfFall(height0);
        for (int i = 0; i < pointCount; i++)
        {
            double t = tMax * i / (pointCount - 1);
            list.Add((t, VelocityAt(t)));
        }
        list.Add((tMax, FinalSpeed(height0)));
        return list;
    }
}
