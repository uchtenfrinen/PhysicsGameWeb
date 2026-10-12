using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace WPFPhisics1._1;

/// <summary>Неоновая визуализация тонкой собирающей линзы с анимацией луча.</summary>
public class ThinLensCanvas : FrameworkElement
{
    public ThinLensCanvas()
    {
        DataContextChanged += (s, e) =>
        {
            if (e.OldValue is ThinLensViewModel oldVm)
                oldVm.PropertyChanged -= Vm_PropertyChanged;
            if (e.NewValue is ThinLensViewModel newVm)
                newVm.PropertyChanged += Vm_PropertyChanged;
        };
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ThinLensViewModel.D) or nameof(ThinLensViewModel.F)
            or nameof(ThinLensViewModel.ObjectHeight) or nameof(ThinLensViewModel.DPrime)
            or nameof(ThinLensViewModel.Magnification) or nameof(ThinLensViewModel.ImageHeight)
            or nameof(ThinLensViewModel.IsRealImage) or nameof(ThinLensViewModel.AnimationProgress))
            InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double w = double.IsPositiveInfinity(availableSize.Width) ? 400 : Math.Max(0, availableSize.Width);
        double h = double.IsPositiveInfinity(availableSize.Height) ? 220 : Math.Max(0, availableSize.Height);
        return new Size(w, h);
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth;
        double h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        var vm = DataContext as ThinLensViewModel;
        if (vm == null)
        {
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(21, 25, 36)), new Pen(new SolidColorBrush(Color.FromRgb(55, 63, 85)), 1), new Rect(0, 0, w, h));
            return;
        }

        double d = vm.D;
        double f = vm.F;
        double dP = vm.DPrime;
        double objH = vm.ObjectHeight;
        double imgH = vm.ImageHeight;
        bool realImage = vm.IsRealImage;

        if (double.IsNaN(dP) || double.IsInfinity(dP))
        {
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(21, 25, 36)), new Pen(new SolidColorBrush(Color.FromRgb(55, 63, 85)), 1), new Rect(0, 0, w, h));
            var ft = new FormattedText("d = f: изображение в бесконечности",
                System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 12, new SolidColorBrush(Color.FromRgb(167, 182, 209)), VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(ft, new Point(w / 2 - 120, h / 2 - 10));
            return;
        }

        double margin = 24;
        double plotW = w - 2 * margin;
        double plotH = h - 2 * margin;
        double centerX = margin + plotW / 2;
        double centerY = margin + plotH / 2;

        // Мир: x от -d до max(d', f) + запас справа (чтобы изображение не у края)
        double xLeft = -d - f * 0.5;
        double xRight = (dP > 0 ? dP : f) + f * 1.2;
        double xSpan = xRight - xLeft;
        double yExtent = Math.Max(objH, imgH) + 2;
        double ySpan = 2 * yExtent;

        double scale = Math.Min(plotW / xSpan, plotH / ySpan);
        double ToScreenX(double xw) => centerX + xw * scale;
        double ToScreenY(double yw) => centerY - yw * scale;

        var bg = new LinearGradientBrush(Color.FromRgb(24, 28, 40), Color.FromRgb(18, 21, 31), 90);
        dc.DrawRoundedRectangle(bg, new Pen(new SolidColorBrush(Color.FromRgb(66, 78, 106)), 1), new Rect(0, 0, w, h), 8, 8);
        var gridBlue = new Pen(new SolidColorBrush(Color.FromArgb(75, 90, 175, 255)), 1);
        var gridPurple = new Pen(new SolidColorBrush(Color.FromArgb(85, 185, 100, 255)), 1);
        for (int i = 0; i <= 16; i++)
        {
            double gx = margin + plotW * i / 16.0;
            dc.DrawLine(i % 2 == 0 ? gridBlue : gridPurple, new Point(gx, margin), new Point(gx, margin + plotH));
        }
        for (int i = 0; i <= 8; i++)
        {
            double gy = margin + plotH * i / 8.0;
            dc.DrawLine(i % 2 == 0 ? gridBlue : gridPurple, new Point(margin, gy), new Point(margin + plotW, gy));
        }

        var axisPen = new Pen(new SolidColorBrush(Color.FromRgb(224, 235, 255)), 1.2);
        var rayGlowPen = new Pen(new SolidColorBrush(Color.FromArgb(95, 255, 170, 85)), 7);
        var rayPen = new Pen(new SolidColorBrush(Color.FromRgb(255, 176, 102)), 2.0);
        var objectPen = new Pen(new SolidColorBrush(Color.FromRgb(90, 255, 130)), 2.4);
        var imagePen = new Pen(new SolidColorBrush(Color.FromRgb(255, 92, 108)), 2.4);
        var focusBrush = new SolidColorBrush(Color.FromRgb(114, 236, 255));

        // Оптическая ось
        double axLeft = ToScreenX(xLeft);
        double axRight = ToScreenX(xRight);
        dc.DrawLine(axisPen, new Point(axLeft, centerY), new Point(axRight, centerY));

        // Линза: две дуги (собирающая, тонкая в плоскости O)
        double lensX = ToScreenX(0);
        double lensHalfH = plotH / 2 - 4;
        double lensCurve = 18;
        var leftArc = new System.Windows.Media.PathGeometry();
        var leftFigure = new System.Windows.Media.PathFigure(new Point(lensX, centerY - lensHalfH), new List<System.Windows.Media.PathSegment>(), false);
        leftFigure.Segments.Add(new System.Windows.Media.ArcSegment(new Point(lensX, centerY + lensHalfH), new Size(lensCurve, lensHalfH), 0, false, System.Windows.Media.SweepDirection.Clockwise, true));
        leftArc.Figures.Add(leftFigure);
        var rightArc = new System.Windows.Media.PathGeometry();
        var rightFigure = new System.Windows.Media.PathFigure(new Point(lensX, centerY + lensHalfH), new List<System.Windows.Media.PathSegment>(), false);
        rightFigure.Segments.Add(new System.Windows.Media.ArcSegment(new Point(lensX, centerY - lensHalfH), new Size(lensCurve, lensHalfH), 0, false, System.Windows.Media.SweepDirection.Clockwise, true));
        rightArc.Figures.Add(rightFigure);
        dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb(180, 180, 220, 255)), 2), leftArc);
        dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb(180, 180, 220, 255)), 2), rightArc);

        // Фокусы F (слева) и F' (справа) — оранжевые круги
        double fxLeft = ToScreenX(-f);
        double fxRight = ToScreenX(f);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(110, 85, 220, 255)), null, new Point(fxLeft, centerY), 10, 10);
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(110, 85, 220, 255)), null, new Point(fxRight, centerY), 10, 10);
        dc.DrawEllipse(focusBrush, new Pen(new SolidColorBrush(Color.FromRgb(200, 246, 255)), 1), new Point(fxLeft, centerY), 3.8, 3.8);
        dc.DrawEllipse(focusBrush, new Pen(new SolidColorBrush(Color.FromRgb(200, 246, 255)), 1), new Point(fxRight, centerY), 3.8, 3.8);

        // Предмет: зелёная стрелка вверх
        double objX = ToScreenX(-d);
        double objY0 = ToScreenY(0);
        double objY1 = ToScreenY(objH);
        dc.DrawLine(objectPen, new Point(objX, objY0), new Point(objX, objY1));
        dc.DrawLine(objectPen, new Point(objX, objY1), new Point(objX - 6, objY1 - 8));
        dc.DrawLine(objectPen, new Point(objX, objY1), new Point(objX + 6, objY1 - 8));

        // Изображение: красная стрелка (вниз при действительном, вверх при мнимом)
        double imgX = ToScreenX(dP);
        double imgY0 = ToScreenY(0);
        double imgY1 = ToScreenY(realImage ? -imgH : imgH);
        dc.DrawLine(imagePen, new Point(imgX, imgY0), new Point(imgX, imgY1));
        double arrowSign = realImage ? 1 : -1;
        dc.DrawLine(imagePen, new Point(imgX, imgY1), new Point(imgX - 6, imgY1 + arrowSign * 8));
        dc.DrawLine(imagePen, new Point(imgX, imgY1), new Point(imgX + 6, imgY1 + arrowSign * 8));

        // Три луча от вершины предмета (-d, objH)
        double ox = -d;
        double oy = objH;
        double sx0 = ToScreenX(ox);
        double sy0 = ToScreenY(oy);

        // Луч 1: параллельно оси до линзы, затем через F'
        dc.DrawLine(rayGlowPen, new Point(ToScreenX(ox), sy0), new Point(lensX, sy0));
        dc.DrawLine(rayPen, new Point(ToScreenX(ox), sy0), new Point(lensX, sy0));
        double ray1EndY = oy * (1 - dP / f);
        dc.DrawLine(rayGlowPen, new Point(lensX, sy0), new Point(ToScreenX(dP), ToScreenY(ray1EndY)));
        dc.DrawLine(rayPen, new Point(lensX, sy0), new Point(ToScreenX(dP), ToScreenY(ray1EndY)));

        // Луч 2: через оптический центр O
        double imageTipY = -oy * dP / d;
        dc.DrawLine(rayGlowPen, new Point(sx0, sy0), new Point(lensX, centerY));
        dc.DrawLine(rayPen, new Point(sx0, sy0), new Point(lensX, centerY));
        dc.DrawLine(rayGlowPen, new Point(lensX, centerY), new Point(ToScreenX(dP), ToScreenY(imageTipY)));
        dc.DrawLine(rayPen, new Point(lensX, centerY), new Point(ToScreenX(dP), ToScreenY(imageTipY)));

        // Луч 3: через F, после линзы параллельно оси
        double yAtLens3 = -objH * f / (d - f);
        dc.DrawLine(rayGlowPen, new Point(sx0, sy0), new Point(ToScreenX(-f), centerY));
        dc.DrawLine(rayPen, new Point(sx0, sy0), new Point(ToScreenX(-f), centerY));
        dc.DrawLine(rayGlowPen, new Point(ToScreenX(-f), centerY), new Point(lensX, ToScreenY(yAtLens3)));
        dc.DrawLine(rayPen, new Point(ToScreenX(-f), centerY), new Point(lensX, ToScreenY(yAtLens3)));
        dc.DrawLine(rayGlowPen, new Point(lensX, ToScreenY(yAtLens3)), new Point(ToScreenX(dP), ToScreenY(yAtLens3)));
        dc.DrawLine(rayPen, new Point(lensX, ToScreenY(yAtLens3)), new Point(ToScreenX(dP), ToScreenY(yAtLens3)));

        // Анимированный маркер по центральному лучу
        if (vm.IsPlaying)
        {
            Point p0 = new Point(sx0, sy0);
            Point p1 = new Point(lensX, centerY);
            Point p2 = new Point(ToScreenX(dP), ToScreenY(imageTipY));
            double t = vm.AnimationProgress;
            Point marker = t < 0.5 ? Lerp(p0, p1, t / 0.5) : Lerp(p1, p2, (t - 0.5) / 0.5);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(120, 50, 227, 255)), null, marker, 12, 12);
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(102, 245, 255)), new Pen(new SolidColorBrush(Color.FromRgb(226, 250, 255)), 1), marker, 4.5, 4.5);
        }

        // Подписи
        double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var typeface = new Typeface("Segoe UI");
        var textBrush = new SolidColorBrush(Color.FromRgb(227, 240, 255));
        dc.DrawText(new FormattedText("F'", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 11, textBrush, pixelsPerDip), new Point(fxLeft - 14, centerY - 16));
        dc.DrawText(new FormattedText("F", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 11, textBrush, pixelsPerDip), new Point(fxRight + 4, centerY - 16));
        dc.DrawText(new FormattedText("O", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 11, textBrush, pixelsPerDip), new Point(lensX - 12, centerY + 6));
        dc.DrawText(new FormattedText("Линза", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 11, new SolidColorBrush(Color.FromRgb(132, 222, 255)), pixelsPerDip), new Point(lensX + 2, margin + 2));
        dc.DrawText(new FormattedText("Предмет", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 11, new SolidColorBrush(Color.FromRgb(119, 255, 145)), pixelsPerDip), new Point(objX - 18, objY1 - 16));
        dc.DrawText(new FormattedText("Изображение", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 11, new SolidColorBrush(Color.FromRgb(255, 118, 124)), pixelsPerDip), new Point(imgX - 30, Math.Min(objY1, ToScreenY(realImage ? -imgH : imgH)) - 14));

        // Декор
        var decoPen = new Pen(new SolidColorBrush(Color.FromArgb(130, 188, 121, 255)), 1.2);
        dc.DrawLine(decoPen, new Point(8, 8), new Point(24, 8));
        dc.DrawLine(decoPen, new Point(8, 8), new Point(8, 24));
        dc.DrawText(new FormattedText("✦", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 24, new SolidColorBrush(Color.FromArgb(140, 210, 220, 255)), pixelsPerDip), new Point(w - 34, h - 38));
    }

    private static Point Lerp(Point a, Point b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return new Point(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
    }
}
