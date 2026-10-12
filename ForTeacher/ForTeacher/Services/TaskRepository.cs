using System.IO;
using System.Linq;
using System.Text.Json;
using ForTeacher.Models;

namespace ForTeacher.Services;

public sealed class TaskRepository
{
    private const string TeacherMarker = "_teacher_";

    private static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        WriteIndented = true,
    };

    private static readonly JsonSerializerOptions JsonReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string _jsonPath;
    private readonly object _lock = new();

    public TaskRepository(string jsonPath)
    {
        _jsonPath = jsonPath ?? throw new ArgumentNullException(nameof(jsonPath));
    }

    public event EventHandler? TasksFileChanged;

    public IReadOnlyList<TaskDefinition> LoadAll()
    {
        lock (_lock)
        {
            return LoadAllUnlocked();
        }
    }

    public void Add(TaskDefinition task)
    {
        if (!task.IsTeacherTask)
        {
            throw new InvalidOperationException("Через модуль преподавателя добавляются только задачи с id …_teacher_….");
        }

        lock (_lock)
        {
            var all = LoadAllUnlocked();
            if (all.Any(x => string.Equals(x.Id, task.Id, StringComparison.Ordinal)))
                throw new InvalidOperationException($"Задача с id «{task.Id}» уже существует.");

            all.Add(task);
            SaveUnlocked(all);
        }

        TasksFileChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Заменяет существующую преподавательскую задачу с тем же <see cref="TaskDefinition.Id"/>.</summary>
    public void ReplaceTeacherTask(TaskDefinition newTask)
    {
        if (!newTask.IsTeacherTask)
        {
            throw new InvalidOperationException("Через модуль преподавателя редактируются только задачи с id …_teacher_….");
        }

        lock (_lock)
        {
            var all = LoadAllUnlocked();
            var idx = all.FindIndex(x =>
                string.Equals(x.Id, newTask.Id, StringComparison.Ordinal) && x.IsTeacherTask);

            if (idx < 0)
                throw new InvalidOperationException($"Задача «{newTask.Id}» не найдена.");

            var old = all[idx];
            if (!string.Equals(old.ImagePath, newTask.ImagePath, StringComparison.Ordinal))
                TryDeleteTeacherImageFile(old.ImagePath);

            if (!string.Equals(old.PdfPath, newTask.PdfPath, StringComparison.Ordinal))
                TryDeleteTeacherPdfFile(old.PdfPath);

            all[idx] = newTask;
            SaveUnlocked(all);
        }

        TasksFileChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool TryRemoveTeacherTaskById(string id)
    {
        lock (_lock)
        {
            var all = LoadAllUnlocked();
            var idx = all.FindIndex(x =>
                string.Equals(x.Id, id, StringComparison.Ordinal) && x.IsTeacherTask);

            if (idx < 0)
                return false;

            TryDeleteTeacherImageFile(all[idx].ImagePath);
            TryDeleteTeacherPdfFile(all[idx].PdfPath);
            all.RemoveAt(idx);
            SaveUnlocked(all);
        }

        TasksFileChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private List<TaskDefinition> LoadAllUnlocked()
    {
        EnsureDirectories();
        if (!File.Exists(_jsonPath))
            return [];

        var json = File.ReadAllText(_jsonPath);
        return DeserializeTaskList(json);
    }

    /// <summary>Как в игре: массив <c>[…]</c> или <c>{ "tasks": […] }</c>.</summary>
    private static List<TaskDefinition> DeserializeTaskList(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
        {
            return JsonSerializer.Deserialize<List<TaskDefinition>>(root.GetRawText(), JsonReadOptions) ?? [];
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Name.Equals("tasks", StringComparison.OrdinalIgnoreCase)
                    && prop.Value.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<TaskDefinition>>(prop.Value.GetRawText(), JsonReadOptions)
                           ?? [];
                }
            }
        }

        return [];
    }

    private void SaveUnlocked(IReadOnlyList<TaskDefinition> tasks)
    {
        EnsureDirectories();
        var json = JsonSerializer.Serialize(tasks.ToList(), JsonWriteOptions);
        File.WriteAllText(_jsonPath, json);
    }

    private void EnsureDirectories()
    {
        var dir = Path.GetDirectoryName(_jsonPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        Directory.CreateDirectory(AppPaths.TeacherImagesDirectory);
        Directory.CreateDirectory(AppPaths.TeacherPdfsDirectory);
    }

    private static void TryDeleteTeacherImageFile(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return;

        var normalized = imagePath.Replace('\\', '/').TrimStart('/');
        if (!normalized.StartsWith("Data/TeacherImages/", StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            var full = AppPaths.ResolveGameRelativePath(normalized);
            var baseDir = Path.GetFullPath(AppPaths.TeacherImagesDirectory);
            if (!full.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                return;

            if (File.Exists(full))
                File.Delete(full);
        }
        catch
        {
            // ignore
        }
    }

    private static void TryDeleteTeacherPdfFile(string? pdfPath)
    {
        if (string.IsNullOrWhiteSpace(pdfPath))
            return;

        var normalized = pdfPath.Replace('\\', '/').TrimStart('/');
        if (!normalized.StartsWith("Data/TeacherPdfs/", StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            var full = AppPaths.ResolveGameRelativePath(normalized);
            var baseDir = Path.GetFullPath(AppPaths.TeacherPdfsDirectory);
            if (!full.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                return;

            if (File.Exists(full))
                File.Delete(full);
        }
        catch
        {
            // ignore
        }
    }
}
