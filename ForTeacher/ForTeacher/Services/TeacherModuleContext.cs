namespace ForTeacher.Services;

/// <summary>Зависимости модуля преподавателя для exe или встраивания UserControl.</summary>
public sealed class TeacherModuleContext
{
    public TeacherModuleContext(
        TaskRepository taskRepository,
        ITaskWpfSetupCatalog labCatalog,
        IDatabaseService databaseService,
        IFileDialogService fileDialogs,
        IConfirmationService confirmations,
        IDifficultyAnalyzer difficultyAnalyzer,
        string expectedLoginPassword)
    {
        TaskRepository = taskRepository;
        LabCatalog = labCatalog;
        DatabaseService = databaseService;
        FileDialogs = fileDialogs;
        Confirmations = confirmations;
        DifficultyAnalyzer = difficultyAnalyzer;
        ExpectedLoginPassword = expectedLoginPassword;
    }

    public TaskRepository TaskRepository { get; }
    public ITaskWpfSetupCatalog LabCatalog { get; }
    public IDatabaseService DatabaseService { get; }
    public IFileDialogService FileDialogs { get; }
    public IConfirmationService Confirmations { get; }
    public IDifficultyAnalyzer DifficultyAnalyzer { get; }
    public string ExpectedLoginPassword { get; }
}
