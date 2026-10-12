using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace WPFPhisics1._1;

/// <summary>Схема свободного падения: вертикальная ось высоты, тело на текущей высоте, земля.</summary>
public class FreeFallCanvas : FrameworkElement
{
    private const double LayoutMargin = 24;
    private static readonly Color NeonCyan = Color.FromRgb(0x5C, 0xE1, 0xFF);
    private static readonly Color NeonPurple = Color.FromRgb(0x9B, 0x6B, 0xFF);
    private static readonly Color GridMuted = Color.FromArgb(0x55, 0x6B, 0x4F, 0x9F);

    private FreeFallViewModel? _vm;

    public FreeFallCanvas()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        DataContextChanged += OnDataContextChanged;
        SizeChanged += (_, _) => { if (ActualWidth > 0 && ActualHeight > 0) InvalidateVisual(); };
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm != null)
            _vm.PropertyChanged -= Vm_PropertyChanged;
        _vm = e.NewValue as FreeFallViewModel;
        if (_vm != null)
            _vm.PropertyChanged += Vm_PropertyChanged;
        InvalidateVisual();
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(FreeFallViewModel.Height0) or nameof(FreeFallViewModel.CurrentHeight)
            or nameof(FreeFallViewModel.CurrentTime) or nameof(FreeFallViewModel.IsPlaying))
            InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(Math.Max(0, availableSize.Width), Math.Max(0, availableSize.Height));
    }

    private static void DrawNeonBall(DrawingContext dc, Point c, double r, Color core, double dpi)
    {
        for (int i = 3; i >= 0; i--)
        {
            double rr = r + i * 4;
            byte a = (byte)(40 - i * 10);
            var brush = new RadialGradientBrush
            {
                GradientStops =
                {
                    new(Color.FromArgb(a, core.R, core.G, core.B), 0),
                    new(Color.FromArgb(0, core.R, core.G, core.B), 1)
                },
                Center = new Point(0.35, 0.35),
                RadiusX = 0.9,
                RadiusY = 0.9
            };
            dc.DrawEllipse(brush, null, c, rr, rr);
        }

        var coreBrush = new RadialGradientBrush
        {
            GradientStops =
            {
                new(Color.FromRgb(0xE0, 0xFB, 0xFF), 0),
                new(core, 1)
            },
            Center = new Point(0.35, 0.3),
            RadiusX = 1,
            RadiusY = 1
        };
        dc.DrawEllipse(coreBrush, new Pen(new SolidColorBrush(Color.FromRgb(0x88, 0xEE, 0xFF)), 1.2), c, r, r);
    }

    private static void DrawTechBracket(DrawingContext dc, bool left, Rect area, Pen pen)
    {
        double s = 18;
        double x = left ? area.Left + 6 : area.Right - 6 - s;
        double y = area.Bottom - 6 - s;
        var fig = new PathFigure(left ? new Point(x, y + s) : new Point(x + s, y + s), [], false);
        if (left)
        {
            fig.Segments.Add(new LineSegment(new Point(x, y), true));
            fig.Segments.Add(new LineSegment(new Point(x + s * 0.65, y), true));
        }
        else
        {
            fig.Segments.Add(new LineSegment(new Point(x + s, y), true));
            fig.Segments.Add(new LineSegment(new Point(x + s * 0.35, y), true));
        }
        dc.DrawGeometry(null, pen, new PathGeometry(new[] { fig }));
    }

    protected override void OnRender(DrawingContext dc)
    {
        var vm = DataContext as FreeFallViewModel;
        double w = ActualWidth;
        double h = ActualHeight;
        if (w <= 0 || h <= 0 || vm == null) return;

        double h0 = vm.Height0;
        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var darkBg = new LinearGradientBrush(
            Color.FromRgb(0x18, 0x1A, 0x24),
            Color.FromRgb(0x14, 0x16, 0x20),
            new Point(0, 0),
            new Point(1, 1));
        var borderPen = new Pen(new LinearGradientBrush(
            NeonPurple, NeonCyan, new Point(0, 0), new Point(1, 1)), 1.2);

        if (h0 <= 0)
        {
            dc.DrawRectangle(darkBg, borderPen, new Rect(0, 0, w, h));
            return;
        }

        dc.DrawRectangle(darkBg, borderPen, new Rect(0, 0, w, h));
        var plotRect = new Rect(LayoutMargin * 0.4, LayoutMargin * 0.5, w - LayoutMargin * 0.8, h - LayoutMargin * 0.9);

        const double scaleLeft = 40;
        var gridPenLo = new Pen(new SolidColorBrush(GridMuted), 0.6);
        double gridStep = Math.Max(28, Math.Min(40, plotRect.Width / 14));
        for (double gx = scaleLeft; gx <= w - LayoutMargin; gx += gridStep)
            dc.DrawLine(gridPenLo, new Point(gx, LayoutMargin), new Point(gx, h - LayoutMargin));
        for (double gy = LayoutMargin; gy <= h - LayoutMargin; gy += gridStep)
            dc.DrawLine(gridPenLo, new Point(scaleLeft, gy), new Point(w - LayoutMargin, gy));

        var axisNeon = new Pen(new SolidColorBrush(NeonCyan), 1);
        var textBrush = new SolidColorBrush(Color.FromRgb(0xC7, 0xDA, 0xF4));
        var groundPen = new Pen(new SolidColorBrush(NeonCyan), 2);

        double cx = (scaleLeft + w - LayoutMargin) / 2;
        double plotH = h - 2 * LayoutMargin;
        double maxHeight = Math.Max(h0, 1);
        double scaleY = plotH / maxHeight;
        double yGround = h - LayoutMargin;
        double yTop = yGround - h0 * scaleY;
        double currentH = vm.CurrentHeight;
        double yBall = yGround - currentH * scaleY;

        var typeface = new Typeface("Segoe UI");
        dc.DrawLine(axisNeon, new Point(scaleLeft, yTop), new Point(scaleLeft, yGround));
        for (int i = 0; i <= 4; i++)
        {
            double val = h0 * i / 4;
            double y = yGround - val * scaleY;
            dc.DrawLine(gridPenLo, new Point(scaleLeft, y), new Point(scaleLeft + 7, y));
            var ftScale = new FormattedText($"{val:F1} м", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 9, textBrush, dpi);
            dc.DrawText(ftScale, new Point(4, y - ftScale.Height / 2));
        }

        var dashPen = new Pen(new SolidColorBrush(NeonCyan), 1.3);
        dashPen.DashStyle = new DashStyle(new[] { 7.0, 5.0 }, 0);
        dc.DrawLine(dashPen, new Point(cx, yTop), new Point(cx, yGround));
        dc.DrawLine(groundPen, new Point(scaleLeft, yGround), new Point(w - LayoutMargin, yGround));

        double ballR = 15;
        DrawNeonBall(dc, new Point(cx, yBall), ballR, NeonCyan, dpi);
        DrawNeonBall(dc, new Point(cx, yGround), ballR * 0.92, Color.FromRgb(0x44, 0xAA, 0xCC), dpi);

        var ftH0 = new FormattedText($"h₀ = {h0:F1} м", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 11, textBrush, dpi);
        dc.DrawText(ftH0, new Point(cx + ballR + 10, yTop - 8));

        var ftCurrent = new FormattedText($"h = {currentH:F1} м", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, textBrush, dpi);
        dc.DrawText(ftCurrent, new Point(cx + ballR + 10, yBall - 6));

        var ft0 = new FormattedText("h = 0", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 10, textBrush, dpi);
        dc.DrawText(ft0, new Point(cx + ballR + 10, yGround - ballR - 4));

        double arrowY = yBall * 0.55 + yGround * 0.45;
        double arrowLen = 34;
        var fBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x99, 0xAA));
        var fPen = new Pen(fBrush, 2);
        double ax = cx + ballR + 6;
        dc.DrawLine(fPen, new Point(ax, arrowY - arrowLen / 2), new Point(ax, arrowY + arrowLen / 2));
        Point arrowTip = new Point(ax, arrowY + arrowLen / 2);
        dc.DrawGeometry(fBrush, null, new PathGeometry(new[]
        {
            new PathFigure(arrowTip, new[]
            {
                new LineSegment(new Point(arrowTip.X - 5, arrowTip.Y - 10), true),
                new LineSegment(new Point(arrowTip.X + 5, arrowTip.Y - 10), true)
            }, true)
        }));
        var ftF = new FormattedText("F_g", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 11, fBrush, dpi);
        dc.DrawText(ftF, new Point(ax + 8, arrowY - ftF.Height / 2));

        var decoPen = new Pen(new SolidColorBrush(Color.FromArgb(0x55, 0xCE, 0xD4, 0xFF)), 1);
        DrawTechBracket(dc, true, plotRect, decoPen);
        DrawTechBracket(dc, false, plotRect, decoPen);
    }
}
