using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WPFPhisics1._1.Physics;
using WPFPhisics1._1.Services;

namespace WPFPhisics1._1;

/// <summary>Свободное падение: v₀ = 0, h(t) = h₀ − g·t²/2, v(t) = g·t.</summary>
public class FreeFallViewModel : INotifyPropertyChanged
{
    private readonly DispatcherTimer _animationTimer;
    private const double AnimDt = 0.03;

    private double _height0 = 20;
    private double _massKg = 2;
    private double _timeOfFall;
    private double _finalSpeed;
    private string _explanationText = "";
    private double _currentTime;
    private bool _isPlaying;

    public double Height0
    {
        get => _height0;
        set { _height0 = Math.Clamp(value, 0.1, 1000); OnPropertyChanged(); Build(); }
    }

    /// <summary>Масса (кг); в идеальном падении не входит в T и v, используется в текстах и запросе к ИИ.</summary>
    public double MassKg
    {
        get => _massKg;
        set
        {
            _massKg = Math.Clamp(value, 0.01, 1_000_000);
            OnPropertyChanged();
            ExplanationText = BuildExplanation();
        }
    }

    public double TimeOfFall { get => _timeOfFall; set => Set(ref _timeOfFall, value); }
    public double FinalSpeed { get => _finalSpeed; set => Set(ref _finalSpeed, value); }
    public string ExplanationText { get => _explanationText; set => Set(ref _explanationText, value); }
    public double CurrentTime { get => _currentTime; set { Set(ref _currentTime, value); OnPropertyChanged(nameof(CurrentHeight)); OnPropertyChanged(nameof(CurrentSpeed)); OnPropertyChanged(nameof(MotionStateText)); } }
    public bool IsPlaying { get => _isPlaying; set { Set(ref _isPlaying, value); OnPropertyChanged(nameof(PlayButtonText)); OnPropertyChanged(nameof(MotionStateText)); } }
    /// <summary>Текущая высота при анимации (м).</summary>
    public double CurrentHeight => Physics.FreeFall.HeightAt(CurrentTime, Height0);

    /// <summary>Модуль скорости вниз (м/с) в текущий момент анимации.</summary>
    public double CurrentSpeed => Physics.FreeFall.VelocityAt(Math.Min(Math.Max(0, CurrentTime), TimeOfFall));

    public string MotionStateText
    {
        get
        {
            if (IsPlaying) return "Движение вниз";
            if (CurrentTime <= 1e-9) return "Ожидание запуска";
            if (CurrentTime >= TimeOfFall - 1e-6 || CurrentHeight <= 1e-6) return "На поверхности";
            return "Пауза";
        }
    }

    public string PlayButtonText => IsPlaying ? "■ Остановить анимацию" : "▶ Запустить анимацию";

    public ObservableCollection<Point> HeightChartPoints { get; } = new();
    public ObservableCollection<Point> VelocityChartPoints { get; } = new();

    public ICommand BuildCommand { get; }
    public ICommand PlayAnimationCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand ExplainCommand { get; }
    public ICommand ExplainGroqCommand { get; }

    public FreeFallViewModel()
    {
        BuildCommand = new RelayCommand(_ => Build());
        PlayAnimationCommand = new RelayCommand(_ => ToggleAnimation());
        ResetCommand = new RelayCommand(_ => Reset());
        ExplainCommand = new RelayCommand(_ => ExplanationText = BuildExplanation());
        ExplainGroqCommand = new RelayCommand(_ => _ = ExplainGroqAsync());
        _animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
        _animationTimer.Tick += AnimationTimer_Tick;
        Build();
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        CurrentTime += AnimDt;
        OnPropertyChanged(nameof(CurrentHeight));
        if (CurrentTime >= TimeOfFall || CurrentHeight <= 0)
        {
            _animationTimer.Stop();
            CurrentTime = Math.Min(CurrentTime, TimeOfFall);
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
            if (HeightChartPoints.Count < 2) Build();
            CurrentTime = 0;
            OnPropertyChanged(nameof(CurrentHeight));
            IsPlaying = true;
            _animationTimer.Start();
        }
    }

    private void Reset()
    {
        if (_animationTimer.IsEnabled) { _animationTimer.Stop(); IsPlaying = false; }
        CurrentTime = 0;
        OnPropertyChanged(nameof(CurrentHeight));
        Build();
    }

    public void Build()
    {
        if (_animationTimer.IsEnabled) { _animationTimer.Stop(); IsPlaying = false; }
        CurrentTime = 0;
        TimeOfFall = FreeFall.TimeOfFall(Height0);
        FinalSpeed = FreeFall.FinalSpeed(Height0);

        var hPoints = FreeFall.GetHeightPoints(Height0);
        HeightChartPoints.Clear();
        foreach (var p in hPoints)
            HeightChartPoints.Add(new Point(p.T, p.H));
        OnPropertyChanged(nameof(HeightChartPoints));

        var vPoints = FreeFall.GetVelocityPoints(Height0);
        VelocityChartPoints.Clear();
        foreach (var p in vPoints)
            VelocityChartPoints.Add(new Point(p.T, p.V));
        OnPropertyChanged(nameof(VelocityChartPoints));

        OnPropertyChanged(nameof(MotionStateText));
        OnPropertyChanged(nameof(CurrentSpeed));
        ExplanationText = BuildExplanation();
    }

    private async System.Threading.Tasks.Task ExplainGroqAsync()
    {
        ExplanationText = "Запрос к нейросети…";
        try
        {
            var prompt = $"Объясни кратко по-русски свободное падение тела без начальной скорости (v₀ = 0). " +
                         $"Начальная высота h₀ = {Height0:F1} м, масса m = {MassKg:F2} кг (в идеальной модели без сопротивления воздуха T и v от m не зависят). " +
                         $"Результаты: время падения T = {TimeOfFall:F2} с, скорость в момент приземления v = {FinalSpeed:F2} м/с. " +
                         "Формулы: h(t) = h₀ − g·t²/2, v(t) = g·t, T = √(2h₀/g), v = √(2g·h₀). Используй в объяснении именно эти численные значения.";
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
        return $"Свободное падение: тело массой m = {MassKg:F2} кг отпущено с высоты h₀ = {Height0:F1} м без начальной скорости (v₀ = 0).\n\n" +
               $"Ускорение g = {FreeFall.G:F2} м/с² направлено вниз.\n\n" +
               $"Зависимости: h(t) = h₀ − g·t²/2, v(t) = g·t (скорость вниз положительна).\n\n" +
               $"Время падения: T = √(2h₀/g) = √(2·{Height0:F1}/{FreeFall.G:F2}) = {TimeOfFall:F2} с.\n\n" +
               $"Скорость в момент приземления: v = √(2g·h₀) = g·T = {FinalSpeed:F2} м/с.";
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
