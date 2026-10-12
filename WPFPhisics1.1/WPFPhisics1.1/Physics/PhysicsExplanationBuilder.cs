using System.Globalization;

namespace WPFPhisics1._1.Physics;

/// <summary>
/// Формирует структурированное текстовое объяснение для груза на наклонной плоскости.
/// </summary>
public static class PhysicsExplanationBuilder
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static string Build(double massKg, double angleDegrees, double frictionMu)
    {
        var r = ForceCalculator.Compute(massKg, angleDegrees, frictionMu);
        double f = r.WeightN;
        double n = r.NormalN;
        double fAlong = r.ForceAlongPlaneN;
        double fTr = r.FrictionN;
        double a = r.AccelerationMps2;
        bool atRest = r.IsAtRest;

        var sb = new System.Text.StringBuilder();

        sb.AppendLine("—— Силы и формулы ——");
        sb.AppendLine();
        sb.AppendLine($"Масса груза: m = {massKg.ToString("0.##", Invariant)} кг.");
        sb.AppendLine($"Угол наклона плоскости: α = {angleDegrees.ToString("0.#", Invariant)}°.");
        sb.AppendLine($"Коэффициент трения: μ = {frictionMu.ToString("0.##", Invariant)}.");
        sb.AppendLine();
        sb.AppendLine("Сила тяжести: F = m·g.");
        sb.AppendLine($"  F = {massKg.ToString("0.##", Invariant)} · 9,81 = {f.ToString("0.#", Invariant)} Н (направлена вниз).");
        sb.AppendLine();
        sb.AppendLine("Нормальная реакция: N = m·g·cos(α).");
        sb.AppendLine($"  N = {n.ToString("0.#", Invariant)} Н (перпендикулярна плоскости).");
        sb.AppendLine();
        sb.AppendLine("Составляющая силы тяжести вдоль плоскости: m·g·sin(α). Из-за неё брусок движется вниз.");
        sb.AppendLine($"  m·g·sin α = {fAlong.ToString("0.#", Invariant)} Н.");
        sb.AppendLine();
        sb.AppendLine("Сила трения: F_тр = μ·N (против движения, вверх по плоскости).");
        sb.AppendLine($"  F_тр = {fTr.ToString("0.#", Invariant)} Н.");
        sb.AppendLine();

        sb.AppendLine("—— Условие покоя ——");
        sb.AppendLine();
        sb.AppendLine("Груз остаётся в покое, если F_тр ≥ m·g·sin α (трение компенсирует движущую силу).");
        sb.AppendLine("Груз скользит вниз, если m·g·sin α > F_тр.");
        sb.AppendLine();
        if (atRest)
        {
            sb.AppendLine($"У нас: F_тр = {fTr.ToString("0.#", Invariant)} Н ≥ m·g·sin α = {fAlong.ToString("0.#", Invariant)} Н.");
            sb.AppendLine("Вывод: груз в покое, ускорение a = 0.");
        }
        else
        {
            sb.AppendLine($"У нас: m·g·sin α = {fAlong.ToString("0.#", Invariant)} Н > F_тр = {fTr.ToString("0.#", Invariant)} Н.");
            sb.AppendLine("Вывод: груз скользит вниз по плоскости под действием составляющей m·g·sin α.");
        }
        sb.AppendLine();

        sb.AppendLine("—— Второй закон Ньютона ——");
        sb.AppendLine();
        sb.AppendLine("Вдоль плоскости: m·a = m·g·sin α − F_тр (вниз — положительное направление).");
        if (atRest)
            sb.AppendLine("В покое: a = 0 м/с².");
        else
            sb.AppendLine($"  a = (m·g·sin α − F_тр) / m = ({fAlong.ToString("0.#", Invariant)} − {fTr.ToString("0.#", Invariant)}) / {massKg.ToString("0.##", Invariant)} = {a.ToString("0.##", Invariant)} м/с².");
        sb.AppendLine();
        sb.AppendLine("—— Итог ——");
        sb.AppendLine();
        sb.AppendLine($"F тяжести: {f.ToString("0.#", Invariant)} Н.");
        sb.AppendLine($"N: {n.ToString("0.#", Invariant)} Н.");
        sb.AppendLine($"m·g·sin α (движущая сила): {fAlong.ToString("0.#", Invariant)} Н.");
        sb.AppendLine($"F трения: {fTr.ToString("0.#", Invariant)} Н.");
        sb.AppendLine($"Ускорение: {a.ToString("0.##", Invariant)} м/с².");
        sb.AppendLine(atRest ? "Состояние: покой." : "Состояние: скольжение вниз.");

        return sb.ToString();
    }
}
