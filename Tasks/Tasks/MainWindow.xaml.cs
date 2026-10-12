using System.Windows;
using System.Windows.Input;
using PhysicsTasks.Terminal.ViewModels;
using PhysicsTasks.Terminal.Views;

namespace PhysicsTasks.Terminal;

public partial class MainWindow : Window
{
    public MainWindow(TaskWorkspaceViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        Workspace.DataContext = viewModel;
    }

    private void CloseWindowButton_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;

        if (DataContext is not TaskWorkspaceViewModel vm || !vm.IsFullscreenOpen)
            return;

        vm.CloseFullscreenMediaCommand.Execute(null);
        e.Handled = true;
    }
}
