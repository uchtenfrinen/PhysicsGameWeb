namespace PhysicsTasks;

/// <summary>
/// Единые пути для игры, терминала задач и панели преподавателя (два WPF + MonoGame).
/// </summary>
public static class PhysicsTasksPaths
{
    /// <summary>Папка в LocalAppData / ProgramData (как в PhisicsGame).</summary>
    public const string ApplicationFolderName = "PhisicsGame";

    public const string SaveFileName = "save.json";

    public const string TasksFileName = "tasks.json";

    /// <summary>Папка рядом с exe: <c>Data/tasks.json</c>.</summary>
    public const string PortableTasksRelativeFolder = "Data";

    /// <summary>Подкаталог в ProgramData для общего каталога задач.</summary>
    public const string SharedCatalogFolderSegment = "catalog";

    /// <summary>Переменная среды: абсолютный путь к <c>tasks.json</c> (dev, CI, кастомный деплой).</summary>
    public const string TasksJsonEnvironmentVariable = "PHYSICS_TASKS_JSON";

    public static string GetLocalAppDataRoot() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ApplicationFolderName);

    public static string GetDefaultSavePath() =>
        Path.Combine(GetLocalAppDataRoot(), SaveFileName);

    /// <summary>Каталог задач для синхронизации между WPF: преподаватель пишет, терминал читает.</summary>
    public static string GetSharedCatalogTasksPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            ApplicationFolderName,
            SharedCatalogFolderSegment,
            TasksFileName);

    public static string GetSharedCatalogDirectory() =>
        Path.GetDirectoryName(GetSharedCatalogTasksPath())!;

    public static string GetPortableTasksPath(string applicationBaseDirectory) =>
        Path.Combine(applicationBaseDirectory, PortableTasksRelativeFolder, TasksFileName);
}

/// <summary>
/// Выбор файла <c>tasks.json</c>: переменная среды → общий ProgramData → рядом с приложением.
/// </summary>
public static class TaskCatalogPathResolver
{
    public static string Resolve(string applicationBaseDirectory)
    {
        var envPath = Environment.GetEnvironmentVariable(PhysicsTasksPaths.TasksJsonEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
            return envPath;

        var shared = PhysicsTasksPaths.GetSharedCatalogTasksPath();
        if (File.Exists(shared))
            return shared;

        return PhysicsTasksPaths.GetPortableTasksPath(applicationBaseDirectory);
    }

    /// <summary>
    /// Путь к картинке в <c>tasks.json</c> задаётся относительно каталога игры (родитель папки <c>Data</c>, где лежит json).
    /// </summary>
    public static string ResolveImageFullPath(string tasksJsonFullPath, string? relativeImagePath)
    {
        if (string.IsNullOrWhiteSpace(relativeImagePath))
        {
            return string.Empty;
        }

        var r = relativeImagePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(r))
        {
            return Path.GetFullPath(r);
        }

        var tasksPath = Path.GetFullPath(tasksJsonFullPath);
        var dataDir = Path.GetDirectoryName(tasksPath);
        if (string.IsNullOrEmpty(dataDir))
        {
            return string.Empty;
        }

        var gameRoot = Path.GetDirectoryName(dataDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrEmpty(gameRoot))
        {
            return string.Empty;
        }

        return Path.GetFullPath(Path.Combine(gameRoot, r));
    }

    /// <summary>То же сопоставление пути относительно корня игры, что и для изображений (PDF, вложения).</summary>
    public static string ResolveAssetFullPath(string tasksJsonFullPath, string? relativePath) =>
        ResolveImageFullPath(tasksJsonFullPath, relativePath);
}
