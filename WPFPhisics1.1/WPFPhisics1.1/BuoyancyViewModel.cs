using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using WPFPhisics1._1.Physics;
using WPFPhisics1._1.Services;

namespace WPFPhisics1._1;

public class BuoyancyViewModel : INotifyPropertyChanged
{
    private double _rhoBody = 600;
    private double _rhoFluid = 1000;
    private double _volume = 0.001;

    private double _archimedesForce;
    private double _weight;
    private double _submergedFraction;
    private double _currentSubmergedFraction;
    private string _conditionText = "";
    private string _statusInequality = "";
    private string _explanationText = "";
    private readonly DispatcherTimer _animationTimer;
    private const double AnimStep = 0.03;

    public double RhoBody
    {
        get => _rhoBody;
        set { _rhoBody = Math.Clamp(value, 1, 20000); OnPropertyChanged(); Build(); }
    }

    public double RhoFluid
    {
        get => _rhoFluid;
        set { _rhoFluid = Math.Clamp(value, 1, 20000); OnPropertyChanged(); Build(); }
    }

    /// <summary>Объём тела (м³).</summary>
    public double Volume
    {
        get => _volume;
        set { _volume = Math.Clamp(value, 1e-6, 100); OnPropertyChanged(); Build(); }
    }

    public double ArchimedesForce { get => _archimedesForce; set => Set(ref _archimedesForce, value); }
    public double Weight { get => _weight; set => Set(ref _weight, value); }
    public double SubmergedFraction { get => _submergedFraction; set => Set(ref _submergedFraction, value); }
    /// <summary>Текущая доля погружения (для анимации движения тела к равновесию).</summary>
    public double CurrentSubmergedFraction { get => _currentSubmergedFraction; set => Set(ref _currentSubmergedFraction, value); }
    public string ConditionText { get => _conditionText; set => Set(ref _conditionText, value); }
    /// <summary>Неравенство для статуса, например «ρт &lt; ρж».</summary>
    public string StatusInequality { get => _statusInequality; set => Set(ref _statusInequality, value); }
    public string ExplanationText { get => _explanationText; set => Set(ref _explanationText, value); }

    public ICommand BuildCommand { get; }
    public ICommand ExplainCommand { get; }
    public ICommand ExplainGroqCommand { get; }

    public BuoyancyViewModel()
    {
        BuildCommand = new RelayCommand(_ => Build());
        ExplainCommand = new RelayCommand(_ => ExplanationText = BuildExplanation());
        ExplainGroqCommand = new RelayCommand(_ => _ = ExplainGroqAsync());
        _animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(25) };
        _animationTimer.Tick += AnimationTimer_Tick;
        _currentSubmergedFraction = 0;
        Build();
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        double target = SubmergedFraction;
        double current = CurrentSubmergedFraction;
        double diff = target - current;
        if (Math.Abs(diff) < 0.002) { CurrentSubmergedFraction = target; _animationTimer.Stop(); return; }
        CurrentSubmergedFraction = current + Math.Sign(diff) * Math.Min(AnimStep, Math.Abs(diff));
    }

    /// <summary>
    /// Установить текущую долю погружения от пользовательского перетаскивания куба.
    /// После этого таймер снова запустится и вернёт тело к равновесию в соответствии с силами (F_A и P).
    /// </summary>
    public void SetCurrentSubmergedFromCanvas(double value)
    {
        CurrentSubmergedFraction = Math.Clamp(value, 0, 1);
        _animationTimer.Stop();
        _animationTimer.Start();
    }

    public void Build()
    {
        double g = Buoyancy.G;
        SubmergedFraction = Buoyancy.SubmergedFraction(RhoBody, RhoFluid);
        double vSub = Volume * SubmergedFraction;
        ArchimedesForce = Buoyancy.ArchimedesForce(RhoFluid, vSub, g);
        Weight = Buoyancy.Weight(RhoBody, Volume, g);

        if (Buoyancy.IsSinking(RhoBody, RhoFluid))
        {
            ConditionText = "Тело тонет (ρ_тела > ρ_жидкости)";
            StatusInequality = "ρт > ρж";
        }
        else if (Buoyancy.IsFloating(RhoBody, RhoFluid))
        {
            ConditionText = "Куб плавает";
            StatusInequality = "ρт < ρж";
        }
        else
        {
            ConditionText = "Безразличное равновесие";
            StatusInequality = "ρт = ρж";
        }

        _animationTimer.Stop();
        _animationTimer.Start();
        ExplanationText = BuildExplanation();
    }

    private async System.Threading.Tasks.Task ExplainGroqAsync()
    {
        ExplanationText = "Запрос к нейросети…";
        try
        {
            var prompt = $"Объясни кратко по-русски закон Архимеда и условия плавания. " +
                         $"Параметры: плотность тела ρ_т = {RhoBody:F0} кг/м³, плотность жидкости ρ_ж = {RhoFluid:F0} кг/м³, объём тела V = {Volume:G4} м³. " +
                         $"Вес P = {Weight:F2} Н, сила Архимеда F_A = {ArchimedesForce:F2} Н, доля погружения = {SubmergedFraction:P1}. {ConditionText}. " +
                         "Дай короткое объяснение с формулами.";
            var text = await GroqService.GetExplanationAsync(prompt);
            ExplanationText = text;
        }
        catch (Exception ex)
        {
            ExplanationText = "Ошибка: " + ex.Message;
        }
    }

    private string BuildExplanation()
    {
        double vSub = Volume * SubmergedFraction;
        bool sink = Buoyancy.IsSinking(RhoBody, RhoFluid);
        string equilibriumPart = sink
            ? "Тело тонет: ρ_т > ρ_ж, поэтому F_A < P. Погружённый объём равен полному объёму тела (V_погр = V)."
            : $"При равновесии F_A = P, откуда ρ_ж · V_погр = ρ_т · V ⇒ доля погружённого объёма V_погр/V = ρ_т/ρ_ж = {SubmergedFraction:P1}. У тела постоянного сечения такая же доля высоты находится ниже уровня жидкости.";
        return $"Закон Архимеда: на тело, погружённое в жидкость, действует выталкивающая сила F_A = ρ_ж · g · V_погр (V_погр — объём погружённой части).\n\n" +
               $"Параметры: ρ_т = {RhoBody:F0} кг/м³, ρ_ж = {RhoFluid:F0} кг/м³, V = {Volume:G4} м³, g = {Buoyancy.G:F2} м/с².\n\n" +
               $"Вес тела: P = ρ_т · g · V = {Weight:F2} Н.\n" +
               $"Сила Архимеда: F_A = ρ_ж · g · V_погр = {ArchimedesForce:F2} Н (V_погр = {vSub:G4} м³).\n\n" +
               equilibriumPart + "\n\n" + ConditionText + ".";
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
