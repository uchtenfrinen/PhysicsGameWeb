using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WPFPhisics1._1;

namespace WPFPhisics1._1.Pages;

public partial class InclinedPlaneView : UserControl
{
    private const double LayoutMargin = 40;
    private bool _planeDragging;
    private Point _planeBottom;
    private Point _planeTop;

    public static readonly RoutedEvent BackRequestedEvent =
        EventManager.RegisterRoutedEvent(nameof(BackRequested), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(InclinedPlaneView));

    public event RoutedEventHandler BackRequested
    {
        add => AddHandler(BackRequestedEvent, value);
        remove => RemoveHandler(BackRequestedEvent, value);
    }

    public InclinedPlaneView()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        Loaded += (_, _) => Dispatcher.BeginInvoke(() =>
        {
            (DataContext as MainViewModel)?.RefreshForces();
            MiniScheme.InvalidateVisual();
        });
    }

    private void BackBtn_Click(object sender, RoutedEventArgs e) => RaiseEvent(new RoutedEventArgs(BackRequestedEvent));

    private void UpdatePlaneGeometry()
    {
        var overlay = PlaneDragOverlay;
        if (overlay == null) return;
        double w = overlay.ActualWidth;
        double h = overlay.ActualHeight;
        if (w <= 0 || h <= 0) return;
        var vm = DataContext as MainViewModel;
        if (vm == null) return;
        double angleRad = vm.Plane.AngleRadians;
        double planeLength = Math.Min((w - 2 * LayoutMargin) / Math.Cos(angleRad), (h - 2 * LayoutMargin) / Math.Sin(angleRad));
        _planeBottom = new Point(LayoutMargin, h - LayoutMargin);
        _planeTop = new Point(LayoutMargin + planeLength * Math.Cos(angleRad), h - LayoutMargin - planeLength * Math.Sin(angleRad));
    }

    private double ProjectToPlaneT(Point p)
    {
        var v = new Vector(p.X - _planeBottom.X, p.Y - _planeBottom.Y);
        var along = new Vector(_planeTop.X - _planeBottom.X, _planeTop.Y - _planeBottom.Y);
        double len2 = along.X * along.X + along.Y * along.Y;
        if (len2 < 1e-10) return 0.5;
        return (v.X * along.X + v.Y * along.Y) / len2;
    }

    private void PlaneDragOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var vm = DataContext as MainViewModel;
        if (vm == null || !vm.IsDraggingEnabled) return;
        UpdatePlaneGeometry();
        _planeDragging = true;
        ((UIElement)sender).CaptureMouse();
    }

    private void PlaneDragOverlay_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_planeDragging) return;
        var vm = DataContext as MainViewModel;
        if (vm == null) return;
        UpdatePlaneGeometry();
        Point pos = e.GetPosition(PlaneDragOverlay);
        double t = ProjectToPlaneT(pos);
        t = Math.Clamp(t, 0.14, 0.86);
        vm.Block.PositionAlongPlane = t;
        vm.RefreshForces();
        vm.NotifyBlockChanged();
    }

    private void PlaneDragOverlay_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_planeDragging)
        {
            _planeDragging = false;
            ((UIElement)sender).ReleaseMouseCapture();
            (DataContext as MainViewModel)?.ClampBlockPositionForDrag();
        }
    }

    private void PlaneDragOverlay_MouseLeave(object sender, MouseEventArgs e)
    {
        if (_planeDragging)
        {
            _planeDragging = false;
            ((UIElement)sender).ReleaseMouseCapture();
            (DataContext as MainViewModel)?.ClampBlockPositionForDrag();
        }
    }
}
