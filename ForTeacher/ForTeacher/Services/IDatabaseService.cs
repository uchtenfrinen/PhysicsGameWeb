using ForTeacher.Models;

namespace ForTeacher.Services;

/// <summary>
/// Опциональная синхронизация набора преподавательских задач с PostgreSQL (Dapper в реализации).
/// </summary>
public interface IDatabaseService
{
    Task UpsertTeacherTasksAsync(IReadOnlyList<TaskDefinition> teacherTasks, CancellationToken cancellationToken = default);
}
