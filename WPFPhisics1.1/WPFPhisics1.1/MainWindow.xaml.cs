using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFPhisics1._1.Navigation;
using WPFPhisics1._1.Pages;
using WPFPhisics1._1.Services;

namespace WPFPhisics1._1;

public partial class MainWindow : Window
{
    private PhysicsSection? _currentSection;

    public MainWindow()
    {
        InitializeComponent();

        // Без рамки, на весь экран (надёжнее, чем только атрибуты в XAML при некоторых DPI/запусках).
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        WindowState = WindowState.Maximized;

        ContentRendered += (_, _) =>
        {
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
        };

        Loaded += (_, _) =>
        {
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
        };

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && MainBackButton.Visibility == Visibility.Visible)
            {
                MainBackButton_Click(MainBackButton, new RoutedEventArgs());
                e.Handled = true;
            }
        };

        NavigateOnStartup();
    }

    private void NavigateOnStartup()
    {
        if (TryParseLaunchSetupId(out var setupId) && TryOpenSetupById(setupId))
        {
            return;
        }

        ShowMainMenu();
    }

    private static bool TryParseLaunchSetupId(out string setupId)
    {
        setupId = string.Empty;
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], "--setup", StringComparison.OrdinalIgnoreCase))
            {
                setupId = args[i + 1] ?? string.Empty;
                return !string.IsNullOrWhiteSpace(setupId);
            }
        }

        return false;
    }

    private bool TryOpenSetupById(string setupId)
    {
        foreach (var section in PhysicsCatalog.Sections)
        {
            foreach (var setup in section.Setups)
            {
                if (!string.Equals(setup.Id, setupId, StringComparison.Ordinal))
                {
                    continue;
                }

                _currentSection = section;
                OpenSetupView(setup);
                return true;
            }
        }

        return false;
    }

    private void OpenSetupView(PhysicsSetup setup)
    {
        var view = CreateSetupView(setup.Id);
        if (view is null)
        {
            return;
        }

        PhysicsChatContext.CurrentSetupName = setup.Name;
        AttachBackHandler(view);
        MainContent.Content = view;
        UpdateMainChrome();
    }

    private static UserControl? CreateSetupView(string setupId) =>
        setupId switch
        {
            "inclined_plane" => new InclinedPlaneView(),
            "free_fall" => new FreeFallView(),
            "projectile" => new ProjectileMotionView(),
            "spring" => new SpringOscillatorView(),
            "buoyancy" => new BuoyancyView(),
            "communicating_vessels" => new CommunicatingVesselsView(),
            "ideal_gas" => new IdealGasView(),
            "ohm_law" => new OhmLawView(),
            "resistors_connection" => new ResistorsConnectionView(),
            "charge_in_field" => new ChargeInFieldView(),
            "thin_lens" => new ThinLensView(),
            "refraction_at_boundary" => new RefractionAtBoundaryView(),
            "harmonic_oscillations" => new HarmonicOscillationsView(),
            _ => null
        };

    private void AttachBackHandler(UserControl view)
    {
        switch (view)
        {
            case InclinedPlaneView v:
                v.AddHandler(InclinedPlaneView.BackRequestedEvent, (RoutedEventHandler)OnBackFromSetup);
                break;
            case FreeFallView v:
                v.AddHandler(FreeFallView.BackRequestedEvent, (RoutedEventHandler)OnBackFromSetup);
                break;
            case ProjectileMotionView v:
                v.AddHandler(ProjectileMotionView.BackRequestedEvent, (RoutedEventHandler)OnBackFromSetup);
                break;
            case SpringOscillatorView v:
                v.AddHandler(SpringOscillatorView.BackRequestedEvent, (RoutedEventHandler)OnBackFromSetup);
                break;
            case BuoyancyView v:
                v.AddHandler(BuoyancyView.BackRequestedEvent, (RoutedEventHandler)OnBackFromSetup);
                break;
            case CommunicatingVesselsView v:
                v.AddHandler(CommunicatingVesselsView.BackRequestedEvent, (RoutedEventHandler)OnBackFromSetup);
                break;
            case IdealGasView v:
                v.AddHandler(IdealGasView.BackRequestedEvent, (RoutedEventHandler)OnBackFromSetup);
                break;
            case OhmLawView v:
                v.AddHandler(OhmLawView.BackRequestedEvent, (RoutedEventHandler)OnBackFromSetup);
                break;
            case ResistorsConnectionView v:
                v.AddHandler(ResistorsConnectionView.BackRequestedEvent, (RoutedEventHandler)OnBackFromSetup);
                break;
            case ChargeInFieldView v:
                v.AddHandler(ChargeInFieldView.BackRequestedEvent, (RoutedEventHandler)OnBackFromSetup);
                break;
            case ThinLensView v:
                v.AddHandler(ThinLensView.BackRequestedEvent, (RoutedEventHandler)OnBackFromSetup);
                break;
            case RefractionAtBoundaryView v:
                v.AddHandler(RefractionAtBoundaryView.BackRequestedEvent, (RoutedEventHandler)OnBackFromSetup);
                break;
            case HarmonicOscillationsView v:
                v.AddHandler(HarmonicOscillationsView.BackRequestedEvent, (RoutedEventHandler)OnBackFromSetup);
                break;
        }
    }

    private void UpdateMainChrome()
    {
        MainBackButton.Visibility = MainContent.Content is MainMenuPage ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ShowMainMenu()
    {
        PhysicsChatContext.CurrentSetupName = "";
        var menu = new MainMenuPage();
        menu.AddHandler(MainMenuPage.SectionSelectedEvent, (RoutedEventHandler)OnSectionSelected);
        MainContent.Content = menu;
        _currentSection = null;
        UpdateMainChrome();
    }

    private void MainBackButton_Click(object sender, RoutedEventArgs e)
    {
        switch (MainContent.Content)
        {
            case SetupListPage:
                ShowMainMenu();
                return;
            case MainMenuPage:
                return;
        }

        if (_currentSection != null)
        {
            var listPage = new SetupListPage();
            listPage.SetSection(_currentSection);
            listPage.AddHandler(SetupListPage.SetupSelectedEvent, (RoutedEventHandler)OnSetupSelected);
            listPage.AddHandler(SetupListPage.BackRequestedEvent, (RoutedEventHandler)OnBackFromSetups);
            MainContent.Content = listPage;
        }
        else
        {
            ShowMainMenu();
        }

        UpdateMainChrome();
    }

    private void OnSectionSelected(object sender, RoutedEventArgs e)
    {
        if (e is not SectionSelectedRoutedEventArgs args) return;
        _currentSection = args.Section;
        PhysicsChatContext.CurrentSetupName = "";
        var listPage = new SetupListPage();
        listPage.SetSection(args.Section);
        listPage.AddHandler(SetupListPage.SetupSelectedEvent, (RoutedEventHandler)OnSetupSelected);
        listPage.AddHandler(SetupListPage.BackRequestedEvent, (RoutedEventHandler)OnBackFromSetups);
        MainContent.Content = listPage;
        UpdateMainChrome();
    }

    private void OnSetupSelected(object sender, RoutedEventArgs e)
    {
        if (e is not SetupSelectedRoutedEventArgs args || _currentSection == null)
        {
            return;
        }

        OpenSetupView(args.Setup);
    }

    private void OnBackFromSetups(object sender, RoutedEventArgs e) => ShowMainMenu();

    private void OnBackFromSetup(object sender, RoutedEventArgs e)
    {
        if (_currentSection == null) return;
        var listPage = new SetupListPage();
        listPage.SetSection(_currentSection);
        listPage.AddHandler(SetupListPage.SetupSelectedEvent, (RoutedEventHandler)OnSetupSelected);
        listPage.AddHandler(SetupListPage.BackRequestedEvent, (RoutedEventHandler)OnBackFromSetups);
        MainContent.Content = listPage;
        UpdateMainChrome();
    }

    private void OpenChatButton_Click(object sender, RoutedEventArgs e)
    {
        var chat = new ChatWindow();
        chat.Owner = this;
        chat.Left = Left + Width - chat.Width - 24;
        chat.Top = Top + 60;
        chat.Show();
    }

    private void CloseWindowButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        Close();
    }
}
