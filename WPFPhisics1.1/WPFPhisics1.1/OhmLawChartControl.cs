using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace WPFPhisics1._1;

/// <summary>График U(I): характеристика цепи — прямая U = ε − I·r, рабочая точка (I, U_R).</summary>
public class OhmLawChartControl : FrameworkElement
{
    private const double MarginLeft = 48;
    private const double MarginRight = 20;
    private const double MarginTop = 20;
    private const double MarginBottom = 36;

    public OhmLawChartControl()
    {
        DataContextChanged += (s, e) =>
        {
            if (e.OldValue is OhmLawViewModel oldVm)
                oldVm.PropertyChanged -= Vm_PropertyChanged;
            if (e.NewValue is OhmLawViewModel newVm)
                newVm.PropertyChanged += Vm_PropertyChanged;
        };
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(OhmLawViewModel.EpsilonV) or nameof(OhmLawViewModel.InternalOhm)
            or nameof(OhmLawViewModel.ExternalOhm) or nameof(OhmLawViewModel.CurrentA)
            or nameof(OhmLawViewModel.VoltageExternal))
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

        var vm = DataContext as OhmLawViewModel;
        double eps = vm?.EpsilonV ?? 12;
        double r = vm?.InternalOhm ?? 0.5;
        double iMax = r > 0 ? eps / r : 1;
        if (iMax <= 0) iMax = 1;
        double iCur = vm?.CurrentA ?? 0;
        double uCur = vm?.VoltageExternal ?? 0;

        var plotBg = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops =
            {
                new GradientStop(Color.FromRgb(0x10, 0x14, 0x1E), 0),
                new GradientStop(Color.FromRgb(0x0C, 0x10, 0x18), 1)
            }
        };
        dc.DrawRectangle(plotBg, new Pen(new SolidColorBrush(Color.FromRgb(0x2A, 0x3A, 0x52)), 1), new Rect(0, 0, w, h));

        double plotW = w - MarginLeft - MarginRight;
        double plotH = h - MarginTop - MarginBottom;
        double iMaxPlot = Math.Max(iMax * 1.05, 0.1);
        double mapX(double i) => MarginLeft + (i / iMaxPlot) * plotW;
        double mapY(double u) => MarginTop + plotH - (u / eps) * plotH;

        var typeface = new Typeface("Segoe UI");
        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var axisPen = new Pen(new SolidColorBrush(Color.FromRgb(0x8A, 0x9E, 0xB8)), 1);
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(0x35, 0x5A, 0x78, 0xA0)), 0.5);
        var labelBrush = new SolidColorBrush(Color.FromRgb(0xB8, 0xCC, 0xE8));

        // Сетка и подписи по I
        int nTicksI = 5;
        for (int i = 0; i <= nTicksI; i++)
        {
            double iv = iMaxPlot * i / nTicksI;
            double x = mapX(iv);
            dc.DrawLine(gridPen, new Point(x, MarginTop), new Point(x, h - MarginBottom));
            dc.DrawLine(axisPen, new Point(x, h - MarginBottom), new Point(x, h - MarginBottom + 4));
            var ft = new FormattedText(iv.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 9, labelBrush, dpi);
            dc.DrawText(ft, new Point(x - ft.Width / 2, h - MarginBottom + 5));
        }
        // Сетка и подписи по U
        int nTicksU = 4;
        for (int i = 0; i <= nTicksU; i++)
        {
            double uv = eps * i / nTicksU;
            double y = mapY(uv);
            dc.DrawLine(gridPen, new Point(MarginLeft, y), new Point(w - MarginRight, y));
            dc.DrawLine(axisPen, new Point(MarginLeft - 4, y), new Point(MarginLeft, y));
            var ft = new FormattedText(uv.ToString("0", System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 9, labelBrush, dpi);
            dc.DrawText(ft, new Point(MarginLeft - ft.Width - 4, y - ft.Height / 2));
        }

        dc.DrawLine(axisPen, new Point(MarginLeft, MarginTop), new Point(MarginLeft, h - MarginBottom));
        dc.DrawLine(axisPen, new Point(MarginLeft, h - MarginBottom), new Point(w - MarginRight, h - MarginBottom));

        var ftI = new FormattedText("I (А)", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, 10, labelBrush, dpi);
        dc.DrawText(ftI, new Point(MarginLeft + plotW / 2 - 18, h - MarginBottom + 4));
        var ftU = new FormattedText("U_R (В)", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, 10, labelBrush, dpi);
        dc.DrawText(ftU, new Point(4, MarginTop - 12));

        // Прямая U = ε − I·r (от (0, ε) до (iMax, 0))
        double x0 = mapX(0);
        double y0 = mapY(eps);
        double x1 = mapX(iMaxPlot);
        double y1 = mapY(0);
        var lineNeon = new SolidColorBrush(Color.FromRgb(0x3A, 0xC8, 0xFF));
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(90, 0x3A, 0xC8, 0xFF)), 5), new Point(x0, y0), new Point(x1, y1));
        dc.DrawLine(new Pen(lineNeon, 2), new Point(x0, y0), new Point(x1, y1));

        // Формула на графике
        var ftEq = new FormattedText("U = ε − I·r", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, 10, labelBrush, dpi);
        dc.DrawText(ftEq, new Point(w - MarginRight - ftEq.Width - 4, MarginTop + 2));

        // Рабочая точка
        if (iCur >= 0 && iCur <= iMaxPlot && uCur >= 0 && uCur <= eps * 1.02)
        {
            double px = mapX(iCur);
            double py = mapY(uCur);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(0x56, 0xD9, 0x81)), new Pen(new SolidColorBrush(Color.FromRgb(0x2A, 0xA8, 0x58)), 1.2), new Point(px, py), 5, 5);
            // Пунктирные линии к осям
            var dashPen = new Pen(new SolidColorBrush(Color.FromArgb(0xAA, 0x7A, 0x90, 0xA8)), 1) { DashStyle = new DashStyle(new[] { 4.0, 3.0 }, 0) };
            dc.DrawLine(dashPen, new Point(px, py), new Point(px, h - MarginBottom));
            dc.DrawLine(dashPen, new Point(px, py), new Point(MarginLeft, py));
        }
    }
}
