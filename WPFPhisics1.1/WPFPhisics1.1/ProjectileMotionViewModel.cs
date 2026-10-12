using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using WPFPhisics1._1.Physics;
using WPFPhisics1._1.Services;

namespace WPFPhisics1._1;

public class ProjectileMotionViewModel : INotifyPropertyChanged
{
    private readonly DispatcherTimer _animationTimer;
    private const double AnimDt = 0.03;

    private double _v0 = 15;
    private double _angleDegrees = 45;
    private double _y0 = 0;

    public double V0
    {
        get => _v0;
        set { _v0 = Math.Clamp(value, 0.1, 200); OnPropertyChanged(); Build(); }
    }

    public double AngleDegrees
    {
        get => _angleDegrees;
        set { _angleDegrees = Math.Clamp(value, 1, 89); OnPropertyChanged(); Build(); }
    }

    public double Y0
    {
        get => _y0;
        set { _y0 = Math.Max(0, value); OnPropertyChanged(); Build(); }
    }

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

    /// <summary>Время для расчёта мгновенных величин (ограничено длительностью полёта).</summary>
    public double EffectiveTime => Math.Min(Math.Max(0, CurrentTime), TimeOfFlight);

    public double CurrentVx => ProjectileMotion.VelocityAt(V0, AngleDegrees, EffectiveTime).Vx;

    public double CurrentVy => ProjectileMotion.VelocityAt(V0, AngleDegrees, EffectiveTime).Vy;

    public double CurrentSpeedMag
    {
        get
        {
            var (vx, vy) = ProjectileMotion.VelocityAt(V0, AngleDegrees, EffectiveTime);
            return Math.Sqrt(vx * vx + vy * vy);
        }
    }

    /// <summary>Начальная вертикальная составляющая |v₀y| (макс. по модулю в начале полёта).</summary>
    public double V0y => V0 * Math.Sin(AngleDegrees * Math.PI / 180.0);

    /// <summary>Модуль скорости в момент приземления.</summary>
    public double ImpactSpeed
    {
        get
        {
            if (TimeOfFlight <= 0) return 0;
            var (vx, vy) = ProjectileMotion.VelocityAt(V0, AngleDegrees, TimeOfFlight);
            return Math.Sqrt(vx * vx + vy * vy);
        }
    }

    public string PlayButtonText => IsPlaying ? "■ Остановить анимацию" : "▶ Запустить анимацию";

    public ICommand BuildCommand { get; }
    public ICommand PlayAnimationCommand { get; }
    public ICommand ExplainCommand { get; }
    public ICommand ExplainGroqCommand { get; }

    public ProjectileMotionViewModel()
    {
        BuildCommand = new RelayCommand(_ => Build());
        PlayAnimationCommand = new RelayCommand(_ => ToggleAnimation());
        ExplainCommand = new RelayCommand(_ => ExplanationText = BuildExplanation());
        ExplainGroqCommand = new RelayCommand(_ => _ = ExplainGroqAsync());
        _animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
        _animationTimer.Tick += AnimationTimer_Tick;
        Build();
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        CurrentTime += AnimDt;
        var (x, y) = ProjectileMotion.PositionAt(V0, AngleDegrees, CurrentTime, Y0);
        CurrentX = x;
        CurrentY = y;
        NotifyKinematics();
        if (CurrentTime >= TimeOfFlight || CurrentY < 0)
        {
            _animationTimer.Stop();
            IsPlaying = false;
        }
    }

    private void NotifyKinematics()
    {
        OnPropertyChanged(nameof(EffectiveTime));
        OnPropertyChanged(nameof(CurrentVx));
        OnPropertyChanged(nameof(CurrentVy));
        OnPropertyChanged(nameof(CurrentSpeedMag));
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
            CurrentY = Y0;
            NotifyKinematics();
            IsPlaying = true;
            _animationTimer.Start();
        }
    }

    public void Build()
    {
        var points = ProjectileMotion.GetTrajectoryPoints(V0, AngleDegrees, Y0);
        TrajectoryPoints.Clear();
        foreach (var p in points)
            TrajectoryPoints.Add(new System.Windows.Point(p.X, p.Y));

        Range = ProjectileMotion.Range(V0, AngleDegrees, Y0);
        MaxHeight = ProjectileMotion.MaxHeight(V0, AngleDegrees, Y0);
        TimeOfFlight = ProjectileMotion.TimeOfFlight(V0, AngleDegrees, Y0);
        OnPropertyChanged(nameof(TrajectoryPoints));
        CurrentTime = 0;
        CurrentX = 0;
        CurrentY = Y0;

        OnPropertyChanged(nameof(EffectiveTime));
        OnPropertyChanged(nameof(CurrentVx));
        OnPropertyChanged(nameof(CurrentVy));
        OnPropertyChanged(nameof(CurrentSpeedMag));
        OnPropertyChanged(nameof(V0y));
        OnPropertyChanged(nameof(ImpactSpeed));

        ExplanationText = BuildExplanation();
    }

    private async System.Threading.Tasks.Task ExplainGroqAsync()
    {
        ExplanationText = "Запрос к нейросети…";
        try
        {
            var prompt = $"Объясни кратко по-русски движение тела, брошенного под углом к горизонту. " +
                         $"Параметры: v₀ = {V0:F1} м/с, угол α = {AngleDegrees:F0}°, начальная высота y₀ = {Y0:F1} м. " +
                         $"Рассчитанные результаты (используй в объяснении именно эти числа): дальность полёта L = {Range:F2} м, макс. высота H = {MaxHeight:F2} м, время полёта T = {TimeOfFlight:F2} с. " +
                         "Дай короткое объяснение с формулами, везде подставляя указанные L, H и T.";
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
        double rad = AngleDegrees * Math.PI / 180.0;
        double v0x = V0 * Math.Cos(rad);
        double v0y = V0 * Math.Sin(rad);
        string timeFormula = Y0 <= 0 ? "T = 2v₀y/g" : "T = (v₀y + √(v₀y² + 2gy₀))/g";
        return $"Движение тела, брошенного под углом α = {AngleDegrees:F0}° с начальной скоростью v₀ = {V0:F1} м/с, y₀ = {Y0:F1} м.\n\n" +
               $"Составляющие скорости: v₀x = v₀·cos α = {v0x:F2} м/с, v₀y = v₀·sin α = {v0y:F2} м/с.\n\n" +
               $"Уравнения: x(t) = v₀x·t, y(t) = y₀ + v₀y·t − g·t²/2.\n\n" +
               $"Время полёта: {timeFormula} = {TimeOfFlight:F2} с.\n" +
               $"Дальность: L = {Range:F2} м. Максимальная высота: H = {MaxHeight:F2} м.";
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
