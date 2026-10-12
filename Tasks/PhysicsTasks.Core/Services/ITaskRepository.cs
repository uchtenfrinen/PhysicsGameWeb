using PhysicsTasks.Models;

namespace PhysicsTasks.Services;

public interface ITaskRepository
{
    Task<IReadOnlyList<TaskDefinition>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TaskDefinition?> GetByIdAsync(string taskId, CancellationToken cancellationToken = default);
}
