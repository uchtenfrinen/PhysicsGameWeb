using CommunityToolkit.Mvvm.Messaging;
using PhysicsTasks.Messaging;

namespace PhysicsTasks.Hosting;

public sealed class MessengerTaskSessionHost : ITaskSessionHost
{
    public void OpenTask(string taskId, string? topicId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        WeakReferenceMessenger.Default.Send(new TaskTerminalTriggeredMessage(taskId, topicId));
    }
}
