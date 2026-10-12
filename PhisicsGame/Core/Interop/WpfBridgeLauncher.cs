using System.Diagnostics;

namespace PhisicsGame.Core.Interop;

public static class WpfBridgeLauncher
{
    public static bool TryLaunch(out string error) => TryLaunchInternal(null, out error);

    public static bool TryLaunchSetup(string setupId, out string error)
    {
        if (string.IsNullOrWhiteSpace(setupId))
        {
            error = "Empty setup id.";
            return false;
        }

        return TryLaunchInternal(setupId, out error);
    }

    /// <summary>WPF «Терминал задач» — все задачи из <c>tasks.json</c> игры.</summary>
    public static bool TryLaunchTasksTerminal(out string error) =>
        TryLaunchTasksTerminal(null, out error);

    /// <summary>WPF «Терминал задач» с фильтром по разделу (<c>--topic mechanics</c> и т.д.).</summary>
    public static bool TryLaunchTasksTerminal(string? topicId, out string error)
        => TryLaunchTasksTerminal(topicId, null, out error);

    /// <summary>WPF «Терминал задач» с фильтрами по разделу и подтеме (<c>--lab-setup</c>).</summary>
    public static bool TryLaunchTasksTerminal(string? topicId, string? labSetupId, out string error)
    {
        error = string.Empty;
        var tasksJson = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Data", "tasks.json"));
        var extra = new List<string> { "--tasks-json", tasksJson };
        if (!string.IsNullOrWhiteSpace(topicId))
        {
            extra.Add("--topic");
            extra.Add(topicId.Trim());
        }
        if (!string.IsNullOrWhiteSpace(labSetupId))
        {
            extra.Add("--lab-setup");
            extra.Add(labSetupId.Trim());
        }

        var args = extra.ToArray();

        if (TryStartTasksFromSameDirectoryAsGame(args, out error))
            return true;

        if (!string.IsNullOrEmpty(error))
            return false;

        if (TryStartTasksFromProjectBuild(args, out error))
            return true;

        error =
            "Не найден PhysicsTasksTerminal.exe. Соберите проект Tasks (PhysicsTasks.Terminal) или скопируйте exe рядом с PhisicsGame.exe.";
        return false;
    }

    /// <summary>Запуск отдельного WPF-модуля преподавателя (ForTeacher или ForCheater) с тем же <c>tasks.json</c>, что у игры.</summary>
    public static bool TryLaunchTeacherPanel(out string error)
    {
        error = string.Empty;
        var tasksJson = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Data", "tasks.json"));
        var extra = new[] { "--tasks-json", tasksJson };

        if (TryStartTeacherFromSameDirectoryAsGame(extra, out error))
            return true;

        if (!string.IsNullOrEmpty(error))
            return false;

        if (TryStartTeacherFromProjectBuild(extra, out error))
            return true;

        error =
            "Не найден ForTeacher.exe / ForCheater.exe. Соберите проект ForTeacher (или ForCheater) и при необходимости скопируйте exe рядом с PhisicsGame.exe.";
        return false;
    }

    private static bool TryStartTeacherFromSameDirectoryAsGame(string[] extraArgs, out string error)
    {
        error = string.Empty;
        var baseDir = AppContext.BaseDirectory;
        foreach (var exe in new[] { "ForTeacher.exe", "ForCheater.exe" })
        {
            var full = Path.Combine(baseDir, exe);
            if (!File.Exists(full))
                continue;

            if (!StartProcess(
                    new ProcessStartInfo
                    {
                        FileName = full,
                        WorkingDirectory = baseDir,
                        UseShellExecute = false,
                    },
                    extraArgs,
                    out error))
                return false;

            return true;
        }

        return false;
    }

    private static readonly (string Dir1, string Dir2, string Exe)[] TasksProjectLayouts =
    [
        ("Tasks", "Tasks", "PhysicsTasksTerminal.exe"),
    ];

    private static bool TryStartTasksFromSameDirectoryAsGame(string[] extraArgs, out string error)
    {
        error = string.Empty;
        var baseDir = AppContext.BaseDirectory;
        var full = Path.Combine(baseDir, "PhysicsTasksTerminal.exe");
        if (!File.Exists(full))
            return false;

        return StartProcess(
            new ProcessStartInfo
            {
                FileName = full,
                WorkingDirectory = baseDir,
                UseShellExecute = false,
            },
            extraArgs,
            out error);
    }

    private static bool TryStartTasksFromProjectBuild(string[] extraArgs, out string error)
    {
        error = string.Empty;
        foreach (var layout in TasksProjectLayouts)
        {
            if (!TryFindNestedProjectDir(layout.Dir1, layout.Dir2, out var projectDir))
                continue;

            foreach (var binDir in new[]
                     {
                         Path.Combine(projectDir, "bin", "Debug", "net9.0-windows"),
                         Path.Combine(projectDir, "bin", "Release", "net9.0-windows"),
                     })
            {
                var exePath = Path.Combine(binDir, layout.Exe);
                if (!File.Exists(exePath))
                    continue;

                if (!StartProcess(
                        new ProcessStartInfo
                        {
                            FileName = exePath,
                            WorkingDirectory = binDir,
                            UseShellExecute = false,
                        },
                        extraArgs,
                        out error))
                    return false;

                return true;
            }
        }

        return false;
    }

