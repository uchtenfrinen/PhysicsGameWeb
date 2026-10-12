using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using WPFPhisics1._1.Physics;
using WPFPhisics1._1.Services;

namespace WPFPhisics1._1;

public class CommunicatingVesselsViewModel : INotifyPropertyChanged
{
    private const double ChartRhoMin = 200;
    private const double ChartRhoMax = 1200;

    private bool _isTwoLiquidsMode = true;

    /// <summary>Площадь сечения левого колена (м²).</summary>
    private double _area1 = 0.01;
    /// <summary>Площадь сечения правого колена (м²).</summary>
    private double _area2 = 0.005;
    /// <summary>Объём жидкости (м³).</summary>
    private double _volume = 0.001;
    /// <summary>Плотность жидкости (кг/м³).</summary>
    private double _density = 1000;

    /// <summary>Плотность лёгкой жидкости (масло), кг/м³.</summary>
    private double _rhoLight = 800;
    /// <summary>Плотность тяжёлой жидкости (вода), кг/м³.</summary>
    private double _rhoHeavy = 1000;
    /// <summary>Высота столба воды в правом колене над границей раздела (м).</summary>
    private double _heightHeavy = 0.1;

    private double _levelHeight;
    private double _pressureAtBottom;
    private double _heightLight;
    private double _pressureAtInterface;
    private string _explanationText = "";

