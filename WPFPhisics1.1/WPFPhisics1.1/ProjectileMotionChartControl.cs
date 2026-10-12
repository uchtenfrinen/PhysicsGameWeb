using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using WPFPhisics1._1.Physics;

namespace WPFPhisics1._1;

/// <summary>График зависимости координаты x от времени: x(t) = v₀·cos α·t.</summary>
public class ProjectileMotionChartControl : FrameworkElement
{
    private const double MarginLeft = 44;
    private const double MarginRight = 16;
    private const double MarginTop = 16;
    private const double MarginBottom = 32;

    public ProjectileMotionChartControl()
    {
        DataContextChanged += (s, e) =>
        {
            if (e.OldValue is ProjectileMotionViewModel oldVm)
                oldVm.PropertyChanged -= Vm_PropertyChanged;
            if (e.NewValue is ProjectileMotionViewModel newVm)
                newVm.PropertyChanged += Vm_PropertyChanged;
        };
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ProjectileMotionViewModel.V0) or nameof(ProjectileMotionViewModel.AngleDegrees)
            or nameof(ProjectileMotionViewModel.Y0) or nameof(ProjectileMotionViewModel.TimeOfFlight)
            or nameof(ProjectileMotionViewModel.Range) or nameof(ProjectileMotionViewModel.CurrentTime)
            or nameof(ProjectileMotionViewModel.CurrentX))
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

        var vm = DataContext as ProjectileMotionViewModel;
        double v0 = vm?.V0 ?? 15;
        double angleDeg = vm?.AngleDegrees ?? 45;
        double y0 = vm?.Y0 ?? 0;
        double T = vm?.TimeOfFlight ?? 1;
        double range = vm?.Range ?? 10;
        double currentT = vm?.CurrentTime ?? 0;
        double currentX = vm?.CurrentX ?? 0;

        var darkBg = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x30));
        var borderPen = new Pen(new SolidColorBrush(Color.FromRgb(0x3F, 0x3F, 0x46)), 1);
        var textBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
        var gridPen = new Pen(new SolidColorBrush(Color.FromRgb(0x3F, 0x3F, 0x46)), 0.5);
        var axisPen = new Pen(textBrush, 1);

        dc.DrawRectangle(darkBg, borderPen, new Rect(0, 0, w, h));

        if (v0 <= 0 || T <= 0) return;

        double tMax = T;
        double xMax = range;
        if (xMax <= 0) xMax = 1;

        double plotW = w - MarginLeft - MarginRight;
        double plotH = h - MarginTop - MarginBottom;

        double mapX(double t) => MarginLeft + (t / tMax) * plotW;
        double mapY(double x) => h - MarginBottom - (x / xMax) * plotH;

        var typeface = new Typeface("Segoe UI");
        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        int nTicksT = 5;
        for (int i = 0; i <= nTicksT; i++)
        {
            double tv = tMax * i / nTicksT;
            double x = mapX(tv);
            dc.DrawLine(gridPen, new Point(x, MarginTop), new Point(x, h - MarginBottom));
            dc.DrawLine(axisPen, new Point(x, h - MarginBottom), new Point(x, h - MarginBottom + 4));
            var ft = new FormattedText(tv.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 9, textBrush, dpi);
            dc.DrawText(ft, new Point(x - ft.Width / 2, h - MarginBottom + 5));
        }

        int nTicksX = 4;
        for (int i = 0; i <= nTicksX; i++)
        {
            double xv = xMax * i / nTicksX;
            double y = mapY(xv);
            dc.DrawLine(gridPen, new Point(MarginLeft, y), new Point(w - MarginRight, y));
            dc.DrawLine(axisPen, new Point(MarginLeft - 4, y), new Point(MarginLeft, y));
            var ft = new FormattedText(xv.ToString("0", System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 9, textBrush, dpi);
            dc.DrawText(ft, new Point(MarginLeft - ft.Width - 4, y - ft.Height / 2));
        }

        dc.DrawLine(axisPen, new Point(MarginLeft, MarginTop), new Point(MarginLeft, h - MarginBottom));
        dc.DrawLine(axisPen, new Point(MarginLeft, h - MarginBottom), new Point(w - MarginRight, h - MarginBottom));

        dc.DrawText(new FormattedText("t (с)", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, 10, textBrush, dpi), new Point(MarginLeft + plotW / 2 - 18, h - MarginBottom + 4));
        dc.DrawText(new FormattedText("x (м)", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, 10, textBrush, dpi), new Point(4, MarginTop + plotH / 2 - 20));

        var lineBrush = new SolidColorBrush(Color.FromRgb(0xE5, 0x94, 0x00));
        var p0 = new Point(mapX(0), mapY(0));
        var p1 = new Point(mapX(tMax), mapY(xMax));
        dc.DrawLine(new Pen(lineBrush, 2), p0, p1);

        if (currentT >= 0 && currentT <= tMax && currentX >= 0 && currentX <= xMax * 1.02)
        {
            double px = mapX(currentT);
            double py = mapY(currentX);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(0xFF, 0xCC, 0x00)), null, new Point(px, py), 5, 5);
        }
    }
}
