using System.IO;

namespace ForTeacher.Services;

public static class AppPaths
{
    private static string? _tasksJsonOverride;

    /// <summary>Каталог приложения ForTeacher.exe (если не передан --tasks-json).</summary>
    public static string ApplicationBase => AppContext.BaseDirectory;

    /// <summary>Полный путь к tasks.json (игра или локальная копия).</summary>
    public static string TasksJsonPath =>
        _tasksJsonOverride ?? Path.Combine(ApplicationBase, "Data", "tasks.json");

    /// <summary>Каталог <c>Data</c>, в котором лежит <c>tasks.json</c>.</summary>
    public static string DataDirectory => Path.GetDirectoryName(TasksJsonPath) ?? ApplicationBase;

    /// <summary>Корень вывода игры: родитель каталога <c>Data</c> (рядом с exe игры).</summary>
    public static string GameContentRoot
    {
        get
        {
            var dataDir = Path.GetDirectoryName(TasksJsonPath);
            return string.IsNullOrEmpty(dataDir)
                ? ApplicationBase
                : Path.GetDirectoryName(dataDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                  ?? ApplicationBase;
        }
    }

    public static string TeacherImagesDirectory => Path.Combine(DataDirectory, "TeacherImages");

    public static string TeacherPdfsDirectory => Path.Combine(DataDirectory, "TeacherPdfs");

    /// <summary>Вызывать из <see cref="Application"/> до создания контекста (аргумент <c>--tasks-json</c> из MonoGame).</summary>
    public static void InitializeTasksJsonPath(string tasksJsonFullPath) =>
        _tasksJsonOverride = Path.GetFullPath(tasksJsonFullPath);

    /// <summary>Путь к файлу относительно <see cref="GameContentRoot"/> (как в json игры).</summary>
    public static string ResolveGameRelativePath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return string.Empty;

        var r = relativePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(GameContentRoot, r));
    }
}
