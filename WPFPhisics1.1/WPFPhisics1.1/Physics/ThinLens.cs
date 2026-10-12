namespace WPFPhisics1._1.Physics;

/// <summary>
/// Тонкая собирающая линза. Формула линзы: 1/f = 1/d + 1/d'.
/// d — расстояние от предмета до линзы, d' — от линзы до изображения, f — фокусное расстояние.
/// Увеличение Г = −d'/d: действительное изображение перевёрнутое (Г < 0), мнимое — прямое (Г > 0).
/// </summary>
public static class ThinLens
{
    /// <summary>Расстояние от линзы до изображения. d' = f·d/(d−f). Если d &lt; f — мнимое (d' &lt; 0).</summary>
    public static double ImageDistance(double d, double f)
    {
        if (f <= 0 || d <= 0) return 0;
        if (Math.Abs(d - f) < 1e-10) return double.NaN; // d = f -> бесконечность
        return f * d / (d - f);
    }

    /// <summary>Линейное увеличение Г = −d'/d. Перевёрнутое (действительное): Г &lt; 0, прямое (мнимое): Г &gt; 0.</summary>
    public static double Magnification(double d, double f)
    {
        double dPrime = ImageDistance(d, f);
        if (double.IsNaN(dPrime)) return 0;
        return -dPrime / d;
    }

    /// <summary>Модуль увеличения |Г|.</summary>
    public static double MagnificationAbs(double d, double f) => Math.Abs(Magnification(d, f));

    /// <summary>Высота изображения (положительная). h' = |Г|·h.</summary>
    public static double ImageHeight(double objectHeight, double d, double f)
    {
        return objectHeight * MagnificationAbs(d, f);
    }

    /// <summary>Изображение перевёрнутое (действительное).</summary>
    public static bool IsInverted(double d, double f) => Magnification(d, f) < 0;

    /// <summary>Изображение действительное (d' &gt; 0) или мнимое (d' &lt; 0).</summary>
    public static bool IsRealImage(double d, double f)
    {
        return d > f;
    }
}
