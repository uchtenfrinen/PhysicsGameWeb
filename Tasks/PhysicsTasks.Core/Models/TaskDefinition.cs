namespace PhysicsTasks.Models;

/// <summary>
/// Serializable task metadata (matches <c>Data/tasks.json</c> contract).
/// </summary>
public sealed class TaskDefinition
{
    public string Id { get; set; } = "";

    public string? TopicId { get; set; }

    public string Title { get; set; } = "";

    /// <summary>Markdown condition body.</summary>
    public string ConditionMarkdown { get; set; } = "";

    /// <summary>Относительный путь к изображению (как в PhisicsGame <c>tasks.json</c>, напр. <c>Data/TeacherImages/…</c>).</summary>
    public string? ImagePath { get; set; }

    /// <summary>Относительный путь к PDF (те же правила разрешения пути, что и для <see cref="ImagePath"/>).</summary>
    public string? PdfPath { get; set; }

    /// <summary>Optional formula line (plain text / pseudo-LaTeX).</summary>
    public string? FormulaText { get; set; }

    public TaskKind Kind { get; set; }

    public double? ExpectedNumeric { get; set; }

    public double NumericTolerance { get; set; } = 0.01;

    public List<string>? Options { get; set; }

    public int? CorrectChoiceIndex { get; set; }

    public double? SimulationMin { get; set; }

    public double? SimulationMax { get; set; }

    public double? CorrectSimulationValue { get; set; }

    public double SimulationTolerance { get; set; } = 0.05;

    public string? LabWpfSetupId { get; set; }

    /// <summary>Сложность задачи от 1 до 5 (0 — не задано).</summary>
    public int Difficulty { get; set; }

    /// <summary>Краткое объяснение оценки сложности (если есть).</summary>
    public string? AiReasoning { get; set; }

    /// <summary>Ключевые законы в формате JSON-строки (опционально).</summary>
    public string? KeyLawsJson { get; set; }

    /// <summary>Оценка времени решения в минутах (опционально).</summary>
    public int? EstimatedTimeMin { get; set; }
}
