using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WPFPhisics1._1.Physics;
using WPFPhisics1._1.Services;

namespace WPFPhisics1._1;

/// <summary>Установка «Резисторы: последовательное и параллельное соединение» — панель управления, расчёты, объяснение.</summary>
public class ResistorsConnectionViewModel : INotifyPropertyChanged
{
    private double _voltageV = 12;
    private double _r1 = 4;
    private double _r2 = 6;
    private double _r3 = 12;
    private bool _isSeries = true;

    private double _rTotal;
    private double _currentA;
    private double _u1;
    private double _u2;
    private double _u3;
    private double _i1;
    private double _i2;
    private double _i3;
    private double _powerW;
    private string _explanationText = "";
    private string _givenText = "";
    private string _solutionText = "";
    private string _conclusionText = "";
    private double _barWidthU;
    private double _barWidthI;
    private double _barWidthR;
    private double _barWidthP;

    /// <summary>Напряжение источника U (В). 0–20 В.</summary>
    public double VoltageV
    {
        get => _voltageV;
        set { _voltageV = Math.Clamp(value, 0, 20); OnPropertyChanged(); Build(); }
    }

    public double R1
    {
        get => _r1;
        set { _r1 = Math.Clamp(value, 0.1, 100); OnPropertyChanged(); Build(); }
    }
    public double R2
    {
        get => _r2;
        set { _r2 = Math.Clamp(value, 0.1, 100); OnPropertyChanged(); Build(); }
    }
    public double R3
    {
        get => _r3;
        set { _r3 = Math.Clamp(value, 0.1, 100); OnPropertyChanged(); Build(); }
    }

