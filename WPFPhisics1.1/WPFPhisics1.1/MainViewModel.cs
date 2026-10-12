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

public class MainViewModel : INotifyPropertyChanged
{
    private readonly DispatcherTimer _timer;
    private const double SimDt = 0.04; // ~25 FPS
    private const double MinT = 0.14;
    private const double MaxT = 0.86;
    private double _simTimeSec;
    private double _chartStartPosition; // позиция t при старте записи графика

    public MainViewModel()
    {
        Plane = new InclinedPlane { AngleDegrees = 30 };
        Block = new Block { MassKg = 2, PositionAlongPlane = 0.5 };
        Engine = new PhysicsEngine(Plane, Block, _frictionMu);

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(40)
        };
        _timer.Tick += Timer_Tick;

        StartStopCommand = new RelayCommand(_ => ToggleSimulation());
        ResetCommand = new RelayCommand(_ => Reset());
        ExplainCommand = new RelayCommand(_ => Explain());
        ExplainGroqCommand = new RelayCommand(_ => _ = ExplainGroqAsync());

        ChartData = new ObservableCollection<ChartPoint>();
        PathPoints = new ObservableCollection<ObservablePoint>();
        VelocityPoints = new ObservableCollection<ObservablePoint>();
        AccelerationPoints = new ObservableCollection<ObservablePoint>();
        PathSeries = new ObservableCollection<ISeries> { new LineSeries<ObservablePoint> { Values = PathPoints, Name = "s, м" } };
        VelocitySeries = new ObservableCollection<ISeries> { new LineSeries<ObservablePoint> { Values = VelocityPoints, Name = "v, м/с" } };
        AccelerationSeries = new ObservableCollection<ISeries> { new LineSeries<ObservablePoint> { Values = AccelerationPoints, Name = "a, м/с²" } };
        ChartData.CollectionChanged += (_, _) => SyncChartPoints();

