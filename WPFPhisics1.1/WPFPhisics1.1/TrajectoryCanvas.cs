using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using WPFPhisics1._1.Physics;

namespace WPFPhisics1._1;

/// <summary>
/// Рисует траекторию полёта тела (парабола) по списку точек в метрах. Сетка и координаты осей.
/// </summary>
public class TrajectoryCanvas : FrameworkElement
{
    private const double MarginLeft = 44;
    private const double MarginRight = 14;
    private const double MarginTop = 14;
    private const double MarginBottom = 32;

    public TrajectoryCanvas()
    {
        SizeChanged += (s, e) => InvalidateVisual();
        DataContextChanged += (s, e) =>
        {
            if (e.OldValue is ProjectileMotionViewModel oldVm)
            {
                if (oldVm.TrajectoryPoints is INotifyCollectionChanged oldCol)
                    oldCol.CollectionChanged -= Points_CollectionChanged;
                oldVm.PropertyChanged -= Vm_PropertyChanged;
            }
            if (e.NewValue is ProjectileMotionViewModel newVm)
            {
                if (newVm.TrajectoryPoints is INotifyCollectionChanged newCol)
                    newCol.CollectionChanged += Points_CollectionChanged;
                newVm.PropertyChanged += Vm_PropertyChanged;
            }
        };
    }

