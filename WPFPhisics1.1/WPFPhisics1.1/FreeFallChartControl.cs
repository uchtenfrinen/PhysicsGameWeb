using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
namespace WPFPhisics1._1;

/// <summary>График h(t) или v(t) для свободного падения.</summary>
public class FreeFallChartControl : FrameworkElement
{
    public static readonly DependencyProperty IsVelocityChartProperty =
        DependencyProperty.Register(nameof(IsVelocityChart), typeof(bool), typeof(FreeFallChartControl), new PropertyMetadata(false));

    public bool IsVelocityChart
    {
        get => (bool)GetValue(IsVelocityChartProperty);
        set => SetValue(IsVelocityChartProperty, value);
    }

    private const double MarginLeft = 40;
    private const double MarginRight = 14;
    private const double MarginTop = 10;
    private const double MarginBottom = 28;

    private static readonly Color NeonLineH = Color.FromRgb(0x5C, 0xE1, 0xFF);
    private static readonly Color NeonLineV = Color.FromRgb(0x7A, 0xF0, 0x9A);
    private static readonly Color GridColor = Color.FromArgb(0x48, 0x9B, 0x6B, 0xFF);

    private FreeFallViewModel? _vm;

    public FreeFallChartControl()
    {
        SizeChanged += (_, _) => InvalidateVisual();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is FreeFallViewModel oldVm)
        {
            if (oldVm.HeightChartPoints is INotifyCollectionChanged oh)
                oh.CollectionChanged -= Points_CollectionChanged;
            if (oldVm.VelocityChartPoints is INotifyCollectionChanged ov)
                ov.CollectionChanged -= Points_CollectionChanged;
            oldVm.PropertyChanged -= VmOnPropertyChanged;
        }

