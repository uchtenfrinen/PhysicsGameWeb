namespace ForTeacher.Models;

/// <summary>Ответ LLM-классификатора сложности (строгий JSON).</summary>
public sealed class DifficultyAnalysisResult
{
    public int Difficulty { get; init; }

    public string Reasoning { get; init; } = "";

    public IReadOnlyList<string> KeyLaws { get; init; } = Array.Empty<string>();

    public int EstimatedTimeMin { get; init; }
}
