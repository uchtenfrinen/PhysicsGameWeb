using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace WPFPhisics1._1;

/// <summary>Тип графика: что откладывать по оси Y.</summary>
public enum ChartKind { Path, Velocity, Acceleration }

/// <summary>
/// Небольшой график для установки «груз на наклонной плоскости»: s(t), v(t) или a(t).
/// </summary>
public class ChartControl : FrameworkElement
{
    private const double MarginLeft = 40;
    private const double MarginRight = 14;
    private const double MarginTop = 14;
    private const double MarginBottom = 38;

    public static readonly System.Windows.DependencyProperty ChartKindProperty =
        System.Windows.DependencyProperty.Register(nameof(Kind), typeof(ChartKind), typeof(ChartControl), new PropertyMetadata(ChartKind.Path));

    public ChartKind Kind
    {
        get => (ChartKind)GetValue(ChartKindProperty);
        set => SetValue(ChartKindProperty, value);
    }

    public ChartControl()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        SizeChanged += (s, e) => InvalidateVisual();
        DataContextChanged += (s, e) =>
        {
            if (e.OldValue is MainViewModel oldVm && oldVm.ChartData is INotifyCollectionChanged oldCol)
                oldCol.CollectionChanged -= ChartData_CollectionChanged;
            if (e.NewValue is MainViewModel newVm && newVm.ChartData is INotifyCollectionChanged newCol)
                newCol.CollectionChanged += ChartData_CollectionChanged;
        };
    }

    private void ChartData_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateVisual();

    protected override Size MeasureOverride(Size availableSize)
    {
        // WPF не допускает возврата PositiveInfinity в DesiredSize — только конечные значения.
        double w = double.IsPositiveInfinity(availableSize.Width) ? 0 : Math.Max(0, availableSize.Width);
        double h = double.IsPositiveInfinity(availableSize.Height) ? 0 : Math.Max(0, availableSize.Height);
        return new Size(w, h);
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth;
        double h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        var vm = DataContext as MainViewModel;
        var points = vm?.ChartData;
        if (points == null || points.Count == 0)
        {
            DrawEmptyState(dc, w, h);
            return;
        }

        double maxT = 0.001;
        double maxY = 0.001;
        foreach (var p in points)
        {
            if (p.TimeSec > maxT) maxT = p.TimeSec;
            double yVal = GetYValue(p);
            if (Math.Abs(yVal) > maxY) maxY = Math.Abs(yVal);
        }
        maxT = Math.Max(maxT, 0.5);
        if (Kind == ChartKind.Acceleration) maxY = Math.Max(maxY, 0.5);
        else maxY = Math.Max(maxY, 0.1);

        double plotW = w - MarginLeft - MarginRight;
        double plotH = h - MarginTop - MarginBottom;
        if (plotW <= 0 || plotH <= 0) return;

        dc.DrawRectangle(Brushes.White, new Pen(Brushes.LightGray, 1), new Rect(0, 0, w, h));
        var axisPen = new Pen(Brushes.Black, 1);
        var gridPen = new Pen(Brushes.LightGray, 0.5);
        dc.DrawLine(axisPen, new Point(MarginLeft, MarginTop), new Point(MarginLeft, h - MarginBottom));
        dc.DrawLine(axisPen, new Point(MarginLeft, h - MarginBottom), new Point(w - MarginRight, h - MarginBottom));

        int divT = 4;
        int divY = 4;
        for (int i = 0; i <= divT; i++)
        {
            double t = maxT * i / divT;
            double x = MarginLeft + (t / maxT) * plotW;
            dc.DrawLine(gridPen, new Point(x, MarginTop), new Point(x, h - MarginBottom));
            dc.DrawLine(axisPen, new Point(x, h - MarginBottom), new Point(x, h - MarginBottom + 4));
            DrawText(dc, t.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), x - 8, h - MarginBottom + 4, 11);
        }
        for (int i = 0; i <= divY; i++)
        {
            double val = maxY * i / divY;
            double y = h - MarginBottom - (val / maxY) * plotH;
            dc.DrawLine(gridPen, new Point(MarginLeft, y), new Point(w - MarginRight, y));
            dc.DrawLine(axisPen, new Point(MarginLeft - 4, y), new Point(MarginLeft, y));
            DrawText(dc, val.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), MarginLeft - 38, y - 6, 10);
        }

        string yLabel = Kind switch { ChartKind.Path => "s", ChartKind.Velocity => "v", _ => "a" };
        DrawText(dc, "t, с", MarginLeft + plotW / 2 - 14, h - MarginBottom + 2, 11);
        DrawText(dc, yLabel, 2, MarginTop + plotH / 2 - 14, 11);

        var pts = new List<Point>();
        foreach (var p in points)
        {
            double x = MarginLeft + (p.TimeSec / maxT) * plotW;
            double yVal = GetYValue(p);
            double y = h - MarginBottom - (yVal / maxY) * plotH;
            pts.Add(new Point(x, y));
        }
        if (pts.Count >= 2)
        {
            var segments = pts.Skip(1).Select(pt => new LineSegment(pt, true)).ToArray();
            var path = new PathFigure(pts[0], segments, false);
            var geom = new PathGeometry(new[] { path });
            var color = Kind switch { ChartKind.Path => Brushes.DarkBlue, ChartKind.Velocity => Brushes.DarkGreen, _ => Brushes.DarkOrange };
            dc.DrawGeometry(null, new Pen(color, 1.5), geom);
        }
    }

    private double GetYValue(ChartPoint p) => Kind switch
    {
        ChartKind.Path => p.PathM,
        ChartKind.Velocity => p.VelocityMps,
        _ => p.AccelerationMps2
    };

    private void DrawEmptyState(DrawingContext dc, double w, double h)
    {
        dc.DrawRectangle(Brushes.White, new Pen(Brushes.LightGray, 1), new Rect(0, 0, w, h));
        string hint = Kind switch
        {
            ChartKind.Path => "s(t)",
            ChartKind.Velocity => "v(t)",
            _ => "a(t)"
        };
        DrawText(dc, hint, w / 2 - 16, h / 2 - 8, 12);
    }

    private void DrawText(DrawingContext dc, string text, double x, double y, double fontSize)
    {
        var ft = new FormattedText(text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), fontSize, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(ft, new Point(x, y));
    }
}
