using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WPFPhisics1._1.Physics;

namespace WPFPhisics1._1;

/// <summary>
/// Основная сцена: наклонная плоскость, груз, стрелки сил. Перетаскивание груза вдоль плоскости.
/// </summary>
public class SimulationCanvas : FrameworkElement
{
    private const double LayoutMargin = 40;
    private const double ForceScalePxPerN = 8;
    private Point _planeBottom;
    private Point _planeTop;
    private double _planeLength;
    private bool _dragging;
    private Point _dragStart;

    public SimulationCanvas()
    {
        ClipToBounds = true;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        DataContextChanged += (s, e) =>
        {
            if (e.OldValue is MainViewModel oldVm)
                oldVm.PropertyChanged -= Vm_PropertyChanged;
            if (e.NewValue is MainViewModel newVm)
                newVm.PropertyChanged += Vm_PropertyChanged;
        };
        SizeChanged += (s, e) => { if (ActualWidth > 0 && ActualHeight > 0) InvalidateVisual(); };
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        MouseLeave += (s, e) => { if (_dragging) EndDrag(); };
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(Math.Max(0, availableSize.Width), Math.Max(0, availableSize.Height));
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => InvalidateVisual();

    private MainViewModel? Vm => DataContext as MainViewModel;

    protected override void OnRender(DrawingContext dc)
    {
        var vm = Vm;
        if (vm == null) return;

        double w = ActualWidth;
        double h = ActualHeight;
        if (w <= 0 || h <= 0)
        {
            Dispatcher.BeginInvoke(() => InvalidateVisual(), System.Windows.Threading.DispatcherPriority.Loaded);
            return;
        }
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(250, 250, 250)), null, new Rect(0, 0, w, h));

        double angleDeg = vm.Plane.AngleDegrees;
        double angleRad = angleDeg * Math.PI / 180.0;
        _planeLength = Math.Min((w - 2 * LayoutMargin) / Math.Cos(angleRad), (h - 2 * LayoutMargin) / Math.Sin(angleRad));
        _planeBottom = new Point(LayoutMargin, h - LayoutMargin);
        _planeTop = new Point(LayoutMargin + _planeLength * Math.Cos(angleRad), h - LayoutMargin - _planeLength * Math.Sin(angleRad));

        // Плоскость (трапеция)
        double thick = 12;
        Vector along = _planeTop - _planeBottom;
        along.Normalize();
        Vector perp = new Vector(-along.Y, along.X);
        Point[] planePoints =
        {
            _planeBottom - perp * thick,
            _planeBottom + perp * thick,
            _planeTop + perp * thick,
            _planeTop - perp * thick
        };
        var pg = new PathGeometry();
        var pf = new PathFigure(planePoints[0], new PathSegment[] { new LineSegment(planePoints[1], true), new LineSegment(planePoints[2], true), new LineSegment(planePoints[3], true) }, true);
        pg.Figures.Add(pf);
        dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(200, 200, 200)), new Pen(Brushes.Gray, 1), pg);

        // Груз
        double t = vm.Block.PositionAlongPlane;
        Point blockCenter = new Point(
            _planeBottom.X + t * (_planeTop.X - _planeBottom.X),
            _planeBottom.Y + t * (_planeTop.Y - _planeBottom.Y));
        double blockW = 36;
        double blockH = 24;
        Rect blockRect = new Rect(blockCenter.X - blockW / 2, blockCenter.Y - blockH / 2, blockW, blockH);
        dc.DrawRectangle(Brushes.LightSteelBlue, new Pen(Brushes.DarkSlateGray, 1), blockRect);
        var ft = new FormattedText($"{vm.Block.MassKg:F1} кг",
            System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), 11, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(ft, new Point(blockCenter.X - ft.Width / 2, blockCenter.Y - ft.Height / 2));

        // Стрелки сил (из центра груза)
        var r = vm.Engine.CurrentForces;
        DrawArrow(dc, blockCenter, new Vector(0, 1), r.WeightN, Brushes.DarkRed, "F");
        DrawArrow(dc, blockCenter, new Vector(-Math.Sin(angleRad), -Math.Cos(angleRad)), r.NormalN, Brushes.DarkGreen, "N");
        DrawArrow(dc, blockCenter, new Vector(-along.X, -along.Y), r.ForceAlongPlaneN, Brushes.DarkBlue, "m·g·sin α");
        DrawArrow(dc, blockCenter, new Vector(along.X, along.Y), r.FrictionN, Brushes.DarkOrange, "F_tr", lengthMultiplier: 1.6);
    }

    private void DrawArrow(DrawingContext dc, Point start, Vector dir, double forceN, Brush brush, string label, double lengthMultiplier = 1.0)
    {
        if (forceN <= 0) return;
        dir.Normalize();
        double len = Math.Min(forceN * ForceScalePxPerN * lengthMultiplier, 120 * lengthMultiplier);
        Point end = new Point(start.X + dir.X * len, start.Y + dir.Y * len);
        dc.DrawLine(new Pen(brush, 2), start, end);
        // Стрелка (треугольник)
        double ax = 8;
        Vector perp = new Vector(-dir.Y, dir.X);
        Point[] head = { end, new Point(end.X - dir.X * ax + perp.X * 4, end.Y - dir.Y * ax + perp.Y * 4), new Point(end.X - dir.X * ax - perp.X * 4, end.Y - dir.Y * ax - perp.Y * 4) };
        dc.DrawGeometry(brush, null, new PathGeometry(new[] { new PathFigure(head[0], new[] { new LineSegment(head[1], true), new LineSegment(head[2], true) }, true) }));
        var ft = new FormattedText(label, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), 10, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(ft, new Point(end.X + dir.X * 6, end.Y + dir.Y * 6));
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var vm = Vm;
        if (vm == null || !vm.IsDraggingEnabled) return;
        _dragStart = e.GetPosition(this);
        _dragging = true;
        CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        var vm = Vm;
        if (vm == null) return;
        Point pos = e.GetPosition(this);
        double t = ProjectToPlaneT(pos);
        t = Math.Clamp(t, 0.14, 0.86);
        vm.Block.PositionAlongPlane = t;
        vm.RefreshForces();
        vm.NotifyBlockChanged();
        InvalidateVisual();
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_dragging) EndDrag();
    }

    private void EndDrag()
    {
        _dragging = false;
        ReleaseMouseCapture();
        Vm?.ClampBlockPositionForDrag();
        InvalidateVisual();
    }

    private double ProjectToPlaneT(Point p)
    {
        Vector v = p - _planeBottom;
        Vector along = _planeTop - _planeBottom;
        double len2 = along.X * along.X + along.Y * along.Y;
        if (len2 < 1e-10) return 0.5;
        double t = (v.X * along.X + v.Y * along.Y) / len2;
        return t;
    }
}
