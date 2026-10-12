using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using PdfiumViewer;
using PhysicsTasks;
using PhysicsTasks.Labs;
using PhysicsTasks.Messaging;
using PhysicsTasks.Models;
using PhysicsTasks.Services;
using PhysicsTasks.Terminal.Helpers;

namespace PhysicsTasks.Terminal.ViewModels;

public sealed partial class TaskWorkspaceViewModel
    : ObservableObject,
      IRecipient<TaskTerminalTriggeredMessage>
{
    public sealed class ConditionFragment
    {
        public string Text { get; init; } = "";
        public bool IsFormula { get; init; }
    }

    private static readonly Regex InlineFormulaRegex = new(@"\$(.+?)\$", RegexOptions.Compiled | RegexOptions.Singleline);

    private readonly ITaskRepository _tasks;
    private readonly ISaveService _saveService;
    private readonly ILabNavigator _labNavigator;
    private readonly string _tasksCatalogPath;
    private SaveData _saveData = new();
    private PdfDocument? _pdfDocument;

    public TaskWorkspaceViewModel(
        ITaskRepository tasks,
        ISaveService saveService,
        ILabNavigator labNavigator,
        string tasksCatalogPath)
    {
        _tasks = tasks;
        _saveService = saveService;
        _labNavigator = labNavigator;
        _tasksCatalogPath = tasksCatalogPath;

        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public ObservableCollection<TaskListItemViewModel> Items { get; } = new();

    [ObservableProperty]
    private TaskListItemViewModel? _selectedItem;

    [ObservableProperty]
    private string _numericInputText = "";

    [ObservableProperty]
    private int _selectedChoiceIndex = -1;

    [ObservableProperty]
    private double _simulationValue;

    [ObservableProperty]
    private string _feedbackMessage = "";

    [ObservableProperty]
    private bool _isFeedbackSuccess;

    [ObservableProperty]
    private bool _isInitialized;

    [ObservableProperty]
    private bool _showNumericPanel;

    [ObservableProperty]
    private bool _showChoicePanel;

    [ObservableProperty]
    private bool _showSimulationPanel;

    [ObservableProperty]
    private int _pdfCurrentPage = 1;

    [ObservableProperty]
    private int _pdfPageCount;

    [ObservableProperty]
    private ImageSource? _pdfPageImage;

    /// <summary>Сообщение об ошибке открытия/отрисовки PDF (показывается в UI).</summary>
    [ObservableProperty]
    private string _pdfLoadError = "";

    [ObservableProperty]
    private FullscreenMediaKind _fullscreenMedia = FullscreenMediaKind.None;

    private const double MediaZoomMin = 0.5;
    private const double MediaZoomMax = 4.0;
    private const double MediaZoomStep = 1.2;

    [ObservableProperty]
    private double _previewPhotoZoom = 1.0;

    [ObservableProperty]
    private double _previewPhotoPanX;

    [ObservableProperty]
    private double _previewPhotoPanY;

    [ObservableProperty]
    private double _previewPdfZoom = 1.0;

    [ObservableProperty]
    private double _previewPdfPanX;

    [ObservableProperty]
    private double _previewPdfPanY;

    [ObservableProperty]
    private double _fullscreenZoom = 1.0;

    [ObservableProperty]
    private double _fullscreenPanX;

    [ObservableProperty]
    private double _fullscreenPanY;

    public bool IsFullscreenOpen => FullscreenMedia != FullscreenMediaKind.None;

    public bool IsFullscreenPhoto => FullscreenMedia == FullscreenMediaKind.Photo;

    public bool IsFullscreenPdf => FullscreenMedia == FullscreenMediaKind.Pdf;

    public TaskKind? CurrentKind => SelectedItem?.Definition.Kind;

    public string? ConditionMarkdown => SelectedItem?.Definition.ConditionMarkdown;

    public IReadOnlyList<ConditionFragment> ConditionFragments => BuildConditionFragments(ConditionMarkdown);

    public string? FormulaText => SelectedItem?.Definition.FormulaText;

    public bool HasFormula => !string.IsNullOrWhiteSpace(FormulaText);

    public bool HasLabSetup => !string.IsNullOrWhiteSpace(ResolveLabSetupId(SelectedItem?.Definition));

    public bool HasCondition => ConditionFragments.Count > 0;

    /// <summary>Абсолютный путь к файлу иллюстрации (для WPF <see cref="System.Windows.Controls.Image"/>).</summary>
    public string? TaskImageAbsolutePath
    {
        get
        {
            var rel = SelectedItem?.Definition.ImagePath?.Trim();
            if (string.IsNullOrEmpty(rel))
            {
                return null;
            }

            var full = TaskCatalogPathResolver.ResolveAssetFullPath(_tasksCatalogPath, rel);
            return !string.IsNullOrEmpty(full) && File.Exists(full) ? full : null;
        }
    }

    /// <summary>Абсолютный путь к PDF, если задан в каталоге и файл найден.</summary>
    public string? PdfAbsolutePath
    {
        get
        {
            var rel = SelectedItem?.Definition.PdfPath?.Trim();
            if (string.IsNullOrEmpty(rel))
                return null;

            var full = TaskCatalogPathResolver.ResolveAssetFullPath(_tasksCatalogPath, rel);
            return !string.IsNullOrEmpty(full) && File.Exists(full) ? full : null;
        }
    }

    public bool HasTaskImage => TaskImageAbsolutePath is not null;

    /// <summary>В каталоге указан путь к PDF (независимо от того, найден ли файл).</summary>
    public bool ExpectsPdf => !string.IsNullOrWhiteSpace(SelectedItem?.Definition.PdfPath?.Trim());

    public bool PdfFileMissing => ExpectsPdf && PdfAbsolutePath is null;

    /// <summary>Показать строку с фото и/или PDF под условием задачи.</summary>
    public bool ShowMediaRow => HasTaskImage || ExpectsPdf;

    /// <summary>Промежуток между колонками только если видны и картинка, и блок PDF.</summary>
    public bool ShowMediaGap => HasTaskImage && ExpectsPdf;

    public bool HasPdf => _pdfDocument is not null && PdfPageCount > 0;

    public string PdfPageLabel => HasPdf ? $"{PdfCurrentPage} / {PdfPageCount}" : "";

    public bool HasFeedback => !string.IsNullOrWhiteSpace(FeedbackMessage);

    public int CurrentDifficulty => Math.Clamp(SelectedItem?.Definition.Difficulty ?? 0, 0, 5);

    public bool HasDifficulty => CurrentDifficulty > 0;

    public IReadOnlyList<string>? ChoiceOptions => SelectedItem?.Definition.Options;

    public double SimulationMinimum => SelectedItem?.Definition.SimulationMin ?? 0;

    public double SimulationMaximum => SelectedItem?.Definition.SimulationMax ?? 1;

    /// <param name="topicIdFilter">Если задано (из MonoGame), в списке только задачи с этим <see cref="TaskDefinition.TopicId"/>.</param>
    /// <param name="labSetupFilter">Если задано, в списке только задачи конкретной подтемы/установки (inclined_plane и т.д.).</param>
    public async Task InitializeAsync(string? topicIdFilter = null, string? labSetupFilter = null, CancellationToken cancellationToken = default)
    {
        _saveData = _saveService.Load();
        var allDefs = await _tasks.GetAllAsync(cancellationToken).ConfigureAwait(true);
        var topic = topicIdFilter?.Trim();
        var setup = labSetupFilter?.Trim();
        var defs = string.IsNullOrEmpty(topic)
            ? allDefs
            : allDefs.Where(d => string.Equals(d.TopicId, topic, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrEmpty(setup))
        {
            defs = defs.Where(d => string.Equals(ResolveLabSetupId(d), setup, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        Items.Clear();
        foreach (var d in defs.OrderBy(x => x.Title, StringComparer.CurrentCultureIgnoreCase))
        {
            var completed = _saveData.CompletedTaskIds.Contains(d.Id);
            Items.Add(new TaskListItemViewModel(d, completed));
        }

        IsInitialized = true;
        OnPropertyChanged(nameof(ConditionMarkdown));
        OnPropertyChanged(nameof(ConditionFragments));
        OnPropertyChanged(nameof(FormulaText));
        OnPropertyChanged(nameof(HasFormula));
        OnPropertyChanged(nameof(HasLabSetup));
        OnPropertyChanged(nameof(HasCondition));
        OnPropertyChanged(nameof(TaskImageAbsolutePath));
        OnPropertyChanged(nameof(HasTaskImage));
        NotifyMediaLayoutProperties();
        OnPropertyChanged(nameof(ChoiceOptions));
        OnPropertyChanged(nameof(SimulationMinimum));
        OnPropertyChanged(nameof(SimulationMaximum));
        OnPropertyChanged(nameof(CurrentKind));
        OnPropertyChanged(nameof(CurrentDifficulty));
        OnPropertyChanged(nameof(HasDifficulty));

        SelectedItem = Items.Count > 0 ? Items[0] : null;
        if (Items.Count == 0)
            ReloadPdfDocument();
    }

    partial void OnSelectedItemChanged(TaskListItemViewModel? value)
    {
        FeedbackMessage = "";
        ClearAnswerInputs();
        ApplyTaskKindLayout(value);

        OpenLabCommand.NotifyCanExecuteChanged();
        CheckAnswerCommand.NotifyCanExecuteChanged();

        OnPropertyChanged(nameof(ConditionMarkdown));
        OnPropertyChanged(nameof(ConditionFragments));
        OnPropertyChanged(nameof(FormulaText));
        OnPropertyChanged(nameof(HasFormula));
        OnPropertyChanged(nameof(HasLabSetup));
        OnPropertyChanged(nameof(HasCondition));
        OnPropertyChanged(nameof(TaskImageAbsolutePath));
        OnPropertyChanged(nameof(HasTaskImage));
        OnPropertyChanged(nameof(PdfAbsolutePath));
        NotifyMediaLayoutProperties();
        ReloadPdfDocument();
        OnPropertyChanged(nameof(ChoiceOptions));
        OnPropertyChanged(nameof(SimulationMinimum));
        OnPropertyChanged(nameof(SimulationMaximum));
        OnPropertyChanged(nameof(CurrentKind));
        OnPropertyChanged(nameof(CurrentDifficulty));
        OnPropertyChanged(nameof(HasDifficulty));
        FullscreenMedia = FullscreenMediaKind.None;
        NotifyFullscreenProperties();
        RefreshFullscreenCommands();
        ResetPreviewMediaState();
    }

    partial void OnFeedbackMessageChanged(string value) => OnPropertyChanged(nameof(HasFeedback));

    partial void OnFullscreenMediaChanged(FullscreenMediaKind value)
    {
        NotifyFullscreenProperties();
        if (value is FullscreenMediaKind.Photo or FullscreenMediaKind.Pdf)
            ResetFullscreenMediaState();

        if (HasPdf && _pdfDocument is not null && (value == FullscreenMediaKind.Pdf || value == FullscreenMediaKind.None))
            RenderCurrentPdfPage();
    }

    partial void OnPreviewPhotoZoomChanged(double value) => RefreshMediaZoomCommands();

    partial void OnPreviewPdfZoomChanged(double value) => RefreshMediaZoomCommands();

    partial void OnFullscreenZoomChanged(double value) => RefreshMediaZoomCommands();

    private void ResetPreviewMediaState()
    {
        PreviewPhotoZoom = 1.0;
        PreviewPhotoPanX = 0;
        PreviewPhotoPanY = 0;
        PreviewPdfZoom = 1.0;
        PreviewPdfPanX = 0;
        PreviewPdfPanY = 0;
        RefreshMediaZoomCommands();
    }

    private void ResetFullscreenMediaState()
    {
        FullscreenZoom = 1.0;
        FullscreenPanX = 0;
        FullscreenPanY = 0;
        RefreshMediaZoomCommands();
    }

    private void ResetPdfPanState()
    {
        PreviewPdfPanX = 0;
        PreviewPdfPanY = 0;
        FullscreenPanX = 0;
        FullscreenPanY = 0;
    }

    private void RefreshMediaZoomCommands()
    {
        ZoomPreviewPhotoInCommand.NotifyCanExecuteChanged();
        ZoomPreviewPhotoOutCommand.NotifyCanExecuteChanged();
        ZoomPreviewPdfInCommand.NotifyCanExecuteChanged();
        ZoomPreviewPdfOutCommand.NotifyCanExecuteChanged();
        ZoomFullscreenInCommand.NotifyCanExecuteChanged();
        ZoomFullscreenOutCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanZoomPreviewPhotoIn))]
    private void ZoomPreviewPhotoIn()
    {
        PreviewPhotoZoom = Math.Min(MediaZoomMax, PreviewPhotoZoom * MediaZoomStep);
    }

    private bool CanZoomPreviewPhotoIn() => PreviewPhotoZoom < MediaZoomMax - 1e-9;

    [RelayCommand(CanExecute = nameof(CanZoomPreviewPhotoOut))]
    private void ZoomPreviewPhotoOut()
    {
        PreviewPhotoZoom = Math.Max(MediaZoomMin, PreviewPhotoZoom / MediaZoomStep);
        if (PreviewPhotoZoom <= 1.003)
        {
            PreviewPhotoPanX = 0;
            PreviewPhotoPanY = 0;
        }
    }

    private bool CanZoomPreviewPhotoOut() => PreviewPhotoZoom > MediaZoomMin + 1e-9;

    [RelayCommand]
    private void ResetPreviewPhotoView()
    {
        PreviewPhotoZoom = 1.0;
        PreviewPhotoPanX = 0;
        PreviewPhotoPanY = 0;
    }

    [RelayCommand(CanExecute = nameof(CanZoomPreviewPdfIn))]
    private void ZoomPreviewPdfIn()
    {
        PreviewPdfZoom = Math.Min(MediaZoomMax, PreviewPdfZoom * MediaZoomStep);
    }

    private bool CanZoomPreviewPdfIn() => PreviewPdfZoom < MediaZoomMax - 1e-9;

    [RelayCommand(CanExecute = nameof(CanZoomPreviewPdfOut))]
    private void ZoomPreviewPdfOut()
    {
        PreviewPdfZoom = Math.Max(MediaZoomMin, PreviewPdfZoom / MediaZoomStep);
        if (PreviewPdfZoom <= 1.003)
        {
            PreviewPdfPanX = 0;
            PreviewPdfPanY = 0;
        }
    }

    private bool CanZoomPreviewPdfOut() => PreviewPdfZoom > MediaZoomMin + 1e-9;

    [RelayCommand]
    private void ResetPreviewPdfView()
    {
        PreviewPdfZoom = 1.0;
        PreviewPdfPanX = 0;
        PreviewPdfPanY = 0;
    }

    [RelayCommand(CanExecute = nameof(CanZoomFullscreenIn))]
    private void ZoomFullscreenIn()
    {
        FullscreenZoom = Math.Min(MediaZoomMax, FullscreenZoom * MediaZoomStep);
    }

    private bool CanZoomFullscreenIn() => FullscreenZoom < MediaZoomMax - 1e-9;

    [RelayCommand(CanExecute = nameof(CanZoomFullscreenOut))]
    private void ZoomFullscreenOut()
    {
        FullscreenZoom = Math.Max(MediaZoomMin, FullscreenZoom / MediaZoomStep);
        if (FullscreenZoom <= 1.003)
        {
            FullscreenPanX = 0;
            FullscreenPanY = 0;
        }
    }

    private bool CanZoomFullscreenOut() => FullscreenZoom > MediaZoomMin + 1e-9;

    [RelayCommand]
    private void ResetFullscreenView()
    {
        FullscreenZoom = 1.0;
        FullscreenPanX = 0;
        FullscreenPanY = 0;
    }

    private void NotifyFullscreenProperties()
    {
        OnPropertyChanged(nameof(IsFullscreenOpen));
        OnPropertyChanged(nameof(IsFullscreenPhoto));
        OnPropertyChanged(nameof(IsFullscreenPdf));
    }

    private void RefreshFullscreenCommands()
    {
        OpenPhotoFullscreenCommand.NotifyCanExecuteChanged();
        OpenPdfFullscreenCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanOpenPhotoFullscreen))]
    private void OpenPhotoFullscreen() => FullscreenMedia = FullscreenMediaKind.Photo;

    private bool CanOpenPhotoFullscreen() => HasTaskImage;

    [RelayCommand(CanExecute = nameof(CanOpenPdfFullscreen))]
    private void OpenPdfFullscreen() => FullscreenMedia = FullscreenMediaKind.Pdf;

    private bool CanOpenPdfFullscreen() => HasPdf;

    [RelayCommand]
    private void CloseFullscreenMedia() => FullscreenMedia = FullscreenMediaKind.None;

    partial void OnPdfCurrentPageChanged(int value)
    {
        if (_pdfDocument is null || PdfPageCount < 1)
            return;

        ResetPdfPanState();
        RenderCurrentPdfPage();
        OnPropertyChanged(nameof(PdfPageLabel));
        RefreshPdfNavigationCommands();
    }

    partial void OnPdfPageCountChanged(int value)
    {
        OnPropertyChanged(nameof(HasPdf));
        OnPropertyChanged(nameof(PdfPageLabel));
        NotifyMediaLayoutProperties();
        RefreshPdfNavigationCommands();
        RefreshFullscreenCommands();
    }

    partial void OnPdfLoadErrorChanged(string value)
    {
        OnPropertyChanged(nameof(HasPdfError));
        NotifyMediaLayoutProperties();
    }

    public bool HasPdfError => !string.IsNullOrWhiteSpace(PdfLoadError);

    private void NotifyMediaLayoutProperties()
    {
        OnPropertyChanged(nameof(ExpectsPdf));
        OnPropertyChanged(nameof(PdfFileMissing));
        OnPropertyChanged(nameof(ShowMediaRow));
        OnPropertyChanged(nameof(ShowMediaGap));
    }

    private void ClearAnswerInputs()
    {
        NumericInputText = "";
        SelectedChoiceIndex = -1;
    }

    private void ApplyTaskKindLayout(TaskListItemViewModel? value)
    {
        if (value?.Definition is null)
        {
            SimulationValue = 0;
            ShowNumericPanel = false;
            ShowChoicePanel = false;
            ShowSimulationPanel = false;
            return;
        }

        ShowNumericPanel = value.Definition.Kind == TaskKind.Numeric;
        ShowChoicePanel = value.Definition.Kind == TaskKind.Choice;
        ShowSimulationPanel = value.Definition.Kind == TaskKind.Simulation;

        if (value.Definition.Kind == TaskKind.Simulation)
        {
            var min = value.Definition.SimulationMin ?? 0;
            var max = value.Definition.SimulationMax ?? 1;
            SimulationValue = value.Definition.CorrectSimulationValue is { } target
                ? Math.Clamp(target, min, max)
                : min + (max - min) * 0.5;
        }
        else
        {
            SimulationValue = 0;
        }
    }

    [RelayCommand]
    private void RestartCurrentTask()
    {
        FeedbackMessage = "";
        ClearAnswerInputs();
        ApplyTaskKindLayout(SelectedItem);
        if (_pdfDocument is not null && PdfPageCount > 0)
        {
            PdfCurrentPage = 1;
            RenderCurrentPdfPage();
            OnPropertyChanged(nameof(PdfPageLabel));
            RefreshPdfNavigationCommands();
        }

        OpenLabCommand.NotifyCanExecuteChanged();
        CheckAnswerCommand.NotifyCanExecuteChanged();
        FullscreenMedia = FullscreenMediaKind.None;
        NotifyFullscreenProperties();
        RefreshFullscreenCommands();
        ResetPreviewMediaState();
    }

    [RelayCommand]
    private void CheckTheory()
    {
        FeedbackMessage = "Проверка теории будет связана с модулем курса.";
        IsFeedbackSuccess = false;
    }

    [RelayCommand]
    private void ViewTheory()
    {
        FeedbackMessage = "Просмотр теории: подключите контент курса или WebView.";
        IsFeedbackSuccess = false;
    }

    public void OpenTask(string taskId, string? topicId = null)
    {
        var match = Items.FirstOrDefault(i => string.Equals(i.Definition.Id, taskId, StringComparison.Ordinal));
        if (match is not null)
        {
            SelectedItem = match;
            return;
        }

        FeedbackMessage = $"Задача «{taskId}» не найдена в каталоге.";
        IsFeedbackSuccess = false;

        if (!string.IsNullOrWhiteSpace(topicId))
            _ = topicId;
    }

    [RelayCommand(CanExecute = nameof(CanCheck))]
    private void CheckAnswer()
    {
        if (SelectedItem is null)
            return;

        var def = SelectedItem.Definition;
        var state = new TaskRuntimeState();

        switch (def.Kind)
        {
            case TaskKind.Numeric:
                if (!double.TryParse(NumericInputText, NumberStyles.Float, CultureInfo.CurrentCulture, out var n)
                    && !double.TryParse(NumericInputText, NumberStyles.Float, CultureInfo.InvariantCulture, out n))
                {
                    FeedbackMessage = "Введите корректное число.";
                    IsFeedbackSuccess = false;
                    return;
                }

                state.NumericAnswer = n;
                break;

            case TaskKind.Choice:
                state.SelectedChoiceIndex = SelectedChoiceIndex < 0 ? null : SelectedChoiceIndex;
                break;

            case TaskKind.Simulation:
                state.SimulationValue = SimulationValue;
                break;
        }

        var physics = new PhysicsTask(def);
        if (!physics.Validate(state, out var error))
        {
            FeedbackMessage = error ?? "Ошибка проверки.";
            IsFeedbackSuccess = false;
            return;
        }

        FeedbackMessage = "Верно! Задача засчитана.";
        IsFeedbackSuccess = true;

        _saveData.CompletedTaskIds.Add(def.Id);
        _saveService.Save(_saveData);
        SelectedItem.RefreshCompletion(true);
    }

    private bool CanCheck() => SelectedItem is not null;

    [RelayCommand(CanExecute = nameof(CanOpenLab))]
    private void OpenLab()
    {
        var id = ResolveLabSetupId(SelectedItem?.Definition);
        if (_labNavigator.TryOpenLab(id))
        {
            FeedbackMessage = "Лаборатория открыта.";
            IsFeedbackSuccess = true;
            return;
        }

        FeedbackMessage = "Лаборатория не подключена: реализуйте ILabNavigator в хосте или подключите пакет страниц.";
        IsFeedbackSuccess = false;
    }

    private bool CanOpenLab() => HasLabSetup;

    private static string? ResolveLabSetupId(TaskDefinition? definition)
    {
        if (definition is null)
            return null;

        var explicitId = definition.LabWpfSetupId?.Trim();
        if (!string.IsNullOrWhiteSpace(explicitId)
            && !string.Equals(explicitId, "none", StringComparison.OrdinalIgnoreCase))
        {
            return explicitId;
        }

        var topic = definition.TopicId?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(topic))
            return null;

        return topic switch
        {
            "mechanics" => definition.Kind switch
            {
                TaskKind.Numeric => "inclined_plane",
                TaskKind.Choice => "inclined_plane",
                TaskKind.Simulation => "spring",
                _ => "inclined_plane",
            },
            "electricity" => "ohm_law",
            "electrostatics" => "charge_in_field",
            "optics" => definition.Kind switch
            {
                TaskKind.Choice => "refraction_at_boundary",
                _ => "thin_lens",
            },
            "waves" => "harmonic_oscillations",
            "thermodynamics" or "heat" => "ideal_gas",
            "hydro" => definition.Kind switch
            {
                TaskKind.Choice => "communicating_vessels",
                _ => "buoyancy",
            },
            _ => null,
        };
    }

    private void ReloadPdfDocument()
    {
        _pdfDocument?.Dispose();
        _pdfDocument = null;
        PdfPageCount = 0;
        PdfCurrentPage = 1;
        PdfPageImage = null;
        PdfLoadError = "";

        OnPropertyChanged(nameof(PdfAbsolutePath));

        var expects = ExpectsPdf;
        var path = PdfAbsolutePath;

        if (!expects)
        {
            OnPropertyChanged(nameof(HasPdf));
            OnPropertyChanged(nameof(PdfPageLabel));
            NotifyMediaLayoutProperties();
            RefreshPdfNavigationCommands();
            RefreshFullscreenCommands();
            return;
        }

        if (string.IsNullOrEmpty(path))
        {
            PdfLoadError = "Файл PDF не найден. Убедитесь, что методичка лежит в папке Data рядом с tasks.json (например Data/TeacherPdfs/…).";
            OnPropertyChanged(nameof(HasPdf));
            OnPropertyChanged(nameof(PdfPageLabel));
            NotifyMediaLayoutProperties();
            RefreshPdfNavigationCommands();
            RefreshFullscreenCommands();
            return;
        }

        try
        {
            var doc = PdfDocument.Load(path);
            _pdfDocument = doc;
            PdfPageCount = doc.PageCount;
            if (PdfPageCount < 1)
            {
                _pdfDocument.Dispose();
                _pdfDocument = null;
                PdfPageCount = 0;
                PdfLoadError = "В PDF нет страниц.";
                OnPropertyChanged(nameof(HasPdf));
                OnPropertyChanged(nameof(PdfPageLabel));
                NotifyMediaLayoutProperties();
                RefreshPdfNavigationCommands();
                RefreshFullscreenCommands();
                return;
            }

            PdfCurrentPage = 1;
            RenderCurrentPdfPage();
        }
        catch (Exception ex)
        {
            _pdfDocument?.Dispose();
            _pdfDocument = null;
            PdfPageCount = 0;
            PdfPageImage = null;
            PdfLoadError = ex.Message.Length > 0 ? ex.Message : "Не удалось открыть PDF.";
        }

        OnPropertyChanged(nameof(HasPdf));
        OnPropertyChanged(nameof(PdfPageLabel));
        NotifyMediaLayoutProperties();
        RefreshPdfNavigationCommands();
        RefreshFullscreenCommands();
    }

    private void RenderCurrentPdfPage()
    {
        if (_pdfDocument is null || PdfPageCount < 1)
        {
            PdfPageImage = null;
            return;
        }

        var pageIdx = Math.Clamp(PdfCurrentPage - 1, 0, PdfPageCount - 1);
        var sz = _pdfDocument.PageSizes[pageIdx];
        var maxW = FullscreenMedia == FullscreenMediaKind.Pdf ? 1280.0 : 520.0;
        var w = (int)Math.Max(1, Math.Round(maxW));
        var h = (int)Math.Max(1, Math.Round(sz.Height * (maxW / Math.Max((double)sz.Width, 1e-6))));

        try
        {
            using var img = _pdfDocument.Render(pageIdx, w, h, 96, 96, false);
            PdfPageImage = BitmapSourceHelper.FromDrawingImage(img);
            PdfLoadError = "";
        }
        catch (Exception ex)
        {
            PdfPageImage = null;
            PdfLoadError = ex.Message.Length > 0 ? ex.Message : "Ошибка отрисовки страницы PDF.";
        }
    }

    private void RefreshPdfNavigationCommands()
    {
        PdfPrevPageCommand.NotifyCanExecuteChanged();
        PdfNextPageCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanPdfPrev))]
    private void PdfPrevPage()
    {
        if (PdfCurrentPage > 1)
            PdfCurrentPage--;
    }

    private bool CanPdfPrev() => HasPdf && PdfCurrentPage > 1;

    [RelayCommand(CanExecute = nameof(CanPdfNext))]
    private void PdfNextPage()
    {
        if (PdfCurrentPage < PdfPageCount)
            PdfCurrentPage++;
    }

    private bool CanPdfNext() => HasPdf && PdfCurrentPage < PdfPageCount;

    public void Receive(TaskTerminalTriggeredMessage message)
    {
        var payload = message.Value;
        Application.Current?.Dispatcher.Invoke(() => OpenTask(payload.TaskId, payload.TopicId));
    }

    private static IReadOnlyList<ConditionFragment> BuildConditionFragments(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<ConditionFragment>();

        var raw = text!;
        var list = new List<ConditionFragment>();
        var matches = InlineFormulaRegex.Matches(raw);
        var start = 0;
        foreach (Match m in matches)
        {
            if (m.Index > start)
            {
                var plain = raw[start..m.Index];
                if (!string.IsNullOrEmpty(plain))
                    list.Add(new ConditionFragment { Text = plain, IsFormula = false });
            }

            var formula = m.Groups[1].Value.Trim();
            if (formula.Length > 0)
                list.Add(new ConditionFragment { Text = formula, IsFormula = true });

            start = m.Index + m.Length;
        }

        if (start < raw.Length)
        {
            var tail = raw[start..];
            if (!string.IsNullOrEmpty(tail))
                list.Add(new ConditionFragment { Text = tail, IsFormula = false });
        }

        if (list.Count == 0 && raw.Length > 0)
            list.Add(new ConditionFragment { Text = raw, IsFormula = false });

        return list;
    }
}