    /// <summary>true — последовательное, false — параллельное.</summary>
    public bool IsSeries
    {
        get => _isSeries;
        set { _isSeries = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsParallel)); Build(); }
    }
    public bool IsParallel => !_isSeries;

    public double RTotal { get => _rTotal; set => Set(ref _rTotal, value); }
    public double CurrentA { get => _currentA; set => Set(ref _currentA, value); }
    public double U1 { get => _u1; set => Set(ref _u1, value); }
    public double U2 { get => _u2; set => Set(ref _u2, value); }
    public double U3 { get => _u3; set => Set(ref _u3, value); }
    public double I1 { get => _i1; set => Set(ref _i1, value); }
    public double I2 { get => _i2; set => Set(ref _i2, value); }
    public double I3 { get => _i3; set => Set(ref _i3, value); }
    public double PowerW { get => _powerW; set => Set(ref _powerW, value); }
    public string ExplanationText { get => _explanationText; set => Set(ref _explanationText, value); }
    public string GivenText { get => _givenText; set => Set(ref _givenText, value); }
    public string SolutionText { get => _solutionText; set => Set(ref _solutionText, value); }
    public string ConclusionText { get => _conclusionText; set => Set(ref _conclusionText, value); }
    /// <summary>Ширина столбца для диаграммы (0–120). Масштаб: U 20 В, I 2 А, R 50 Ом, P 50 Вт.</summary>
    public double BarWidthU { get => _barWidthU; set => Set(ref _barWidthU, value); }
    public double BarWidthI { get => _barWidthI; set => Set(ref _barWidthI, value); }
    public double BarWidthR { get => _barWidthR; set => Set(ref _barWidthR, value); }
    public double BarWidthP { get => _barWidthP; set => Set(ref _barWidthP, value); }

    public ICommand BuildCommand { get; }
    public ICommand ExplainCommand { get; }
    public ICommand ExplainGroqCommand { get; }

    public ResistorsConnectionViewModel()
    {
        BuildCommand = new RelayCommand(_ => Build());
        ExplainCommand = new RelayCommand(_ =>
        {
            ExplanationText = BuildExplanation();
            BuildExplanationBlocks();
        });
        ExplainGroqCommand = new RelayCommand(_ => _ = ExplainGroqAsync());
        Build();
    }

    public void Build()
    {
        if (IsSeries)
        {
            RTotal = ResistorsConnection.SeriesEquivalent(R1, R2, R3);
            CurrentA = ResistorsConnection.Current(VoltageV, RTotal);
            U1 = ResistorsConnection.VoltageDropSeries(CurrentA, R1);
            U2 = ResistorsConnection.VoltageDropSeries(CurrentA, R2);
            U3 = ResistorsConnection.VoltageDropSeries(CurrentA, R3);
            I1 = I2 = I3 = CurrentA;
            PowerW = ResistorsConnection.Power(VoltageV, CurrentA);
        }
        else
        {
            RTotal = ResistorsConnection.ParallelEquivalent(R1, R2, R3);
            CurrentA = ResistorsConnection.Current(VoltageV, RTotal);
            U1 = U2 = U3 = VoltageV;
            I1 = ResistorsConnection.BranchCurrentParallel(VoltageV, R1);
            I2 = ResistorsConnection.BranchCurrentParallel(VoltageV, R2);
            I3 = ResistorsConnection.BranchCurrentParallel(VoltageV, R3);
            PowerW = ResistorsConnection.Power(VoltageV, CurrentA);
        }
        const double barMax = 120;
        BarWidthU = Math.Min(barMax, VoltageV / 20.0 * barMax);
        BarWidthI = Math.Min(barMax, CurrentA / 2.0 * barMax);
        BarWidthR = Math.Min(barMax, RTotal / 50.0 * barMax);
        BarWidthP = Math.Min(barMax, PowerW / 50.0 * barMax);
        ExplanationText = BuildExplanation();
        BuildExplanationBlocks();
    }

    private string BuildExplanation()
    {
        if (IsSeries)
            return $"Последовательное соединение: R = R1 + R2 + R3 = {R1:F1} + {R2:F1} + {R3:F1} = {RTotal:F2} Ом. " +
                   $"I = U/R = {VoltageV:F1}/{RTotal:F2} = {CurrentA:F3} А. " +
                   $"U1 = I·R1 = {U1:F2} В, U2 = {U2:F2} В, U3 = {U3:F2} В. P = U·I = {PowerW:F2} Вт.";
        return $"Параллельное соединение: 1/R = 1/R1 + 1/R2 + 1/R3 ⇒ R = {RTotal:F2} Ом. " +
               $"U = {VoltageV:F1} В на всех ветвях. I1 = U/R1 = {I1:F3} А, I2 = {I2:F3} А, I3 = {I3:F3} А. " +
               $"I = I1 + I2 + I3 = {CurrentA:F3} А. P = U·I = {PowerW:F2} Вт.";
    }

    private async System.Threading.Tasks.Task ExplainGroqAsync()
    {
        ExplanationText = "Запрос к нейросети…";
        SolutionText = "Запрос к нейросети…";
        ConclusionText = "Ожидаем ответ.";
        try
        {
            var prompt = "Объясни кратко по-русски последовательное и параллельное соединение резисторов. " +
                         "Последовательно: I = I1 = I2 = I3, U = U1 + U2 + U3, R = R1 + R2 + R3. " +
                         "Параллельно: I = I1 + I2 + I3, U = U1 = U2 = U3, 1/R = 1/R1 + 1/R2 + 1/R3. Дай короткое объяснение.";
            var text = await GroqService.GetExplanationAsync(prompt);
            ExplanationText = text;
            GivenText = $"U = {VoltageV:F2} В; R1 = {R1:F2} Ом; R2 = {R2:F2} Ом; R3 = {R3:F2} Ом. Режим: {(IsSeries ? "последовательный" : "параллельный")}.";
            SolutionText = text;
            ConclusionText = "Объяснение сгенерировано нейросетью.";
        }
        catch (Exception ex)
        {
            ExplanationText = "Ошибка: " + ex.Message;
            SolutionText = "Не удалось получить объяснение от нейросети.";
            ConclusionText = "Проверь подключение к интернету и корректность API-ключа Groq.";
        }
    }

    private void BuildExplanationBlocks()
    {
        GivenText = $"U = {VoltageV:F2} В; R1 = {R1:F2} Ом; R2 = {R2:F2} Ом; R3 = {R3:F2} Ом. Режим: {(IsSeries ? "последовательный" : "параллельный")}.";
        if (IsSeries)
        {
            SolutionText =
                $"Rобщ = R1 + R2 + R3 = {R1:F2} + {R2:F2} + {R3:F2} = {RTotal:F2} Ом.\n" +
                $"I = U/Rобщ = {VoltageV:F2}/{RTotal:F2} = {CurrentA:F3} А.\n" +
                $"U1 = I·R1 = {U1:F2} В, U2 = {U2:F2} В, U3 = {U3:F2} В.\n" +
                $"P = U·I = {PowerW:F2} Вт.";
            ConclusionText = "При последовательном соединении общее сопротивление больше каждого из резисторов, ток везде одинаков.";
        }
        else
        {
            SolutionText =
                $"1/Rобщ = 1/R1 + 1/R2 + 1/R3 ⇒ Rобщ = {RTotal:F2} Ом.\n" +
                $"I1 = U/R1 = {I1:F3} А, I2 = {I2:F3} А, I3 = {I3:F3} А.\n" +
                $"Iобщ = I1 + I2 + I3 = {CurrentA:F3} А.\n" +
                $"P = U·Iобщ = {PowerW:F2} Вт.";
            ConclusionText = "При параллельном соединении напряжение на всех ветвях одинаковое, а общее сопротивление меньше наименьшего из резисторов.";
        }
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
