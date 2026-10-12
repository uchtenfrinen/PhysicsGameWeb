using System.Windows;
using System.Windows.Threading;
using PhysicsTasks;
using PhysicsTasks.Services;
using PhysicsTasks.Terminal.Labs;
using PhysicsTasks.Terminal.ViewModels;

namespace PhysicsTasks.Terminal;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            MessageBox.Show(
                e.Exception.ToString(),
                "PhysicsTasksTerminal",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            e.Handled = true;
        }
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var tasksPath = StartupArgsParser.TryGetTasksJsonPath(e.Args)
                            ?? TaskCatalogPathResolver.Resolve(AppContext.BaseDirectory);

            var repository = new JsonTaskRepository(tasksPath);
            var saveService = new SaveService();
            var labNavigator = new WpfPhyzixLabNavigator();

            var viewModel = new TaskWorkspaceViewModel(repository, saveService, labNavigator, tasksPath);
            var topicFilter = StartupArgsParser.TryGetTopicFilter(e.Args);
            var labSetupFilter = StartupArgsParser.TryGetLabSetupFilter(e.Args);
            await viewModel.InitializeAsync(topicFilter, labSetupFilter);

            var parsed = StartupArgsParser.TryParseTask(e.Args);
            if (parsed is { } p)
                viewModel.OpenTask(p.TaskId, p.TopicId);

            MainWindow = new MainWindow(viewModel);
            MainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToString(),
                "PhysicsTasksTerminal — ошибка запуска",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
