namespace ForTeacher.Services;

public static class TeacherModuleFactory
{
    /// <summary>Пароль только на время процесса; значение не сохраняется на диск.</summary>
    public static TeacherModuleContext CreateDefault(string? tasksJsonPath = null, string? loginPassword = null)
    {
        var path = string.IsNullOrWhiteSpace(tasksJsonPath)
            ? AppPaths.TasksJsonPath
            : tasksJsonPath!;

        var repo = new TaskRepository(path);
        var pg = Environment.GetEnvironmentVariable("FOR_TEACHER_PG_CONNECTION");
        IDatabaseService db = string.IsNullOrWhiteSpace(pg)
            ? new NullDatabaseService()
            : new PostgreSqlDatabaseService(pg);

        IDifficultyAnalyzer analyzer = new DifficultyAnalyzerRouter();

        return new TeacherModuleContext(
            repo,
            new TaskWpfSetupCatalog(),
            db,
            new WindowsFileDialogService(),
            new WindowsConfirmationService(),
            analyzer,
            loginPassword ?? "1234");
    }
}
