namespace PhysicsTasks.Hosting;

/// <summary>
/// Entry point for the host game or shell to focus a task in the workspace.
/// </summary>
public interface ITaskSessionHost
{
    void OpenTask(string taskId, string? topicId = null);
}
