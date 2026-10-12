using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using WPFPhisics1._1.Physics;
using WPFPhisics1._1.Services;

namespace WPFPhisics1._1;

public class SpringOscillatorViewModel : INotifyPropertyChanged
{
    private readonly DispatcherTimer _animationTimer;
    private const double AnimDt = 0.02;

    private double _k = 50;
    private double _m = 1.0;
    private double _x0 = 0.2;
    private double _v0 = 0;
    private double _damping = 0.5;

    public double K
    {
        get => _k;
        set { _k = Math.Clamp(value, 0.1, 2000); OnPropertyChanged(); Build(); }
    }

    public double M
    {
        get => _m;
        set { _m = Math.Clamp(value, 0.01, 100); OnPropertyChanged(); Build(); }
    }

    public double X0
    {
        get => _x0;
        set { _x0 = Math.Clamp(value, -2, 2); OnPropertyChanged(); Build(); }
    }

    public double V0
    {
        get => _v0;
        set { _v0 = Math.Clamp(value, -20, 20); OnPropertyChanged(); Build(); }
    }

    /// <summary>Демпфирование (коэффициент затухания ζ для отображения).</summary>
    public double Damping
    {
        get => _damping;
        set { _damping = Math.Clamp(value, 0, 1); OnPropertyChanged(); OnPropertyChanged(nameof(Zeta)); Build(); }
    }

    /// <summary>ζ для отображения в результатах (равен Damping).</summary>
    public double Zeta => _damping;

    private double _period;
    public double Period { get => _period; set => Set(ref _period, value); }

    private double _omega;
    public double Omega { get => _omega; set => Set(ref _omega, value); }

    private double _frequency;
    public double Frequency { get => _frequency; set => Set(ref _frequency, value); }

    public ObservableCollection<System.Windows.Point> ChartPoints { get; } = new();
    public ObservableCollection<System.Windows.Point> VelocityChartPoints { get; } = new();
    public ObservableCollection<System.Windows.Point> AccelerationChartPoints { get; } = new();

    private bool _isPlaying;
    public bool IsPlaying { get => _isPlaying; set { Set(ref _isPlaying, value); OnPropertyChanged(nameof(PlayButtonText)); } }

    private double _currentTime;
    public double CurrentTime { get => _currentTime; set => Set(ref _currentTime, value); }

    private double _currentX;
    public double CurrentX { get => _currentX; set => Set(ref _currentX, value); }

    private string _explanationText = "";
    public string ExplanationText { get => _explanationText; set => Set(ref _explanationText, value); }

    public string PlayButtonText => IsPlaying ? "■ Остановить симуляцию" : "▶ Запустить симуляцию";

    public ICommand BuildCommand { get; }
    public ICommand PlayAnimationCommand { get; }
    public ICommand ExplainCommand { get; }
    public ICommand ExplainGroqCommand { get; }

    public SpringOscillatorViewModel()
    {
        BuildCommand = new RelayCommand(_ => Build());
        PlayAnimationCommand = new RelayCommand(_ => ToggleAnimation());
        ExplainCommand = new RelayCommand(_ => ExplanationText = BuildExplanation());
        ExplainGroqCommand = new RelayCommand(_ => _ = ExplainGroqAsync());
        _animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
        _animationTimer.Tick += AnimationTimer_Tick;
        Build();
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        CurrentTime += AnimDt;
        CurrentX = SpringOscillator.PositionAt(K, M, X0, V0, CurrentTime);
    }

    private void ToggleAnimation()
    {
        if (IsPlaying)
        {
            _animationTimer.Stop();
            IsPlaying = false;
        }
        else
        {
            if (ChartPoints.Count < 2) Build();
            CurrentTime = 0;
            CurrentX = X0;
            IsPlaying = true;
            _animationTimer.Start();
        }
    }

    public void Build()
    {
        Omega = SpringOscillator.Omega(K, M);
        Period = SpringOscillator.Period(K, M);
        Frequency = SpringOscillator.Frequency(K, M);

        var points = SpringOscillator.GetPositionPoints(K, M, X0, V0, 2);
        ChartPoints.Clear();
        foreach (var p in points)
            ChartPoints.Add(new System.Windows.Point(p.T, p.X));
        OnPropertyChanged(nameof(ChartPoints));

        var vPoints = SpringOscillator.GetVelocityPoints(K, M, X0, V0, 2);
        VelocityChartPoints.Clear();
        foreach (var p in vPoints)
            VelocityChartPoints.Add(new System.Windows.Point(p.T, p.V));
        OnPropertyChanged(nameof(VelocityChartPoints));

        var aPoints = SpringOscillator.GetAccelerationPoints(K, M, X0, V0, 2);
        AccelerationChartPoints.Clear();
        foreach (var p in aPoints)
            AccelerationChartPoints.Add(new System.Windows.Point(p.T, p.A));
        OnPropertyChanged(nameof(AccelerationChartPoints));

        CurrentTime = 0;
        CurrentX = X0;
        ExplanationText = BuildExplanation();
    }

    private async System.Threading.Tasks.Task ExplainGroqAsync()
    {
        ExplanationText = "Запрос к нейросети…";
        try
        {
            var prompt = $"Объясни кратко по-русски гармонические колебания груза на пружине с демпфированием (коэффициент ζ = {Damping:F2}). " +
                         $"Параметры: жёсткость k = {K:F1} Н/м, масса m = {M:F2} кг, начальное смещение x₀ = {X0:F2} м, начальная скорость v₀ = {V0:F2} м/с. " +
                         $"Период T = {Period:F2} с, частота f = {Frequency:F2} Гц, ω = {Omega:F2} рад/с. " +
                         "Дай короткое объяснение с формулами (Гук, ω² = k/m, x(t)).";
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
        return $"Колебания груза на пружине. Жёсткость k = {K:F1} Н/м, масса m = {M:F2} кг.\n\n" +
               $"Закон Гука: F = −k·x. Уравнение движения: m·a = −k·x ⇒ ω² = k/m.\n\n" +
               $"Циклическая частота: ω = √(k/m) = {Omega:F2} рад/с.\n" +
               $"Период: T = 2π√(m/k) = {Period:F2} с.\n" +
               $"Частота: f = 1/T = {Frequency:F2} Гц.\n\n" +
               $"Начальные условия: x₀ = {X0:F2} м, v₀ = {V0:F2} м/с.\n" +
               $"Смещение: x(t) = x₀·cos(ωt) + (v₀/ω)·sin(ωt).";
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
