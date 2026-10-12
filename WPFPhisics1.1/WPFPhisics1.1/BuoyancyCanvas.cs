using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace WPFPhisics1._1;

/// <summary>
/// Рисует сосуд с жидкостью и тело (прямоугольник) с учётом объёма и доли погружения (закон Архимеда).
/// Размер тела масштабируется по объёму: линейный размер ∝ V^(1/3). Тонущее тело рисуется у дна сосуда.
/// </summary>
public class BuoyancyCanvas : FrameworkElement
{
    private const double LayoutMargin = 20;
    // Блок сделан крупнее примерно в 3 раза
    private const double BodyWidthAtV0 = 210;
    private const double BodyHeightAtV0 = 240;
    private const double ReferenceVolumeM3 = 0.001;
    private const double WaterLevelRatio = 0.5;
    private const double MinBodyPx = 72;
    private const double MaxBodyPx = 260;

    private bool _isDragging;

    public BuoyancyCanvas()
    {
        DataContextChanged += (s, e) =>
        {
            if (e.OldValue is BuoyancyViewModel oldVm)
                oldVm.PropertyChanged -= Vm_PropertyChanged;
            if (e.NewValue is BuoyancyViewModel newVm)
                newVm.PropertyChanged += Vm_PropertyChanged;
        };

        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        MouseLeave += OnMouseLeftButtonUp;
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(BuoyancyViewModel.SubmergedFraction)
            or nameof(BuoyancyViewModel.CurrentSubmergedFraction)
            or nameof(BuoyancyViewModel.ArchimedesForce)
            or nameof(BuoyancyViewModel.Weight)
            or nameof(BuoyancyViewModel.Volume)
            or nameof(BuoyancyViewModel.RhoBody)
            or nameof(BuoyancyViewModel.RhoFluid))
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

