using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace WPFPhisics1._1;

/// <summary>График h_масла от ρ_масла при фиксированных ρ_воды и h_воды (ρ₁·h₁ = ρ₂·h₂).</summary>
public class CommunicatingVesselsChartControl : FrameworkElement
{
    private const double MarginLeft = 40;
    private const double MarginRight = 12;
    private const double MarginTop = 12;
    private const double MarginBottom = 28;
    private const double RhoMin = 200;
    private const double RhoMax = 1200;

    public CommunicatingVesselsChartControl()
    {
        DataContextChanged += (s, e) =>
        {
            if (e.OldValue is CommunicatingVesselsViewModel oldVm)
                oldVm.PropertyChanged -= Vm_PropertyChanged;
            if (e.NewValue is CommunicatingVesselsViewModel newVm)
                newVm.PropertyChanged += Vm_PropertyChanged;
        };
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CommunicatingVesselsViewModel.RhoLight)
            or nameof(CommunicatingVesselsViewModel.RhoHeavy)
            or nameof(CommunicatingVesselsViewModel.HeightHeavy)
            or nameof(CommunicatingVesselsViewModel.HeightLight))
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

        var vm = DataContext as CommunicatingVesselsViewModel;
        double rhoWater = vm?.RhoHeavy ?? 1000;
        double hWater = Math.Max(0.02, vm?.HeightHeavy ?? 0.1);
        if (rhoWater <= 0) rhoWater = 1000;

        double plotW = w - MarginLeft - MarginRight;
        double plotH = h - MarginTop - MarginBottom;
        if (plotW <= 0 || plotH <= 0) return;

        var typeface = new Typeface("Segoe UI");
        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var axisPen = new Pen(Brushes.Black, 1);
        var gridPen = new Pen(Brushes.LightGray, 0.5);

        dc.DrawRectangle(Brushes.White, new Pen(Brushes.LightGray, 1), new Rect(0, 0, w, h));

        for (int i = 0; i <= 5; i++)
        {
            double rho = RhoMin + (RhoMax - RhoMin) * i / 5;
            double x = MarginLeft + (rho - RhoMin) / (RhoMax - RhoMin) * plotW;
            dc.DrawLine(gridPen, new Point(x, MarginTop), new Point(x, h - MarginBottom));
            dc.DrawLine(axisPen, new Point(x, h - MarginBottom), new Point(x, h - MarginBottom + 4));
            var ft = new FormattedText(rho.ToString("0", System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 8, Brushes.Black, dpi);
            dc.DrawText(ft, new Point(x - ft.Width / 2, h - MarginBottom + 5));
        }

        double hMax = rhoWater > 0 ? hWater * (rhoWater / RhoMin) : 0.5;
        double hMin = rhoWater > 0 ? hWater * (rhoWater / RhoMax) : 0.05;
        for (int i = 0; i <= 4; i++)
        {
            double val = hMin + (hMax - hMin) * i / 4;
            double y = h - MarginBottom - (val - hMin) / (hMax - hMin) * plotH;
            dc.DrawLine(gridPen, new Point(MarginLeft, y), new Point(w - MarginRight, y));
            dc.DrawLine(axisPen, new Point(MarginLeft - 4, y), new Point(MarginLeft, y));
            var ft = new FormattedText(val.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 8, Brushes.Black, dpi);
            dc.DrawText(ft, new Point(MarginLeft - ft.Width - 4, y - ft.Height / 2));
        }

        dc.DrawLine(axisPen, new Point(MarginLeft, MarginTop), new Point(MarginLeft, h - MarginBottom));
        dc.DrawLine(axisPen, new Point(MarginLeft, h - MarginBottom), new Point(w - MarginRight, h - MarginBottom));

        var ftX = new FormattedText("ρ (кг/м³)", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 9, Brushes.Black, dpi);
        dc.DrawText(ftX, new Point(MarginLeft + plotW / 2 - 28, h - MarginBottom + 4));
        var ftY = new FormattedText("h_масла (м)", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 9, Brushes.Black, dpi);
        dc.DrawText(ftY, new Point(2, MarginTop + plotH / 2 - 24));

        var pts = new List<Point>();
        int n = 60;
        for (int i = 0; i <= n; i++)
        {
            double rho = RhoMin + (RhoMax - RhoMin) * i / (double)n;
            double hOil = rhoWater > 0 && rho > 0 ? hWater * (rhoWater / rho) : 0;
            double px = MarginLeft + (rho - RhoMin) / (RhoMax - RhoMin) * plotW;
            double py = h - MarginBottom - (hOil - hMin) / (hMax - hMin) * plotH;
            pts.Add(new Point(px, py));
        }
        if (pts.Count >= 2)
        {
            var segs = pts.Skip(1).Select(p => new LineSegment(p, true)).ToArray();
            dc.DrawGeometry(null, new Pen(Brushes.Black, 1.5), new PathGeometry(new[] { new PathFigure(pts[0], segs, false) }));
        }
    }
}
