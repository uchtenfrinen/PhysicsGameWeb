using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WPFPhisics1._1;

/// <summary>
/// Упрощённая схема последовательной цепи: прямоугольный контур, источник ε,r наверху,
/// резистор R внизу, направление тока на правой стороне. Неоновые провода и «bloom»,
/// анимация зарядов по контуру (скорость ~ I), как раньше.
/// </summary>
public class OhmLawCircuitCanvas : FrameworkElement
{
    private const double Pad = 28;
    private const int ChargeCount = 28;

    private static readonly Color NeonCyan = Color.FromRgb(0x2F, 0xE8, 0xFF);
    private static readonly Color NeonCyanDim = Color.FromRgb(0x18, 0x7A, 0xB8);
    private static readonly SolidColorBrush LabelWhiteBrush = new(Color.FromRgb(0xF0, 0xF4, 0xFF));
    private static readonly SolidColorBrush LabelSerifBrush = new(Color.FromRgb(0xEE, 0xF2, 0xFA));

    private double _phase;
    private DateTime _lastFrameUtc = DateTime.UtcNow;

    private Rect _batteryHitRect;
    private Rect _resistorHitRect;

    private ToolTip? _hoverTip;

    public OhmLawCircuitCanvas()
    {
        Loaded += (_, _) =>
        {
            _lastFrameUtc = DateTime.UtcNow;
            CompositionTarget.Rendering += OnRendering;
        };
        Unloaded += (_, _) => CompositionTarget.Rendering -= OnRendering;
        MouseMove += OnMouseMove;

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
            or nameof(OhmLawViewModel.VoltageExternal) or nameof(OhmLawViewModel.VoltageInternal) or nameof(OhmLawViewModel.PowerW))
            InvalidateVisual();
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;
        double dt = Math.Clamp((now - _lastFrameUtc).TotalSeconds, 0, 0.05);
        _lastFrameUtc = now;

        var vm = DataContext as OhmLawViewModel;
        double i = vm?.CurrentA ?? 0;
        double speed = 0.10 + Math.Min(2.4, i * 0.45);
        _phase = (_phase + dt * speed) % 1.0;
        InvalidateVisual();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var vm = DataContext as OhmLawViewModel;
        if (vm == null) return;

        var p = e.GetPosition(this);
        string? tipText = null;

        if (_resistorHitRect.Contains(p))
            tipText = $"R = {vm.ExternalOhm:F2} Ом\nU_R = {vm.VoltageExternal:F2} В";
        else if (_batteryHitRect.Contains(p))
            tipText = $"ε = {vm.EpsilonV:F2} В,  r = {vm.InternalOhm:F2} Ом\nU_r = {vm.VoltageInternal:F2} В";

        if (tipText == null)
        {
            ToolTip = null;
            return;
        }

        _hoverTip ??= new ToolTip { Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse };
        _hoverTip.Content = tipText;
        ToolTip = _hoverTip;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double w = double.IsPositiveInfinity(availableSize.Width) ? 520 : Math.Max(0, availableSize.Width);
        double h = double.IsPositiveInfinity(availableSize.Height) ? 0.72 * w : Math.Max(0, availableSize.Height);
        return new Size(w, h);
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth;
        double h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        var vm = DataContext as OhmLawViewModel;

        DrawBackground(dc, w, h);
        DrawGrid(dc, w, h);

        double dpi = 1.0;
        try { dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip; } catch { }

        // Прямоугольник контура
        double xL = Pad + 56;
        double xR = w - Pad - 56;
        double yT = Pad + 52;
        double yB = h - Pad - 56;
        double cx = (xL + xR) / 2;

        // Зазоры под условные обозначения (как на учебном рисунке)
        double batGap = 56;
        double batL = cx - batGap / 2;
        double batR = cx + batGap / 2;

        double resW = 64;
        double resL = cx - resW / 2;
        double resR = cx + resW / 2;

        _batteryHitRect = new Rect(batL - 8, yT - 42, batGap + 16, 52);
        _resistorHitRect = new Rect(resL - 4, yB - 22, resW + 8, 36);

        // Провода: по часовой — верх (с разрывом под ε), право, низ (с R), лево
        var loopForWires = new List<Point>
        {
            new(xL, yT), new(batL, yT), new(batR, yT), new(xR, yT),
            new(xR, yB), new(resR, yB), new(resL, yB), new(xL, yB), new(xL, yT)
        };

