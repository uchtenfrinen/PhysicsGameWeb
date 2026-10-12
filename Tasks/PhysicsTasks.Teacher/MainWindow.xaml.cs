using System.Diagnostics;
using System.IO;
using System.Windows;
using PhysicsTasks;

namespace PhysicsTasks.Teacher;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        SharedPathText.Text = PhysicsTasksPaths.GetSharedCatalogTasksPath();
        SavePathText.Text = PhysicsTasksPaths.GetDefaultSavePath();
        EnvHintText.Text = $"{PhysicsTasksPaths.TasksJsonEnvironmentVariable}=C:\\path\\tasks.json";
    }

    private void OnOpenSharedFolder(object sender, RoutedEventArgs e)
    {
        var dir = PhysicsTasksPaths.GetSharedCatalogDirectory();
        Directory.CreateDirectory(dir);
        Process.Start(new ProcessStartInfo
        {
            FileName = dir,
            UseShellExecute = true,
        });
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
