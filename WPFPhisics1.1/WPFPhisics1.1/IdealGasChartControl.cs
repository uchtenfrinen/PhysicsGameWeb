using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace WPFPhisics1._1;

/// <summary>График P-V (изотерма идеального газа) с сеткой и подписями осей.</summary>
public class IdealGasChartControl : FrameworkElement
{
    private const double MarginLeft = 52;
    private const double MarginRight = 24;
    private const double MarginTop = 26;
    private const double MarginBottom = 38;

    public IdealGasChartControl()
    {
        DataContextChanged += (s, e) =>
        {
            if (e.OldValue is IdealGasViewModel oldVm)
            {
                if (oldVm.IsothermPoints is INotifyCollectionChanged oldCol)
                    oldCol.CollectionChanged -= Points_CollectionChanged;
                oldVm.PropertyChanged -= Vm_PropertyChanged;
            }
            if (e.NewValue is IdealGasViewModel newVm)
            {
                if (newVm.IsothermPoints is INotifyCollectionChanged newCol)
                    newCol.CollectionChanged += Points_CollectionChanged;
                newVm.PropertyChanged += Vm_PropertyChanged;
            }
        };
    }

    private void Points_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateVisual();
    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IdealGasViewModel.CurrentV) or nameof(IdealGasViewModel.CurrentP))
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
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(24, 27, 36)), new Pen(new SolidColorBrush(Color.FromRgb(69, 75, 96)), 1), new Rect(0, 0, w, h));

        var points = vm?.IsothermPoints;
        if (points == null || points.Count < 2) return;

        double vMin = points.Min(p => p.X);
        double vMax = points.Max(p => p.X);
        double pMin = points.Min(p => p.Y);
        double pMax = points.Max(p => p.Y);
        if (vMax <= vMin) vMax = vMin + 1e-9;
        if (pMax <= pMin) pMax = pMin + 1e-9;

        double plotW = w - MarginLeft - MarginRight;
        double plotH = h - MarginTop - MarginBottom;

        double mapX(double v) => MarginLeft + (v - vMin) / (vMax - vMin) * plotW;
        double mapY(double p) => MarginTop + plotH - (p - pMin) / (pMax - pMin) * plotH;

        var typeface = new Typeface("Segoe UI");
        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var axisPen = new Pen(new SolidColorBrush(Color.FromRgb(192, 204, 238)), 1);
        var gridPenX = new Pen(new SolidColorBrush(Color.FromArgb(70, 84, 182, 255)), 0.7);
        var gridPenY = new Pen(new SolidColorBrush(Color.FromArgb(65, 176, 90, 255)), 0.7);

        // Сетка и подписи по оси V (горизонталь)
        int nTicksV = 6;
        for (int i = 0; i <= nTicksV; i++)
        {
            double v = vMin + (vMax - vMin) * i / nTicksV;
            double x = mapX(v);
            dc.DrawLine(gridPenX, new Point(x, MarginTop), new Point(x, h - MarginBottom));
            dc.DrawLine(axisPen, new Point(x, h - MarginBottom), new Point(x, h - MarginBottom + 4));
            string label = v >= 0.01 ? v.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) : v.ToString("G3", System.Globalization.CultureInfo.InvariantCulture);
            var ft = new FormattedText(label, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 9, new SolidColorBrush(Color.FromRgb(207, 218, 245)), dpi);
            dc.DrawText(ft, new Point(x - ft.Width / 2, h - MarginBottom + 5));
        }

        // Сетка и подписи по оси P (вертикаль)
        int nTicksP = 5;
        for (int i = 0; i <= nTicksP; i++)
        {
            double p = pMin + (pMax - pMin) * i / nTicksP;
            double y = mapY(p);
            dc.DrawLine(gridPenY, new Point(MarginLeft, y), new Point(w - MarginRight, y));
            dc.DrawLine(axisPen, new Point(MarginLeft - 4, y), new Point(MarginLeft, y));
            string label = p >= 1000 ? (p / 1000).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "k" : p.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
            var ft = new FormattedText(label, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 9, new SolidColorBrush(Color.FromRgb(207, 218, 245)), dpi);
            dc.DrawText(ft, new Point(MarginLeft - ft.Width - 4, y - ft.Height / 2));
        }

        // Оси
        dc.DrawLine(axisPen, new Point(MarginLeft, MarginTop), new Point(MarginLeft, h - MarginBottom));
        dc.DrawLine(axisPen, new Point(MarginLeft, h - MarginBottom), new Point(w - MarginRight, h - MarginBottom));

        // Подписи осей
        var ftV = new FormattedText("V (м³)", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, 11, new SolidColorBrush(Color.FromRgb(207, 218, 245)), dpi);
        dc.DrawText(ftV, new Point(MarginLeft + plotW / 2 - 24, h - MarginBottom + 4));
        var ftP = new FormattedText("P (Па)", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, 11, new SolidColorBrush(Color.FromRgb(207, 218, 245)), dpi);
        dc.DrawText(ftP, new Point(6, MarginTop - 14));

        // Изотерма (не замыкать контур — только кривая)
        var path = new PathFigure(new Point(mapX(points[0].X), mapY(points[0].Y)), new List<PathSegment>(), false);
        for (int i = 1; i < points.Count; i++)
            path.Segments.Add(new LineSegment(new Point(mapX(points[i].X), mapY(points[i].Y)), true));
        var geometry = new PathGeometry(new[] { path });
        dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb(85, 75, 242, 255)), 6), geometry);
        dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromRgb(25, 211, 255)), 2.4), geometry);

        // Текущая точка анимации (на изотерме)
        double cv = vm?.CurrentV ?? 0;
        double cp = vm?.CurrentP ?? 0;
        if (cv >= vMin && cv <= vMax && cp >= pMin && cp <= pMax)
        {
            double x = mapX(cv);
            double y = mapY(cp);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(120, 57, 255, 153)), null, new Point(x, y), 10, 10);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 64, 102)), new Pen(new SolidColorBrush(Color.FromRgb(255, 186, 197)), 1.2), new Point(x, y), 5.5, 5.5);
        }
    }
}