        foreach (var seg in EnumerateSegments(loopForWires))
            DrawNeonWire(dc, seg.a, seg.b);

        // Источник: две вертикали (длинная/короткая) + подпись ε, r
        DrawDcSourceSymbol(dc, batL, batR, yT);
        var serifTf = new Typeface(new FontFamily("Cambria"), FontStyles.Italic, FontWeights.Normal, FontStretches.Normal);
        DrawMathLabel(dc, serifTf, dpi, "ε,  r", cx, yT - 36, 15, LabelSerifBrush);

        // Резистор: прямоугольник + R
        DrawResistorBox(dc, new Rect(resL, yB - 14, resW, 28));
        DrawMathLabel(dc, new Typeface("Segoe UI"), dpi, "R", cx, yB - 36, 16, LabelWhiteBrush);

        // Стрелка тока вниз на правой стороне + I = …
        if (vm != null)
            DrawRightCurrentArrow(dc, xR, yT, yB, vm.CurrentA, dpi);

        // Частицы по тому же контуру (по часовой)
        DrawCharges(dc, loopForWires, vm?.CurrentA ?? 0);
    }

    private static IEnumerable<(Point a, Point b)> EnumerateSegments(List<Point> pts)
    {
        for (int i = 0; i < pts.Count - 1; i++)
            yield return (pts[i], pts[i + 1]);
    }

    private static void DrawBackground(DrawingContext dc, double w, double h)
    {
        var bg = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops =
            {
                new GradientStop(Color.FromRgb(0x0A, 0x0D, 0x16), 0),
                new GradientStop(Color.FromRgb(0x06, 0x0A, 0x12), 0.5),
                new GradientStop(Color.FromRgb(0x09, 0x0E, 0x18), 1)
            }
        };
        dc.DrawRectangle(bg, null, new Rect(0, 0, w, h));
    }

    private static void DrawGrid(DrawingContext dc, double w, double h)
    {
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(0x1E, 0x40, 0x60, 0x88)), 0.5);
        for (double x = 0; x <= w; x += 22)
            dc.DrawLine(pen, new Point(x, 0), new Point(x, h));
        for (double y = 0; y <= h; y += 22)
            dc.DrawLine(pen, new Point(0, y), new Point(w, y));
    }

    /// <summary>Неон + bloom: широкие полупрозрачные слои, затем яркое ядро.</summary>
    private static void DrawNeonWire(DrawingContext dc, Point a, Point b)
    {
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(35, NeonCyanDim.R, NeonCyanDim.G, NeonCyanDim.B)), 16), a, b);
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(70, NeonCyan.R, NeonCyan.G, NeonCyan.B)), 9), a, b);
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(140, 160, 230, 255)), 4.2), a, b);
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(255, 220, 250, 255)), 1.8), a, b);
    }

    /// <summary>Классический знак источника: длинная и короткая вертикали.</summary>
    private static void DrawDcSourceSymbol(DrawingContext dc, double xL, double xR, double yWire)
    {
        double xm = (xL + xR) / 2;
        double hLong = 22;
        double hShort = 14;
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(250, 240, 250, 255)), 2.2);
        // Длинная (отрицательный полюс слева)
        dc.DrawLine(pen, new Point(xm - 8, yWire - hLong / 2), new Point(xm - 8, yWire + hLong / 2));
        // Короткая (положительный справа)
        dc.DrawLine(pen, new Point(xm + 8, yWire - hShort / 2), new Point(xm + 8, yWire + hShort / 2));
        // Лёгкое свечение
        var glow = new Pen(new SolidColorBrush(Color.FromArgb(90, NeonCyan.R, NeonCyan.G, NeonCyan.B)), 6);
        dc.DrawLine(glow, new Point(xm - 8, yWire - hLong / 2), new Point(xm - 8, yWire + hLong / 2));
        dc.DrawLine(glow, new Point(xm + 8, yWire - hShort / 2), new Point(xm + 8, yWire + hShort / 2));
    }

    private static void DrawResistorBox(DrawingContext dc, Rect r)
    {
        var fill = new LinearGradientBrush(
            Color.FromRgb(0x2A, 0x32, 0x44), Color.FromRgb(0x1A, 0x22, 0x34),
            new Point(0, r.Top), new Point(0, r.Bottom));
        dc.DrawRoundedRectangle(fill, new Pen(new SolidColorBrush(Color.FromArgb(200, 120, 200, 255)), 1.5), r, 3, 3);
        var glow = new SolidColorBrush(Color.FromArgb(50, NeonCyan.R, NeonCyan.G, NeonCyan.B));
        dc.DrawRoundedRectangle(glow, null, new Rect(r.Left - 2, r.Top - 2, r.Width + 4, r.Height + 4), 5, 5);
        dc.DrawRoundedRectangle(fill, new Pen(new SolidColorBrush(Color.FromArgb(200, 140, 210, 255)), 1.5), r, 3, 3);

        // Зигзаг внутри (условное обозначение)
        var zig = new StreamGeometry();
        double zTop = r.Top + 5, zBot = r.Bottom - 5, zL = r.Left + 6, zR = r.Right - 6;
        int n = 5;
        using (var g = zig.Open())
        {
            g.BeginFigure(new Point(zL, (zTop + zBot) / 2), false, false);
            for (int i = 0; i <= n; i++)
            {
                double t = i / (double)n;
                double x = zL + t * (zR - zL);
                double yy = (i % 2 == 0) ? zTop : zBot;
                g.LineTo(new Point(x, yy), true, false);
            }
        }
        zig.Freeze();
        dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb(220, 200, 220, 255)), 1.2), zig);
    }

    private static void DrawMathLabel(DrawingContext dc, Typeface tf, double dpi, string text, double cx, double y, double size, Brush brush)
    {
        var ft = new FormattedText(text, System.Globalization.CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight, tf, size, brush, dpi);
        dc.DrawText(ft, new Point(cx - ft.Width / 2, y));
    }

    private static void DrawRightCurrentArrow(DrawingContext dc, double xR, double yT, double yB, double currentA, double dpi)
    {
        // Левее провода (xR), компактнее в 2 раза — не сливается с неоном
        double xShaft = xR - 34;
        double yMid = (yT + yB) / 2;
        double halfLen = ((yB - yT) - 96) / 4;
        double y1 = yMid - halfLen;
        double y2 = yMid + halfLen;

        var green = new SolidColorBrush(Color.FromRgb(0x3E, 0xF5, 0x8E));
        var pen = new Pen(green, 1.5);
        dc.DrawLine(pen, new Point(xShaft, y1), new Point(xShaft, y2));
        dc.DrawLine(pen, new Point(xShaft, y2), new Point(xShaft - 3.5, y2 - 6));
        dc.DrawLine(pen, new Point(xShaft, y2), new Point(xShaft + 3.5, y2 - 6));
        var glow = new Pen(new SolidColorBrush(Color.FromArgb(55, 80, 255, 140)), 5);
        dc.DrawLine(glow, new Point(xShaft, y1), new Point(xShaft, y2));

        string s = $"I = {currentA:F3} А";
        var ft = new FormattedText(s, System.Globalization.CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight, new Typeface("Segoe UI"), 7.5, LabelWhiteBrush, dpi);
        double textX = xShaft + 8;
        double textY = (y1 + y2) / 2 - ft.Height / 2;
        dc.DrawText(ft, new Point(textX, textY));
    }

    private void DrawCharges(DrawingContext dc, List<Point> polyline, double current)
    {
        if (polyline.Count < 2) return;
        double total = 0;
        var segLen = new List<double>();
        for (int i = 0; i < polyline.Count - 1; i++)
        {
            double len = (polyline[i + 1] - polyline[i]).Length;
            segLen.Add(len);
            total += len;
        }
        if (total < 1) return;

        double brightness = Math.Clamp(0.35 + current * 0.22, 0.35, 1);
        for (int i = 0; i < ChargeCount; i++)
        {
            double t = ((_phase + i / (double)ChargeCount) % 1.0) * total;
            int seg = 0;
            while (seg < segLen.Count - 1 && t > segLen[seg])
            {
                t -= segLen[seg];
                seg++;
            }
            Point p0 = polyline[seg];
            Point p1 = polyline[seg + 1];
            double k = segLen[seg] <= 0 ? 0 : t / segLen[seg];
            Point p = new(p0.X + (p1.X - p0.X) * k, p0.Y + (p1.Y - p0.Y) * k);

            byte a = (byte)(100 + 155 * brightness);
            var c = Color.FromArgb(a, 100, 230, 255);
            dc.DrawEllipse(new SolidColorBrush(c), null, p, 3.6, 3.6);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(60, 200, 255, 255)), null, p, 6, 6);
        }
    }
}
