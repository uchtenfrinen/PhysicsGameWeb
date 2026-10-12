using System.Windows;
using System.Windows.Controls;
using WPFPhisics1._1;

namespace WPFPhisics1._1.Pages;

public partial class FreeFallView : UserControl
{
    public static readonly RoutedEvent BackRequestedEvent =
        EventManager.RegisterRoutedEvent(nameof(BackRequested), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(FreeFallView));

    public event RoutedEventHandler BackRequested
    {
        add => AddHandler(BackRequestedEvent, value);
        remove => RemoveHandler(BackRequestedEvent, value);
    }

    public FreeFallView()
    {
        InitializeComponent();
        DataContext = new FreeFallViewModel();
        Loaded += (_, _) => (DataContext as FreeFallViewModel)?.Build();
    }

    private void BackBtn_Click(object sender, RoutedEventArgs e) => RaiseEvent(new RoutedEventArgs(BackRequestedEvent));
}
