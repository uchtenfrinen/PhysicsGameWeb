using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WPFPhisics1._1.Physics;
using WPFPhisics1._1.Services;

namespace WPFPhisics1._1;

public class IdealGasViewModel : INotifyPropertyChanged
{
    private readonly DispatcherTimer _animationTimer;
    private const double AnimPeriod = 4.0; // секунд на цикл (V_min → V_max → V_min)
    private const double AnimDt = 0.03;

    /// <summary>Количество вещества (моль).</summary>
    private double _n = 1;
    /// <summary>Температура (К).</summary>
    private double _temperature = 300;
    /// <summary>Начальный объём (м³).</summary>
    private double _volume = 0.025;

    private double _pressure;
    private double _currentV;
    private double _currentP;
    private double _animTime;
    private bool _isPlaying;
    private string _explanationText = "";

    public double N
    {
        get => _n;
        set { _n = Math.Clamp(value, 0.01, 1000); OnPropertyChanged(); }
    }

    public double Temperature
    {
        get => _temperature;
        set { _temperature = Math.Clamp(value, 1, 10000); OnPropertyChanged(); }
    }

    public double Volume
    {
        get => _volume;
        set { _volume = Math.Clamp(value, 1e-6, 100); OnPropertyChanged(); }
    }

    public double Pressure { get => _pressure; set => Set(ref _pressure, value); }
    public double CurrentV { get => _currentV; set => Set(ref _currentV, value); }
    public double CurrentP { get => _currentP; set => Set(ref _currentP, value); }
    public bool IsPlaying { get => _isPlaying; set { Set(ref _isPlaying, value); OnPropertyChanged(nameof(PlayButtonText)); } }
    public string ExplanationText { get => _explanationText; set => Set(ref _explanationText, value); }
    public string PlayButtonText => IsPlaying ? "Стоп" : "Запустить анимацию";

    public ObservableCollection<Point> IsothermPoints { get; } = new();
    public ICommand BuildCommand { get; }
    public ICommand PlayAnimationCommand { get; }
    public ICommand ExplainCommand { get; }
    public ICommand ExplainGroqCommand { get; }

    public double VMin => Volume * 0.4;
    public double VMax => Volume * 1.6;

    public IdealGasViewModel()
    {
        BuildCommand = new RelayCommand(_ => Build());
        PlayAnimationCommand = new RelayCommand(_ => ToggleAnimation());
        ExplainCommand = new RelayCommand(_ => ExplanationText = BuildExplanation());
        ExplainGroqCommand = new RelayCommand(_ => _ = ExplainGroqAsync());
        _animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
        _animationTimer.Tick += AnimationTimer_Tick;
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        _animTime += AnimDt;
        double phase = (_animTime % AnimPeriod) / AnimPeriod; // 0..1 за цикл
        // Плавное изменение V по синусу: V_min → V_max → V_min
        double t = Math.Sin(phase * Math.PI * 2);
        double V = (VMin + VMax) / 2 + (VMax - VMin) / 2 * t;
        CurrentV = Math.Clamp(V, VMin, VMax);
        CurrentP = IdealGas.Pressure(N, Temperature, CurrentV);
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
            if (IsothermPoints.Count < 2) Build();
            _animTime = 0;
            CurrentV = Volume;
            CurrentP = IdealGas.Pressure(N, Temperature, Volume);
            IsPlaying = true;
            _animationTimer.Start();
        }
    }

    public void Build()
    {
        Pressure = IdealGas.Pressure(N, Temperature, Volume);
        CurrentV = Volume;
        CurrentP = Pressure;

        var points = IdealGas.IsothermPoints(N, Temperature, VMin, VMax);
        IsothermPoints.Clear();
        foreach (var p in points)
            IsothermPoints.Add(new Point(p.V, p.P));
        OnPropertyChanged(nameof(IsothermPoints));

        ExplanationText = BuildExplanation();
    }

    private string BuildExplanation()
    {
        return $"Уравнение состояния идеального газа (Менделеева — Клапейрона): PV = νRT.\n\n" +
               $"R = {IdealGas.R:F3} Дж/(моль·К). Параметры: ν = {N:G4} моль, T = {Temperature:F0} К, V = {Volume:G4} м³.\n\n" +
               $"Давление: P = νRT/V = {Pressure:F1} Па.\n\n" +
               $"Анимация — изотермический процесс (T = const): закон Бойля — Мариотта PV = const. " +
               $"При движении поршня объём меняется от {VMin:G4} до {VMax:G4} м³, давление пересчитывается по формуле P = νRT/V.";
    }

    private async System.Threading.Tasks.Task ExplainGroqAsync()
    {
        ExplanationText = "Запрос к нейросети…";
        try
        {
            var prompt = $"Объясни кратко по-русски уравнение состояния идеального газа (Менделеева — Клапейрона). " +
                         $"Параметры: ν = {N:G4} моль, T = {Temperature:F0} К, V = {Volume:G4} м³. Давление P = {Pressure:F1} Па. " +
                         "Упомяни PV = νRT и изотермический процесс (Бойль — Мариотт). Дай короткое объяснение.";
            var text = await GroqService.GetExplanationAsync(prompt);
            ExplanationText = text;
        }
        catch (Exception ex)
        {
            ExplanationText = "Ошибка: " + ex.Message;
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