    private void Points_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateVisual();

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ProjectileMotionViewModel.CurrentX) or nameof(ProjectileMotionViewModel.CurrentY)
            or nameof(ProjectileMotionViewModel.CurrentTime) or nameof(ProjectileMotionViewModel.IsPlaying))
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

        var vm = DataContext as ProjectileMotionViewModel;
        var points = vm?.TrajectoryPoints;

        var darkBg = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x30));
        var borderPen = new Pen(new SolidColorBrush(Color.FromRgb(0x3F, 0x3F, 0x46)), 1);
        var textBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));

        if (points == null || points.Count < 2)
        {
            dc.DrawRectangle(darkBg, borderPen, new Rect(0, 0, w, h));
            var ft = new FormattedText("Нажмите «Построить траекторию»",
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 12, textBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(ft, new Point(w / 2 - 100, h / 2 - 10));
            return;
        }

        double maxX = 0.001, maxY = 0.001;
        foreach (System.Windows.Point p in points)
        {
            if (p.X > maxX) maxX = p.X;
            if (p.Y > maxY) maxY = p.Y;
        }
        maxX = Math.Max(maxX, 1);
        maxY = Math.Max(maxY, 1);

        double plotW = w - MarginLeft - MarginRight;
        double plotH = h - MarginTop - MarginBottom;
        if (plotW <= 0 || plotH <= 0) return;

        double scale = Math.Min(plotW / maxX, plotH / maxY);
        double y0Screen = MarginTop + scale * maxY;

        dc.DrawRectangle(darkBg, borderPen, new Rect(0, 0, w, h));

        var gridBrush = new SolidColorBrush(Color.FromArgb(0x18, 0x3F, 0x3F, 0x46));
        double gridStep = 28;
        for (double gx = MarginLeft; gx <= w - MarginRight; gx += gridStep)
            dc.DrawLine(new Pen(gridBrush, 0.5), new Point(gx, MarginTop), new Point(gx, h - MarginBottom));
        for (double gy = MarginTop; gy <= h - MarginBottom; gy += gridStep)
            dc.DrawLine(new Pen(gridBrush, 0.5), new Point(MarginLeft, gy), new Point(w - MarginRight, gy));

        var axisPen = new Pen(textBrush, 1);
        var typeface = new Typeface("Segoe UI");
        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        dc.DrawLine(axisPen, new Point(MarginLeft, MarginTop), new Point(MarginLeft, h - MarginBottom));
        dc.DrawLine(axisPen, new Point(MarginLeft, y0Screen), new Point(w - MarginRight, y0Screen));

        int divX = 5, divY = 4;
        for (int i = 0; i <= divX; i++)
        {
            double val = maxX * i / divX;
            double x = MarginLeft + val * scale;
            dc.DrawLine(new Pen(gridBrush, 0.5), new Point(x, MarginTop), new Point(x, h - MarginBottom));
            dc.DrawLine(axisPen, new Point(x, y0Screen), new Point(x, y0Screen + 4));
            var ft = new FormattedText(val.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, textBrush, dpi);
            dc.DrawText(ft, new Point(x - ft.Width / 2, y0Screen + 5));
        }
        for (int i = 0; i <= divY; i++)
        {
            double val = maxY * i / divY;
            double y = y0Screen - val * scale;
            dc.DrawLine(new Pen(gridBrush, 0.5), new Point(MarginLeft, y), new Point(w - MarginRight, y));
            dc.DrawLine(axisPen, new Point(MarginLeft - 4, y), new Point(MarginLeft, y));
            var ft = new FormattedText(val.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, textBrush, dpi);
            dc.DrawText(ft, new Point(MarginLeft - ft.Width - 4, y - ft.Height / 2));
        }
        dc.DrawText(new FormattedText("x, м", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 11, textBrush, dpi), new Point(MarginLeft + plotW / 2 - 12, y0Screen + 4));
        dc.DrawText(new FormattedText("y, м", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 11, textBrush, dpi), new Point(4, MarginTop + plotH / 2 - 12));

        var pts = new List<Point>();
        foreach (System.Windows.Point p in points)
        {
            double x = MarginLeft + p.X * scale;
            double y = y0Screen - p.Y * scale;
            pts.Add(new Point(x, y));
        }

        double range = vm?.Range ?? 0;

        if (pts.Count >= 2)
        {
            var segments = pts.Skip(1).Select(pt => new LineSegment(pt, true)).ToArray();
            var path = new PathFigure(pts[0], segments, false);
            var orangePen = new Pen(new SolidColorBrush(Color.FromRgb(0xE5, 0x94, 0x00)), 2) { DashStyle = new DashStyle(new[] { 6.0, 4.0 }, 0) };
            dc.DrawGeometry(null, orangePen, new PathGeometry(new[] { path }));
        }

        if (points.Count > 0)
        {
            var p0 = points[0];
            double x0 = MarginLeft + p0.X * scale;
            double yy0 = y0Screen - p0.Y * scale;
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(0xE5, 0x4C, 0x4C)), null, new Point(x0, yy0), 7, 7);
        }

        if (vm != null && pts.Count >= 2)
        {
            double rad = vm.AngleDegrees * Math.PI / 180.0;
            double v0y = vm.V0 * Math.Sin(rad);
            double tApex = v0y / ProjectileMotion.G;
            double apexXPhys = vm.V0 * Math.Cos(rad) * tApex;
            double apexYPhys = vm.Y0 + v0y * tApex - ProjectileMotion.G * tApex * tApex / 2;
            double xApex = MarginLeft + apexXPhys * scale;
            double yApex = y0Screen - apexYPhys * scale;
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(0xFF, 0xCC, 0x00)), null, new Point(xApex, yApex), 6, 6);
            double xEnd = MarginLeft + range * scale;
            double yEnd = y0Screen;
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(0xFF, 0xCC, 0x00)), null, new Point(xEnd, yEnd), 6, 6);

            var dashPen = new Pen(textBrush, 1) { DashStyle = new DashStyle(new[] { 4.0, 4.0 }, 0) };
            dc.DrawLine(dashPen, new Point(xApex, yApex), new Point(xApex, y0Screen));
            dc.DrawText(new FormattedText($"{apexYPhys:F1} м — h_max", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, textBrush, dpi), new Point(xApex + 6, (yApex + y0Screen) / 2 - 6));
        }

        // Векторы скорости v = vx + vy: при анимации — в точке тела; в покое — на траектории (~40% времени полёта)
        if (vm != null && vm.TrajectoryPoints.Count > 1 && vm.TimeOfFlight > 1e-6)
        {
            double sx, sy, tVel;
            if (vm.IsPlaying)
            {
                tVel = vm.EffectiveTime;
                sx = vm.CurrentX;
                sy = vm.CurrentY;
            }
            else
            {
                tVel = vm.TimeOfFlight * 0.4;
                (sx, sy) = ProjectileMotion.PositionAt(vm.V0, vm.AngleDegrees, tVel, vm.Y0);
            }

            var (pvx, pvy) = ProjectileMotion.VelocityAt(vm.V0, vm.AngleDegrees, tVel);
            if (Math.Sqrt(pvx * pvx + pvy * pvy) > 1e-4)
            {
                double cx = MarginLeft + sx * scale;
                double cy = y0Screen - sy * scale;
                var center = new Point(cx, cy);
                if (!vm.IsPlaying)
                    DrawVelocitySampleMarker(dc, center);
                DrawVelocityComposition(dc, center, pvx, pvy, typeface, dpi);
            }
        }

        if (vm != null && vm.IsPlaying && vm.TrajectoryPoints.Count > 0)
        {
            double bx = MarginLeft + vm.CurrentX * scale;
            double by = y0Screen - vm.CurrentY * scale;
            var ballCenter = new Point(bx, by);
            DrawNeonProjectileBall(dc, ballCenter, dpi);

            var (pvxb, pvyb) = ProjectileMotion.VelocityAt(vm.V0, vm.AngleDegrees, vm.EffectiveTime);
            double svxb = pvxb;
            double svyb = -pvyb;
            double vlenb = Math.Sqrt(svxb * svxb + svyb * svyb);
            if (vlenb > 1e-6)
            {
                Vector vdir = new Vector(svxb / vlenb, svyb / vlenb);
                Vector dragDir = new Vector(-vdir.X, -vdir.Y);
                double fgLen = 52;
                var fgBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x88, 0x99));
                DrawLabeledForce(dc, ballCenter, new Vector(0, 1), fgLen, fgBrush, "F_g", typeface, dpi, 12);
                double dragLen = 40;
                var dragBrush = new SolidColorBrush(Color.FromRgb(0xAA, 0x88, 0xFF));
                DrawLabeledForce(dc, ballCenter, dragDir, dragLen, dragBrush, "F_drag", typeface, dpi, 10);
            }
        }

        if (vm != null && pts.Count > 0)
        {
            double v0 = vm.V0;
            double angleRad = vm.AngleDegrees * Math.PI / 180.0;
            double x0 = pts[0].X;
            double y0 = pts[0].Y;
            double arrowLen = Math.Min(80, v0 * 3);
            double v0x = Math.Cos(angleRad) * arrowLen;
            double v0y = -Math.Sin(angleRad) * arrowLen;
            var greenBrush = new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0x6B));
            var v0Pen = new Pen(greenBrush, 2.5);
            dc.DrawLine(v0Pen, new Point(x0, y0), new Point(x0 + v0x, y0 + v0y));
            Point tip = new Point(x0 + v0x, y0 + v0y);
            var dir = new Vector(v0x, v0y);
            dir.Normalize();
            double ax = 6;
            var perp = new Vector(-dir.Y, dir.X);
            var head = new[] { tip, new Point(tip.X - dir.X * ax + perp.X * 3, tip.Y - dir.Y * ax + perp.Y * 3), new Point(tip.X - dir.X * ax - perp.X * 3, tip.Y - dir.Y * ax - perp.Y * 3) };
            dc.DrawGeometry(greenBrush, null, new PathGeometry(new[] { new PathFigure(head[0], new PathSegment[] { new LineSegment(head[1], true), new LineSegment(head[2], true) }, true) }));
            dc.DrawText(new FormattedText($"V₀ = {v0:F0} м/с", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 11, greenBrush, dpi), new Point(tip.X + 6, tip.Y - 6));

            double arcR = 22;
            int arcSteps = 12;
            var arcPts = new List<Point> { new Point(x0, y0) };
            for (int i = 1; i <= arcSteps; i++)
            {
                double phi = angleRad * i / arcSteps;
                arcPts.Add(new Point(x0 + arcR * Math.Cos(phi), y0 - arcR * Math.Sin(phi)));
            }
            var arcSegs = arcPts.Skip(1).Select(pt => new LineSegment(pt, true)).ToArray();
            dc.DrawGeometry(null, new Pen(textBrush, 1), new PathGeometry(new[] { new PathFigure(arcPts[0], arcSegs, false) }));
            double midPhi = angleRad * 0.5;
            double tx = x0 + (arcR + 4) * Math.Cos(midPhi);
            double ty = y0 - (arcR + 4) * Math.Sin(midPhi);
            dc.DrawText(new FormattedText($"{vm.AngleDegrees:F0}°", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, textBrush, dpi), new Point(tx - 8, ty - 6));
        }

        if (vm != null && range > 0)
        {
            double xL = MarginLeft + range * scale;
            dc.DrawText(new FormattedText($"L = {range:F2} м", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, textBrush, dpi), new Point(xL - 40, y0Screen + 6));
        }
    }

    private static void DrawNeonProjectileBall(DrawingContext dc, Point c, double dpi)
    {
        Color core = Color.FromRgb(0x5C, 0xE1, 0xFF);
        for (int i = 3; i >= 0; i--)
        {
            double rr = 10 + i * 3;
            byte a = (byte)(35 - i * 8);
            var brush = new RadialGradientBrush
            {
                GradientStops =
                {
                    new(Color.FromArgb(a, core.R, core.G, core.B), 0),
                    new(Color.FromArgb(0, core.R, core.G, core.B), 1)
                },
                Center = new Point(0.35, 0.35),
                RadiusX = 0.95,
                RadiusY = 0.95
            };
            dc.DrawEllipse(brush, null, c, rr, rr);
        }
        var coreBrush = new RadialGradientBrush
        {
            GradientStops = { new(Color.FromRgb(0xE0, 0xFB, 0xFF), 0), new(core, 1) },
            Center = new Point(0.35, 0.3),
            RadiusX = 1,
            RadiusY = 1
        };
        dc.DrawEllipse(coreBrush, new Pen(new SolidColorBrush(Color.FromRgb(0x88, 0xEE, 0xFF)), 1), c, 10, 10);
    }

    private static void DrawLabeledForce(DrawingContext dc, Point start, Vector dir, double len, Brush brush, string label, Typeface typeface, double dpi, double fontSize)
    {
        dir.Normalize();
        Point end = new Point(start.X + dir.X * len, start.Y + dir.Y * len);
        dc.DrawLine(new Pen(brush, 2.2), start, end);
        double ax = 5;
        Vector perp = new Vector(-dir.Y, dir.X);
        var head = new[] { end, new Point(end.X - dir.X * ax + perp.X * 2.5, end.Y - dir.Y * ax + perp.Y * 2.5), new Point(end.X - dir.X * ax - perp.X * 2.5, end.Y - dir.Y * ax - perp.Y * 2.5) };
        dc.DrawGeometry(brush, null, new PathGeometry(new[] { new PathFigure(head[0], new PathSegment[] { new LineSegment(head[1], true), new LineSegment(head[2], true) }, true) }));
        var ft = new FormattedText(label, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, fontSize, brush, dpi);
        dc.DrawText(ft, new Point(end.X + dir.X * 6, end.Y + dir.Y * 6 - ft.Height / 2));
    }

    private static void DrawVelocitySampleMarker(DrawingContext dc, Point c)
    {
        dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(0xAA, 0x5C, 0xE1, 0xFF)), 1.3), c, 9, 9);
    }

    /// <summary>Разложение v = vx + vy в экранных осях (vx вправо, vy вверх по физике → вверх на экране).</summary>
    private static void DrawVelocityComposition(DrawingContext dc, Point c, double pvx, double pvy, Typeface typeface, double dpi)
    {
        double vmag = Math.Sqrt(pvx * pvx + pvy * pvy);
        if (vmag < 1e-6) return;
        double k = Math.Min(3.8, 88 / Math.Max(vmag, 1));
        Point pxEnd = new Point(c.X + pvx * k, c.Y);
        Point pyEnd = new Point(c.X, c.Y - pvy * k);
        Point vEnd = new Point(c.X + pvx * k, c.Y - pvy * k);

        var dashPen = new Pen(new SolidColorBrush(Color.FromArgb(0x55, 0xAA, 0xCC, 0xDD)), 1) { DashStyle = DashStyles.Dash };
        dc.DrawLine(dashPen, pxEnd, vEnd);
        dc.DrawLine(dashPen, pyEnd, vEnd);

        DrawArrowLabeled(dc, c, pxEnd, new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0x6B)), 2.2, "vx", typeface, dpi, 10);
        DrawArrowLabeled(dc, c, pyEnd, new SolidColorBrush(Color.FromRgb(0x56, 0x9C, 0xD6)), 2.2, "vy", typeface, dpi, 10);
        DrawArrowLabeled(dc, c, vEnd, new SolidColorBrush(Color.FromRgb(0xE5, 0x94, 0x00)), 2.4, "v", typeface, dpi, 11);
    }

    private static void DrawArrowLabeled(DrawingContext dc, Point start, Point end, Brush brush, double penW, string label, Typeface tf, double dpi, double fontSize)
    {
        Vector dir = end - start;
        double len = dir.Length;
        if (len < 1e-6) return;
        dir.Normalize();
        dc.DrawLine(new Pen(brush, penW), start, end);
        double ax = 5;
        Vector perp = new Vector(-dir.Y, dir.X);
        Point tip = end;
        dc.DrawGeometry(brush, null, new PathGeometry(new[]
        {
            new PathFigure(tip, new PathSegment[]
            {
                new LineSegment(new Point(tip.X - dir.X * ax + perp.X * 2.5, tip.Y - dir.Y * ax + perp.Y * 2.5), true),
                new LineSegment(new Point(tip.X - dir.X * ax - perp.X * 2.5, tip.Y - dir.Y * ax - perp.Y * 2.5), true),
            }, true)
        }));
        var ft = new FormattedText(label, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf, fontSize, brush, dpi);
        dc.DrawText(ft, new Point(end.X + dir.X * 5 + perp.X * 3, end.Y + dir.Y * 5 + perp.Y * 3 - ft.Height / 2));
    }

}
