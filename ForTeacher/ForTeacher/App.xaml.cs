using System.Windows;
using ForTeacher.Services;
using ForTeacher.Views;
using QuestPDF.Infrastructure;

namespace ForTeacher;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        QuestPDF.Settings.License = LicenseType.Community;
        ShutdownMode = ShutdownMode.OnLastWindowClose;

        var tasksArg = GetCommandLineValue(e.Args, "--tasks-json");
        if (!string.IsNullOrWhiteSpace(tasksArg))
            AppPaths.InitializeTasksJsonPath(tasksArg);

        var ctx = TeacherModuleFactory.CreateDefault();
        new LoginWindow(ctx).Show();
    }

    private static string? GetCommandLineValue(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return null;
    }
}
