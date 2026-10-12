using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace WPFPhisics1._1;

/// <summary>
/// Рисует сообщающиеся сосуды: два несмешивающихся слоя (масло/вода)
/// с границей раздела и подписями высот. Стиль: тёмный фон, сетка, неоновое «стекло».
/// </summary>
public class CommunicatingVesselsCanvas : FrameworkElement
{
    private const double LayoutMargin = 28;
    /// <summary>Ширина вертикального колена в пикселях (увеличено ~в 2 раза относительно первой версии).</summary>
    private const double ColWidth = 104;

    private static readonly Brush LabelBrush = new SolidColorBrush(Color.FromRgb(0xD5, 0xE8, 0xFF));
    private static readonly Brush DimLabelBrush = new SolidColorBrush(Color.FromRgb(0x9B, 0xB4, 0xD6));

    public CommunicatingVesselsCanvas()
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
        if (e.PropertyName is nameof(CommunicatingVesselsViewModel.LevelHeight)
            or nameof(CommunicatingVesselsViewModel.Area1)
            or nameof(CommunicatingVesselsViewModel.Area2)
            or nameof(CommunicatingVesselsViewModel.Volume)
            or nameof(CommunicatingVesselsViewModel.HeightLight)
            or nameof(CommunicatingVesselsViewModel.HeightHeavy)
            or nameof(CommunicatingVesselsViewModel.RhoLight)
            or nameof(CommunicatingVesselsViewModel.RhoHeavy)
            or nameof(CommunicatingVesselsViewModel.PressureAtInterface)
            or nameof(CommunicatingVesselsViewModel.IsPlaying)
            or nameof(CommunicatingVesselsViewModel.SimulationPhase))
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
        DrawBackground(dc, w, h);
        if (vm != null)
            RenderTwoLiquids(dc, w, h, vm);
    }

    private static void DrawBackground(DrawingContext dc, double w, double h)
    {
        var bg = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops =
            {
                new GradientStop(Color.FromRgb(0x12, 0x16, 0x22), 0),
                new GradientStop(Color.FromRgb(0x0C, 0x0F, 0x18), 0.55),
                new GradientStop(Color.FromRgb(0x10, 0x14, 0x1E), 1)
            }
        };
        dc.DrawRectangle(bg, null, new Rect(0, 0, w, h));

        const double step = 24;
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(0x22, 0x5A, 0x7A, 0xA8)), 0.6);
        for (double x = 0; x <= w; x += step)
            dc.DrawLine(gridPen, new Point(x, 0), new Point(x, h));
        for (double y = 0; y <= h; y += step)
            dc.DrawLine(gridPen, new Point(0, y), new Point(w, y));
    }

    private void RenderTwoLiquids(DrawingContext dc, double w, double h, CommunicatingVesselsViewModel vm)
    {
        double fluidTop = LayoutMargin;
        double fluidBottom = h - LayoutMargin;
        double vesselHeightPx = fluidBottom - fluidTop;

        double hLight = Math.Max(0.01, vm.HeightLight);
        double hHeavy = Math.Max(0.01, vm.HeightHeavy);
        double interfaceRatio = 0.55;
        double interfaceY = fluidTop + vesselHeightPx * interfaceRatio;
        double spaceAboveInterface = interfaceY - fluidTop;
        double pixelsPerMeter = spaceAboveInterface / Math.Max(Math.Max(hLight, hHeavy) * 1.15, 0.05);

        double colWidth = ColWidth;
        double leftColLeft = w * 0.26 - colWidth / 2;
        double leftColRight = leftColLeft + colWidth;
        double rightColLeft = w * 0.74 - colWidth / 2;
        double rightColRight = rightColLeft + colWidth;
        double bottomY = fluidBottom;
        double bottomTop = fluidBottom - 26;

        double pulse = vm.IsPlaying ? 0.35 + 0.65 * (0.5 + 0.5 * Math.Sin(vm.SimulationPhase)) : 1.0;

        // Контур U-трубки (неон + «стекло»)
        var glassPen = new Pen(new SolidColorBrush(Color.FromRgb(0x6A, 0x8E, 0xB8)), 2);
        var neonPen = new Pen(new SolidColorBrush(Color.FromArgb((byte)(90 + 120 * pulse), 0x2F, 0xA2, 0xFF)), 2.5 + pulse * 1.2);

        void DrawTubeOutline(Pen pen)
        {
            dc.DrawRectangle(null, pen, new Rect(leftColLeft, fluidTop, colWidth, vesselHeightPx));
            dc.DrawRectangle(null, pen, new Rect(rightColLeft, fluidTop, colWidth, vesselHeightPx));
            dc.DrawLine(pen, new Point(leftColRight, bottomY), new Point(leftColRight, bottomTop));
            dc.DrawLine(pen, new Point(leftColRight, bottomTop), new Point(rightColLeft, bottomTop));
            dc.DrawLine(pen, new Point(rightColLeft, bottomTop), new Point(rightColLeft, bottomY));
        }

        DrawTubeOutline(glassPen);
        if (vm.IsPlaying)
            DrawTubeOutline(neonPen);

        // Жидкости с лёгким градиентом
        var waterBrush = new LinearGradientBrush(
            Color.FromRgb(0x4A, 0x8E, 0xD8), Color.FromRgb(0x28, 0x5A, 0x9E),
            new Point(0, fluidTop), new Point(0, bottomY));
        var oilBrush = new LinearGradientBrush(
            Color.FromRgb(0xF0, 0xD8, 0x50), Color.FromRgb(0xC8, 0xA8, 0x28),
            new Point(0, fluidTop), new Point(0, interfaceY));

        double leftWaterTop = interfaceY;
        double leftWaterHeight = bottomY - leftWaterTop;
        if (leftWaterHeight > 0)
            dc.DrawRectangle(waterBrush, null, new Rect(leftColLeft + 3, leftWaterTop, colWidth - 6, leftWaterHeight));

        double leftOilBottom = interfaceY;
        double leftOilTop = interfaceY - hLight * pixelsPerMeter;
        double leftOilHeight = leftOilBottom - leftOilTop;
        if (leftOilHeight > 0)
            dc.DrawRectangle(oilBrush, null, new Rect(leftColLeft + 3, leftOilTop, colWidth - 6, leftOilHeight));

        double rightWaterSurfaceY = interfaceY - hHeavy * pixelsPerMeter;
        double rightWaterHeight = bottomY - rightWaterSurfaceY;
        if (rightWaterHeight > 0)
            dc.DrawRectangle(waterBrush, null, new Rect(rightColLeft + 3, rightWaterSurfaceY, colWidth - 6, rightWaterHeight));

        double connTop = Math.Min(leftWaterTop, bottomTop);
        if (connTop < bottomY)
            dc.DrawRectangle(waterBrush, null, new Rect(leftColRight, connTop, rightColLeft - leftColRight, bottomY - connTop));

        // Линия границы раздела
        var ifacePen = new Pen(new SolidColorBrush(Color.FromRgb(0x6C, 0xD4, 0xFF)), 2);
        dc.DrawLine(ifacePen, new Point(leftColLeft, interfaceY), new Point(rightColRight, interfaceY));

        double arrowX = leftColRight + 12;
        double arrowXRight = rightColLeft - 12;

        double hOilCenterY = (leftOilTop + interfaceY) / 2;
        dc.DrawLine(new Pen(DimLabelBrush, 1.2), new Point(arrowX, leftOilTop), new Point(arrowX, interfaceY));
        DrawArrowCap(dc, arrowX, leftOilTop, true);
        DrawArrowCap(dc, arrowX, interfaceY, false);
        DrawText(dc, $"h₁ = {vm.HeightLight:F3} м", arrowX + 6, hOilCenterY - 10, 12.5);

        double rightSurfaceY = rightWaterSurfaceY;
        double hWaterCenterY = (rightSurfaceY + interfaceY) / 2;
        dc.DrawLine(new Pen(DimLabelBrush, 1.2), new Point(arrowXRight, rightSurfaceY), new Point(arrowXRight, interfaceY));
        DrawArrowCap(dc, arrowXRight, rightSurfaceY, true);
        DrawArrowCap(dc, arrowXRight, interfaceY, false);
        DrawText(dc, $"h₂ = {vm.HeightHeavy:F3} м", rightColRight + 6, hWaterCenterY - 10, 12.5);

        DrawText(dc, $"ρ = {vm.RhoLight:F0} кг/м³", leftColLeft + 6, leftOilTop + Math.Max(0, leftOilHeight / 2 - 10), 11.5);
        DrawText(dc, $"ρ = {vm.RhoHeavy:F0} кг/м³", leftColLeft + 6, leftWaterTop + Math.Max(0, leftWaterHeight / 2 - 10), 11.5);
        DrawText(dc, $"ρ = {vm.RhoHeavy:F0} кг/м³", rightColLeft + 6, rightSurfaceY + Math.Max(8, rightWaterHeight / 2 - 10), 11.5);

        double arrowCenterX = leftColLeft + colWidth / 2;
        double arrowSpacing = 8;
        var arrowPen = new Pen(new SolidColorBrush(Color.FromRgb(0xE8, 0xF4, 0xFF)), 1.4);
        for (int i = -1; i <= 1; i++)
        {
            double ax = arrowCenterX + i * arrowSpacing;
            DrawSmallArrow(dc, ax, interfaceY - 14, true, arrowPen);
            DrawSmallArrow(dc, ax, interfaceY + 14, false, arrowPen);
        }

        double labelCenterX = (leftColLeft + rightColRight) / 2;
        DrawTextCentered(dc, $"Давление на границе p₁ ({vm.PressureAtInterface:F0} Па)", labelCenterX, interfaceY - 36, 11);

        DrawText(dc, "⬡", leftColLeft + 4, fluidBottom - 22, 14, DimLabelBrush);
    }

    private static void DrawArrowCap(DrawingContext dc, double x, double y, bool up)
    {
        double dy = up ? 7 : -7;
        var pen = new Pen(DimLabelBrush, 1.1);
        dc.DrawLine(pen, new Point(x - 4, y + dy), new Point(x, y));
        dc.DrawLine(pen, new Point(x + 4, y + dy), new Point(x, y));
    }

    private static void DrawSmallArrow(DrawingContext dc, double x, double y, bool down, Pen pen)
    {
        double dy = down ? 9 : -9;
        dc.DrawLine(pen, new Point(x, y), new Point(x, y + dy));
        dc.DrawLine(pen, new Point(x - 2, y + (down ? dy - 5 : dy + 5)), new Point(x, y + dy));
        dc.DrawLine(pen, new Point(x + 2, y + (down ? dy - 5 : dy + 5)), new Point(x, y + dy));
    }

    private static void DrawText(DrawingContext dc, string text, double x, double y, double fontSize, Brush? brush = null)
    {
        var ft = new FormattedText(text, System.Globalization.CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight, new Typeface("Segoe UI"), fontSize, brush ?? LabelBrush, 1);
        dc.DrawText(ft, new Point(x, y));
    }

    private static void DrawTextCentered(DrawingContext dc, string text, double centerX, double y, double fontSize)
    {
        var ft = new FormattedText(text, System.Globalization.CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight, new Typeface("Segoe UI"), fontSize, LabelBrush, 1);
        dc.DrawText(ft, new Point(centerX - ft.Width / 2, y));
    }
}
