using System.Windows;
using System.Windows.Media;

namespace WPFPhisics1._1;

/// <summary>Схема сил для плавания тела: тело, P вниз, F_A вверх.</summary>
public class BuoyancyForcesSchemeControl : FrameworkElement
{
    private const double LayoutMargin = 16;
    private const double ForceScalePxPerN = 1.2;
    private const double MaxArrowLen = 60;

    public BuoyancyForcesSchemeControl()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        DataContextChanged += (s, e) =>
        {
            if (e.OldValue is BuoyancyViewModel oldVm)
                oldVm.PropertyChanged -= Vm_PropertyChanged;
            if (e.NewValue is BuoyancyViewModel newVm)
                newVm.PropertyChanged += Vm_PropertyChanged;
        };
        SizeChanged += (s, e) => { if (ActualWidth > 0 && ActualHeight > 0) InvalidateVisual(); };
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => InvalidateVisual();

    private BuoyancyViewModel? Vm => DataContext as BuoyancyViewModel;

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

        double cx = w / 2;
        double cy = h / 2;
        double bodyW = 40;
        double bodyH = 32;
        var bodyRect = new Rect(cx - bodyW / 2, cy - bodyH / 2, bodyW, bodyH);

        // Фон
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x30)), null, new Rect(0, 0, w, h));

        // Уровень воды (визуальная подсказка среды)
        double waterY = cy + bodyH;
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(0x9E, 0xD4, 0xFF)), 1),
            new Point(LayoutMargin, waterY), new Point(w - LayoutMargin, waterY));

        // Куб
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x40, 0x83, 0xC1)), new Pen(Brushes.SlateGray, 0.8), bodyRect);

        Point center = new Point(cx, cy);
        double lenP = Math.Min(vm.Weight * ForceScalePxPerN, MaxArrowLen);
        double lenFa = Math.Min(vm.ArchimedesForce * ForceScalePxPerN, MaxArrowLen);
        if (lenP < 18) lenP = 28;
        if (lenFa < 18) lenFa = 28;

        DrawArrow(dc, center, new Vector(0, 1), lenP, new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36)));
        DrawArrow(dc, center, new Vector(0, -1), lenFa, new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)));

        var typeface = new Typeface("Segoe UI");
        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var ftP = new FormattedText("P = ρт g V", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            typeface, 10, new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36)), dpi);
        var ftFa = new FormattedText("Fₐ = ρж g Vпогр", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            typeface, 10, new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)), dpi);
        dc.DrawText(ftP, new Point(cx + bodyW / 2 + 8, center.Y + lenP - ftP.Height / 2));
        dc.DrawText(ftFa, new Point(cx + bodyW / 2 + 8, center.Y - lenFa - ftFa.Height));
    }

    private static void DrawArrow(DrawingContext dc, Point start, Vector dir, double len, Brush brush)
    {
        dir.Normalize();
        Point end = new Point(start.X + dir.X * len, start.Y + dir.Y * len);
        dc.DrawLine(new Pen(brush, 1.2), start, end);
        double ax = 4;
        Vector perp = new Vector(-dir.Y, dir.X);
        Point[] head = { end, new Point(end.X - dir.X * ax + perp.X * 2, end.Y - dir.Y * ax + perp.Y * 2), new Point(end.X - dir.X * ax - perp.X * 2, end.Y - dir.Y * ax - perp.Y * 2) };
        dc.DrawGeometry(brush, null, new PathGeometry(new[] { new PathFigure(head[0], new PathSegment[] { new LineSegment(head[1], true), new LineSegment(head[2], true) }, true) }));
    }
}
