using ForTeacher.Models;

namespace ForTeacher.Services;

public sealed class NullDatabaseService : IDatabaseService
{
    public Task UpsertTeacherTasksAsync(IReadOnlyList<TaskDefinition> teacherTasks, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
