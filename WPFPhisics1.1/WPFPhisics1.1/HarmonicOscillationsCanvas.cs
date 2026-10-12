using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace WPFPhisics1._1;

/// <summary>
/// Рисует график x(t) гармонических колебаний и текущую точку при анимации.
/// </summary>
public class HarmonicOscillationsCanvas : FrameworkElement
{
    private readonly Queue<Point> _trail = new();

    public HarmonicOscillationsCanvas()
    {
        DataContextChanged += (s, e) =>
        {
            if (e.OldValue is HarmonicOscillationsViewModel oldVm)
            {
                if (oldVm.ChartPoints is INotifyCollectionChanged oldCol)
                    oldCol.CollectionChanged -= Chart_CollectionChanged;
                oldVm.PropertyChanged -= Vm_PropertyChanged;
            }
            if (e.NewValue is HarmonicOscillationsViewModel newVm)
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
        if (e.PropertyName is nameof(HarmonicOscillationsViewModel.CurrentTime) or nameof(HarmonicOscillationsViewModel.CurrentX)
            or nameof(HarmonicOscillationsViewModel.IsPlaying))
            InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double w = double.IsPositiveInfinity(availableSize.Width) ? 400 : Math.Max(0, availableSize.Width);
        double h = double.IsPositiveInfinity(availableSize.Height) ? 220 : Math.Max(0, availableSize.Height);
        return new Size(w, h);
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth;
        double h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        var vm = DataContext as HarmonicOscillationsViewModel;
        var bg = new LinearGradientBrush(Color.FromRgb(24, 28, 40), Color.FromRgb(18, 21, 31), 90);
        dc.DrawRoundedRectangle(bg, new Pen(new SolidColorBrush(Color.FromRgb(66, 78, 106)), 1), new Rect(0, 0, w, h), 8, 8);

        var points = vm?.ChartPoints;
        if (points == null || points.Count < 2)
        {
            var ft = new FormattedText("Задайте A, T, φ₀ и нажмите «Построить»",
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 12, new SolidColorBrush(Color.FromRgb(167, 182, 209)), VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(ft, new Point(w / 2 - 120, h / 2 - 10));
            return;
        }

        double margin = 34;
        double plotW = w - 2 * margin;
        double plotH = h - 2 * margin;
        double maxT = 0.001, maxX = 0.001;
        foreach (System.Windows.Point p in points)
        {
            if (p.X > maxT) maxT = p.X;
            if (Math.Abs(p.Y) > maxX) maxX = Math.Abs(p.Y);
        }
        maxT = Math.Max(maxT, 0.1);
        maxX = Math.Max(maxX, 0.1);

        double scaleT = plotW / maxT;
        double scaleX = plotH / (2 * maxX);
        double centerY = margin + plotH / 2;

        // Неоновая сетка
        var gridBlue = new Pen(new SolidColorBrush(Color.FromArgb(80, 90, 175, 255)), 1);
        var gridPurple = new Pen(new SolidColorBrush(Color.FromArgb(85, 185, 100, 255)), 1);
        for (int i = 0; i <= 10; i++)
        {
            double gx = margin + plotW * i / 10.0;
            dc.DrawLine(i % 2 == 0 ? gridBlue : gridPurple, new Point(gx, margin), new Point(gx, margin + plotH));
        }
        for (int i = 0; i <= 8; i++)
        {
            double gy = margin + plotH * i / 8.0;
            dc.DrawLine(i % 2 == 0 ? gridBlue : gridPurple, new Point(margin, gy), new Point(margin + plotW, gy));
        }

        // Оси
        var axisPen = new Pen(new SolidColorBrush(Color.FromRgb(224, 235, 255)), 1.2);
        dc.DrawLine(axisPen, new Point(margin, centerY), new Point(margin + plotW, centerY));
        dc.DrawLine(axisPen, new Point(margin, margin), new Point(margin, margin + plotH));

        var pts = new List<Point>();
        foreach (System.Windows.Point p in points)
        {
            double px = margin + p.X * scaleT;
            double py = centerY - p.Y * scaleX;
            pts.Add(new Point(px, py));
        }
        if (pts.Count >= 2)
        {
            var segments = pts.Skip(1).Select(pt => new LineSegment(pt, true)).ToArray();
            var path = new PathFigure(pts[0], segments, false);
            dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb(90, 46, 231, 255)), 8), new PathGeometry(new[] { path }));
            dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromRgb(40, 211, 255)), 2.6), new PathGeometry(new[] { path }));
        }

        // Текущая точка при анимации + шлейф
        if (vm != null && vm.IsPlaying)
        {
            double t = vm.CurrentTime % maxT;
            double cx = margin + t * scaleT;
            double cy = centerY - vm.CurrentX * scaleX;
            var cur = new Point(cx, cy);
            _trail.Enqueue(cur);
            while (_trail.Count > 18) _trail.Dequeue();
            int idx = 0;
            foreach (var tp in _trail)
            {
                double k = (double)(idx + 1) / _trail.Count;
                var tb = new SolidColorBrush(Color.FromArgb((byte)(30 + 120 * k), 65, 225, 255));
                dc.DrawEllipse(tb, null, tp, 2 + 2.5 * k, 2 + 2.5 * k);
                idx++;
            }
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(120, 50, 227, 255)), null, cur, 10, 10);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(102, 245, 255)), new Pen(new SolidColorBrush(Color.FromRgb(226, 250, 255)), 1), cur, 4.5, 4.5);
        }
        else
        {
            _trail.Clear();
        }

        // Подписи осей и делений
        double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var typeface = new Typeface("Segoe UI");
        var labelBrush = new SolidColorBrush(Color.FromRgb(227, 240, 255));
        var tickBrush = new SolidColorBrush(Color.FromRgb(181, 201, 232));
        dc.DrawText(new FormattedText("t (с)", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, labelBrush, pixelsPerDip), new Point(margin + plotW - 28, centerY + 6));
        dc.DrawText(new FormattedText("x (м)", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, labelBrush, pixelsPerDip), new Point(margin - 24, margin - 16));
        for (int i = 0; i <= 10; i += 2)
        {
            double tx = maxT * i / 10.0;
            double px = margin + plotW * i / 10.0;
            var tft = new FormattedText(tx.ToString("F1"), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 9, tickBrush, pixelsPerDip);
            dc.DrawText(tft, new Point(px - tft.Width / 2, centerY + 8));
        }
        for (int i = -2; i <= 2; i++)
        {
            double xv = maxX * i / 2.0;
            double py = centerY - xv * scaleX;
            var xft = new FormattedText(xv.ToString("F1"), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 9, tickBrush, pixelsPerDip);
            dc.DrawText(xft, new Point(margin - 28, py - xft.Height / 2));
        }

        // Декоративные уголки
        var decoPen = new Pen(new SolidColorBrush(Color.FromArgb(130, 188, 121, 255)), 1.2);
        dc.DrawLine(decoPen, new Point(8, 8), new Point(24, 8));
        dc.DrawLine(decoPen, new Point(8, 8), new Point(8, 24));
        dc.DrawLine(decoPen, new Point(w - 8, 8), new Point(w - 24, 8));
        dc.DrawLine(decoPen, new Point(w - 8, 8), new Point(w - 8, 24));
        dc.DrawLine(decoPen, new Point(8, h - 8), new Point(24, h - 8));
        dc.DrawLine(decoPen, new Point(8, h - 8), new Point(8, h - 24));
        dc.DrawLine(decoPen, new Point(w - 8, h - 8), new Point(w - 24, h - 8));
        dc.DrawLine(decoPen, new Point(w - 8, h - 8), new Point(w - 8, h - 24));
    }
}
