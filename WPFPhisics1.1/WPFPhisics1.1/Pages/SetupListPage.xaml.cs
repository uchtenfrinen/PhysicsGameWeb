using System.Windows;
using System.Windows.Controls;
using WPFPhisics1._1.Navigation;

namespace WPFPhisics1._1.Pages;

public partial class SetupListPage : UserControl
{
    public static readonly RoutedEvent SetupSelectedEvent =
        EventManager.RegisterRoutedEvent(nameof(SetupSelected), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SetupListPage));

    public event RoutedEventHandler SetupSelected
    {
        add => AddHandler(SetupSelectedEvent, value);
        remove => RemoveHandler(SetupSelectedEvent, value);
    }

    private PhysicsSection? _section;

    public SetupListPage()
    {
        InitializeComponent();
    }

    public void SetSection(PhysicsSection section)
    {
        _section = section;
        SectionTitle.Text = section.Name;
        SetupsList.ItemsSource = section.Setups;
        EmptyMessage.Visibility = section.Setups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BackBtn_Click(object sender, RoutedEventArgs e) => RaiseEvent(new RoutedEventArgs(BackRequestedEvent));

    public static readonly RoutedEvent BackRequestedEvent =
        EventManager.RegisterRoutedEvent(nameof(BackRequested), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SetupListPage));

    public event RoutedEventHandler BackRequested
    {
        add => AddHandler(BackRequestedEvent, value);
        remove => RemoveHandler(BackRequestedEvent, value);
    }

    private void SetupButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is PhysicsSetup setup && _section != null)
            RaiseEvent(new SetupSelectedRoutedEventArgs(SetupSelectedEvent, _section, setup));
    }
}

public class SetupSelectedRoutedEventArgs : RoutedEventArgs
{
    public PhysicsSection Section { get; }
    public PhysicsSetup Setup { get; }

    public SetupSelectedRoutedEventArgs(RoutedEvent routedEvent, PhysicsSection section, PhysicsSetup setup) : base(routedEvent)
    {
        Section = section;
        Setup = setup;
    }
}