        _vm = e.NewValue as FreeFallViewModel;
        if (_vm != null)
        {
            if (_vm.HeightChartPoints is INotifyCollectionChanged nh)
                nh.CollectionChanged += Points_CollectionChanged;
            if (_vm.VelocityChartPoints is INotifyCollectionChanged nv)
                nv.CollectionChanged += Points_CollectionChanged;
            _vm.PropertyChanged += VmOnPropertyChanged;
        }
        InvalidateVisual();
    }

    private void VmOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(FreeFallViewModel.CurrentTime) or nameof(FreeFallViewModel.IsPlaying)
            or nameof(FreeFallViewModel.Height0) or nameof(FreeFallViewModel.TimeOfFall))
            InvalidateVisual();
    }

    private void Points_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateVisual();

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

        var vm = DataContext as FreeFallViewModel;
        var points = IsVelocityChart ? vm?.VelocityChartPoints : vm?.HeightChartPoints;

        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var darkBg = new LinearGradientBrush(
            Color.FromRgb(0x19, 0x1D, 0x28),
            Color.FromRgb(0x12, 0x14, 0x1C),
            new Point(0, 0),
            new Point(1, 1));
        var gridPen = new Pen(new SolidColorBrush(GridColor), 0.55);
        var axisPen = new Pen(new SolidColorBrush(Color.FromRgb(0x7A, 0xA8, 0xE8)), 1);
        var textBrush = new SolidColorBrush(Color.FromRgb(0xB8, 0xCC, 0xE8));
        var lineColor = IsVelocityChart ? NeonLineV : NeonLineH;

        if (points == null || points.Count < 2)
        {
            dc.DrawRectangle(darkBg, new Pen(new SolidColorBrush(Color.FromRgb(0x3A, 0x4C, 0x66)), 1), new Rect(0, 0, w, h));
            var tf = new Typeface("Segoe UI");
            var ft = new FormattedText("Нажмите «Построить траекторию»",
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                tf, 11, textBrush, dpi);
            dc.DrawText(ft, new Point(12, h / 2 - 8));
            return;
        }

        double maxT = 0.001, maxY = 0.001;
        foreach (Point p in points)
        {
            if (p.X > maxT) maxT = p.X;
            if (p.Y > maxY) maxY = p.Y;
        }
        maxT = Math.Max(maxT, 0.1);
        maxY = Math.Max(maxY, 0.01);

        double plotW = w - MarginLeft - MarginRight;
        double plotH = h - MarginTop - MarginBottom;
        if (plotW <= 0 || plotH <= 0) return;

        dc.DrawRectangle(darkBg, new Pen(new LinearGradientBrush(
            Color.FromRgb(0x4A, 0x3A, 0x7A),
            Color.FromRgb(0x2F, 0x60, 0x90),
            new Point(0, 0), new Point(1, 1)), 1), new Rect(0, 0, w, h));
        dc.DrawLine(axisPen, new Point(MarginLeft, MarginTop), new Point(MarginLeft, h - MarginBottom));
        dc.DrawLine(axisPen, new Point(MarginLeft, h - MarginBottom), new Point(w - MarginRight, h - MarginBottom));

        int divT = 4, divY = 4;
        var typeface = new Typeface("Segoe UI");
        for (int i = 0; i <= divT; i++)
        {
            double t = maxT * i / divT;
            double x = MarginLeft + (t / maxT) * plotW;
            dc.DrawLine(gridPen, new Point(x, MarginTop), new Point(x, h - MarginBottom));
            dc.DrawLine(axisPen, new Point(x, h - MarginBottom), new Point(x, h - MarginBottom + 4));
            var ftTick = new FormattedText(t.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 9, textBrush, dpi);
            dc.DrawText(ftTick, new Point(x - ftTick.Width / 2, h - MarginBottom + 5));
        }
        for (int i = 0; i <= divY; i++)
        {
            double val = maxY * i / divY;
            double y = h - MarginBottom - (val / maxY) * plotH;
            dc.DrawLine(gridPen, new Point(MarginLeft, y), new Point(w - MarginRight, y));
            dc.DrawLine(axisPen, new Point(MarginLeft - 4, y), new Point(MarginLeft, y));
            var ftTick = new FormattedText(val.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 9, textBrush, dpi);
            dc.DrawText(ftTick, new Point(MarginLeft - ftTick.Width - 4, y - 4));
        }

        string yLabel = IsVelocityChart ? "v(t), м/с" : "h(t), м";
        var ftT = new FormattedText("t, с", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 9, textBrush, dpi);
        dc.DrawText(ftT, new Point(MarginLeft + plotW / 2 - 8, h - MarginBottom + 4));
        var ftY = new FormattedText(yLabel, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 9, textBrush, dpi);
        dc.DrawText(ftY, new Point(4, MarginTop + plotH / 2 - 8));

        var pts = new List<Point>();
        foreach (Point p in points)
        {
            double px = MarginLeft + (p.X / maxT) * plotW;
            double py = h - MarginBottom - (p.Y / maxY) * plotH;
            pts.Add(new Point(px, py));
        }
        if (pts.Count >= 2)
        {
            if (IsVelocityChart)
            {
                var fillPts = new List<Point>(pts) { new Point(pts[^1].X, h - MarginBottom), new Point(pts[0].X, h - MarginBottom) };
                var fillSegs = fillPts.Skip(1).Select(pt => new LineSegment(pt, true)).ToArray();
                var fillGeo = new PathGeometry(new[] { new PathFigure(fillPts[0], fillSegs, true) });
                var fillBrush = new LinearGradientBrush(
                    Color.FromArgb(0x88, NeonLineV.R, NeonLineV.G, NeonLineV.B),
                    Color.FromArgb(0x18, NeonLineV.R, NeonLineV.G, NeonLineV.B),
                    new Point(0.5, 0),
                    new Point(0.5, 1));
                dc.DrawGeometry(fillBrush, null, fillGeo);
            }
            var segs = pts.Skip(1).Select(pt => new LineSegment(pt, true)).ToArray();
            dc.DrawGeometry(null, new Pen(new SolidColorBrush(lineColor) { Opacity = 0.35 }, 5),
                new PathGeometry(new[] { new PathFigure(pts[0], segs, false) }));
            dc.DrawGeometry(null, new Pen(new SolidColorBrush(lineColor), 2.2),
                new PathGeometry(new[] { new PathFigure(pts[0], segs, false) }));

            if (vm != null && vm.TimeOfFall > 1e-9)
            {
                double tMark = Math.Min(Math.Max(0, vm.CurrentTime), vm.TimeOfFall);
                double yMark = IsVelocityChart
                    ? Physics.FreeFall.VelocityAt(tMark)
                    : Physics.FreeFall.HeightAt(tMark, vm.Height0);
                double mx = MarginLeft + (tMark / maxT) * plotW;
                double my = h - MarginBottom - (yMark / maxY) * plotH;
                if (tMark >= 0 && mx >= MarginLeft && mx <= w - MarginRight)
                {
                    for (int ring = 2; ring >= 0; ring--)
                    {
                        double rr = 4 + ring * 3.5;
                        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb((byte)(50 - ring * 15), lineColor.R, lineColor.G, lineColor.B)),
                            null, new Point(mx, my), rr, rr);
                    }
                    dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF)),
                        new Pen(new SolidColorBrush(lineColor), 1.5), new Point(mx, my), 4.5, 4.5);
                }
            }
        }
    }
}
