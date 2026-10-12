using ForTeacher.Models;

namespace ForTeacher.Services;

public interface IDifficultyAnalyzer
{
    /// <param name="imageBytes">Необязательно: байты изображения для vision-модели.</param>
    /// <param name="imageMediaType">Напр. <c>image/jpeg</c>, <c>image/png</c>.</param>
    Task<DifficultyAnalysisResult?> AnalyzeAsync(
        string title,
        string taskText,
        byte[]? imageBytes,
        string? imageMediaType,
        CancellationToken cancellationToken = default);
}

/// <summary>Опционально: пояснение последней ошибки анализа (HTTP, разбор ответа).</summary>
public interface IDifficultyAnalyzerDiagnostics
{
    string? LastFailureDetail { get; }
}
