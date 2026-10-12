using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace WPFPhisics1._1;

/// <summary>Неоновая визуализация преломления с анимированным маркером.</summary>
public class RefractionAtBoundaryCanvas : FrameworkElement
{
    public RefractionAtBoundaryCanvas()
    {
        DataContextChanged += (s, e) =>
        {
            if (e.OldValue is RefractionAtBoundaryViewModel oldVm)
                oldVm.PropertyChanged -= Vm_PropertyChanged;
            if (e.NewValue is RefractionAtBoundaryViewModel newVm)
                newVm.PropertyChanged += Vm_PropertyChanged;
        };
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(RefractionAtBoundaryViewModel.N1) or nameof(RefractionAtBoundaryViewModel.N2)
            or nameof(RefractionAtBoundaryViewModel.IncidenceAngleDegrees) or nameof(RefractionAtBoundaryViewModel.RefractionAngleDegrees)
            or nameof(RefractionAtBoundaryViewModel.IsTotalInternalReflection) or nameof(RefractionAtBoundaryViewModel.AnimationProgress))
            InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double w = double.IsPositiveInfinity(availableSize.Width) ? 400 : Math.Max(0, availableSize.Width);
        double h = double.IsPositiveInfinity(availableSize.Height) ? 280 : Math.Max(0, availableSize.Height);
        return new Size(w, h);
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth;
        double h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        var vm = DataContext as RefractionAtBoundaryViewModel;
        if (vm == null)
        {
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(21, 25, 36)), new Pen(new SolidColorBrush(Color.FromRgb(55, 63, 85)), 1), new Rect(0, 0, w, h));
            return;
        }

        double margin = 20;
        double centerX = margin + (w - 2 * margin) / 2;
        double centerY = margin + (h - 2 * margin) / 2;
        double plotW = w - 2 * margin;
        double plotH = h - 2 * margin;
        double rayLen = Math.Min(plotW, plotH) * 0.5;
        double scale = rayLen; // мир: луч длины 1

        // Мир: граница y=0, нормаль x=0. Экран: screenX = centerX + x*scale, screenY = centerY - y*scale (y вверх = экран вниз от centerY)
        double ToScreenX(double xw) => centerX + xw * scale;
        double ToScreenY(double yw) => centerY - yw * scale;

        double rad1 = vm.IncidenceAngleDegrees * Math.PI / 180.0;
        double rad2 = double.IsNaN(vm.RefractionAngleDegrees) ? 0 : vm.RefractionAngleDegrees * Math.PI / 180.0;
        bool tir = vm.IsTotalInternalReflection;

        // Темный фон и неоновая сетка
        var bg = new LinearGradientBrush(Color.FromRgb(24, 28, 40), Color.FromRgb(18, 21, 31), 90);
        dc.DrawRoundedRectangle(bg, new Pen(new SolidColorBrush(Color.FromRgb(66, 78, 106)), 1), new Rect(0, 0, w, h), 8, 8);
        var gridBlue = new Pen(new SolidColorBrush(Color.FromArgb(75, 90, 175, 255)), 1);
        var gridPurple = new Pen(new SolidColorBrush(Color.FromArgb(85, 185, 100, 255)), 1);
        for (int i = 0; i <= 12; i++)
        {
            double gx = margin + plotW * i / 12.0;
            dc.DrawLine(i % 2 == 0 ? gridBlue : gridPurple, new Point(gx, margin), new Point(gx, margin + plotH));
        }
        for (int i = 0; i <= 8; i++)
        {
            double gy = margin + plotH * i / 8.0;
            dc.DrawLine(i % 2 == 0 ? gridBlue : gridPurple, new Point(margin, gy), new Point(margin + plotW, gy));
        }

        // Граница (горизонтальная линия)
        var boundaryPen = new Pen(new SolidColorBrush(Color.FromRgb(196, 226, 255)), 1.5);
        dc.DrawLine(boundaryPen, new Point(margin, centerY), new Point(w - margin, centerY));

        // Нормаль (пунктир)
        var normalPen = new Pen(new SolidColorBrush(Color.FromArgb(170, 180, 200, 225)), 1) { DashStyle = new DashStyle(new[] { 4.0, 4.0 }, 0) };
        double normHalf = Math.Min(plotH / 2 - 8, 50);
        dc.DrawLine(normalPen, new Point(centerX, centerY - normHalf), new Point(centerX, centerY + normHalf));

        // Падающий луч: из (-sin θ₁, cos θ₁) к (0, 0)
        double x1 = -Math.Sin(rad1);
        double y1 = Math.Cos(rad1);
        var incidentPenGlow = new Pen(new SolidColorBrush(Color.FromArgb(95, 90, 255, 165)), 8);
        var incidentPen = new Pen(new SolidColorBrush(Color.FromRgb(126, 255, 155)), 2.4);
        dc.DrawLine(incidentPenGlow, new Point(ToScreenX(x1), ToScreenY(y1)), new Point(centerX, centerY));
        dc.DrawLine(incidentPen, new Point(ToScreenX(x1), ToScreenY(y1)), new Point(centerX, centerY));

        Point outgoingEnd;
        if (tir)
        {
            // Отражённый луч: из (0,0) в (sin θ₁, cos θ₁) — симметрично в другую сторону
            double xRefl = Math.Sin(rad1);
            double yRefl = Math.Cos(rad1);
            outgoingEnd = new Point(ToScreenX(xRefl), ToScreenY(yRefl));
            var reflectedGlow = new Pen(new SolidColorBrush(Color.FromArgb(100, 255, 190, 100)), 8);
            var reflectedPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 211, 101)), 2.4);
            dc.DrawLine(reflectedGlow, new Point(centerX, centerY), outgoingEnd);
            dc.DrawLine(reflectedPen, new Point(centerX, centerY), outgoingEnd);
        }
        else
        {
            // Преломлённый луч: из (0,0) в (sin θ₂, -cos θ₂) — в нижнюю среду
            double x2 = Math.Sin(rad2);
            double y2 = -Math.Cos(rad2);
            outgoingEnd = new Point(ToScreenX(x2), ToScreenY(y2));
            var refractedGlow = new Pen(new SolidColorBrush(Color.FromArgb(95, 46, 231, 255)), 8);
            var refractedPen = new Pen(new SolidColorBrush(Color.FromRgb(72, 211, 255)), 2.4);
            dc.DrawLine(refractedGlow, new Point(centerX, centerY), outgoingEnd);
            dc.DrawLine(refractedPen, new Point(centerX, centerY), outgoingEnd);
        }

        // Анимированный маркер
        if (vm.IsPlaying)
        {
            Point start = new Point(ToScreenX(x1), ToScreenY(y1));
            Point split = new Point(centerX, centerY);
            double p = vm.AnimationProgress;
            Point marker = p < 0.5 ? Lerp(start, split, p / 0.5) : Lerp(split, outgoingEnd, (p - 0.5) / 0.5);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(120, 50, 227, 255)), null, marker, 12, 12);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(102, 245, 255)), new Pen(new SolidColorBrush(Color.FromRgb(226, 250, 255)), 1), marker, 4.5, 4.5);
        }

        // Подписи и угол
        double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var typeface = new Typeface("Segoe UI");
        var lightBrush = new SolidColorBrush(Color.FromRgb(227, 240, 255));
        dc.DrawText(new FormattedText("n₁", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 13, lightBrush, pixelsPerDip), new Point(centerX - 20, centerY - 34));
        dc.DrawText(new FormattedText("n₂", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 13, lightBrush, pixelsPerDip), new Point(centerX - 20, centerY + 8));
        dc.DrawText(new FormattedText($"θ₁", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 12, new SolidColorBrush(Color.FromRgb(130, 255, 170)), pixelsPerDip), new Point(ToScreenX(-0.12), ToScreenY(0.3)));
        if (!tir)
            dc.DrawText(new FormattedText($"θ₂  {vm.RefractionAngleDegrees:F1}°", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 12, new SolidColorBrush(Color.FromRgb(122, 222, 255)), pixelsPerDip), new Point(ToScreenX(0.17), ToScreenY(-0.2)));
        else
            dc.DrawText(new FormattedText("полное отражение", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, new SolidColorBrush(Color.FromRgb(255, 201, 120)), pixelsPerDip), new Point(ToScreenX(0.08), ToScreenY(0.38)));

        // Дуга для θ1 возле начала координат
        var arcPen = new Pen(new SolidColorBrush(Color.FromRgb(117, 255, 150)), 2);
        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            double r = 34;
            Point s = new Point(centerX, centerY - r);
            Point e = new Point(centerX - r * Math.Sin(rad1), centerY - r * Math.Cos(rad1));
            ctx.BeginFigure(s, false, false);
            ctx.ArcTo(e, new Size(r, r), 0, rad1 > Math.PI, SweepDirection.Counterclockwise, true, false);
        }
        dc.DrawGeometry(null, arcPen, geo);

        // Декоративные элементы в углах
        var decoPen = new Pen(new SolidColorBrush(Color.FromArgb(130, 188, 121, 255)), 1.2);
        dc.DrawLine(decoPen, new Point(8, 8), new Point(24, 8));
        dc.DrawLine(decoPen, new Point(8, 8), new Point(8, 24));
        dc.DrawLine(decoPen, new Point(w - 8, 8), new Point(w - 24, 8));
        dc.DrawLine(decoPen, new Point(w - 8, 8), new Point(w - 8, 24));
        dc.DrawText(new FormattedText("✦", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 24, new SolidColorBrush(Color.FromArgb(140, 210, 220, 255)), pixelsPerDip), new Point(w - 34, h - 38));
    }

    private static Point Lerp(Point a, Point b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return new Point(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
    }
}
