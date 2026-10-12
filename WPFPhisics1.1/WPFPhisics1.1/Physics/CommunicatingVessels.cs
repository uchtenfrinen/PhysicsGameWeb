namespace WPFPhisics1._1.Physics;

/// <summary>
/// Сообщающиеся сосуды: закон гидростатики.
/// В равновесии в открытых сосудах с одной и той же жидкостью свободная поверхность устанавливается на одном уровне:
/// p₁ = p₂ на одной горизонтали ⇒ ρ·g·h₁ = ρ·g·h₂ ⇒ h₁ = h₂ (независимо от формы и площади сечения).
/// Объём жидкости: V = S₁·h + S₂·h = (S₁ + S₂)·h ⇒ h = V / (S₁ + S₂).
/// Гидростатическое давление на глубине h: p = ρ·g·h (избыточное над атмосферным).
/// </summary>
public static class CommunicatingVessels
{
    /// <summary>Ускорение свободного падения (м/с²).</summary>
    public const double G = 9.81;

    /// <summary>
    /// Высота уровня жидкости в обоих коленах (м).
    /// Из условия V = (S1 + S2)·h ⇒ h = V / (S1 + S2).
    /// </summary>
    public static double LevelHeight(double volume, double area1, double area2)
    {
        double totalArea = area1 + area2;
        if (totalArea <= 0 || volume < 0) return 0;
        return volume / totalArea;
    }

    /// <summary>Гидростатическое давление на глубине h (Па): p = ρ·g·h.</summary>
    public static double PressureAtDepth(double rho, double h, double g = G)
    {
        if (rho < 0 || h < 0) return 0;
        return rho * g * h;
    }

    /// <summary>Объём жидкости при заданной высоте уровня (м³): V = h·(S1 + S2).</summary>
    public static double VolumeFromLevel(double levelHeight, double area1, double area2)
    {
        if (levelHeight < 0) return 0;
        return levelHeight * (area1 + area2);
    }

    /// <summary>
    /// Две несмешивающиеся жидкости: на границе раздела давления равны.
    /// ρ₁·g·h₁ = ρ₂·g·h₂ ⇒ h₁/h₂ = ρ₂/ρ₁ (высоты обратно пропорциональны плотностям).
    /// </summary>
    /// <param name="rhoLight">Плотность лёгкой жидкости (над границей, напр. масло), кг/м³.</param>
    /// <param name="rhoHeavy">Плотность тяжёлой жидкости (под границей, напр. вода), кг/м³.</param>
    /// <param name="heightHeavy">Высота столба тяжёлой жидкости над границей раздела (м), напр. h_воды.</param>
    /// <returns>Высота столба лёгкой жидкости над границей (м), напр. h_масла.</returns>
    public static double HeightLightFromHeavy(double rhoLight, double rhoHeavy, double heightHeavy)
    {
        if (rhoLight <= 0 || heightHeavy < 0) return 0;
        return heightHeavy * (rhoHeavy / rhoLight);
    }

    /// <summary>Давление на границе раздела (Па): p = ρ_heavy·g·h_heavy = ρ_light·g·h_light.</summary>
    public static double PressureAtInterface(double rhoHeavy, double heightHeavy, double g = G)
    {
        return PressureAtDepth(rhoHeavy, heightHeavy, g);
    }
}
