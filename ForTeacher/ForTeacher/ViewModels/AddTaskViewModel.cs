using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ForTeacher.Models;
using ForTeacher.Services;

namespace ForTeacher.ViewModels;

public sealed partial class AddTaskViewModel : ObservableObject, INotifyDataErrorInfo
{
    private readonly TeacherModuleContext _ctx;
    private readonly Action _onTaskAdded;
    private readonly Dictionary<string, List<string>> _errors = [];
    private string _analyzeFingerprint = string.Empty;

    private string? _editingTaskId;

    private string? _editingOriginalImageRelative;

    private string? _editingOriginalPdfRelative;

    public AddTaskViewModel(TeacherModuleContext ctx, Action onTaskAdded)
    {
        _ctx = ctx;
        _onTaskAdded = onTaskAdded;

        TopicChoices = new ObservableCollection<SectionItem>(
        [
            new SectionItem("mechanics", "Механика"),
            new SectionItem("electrostatics", "Электростатика"),
            new SectionItem("optics", "Оптика"),
            new SectionItem("waves", "Колебания и волны"),
            new SectionItem("electricity", "Электричество"),
            new SectionItem("thermodynamics", "Термодинамика"),
        ]);

        ReloadLabs();
    }

    /// <summary>Сброс формы перед созданием новой задачи (выход из режима редактирования).</summary>
    public void PrepareForNewTask() => ResetForm();

    /// <summary>Заполняет форму данными задачи для сохранения под тем же id.</summary>
    public void BeginEdit(TaskDefinition t)
    {
        ClearAllErrors();
        _editingTaskId = t.Id;
        _editingOriginalImageRelative = string.IsNullOrWhiteSpace(t.ImagePath) ? null : t.ImagePath.Trim();
        _editingOriginalPdfRelative = string.IsNullOrWhiteSpace(t.PdfPath) ? null : t.PdfPath.Trim();

        Title = t.TitleKey;
        Text = t.DescriptionKey;
        NumericAnswerText = t.ExpectedNumeric?.ToString(CultureInfo.InvariantCulture) ?? "";
        SelectedTopicKey = string.IsNullOrWhiteSpace(t.Topic) ? "mechanics" : t.Topic;

        ReloadLabs();
        var labId = t.LabWpfSetupId ?? "";
        SelectedLab = LabOptions.FirstOrDefault(o => o.SetupId == labId) ?? LabOptions[0];

        PendingImagePath = "";
        if (!string.IsNullOrWhiteSpace(_editingOriginalImageRelative))
        {
            var full = AppPaths.ResolveGameRelativePath(_editingOriginalImageRelative);
            if (File.Exists(full))
                PendingImagePath = full;
        }

        PendingPdfPath = "";
        if (!string.IsNullOrWhiteSpace(_editingOriginalPdfRelative))
        {
            var fullP = AppPaths.ResolveGameRelativePath(_editingOriginalPdfRelative);
            if (File.Exists(fullP))
                PendingPdfPath = fullP;
        }

        Difficulty = t.Difficulty;
        AiReasoning = t.AiReasoning?.Trim() ?? "";
        _keyLawsJson = string.IsNullOrWhiteSpace(t.KeyLawsJson) ? "[]" : t.KeyLawsJson.Trim();
        EstimatedTimeMin = t.EstimatedTimeMin;
        DifficultyAnalysisError = "";
        _analyzeFingerprint = ComputeFingerprint();
        OnPropertyChanged(nameof(EstimatedTimeHint));
    }

    public ObservableCollection<SectionItem> TopicChoices { get; }

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public bool HasErrors => _errors.Count > 0;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (propertyName is null || !_errors.TryGetValue(propertyName, out var list))
            return Array.Empty<string>();

