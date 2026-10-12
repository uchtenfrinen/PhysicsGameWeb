using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using WPFPhisics1._1.Physics;
using WPFPhisics1._1.Services;

namespace WPFPhisics1._1;

/// <summary>Гармонические колебания: x(t) = A·cos(ωt + φ₀). Параметры A, T, φ₀.</summary>
public class HarmonicOscillationsViewModel : INotifyPropertyChanged
{
    private readonly DispatcherTimer _animationTimer;
    private const double AnimDt = 0.02;

    private double _amplitude = 1.0;
    private double _period = 2.0;
    private double _phi0Degrees = 0;

    private double _omega;
    private double _frequency;
    private string _explanationText = "";

    public double Amplitude
    {
        get => _amplitude;
        set { _amplitude = Math.Max(0.01, value); OnPropertyChanged(); }
    }

    public double Period
    {
        get => _period;
        set { _period = Math.Max(0.1, value); OnPropertyChanged(); }
    }

    public double Phi0Degrees
    {
        get => _phi0Degrees;
        set { _phi0Degrees = value; OnPropertyChanged(); }
    }

    public double Omega { get => _omega; set => Set(ref _omega, value); }
    public double Frequency { get => _frequency; set => Set(ref _frequency, value); }
    public double CurrentAngularSpeed => Omega;
    public ObservableCollection<System.Windows.Point> ChartPoints { get; } = new();

    private bool _isPlaying;
    public bool IsPlaying { get => _isPlaying; set { Set(ref _isPlaying, value); OnPropertyChanged(nameof(PlayButtonText)); } }

    private double _currentTime;
    public double CurrentTime { get => _currentTime; set => Set(ref _currentTime, value); }

    private double _currentX;
    public double CurrentX { get => _currentX; set => Set(ref _currentX, value); }

    private double _currentV;
    public double CurrentV { get => _currentV; set => Set(ref _currentV, value); }

    private double _currentA;
    public double CurrentA { get => _currentA; set => Set(ref _currentA, value); }

    public string PlayButtonText => IsPlaying ? "Стоп" : "Запустить анимацию";
    public string ExplanationText { get => _explanationText; set => Set(ref _explanationText, value); }

    public ICommand BuildCommand { get; }
    public ICommand PlayAnimationCommand { get; }
    public ICommand ExplainCommand { get; }
    public ICommand ExplainGroqCommand { get; }

    public HarmonicOscillationsViewModel()
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
        CurrentX = HarmonicOscillations.PositionAt(Amplitude, Omega, Phi0Degrees, CurrentTime);
        CurrentV = HarmonicOscillations.VelocityAt(Amplitude, Omega, Phi0Degrees, CurrentTime);
        CurrentA = HarmonicOscillations.AccelerationAt(Amplitude, Omega, Phi0Degrees, CurrentTime);
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
            CurrentX = HarmonicOscillations.PositionAt(Amplitude, Omega, Phi0Degrees, 0);
            CurrentV = HarmonicOscillations.VelocityAt(Amplitude, Omega, Phi0Degrees, 0);
            CurrentA = HarmonicOscillations.AccelerationAt(Amplitude, Omega, Phi0Degrees, 0);
            IsPlaying = true;
            _animationTimer.Start();
        }
    }

    public void Build()
    {
        Omega = 2 * Math.PI / Period;
        OnPropertyChanged(nameof(CurrentAngularSpeed));
        Frequency = HarmonicOscillations.Frequency(Omega);
        var points = HarmonicOscillations.GetChartPoints(Amplitude, Omega, Phi0Degrees, 2, 120);
        ChartPoints.Clear();
        foreach (var p in points)
            ChartPoints.Add(new System.Windows.Point(p.T, p.X));
        OnPropertyChanged(nameof(ChartPoints));
        CurrentTime = 0;
        CurrentX = HarmonicOscillations.PositionAt(Amplitude, Omega, Phi0Degrees, 0);
        CurrentV = HarmonicOscillations.VelocityAt(Amplitude, Omega, Phi0Degrees, 0);
        CurrentA = HarmonicOscillations.AccelerationAt(Amplitude, Omega, Phi0Degrees, 0);
        ExplanationText = BuildExplanation();
    }

    private async System.Threading.Tasks.Task ExplainGroqAsync()
    {
        ExplanationText = "Запрос к нейросети…";
        try
        {
            var prompt = $"Объясни кратко по-русски гармонические колебания. Уравнение x(t) = A·cos(ωt + φ₀). " +
                         $"Параметры: амплитуда A = {Amplitude:F2}, период T = {Period:F2} с, начальная фаза φ₀ = {Phi0Degrees:F0}°. " +
                         $"Циклическая частота ω = 2π/T = {Omega:F2} рад/с, частота f = {Frequency:F2} Гц. Связь v(t) и a(t) с x(t).";
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
        return $"Гармонические колебания: x(t) = A·cos(ωt + φ₀).\n\n" +
               $"A = {Amplitude:F2} м, T = {Period:F2} с ⇒ ω = 2π/T = {Omega:F2} рад/с, f = {Frequency:F2} Гц. Начальная фаза φ₀ = {Phi0Degrees:F0}°.\n\n" +
               $"Скорость: v(t) = −Aω·sin(ωt + φ₀). Ускорение: a(t) = −ω²·x(t) = −ω²·x.";
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
