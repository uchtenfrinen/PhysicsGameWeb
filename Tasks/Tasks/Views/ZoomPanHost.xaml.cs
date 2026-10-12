using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace PhysicsTasks.Terminal.Views;

/// <summary>
/// Область с масштабированием (через свойство <see cref="Zoom"/>) и сдвигом ЛКМ при увеличении.
/// </summary>
/// <remarks>
/// Дочерние элементы в XAML должны попадать в <see cref="ZoomContent"/>, а не в <see cref="ContentControl.Content"/>,
/// иначе они заменяют всю разметку контрола (типичная ошибка UserControl).
/// </remarks>
[ContentProperty(nameof(ZoomContent))]
public partial class ZoomPanHost : UserControl
{
    private bool _dragging;
    private Point _lastPos;

    public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(
        nameof(Zoom),
        typeof(double),
        typeof(ZoomPanHost),
        new PropertyMetadata(1.0, OnZoomPropertyChanged, CoerceZoom));

    public static readonly DependencyProperty PanXProperty = DependencyProperty.Register(
        nameof(PanX),
        typeof(double),
        typeof(ZoomPanHost));

    public static readonly DependencyProperty PanYProperty = DependencyProperty.Register(
        nameof(PanY),
        typeof(double),
        typeof(ZoomPanHost));

    public static readonly DependencyProperty PanZoomThresholdProperty = DependencyProperty.Register(
        nameof(PanZoomThreshold),
        typeof(double),
        typeof(ZoomPanHost),
        new PropertyMetadata(1.002));

    public static readonly DependencyProperty MinZoomProperty = DependencyProperty.Register(
        nameof(MinZoom),
        typeof(double),
        typeof(ZoomPanHost),
        new PropertyMetadata(0.5, OnMinMaxZoomChanged));

    public static readonly DependencyProperty MaxZoomProperty = DependencyProperty.Register(
        nameof(MaxZoom),
        typeof(double),
        typeof(ZoomPanHost),
        new PropertyMetadata(4.0, OnMinMaxZoomChanged));

    public static readonly DependencyProperty ZoomContentProperty = DependencyProperty.Register(
        nameof(ZoomContent),
        typeof(object),
        typeof(ZoomPanHost),
        new PropertyMetadata(null));

    private static void OnMinMaxZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ZoomPanHost h)
            h.CoerceValue(ZoomProperty);
    }

    public ZoomPanHost()
    {
        InitializeComponent();
        HitSurface.LostMouseCapture += (_, _) =>
        {
            if (!_dragging)
                return;
            _dragging = false;
            ApplyMousePointer();
        };
    }

    public double Zoom
    {
        get => (double)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public double PanX
    {
        get => (double)GetValue(PanXProperty);
        set => SetValue(PanXProperty, value);
    }

    public double PanY
    {
        get => (double)GetValue(PanYProperty);
        set => SetValue(PanYProperty, value);
    }

    /// <summary>Панорамирование разрешено только при масштабе выше этого значения.</summary>
    public double PanZoomThreshold
    {
        get => (double)GetValue(PanZoomThresholdProperty);
        set => SetValue(PanZoomThresholdProperty, value);
    }

    public double MinZoom
    {
        get => (double)GetValue(MinZoomProperty);
        set => SetValue(MinZoomProperty, value);
    }

    public double MaxZoom
    {
        get => (double)GetValue(MaxZoomProperty);
        set => SetValue(MaxZoomProperty, value);
    }

    /// <summary>Содержимое внутри масштабирования (в XAML — дочерний элемент контрола).</summary>
    public object? ZoomContent
    {
        get => GetValue(ZoomContentProperty);
        set => SetValue(ZoomContentProperty, value);
    }

    private static object CoerceZoom(DependencyObject d, object baseValue)
    {
        var h = (ZoomPanHost)d;
        var z = (double)baseValue;
        if (double.IsNaN(z) || double.IsInfinity(z))
            return 1.0;
        var lo = Math.Min(h.MinZoom, h.MaxZoom);
        var hi = Math.Max(h.MinZoom, h.MaxZoom);
        return Math.Clamp(z, lo, hi);
    }

    private static void OnZoomPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ZoomPanHost h)
            h.ApplyMousePointer();
    }

    private bool CanPanNow => Zoom > PanZoomThreshold + 1e-9;

    private void HitSurface_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!CanPanNow)
            return;

        _dragging = true;
        _lastPos = e.GetPosition(HitSurface);
        HitSurface.CaptureMouse();
        e.Handled = true;
    }

    private void HitSurface_MouseMove(object sender, MouseEventArgs e)
    {
        if (_dragging && e.LeftButton == MouseButtonState.Pressed)
        {
            var pos = e.GetPosition(HitSurface);
            var dx = pos.X - _lastPos.X;
            var dy = pos.Y - _lastPos.Y;
            _lastPos = pos;
            PanX += dx;
            PanY += dy;
            e.Handled = true;
        }
        else if (!_dragging)
            ApplyMousePointer();
    }

    private void HitSurface_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        EndDrag();
    }

    private void HitSurface_MouseEnter(object sender, MouseEventArgs e)
    {
        ApplyMousePointer();
    }

    private void HitSurface_MouseLeave(object sender, MouseEventArgs e)
    {
        if (!_dragging)
            ApplyMousePointer();
    }

    private void EndDrag()
    {
        if (!_dragging)
            return;
        _dragging = false;
        HitSurface.ReleaseMouseCapture();
        ApplyMousePointer();
    }

    private void ApplyMousePointer()
    {
        if (HitSurface.IsMouseCaptured && _dragging)
        {
            HitSurface.Cursor = Cursors.SizeAll;
            return;
        }

        HitSurface.Cursor = CanPanNow && HitSurface.IsMouseOver ? Cursors.SizeAll : null;
    }
}
