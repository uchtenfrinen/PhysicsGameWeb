using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace WPFPhisics1._1;

/// <summary>Неоновая схема параллельного соединения на темном фоне.</summary>
public class ParallelResistorsCanvas : FrameworkElement
{
    private const double ResW = 18;
    private const double ResH = 52;
    private const double RailY = 28;
    private const double BranchSpacing = 110;

    public ParallelResistorsCanvas()
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
        return new Size(560, 260);
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth > 0 ? ActualWidth : 560;
        double h = ActualHeight > 0 ? ActualHeight : 260;
        if (w <= 0 || h <= 0) return;

        var vm = DataContext as ResistorsConnectionViewModel;
        double glow = Math.Clamp((vm?.VoltageV ?? 0) / 20.0, 0.2, 1.0);

        double pixelsPerDip = 1.0;
        try { pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip; } catch { }
        var typeface = new Typeface("Segoe UI");

        var back = new LinearGradientBrush(Color.FromRgb(23, 27, 36), Color.FromRgb(18, 21, 30), 90);
        dc.DrawRoundedRectangle(back, new Pen(new SolidColorBrush(Color.FromRgb(63, 71, 96)), 1), new Rect(0, 0, w, h), 8, 8);

        var railGlow = new Pen(new SolidColorBrush(Color.FromArgb((byte)(80 + glow * 95), 65, 190, 255)), 7);
        var railCore = new Pen(new SolidColorBrush(Color.FromRgb(113, 233, 255)), 2.2);
        var branchPen = new Pen(new SolidColorBrush(Color.FromRgb(106, 255, 220)), 2);
        var dashPen = new Pen(new SolidColorBrush(Color.FromArgb(120, 142, 156, 186)), 1) { DashStyle = DashStyles.Dash };

        double left = 28;
        double railBottomY = RailY + 132;
        double centerY = (RailY + railBottomY) / 2;

        // Источник
        double circleX = left + 16;
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(48, 58, 78)), new Pen(new SolidColorBrush(Color.FromRgb(103, 195, 255)), 1.2), new Point(circleX, RailY), 16, 16);
        var plusU = new FormattedText("+U", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, 11, new SolidColorBrush(Color.FromRgb(227, 240, 255)), pixelsPerDip);
        dc.DrawText(plusU, new Point(circleX - plusU.Width / 2, RailY - plusU.Height / 2 - 1));

        // Верхняя/нижняя шины
        double railStartX = circleX + 16;
        double railEndX = w - 26;
        dc.DrawLine(railGlow, new Point(railStartX, RailY), new Point(railEndX, RailY));
        dc.DrawLine(railCore, new Point(railStartX, RailY), new Point(railEndX, RailY));
        dc.DrawLine(railGlow, new Point(railStartX, railBottomY), new Point(railEndX, railBottomY));
        dc.DrawLine(railCore, new Point(railStartX, railBottomY), new Point(railEndX, railBottomY));

        // Вертикальные ветви (R1, R2, R3 + пунктир между R2 и R3)
        double r1x = railStartX + BranchSpacing;
        double r2x = railStartX + BranchSpacing * 2;
        double rnX = railEndX - BranchSpacing * 0.8;
        double dashX = (r2x + rnX) / 2;
        double[] rVals = { vm?.R1 ?? 0, vm?.R2 ?? 0, vm?.R3 ?? 0 };

        DrawBranch(dc, branchPen, typeface, pixelsPerDip, r1x, centerY, railBottomY, "R1", rVals[0]);
        DrawBranch(dc, branchPen, typeface, pixelsPerDip, r2x, centerY, railBottomY, "R2", rVals[1]);

        dc.DrawLine(dashPen, new Point(dashX, RailY), new Point(dashX, railBottomY));

        DrawBranch(dc, branchPen, typeface, pixelsPerDip, rnX, centerY, railBottomY, "R3", rVals[2]);

        // Токи в ветвях
        var aBrush = new SolidColorBrush(Color.FromRgb(255, 115, 115));
        DrawArrowDown(dc, aBrush, r1x - 22, centerY - 8, 6);
        DrawArrowDown(dc, aBrush, r2x - 22, centerY - 8, 6);
        DrawArrowDown(dc, aBrush, rnX + 22, centerY - 8, 6);

        // Общий ток на шине
        DrawArrowRight(dc, aBrush, railStartX + 16, RailY - 16, 7);
        var iLabel = new FormattedText("I", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, 11, new SolidColorBrush(Color.FromRgb(255, 180, 180)), pixelsPerDip);
        dc.DrawText(iLabel, new Point(railStartX + 26, RailY - 28));
    }

    private static void DrawBranch(DrawingContext dc, Pen branchPen, Typeface typeface, double dpi, double x, double centerY, double railBottomY, string label, double resistance)
    {
        dc.DrawLine(branchPen, new Point(x, RailY), new Point(x, centerY - ResH / 2));
        dc.DrawLine(branchPen, new Point(x, centerY + ResH / 2), new Point(x, railBottomY));
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(44, 58, 82)),
            new Pen(new SolidColorBrush(Color.FromRgb(110, 252, 227)), 1.2),
            new Rect(x - ResW / 2, centerY - ResH / 2, ResW, ResH), 3, 3);

        var rLabel = new FormattedText(label, System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, 11, new SolidColorBrush(Color.FromRgb(216, 244, 255)), dpi);
        dc.DrawText(rLabel, new Point(x - rLabel.Width / 2, centerY - ResH / 2 - rLabel.Height - 3));
        var rVal = new FormattedText($"{resistance:F1} Ω", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, 10, new SolidColorBrush(Color.FromRgb(155, 192, 245)), dpi);
        dc.DrawText(rVal, new Point(x - rVal.Width / 2, centerY + ResH / 2 + 4));
    }

    private static void DrawArrowRight(DrawingContext dc, SolidColorBrush brush, double x, double y, double size)
    {
        var fig = new PathFigure(new Point(x + size, y), new List<PathSegment>
        {
            new LineSegment(new Point(x - size * 0.5, y - size), true),
            new LineSegment(new Point(x - size * 0.5, y + size), true)
        }, true);
        dc.DrawGeometry(brush, new Pen(brush, 1.5), new PathGeometry(new[] { fig }));
    }

    private static void DrawArrowDown(DrawingContext dc, SolidColorBrush brush, double x, double y, double size)
    {
        var fig = new PathFigure(new Point(x, y + size), new List<PathSegment>
        {
            new LineSegment(new Point(x - size, y - size * 0.5), true),
            new LineSegment(new Point(x + size, y - size * 0.5), true)
        }, true);
        dc.DrawGeometry(brush, new Pen(brush, 1.5), new PathGeometry(new[] { fig }));
    }
}
