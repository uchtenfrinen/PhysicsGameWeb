namespace WPFPhisics1._1.Physics;

/// <summary>
/// Закон Ома для замкнутой цепи: I = ε/(r+R).
/// ε — ЭДС источника, r — внутреннее сопротивление, R — внешнее сопротивление.
/// Также формулы для участка цепи U = I·R и мощность P = I²·R.
/// </summary>
public static class OhmLaw
{
    // ——— Закон Ома для замкнутой цепи: I = ε / (r + R) ———

    /// <summary>Сила тока (А): I = ε/(r+R).</summary>
    public static double CurrentClosed(double emfV, double internalOhm, double externalOhm)
    {
        double sum = internalOhm + externalOhm;
        if (sum <= 0) return 0;
        return emfV / sum;
    }

    /// <summary>ЭДС (В): ε = I·(r+R).</summary>
    public static double Emf(double currentA, double internalOhm, double externalOhm)
    {
        if (currentA <= 0) return 0;
        return currentA * (internalOhm + externalOhm);
    }

    /// <summary>Внутреннее сопротивление (Ом): r = ε/I − R.</summary>
    public static double InternalResistance(double emfV, double currentA, double externalOhm)
    {
        if (currentA <= 0) return 0;
        return Math.Max(0, emfV / currentA - externalOhm);
    }

    /// <summary>Внешнее сопротивление (Ом): R = ε/I − r.</summary>
    public static double ExternalResistance(double emfV, double currentA, double internalOhm)
    {
        if (currentA <= 0) return 0;
        return Math.Max(0, emfV / currentA - internalOhm);
    }

    /// <summary>Напряжение на внешнем участке (В): U_R = I·R.</summary>
    public static double VoltageExternal(double currentA, double externalOhm) => currentA * externalOhm;

    /// <summary>Падение напряжения на внутреннем участке (В): U_r = I·r.</summary>
    public static double VoltageInternal(double currentA, double internalOhm) => currentA * internalOhm;

    /// <summary>Мощность на внешнем участке (Вт): P = I²·R.</summary>
    public static double PowerExternal(double currentA, double externalOhm) => currentA * currentA * externalOhm;

    // ——— Участок цепи (для справки): U = I·R ———

    public static double Voltage(double currentA, double resistanceOhm) => currentA * resistanceOhm;
    public static double Current(double voltageV, double resistanceOhm) => resistanceOhm <= 0 ? 0 : voltageV / resistanceOhm;
    public static double Resistance(double voltageV, double currentA) => currentA <= 0 ? 0 : voltageV / currentA;
    public static double Power(double voltageV, double currentA) => voltageV * currentA;
}
