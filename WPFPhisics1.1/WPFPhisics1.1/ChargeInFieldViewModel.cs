using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using WPFPhisics1._1.Physics;
using WPFPhisics1._1.Services;

namespace WPFPhisics1._1;

public class ChargeInFieldViewModel : INotifyPropertyChanged
{
    private readonly DispatcherTimer _animationTimer;
    private const double AnimDt = 0.02;

    private double _q = 1e-6;   // Кл
    private double _e = 100;    // В/м
    private double _m = 1e-6;   // кг
    private double _v0 = 20;
    private double _angleDegrees = 45;

    public double Q
    {
        get => _q;
        set { _q = value; OnPropertyChanged(); }
    }

    public double E
    {
        get => _e;
        set { _e = Math.Max(0.1, value); OnPropertyChanged(); }
    }

    public double M
    {
        get => _m;
        set { _m = Math.Max(1e-9, value); OnPropertyChanged(); }
    }

    public double V0
    {
        get => _v0;
        set { _v0 = Math.Clamp(value, 0.1, 500); OnPropertyChanged(); }
    }

    public double AngleDegrees
    {
        get => _angleDegrees;
        set { _angleDegrees = Math.Clamp(value, 1, 89); OnPropertyChanged(); }
    }

    private double _acceleration;
    public double Acceleration { get => _acceleration; set => Set(ref _acceleration, value); }

    private double _range;
    public double Range { get => _range; set => Set(ref _range, value); }

    private double _maxHeight;
    public double MaxHeight { get => _maxHeight; set => Set(ref _maxHeight, value); }

    private double _timeOfFlight;
    public double TimeOfFlight { get => _timeOfFlight; set => Set(ref _timeOfFlight, value); }

    public ObservableCollection<System.Windows.Point> TrajectoryPoints { get; } = new();

    private string _explanationText = "";
    public string ExplanationText { get => _explanationText; set => Set(ref _explanationText, value); }

    private bool _isPlaying;
    public bool IsPlaying { get => _isPlaying; set { Set(ref _isPlaying, value); OnPropertyChanged(nameof(PlayButtonText)); } }

    private double _currentTime;
    public double CurrentTime { get => _currentTime; set => Set(ref _currentTime, value); }

    private double _currentX;
    public double CurrentX { get => _currentX; set => Set(ref _currentX, value); }

    private double _currentY;
    public double CurrentY { get => _currentY; set => Set(ref _currentY, value); }

    public string PlayButtonText => IsPlaying ? "Стоп" : "Запустить анимацию";

    public ICommand BuildCommand { get; }
    public ICommand PlayAnimationCommand { get; }
    public ICommand ExplainCommand { get; }
    public ICommand ExplainGroqCommand { get; }

    public ChargeInFieldViewModel()
    {
        BuildCommand = new RelayCommand(_ => Build());
        PlayAnimationCommand = new RelayCommand(_ => ToggleAnimation());
        ExplainCommand = new RelayCommand(_ => ExplanationText = BuildExplanation());
        ExplainGroqCommand = new RelayCommand(_ => _ = ExplainGroqAsync());
        _animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
        _animationTimer.Tick += AnimationTimer_Tick;
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        CurrentTime += AnimDt;
        var (x, y) = ChargeInField.PositionAt(V0, AngleDegrees, Q, E, M, CurrentTime);
        CurrentX = x;
        CurrentY = y;
        if (CurrentTime >= TimeOfFlight || CurrentY < 0)
        {
            _animationTimer.Stop();
            IsPlaying = false;
        }
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
            if (TrajectoryPoints.Count < 2) Build();
            CurrentTime = 0;
            CurrentX = 0;
            CurrentY = 0;
            IsPlaying = true;
            _animationTimer.Start();
        }
    }

    public void Build()
    {
        var points = ChargeInField.GetTrajectoryPoints(V0, AngleDegrees, Q, E, M);
        TrajectoryPoints.Clear();
        foreach (var p in points)
            TrajectoryPoints.Add(new System.Windows.Point(p.X, p.Y));

        Acceleration = ChargeInField.AccelerationMagnitude(Q, E, M);
        Range = ChargeInField.Range(V0, AngleDegrees, Q, E, M);
        MaxHeight = ChargeInField.MaxHeight(V0, AngleDegrees, Q, E, M);
        TimeOfFlight = ChargeInField.TimeOfFlight(V0, AngleDegrees, Q, E, M);
        OnPropertyChanged(nameof(TrajectoryPoints));
        CurrentTime = 0;
        CurrentX = 0;
        CurrentY = 0;

        ExplanationText = BuildExplanation();
    }

    private async System.Threading.Tasks.Task ExplainGroqAsync()
    {
        ExplanationText = "Запрос к нейросети…";
        try
        {
            var prompt = $"Объясни кратко по-русски движение заряда в однородном электрическом поле (аналог брошенного тела). " +
                         $"Параметры: q = {Q:G3} Кл, E = {E:F0} В/м, m = {M:G3} кг, v₀ = {V0:F1} м/с, угол α = {AngleDegrees:F0}°. " +
                         $"Ускорение a = qE/m = {Acceleration:F2} м/с². Время полёта T = {TimeOfFlight:F2} с, дальность L = {Range:F2} м, макс. высота H = {MaxHeight:F2} м. Дай короткое объяснение с формулами.";
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
        double aY = ChargeInField.AccelerationY(Q, E, M);
        double rad = AngleDegrees * Math.PI / 180.0;
        double v0x = V0 * Math.Cos(rad);
        double v0y = V0 * Math.Sin(rad);
        return $"Заряд q = {Q:G3} Кл в однородном поле E = {E:F0} В/м, масса m = {M:G3} кг. Поле направлено вниз: a_y = −qE/m = {aY:F2} м/с², |a| = {Acceleration:F2} м/с².\n\n" +
               $"Начальная скорость v₀ = {V0:F1} м/с, угол α = {AngleDegrees:F0}°. v₀x = {v0x:F2} м/с, v₀y = {v0y:F2} м/с.\n\n" +
               $"Уравнения: x(t) = v₀·cos α·t, y(t) = v₀·sin α·t + a_y·t²/2.\n\n" +
               $"Время полёта T = −2v₀·sin α/a_y = {TimeOfFlight:F2} с. Дальность L = {Range:F2} м. Макс. высота H = {MaxHeight:F2} м.";
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
