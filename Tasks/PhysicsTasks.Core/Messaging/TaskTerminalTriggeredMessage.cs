using CommunityToolkit.Mvvm.Messaging.Messages;

namespace PhysicsTasks.Messaging;

public readonly record struct TaskTerminalPayload(string TaskId, string? TopicId);

/// <summary>
/// Cross-window signal: focus a task in the task terminal (WeakReferenceMessenger).
/// </summary>
public sealed class TaskTerminalTriggeredMessage : ValueChangedMessage<TaskTerminalPayload>
{
    public TaskTerminalTriggeredMessage(string taskId, string? topicId = null)
        : base(CreatePayload(taskId, topicId))
    {
    }

    private static TaskTerminalPayload CreatePayload(string taskId, string? topicId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        return new TaskTerminalPayload(taskId, topicId);
    }
}