        return list;
    }

    [ObservableProperty]
    private string _title = "";

    [ObservableProperty]
    private string _text = "";

    [ObservableProperty]
    private string _numericAnswerText = "";

    [ObservableProperty]
    private string _selectedTopicKey = "mechanics";

    [ObservableProperty]
    private LabWpfSetupOption? _selectedLab;

    [ObservableProperty]
    private string _pendingImagePath = "";

    [ObservableProperty]
    private string _pendingPdfPath = "";

    /// <summary>Имя файла методички для подписи (без полного пути).</summary>
    public string PendingPdfFileName =>
        string.IsNullOrWhiteSpace(PendingPdfPath) ? "" : Path.GetFileName(PendingPdfPath.Trim());

    /// <summary>1–5 звёзд; 0 — ещё не оценено.</summary>
    [ObservableProperty]
    private int _difficulty;

    [ObservableProperty]
    private string _aiReasoning = "";

    [ObservableProperty]
    private int? _estimatedTimeMin;

    [ObservableProperty]
    private bool _isDifficultyAnalyzing;

    [ObservableProperty]
    private string _difficultyAnalysisError = "";

    public ObservableCollection<LabWpfSetupOption> LabOptions { get; } = [];

    public string EstimatedTimeHint =>
        EstimatedTimeMin is > 0 and var m ? $"Оценка времени: ~{m} мин (ИИ)" : string.Empty;

    partial void OnEstimatedTimeMinChanged(int? value) => OnPropertyChanged(nameof(EstimatedTimeHint));

    partial void OnPendingPdfPathChanged(string value) => OnPropertyChanged(nameof(PendingPdfFileName));

    partial void OnSelectedTopicKeyChanged(string value) => ReloadLabs();

    private void ReloadLabs()
    {
        LabOptions.Clear();
        LabOptions.Add(new LabWpfSetupOption("", "Авто (по разделу)"));
        LabOptions.Add(new LabWpfSetupOption("none", "Без лаборатории WPF"));
        foreach (var o in _ctx.LabCatalog.GetTeacherLabSetupsForTopic(SelectedTopicKey))
            LabOptions.Add(o);

        SelectedLab = LabOptions.FirstOrDefault();
    }

    [RelayCommand]
    private void PickImage()
    {
        var path = _ctx.FileDialogs.PickImageFile();
        if (!string.IsNullOrWhiteSpace(path))
            PendingImagePath = path;
    }

    [RelayCommand]
    private void ClearImage() => PendingImagePath = "";

    [RelayCommand]
    private void PickPdf()
    {
        var path = _ctx.FileDialogs.PickPdfFile();
        if (!string.IsNullOrWhiteSpace(path))
            PendingPdfPath = path;
    }

    [RelayCommand]
    private void ClearPdf() => PendingPdfPath = "";

    [RelayCommand]
    private void ClearForm() => ResetForm();

    /// <summary>Потеря фокуса полем текста — анализ без принудительного сброса кэша.</summary>
    public Task AnalyzeOnTextLostFocusAsync() => RunAnalyzeAsync(force: false);

    [RelayCommand]
    private Task AnalyzeFromButtonAsync() => RunAnalyzeAsync(force: true);

    private async Task RunAnalyzeAsync(bool force)
    {
        DifficultyAnalysisError = string.Empty;
        if (string.IsNullOrWhiteSpace(Text?.Trim()))
        {
            DifficultyAnalysisError = "Введите текст задачи для анализа.";
            return;
        }

        var fp = ComputeFingerprint();
        if (!force && fp == _analyzeFingerprint && Difficulty > 0)
        {
            return;
        }

        IsDifficultyAnalyzing = true;
        try
        {
            byte[]? imgBytes = null;
            string? media = null;
            if (!string.IsNullOrWhiteSpace(PendingImagePath) && File.Exists(PendingImagePath.Trim()))
            {
                try
                {
                    imgBytes = await File.ReadAllBytesAsync(PendingImagePath.Trim()).ConfigureAwait(true);
                    media = GuessImageMediaType(PendingImagePath.Trim());
                }
                catch
                {
                    imgBytes = null;
                }
            }

            var result = await _ctx.DifficultyAnalyzer
                .AnalyzeAsync(Title.Trim(), Text.Trim(), imgBytes, media)
                .ConfigureAwait(true);

            if (result is null)
            {
                var hint = (_ctx.DifficultyAnalyzer as IDifficultyAnalyzerDiagnostics)?.LastFailureDetail;
                DifficultyAnalysisError = string.IsNullOrWhiteSpace(hint)
                    ? "Не удалось получить оценку. Укажите сложность вручную по звёздам."
                    : hint + " При необходимости укажите сложность вручную по звёздам.";
                return;
            }

            ApplyAiDifficulty(result);
            _analyzeFingerprint = fp;
        }
        catch (Exception ex)
        {
            DifficultyAnalysisError = $"Ошибка анализа: {ex.Message}";
        }
        finally
        {
            IsDifficultyAnalyzing = false;
        }
    }

    private string _keyLawsJson = "";

    private void ApplyAiDifficulty(DifficultyAnalysisResult r)
    {
        Difficulty = Math.Clamp(r.Difficulty, 1, 5);
        AiReasoning = r.Reasoning.Trim();
        EstimatedTimeMin = r.EstimatedTimeMin > 0 ? r.EstimatedTimeMin : null;
        _keyLawsJson = r.KeyLaws.Count == 0 ? "[]" : JsonSerializer.Serialize(r.KeyLaws);
        OnPropertyChanged(nameof(EstimatedTimeHint));
    }

    private string ComputeFingerprint()
    {
        var raw = $"{Title}\u001f{Text}\u001f{PendingImagePath}\u001f{PendingPdfPath}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }

    private static string GuessImageMediaType(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "image/png"
        };
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidateAll();
        if (HasErrors)
            return;

        if (_editingTaskId is not null)
        {
            await SaveEditAsync().ConfigureAwait(true);
            return;
        }

        var ts = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
        var id = $"{SelectedTopicKey}_teacher_{ts}";

        string imageRelative = string.Empty;
        if (!string.IsNullOrWhiteSpace(PendingImagePath))
        {
            Directory.CreateDirectory(AppPaths.TeacherImagesDirectory);
            var ext = Path.GetExtension(PendingImagePath);
            if (string.IsNullOrEmpty(ext))
                ext = ".bin";

            var fileName = $"{SelectedTopicKey}_teacher_{ts}_img{ext}";
            var destFull = Path.Combine(AppPaths.TeacherImagesDirectory, fileName);
            File.Copy(PendingImagePath.Trim(), destFull, overwrite: true);
            imageRelative = Path.Combine("Data", "TeacherImages", fileName).Replace('\\', '/');
        }

        string pdfRelative = string.Empty;
        if (!string.IsNullOrWhiteSpace(PendingPdfPath))
        {
            Directory.CreateDirectory(AppPaths.TeacherPdfsDirectory);
            var ext = Path.GetExtension(PendingPdfPath);
            if (!string.Equals(ext, ".pdf", StringComparison.OrdinalIgnoreCase))
                ext = ".pdf";

            var pdfName = $"{SelectedTopicKey}_teacher_{ts}_method{ext}";
            var pdfDest = Path.Combine(AppPaths.TeacherPdfsDirectory, pdfName);
            File.Copy(PendingPdfPath.Trim(), pdfDest, overwrite: true);
            pdfRelative = Path.Combine("Data", "TeacherPdfs", pdfName).Replace('\\', '/');
        }

        var diff = Difficulty <= 0 ? 0 : Math.Clamp(Difficulty, 1, 5);
        var task = new TaskDefinition
        {
            Id = id,
            Topic = SelectedTopicKey,
            Type = "numeric",
            TitleKey = Title.Trim(),
            DescriptionKey = Text.Trim(),
            FormulaKey = string.Empty,
            ImagePath = imageRelative,
            PdfPath = pdfRelative,
            LabWpfSetupId = SelectedLab?.SetupId ?? string.Empty,
            ExpectedNumeric = double.Parse(NumericAnswerText.Trim(), CultureInfo.InvariantCulture),
            NumericTolerance = 0.01,
            Difficulty = diff,
            AiReasoning = AiReasoning.Trim(),
            KeyLawsJson = string.IsNullOrWhiteSpace(_keyLawsJson) ? "[]" : _keyLawsJson,
            EstimatedTimeMin = EstimatedTimeMin
        };

        _ctx.TaskRepository.Add(task);
        var teacherOnly = _ctx.TaskRepository.LoadAll().Where(x => x.IsTeacherTask).ToList();
        await _ctx.DatabaseService.UpsertTeacherTasksAsync(teacherOnly).ConfigureAwait(true);

        ResetForm();
        _onTaskAdded();
        System.Windows.MessageBox.Show("Задача добавлена; файл tasks.json игры обновлён.", "Готово", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    private async Task SaveEditAsync()
    {
        var id = _editingTaskId!;
        var imageRelative = BuildImageRelativeForSave(id);
        var pdfRelative = BuildPdfRelativeForSave(id);

        var diff = Difficulty <= 0 ? 0 : Math.Clamp(Difficulty, 1, 5);
        var task = new TaskDefinition
        {
            Id = id,
            Topic = SelectedTopicKey,
            Type = "numeric",
            TitleKey = Title.Trim(),
            DescriptionKey = Text.Trim(),
            FormulaKey = string.Empty,
            ImagePath = imageRelative,
            PdfPath = pdfRelative,
            LabWpfSetupId = SelectedLab?.SetupId ?? string.Empty,
            ExpectedNumeric = double.Parse(NumericAnswerText.Trim(), CultureInfo.InvariantCulture),
            NumericTolerance = 0.01,
            Difficulty = diff,
            AiReasoning = AiReasoning.Trim(),
            KeyLawsJson = string.IsNullOrWhiteSpace(_keyLawsJson) ? "[]" : _keyLawsJson,
            EstimatedTimeMin = EstimatedTimeMin
        };

        _ctx.TaskRepository.ReplaceTeacherTask(task);
        var teacherOnly = _ctx.TaskRepository.LoadAll().Where(x => x.IsTeacherTask).ToList();
        await _ctx.DatabaseService.UpsertTeacherTasksAsync(teacherOnly).ConfigureAwait(true);

        ResetForm();
        _onTaskAdded();
        System.Windows.MessageBox.Show("Изменения сохранены; tasks.json обновлён.", "Готово", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    private static string TeacherFileSuffixFromId(string taskId)
    {
        const string marker = "_teacher_";
        var i = taskId.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (i < 0)
            return DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
        return taskId[(i + marker.Length)..];
    }

    private string BuildImageRelativeForSave(string taskId)
    {
        var pending = PendingImagePath?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(pending))
            return "";

        var origRel = _editingOriginalImageRelative;
        var origFull = string.IsNullOrWhiteSpace(origRel) ? "" : AppPaths.ResolveGameRelativePath(origRel);
        if (!string.IsNullOrEmpty(origFull) && File.Exists(origFull)
            && string.Equals(Path.GetFullPath(pending), Path.GetFullPath(origFull), StringComparison.OrdinalIgnoreCase))
            return origRel!;

        Directory.CreateDirectory(AppPaths.TeacherImagesDirectory);
        var ext = Path.GetExtension(pending);
        if (string.IsNullOrEmpty(ext))
            ext = ".bin";

        var ts = TeacherFileSuffixFromId(taskId);
        var fileName = $"{SelectedTopicKey}_teacher_{ts}_img{ext}";
        var destFull = Path.Combine(AppPaths.TeacherImagesDirectory, fileName);
        File.Copy(pending, destFull, overwrite: true);
        return Path.Combine("Data", "TeacherImages", fileName).Replace('\\', '/');
    }

    private string BuildPdfRelativeForSave(string taskId)
    {
        var pending = PendingPdfPath?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(pending))
            return "";

        var origRel = _editingOriginalPdfRelative;
        var origFull = string.IsNullOrWhiteSpace(origRel) ? "" : AppPaths.ResolveGameRelativePath(origRel);
        if (!string.IsNullOrEmpty(origFull) && File.Exists(origFull)
            && string.Equals(Path.GetFullPath(pending), Path.GetFullPath(origFull), StringComparison.OrdinalIgnoreCase))
            return origRel!;

        Directory.CreateDirectory(AppPaths.TeacherPdfsDirectory);
        var ext = Path.GetExtension(pending);
        if (!string.Equals(ext, ".pdf", StringComparison.OrdinalIgnoreCase))
            ext = ".pdf";

        var ts = TeacherFileSuffixFromId(taskId);
        var pdfName = $"{SelectedTopicKey}_teacher_{ts}_method{ext}";
        var pdfDest = Path.Combine(AppPaths.TeacherPdfsDirectory, pdfName);
        File.Copy(pending, pdfDest, overwrite: true);
        return Path.Combine("Data", "TeacherPdfs", pdfName).Replace('\\', '/');
    }

    private void ResetForm()
    {
        _editingTaskId = null;
        _editingOriginalImageRelative = null;
        _editingOriginalPdfRelative = null;
        Title = "";
        Text = "";
        NumericAnswerText = "";
        PendingImagePath = "";
        PendingPdfPath = "";
        SelectedTopicKey = "mechanics";
        Difficulty = 0;
        AiReasoning = "";
        _keyLawsJson = "";
        EstimatedTimeMin = null;
        _analyzeFingerprint = string.Empty;
        DifficultyAnalysisError = "";
        ReloadLabs();
        ClearAllErrors();
    }

    private void ValidateAll()
    {
        ClearAllErrors();
        ValidateTitle();
        ValidateText();
        ValidateNumeric();
        ValidateLab();
    }

    private void ValidateLab()
    {
        if (SelectedLab is null)
            AddError(nameof(SelectedLab), "Выберите установку лаборатории.");
    }

    partial void OnTitleChanged(string value) => ValidateTitle();
    partial void OnTextChanged(string value) => ValidateText();
    partial void OnNumericAnswerTextChanged(string value) => ValidateNumeric();

    private void ValidateTitle()
    {
        if (string.IsNullOrWhiteSpace(Title))
            AddError(nameof(Title), "Заголовок обязателен.");
        else if (Title.Trim().Length > 200)
            AddError(nameof(Title), "Не более 200 символов.");
    }

    private void ValidateText()
    {
        if (string.IsNullOrWhiteSpace(Text))
            AddError(nameof(Text), "Текст задачи обязателен.");
    }

    private void ValidateNumeric()
    {
        if (string.IsNullOrWhiteSpace(NumericAnswerText))
        {
            AddError(nameof(NumericAnswerText), "Числовой ответ обязателен.");
            return;
        }

        if (!double.TryParse(NumericAnswerText.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            AddError(nameof(NumericAnswerText), "Введите корректное число (например 3.14).");
    }

    private void AddError(string propertyName, string error)
    {
        if (!_errors.TryGetValue(propertyName, out var list))
        {
            list = [];
            _errors[propertyName] = list;
        }

        if (!list.Contains(error))
            list.Add(error);

        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        OnPropertyChanged(nameof(HasErrors));
    }

    private void ClearAllErrors()
    {
        var keys = _errors.Keys.ToList();
        _errors.Clear();
        foreach (var k in keys)
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(k));

        OnPropertyChanged(nameof(HasErrors));
    }
}
