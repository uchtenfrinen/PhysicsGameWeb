using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace WPFPhisics1._1;

/// <summary>Схема сил для пружинного маятника: стенка, пружина, груз, сила F = −k·x.</summary>
public class SpringForcesSchemeControl : FrameworkElement
{
    private const double LayoutMargin = 14;
    private const double WallX = 22;
    private const double EquilibriumX = 90;
    private const double PixelsPerMeter = 55;
    private const double MassW = 20;
    private const double MassH = 18;
    private const double ForceScale = 0.15;
    private const double MaxArrowLen = 32;

    public SpringForcesSchemeControl()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        DataContextChanged += (s, e) =>
        {
            if (e.OldValue is SpringOscillatorViewModel oldVm)
                oldVm.PropertyChanged -= Vm_PropertyChanged;
            if (e.NewValue is SpringOscillatorViewModel newVm)
                newVm.PropertyChanged += Vm_PropertyChanged;
        };
        SizeChanged += (s, e) => { if (ActualWidth > 0 && ActualHeight > 0) InvalidateVisual(); };
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => InvalidateVisual();

    private SpringOscillatorViewModel? Vm => DataContext as SpringOscillatorViewModel;

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

        double centerY = h / 2;
        double x = vm.CurrentX;
        double massCenterX = EquilibriumX + x * PixelsPerMeter;
        massCenterX = Math.Clamp(massCenterX, WallX + 16, w - LayoutMargin - MassW / 2);

        dc.DrawRectangle(Brushes.White, new Pen(Brushes.LightGray, 1), new Rect(0, 0, w, h));

        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(140, 140, 140)), new Pen(Brushes.Black, 0.5),
            new Rect(0, centerY - 32, WallX + 3, 64));

        double springLeft = WallX + 3;
        double springRight = massCenterX - MassW / 2;
        int coils = Math.Max(3, (int)((springRight - springLeft) / 10));
        var pts = new List<Point> { new Point(springLeft, centerY) };
        for (int i = 1; i < coils; i++)
        {
            double xx = springLeft + (springRight - springLeft) * i / coils;
            double yy = centerY + (i % 2 == 0 ? 6 : -6);
            pts.Add(new Point(xx, yy));
        }
        pts.Add(new Point(springRight, centerY));
        for (int i = 0; i < pts.Count - 1; i++)
            dc.DrawLine(new Pen(Brushes.DarkSlateGray, 1), pts[i], pts[i + 1]);

        dc.DrawRectangle(Brushes.LightSteelBlue, new Pen(Brushes.DarkSlateGray, 0.6),
            new Rect(massCenterX - MassW / 2, centerY - MassH / 2, MassW, MassH));

        double forceMag = vm.K * Math.Abs(x);
        double arrowLen = Math.Min(forceMag * ForceScale, MaxArrowLen);
        if (arrowLen < 6) arrowLen = 18;
        Point massCenter = new Point(massCenterX, centerY);
        Vector forceDir = x >= 0 ? new Vector(-1, 0) : new Vector(1, 0);
        DrawArrow(dc, massCenter, forceDir, arrowLen, Brushes.DarkBlue);

        var typeface = new Typeface("Segoe UI");
        var ft = new FormattedText("F", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, 9, Brushes.DarkBlue, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        double labelX = x >= 0 ? massCenterX - arrowLen - 14 : massCenterX + arrowLen + 2;
        dc.DrawText(ft, new Point(labelX, centerY - 10));
    }

    private static void DrawArrow(DrawingContext dc, Point start, Vector dir, double len, Brush brush)
    {
        dir.Normalize();
        Point end = new Point(start.X + dir.X * len, start.Y + dir.Y * len);
        dc.DrawLine(new Pen(brush, 1.2), start, end);
        double ax = 3.5;
        Vector perp = new Vector(-dir.Y, dir.X);
        Point[] head = { end, new Point(end.X - dir.X * ax + perp.X * 1.8, end.Y - dir.Y * ax + perp.Y * 1.8), new Point(end.X - dir.X * ax - perp.X * 1.8, end.Y - dir.Y * ax - perp.Y * 1.8) };
        dc.DrawGeometry(brush, null, new PathGeometry(new[] { new PathFigure(head[0], new PathSegment[] { new LineSegment(head[1], true), new LineSegment(head[2], true) }, true) }));
    }
}
