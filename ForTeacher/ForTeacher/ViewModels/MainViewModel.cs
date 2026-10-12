using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ForTeacher.Models;
using ForTeacher.Services;
using Microsoft.Win32;

namespace ForTeacher.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly TeacherModuleContext _ctx;
    private readonly CollectionViewSource _tasksView = new();

    public MainViewModel(TeacherModuleContext ctx)
    {
        _ctx = ctx;
        AddForm = new AddTaskViewModel(ctx, OnTeacherTaskSaved);

        Sections =
        [
            new SectionItem("", "Все разделы"),
            new SectionItem("mechanics", "Механика"),
            new SectionItem("electrostatics", "Электростатика"),
            new SectionItem("optics", "Оптика"),
            new SectionItem("waves", "Колебания и волны"),
            new SectionItem("electricity", "Электричество"),
            new SectionItem("thermodynamics", "Термодинамика"),
        ];

        _tasksView.Source = Tasks;
        TasksView = (ListCollectionView)_tasksView.View;
        TasksView.Filter = FilterBySection;

        SelectedSection = Sections[0];

        RefreshTasks();
    }

    public AddTaskViewModel AddForm { get; }

    public ObservableCollection<TaskDefinition> Tasks { get; } = [];

    public ListCollectionView TasksView { get; }

    public ObservableCollection<SectionItem> Sections { get; }

    [ObservableProperty]
    private SectionItem? _selectedSection;

    [ObservableProperty]
    private TaskDefinition? _selectedTask;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsListPane))]
    [NotifyPropertyChangedFor(nameof(IsAddPane))]
    private TeacherWorkspacePane _currentPane = TeacherWorkspacePane.List;

    [ObservableProperty]
    private int _workspaceTabIndex;

    public bool IsListPane => CurrentPane == TeacherWorkspacePane.List;

    public bool IsAddPane => CurrentPane == TeacherWorkspacePane.Add;

    partial void OnCurrentPaneChanged(TeacherWorkspacePane value)
    {
        var idx = value == TeacherWorkspacePane.List ? 0 : 1;
        if (WorkspaceTabIndex != idx)
            WorkspaceTabIndex = idx;
    }

    partial void OnWorkspaceTabIndexChanged(int value)
    {
        var newPane = value == 0 ? TeacherWorkspacePane.List : TeacherWorkspacePane.Add;
        if (CurrentPane != newPane)
            CurrentPane = newPane;
    }

    private void OnTeacherTaskSaved()
    {
        RefreshTasks();
        CurrentPane = TeacherWorkspacePane.List;
    }

    partial void OnSelectedTaskChanged(TaskDefinition? value)
    {
        EditSelectedTaskCommand.NotifyCanExecuteChanged();
        ExportSelectedToPdfCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void GoToList() => CurrentPane = TeacherWorkspacePane.List;

    [RelayCommand]
    private void GoToAdd()
    {
        AddForm.PrepareForNewTask();
        CurrentPane = TeacherWorkspacePane.Add;
    }

    [RelayCommand(CanExecute = nameof(CanUseSelectedTask))]
    private void EditSelectedTask()
    {
        if (SelectedTask is null)
            return;

        AddForm.BeginEdit(SelectedTask);
        WorkspaceTabIndex = 1;
    }

    [RelayCommand(CanExecute = nameof(CanUseSelectedTask))]
    private async Task ExportSelectedToPdf()
    {
        if (SelectedTask is null)
            return;

        var dlg = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = TeacherTaskPdfExporter.SanitizeFileName(SelectedTask.TitleKey) + ".pdf",
            DefaultExt = ".pdf"
        };

        if (dlg.ShowDialog() != true)
            return;

        try
        {
            var path = dlg.FileName;
            var task = SelectedTask;
            await System.Threading.Tasks.Task.Run(() => TeacherTaskPdfExporter.ExportToFile(task!, path)).ConfigureAwait(true);
            MessageBox.Show(
                $"Файл сохранён:\n{path}",
                "Экспорт PDF",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка экспорта", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool CanUseSelectedTask() => SelectedTask is not null;

    partial void OnSelectedSectionChanged(SectionItem? value) => TasksView?.Refresh();

    [RelayCommand]
    private void RefreshTasks()
    {
        SelectedTask = null;
        Tasks.Clear();
        foreach (var t in _ctx.TaskRepository.LoadAll().Where(x => x.IsTeacherTask).OrderBy(x => x.Topic).ThenBy(x => x.TitleKey))
            Tasks.Add(t);

        TasksView?.Refresh();
    }

    private bool FilterBySection(object obj)
    {
        if (obj is not TaskDefinition t)
            return false;

        if (SelectedSection is null || string.IsNullOrEmpty(SelectedSection.TopicKey))
            return true;

        return string.Equals(t.Topic, SelectedSection.TopicKey, StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (SelectedTask is null)
            return;

        if (!_ctx.Confirmations.Confirm(
                $"Удалить задачу «{SelectedTask.TitleKey}» ({SelectedTask.Id})?",
                "Подтверждение удаления"))
            return;

        _ctx.TaskRepository.TryRemoveTeacherTaskById(SelectedTask.Id);
        var teacherOnly = _ctx.TaskRepository.LoadAll().Where(x => x.IsTeacherTask).ToList();
        await _ctx.DatabaseService.UpsertTeacherTasksAsync(teacherOnly).ConfigureAwait(true);

        RefreshTasks();
        SelectedTask = null;
    }
}
