using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using WPFPhisics1._1.Physics;

namespace WPFPhisics1._1;

/// <summary>Схема с силами и разложением скорости: траектория, тело, v = vx + vy.</summary>
public class ProjectileForcesSchemeControl : FrameworkElement
{
    private const double LayoutMargin = 20;
    private const double ArrowScale = 28; // пикселей на единицу скорости (м/с)
    private const double MinArrowLen = 36; // минимальная длина стрелки, чтобы было видно
    private const double SphereRadius = 10;
    /// <summary>Доля времени полёта, в которую показываем разложение скорости (0.35 — чуть выше середины).</summary>
    private const double TimeFraction = 0.35;

    public ProjectileForcesSchemeControl()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        DataContextChanged += (s, e) =>
        {
            if (e.OldValue is ProjectileMotionViewModel oldVm)
                oldVm.PropertyChanged -= Vm_PropertyChanged;
            if (e.NewValue is ProjectileMotionViewModel newVm)
                newVm.PropertyChanged += Vm_PropertyChanged;
        };
        SizeChanged += (s, e) => { if (ActualWidth > 0 && ActualHeight > 0) InvalidateVisual(); };
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => InvalidateVisual();

    private ProjectileMotionViewModel? Vm => DataContext as ProjectileMotionViewModel;

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(Math.Max(0, availableSize.Width), Math.Max(0, availableSize.Height));
    }

    protected override void OnRender(DrawingContext dc)
    {
        var vm = Vm;
        double w = ActualWidth;
        double h = ActualHeight;
        if (w <= 0 || h <= 0 || vm == null) return;

        double range = Math.Max(0.01, vm.Range);
        double maxH = Math.Max(0.01, vm.MaxHeight);
        double y0 = vm.Y0;
        double tFlight = Math.Max(0, vm.TimeOfFlight);
        double v0 = vm.V0;
        double angleDeg = vm.AngleDegrees;

        double drawW = w - 2 * LayoutMargin;
        double drawH = h - 2 * LayoutMargin;
        double scaleX = drawW / range;
        double scaleY = drawH / maxH;
        double scale = Math.Min(scaleX, scaleY);

        // Физические координаты: x вправо, y вверх. Экран: x вправо, y вниз; начало (LayoutMargin, h - LayoutMargin).
        Point ToScreen(double x, double y)
        {
            return new Point(LayoutMargin + x * scale, h - LayoutMargin - y * scale);
        }

        var darkBg = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x30));
        var borderPen = new Pen(new SolidColorBrush(Color.FromRgb(0x3F, 0x3F, 0x46)), 1);
        dc.DrawRectangle(darkBg, borderPen, new Rect(0, 0, w, h));

        int nPoints = 32;
        var trajectoryPoints = new List<Point>();
        for (int i = 0; i <= nPoints; i++)
        {
            double t = (i / (double)nPoints) * tFlight;
            var (px, py) = ProjectileMotion.PositionAt(v0, angleDeg, t, y0);
            trajectoryPoints.Add(ToScreen(px, py));
        }
        if (trajectoryPoints.Count >= 2)
        {
            var path = new PathGeometry();
            var figure = new PathFigure(trajectoryPoints[0], new List<PathSegment>(), false);
            for (int i = 1; i < trajectoryPoints.Count; i++)
                figure.Segments.Add(new LineSegment(trajectoryPoints[i], true));
            path.Figures.Add(figure);
            dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromRgb(0xE5, 0x94, 0x00)), 2), path);
        }

        double tSample = tFlight * TimeFraction;
        var (sx, sy) = ProjectileMotion.PositionAt(v0, angleDeg, tSample, y0);
        Point centerScreen = ToScreen(sx, sy);
        var (vx, vy) = ProjectileMotion.VelocityAt(v0, angleDeg, tSample);

        dc.DrawEllipse(
            new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x88)),
            new Pen(new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)), 0.8),
            centerScreen, SphereRadius, SphereRadius);

        double vLen = Math.Sqrt(vx * vx + vy * vy);
        if (vLen < 1e-6) vLen = 1;

        double lenPx = Math.Max(MinArrowLen, Math.Min(ArrowScale * vLen, 0.45 * Math.Min(drawW, drawH)));
        double lvx = (vx / vLen) * lenPx;
        double lvy = (vy / vLen) * lenPx;
        Vector dirVx = new Vector(1, 0);
        Vector dirVy = new Vector(0, -1);
        Vector dirV = new Vector(vx / vLen, -vy / vLen);
        dirV.Normalize();

        DrawArrow(dc, centerScreen, dirV, lenPx, new SolidColorBrush(Color.FromRgb(0xE5, 0x94, 0x00)));
        DrawArrow(dc, centerScreen, dirVx, Math.Abs(lvx), new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0x6B)));
        DrawArrow(dc, centerScreen, dirVy, Math.Abs(lvy), new SolidColorBrush(Color.FromRgb(0x56, 0x9C, 0xD6)));

        Point endV = new Point(centerScreen.X + dirV.X * lenPx, centerScreen.Y + dirV.Y * lenPx);
        Point endVx = new Point(centerScreen.X + lvx, centerScreen.Y);
        Point endVy = new Point(centerScreen.X, centerScreen.Y - lvy);
        var dashPen = new Pen(new SolidColorBrush(Color.FromArgb(0x99, 0xCC, 0xCC, 0xCC)), 1) { DashStyle = DashStyles.Dash };
        dc.DrawLine(dashPen, endV, endVx);
        dc.DrawLine(dashPen, endV, endVy);

        var typeface = new Typeface("Segoe UI");
        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        dc.DrawText(Formatted("v", 12, Color.FromRgb(0xE5, 0x94, 0x00)), new Point(endV.X + 2, endV.Y - 4));
        dc.DrawText(Formatted("vx", 11, Color.FromRgb(0x4E, 0xC9, 0x6B)), new Point(endVx.X + 2, endVx.Y - 12));
        dc.DrawText(Formatted("vy", 11, Color.FromRgb(0x56, 0x9C, 0xD6)), new Point(endVy.X - 18, endVy.Y - 2));

        FormattedText Formatted(string text, double fontSize, Color color)
        {
            return new FormattedText(text, System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, fontSize, new SolidColorBrush(color), dpi);
        }
    }

    private static void DrawArrow(DrawingContext dc, Point start, Vector dir, double len, Brush brush)
    {
        if (len <= 0) return;
        dir.Normalize();
        Point end = new Point(start.X + dir.X * len, start.Y + dir.Y * len);
        dc.DrawLine(new Pen(brush, 2), start, end);
        double ax = 5;
        Vector perp = new Vector(-dir.Y, dir.X);
        Point[] head = { end, new Point(end.X - dir.X * ax + perp.X * 2.5, end.Y - dir.Y * ax + perp.Y * 2.5), new Point(end.X - dir.X * ax - perp.X * 2.5, end.Y - dir.Y * ax - perp.Y * 2.5) };
        dc.DrawGeometry(brush, null, new PathGeometry(new[] { new PathFigure(head[0], new PathSegment[] { new LineSegment(head[1], true), new LineSegment(head[2], true) }, true) }));
    }
}
