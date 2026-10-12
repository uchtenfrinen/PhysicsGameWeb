using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace WPFPhisics1._1;

/// <summary>
/// Рисует траекторию заряда в однородном поле (парабола) по TrajectoryPoints из ChargeInFieldViewModel.
/// </summary>
public class ChargeTrajectoryCanvas : FrameworkElement
{
    private double _pulsePhase;
    private DateTime _lastFrameUtc = DateTime.UtcNow;

    public ChargeTrajectoryCanvas()
    {
        Loaded += (_, _) =>
        {
            _lastFrameUtc = DateTime.UtcNow;
            CompositionTarget.Rendering += OnRendering;
        };
        Unloaded += (_, _) => CompositionTarget.Rendering -= OnRendering;

        DataContextChanged += (s, e) =>
        {
            if (e.OldValue is ChargeInFieldViewModel oldVm)
            {
                if (oldVm.TrajectoryPoints is INotifyCollectionChanged oldCol)
                    oldCol.CollectionChanged -= Points_CollectionChanged;
                oldVm.PropertyChanged -= Vm_PropertyChanged;
            }
            if (e.NewValue is ChargeInFieldViewModel newVm)
            {
                if (newVm.TrajectoryPoints is INotifyCollectionChanged newCol)
                    newCol.CollectionChanged += Points_CollectionChanged;
                newVm.PropertyChanged += Vm_PropertyChanged;
            }
        };
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;
        double dt = Math.Clamp((now - _lastFrameUtc).TotalSeconds, 0, 0.05);
        _lastFrameUtc = now;
        _pulsePhase = (_pulsePhase + dt * 1.8) % 1.0;
        InvalidateVisual();
    }

    private void Points_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateVisual();

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ChargeInFieldViewModel.CurrentX) or nameof(ChargeInFieldViewModel.CurrentY)
            or nameof(ChargeInFieldViewModel.IsPlaying))
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

        var vm = DataContext as ChargeInFieldViewModel;
        var points = vm?.TrajectoryPoints;
        if (points == null || points.Count < 2)
        {
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(17, 20, 30)), new Pen(new SolidColorBrush(Color.FromRgb(56, 67, 95)), 1), new Rect(0, 0, w, h));
            var ft = new FormattedText("Введите параметры и нажмите «Построить траекторию»",
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 12, new SolidColorBrush(Color.FromRgb(182, 197, 231)), VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(ft, new Point(w / 2 - 140, h / 2 - 10));
            return;
        }

        double margin = 28;
        double maxX = 0.001, maxY = 0.001;
        foreach (System.Windows.Point p in points)
        {
            if (p.X > maxX) maxX = p.X;
            if (p.Y > maxY) maxY = p.Y;
        }
        maxX = Math.Max(maxX, 1);
        maxY = Math.Max(maxY, 1);

        double plotW = w - 2 * margin;
        double plotH = h - 2 * margin;
        if (plotW <= 0 || plotH <= 0) return;

        double scale = Math.Min(plotW / maxX, plotH / maxY);

        // Фон графика
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(17, 20, 30)), new Pen(new SolidColorBrush(Color.FromRgb(67, 78, 110)), 1), new Rect(0, 0, w, h));

        // Неоновая сетка
        var gridPenX = new Pen(new SolidColorBrush(Color.FromArgb(70, 67, 173, 255)), 0.8);
        var gridPenY = new Pen(new SolidColorBrush(Color.FromArgb(65, 176, 90, 255)), 0.8);
        int gx = 14;
        int gy = 10;
        for (int i = 0; i <= gx; i++)
        {
            double xg = margin + plotW * i / gx;
            dc.DrawLine(gridPenX, new Point(xg, margin), new Point(xg, margin + plotH));
        }
        for (int i = 0; i <= gy; i++)
        {
            double yg = margin + plotH * i / gy;
            dc.DrawLine(gridPenY, new Point(margin, yg), new Point(margin + plotW, yg));
        }

        double y0 = margin + scale * maxY;
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(187, 205, 250)), 1.2), new Point(margin, y0), new Point(margin + scale * maxX, y0));

        var pts = new List<Point>();
        foreach (System.Windows.Point p in points)
        {
            double x = margin + p.X * scale;
            double y = y0 - p.Y * scale;
            pts.Add(new Point(x, y));
        }
        if (pts.Count >= 2)
        {
            var segments = pts.Skip(1).Select(pt => new LineSegment(pt, true)).ToArray();
            var path = new PathFigure(pts[0], segments, false);
            var neonPath = new PathGeometry(new[] { path });

            // Внешнее свечение + яркое ядро траектории
            dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb(85, 46, 231, 255)), 7), neonPath);
            dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromRgb(22, 209, 255)), 3), neonPath);
        }

        if (points.Count > 0)
        {
            var p0 = points[0];
            double x0 = margin + p0.X * scale;
            double yy0 = y0 - p0.Y * scale;

            // Пульсирующая начальная точка (неоново-зелёная)
            double pulse = 6 + 2.5 * Math.Sin(_pulsePhase * Math.PI * 2);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(60, 57, 255, 153)), null, new Point(x0, yy0), pulse * 1.9, pulse * 1.9);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(170, 57, 255, 153)), null, new Point(x0, yy0), pulse, pulse);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(27, 255, 140)), new Pen(new SolidColorBrush(Color.FromArgb(180, 130, 255, 195)), 1), new Point(x0, yy0), 5, 5);
        }

        if (points.Count > 1)
        {
            var p1 = points[points.Count - 1];
            double x1 = margin + p1.X * scale;
            double y1 = y0 - p1.Y * scale;
            dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(220, 57, 255, 153)), 2), new Point(x1, y1), 6, 6);
        }

        if (vm != null && vm.IsPlaying && vm.TrajectoryPoints.Count > 0)
        {
            double bx = margin + vm.CurrentX * scale;
            double by = y0 - vm.CurrentY * scale;
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(130, 255, 196, 0)), null, new Point(bx, by), 12, 12);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 174, 0)), new Pen(new SolidColorBrush(Color.FromRgb(255, 244, 210)), 1), new Point(bx, by), 6.5, 6.5);
        }

        // Подписи осей
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var tf = new Typeface("Segoe UI");
        var fx = new FormattedText("X (м)", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf, 11, new SolidColorBrush(Color.FromRgb(206, 219, 255)), dpi);
        var fy = new FormattedText("Y (м)", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf, 11, new SolidColorBrush(Color.FromRgb(206, 219, 255)), dpi);
        dc.DrawText(fx, new Point(w - margin - fx.Width, y0 + 6));
        dc.DrawText(fy, new Point(margin - 4, margin - fy.Height - 2));
    }
}