    private static readonly (string Dir1, string Dir2, string Exe)[] TeacherProjectLayouts =
    [
        ("ForTeacher", "ForTeacher", "ForTeacher.exe"),
        ("ForCheater", "ForCheater", "ForCheater.exe"),
    ];

    private static bool TryStartTeacherFromProjectBuild(string[] extraArgs, out string error)
    {
        error = string.Empty;
        foreach (var layout in TeacherProjectLayouts)
        {
            if (!TryFindNestedProjectDir(layout.Dir1, layout.Dir2, out var projectDir))
                continue;

            foreach (var binDir in new[]
                     {
                         Path.Combine(projectDir, "bin", "Debug", "net9.0-windows"),
                         Path.Combine(projectDir, "bin", "Release", "net9.0-windows"),
                     })
            {
                var exePath = Path.Combine(binDir, layout.Exe);
                if (!File.Exists(exePath))
                    continue;

                if (!StartProcess(
                        new ProcessStartInfo
                        {
                            FileName = exePath,
                            WorkingDirectory = binDir,
                            UseShellExecute = false,
                        },
                        extraArgs,
                        out error))
                    return false;

                return true;
            }
        }

        return false;
    }

    private static bool TryFindNestedProjectDir(string a, string b, out string projectDir)
    {
        projectDir = string.Empty;
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, a, b);
            if (Directory.Exists(candidate))
            {
                projectDir = candidate;
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private static bool StartProcess(ProcessStartInfo psi, string[] extraArgs, out string error)
    {
        error = string.Empty;
        TryInjectGroqApiKeyForTeacherProcess(psi);
        foreach (var a in extraArgs)
            psi.ArgumentList.Add(a);

        try
        {
            var process = Process.Start(psi);
            if (process is null)
            {
                error = "Не удалось запустить процесс.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// ForTeacher ищет ключ от своего <c>BaseDirectory</c>; при запуске из bin ForTeacher ключ из WPF/игры может не находиться.
    /// Подставляем <c>GROQ_API_KEY</c> в окружение дочернего процесса, если в текущем процессе его ещё нет, но файл найден от каталога игры.
    /// </summary>
    private static void TryInjectGroqApiKeyForTeacherProcess(ProcessStartInfo psi)
    {
        try
        {
            var name = Path.GetFileName(psi.FileName);
            if (!name.Equals("ForTeacher.exe", StringComparison.OrdinalIgnoreCase) &&
                !name.Equals("ForCheater.exe", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GROQ_API_KEY")))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FOR_TEACHER_OPENAI_API_KEY")))
            {
                return;
            }

            var key = GroqApiKeyFileLocator.TryReadFirstGroqKey(AppContext.BaseDirectory);
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            psi.Environment["GROQ_API_KEY"] = key;
        }
        catch
        {
            // не мешаем запуску
        }
    }

    private static bool TryFindWpfProjectDir(out string wpfProjectDir)
    {
        wpfProjectDir = string.Empty;
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "WPFPhisics1.1", "WPFPhisics1.1");
            if (Directory.Exists(candidate))
            {
                wpfProjectDir = candidate;
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private static bool TryLaunchInternal(string? setupId, out string error)
    {
        error = string.Empty;
        if (!TryFindWpfProjectDir(out var wpfProjectDir))
        {
            error = "WPF project directory not found.";
            return false;
        }

        var launchInfo = ResolveLaunchTarget(wpfProjectDir, setupId);
        if (launchInfo is null)
        {
            error = "WPF build output not found. Build WPFPhisics1.1 first.";
            return false;
        }

        try
        {
            using var process = Process.Start(launchInfo);
            if (process is null)
            {
                error = "Failed to start WPF process.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static ProcessStartInfo? ResolveLaunchTarget(string wpfProjectDir, string? setupId)
    {
        var outputCandidates = new[]
        {
            Path.Combine(wpfProjectDir, "bin", "Debug", "net9.0-windows"),
            Path.Combine(wpfProjectDir, "bin", "Release", "net9.0-windows"),
        };

        foreach (var dir in outputCandidates)
        {
            var exePath = Path.Combine(dir, "WPFPhisics1.1.exe");
            if (File.Exists(exePath))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = dir,
                    UseShellExecute = false,
                };
                if (!string.IsNullOrWhiteSpace(setupId))
                {
                    psi.ArgumentList.Add("--setup");
                    psi.ArgumentList.Add(setupId);
                }

                return psi;
            }
        }

        foreach (var dir in outputCandidates)
        {
            var dllPath = Path.Combine(dir, "WPFPhisics1.1.dll");
            if (File.Exists(dllPath))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    WorkingDirectory = dir,
                    UseShellExecute = false,
                };
                psi.ArgumentList.Add(dllPath);
                if (!string.IsNullOrWhiteSpace(setupId))
                {
                    psi.ArgumentList.Add("--setup");
                    psi.ArgumentList.Add(setupId);
                }

                return psi;
            }
        }

        return null;
    }
}
