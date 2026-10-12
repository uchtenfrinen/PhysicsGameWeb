using System.Text.Json;

namespace PhisicsGame.Core.Save;

public sealed class SaveService
{
    private readonly string _savePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhisicsGame", "save.json");

    public SaveData Load()
    {
        if (!File.Exists(_savePath))
        {
            return new SaveData();
        }

        var json = File.ReadAllText(_savePath);
        var data = JsonSerializer.Deserialize<SaveData>(json) ?? new SaveData();
        data.CompletedTaskIds ??= [];
        return data;
    }

    public void Save(SaveData data)
    {
        var dir = Path.GetDirectoryName(_savePath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_savePath, json);
    }
}
