using System.Windows;
using System.Windows.Media;
using WPFPhisics1._1.Physics;

namespace WPFPhisics1._1;

/// <summary>
/// Уменьшенная схема: плоскость, груз, стрелки сил (без перетаскивания).
/// </summary>
public class MiniSchemeControl : FrameworkElement
{
    private const double LayoutMargin = 20;
    private const double ForceScalePxPerN = 3;

    public MiniSchemeControl()
    {
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

        double angleDeg = vm.Plane.AngleDegrees;
        double angleRad = angleDeg * Math.PI / 180.0;
        double planeLength = Math.Min((w - 2 * LayoutMargin) / Math.Cos(angleRad), (h - 2 * LayoutMargin) / Math.Sin(angleRad));
        Point planeBottom = new Point(LayoutMargin, h - LayoutMargin);
        Point planeTop = new Point(LayoutMargin + planeLength * Math.Cos(angleRad), h - LayoutMargin - planeLength * Math.Sin(angleRad));

        Vector along = planeTop - planeBottom;
        along.Normalize();
        Vector perp = new Vector(-along.Y, along.X);
        double thick = 5;
        Point[] planePoints =
        {
            planeBottom - perp * thick,
            planeBottom + perp * thick,
            planeTop + perp * thick,
            planeTop - perp * thick
        };
        var pg = new PathGeometry();
        var pf = new PathFigure(planePoints[0], new PathSegment[] { new LineSegment(planePoints[1], true), new LineSegment(planePoints[2], true), new LineSegment(planePoints[3], true) }, true);
        pg.Figures.Add(pf);
        dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(200, 200, 200)), new Pen(Brushes.Gray, 0.5), pg);

        double t = vm.Block.PositionAlongPlane;
        Point blockCenter = new Point(
            planeBottom.X + t * (planeTop.X - planeBottom.X),
            planeBottom.Y + t * (planeTop.Y - planeBottom.Y));
        double blockW = 16;
        double blockH = 10;
        dc.DrawRectangle(Brushes.LightSteelBlue, new Pen(Brushes.DarkSlateGray, 0.5), new Rect(blockCenter.X - blockW / 2, blockCenter.Y - blockH / 2, blockW, blockH));

        var r = vm.Engine.CurrentForces;
        DrawArrow(dc, blockCenter, new Vector(0, 1), r.WeightN, Brushes.DarkRed);
        DrawArrow(dc, blockCenter, new Vector(-Math.Sin(angleRad), -Math.Cos(angleRad)), r.NormalN, Brushes.DarkGreen);
        // m·g·sin α — вниз по плоскости; F_тр — вверх по плоскости
        DrawArrow(dc, blockCenter, new Vector(-along.X, -along.Y), r.ForceAlongPlaneN, Brushes.DarkBlue);
        DrawArrow(dc, blockCenter, new Vector(along.X, along.Y), r.FrictionN, Brushes.DarkOrange, lengthMultiplier: 1.6);
    }

    private void DrawArrow(DrawingContext dc, Point start, Vector dir, double forceN, Brush brush, double lengthMultiplier = 1.0)
    {
        if (forceN <= 0) return;
        dir.Normalize();
        double len = Math.Min(forceN * ForceScalePxPerN * lengthMultiplier, 40 * lengthMultiplier);
        Point end = new Point(start.X + dir.X * len, start.Y + dir.Y * len);
        dc.DrawLine(new Pen(brush, 1), start, end);
        double ax = 4;
        Vector perp = new Vector(-dir.Y, dir.X);
        Point[] head = { end, new Point(end.X - dir.X * ax + perp.X * 2, end.Y - dir.Y * ax + perp.Y * 2), new Point(end.X - dir.X * ax - perp.X * 2, end.Y - dir.Y * ax - perp.Y * 2) };
        dc.DrawGeometry(brush, null, new PathGeometry(new[] { new PathFigure(head[0], new PathSegment[] { new LineSegment(head[1], true), new LineSegment(head[2], true) }, true) }));
    }
}
