using System.Text.Json;
using PhysicsTasks.Models;

namespace PhysicsTasks.Services;

/// <summary>
/// Loads tasks from a JSON file (array or <c>{ "tasks": [...] }</c>).
/// </summary>
public sealed class JsonTaskRepository : ITaskRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string _path;
    private IReadOnlyList<TaskDefinition>? _cache;

    public JsonTaskRepository(string tasksJsonPath)
    {
        _path = tasksJsonPath ?? throw new ArgumentNullException(nameof(tasksJsonPath));
    }

    public async Task<IReadOnlyList<TaskDefinition>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _cache ??= await LoadCoreAsync(cancellationToken).ConfigureAwait(false);
        return _cache;
    }

    public async Task<TaskDefinition?> GetByIdAsync(string taskId, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken).ConfigureAwait(false);
        return all.FirstOrDefault(t => string.Equals(t.Id, taskId, StringComparison.Ordinal));
    }

    private async Task<IReadOnlyList<TaskDefinition>> LoadCoreAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path))
            return Array.Empty<TaskDefinition>();

        var text = await File.ReadAllTextAsync(_path, cancellationToken).ConfigureAwait(false);
        if (PhisicsGameTaskCatalogReader.TryImport(text, out var fromGame))
            return fromGame;

        using var doc = JsonDocument.Parse(text, default);

        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            return JsonSerializer.Deserialize<List<TaskDefinition>>(doc.RootElement.GetRawText(), JsonOptions)
                   ?? new List<TaskDefinition>();
        }

        if (doc.RootElement.TryGetProperty("tasks", out var tasksEl) && tasksEl.ValueKind == JsonValueKind.Array)
        {
            return JsonSerializer.Deserialize<List<TaskDefinition>>(tasksEl.GetRawText(), JsonOptions)
                   ?? new List<TaskDefinition>();
        }

        return Array.Empty<TaskDefinition>();
    }
}

