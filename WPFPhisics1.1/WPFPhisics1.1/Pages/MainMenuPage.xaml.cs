using System.Windows;
using System.Windows.Controls;
using WPFPhisics1._1.Navigation;

namespace WPFPhisics1._1.Pages;

public partial class MainMenuPage : UserControl
{
    public static readonly RoutedEvent SectionSelectedEvent =
        EventManager.RegisterRoutedEvent(nameof(SectionSelected), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MainMenuPage));

    public event RoutedEventHandler SectionSelected
    {
        add => AddHandler(SectionSelectedEvent, value);
        remove => RemoveHandler(SectionSelectedEvent, value);
    }

    public MainMenuPage()
    {
        InitializeComponent();
        SectionsList.ItemsSource = PhysicsCatalog.Sections;
    }

    private void SectionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is PhysicsSection section)
            RaiseEvent(new SectionSelectedRoutedEventArgs(SectionSelectedEvent, section));
    }

}

public class SectionSelectedRoutedEventArgs : RoutedEventArgs
{
    public PhysicsSection Section { get; }

    public SectionSelectedRoutedEventArgs(RoutedEvent routedEvent, PhysicsSection section) : base(routedEvent)
    {
        Section = section;
    }
}
