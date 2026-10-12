using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace WPFPhisics1._1;

/// <summary>График «Доля погружения» от ρт при фиксированной ρж.</summary>
public class BuoyancyChartControl : FrameworkElement
{
    private const double MarginLeft = 44;
    private const double MarginRight = 14;
    private const double MarginTop = 14;
    private const double MarginBottom = 32;
    private const double MaxRhoBody = 2500;

    public BuoyancyChartControl()
    {
        DataContextChanged += (s, e) =>
        {
            if (e.OldValue is BuoyancyViewModel oldVm)
                oldVm.PropertyChanged -= Vm_PropertyChanged;
            if (e.NewValue is BuoyancyViewModel newVm)
                newVm.PropertyChanged += Vm_PropertyChanged;
        };
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(BuoyancyViewModel.RhoBody) or nameof(BuoyancyViewModel.RhoFluid))
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

        var vm = DataContext as BuoyancyViewModel;
        double rhoFluid = vm?.RhoFluid ?? 1000;
        if (rhoFluid <= 0) rhoFluid = 1000;

        double plotW = w - MarginLeft - MarginRight;
        double plotH = h - MarginTop - MarginBottom;
        if (plotW <= 0 || plotH <= 0) return;

        var typeface = new Typeface("Segoe UI");
        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var axisPen = new Pen(new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)), 1);
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(0x40, 0xCC, 0xCC, 0xCC)), 0.5)
        {
            DashStyle = DashStyles.Dash
        };

        // Тёмный фон
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x30)), null, new Rect(0, 0, w, h));

        for (int i = 0; i <= 5; i++)
        {
            double rho = MaxRhoBody * i / 5;
            double x = MarginLeft + (rho / MaxRhoBody) * plotW;
            dc.DrawLine(gridPen, new Point(x, MarginTop), new Point(x, h - MarginBottom));
            dc.DrawLine(axisPen, new Point(x, h - MarginBottom), new Point(x, h - MarginBottom + 4));
            var ft = new FormattedText(rho.ToString("0", System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 9, new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)), dpi);
            dc.DrawText(ft, new Point(x - ft.Width / 2, h - MarginBottom + 5));
        }
        for (int i = 0; i <= 5; i++)
        {
            double val = i / 5.0;
            double y = h - MarginBottom - val * plotH;
            dc.DrawLine(gridPen, new Point(MarginLeft, y), new Point(w - MarginRight, y));
            dc.DrawLine(axisPen, new Point(MarginLeft - 4, y), new Point(MarginLeft, y));
            var ft = new FormattedText(val.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 9, new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)), dpi);
            dc.DrawText(ft, new Point(MarginLeft - ft.Width - 4, y - ft.Height / 2));
        }

        dc.DrawLine(axisPen, new Point(MarginLeft, MarginTop), new Point(MarginLeft, h - MarginBottom));
        dc.DrawLine(axisPen, new Point(MarginLeft, h - MarginBottom), new Point(w - MarginRight, h - MarginBottom));

        var ftX = new FormattedText("ρт, кг/м³", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)), dpi);
        dc.DrawText(ftX, new Point(MarginLeft + plotW / 2 - 28, h - MarginBottom + 5));

        var pts = new List<Point>();
        int n = 80;
        for (int i = 0; i <= n; i++)
        {
            double rho = MaxRhoBody * i / (double)n;
            double frac = rhoFluid > 0 ? Math.Min(1.0, rho / rhoFluid) : 1;
            double px = MarginLeft + (rho / MaxRhoBody) * plotW;
            double py = h - MarginBottom - frac * plotH;
            pts.Add(new Point(px, py));
        }
        if (pts.Count >= 2)
        {
            var segs = pts.Skip(1).Select(p => new LineSegment(p, true)).ToArray();
            dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3)), 1.5), new PathGeometry(new[] { new PathFigure(pts[0], segs, false) }));
        }

        // Жёлтая точка для текущей плотности тела
        if (vm != null)
        {
            double rhoBody = Math.Max(0, Math.Min(MaxRhoBody, vm.RhoBody));
            double frac = rhoFluid > 0 ? Math.Min(1.0, rhoBody / rhoFluid) : 1;
            double px = MarginLeft + (rhoBody / MaxRhoBody) * plotW;
            double py = h - MarginBottom - frac * plotH;
            var yellow = new SolidColorBrush(Color.FromRgb(0xFF, 0xEB, 0x3B));
            dc.DrawEllipse(yellow, new Pen(new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)), 0.7), new Point(px, py), 4, 4);
        }
    }
}
