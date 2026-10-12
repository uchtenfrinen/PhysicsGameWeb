using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using WPFPhisics1._1.Physics;
using WPFPhisics1._1.Services;

namespace WPFPhisics1._1;

/// <summary>Тонкая линза: d, f → положение и увеличение изображения, построение лучей.</summary>
public class ThinLensViewModel : INotifyPropertyChanged
{
    private readonly DispatcherTimer _animationTimer;
    private double _d = 30;   // см, расстояние от предмета до линзы
    private double _f = 20;    // см, фокусное расстояние
    private double _objectHeight = 10; // см, высота предмета (стрелка)

    private double _dPrime;
    private double _magnification;
    private double _imageHeight;
    private bool _isRealImage;
    private string _imageCharacter = "";
    private string _explanationText = "";
    private bool _isPlaying;
    private double _animationProgress;

    public double D
    {
        get => _d;
        set { _d = Math.Max(0.1, value); OnPropertyChanged(); Build(); }
    }

    public double F
    {
        get => _f;
        set { _f = Math.Max(0.1, value); OnPropertyChanged(); Build(); }
    }

    public double ObjectHeight
    {
        get => _objectHeight;
        set { _objectHeight = Math.Max(0.1, value); OnPropertyChanged(); Build(); }
    }

    public double DPrime { get => _dPrime; set => Set(ref _dPrime, value); }
    /// <summary>Для отображения: число или "—" при d=f.</summary>
    public string DPrimeDisplay => double.IsNaN(_dPrime) || double.IsInfinity(_dPrime) ? "—" : $"{_dPrime:F1} см";
    /// <summary>Число d' для панели результатов (например -2.00).</summary>
    public string DPrimeValue => double.IsNaN(_dPrime) || double.IsInfinity(_dPrime) ? "—" : $"{_dPrime:F2}";
    public double Magnification { get => _magnification; set => Set(ref _magnification, value); }
    public double ImageHeight { get => _imageHeight; set => Set(ref _imageHeight, value); }
    public bool IsRealImage { get => _isRealImage; set => Set(ref _isRealImage, value); }
    public string ImageCharacter { get => _imageCharacter; set => Set(ref _imageCharacter, value); }
    public string ExplanationText { get => _explanationText; set => Set(ref _explanationText, value); }
    public bool IsPlaying { get => _isPlaying; set { Set(ref _isPlaying, value); OnPropertyChanged(nameof(PlayButtonText)); } }
    public string PlayButtonText => IsPlaying ? "■ Остановить симуляцию" : "▶ Запустить симуляцию";
    public double AnimationProgress { get => _animationProgress; set => Set(ref _animationProgress, value); }

    public ICommand BuildCommand { get; }
    public ICommand ExplainCommand { get; }
    public ICommand ExplainGroqCommand { get; }
    public ICommand PlaySimulationCommand { get; }

    public ThinLensViewModel()
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
        double dP = ThinLens.ImageDistance(D, F);
        DPrime = dP;
        Magnification = ThinLens.Magnification(D, F);
        ImageHeight = ThinLens.ImageHeight(ObjectHeight, D, F);
        IsRealImage = ThinLens.IsRealImage(D, F);
        ImageCharacter = double.IsNaN(dP) || double.IsInfinity(dP)
            ? "На экране в бесконечности (d = f)"
            : IsRealImage ? "Действительное, перевёрнутое" : "Мнимое, прямое";
        OnPropertyChanged(nameof(DPrimeDisplay));
        OnPropertyChanged(nameof(DPrimeValue));
        ExplanationText = BuildExplanation();
        AnimationProgress = 0;
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        AnimationProgress += 0.01;
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
            var prompt = $"Объясни кратко по-русски тонкую собирающую линзу. Формула линзы 1/f = 1/d + 1/d'. " +
                         $"Дано: расстояние от предмета до линзы d = {D:F1} см, фокус f = {F:F1} см. " +
                         $"Расстояние до изображения d' = {DPrime:F1} см, увеличение Г = {Magnification:F2}. " +
                         $"Опиши построение хода трёх лучей и характер изображения.";
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
        if (double.IsNaN(DPrime))
            return "При d = f изображение уходит в бесконечность (лучи после линзы параллельны).";
        string formula = "1/f = 1/d + 1/d'  ⇒  d' = f·d/(d−f)";
        string dist = DPrime > 0
            ? $"Изображение справа от линзы на расстоянии d' = {DPrime:F1} см (действительное)."
            : $"Изображение слева от линзы на расстоянии |d'| = {Math.Abs(DPrime):F1} см (мнимое).";
        return $"Формула линзы: {formula}.\n\n" +
               $"d = {D:F1} см, f = {F:F1} см → d' = {DPrime:F1} см.\n\n" +
               $"{dist}\n\n" +
               $"Увеличение Г = −d'/d = {Magnification:F2}. Высота изображения h' = |Г|·h = {ImageHeight:F2} см.\n\n" +
               $"Ход лучей: 1) луч параллельно оси → через задний фокус F'; 2) луч через оптический центр O без преломления; 3) луч через передний фокус F → параллельно оси. Три луча пересекаются в точке изображения.";
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
