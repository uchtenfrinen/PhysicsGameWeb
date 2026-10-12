using System.Windows;
using System.Windows.Controls;
using ForTeacher.Services;
using ForTeacher.ViewModels;

namespace ForTeacher.Views;

/// <summary>
/// Встраиваемый модуль CRUD для преподавателя. По умолчанию без окна входа (хост сам решает доступ).
/// Событие <see cref="TasksFileChanged"/> дублирует <see cref="TaskRepository.TasksFileChanged"/>.
/// </summary>
public partial class TeacherTasksUserControl : UserControl
{
    private TeacherModuleContext? _context;

    public TeacherTasksUserControl()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyIfNeeded();
    }

    public static readonly DependencyProperty TasksJsonPathProperty = DependencyProperty.Register(
        nameof(TasksJsonPath),
        typeof(string),
        typeof(TeacherTasksUserControl),
        new PropertyMetadata(AppPaths.TasksJsonPath, OnTasksJsonPathChanged));

    private static void OnTasksJsonPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TeacherTasksUserControl c || !c.IsLoaded)
            return;

        if (e.NewValue is string path)
            c.Apply(path);
    }

    /// <summary>Полный путь к <c>tasks.json</c> (по умолчанию <see cref="AppPaths.TasksJsonPath"/>).</summary>
    public string TasksJsonPath
    {
        get => (string)GetValue(TasksJsonPathProperty);
        set => SetValue(TasksJsonPathProperty, value);
    }

    /// <summary>Вызывается после успешной перезаписи <c>tasks.json</c> репозиторием.</summary>
    public event EventHandler? TasksFileChanged;

    private void ApplyIfNeeded()
    {
        Apply(TasksJsonPath);
    }

    /// <summary>Переинициализация контекста (например, при смене пути из хоста).</summary>
    public void Apply(string tasksJsonFullPath)
    {
        if (_context is not null)
            _context.TaskRepository.TasksFileChanged -= OnRepositoryFileChanged;

        AppPaths.InitializeTasksJsonPath(tasksJsonFullPath);
        _context = TeacherModuleFactory.CreateDefault(AppPaths.TasksJsonPath);
        _context.TaskRepository.TasksFileChanged += OnRepositoryFileChanged;
        Workspace.DataContext = new MainViewModel(_context);
    }

    private void OnRepositoryFileChanged(object? sender, EventArgs e) =>
        TasksFileChanged?.Invoke(this, EventArgs.Empty);
}
