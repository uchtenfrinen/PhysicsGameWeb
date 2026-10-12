using ForTeacher.Models;

namespace ForTeacher.Services;

/// <summary>
/// Проверяет ключ при каждом анализе (не только при старте приложения), кеширует <see cref="OpenAiDifficultyAnalyzer"/>.
/// </summary>
public sealed class DifficultyAnalyzerRouter : IDifficultyAnalyzer, IDifficultyAnalyzerDiagnostics
{
    private static readonly NullDifficultyAnalyzer NullAnalyzer = new();
    private OpenAiDifficultyAnalyzer? _openAi;
    private string _activeApiKey = string.Empty;
    private string _activeBaseUrl = string.Empty;
    private string _activeModel = string.Empty;
    private string _activeVisionModel = string.Empty;

    public string? LastFailureDetail { get; private set; }

    public async Task<DifficultyAnalysisResult?> AnalyzeAsync(
        string title,
        string taskText,
        byte[]? imageBytes,
        string? imageMediaType,
        CancellationToken cancellationToken = default)
    {
        LastFailureDetail = null;

        if (string.IsNullOrWhiteSpace(TeacherAiCredentials.ResolveApiKey()))
        {
            LastFailureDetail = NullAnalyzer.LastFailureDetail;
            return await NullAnalyzer
                .AnalyzeAsync(title, taskText, imageBytes, imageMediaType, cancellationToken)
                .ConfigureAwait(false);
        }

        EnsureAnalyzerIsFresh();
        IDifficultyAnalyzer analyzer = (IDifficultyAnalyzer?)_openAi ?? NullAnalyzer;
        var result = await analyzer
            .AnalyzeAsync(title, taskText, imageBytes, imageMediaType, cancellationToken)
            .ConfigureAwait(false);

        if (result is null)
        {
            LastFailureDetail = (analyzer as IDifficultyAnalyzerDiagnostics)?.LastFailureDetail;
        }

        return result;
    }

    private void EnsureAnalyzerIsFresh()
    {
        var key = TeacherAiCredentials.ResolveApiKey()?.Trim() ?? string.Empty;
        var baseUrl = Environment.GetEnvironmentVariable("FOR_TEACHER_OPENAI_BASE_URL")?.Trim() ?? string.Empty;
        var model = Environment.GetEnvironmentVariable("FOR_TEACHER_OPENAI_MODEL")?.Trim() ?? string.Empty;
        var visionModel = Environment.GetEnvironmentVariable("FOR_TEACHER_OPENAI_VISION_MODEL")?.Trim() ?? string.Empty;

        var changed =
            _openAi is null ||
            !string.Equals(_activeApiKey, key, StringComparison.Ordinal) ||
            !string.Equals(_activeBaseUrl, baseUrl, StringComparison.Ordinal) ||
            !string.Equals(_activeModel, model, StringComparison.Ordinal) ||
            !string.Equals(_activeVisionModel, visionModel, StringComparison.Ordinal);

        if (!changed)
            return;

        _openAi?.Dispose();
        _openAi = new OpenAiDifficultyAnalyzer();
        _activeApiKey = key;
        _activeBaseUrl = baseUrl;
        _activeModel = model;
        _activeVisionModel = visionModel;
    }
}
