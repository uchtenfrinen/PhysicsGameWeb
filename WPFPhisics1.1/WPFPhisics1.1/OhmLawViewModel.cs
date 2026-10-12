using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WPFPhisics1._1.Physics;
using WPFPhisics1._1.Services;

namespace WPFPhisics1._1;

/// <summary>Закон Ома для замкнутой цепи: I = ε/(r+R).</summary>
public class OhmLawViewModel : INotifyPropertyChanged
{
    private double _epsilonV = 12;      // ЭДС ε (В)
    private double _internalOhm = 0.5;   // внутреннее сопротивление r (Ом)
    private double _externalOhm = 23.5; // внешнее сопротивление R (Ом)
    private double _currentA;           // сила тока I (А) — вычисляется или задаётся

    private double _voltageExternal;  // U на внешнем участке
    private double _voltageInternal;  // падение на внутреннем
    private double _powerW;
    private string _computedHint = "";
    private string _explanationText = "";

    /// <summary>ЭДС источника ε (В). Ограничение: 0.5–50 В.</summary>
    public double EpsilonV
    {
        get => _epsilonV;
        set { _epsilonV = Math.Clamp(value, 0.5, 50); OnPropertyChanged(); Build(); }
    }

    /// <summary>Внутреннее сопротивление r (Ом). Ограничение: 0–20 Ом.</summary>
    public double InternalOhm
    {
        get => _internalOhm;
        set { _internalOhm = Math.Clamp(value, 0, 20); OnPropertyChanged(); Build(); }
    }

    /// <summary>Внешнее сопротивление R (Ом). Ограничение: 0.1–100 Ом.</summary>
    public double ExternalOhm
    {
        get => _externalOhm;
        set { _externalOhm = Math.Clamp(value, 0.1, 100); OnPropertyChanged(); Build(); }
    }

    /// <summary>Сила тока I (А).</summary>
    public double CurrentA
    {
        get => _currentA;
        set { _currentA = Math.Max(0, value); OnPropertyChanged(); }
    }

    public double VoltageExternal { get => _voltageExternal; set => Set(ref _voltageExternal, value); }
    public double VoltageInternal { get => _voltageInternal; set => Set(ref _voltageInternal, value); }
    public double PowerW { get => _powerW; set => Set(ref _powerW, value); }
    /// <summary>КПД цепи (%): η = U_R/ε · 100.</summary>
    public double EfficiencyPercent => EpsilonV > 0 ? 100.0 * VoltageExternal / EpsilonV : 0;
    public string ComputedHint { get => _computedHint; set => Set(ref _computedHint, value); }
    public string ExplanationText { get => _explanationText; set => Set(ref _explanationText, value); }

    public ICommand BuildCommand { get; }
    public ICommand ExplainCommand { get; }
    public ICommand ExplainGroqCommand { get; }

    public OhmLawViewModel()
    {
        BuildCommand = new RelayCommand(_ => Build());
        ExplainCommand = new RelayCommand(_ => ExplanationText = (EpsilonV > 0 && InternalOhm >= 0 && ExternalOhm > 0) || (CurrentA > 0 && InternalOhm >= 0 && ExternalOhm > 0) || (EpsilonV > 0 && CurrentA > 0 && ExternalOhm > 0) || (EpsilonV > 0 && CurrentA > 0 && InternalOhm >= 0) ? BuildExplanation() : GetSlideExplanationOnly());
        ExplainGroqCommand = new RelayCommand(_ => _ = ExplainGroqAsync());
    }

    public void Build()
    {
        double e = EpsilonV, r = InternalOhm, R = ExternalOhm, i = CurrentA;
        bool hasE = e > 0, hasR = R > 0, hasr = r >= 0, hasI = i > 0;
        int given = (hasE ? 1 : 0) + (hasR ? 1 : 0) + (hasr ? 1 : 0) + (hasI ? 1 : 0);

        if (given < 3)
        {
            ComputedHint = "Задайте три величины: ЭДС ε, внутреннее r, внешнее R или силу тока I.";
            ExplanationText = GetSlideExplanationOnly();
            return;
        }

        if (hasE && hasr && hasR && !hasI) { CurrentA = OhmLaw.CurrentClosed(e, r, R); ComputedHint = "Вычислено: I = ε/(r+R)."; }
        else if (hasI && hasr && hasR && !hasE) { EpsilonV = OhmLaw.Emf(i, r, R); ComputedHint = "Вычислено: ε = I·(r+R)."; }
        else if (hasE && hasI && hasR && !hasr) { InternalOhm = OhmLaw.InternalResistance(e, i, R); ComputedHint = "Вычислено: r = ε/I − R."; }
        else if (hasE && hasI && hasr && !hasR) { ExternalOhm = OhmLaw.ExternalResistance(e, i, r); ComputedHint = "Вычислено: R = ε/I − r."; }
        else { ComputedHint = "Проверка: I = ε/(r+R)."; }

        i = CurrentA;
        VoltageExternal = OhmLaw.VoltageExternal(i, ExternalOhm);
        VoltageInternal = OhmLaw.VoltageInternal(i, InternalOhm);
        PowerW = OhmLaw.PowerExternal(i, ExternalOhm);
        OnPropertyChanged(nameof(EfficiencyPercent));
        ExplanationText = BuildExplanation();
    }

