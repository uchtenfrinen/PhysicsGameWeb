using System.Text.Json;

namespace PhisicsGame.Core.Tasks;

public sealed class TaskRepository
{
    private const string TeacherIdMarker = "_teacher_";

    private static readonly JsonSerializerOptions JsonReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string _filePath;
    private readonly List<TaskDefinition> _definitions = [];
    private readonly Dictionary<string, TaskDefinition> _taskById = new();
    private readonly Dictionary<string, List<TaskDefinition>> _tasksByTopic = new();

    public TaskRepository(string filePath)
    {
        _filePath = filePath;
        if (!File.Exists(filePath))
        {
            return;
        }

        var json = File.ReadAllText(filePath);
        var data = DeserializeTaskList(json);
        foreach (var definition in data)
        {
            AddToIndex(definition);
        }
    }

    /// <summary>
    /// Поддерживает корневой массив <c>[…]</c> и обёртку <c>{ "tasks": […] }</c> (как в терминале задач).
    /// </summary>
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

    public TaskDefinition GetById(string id) => _taskById[id];

    public IReadOnlyList<TaskDefinition> GetByTopic(string topic) =>
        _tasksByTopic.TryGetValue(topic, out var list) ? list : [];

    /// <summary>Задачи, созданные в панели преподавателя (по шаблону id …_teacher_…).</summary>
    public IReadOnlyList<TaskDefinition> GetTeacherTasksForTopic(string topic)
    {
        var list = GetByTopic(topic);
        if (list.Count == 0)
        {
            return [];
        }

        var filtered = new List<TaskDefinition>();
        foreach (var t in list)
        {
            if (t.Id.Contains(TeacherIdMarker, StringComparison.OrdinalIgnoreCase))
            {
                filtered.Add(t);
            }
        }

        filtered.Sort((a, b) => string.Compare(a.Id, b.Id, StringComparison.OrdinalIgnoreCase));
        return filtered;
    }

    /// <summary>Удаляет задачу преподавателя и перезаписывает tasks.json.</summary>
    public bool TryRemoveTeacherTaskById(string id, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(id))
        {
            error = "Не указан id задачи.";
            return false;
        }

        if (!_taskById.TryGetValue(id, out var definition))
        {
            error = "Задача не найдена.";
            return false;
        }

        if (!definition.Id.Contains(TeacherIdMarker, StringComparison.OrdinalIgnoreCase))
        {
            error = "Удалять можно только задачи, созданные в панели преподавателя.";
            return false;
        }

        TryDeleteTeacherImageFile(definition.ImagePath);
        RemoveFromIndex(definition);
        SaveAll();
        return true;
    }

    public void Add(TaskDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.Id))
        {
            throw new ArgumentException("Task id is required.", nameof(definition));
        }

        if (_taskById.ContainsKey(definition.Id))
        {
            throw new InvalidOperationException($"Task with id '{definition.Id}' already exists.");
        }

        AddToIndex(definition);
        SaveAll();
    }

    private void AddToIndex(TaskDefinition definition)
    {
        _definitions.Add(definition);
        _taskById[definition.Id] = definition;
        if (!_tasksByTopic.TryGetValue(definition.Topic, out var list))
        {
            list = [];
            _tasksByTopic[definition.Topic] = list;
        }

        list.Add(definition);
    }

    private void RemoveFromIndex(TaskDefinition definition)
    {
        _definitions.Remove(definition);
        _taskById.Remove(definition.Id);
        if (_tasksByTopic.TryGetValue(definition.Topic, out var list))
        {
            list.Remove(definition);
        }
    }

    private static void TryDeleteTeacherImageFile(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return;
        }

        var normalized = imagePath.Replace('\\', '/').TrimStart('/');
        if (!normalized.StartsWith("Data/TeacherImages/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            var full = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, normalized.Replace('/', Path.DirectorySeparatorChar)));
            var baseDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Data", "TeacherImages"));
            if (!full.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (File.Exists(full))
            {
                File.Delete(full);
            }
        }
        catch
        {
            // игнорируем сбой удаления файла — задача из json всё равно удалена
        }
    }

    private void SaveAll()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(_definitions, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }
}