        // Фон сцены — тёмный с лёгкой засветкой по центру
        var darkBg = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x26));
        dc.DrawRectangle(darkBg, null, new Rect(0, 0, w, h));

        double vesselWidth = w - 2 * LayoutMargin;
        double vesselHeight = h - 2 * LayoutMargin;
        if (vesselWidth <= 0 || vesselHeight <= 0) return;

        double waterY = LayoutMargin + vesselHeight * WaterLevelRatio;
        double fluidTop = LayoutMargin;
        double fluidBottom = h - LayoutMargin;

        // Жидкость (градиент от тёмно-синего к светлому)
        double fluidW = Math.Max(0, vesselWidth);
        double fluidH = Math.Max(0, fluidBottom - waterY);
        var fluidRect = new Rect(LayoutMargin, waterY, fluidW, fluidH);
        var waterBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1)
        };
        waterBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x3B, 0x6D, 0xA8), 0));
        waterBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x0C, 0x2A, 0x4F), 1));
        dc.DrawRectangle(waterBrush, new Pen(new SolidColorBrush(Color.FromRgb(0x33, 0x66, 0x99)), 1.2), fluidRect);

        // Уровень воды — светлая линия
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(0x9E, 0xD4, 0xFF)), 1.5),
            new Point(LayoutMargin, waterY),
            new Point(w - LayoutMargin, waterY));

        double vol = vm?.Volume ?? ReferenceVolumeM3;
        if (vol <= 0) vol = ReferenceVolumeM3;
        // Линейный размер ∝ V^(1/3), опорный объём = 0.001 м³
        double scale = Math.Pow(vol / ReferenceVolumeM3, 1.0 / 3.0);
        double bodyW = Math.Clamp(BodyWidthAtV0 * scale, MinBodyPx, MaxBodyPx);
        double bodyH = Math.Clamp(BodyHeightAtV0 * scale, MinBodyPx, MaxBodyPx);

        double f = vm?.CurrentSubmergedFraction ?? (vm?.SubmergedFraction ?? 0.5);
        f = Math.Clamp(f, 0, 1);
        bool sinking = f >= 1.0 - 1e-6;

        double bodyLeft = (w - bodyW) / 2;
        double bodyTop;
        double bodyBottom;

        if (sinking)
        {
            bodyBottom = fluidBottom - 2;
            bodyTop = bodyBottom - bodyH;
        }
        else
        {
            bodyBottom = waterY + f * bodyH;
            bodyTop = bodyBottom - bodyH;
        }

        var bodyRect = new Rect(bodyLeft, bodyTop, bodyW, bodyH);

        // Верхняя (надводная) часть — ярко-красная, подводная — тёмно-красная с голубым отблеском
        double waterLineWithinBody = waterY - bodyTop;
        waterLineWithinBody = Math.Clamp(waterLineWithinBody, 0, bodyH);
        var topRect = new Rect(bodyLeft, bodyTop, bodyW, Math.Max(0, waterLineWithinBody));
        var bottomRect = new Rect(bodyLeft, bodyTop + waterLineWithinBody, bodyW, Math.Max(0, bodyH - waterLineWithinBody));
        var topBrush = new SolidColorBrush(Color.FromRgb(0xE5, 0x54, 0x4F));
        var bottomBrush = new SolidColorBrush(Color.FromRgb(0xB0, 0x33, 0x2E));
        dc.DrawRectangle(bottomBrush, new Pen(new SolidColorBrush(Color.FromRgb(0x40, 0x00, 0x00)), 1), bottomRect);
        dc.DrawRectangle(topBrush, new Pen(new SolidColorBrush(Color.FromRgb(0x70, 0x10, 0x10)), 1), topRect);

        // Полупрозрачное отражение под кубом (только если есть место до дна)
        double reflectionHeight = Math.Min(40, fluidBottom - bodyBottom);
        if (reflectionHeight > 1)
        {
            var reflectionRect = new Rect(bodyLeft, bodyBottom, bodyW, reflectionHeight);
            var reflectionBrush = new LinearGradientBrush(
                Color.FromArgb(80, 0xE5, 0x54, 0x4F),
                Color.FromArgb(0, 0xE5, 0x54, 0x4F),
                new Point(0, 0),
                new Point(0, 1));
            dc.DrawRectangle(reflectionBrush, null, reflectionRect);
        }

        var typeface = new Typeface("Segoe UI");
        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        if (vm != null)
        {
            // Подпись уровня погружения (проценты)
            var percent = vm.SubmergedFraction;
            var ftPerc = new FormattedText($"{percent:P0}",
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                typeface, 13, new SolidColorBrush(Color.FromRgb(0xFF, 0xEB, 0x3B)), dpi);
            dc.DrawText(ftPerc, new Point(LayoutMargin + 4, waterY - ftPerc.Height - 2));

            // Стрелки сил: зелёная вверх F_A, красная вниз P
            Point center = new Point(bodyLeft + bodyW / 2, bodyTop + bodyH / 2);
            double lenFa = Math.Min(vm.ArchimedesForce * 0.05, 80);
            double lenP = Math.Min(vm.Weight * 0.05, 80);
            if (lenFa < 20) lenFa = 40;
            if (lenP < 20) lenP = 40;

            DrawArrow(dc, center, new Vector(0, -1), lenFa, new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)));
            DrawArrow(dc, center, new Vector(0, 1), lenP, new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36)));

            var ftFa = new FormattedText("Fₐ = ρж g Vпогр",
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                typeface, 11, new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)), dpi);
            dc.DrawText(ftFa, new Point(center.X - ftFa.Width / 2, center.Y - lenFa - ftFa.Height - 2));

            var ftP = new FormattedText("F = mg",
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                typeface, 11, new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36)), dpi);
            dc.DrawText(ftP, new Point(center.X - ftP.Width / 2, center.Y + lenP + 2));

            // Формула для ρ₀ внизу
            double rho0 = vm.RhoBody;
            var ftRho = new FormattedText($"ρ₀ = {rho0:F0} кг/м³",
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                typeface, 12, new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)), dpi);
            dc.DrawText(ftRho, new Point(w / 2 - ftRho.Width / 2, fluidBottom + 4));
        }
    }

    private void OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        CaptureMouse();
        _isDragging = true;
        UpdateSubmergedFromPoint(e.GetPosition(this));
    }

    private void OnMouseLeftButtonUp(object? sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            ReleaseMouseCapture();
        }
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isDragging) return;
        UpdateSubmergedFromPoint(e.GetPosition(this));
    }

    private void UpdateSubmergedFromPoint(Point p)
    {
        if (DataContext is not BuoyancyViewModel vm) return;

        double w = ActualWidth;
        double h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        double vesselWidth = w - 2 * LayoutMargin;
        double vesselHeight = h - 2 * LayoutMargin;
        if (vesselWidth <= 0 || vesselHeight <= 0) return;

        double waterY = LayoutMargin + vesselHeight * WaterLevelRatio;
        double fluidBottom = h - LayoutMargin;

        double vol = vm.Volume <= 0 ? ReferenceVolumeM3 : vm.Volume;
        double scale = Math.Pow(vol / ReferenceVolumeM3, 1.0 / 3.0);
        double bodyH = Math.Clamp(BodyHeightAtV0 * scale, MinBodyPx, MaxBodyPx);

        // Проецируем позицию точки в диапазон от поверхности до дна
        double bodyBottom = Math.Clamp(p.Y + bodyH / 2, waterY, fluidBottom - 2);
        double f = (bodyBottom - waterY) / bodyH;
        f = Math.Clamp(f, 0, 1);

        // Передаём новую долю погружения во ViewModel.
        // Плотности при этом не меняются — законы Архимеда сохраняются,
        // а таймер затем сам вернёт куб в положение равновесия, когда F_A и P сравняются.
        vm.SetCurrentSubmergedFromCanvas(f);
    }

    private static void DrawArrow(DrawingContext dc, Point start, Vector dir, double len, Brush brush)
    {
        dir.Normalize();
        Point end = new Point(start.X + dir.X * len, start.Y + dir.Y * len);
        dc.DrawLine(new Pen(brush, 1.5), start, end);
        double ax = 6;
        Vector perp = new Vector(-dir.Y, dir.X);
        Point[] head =
        {
            end,
            new Point(end.X - dir.X * ax + perp.X * 2.5, end.Y - dir.Y * ax + perp.Y * 2.5),
            new Point(end.X - dir.X * ax - perp.X * 2.5, end.Y - dir.Y * ax - perp.Y * 2.5)
        };
        dc.DrawGeometry(brush, null,
            new PathGeometry(new[] { new PathFigure(head[0], new PathSegment[] { new LineSegment(head[1], true), new LineSegment(head[2], true) }, true) }));
    }
}