    private async System.Threading.Tasks.Task ExplainGroqAsync()
    {
        ExplanationText = "Запрос к нейросети…";
        try
        {
            var prompt = $"Объясни кратко по-русски закон Ома для замкнутой цепи. " +
                         $"Сила тока в замкнутой цепи прямо пропорциональна ЭДС источника и обратно пропорциональна полному сопротивлению цепи. " +
                         $"Формула I = ε/(r+R). Параметры: ЭДС ε = {EpsilonV:F1} В, внутреннее сопротивление r = {InternalOhm:F2} Ом, внешнее R = {ExternalOhm:F1} Ом. " +
                         $"Сила тока I = {CurrentA:F3} А. Дай короткое объяснение.";
            var text = await GroqService.GetExplanationAsync(prompt);
            ExplanationText = text;
        }
        catch (Exception ex)
        {
            ExplanationText = "Ошибка: " + ex.Message;
        }
    }

    /// <summary>Текст со слайда: закон, формула и обозначения (без числового расчёта).</summary>
    private static string GetSlideExplanationOnly()
    {
        return "Закон Ома для замкнутой цепи\n\n" +
               "Сила тока в замкнутой цепи прямо пропорциональна ЭДС источника и обратно пропорциональна полному сопротивлению цепи.\n\n" +
               "Формула: I = ε / (r + R)\n\n" +
               "Обозначения:\n" +
               "• I — сила тока;\n" +
               "• R — сопротивление внешнего участка цепи;\n" +
               "• r — сопротивление внутреннего участка цепи;\n" +
               "• ε — ЭДС источника.\n\n" +
               "На схеме: участок ab – внутренний участок цепи (источник ε и сопротивление r); участок acdb – внешний участок цепи (сопротивление R). Клеммы a и b – у источника тока, c и d – у внешнего участка.\n\n" +
               "Задайте три величины (ε, r, R или I) и нажмите «Рассчитать» — четвёртая будет вычислена.";
    }

    private string BuildExplanation()
    {
        double e = EpsilonV, r = InternalOhm, R = ExternalOhm, i = CurrentA;
        return "Закон Ома для замкнутой цепи\n\n" +
               "Сила тока в замкнутой цепи прямо пропорциональна ЭДС источника и обратно пропорциональна полному сопротивлению цепи.\n\n" +
               "Формула: I = ε / (r + R)\n\n" +
               "Обозначения:\n" +
               "• I — сила тока;\n" +
               "• R — сопротивление внешнего участка цепи;\n" +
               "• r — сопротивление внутреннего участка цепи;\n" +
               "• ε — ЭДС источника.\n\n" +
               "На схеме: участок ab – внутренний участок цепи (источник ε и сопротивление r); участок acdb – внешний участок цепи (сопротивление R).\n\n" +
               "Расчёт для заданных параметров:\n" +
               $"ε = {e:F2} В, r = {r:F2} Ом, R = {R:F2} Ом.\n" +
               $"I = ε/(r+R) = {e:F2}/({r:F2}+{R:F2}) = {i:F3} А.\n\n" +
               $"Напряжение на внешнем участке: U_R = I·R = {VoltageExternal:F2} В. Падение на внутреннем участке: U_r = I·r = {VoltageInternal:F2} В (U_R + U_r = ε).\n" +
               $"Мощность на внешнем участке: P = I²·R = {PowerW:F2} Вт.";
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? prop = null)
    {
        if (Equals(field, value)) return;
        field = value;
        OnPropertyChanged(prop);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
