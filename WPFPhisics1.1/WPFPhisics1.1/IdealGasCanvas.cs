using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace WPFPhisics1._1;

/// <summary>
/// Рисует цилиндр с поршнем: высота газа ∝ V (изотерма P = nRT/V).
/// Поршень движется при анимации по закону Бойля — Мариотта.
/// </summary>
public class IdealGasCanvas : FrameworkElement
{
    private const double LayoutMargin = 24;
    private const double CylinderWidthPx = 80;
    private const double PistonHeightPx = 14;

    public IdealGasCanvas()
    {
        DataContextChanged += (s, e) =>
        {
            if (e.OldValue is IdealGasViewModel oldVm)
                oldVm.PropertyChanged -= Vm_PropertyChanged;
            if (e.NewValue is IdealGasViewModel newVm)
                newVm.PropertyChanged += Vm_PropertyChanged;
        };
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IdealGasViewModel.CurrentV) or nameof(IdealGasViewModel.CurrentP)
            or nameof(IdealGasViewModel.Volume) or nameof(IdealGasViewModel.VMin) or nameof(IdealGasViewModel.VMax)
            or nameof(IdealGasViewModel.Temperature))
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

        var vm = DataContext as IdealGasViewModel;
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(27, 30, 40)), new Pen(new SolidColorBrush(Color.FromRgb(63, 70, 92)), 1), new Rect(0, 0, w, h));

        double vMax = vm?.VMax ?? 0.04;
        double vMin = vm?.VMin ?? 0.01;
        if (vMax <= vMin) vMax = vMin + 0.01;
        double currentV = vm?.CurrentV ?? vm?.Volume ?? 0.025;
        currentV = Math.Clamp(currentV, vMin, vMax);

        double cylinderBottom = h - LayoutMargin;
        double cylinderTop = LayoutMargin;
        double cylinderHeightPx = cylinderBottom - cylinderTop - PistonHeightPx;
        double gasHeightPx = cylinderHeightPx * (currentV - vMin) / (vMax - vMin);
        if (gasHeightPx < 0) gasHeightPx = 0;
        double pistonY = cylinderBottom - PistonHeightPx - gasHeightPx;
        double gasTop = pistonY + PistonHeightPx;
        double gasBottom = cylinderBottom;

        double cx = w / 2;
        double left = cx - CylinderWidthPx / 2;
        double right = cx + CylinderWidthPx / 2;

        // Неоновая сетка фона
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(45, 88, 163, 255)), 0.8);
        for (double gx = 0; gx < w; gx += 20)
            dc.DrawLine(gridPen, new Point(gx, 0), new Point(gx, h));
        for (double gy = 0; gy < h; gy += 20)
            dc.DrawLine(gridPen, new Point(0, gy), new Point(w, gy));

        // Цилиндр (контур + стекло)
        dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(20, 170, 190, 230)),
            new Pen(new SolidColorBrush(Color.FromRgb(154, 170, 196)), 2),
            new Rect(left, cylinderTop, CylinderWidthPx, cylinderBottom - cylinderTop));

        // Газ (под поршнем)
        double gasRectTop = gasTop;
        double gasRectHeight = gasBottom - gasRectTop;
        if (gasRectHeight > 0)
            dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(185, 87, 191, 255)), null,
                new Rect(left + 2, gasRectTop, CylinderWidthPx - 4, gasRectHeight));

        // Частицы газа
        var pBrush = new SolidColorBrush(Color.FromArgb(180, 203, 229, 255));
        int pCount = 24;
        for (int i = 0; i < pCount; i++)
        {
            double px = left + 8 + (i * 17 % (int)Math.Max(1, CylinderWidthPx - 16));
            double py = gasRectTop + 8 + (i * 29 % (int)Math.Max(1, gasRectHeight - 16));
            if (gasRectHeight > 16)
                dc.DrawEllipse(pBrush, null, new Point(px, py), 2, 2);
        }

        // Поршень
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(126, 131, 145)), new Pen(new SolidColorBrush(Color.FromRgb(59, 64, 78)), 1),
            new Rect(left, pistonY, CylinderWidthPx, PistonHeightPx));

        // Подписи P, V, T
        var typeface = new Typeface("Segoe UI");
        double lineH = 16;
        double labelX = right + 8;
        var ftP = new FormattedText($"P = {vm?.CurrentP ?? 0:F0} Па", System.Globalization.CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight, typeface, 12, new SolidColorBrush(Color.FromRgb(226, 236, 255)), 1);
        dc.DrawText(ftP, new Point(labelX, gasRectTop + (gasRectHeight - 12) / 2));
        var ftV = new FormattedText($"V = {(vm?.CurrentV ?? 0):G4} м³", System.Globalization.CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight, typeface, 11, new SolidColorBrush(Color.FromRgb(130, 202, 255)), 1);
        dc.DrawText(ftV, new Point(labelX, pistonY - 18));
        var ftT = new FormattedText($"T = {vm?.Temperature ?? 0:F0} К", System.Globalization.CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight, typeface, 11, new SolidColorBrush(Color.FromRgb(129, 255, 173)), 1);
        dc.DrawText(ftT, new Point(labelX, pistonY - 18 - lineH));
    }
}
