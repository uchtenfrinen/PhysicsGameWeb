using ForTeacher.Models;

namespace ForTeacher.Services;

public sealed class NullDifficultyAnalyzer : IDifficultyAnalyzer, IDifficultyAnalyzerDiagnostics
{
    public string? LastFailureDetail =>
        "Ключ API не найден: задайте FOR_TEACHER_OPENAI_API_KEY, OPENAI_API_KEY или GROQ_API_KEY, либо положите groq_api_key.txt/openai_api_key.txt рядом с exe.";

    public Task<DifficultyAnalysisResult?> AnalyzeAsync(
        string title,
        string taskText,
        byte[]? imageBytes,
        string? imageMediaType,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<DifficultyAnalysisResult?>(null);
}