    private bool _isPlaying;
    private double _simulationPhase;
    private readonly DispatcherTimer _simTimer;

    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            if (_isPlaying == value) return;
            _isPlaying = value;
            OnPropertyChanged();
            if (_isPlaying)
            {
                _simulationPhase = 0;
                OnPropertyChanged(nameof(SimulationPhase));
                _simTimer.Start();
            }
            else
                _simTimer.Stop();
        }
    }

    /// <summary>Фаза для визуальной «пульсации» схемы при воспроизведении (рад).</summary>
    public double SimulationPhase
    {
        get => _simulationPhase;
        private set
        {
            if (Math.Abs(_simulationPhase - value) < 1e-9) return;
            _simulationPhase = value;
            OnPropertyChanged(nameof(SimulationPhase));
        }
    }

    public ObservableCollection<ObservablePoint> HeightByDensityPoints { get; } = new();
    public ObservableCollection<ObservablePoint> CurrentHeightPoint { get; } = new();
    public ObservableCollection<ISeries> HeightSeries { get; }

    public double Area1
    {
        get => _area1;
        set { _area1 = Math.Clamp(value, 1e-6, 10); OnPropertyChanged(); if (!IsTwoLiquidsMode) Build(); }
    }

    public double Area2
    {
        get => _area2;
        set { _area2 = Math.Clamp(value, 1e-6, 10); OnPropertyChanged(); if (!IsTwoLiquidsMode) Build(); }
    }

    public double Volume
    {
        get => _volume;
        set { _volume = Math.Clamp(value, 1e-9, 1000); OnPropertyChanged(); if (!IsTwoLiquidsMode) Build(); }
    }

    public double Density
    {
        get => _density;
        set { _density = Math.Clamp(value, 1, 20000); OnPropertyChanged(); if (!IsTwoLiquidsMode) Build(); }
    }

    public bool IsTwoLiquidsMode
    {
        get => _isTwoLiquidsMode;
        set
        {
            // В приложении оставлен только режим двух несмешивающихся жидкостей.
            _isTwoLiquidsMode = true;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSingleLiquidMode));
            Build();
        }
    }
    public bool IsSingleLiquidMode => !_isTwoLiquidsMode;

    public double RhoLight
    {
        get => _rhoLight;
        set { _rhoLight = Math.Clamp(value, 1, 20000); OnPropertyChanged(); Build(); }
    }
    public double RhoHeavy
    {
        get => _rhoHeavy;
        set { _rhoHeavy = Math.Clamp(value, 1, 20000); OnPropertyChanged(); Build(); }
    }
    public double HeightHeavy
    {
        get => _heightHeavy;
        set { _heightHeavy = Math.Clamp(value, 1e-4, 10); OnPropertyChanged(); Build(); }
    }

    /// <summary>Отношение высот столбов h₁/h₂ (при равновесии ≈ ρ₂/ρ₁).</summary>
    public double RatioH1H2 => _heightHeavy > 1e-12 ? _heightLight / _heightHeavy : 0;

    public double LevelHeight { get => _levelHeight; set => Set(ref _levelHeight, value); }
    public double PressureAtBottom { get => _pressureAtBottom; set => Set(ref _pressureAtBottom, value); }
    public double HeightLight { get => _heightLight; set => Set(ref _heightLight, value); }
    public double PressureAtInterface { get => _pressureAtInterface; set => Set(ref _pressureAtInterface, value); }
    public string ExplanationText { get => _explanationText; set => Set(ref _explanationText, value); }

    public ICommand BuildCommand { get; }
    public ICommand ExplainCommand { get; }
    public ICommand ExplainGroqCommand { get; }
    public ICommand PlaySimulationCommand { get; }

    public CommunicatingVesselsViewModel()
    {
        _simTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
        _simTimer.Tick += (_, _) =>
        {
            double next = _simulationPhase + 0.14;
            if (next > Math.PI * 2) next -= Math.PI * 2;
            SimulationPhase = next;
        };

        BuildCommand = new RelayCommand(_ => Build());
        ExplainCommand = new RelayCommand(_ => ExplanationText = BuildExplanation());
        ExplainGroqCommand = new RelayCommand(_ => _ = ExplainGroqAsync());
        PlaySimulationCommand = new RelayCommand(_ => { IsPlaying = !IsPlaying; });

        HeightSeries = new ObservableCollection<ISeries>
        {
            new LineSeries<ObservablePoint>
            {
                Values = HeightByDensityPoints,
                Name = "h_масла(ρ)",
                Stroke = new SolidColorPaint(new SKColor(0xFF, 0xC1, 0x07)) { StrokeThickness = 2.5f },
                Fill = new SolidColorPaint(new SKColor(0xFF, 0xC1, 0x07, 40)),
                GeometrySize = 0,
                LineSmoothness = 0.3
            },
            new ScatterSeries<ObservablePoint>
            {
                Values = CurrentHeightPoint,
                Name = "Текущее состояние",
                GeometrySize = 16f,
                Fill = new SolidColorPaint(SKColors.Lime),
                Stroke = new SolidColorPaint(SKColors.Black){ StrokeThickness = 1.5f }
            }
        };

        Build();
    }

    public void Build()
    {
        IsPlaying = false;
        _isTwoLiquidsMode = true;
        HeightLight = CommunicatingVessels.HeightLightFromHeavy(RhoLight, RhoHeavy, HeightHeavy);
        PressureAtInterface = CommunicatingVessels.PressureAtInterface(RhoHeavy, HeightHeavy, CommunicatingVessels.G);

        // Обновляем график h_масла(ρ_масла)
        HeightByDensityPoints.Clear();
        int n = 80;
        for (int i = 0; i <= n; i++)
        {
            double rho = ChartRhoMin + (ChartRhoMax - ChartRhoMin) * i / (double)n;
            double hOil = CommunicatingVessels.HeightLightFromHeavy(rho, RhoHeavy, HeightHeavy);
            if (hOil < 0) hOil = 0;
            HeightByDensityPoints.Add(new ObservablePoint(rho, hOil));
        }
        CurrentHeightPoint.Clear();
        CurrentHeightPoint.Add(new ObservablePoint(RhoLight, HeightLight));
        ExplanationText = BuildExplanation();
        OnPropertyChanged(nameof(RatioH1H2));
    }

    private async System.Threading.Tasks.Task ExplainGroqAsync()
    {
        ExplanationText = "Запрос к нейросети…";
        try
        {
            var prompt =
                $"Объясни кратко по-русски сообщающиеся сосуды с двумя жидкостями. ρ_лёгкая = {RhoLight:F0} кг/м³, ρ_тяжёлая = {RhoHeavy:F0} кг/м³, высота столба тяжёлой h = {HeightHeavy:G4} м. Высота лёгкой h_л = {HeightLight:G4} м, давление на границе p = {PressureAtInterface:F1} Па. Дай короткое объяснение.";
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
        return "Сообщающиеся сосуды с двумя несмешивающимися жидкостями (например, масло и вода). На границе раздела давление одинаково в обоих коленах.\n\n" +
               "Условие равновесия: ρ₁·g·h₁ = ρ₂·g·h₂ ⇒ h₁/h₂ = ρ₂/ρ₁ — высоты столбов обратно пропорциональны плотностям.\n\n" +
               $"Параметры: ρ_масла = {RhoLight:F0} кг/м³, ρ_воды = {RhoHeavy:F0} кг/м³. Задана высота столба воды h_воды = {HeightHeavy:G4} м.\n\n" +
               $"Высота столба масла: h_масла = h_воды · (ρ_воды/ρ_масла) = {HeightLight:G4} м.\n\n" +
               $"Давление на границе раздела: p = ρ_воды·g·h_воды = ρ_масла·g·h_масла = {PressureAtInterface:F1} Па.";
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
