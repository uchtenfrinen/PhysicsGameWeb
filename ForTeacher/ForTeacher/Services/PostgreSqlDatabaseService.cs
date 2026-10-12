using System.Data;
using System.Linq;
using Dapper;
using ForTeacher.Models;
using Npgsql;

namespace ForTeacher.Services;

/// <summary>
/// Пример реализации: требуется строка подключения и таблица <c>teacher_tasks</c> (см. README).
/// Поле <c>lab_wpf_setup_id</c> хранит строковый id установки (как в игре).
/// </summary>
public sealed class PostgreSqlDatabaseService : IDatabaseService
{
    private readonly string _connectionString;

    public PostgreSqlDatabaseService(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task UpsertTeacherTasksAsync(IReadOnlyList<TaskDefinition> teacherTasks, CancellationToken cancellationToken = default)
    {
        if (teacherTasks.Count == 0)
            return;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            INSERT INTO teacher_tasks (id, topic, title, text, expected_numeric_answer, image_relative_path, lab_wpf_setup_id, is_teacher_task, difficulty, ai_reasoning, key_laws, estimated_time_min)
            VALUES (@Id, @Topic, @Title, @Text, @ExpectedNumericAnswer, @ImageRelativePath, @LabWpfSetupId, @IsTeacherTask, @Difficulty, @AiReasoning, @KeyLaws, @EstimatedTimeMin)
            ON CONFLICT (id) DO UPDATE SET
                topic = EXCLUDED.topic,
                title = EXCLUDED.title,
                text = EXCLUDED.text,
                expected_numeric_answer = EXCLUDED.expected_numeric_answer,
                image_relative_path = EXCLUDED.image_relative_path,
                lab_wpf_setup_id = EXCLUDED.lab_wpf_setup_id,
                is_teacher_task = EXCLUDED.is_teacher_task,
                difficulty = EXCLUDED.difficulty,
                ai_reasoning = EXCLUDED.ai_reasoning,
                key_laws = EXCLUDED.key_laws,
                estimated_time_min = EXCLUDED.estimated_time_min;
            """;

        foreach (var t in teacherTasks.Where(x => x.IsTeacherTask))
        {
            await conn.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        t.Id,
                        t.Topic,
                        Title = t.TitleKey,
                        Text = t.DescriptionKey,
                        ExpectedNumericAnswer = t.ExpectedNumeric,
                        ImageRelativePath = t.ImagePath,
                        LabWpfSetupId = t.LabWpfSetupId ?? string.Empty,
                        t.IsTeacherTask,
                        Difficulty = t.Difficulty <= 0 ? (int?)null : Math.Clamp(t.Difficulty, 1, 5),
                        AiReasoning = string.IsNullOrWhiteSpace(t.AiReasoning) ? null : t.AiReasoning,
                        KeyLaws = string.IsNullOrWhiteSpace(t.KeyLawsJson) ? null : t.KeyLawsJson,
                        EstimatedTimeMin = t.EstimatedTimeMin
                    },
                    cancellationToken: cancellationToken)).ConfigureAwait(false);
        }
    }
}