        // Диаграмма наклонной плоскости (LiveCharts2): дуга угла, плоскость, груз, силы
        AngleArcPoints = new ObservableCollection<ObservablePoint>();
        PlaneLinePoints = new ObservableCollection<ObservablePoint>();
        BlockPoint = new ObservableCollection<ObservablePoint>();
        ForceFPoints = new ObservableCollection<ObservablePoint>();
        ForceNPoints = new ObservableCollection<ObservablePoint>();
        ForceAlongPoints = new ObservableCollection<ObservablePoint>();
        ForceFrictionPoints = new ObservableCollection<ObservablePoint>();
        InclinedPlaneSeries = new ObservableCollection<ISeries>
        {
            new LineSeries<ObservablePoint>
            {
                Values = AngleArcPoints,
                Name = "α",
                Stroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 1.5f },
                GeometrySize = 0,
                LineSmoothness = 0
            },
            new LineSeries<ObservablePoint>
            {
                Values = PlaneLinePoints,
                Name = "Плоскость",
                Stroke = new SolidColorPaint(new SKColor(70, 130, 180)) { StrokeThickness = 12f },
                GeometrySize = 0,
                LineSmoothness = 0
            },
            new ScatterSeries<ObservablePoint>
            {
                Values = BlockPoint,
                Name = "Груз",
                Fill = new SolidColorPaint(SKColors.IndianRed),
                Stroke = new SolidColorPaint(SKColors.DarkRed) { StrokeThickness = 2f },
                GeometrySize = 22f
            },
            new LineSeries<ObservablePoint> { Values = ForceFPoints, Name = "F = mg", Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 3.5f }, GeometrySize = 0, LineSmoothness = 0 },
            new LineSeries<ObservablePoint> { Values = ForceNPoints, Name = "N", Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 3.5f }, GeometrySize = 0, LineSmoothness = 0 },
            new LineSeries<ObservablePoint> { Values = ForceAlongPoints, Name = "m·g·sin α", Stroke = new SolidColorPaint(SKColors.RoyalBlue) { StrokeThickness = 3f }, GeometrySize = 0, LineSmoothness = 0 },
            new LineSeries<ObservablePoint> { Values = ForceFrictionPoints, Name = "F_тр", Stroke = new SolidColorPaint(new SKColor(135, 206, 250)) { StrokeThickness = 3.5f }, GeometrySize = 0, LineSmoothness = 0 }
        };

        RefreshForces();
        Explain();
        UpdateInclinedPlaneChart();
    }

    /// <summary>Точки графика «путь от времени» s(t), записываются во время симуляции.</summary>
    public ObservableCollection<ChartPoint> ChartData { get; }

    /// <summary>Точки для LiveCharts: s(t).</summary>
    public ObservableCollection<ObservablePoint> PathPoints { get; }
    /// <summary>Точки для LiveCharts: v(t).</summary>
    public ObservableCollection<ObservablePoint> VelocityPoints { get; }
    /// <summary>Точки для LiveCharts: a(t).</summary>
    public ObservableCollection<ObservablePoint> AccelerationPoints { get; }

    public ObservableCollection<ISeries> PathSeries { get; }
    public ObservableCollection<ISeries> VelocitySeries { get; }
    public ObservableCollection<ISeries> AccelerationSeries { get; }

    /// <summary>Серии для диаграммы наклонной плоскости (LiveCharts2): плоскость, груз, силы.</summary>
    public ObservableCollection<ISeries> InclinedPlaneSeries { get; }
    public ObservableCollection<ObservablePoint> AngleArcPoints { get; }
    public ObservableCollection<ObservablePoint> PlaneLinePoints { get; }
    public ObservableCollection<ObservablePoint> BlockPoint { get; }
    public ObservableCollection<ObservablePoint> ForceFPoints { get; }
    public ObservableCollection<ObservablePoint> ForceNPoints { get; }
    public ObservableCollection<ObservablePoint> ForceAlongPoints { get; }
    public ObservableCollection<ObservablePoint> ForceFrictionPoints { get; }

    private void SyncChartPoints()
    {
        PathPoints.Clear();
        VelocityPoints.Clear();
        AccelerationPoints.Clear();
        foreach (var p in ChartData)
        {
            PathPoints.Add(new ObservablePoint(p.TimeSec, p.PathM));
            VelocityPoints.Add(new ObservablePoint(p.TimeSec, p.VelocityMps));
            AccelerationPoints.Add(new ObservablePoint(p.TimeSec, p.AccelerationMps2));
        }
    }

    public InclinedPlane Plane { get; }
    public Block Block { get; }
    public PhysicsEngine Engine { get; }

    public double MassKg { get => Block.MassKg; set { Block.MassKg = value; OnPropertyChanged(); RefreshForces(); Explain(); UpdateInclinedPlaneChart(); } }
    public double AngleDegrees { get => Plane.AngleDegrees; set { Plane.AngleDegrees = value; OnPropertyChanged(); RefreshForces(); Explain(); UpdateInclinedPlaneChart(); } }

    private double _frictionMu = 0.5;
    public double FrictionMu
    {
        get => _frictionMu;
        set
        {
            _frictionMu = Math.Clamp(value, 0, 1);
            if (Engine != null) Engine.FrictionMu = _frictionMu;
            OnPropertyChanged();
            RefreshForces();
            Explain();
            UpdateInclinedPlaneChart();
        }
    }

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set { _isRunning = value; OnPropertyChanged(); OnPropertyChanged(nameof(StartStopButtonText)); OnPropertyChanged(nameof(IsDraggingEnabled)); }
    }

    public string StartStopButtonText => IsRunning ? "Стоп" : "► Запустить";
    public bool IsDraggingEnabled => !IsRunning;

    /// <summary>Текст состояния для панели: "► Скользит вниз" / "Покой".</summary>
    public string StateDisplayText => StateText == "скольжение" ? "► Скользит вниз" : "Покой";

    /// <summary>Время прохождения 1 м при текущем ускорении (с); null если покой.</summary>
    public double? TimeFor1MSeconds => Acceleration > 0.001 ? Math.Sqrt(2.0 / Acceleration) : null;
    public string TimeFor1MText => TimeFor1MSeconds.HasValue ? $"{TimeFor1MSeconds.Value:F2} с" : "—";

    private double _fWeight;
    public double FWeight { get => _fWeight; set => Set(ref _fWeight, value); }
    private double _normalN;
    public double NormalN { get => _normalN; set => Set(ref _normalN, value); }
    private double _fAlong;
    public double FAlong { get => _fAlong; set => Set(ref _fAlong, value); }
    private double _fTr;
    public double FTr { get => _fTr; set => Set(ref _fTr, value); }
    private double _acceleration;
    public double Acceleration { get => _acceleration; set => Set(ref _acceleration, value); }
    private string _stateText = "покой";
    public string StateText { get => _stateText; set => Set(ref _stateText, value); }

    /// <summary>Проверка условия покоя: μ и tg α.</summary>
    private string _restConditionText = "";
    public string RestConditionText { get => _restConditionText; set => Set(ref _restConditionText, value); }

    /// <summary>Итоги последнего заезда (время, путь, конечная скорость).</summary>
    private string _lastRunSummaryText = "";
    public string LastRunSummaryText { get => _lastRunSummaryText; set => Set(ref _lastRunSummaryText, value); }

    private string _explanationText = "";
    public string ExplanationText { get => _explanationText; set => Set(ref _explanationText, value); }

    public ICommand StartStopCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand ExplainCommand { get; }
    public ICommand ExplainGroqCommand { get; }

    private void Reset()
    {
        if (IsRunning) { _timer.Stop(); IsRunning = false; }
        Block.VelocityAlongPlane = 0;
        Block.PositionAlongPlane = 0.5;
        ChartData.Clear();
        LastRunSummaryText = "";
        _simTimeSec = 0;
        RefreshForces();
        NotifyBlockChanged();
        Explain();
    }

    public void RefreshForces()
    {
        var r = Engine.CurrentForces;
        FWeight = r.WeightN;
        NormalN = r.NormalN;
        FAlong = r.ForceAlongPlaneN;
        FTr = r.FrictionN;
        Acceleration = r.AccelerationMps2;
        StateText = r.IsAtRest ? "покой" : "скольжение";
        double tanAlpha = Math.Tan(Plane.AngleRadians);
        RestConditionText = $"μ = {FrictionMu:F2}, tg α = {tanAlpha:F2} → {(FrictionMu >= tanAlpha ? "μ ≥ tg α → покой" : "μ < tg α → скольжение")}";
        UpdateInclinedPlaneChart();
        OnPropertyChanged(nameof(StateDisplayText));
        OnPropertyChanged(nameof(TimeFor1MSeconds));
        OnPropertyChanged(nameof(TimeFor1MText));
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        Engine.Step(SimDt);
        Block.PositionAlongPlane = Math.Max(PhysicsEngine.MinT, Block.PositionAlongPlane);
        _simTimeSec += SimDt;
        double pathM = (_chartStartPosition - Block.PositionAlongPlane) * 1.0;
        ChartData.Add(new ChartPoint(_simTimeSec, pathM, Block.VelocityAlongPlane, Engine.Acceleration));
        RefreshForces();
        OnPropertyChanged(nameof(Block));
        OnPropertyChanged(nameof(ChartData));
        if (Engine.ReachedBottom)
        {
            _timer.Stop();
            IsRunning = false;
            UpdateLastRunSummary();
        }
    }

    private void UpdateLastRunSummary()
    {
        double pathM = (_chartStartPosition - Block.PositionAlongPlane) * 1.0;
        LastRunSummaryText = $"Время: {_simTimeSec:F2} с, путь: {pathM:F2} м, v кон.: {Block.VelocityAlongPlane:F2} м/с";
    }

    private void ToggleSimulation()
    {
        if (IsRunning)
        {
            _timer.Stop();
            IsRunning = false;
            UpdateLastRunSummary();
        }
        else
        {
            Block.VelocityAlongPlane = 0;
            ChartData.Clear();
            LastRunSummaryText = "";
            _simTimeSec = 0;
            _chartStartPosition = Block.PositionAlongPlane;
            ChartData.Add(new ChartPoint(0, 0, 0, Engine.Acceleration)); // начальная точка
            IsRunning = true;
            _timer.Start();
        }
    }

    private void Explain()
    {
        ExplanationText = PhysicsExplanationBuilder.Build(Block.MassKg, Plane.AngleDegrees, FrictionMu);
    }

    private async System.Threading.Tasks.Task ExplainGroqAsync()
    {
        var r = Engine.CurrentForces;
        ExplanationText = "Запрос к нейросети…";
        try
        {
            var text = await GroqService.GetInclinedPlaneExplanationAsync(
                Block.MassKg, Plane.AngleDegrees, FrictionMu,
                r.WeightN, r.NormalN, r.ForceAlongPlaneN, r.FrictionN,
                r.AccelerationMps2, r.IsAtRest);
            ExplanationText = text;
        }
        catch (Exception ex)
        {
            ExplanationText = "Ошибка: " + ex.Message;
        }
    }

    public void ClampBlockPositionForDrag()
    {
        double t = Block.PositionAlongPlane;
        Block.PositionAlongPlane = Math.Clamp(t, MinT, MaxT);
        RefreshForces();
        NotifyBlockChanged();
    }

    public void NotifyBlockChanged()
    {
        OnPropertyChanged(nameof(Block));
        UpdateInclinedPlaneChart();
    }

    private const double ForceChartScale = 0.012; // в единицах графика на 1 Н

    private void UpdateInclinedPlaneChart()
    {
        double angleRad = Plane.AngleRadians;
        double cos = Math.Cos(angleRad);
        double sin = Math.Sin(angleRad);
        double t = Block.PositionAlongPlane;
        double bx = t * cos;
        double by = t * sin;

        // Дуга угла α (от горизонтали до плоскости)
        const double arcRadius = 0.18;
        int arcSteps = 15;
        AngleArcPoints.Clear();
        for (int i = 0; i <= arcSteps; i++)
        {
            double phi = angleRad * i / arcSteps;
            AngleArcPoints.Add(new ObservablePoint(arcRadius * Math.Cos(phi), arcRadius * Math.Sin(phi)));
        }

        PlaneLinePoints.Clear();
        PlaneLinePoints.Add(new ObservablePoint(0, 0));
        PlaneLinePoints.Add(new ObservablePoint(cos, sin));

        BlockPoint.Clear();
        BlockPoint.Add(new ObservablePoint(bx, by));

        var r = Engine.CurrentForces;
        double alongX = -cos;
        double alongY = -sin;

        ForceFPoints.Clear();
        if (r.WeightN > 0)
        {
            double len = Math.Min(r.WeightN * ForceChartScale, 0.25);
            ForceFPoints.Add(new ObservablePoint(bx, by));
            ForceFPoints.Add(new ObservablePoint(bx, by - len)); // вес вниз (Y вниз)
        }

        ForceNPoints.Clear();
        if (r.NormalN > 0)
        {
            double len = Math.Min(r.NormalN * ForceChartScale, 0.25);
            // нормаль из плоскости «вверх»: перпендикуляр (-sin, cos)
            ForceNPoints.Add(new ObservablePoint(bx, by));
            ForceNPoints.Add(new ObservablePoint(bx - sin * len, by + cos * len));
        }

        ForceAlongPoints.Clear();
        if (r.ForceAlongPlaneN > 0)
        {
            double len = Math.Min(r.ForceAlongPlaneN * ForceChartScale, 0.25);
            ForceAlongPoints.Add(new ObservablePoint(bx, by));
            ForceAlongPoints.Add(new ObservablePoint(bx + alongX * len, by + alongY * len));
        }

        ForceFrictionPoints.Clear();
        if (r.FrictionN > 0)
        {
            double len = Math.Min(r.FrictionN * ForceChartScale * 1.6, 0.3);
            ForceFrictionPoints.Add(new ObservablePoint(bx, by));
            ForceFrictionPoints.Add(new ObservablePoint(bx - alongX * len, by - alongY * len));
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
