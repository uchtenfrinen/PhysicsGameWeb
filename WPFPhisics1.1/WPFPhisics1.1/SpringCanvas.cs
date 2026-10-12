using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace WPFPhisics1._1;

/// <summary>
/// Вертикальная пружина с грузом: балка сверху, неоновая пружина, груз с подсветкой.
/// </summary>
public class SpringCanvas : FrameworkElement
{
    /// <summary>Общий масштаб визуала относительно первой версии (~×2).</summary>
    private const double VisScale = 2.0;

    private const double LayoutMargin = 28 * VisScale;
    private const double BarHeight = 18 * VisScale;
    private const double BarHalfWidth = 60 * VisScale;
    private const double CoilCount = 14;
    private const double CubeSize = 32 * VisScale;
    private const double ArrowLen = 28 * VisScale;

    public SpringCanvas()
    {
        DataContextChanged += (s, e) =>
        {
            if (e.OldValue is SpringOscillatorViewModel oldVm)
            {
                if (oldVm.ChartPoints is INotifyCollectionChanged oldCol)
                    oldCol.CollectionChanged -= Chart_CollectionChanged;
                oldVm.PropertyChanged -= Vm_PropertyChanged;
            }
            if (e.NewValue is SpringOscillatorViewModel newVm)
            {
                if (newVm.ChartPoints is INotifyCollectionChanged newCol)
                    newCol.CollectionChanged += Chart_CollectionChanged;
                newVm.PropertyChanged += Vm_PropertyChanged;
            }
        };
    }

