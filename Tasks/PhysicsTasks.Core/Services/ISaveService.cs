using PhysicsTasks.Models;

namespace PhysicsTasks.Services;

public interface ISaveService
{
    SaveData Load();

    void Save(SaveData data);
}
