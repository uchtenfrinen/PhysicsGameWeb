using System.Text.Json;
using System.Text.Json.Serialization;
using PhysicsTasks.Models;

namespace PhysicsTasks.Services;

/// <summary>
/// Persists progress to the same relative path as <c>PhisicsGame</c>: <c>%LocalAppData%/PhisicsGame/save.json</c>.
/// </summary>
public sealed class SaveService : ISaveService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _filePath;

    public SaveService(string? overridePath = null)
    {
        _filePath = overridePath ?? PhysicsTasksPaths.GetDefaultSavePath();
    }

    public static string GetDefaultSavePath() => PhysicsTasksPaths.GetDefaultSavePath();

    public SaveData Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new SaveData();

            var json = File.ReadAllText(_filePath);
            var dto = JsonSerializer.Deserialize<SaveFileDto>(json, JsonOptions);
            var data = new SaveData();
            if (dto?.CompletedTaskIds is not null)
            {
                foreach (var id in dto.CompletedTaskIds)
                    data.CompletedTaskIds.Add(id);
            }

            return data;
        }
        catch
        {
            return new SaveData();
        }
    }

    public void Save(SaveData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var dto = new SaveFileDto { CompletedTaskIds = data.CompletedTaskIds.ToArray() };
        var json = JsonSerializer.Serialize(dto, JsonOptions);
        File.WriteAllText(_filePath, json);
    }

    private sealed class SaveFileDto
    {
        public string[]? CompletedTaskIds { get; set; }
    }
}
