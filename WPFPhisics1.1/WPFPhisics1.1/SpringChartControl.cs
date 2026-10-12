using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace WPFPhisics1._1;

/// <summary>График x(t), v(t) или a(t) для пружинного маятника. Тёмная тема, пунктирная сетка, синяя линия, жёлтые точки.</summary>
public class SpringChartControl : FrameworkElement
{
    public static readonly System.Windows.DependencyProperty ChartKindProperty = System.Windows.DependencyProperty.Register(
        nameof(ChartKind), typeof(SpringChartKind), typeof(SpringChartControl), new System.Windows.PropertyMetadata(SpringChartKind.Position));

    public SpringChartKind ChartKind
    {
        get => (SpringChartKind)GetValue(ChartKindProperty);
        set => SetValue(ChartKindProperty, value);
    }

    private const double MarginLeft = 44;
    private const double MarginRight = 14;
    private const double MarginTop = 12;
    private const double MarginBottom = 28;
    private static readonly SolidColorBrush DarkBg = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x30));
    private static readonly SolidColorBrush GridPenBrush = new SolidColorBrush(Color.FromArgb(0x50, 0xCC, 0xCC, 0xCC));
    private static readonly SolidColorBrush TextBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
    private static readonly SolidColorBrush BlueLineBrush = new SolidColorBrush(Color.FromRgb(0x5C, 0xE1, 0xFF));
    private static readonly SolidColorBrush YellowPointBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xEB, 0x3B));
    private static readonly SolidColorBrush ZeroLineBrush = new SolidColorBrush(Color.FromArgb(0x60, 0xE0, 0xE0, 0xE0));

    public SpringChartControl()
    {
        DataContextChanged += (s, e) =>
        {
            if (e.OldValue is SpringOscillatorViewModel oldVm)
                Unsubscribe(oldVm);
            if (e.NewValue is SpringOscillatorViewModel newVm)
                Subscribe(newVm);
        };
    }

    private void Unsubscribe(SpringOscillatorViewModel vm)
    {
        if (vm.ChartPoints is INotifyCollectionChanged c) c.CollectionChanged -= Points_CollectionChanged;
        if (vm.VelocityChartPoints is INotifyCollectionChanged v) v.CollectionChanged -= Points_CollectionChanged;
        if (vm.AccelerationChartPoints is INotifyCollectionChanged a) a.CollectionChanged -= Points_CollectionChanged;
    }

    private void Subscribe(SpringOscillatorViewModel vm)
    {
        if (vm.ChartPoints is INotifyCollectionChanged c) c.CollectionChanged += Points_CollectionChanged;
        if (vm.VelocityChartPoints is INotifyCollectionChanged v) v.CollectionChanged += Points_CollectionChanged;
        if (vm.AccelerationChartPoints is INotifyCollectionChanged a) a.CollectionChanged += Points_CollectionChanged;
    }

    private void Points_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateVisual();

    private System.Collections.Generic.ICollection<System.Windows.Point>? GetPoints(SpringOscillatorViewModel? vm)
    {
        if (vm == null) return null;
        return ChartKind switch
        {
            SpringChartKind.Velocity => vm.VelocityChartPoints,
            SpringChartKind.Acceleration => vm.AccelerationChartPoints,
            _ => vm.ChartPoints
        };
    }

    private string GetYAxisLabel()
    {
        return ChartKind switch
        {
            SpringChartKind.Velocity => "v, м/с",
            SpringChartKind.Acceleration => "a, м/с²",
            _ => "x, м"
        };
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
        var points = GetPoints(vm);
        if (points == null || points.Count < 2)
        {
            dc.DrawRectangle(DarkBg, null, new Rect(0, 0, w, h));
            var tf = new Typeface("Segoe UI");
            var ft = new FormattedText("Нет данных", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf, 12, TextBrush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(ft, new Point(w / 2 - ft.Width / 2, h / 2 - ft.Height / 2));
            return;
        }

        double maxT = 0.001, maxY = 0.001;
        foreach (System.Windows.Point p in points)
        {
            if (p.X > maxT) maxT = p.X;
            if (Math.Abs(p.Y) > maxY) maxY = Math.Abs(p.Y);
        }
        maxT = Math.Max(maxT, 0.1);
        maxY = Math.Max(maxY, 0.01);

        double plotW = w - MarginLeft - MarginRight;
        double plotH = h - MarginTop - MarginBottom;
        if (plotW <= 0 || plotH <= 0) return;

        double centerY = h - MarginBottom - plotH / 2;
        var typeface = new Typeface("Segoe UI");
        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var gridPen = new Pen(GridPenBrush, 0.5) { DashStyle = DashStyles.Dash };
        var zeroPen = new Pen(ZeroLineBrush, 1) { DashStyle = DashStyles.Dash };

        dc.DrawRectangle(DarkBg, null, new Rect(0, 0, w, h));

        // Сетка пунктиром
        int divT = 6, divY = 4;
        for (int i = 0; i <= divT; i++)
        {
            double val = maxT * i / divT;
            double x = MarginLeft + (val / maxT) * plotW;
            dc.DrawLine(gridPen, new Point(x, MarginTop), new Point(x, h - MarginBottom));
            var ft = new FormattedText(val.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 9, TextBrush, dpi);
            dc.DrawText(ft, new Point(x - ft.Width / 2, h - MarginBottom + 4));
        }
        for (int i = 0; i <= divY; i++)
        {
            double val = maxY * (2 * i / (double)divY - 1);
            double yy = centerY - (val / maxY) * (plotH / 2);
            dc.DrawLine(gridPen, new Point(MarginLeft, yy), new Point(w - MarginRight, yy));
            var ft = new FormattedText(val.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 9, TextBrush, dpi);
            dc.DrawText(ft, new Point(MarginLeft - ft.Width - 4, yy - ft.Height / 2));
        }

        dc.DrawLine(new Pen(TextBrush, 1), new Point(MarginLeft, MarginTop), new Point(MarginLeft, h - MarginBottom));
        dc.DrawLine(new Pen(TextBrush, 1), new Point(MarginLeft, h - MarginBottom), new Point(w - MarginRight, h - MarginBottom));
        dc.DrawLine(zeroPen, new Point(MarginLeft, centerY), new Point(w - MarginRight, centerY));

        var ftT = new FormattedText("t, с", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, TextBrush, dpi);
        dc.DrawText(ftT, new Point(MarginLeft + plotW / 2 - 10, h - MarginBottom + 4));
        var ftY = new FormattedText(GetYAxisLabel(), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, TextBrush, dpi);
        dc.DrawText(ftY, new Point(4, MarginTop + plotH / 2 - 10));

        var pts = new List<Point>();
        foreach (System.Windows.Point p in points)
        {
            double px = MarginLeft + (p.X / maxT) * plotW;
            double py = centerY - (p.Y / maxY) * (plotH / 2);
            pts.Add(new Point(px, py));
        }
        if (pts.Count >= 2)
        {
            var segs = pts.Skip(1).Select(pt => new LineSegment(pt, true)).ToArray();
            dc.DrawGeometry(null, new Pen(BlueLineBrush, 1.5), new PathGeometry(new[] { new PathFigure(pts[0], segs, false) }));
        }

        // Жёлтые точки: первая и одна около середины (например индекс points.Count/2)
        int[] indices = { 0, points.Count / 2 };
        if (ChartKind == SpringChartKind.Position && points.Count > 2)
            indices = new[] { 0, points.Count / 2 };
        double r = 4;
        foreach (int idx in indices)
        {
            if (idx < 0 || idx >= pts.Count) continue;
            var pt = pts[idx];
            dc.DrawEllipse(YellowPointBrush, new Pen(TextBrush, 0.5), pt, r, r);
        }
    }
}

public enum SpringChartKind { Position, Velocity, Acceleration }
