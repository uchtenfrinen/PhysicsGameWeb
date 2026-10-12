namespace WPFPhisics1._1.Physics;

/// <summary>
/// Плавание тела: закон Архимеда и условия равновесия.
/// F_A = ρ_ж · g · V_погр (выталкивающая сила равна весу вытесненной жидкости).
/// P = ρ_т · g · V (вес тела).
/// При плавании в равновесии: F_A = P ⇒ ρ_ж · V_погр = ρ_т · V ⇒ V_погр/V = ρ_т/ρ_ж.
/// Для тела с постоянной площадью сечения по высоте (параллелепипед, цилиндр) доля погружённого объёма равна доле погружённой высоты.
/// При ρ_т &gt; ρ_ж тело тонет (V_погр = V, сила Архимеда &lt; P).
/// </summary>
public static class Buoyancy
{
    /// <summary>Ускорение свободного падения (м/с²).</summary>
    public const double G = 9.81;

    /// <summary>Сила Архимеда (Н): F_A = ρ_fluid * g * V_submerged.</summary>
    public static double ArchimedesForce(double rhoFluid, double volumeSubmerged, double g = G)
    {
        if (rhoFluid < 0 || volumeSubmerged < 0) return 0;
        return rhoFluid * g * volumeSubmerged;
    }

    /// <summary>Вес тела (Н): P = ρ_body * g * V.</summary>
    public static double Weight(double rhoBody, double volume, double g = G)
    {
        if (rhoBody < 0 || volume <= 0) return 0;
        return rhoBody * g * volume;
    }

    /// <summary>
    /// Доля погружённого объёма при равновесии (плавание).
    /// При ρ_т ≤ ρ_ж: V_погр/V = ρ_т/ρ_ж (тело плавает).
    /// При ρ_т > ρ_ж: тело тонет, V_погр = V (доля = 1).
    /// </summary>
    public static double SubmergedFraction(double rhoBody, double rhoFluid)
    {
        if (rhoFluid <= 0) return 1;
        return Math.Clamp(rhoBody / rhoFluid, 0, 1);
    }

    /// <summary>Тело плавает (ρ_тела &lt; ρ_жидкости).</summary>
    public static bool IsFloating(double rhoBody, double rhoFluid) => rhoFluid > 0 && rhoBody < rhoFluid;

    /// <summary>Тело тонет (ρ_тела &gt; ρ_жидкости).</summary>
    public static bool IsSinking(double rhoBody, double rhoFluid) => rhoFluid > 0 && rhoBody > rhoFluid;

    /// <summary>Тело в безразличном равновесии внутри жидкости (ρ_тела = ρ_жидкости).</summary>
    public static bool IsNeutralEquilibrium(double rhoBody, double rhoFluid) =>
        rhoFluid > 0 && Math.Abs(rhoBody - rhoFluid) < 1e-9;
}
