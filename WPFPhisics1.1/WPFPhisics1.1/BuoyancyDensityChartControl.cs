using System.Windows;
using System.Windows.Media;

namespace WPFPhisics1._1;

/// <summary>Простой график плотностей: ρт и ρж как горизонтальные линии во времени.</summary>
public class BuoyancyDensityChartControl : FrameworkElement
{
    private const double MarginLeft = 44;
    private const double MarginRight = 14;
    private const double MarginTop = 14;
    private const double MarginBottom = 28;
    private const double MaxRho = 2500;

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
        double rhoBody = vm?.RhoBody ?? 600;
        double rhoFluid = vm?.RhoFluid ?? 1000;

        double plotW = w - MarginLeft - MarginRight;
        double plotH = h - MarginTop - MarginBottom;
        if (plotW <= 0 || plotH <= 0) return;

        var bg = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x30));
        dc.DrawRectangle(bg, null, new Rect(0, 0, w, h));

        var typeface = new Typeface("Segoe UI");
        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(0x40, 0xCC, 0xCC, 0xCC)), 0.5) { DashStyle = DashStyles.Dash };
        var axisPen = new Pen(new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)), 1);

        // Сетка по ρ
        for (int i = 0; i <= 5; i++)
        {
            double rho = MaxRho * i / 5.0;
            double y = h - MarginBottom - (rho / MaxRho) * plotH;
            dc.DrawLine(gridPen, new Point(MarginLeft, y), new Point(w - MarginRight, y));
            var ft = new FormattedText(rho.ToString("0", System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 9,
                new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)), dpi);
            dc.DrawText(ft, new Point(MarginLeft - ft.Width - 4, y - ft.Height / 2));
        }

        // Оси
        dc.DrawLine(axisPen, new Point(MarginLeft, MarginTop), new Point(MarginLeft, h - MarginBottom));
        dc.DrawLine(axisPen, new Point(MarginLeft, h - MarginBottom), new Point(w - MarginRight, h - MarginBottom));

        var ftX = new FormattedText("t, c", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            typeface, 10, new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)), dpi);
        dc.DrawText(ftX, new Point(MarginLeft + plotW / 2 - 10, h - MarginBottom + 3));

        var ftY = new FormattedText("ρ, кг/м³", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            typeface, 10, new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)), dpi);
        dc.DrawText(ftY, new Point(4, MarginTop + plotH / 2 - 10));

        // Линии плотностей
        double yBody = h - MarginBottom - Math.Clamp(rhoBody, 0, MaxRho) / MaxRho * plotH;
        double yFluid = h - MarginBottom - Math.Clamp(rhoFluid, 0, MaxRho) / MaxRho * plotH;

        var bodyPen = new Pen(new SolidColorBrush(Color.FromRgb(0xFF, 0xB3, 0x4C)), 2);
        var fluidPen = new Pen(new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)), 2);

        dc.DrawLine(bodyPen, new Point(MarginLeft, yBody), new Point(w - MarginRight, yBody));
        dc.DrawLine(fluidPen, new Point(MarginLeft, yFluid), new Point(w - MarginRight, yFluid));
    }
}