    private void Chart_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateVisual();
    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SpringOscillatorViewModel.CurrentX) or nameof(SpringOscillatorViewModel.IsPlaying)
            or nameof(SpringOscillatorViewModel.K) or nameof(SpringOscillatorViewModel.M) or nameof(SpringOscillatorViewModel.Damping))
            InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double w = double.IsPositiveInfinity(availableSize.Width) ? 0 : Math.Max(0, availableSize.Width);
        double h = double.IsPositiveInfinity(availableSize.Height) ? 0 : Math.Max(0, availableSize.Height);
        return new Size(w, h);
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth;
        double h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        var vm = DataContext as SpringOscillatorViewModel;
        double x = vm?.CurrentX ?? vm?.X0 ?? 0.1;

        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var typeface = new Typeface("Segoe UI");

        // Фон: глубокий градиент + лёгкая сетка
        var bgBrush = new LinearGradientBrush(
            Color.FromRgb(0x12, 0x16, 0x22),
            Color.FromRgb(0x0A, 0x0D, 0x14),
            new Point(0, 0),
            new Point(1, 1));
        dc.DrawRectangle(bgBrush, null, new Rect(0, 0, w, h));

        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(0x35, 0x5C, 0xE1, 0xFF)), 0.6);
        gridPen.DashStyle = DashStyles.Dash;
        for (double yy = LayoutMargin; yy <= h - LayoutMargin; yy += 28 * VisScale)
            dc.DrawLine(gridPen, new Point(LayoutMargin, yy), new Point(w - LayoutMargin, yy));
        for (double xx = LayoutMargin; xx <= w - LayoutMargin; xx += 44 * VisScale)
            dc.DrawLine(gridPen, new Point(xx, LayoutMargin), new Point(xx, h - LayoutMargin));

        // Неоновая рамка зоны
        var framePen = new Pen(new LinearGradientBrush(
            Color.FromRgb(0x3F, 0xA2, 0xFF),
            Color.FromRgb(0x9B, 0x6B, 0xFF),
            new Point(0, 0),
            new Point(1, 1)), 1.4);
        dc.DrawRectangle(null, framePen, new Rect(2, 2, w - 4, h - 4));

        double centerX = w / 2;

        // Масштаб по вертикали (~×2 к прежнему)
        double pixelsPerMeter = (h - LayoutMargin * 2) * 0.55;
        if (pixelsPerMeter < 80 * VisScale) pixelsPerMeter = 80 * VisScale;

        double barTop = LayoutMargin + 8;
        double barBottom = barTop + BarHeight;
        double springTopY = barBottom + 6 * VisScale + 4;
        double equilibriumY = springTopY + 50 * VisScale;

        DrawCeilingBeam(dc, centerX, barTop, springTopY);
        double stretchPx = x * pixelsPerMeter;
        double cubeTopY = equilibriumY + stretchPx;
        double cubeCenterY = cubeTopY + CubeSize / 2;
        double springBottomY = cubeTopY;

        double springLen = springBottomY - springTopY;
        if (springLen < 28 * VisScale) springLen = 28 * VisScale;
        double amp = Math.Min(14 * VisScale, springLen / 7);

        var pts = new List<Point> { new Point(centerX, springTopY) };
        for (int i = 1; i < CoilCount; i++)
        {
            double t = i / (double)CoilCount;
            double py = springTopY + springLen * t;
            double px = centerX + (i % 2 == 0 ? amp : -amp);
            pts.Add(new Point(px, py));
        }
        pts.Add(new Point(centerX, springBottomY));
        if (pts.Count >= 2)
        {
            var segs = pts.Skip(1).Select(p => new LineSegment(p, true)).ToArray();
            var pathGeo = new PathGeometry(new[] { new PathFigure(pts[0], segs, false) });
            // Свечение пружины
            dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb(0x55, 0x5C, 0xE1, 0xFF)), 5), pathGeo);
            dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb(0x99, 0x88, 0xDD, 0xFF)), 3), pathGeo);
            dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromRgb(0xB0, 0xE0, 0xFF)), 2.2), pathGeo);
        }

        DrawMassBlock(dc, centerX, cubeTopY, CubeSize, dpi);

        var dashPen = new Pen(new SolidColorBrush(Color.FromArgb(0xAA, 0x5C, 0xE1, 0xFF)), 1) { DashStyle = DashStyles.Dash };
        dc.DrawLine(dashPen, new Point(centerX - 70 * VisScale, equilibriumY), new Point(centerX + 70 * VisScale, equilibriumY));
        var ftEq = new FormattedText("равновесие", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10 * VisScale / 2,
            new SolidColorBrush(Color.FromRgb(0x8E, 0xA1, 0xBE)), dpi);
        dc.DrawText(ftEq, new Point(centerX - 85 * VisScale - ftEq.Width, equilibriumY - 6));

        dc.DrawLine(dashPen, new Point(centerX - 50 * VisScale, cubeTopY), new Point(centerX + 50 * VisScale, cubeTopY));

        var greenBrush = new SolidColorBrush(Color.FromRgb(0x56, 0xD9, 0x81));
        var redBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x7A, 0x8A));
        DrawArrow(dc, new Point(centerX + CubeSize / 2 + 22 * VisScale, cubeTopY + CubeSize / 2), new Vector(0, -1), ArrowLen, greenBrush, 2.2);
        var ftFup = new FormattedText("Fупр = k·x", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 11 * VisScale / 2, greenBrush, dpi);
        dc.DrawText(ftFup, new Point(centerX + CubeSize / 2 + 28 * VisScale, cubeTopY + CubeSize / 2 - ArrowLen - 18 * VisScale));

        DrawArrow(dc, new Point(centerX - CubeSize / 2 - 22 * VisScale, cubeCenterY), new Vector(0, 1), ArrowLen, redBrush, 2.2);
        var ftFmg = new FormattedText("F = m·g", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 11 * VisScale / 2, redBrush, dpi);
        dc.DrawText(ftFmg, new Point(centerX - CubeSize / 2 - 28 * VisScale - ftFmg.Width, cubeCenterY + ArrowLen + 6 * VisScale));

        double omega = vm != null ? Math.Sqrt(vm.K / vm.M) : 3.16;
        var ftOmega = new FormattedText($"ω₀ = √(k/m) = {omega:F2} рад/с", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 12 * VisScale / 2,
            new SolidColorBrush(Color.FromRgb(0xC7, 0xDA, 0xF4)), dpi);
        dc.DrawText(ftOmega, new Point(centerX - ftOmega.Width / 2, h - LayoutMargin - 8));

        if (vm != null)
        {
            var ftParams = new FormattedText($"m = {vm.M:F2} кг   k = {vm.K:F1} Н/м   ζ = {vm.Damping:F2}",
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10 * VisScale / 2,
                new SolidColorBrush(Color.FromRgb(0x9C, 0xB4, 0xD6)), dpi);
            dc.DrawText(ftParams, new Point(centerX - ftParams.Width / 2, springTopY - ftParams.Height - 6));
        }

        DrawCornerAccents(dc, w, h);
    }

    private static void DrawCeilingBeam(DrawingContext dc, double centerX, double barTop, double springAttachY)
    {
        var beamRect = new Rect(centerX - BarHalfWidth, barTop, BarHalfWidth * 2, BarHeight);
        var beamFill = new LinearGradientBrush(
            Color.FromRgb(0x3A, 0x42, 0x55),
            Color.FromRgb(0x22, 0x28, 0x36),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawRectangle(beamFill, new Pen(new SolidColorBrush(Color.FromRgb(0x5A, 0x7A, 0xA8)), 1.2), beamRect);

        double boltY = barTop + BarHeight / 2;
        foreach (double ox in new[] { -42 * VisScale, 42 * VisScale })
        {
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(0x55, 0x60, 0x72)), new Pen(new SolidColorBrush(Color.FromRgb(0x88, 0x99, 0xAA)), 1),
                new Point(centerX + ox, boltY), 4 * VisScale, 4 * VisScale);
        }

        var hangPen = new Pen(new SolidColorBrush(Color.FromRgb(0x66, 0x88, 0xAA)), 2);
        double barBottom = barTop + BarHeight;
        if (springAttachY > barBottom + 1)
            dc.DrawLine(hangPen, new Point(centerX, barBottom), new Point(centerX, springAttachY));
    }

    private static void DrawMassBlock(DrawingContext dc, double centerX, double cubeTopY, double cubeSize, double dpi)
    {
        var r = new Rect(centerX - cubeSize / 2, cubeTopY, cubeSize, cubeSize);
        var glow = new Pen(new SolidColorBrush(Color.FromArgb(0x55, 0x5C, 0xE1, 0xFF)), 10);
        dc.DrawRectangle(null, glow, r);

        var face = new LinearGradientBrush(
            Color.FromRgb(0x35, 0x8C, 0xE8),
            Color.FromRgb(0x0E, 0x4A, 0x9E),
            new Point(0, 0),
            new Point(1, 1));
        dc.DrawRectangle(face, new Pen(new SolidColorBrush(Color.FromRgb(0x88, 0xCC, 0xFF)), 1.5), r);

        double hi = cubeSize * 0.35;
        var hiBrush = new LinearGradientBrush(
            Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF),
            Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF),
            new Point(0, 0),
            new Point(0, 1));
        dc.DrawRectangle(hiBrush, null, new Rect(r.X + cubeSize * 0.15, r.Y + cubeSize * 0.12, cubeSize * 0.35, hi));

        var ft = new FormattedText("m", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), 14 * VisScale / 2, new SolidColorBrush(Color.FromRgb(0xE8, 0xF4, 0xFF)), dpi);
        dc.DrawText(ft, new Point(centerX - ft.Width / 2, cubeTopY + cubeSize / 2 - ft.Height / 2));
    }

    private static void DrawCornerAccents(DrawingContext dc, double w, double h)
    {
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(0x44, 0x5C, 0xE1, 0xFF)), 1);
        double s = 14 * VisScale;
        dc.DrawLine(pen, new Point(8, h - 8), new Point(8 + s, h - 8));
        dc.DrawLine(pen, new Point(8, h - 8), new Point(8, h - 8 - s));
        dc.DrawLine(pen, new Point(w - 8, h - 8), new Point(w - 8 - s, h - 8));
        dc.DrawLine(pen, new Point(w - 8, h - 8), new Point(w - 8, h - 8 - s));
    }

    private static void DrawArrow(DrawingContext dc, Point start, Vector dir, double len, Brush brush, double thickness = 1.5)
    {
        dir.Normalize();
        Point end = new Point(start.X + dir.X * len, start.Y + dir.Y * len);
        dc.DrawLine(new Pen(brush, thickness), start, end);
        double ax = 6 * VisScale / 2;
        Vector perp = new Vector(-dir.Y, dir.X);
        Point[] head = { end, new Point(end.X - dir.X * ax + perp.X * 3, end.Y - dir.Y * ax + perp.Y * 3), new Point(end.X - dir.X * ax - perp.X * 3, end.Y - dir.Y * ax - perp.Y * 3) };
        dc.DrawGeometry(brush, null, new PathGeometry(new[] { new PathFigure(head[0], new PathSegment[] { new LineSegment(head[1], true), new LineSegment(head[2], true) }, true) }));
    }
}
