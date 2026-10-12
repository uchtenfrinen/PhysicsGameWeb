using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace WPFPhisics1._1;

/// <summary>Неоновая схема последовательного соединения на темном фоне.</summary>
public class SeriesResistorsCanvas : FrameworkElement
{
    private const double ResW = 52;
    private const double ResH = 18;
    private const double LegHeight = 10;

    public SeriesResistorsCanvas()
    {
        DataContextChanged += (s, e) =>
        {
            if (e.OldValue is ResistorsConnectionViewModel oldVm) oldVm.PropertyChanged -= Vm_PropertyChanged;
            if (e.NewValue is ResistorsConnectionViewModel newVm) newVm.PropertyChanged += Vm_PropertyChanged;
        };
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ResistorsConnectionViewModel.VoltageV)
            or nameof(ResistorsConnectionViewModel.R1)
            or nameof(ResistorsConnectionViewModel.R2)
            or nameof(ResistorsConnectionViewModel.R3)
            or nameof(ResistorsConnectionViewModel.IsSeries))
            InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(560, 170);
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth > 0 ? ActualWidth : 560;
        double h = ActualHeight > 0 ? ActualHeight : 170;
        if (w <= 0 || h <= 0) return;

        var vm = DataContext as ResistorsConnectionViewModel;
        double glow = Math.Clamp((vm?.VoltageV ?? 0) / 20.0, 0.2, 1.0);

        double pixelsPerDip = 1.0;
        try { pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip; } catch { }
        var typeface = new Typeface("Segoe UI");
        var back = new LinearGradientBrush(Color.FromRgb(23, 27, 36), Color.FromRgb(18, 21, 30), 90);
        dc.DrawRoundedRectangle(back, new Pen(new SolidColorBrush(Color.FromRgb(63, 71, 96)), 1), new Rect(0, 0, w, h), 8, 8);

        var wireGlowPen = new Pen(new SolidColorBrush(Color.FromArgb((byte)(80 + 90 * glow), 65, 190, 255)), 6);
        var wireCorePen = new Pen(new SolidColorBrush(Color.FromRgb(113, 233, 255)), 2.2);

        double cy = h * 0.55;
        double xStart = 26;
        double xEnd = w - 26;
        dc.DrawLine(wireGlowPen, new Point(xStart, cy), new Point(xEnd, cy));
        dc.DrawLine(wireCorePen, new Point(xStart, cy), new Point(xEnd, cy));

        double[] rVals = { vm?.R1 ?? 0, vm?.R2 ?? 0, vm?.R3 ?? 0 };
        for (int i = 0; i < 3; i++)
        {
            double t = 0.23 + i * 0.25;
            double cx = xStart + (xEnd - xStart) * t;
            double leftX = cx - ResW / 2;
            double rightX = cx + ResW / 2;

            dc.DrawLine(wireCorePen, new Point(leftX, cy), new Point(leftX, cy - LegHeight));
            dc.DrawLine(wireCorePen, new Point(rightX, cy - LegHeight), new Point(rightX, cy));

            var resRect = new Rect(leftX, cy - LegHeight - ResH, ResW, ResH);
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(43, 54, 74)),
                new Pen(new SolidColorBrush(Color.FromRgb(114, 236, 255)), 1.3), resRect, 3, 3);

            var label = new FormattedText($"R{i + 1}", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, 11, new SolidColorBrush(Color.FromRgb(216, 244, 255)), pixelsPerDip);
            dc.DrawText(label, new Point(cx - label.Width / 2, resRect.Top - label.Height - 3));

            var value = new FormattedText($"{rVals[i]:F1} Ω", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, 10, new SolidColorBrush(Color.FromRgb(155, 192, 245)), pixelsPerDip);
            dc.DrawText(value, new Point(cx - value.Width / 2, cy + 7));
        }
    }
}
