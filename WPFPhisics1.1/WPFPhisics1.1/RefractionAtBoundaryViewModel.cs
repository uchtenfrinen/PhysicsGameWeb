using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using WPFPhisics1._1.Physics;
using WPFPhisics1._1.Services;

namespace WPFPhisics1._1;

/// <summary>Преломление на границе: n₁, n₂, угол падения → угол преломления, полное внутреннее отражение.</summary>
public class RefractionAtBoundaryViewModel : INotifyPropertyChanged
{
    private readonly DispatcherTimer _animationTimer;
    private double _n1 = 1.0;
    private double _n2 = 1.33;
    private double _incidenceAngleDegrees = 30;

    private double _refractionAngleDegrees;
    private double? _criticalAngleDegrees;
    private bool _isTotalInternalReflection;
    private string _refractionAngleDisplay = "";
    private string _explanationText = "";
    private bool _isPlaying;
    private double _animationProgress;

    public double N1
    {
        get => _n1;
        set { _n1 = Math.Max(0.1, value); OnPropertyChanged(); }
    }

    public double N2
    {
        get => _n2;
        set { _n2 = Math.Max(0.1, value); OnPropertyChanged(); }
    }

    public double IncidenceAngleDegrees
    {
        get => _incidenceAngleDegrees;
        set { _incidenceAngleDegrees = Math.Clamp(value, 0, 90); OnPropertyChanged(); }
    }

    public double RefractionAngleDegrees { get => _refractionAngleDegrees; set => Set(ref _refractionAngleDegrees, value); }
    public double? CriticalAngleDegrees { get => _criticalAngleDegrees; set => Set(ref _criticalAngleDegrees, value); }
    public bool IsTotalInternalReflection { get => _isTotalInternalReflection; set => Set(ref _isTotalInternalReflection, value); }
    public string TotalReflectionDisplay => IsTotalInternalReflection ? "Да" : "Нет";
    public string RefractionAngleDisplay { get => _refractionAngleDisplay; set => Set(ref _refractionAngleDisplay, value); }
    public string CriticalAngleDisplay => CriticalAngleDegrees.HasValue ? $"{CriticalAngleDegrees.Value:F1}°" : "—";
    public string ExplanationText { get => _explanationText; set => Set(ref _explanationText, value); }
    public bool IsPlaying { get => _isPlaying; set { Set(ref _isPlaying, value); OnPropertyChanged(nameof(PlayButtonText)); } }
    public string PlayButtonText => IsPlaying ? "■ Остановить симуляцию" : "▶ Запустить симуляцию";
    public double AnimationProgress { get => _animationProgress; set => Set(ref _animationProgress, value); }

    public ICommand BuildCommand { get; }
    public ICommand ExplainCommand { get; }
    public ICommand ExplainGroqCommand { get; }
    public ICommand PlaySimulationCommand { get; }

    public RefractionAtBoundaryViewModel()
    {
        BuildCommand = new RelayCommand(_ => Build());
        ExplainCommand = new RelayCommand(_ => ExplanationText = BuildExplanation());
        ExplainGroqCommand = new RelayCommand(_ => _ = ExplainGroqAsync());
        PlaySimulationCommand = new RelayCommand(_ => ToggleSimulation());
        _animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(24) };
        _animationTimer.Tick += AnimationTimer_Tick;
        Build();
    }

    public void Build()
    {
        double refr = RefractionAtBoundary.RefractionAngleDegrees(N1, N2, IncidenceAngleDegrees);
        RefractionAngleDegrees = refr;
        CriticalAngleDegrees = RefractionAtBoundary.CriticalAngleDegrees(N1, N2);
        IsTotalInternalReflection = RefractionAtBoundary.IsTotalInternalReflection(N1, N2, IncidenceAngleDegrees);
        RefractionAngleDisplay = IsTotalInternalReflection ? "— (полное отражение)" : $"{refr:F1}°";
        OnPropertyChanged(nameof(TotalReflectionDisplay));
        OnPropertyChanged(nameof(CriticalAngleDisplay));
        ExplanationText = BuildExplanation();
        AnimationProgress = 0;
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        AnimationProgress += 0.012;
        if (AnimationProgress > 1.0) AnimationProgress = 0;
    }

    private void ToggleSimulation()
    {
        if (IsPlaying)
        {
            _animationTimer.Stop();
            IsPlaying = false;
            return;
        }

        Build();
        IsPlaying = true;
        _animationTimer.Start();
    }

    private async System.Threading.Tasks.Task ExplainGroqAsync()
    {
        ExplanationText = "Запрос к нейросети…";
        try
        {
            var prompt = $"Объясни кратко по-русски преломление на границе двух сред. Закон Снеллиуса n₁·sin θ₁ = n₂·sin θ₂. " +
                         $"Дано: n₁ = {N1:F2}, n₂ = {N2:F2}, угол падения θ₁ = {IncidenceAngleDegrees:F0}°. " +
                         (IsTotalInternalReflection
                             ? "Наблюдается полное внутреннее отражение. Объясни предельный угол."
                             : $"Угол преломления θ₂ = {RefractionAngleDegrees:F1}°. Объясни ход луча.");
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
        string snell = "n₁·sin θ₁ = n₂·sin θ₂";
        if (IsTotalInternalReflection)
        {
            string critStr = CriticalAngleDegrees.HasValue ? $" Предельный угол θ_крит = {CriticalAngleDegrees.Value:F1}° (sin θ_крит = n₂/n₁)." : "";
            return $"Закон Снеллиуса: {snell}. Луч идёт из более оптически плотной среды (n₁ = {N1:F2}) в менее плотную (n₂ = {N2:F2}). " +
                   $"При угле падения θ₁ = {IncidenceAngleDegrees:F0}° выполняется полное внутреннее отражение — преломлённого луча нет.{critStr}";
        }
        return $"Закон Снеллиуса: {snell}. При n₁ = {N1:F2}, n₂ = {N2:F2}, θ₁ = {IncidenceAngleDegrees:F0}° получаем " +
               $"sin θ₂ = n₁·sin θ₁/n₂ ⇒ θ₂ = {RefractionAngleDegrees:F1}°. " +
               (CriticalAngleDegrees.HasValue ? $"Предельный угол полного отражения θ_крит = {CriticalAngleDegrees.Value:F1}°." : "");
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
