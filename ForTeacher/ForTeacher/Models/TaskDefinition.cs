using System.Text.Json.Serialization;

namespace ForTeacher.Models;

/// <summary>Формат записи в <c>tasks.json</c> игры Physics Adventure (массив задач).</summary>
public sealed class TaskDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Topic { get; set; } = "mechanics";
    public string Type { get; set; } = "numeric";
    public string TitleKey { get; set; } = string.Empty;
    public string DescriptionKey { get; set; } = string.Empty;
    public string FormulaKey { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>Относительный путь к PDF-методичке (как <see cref="ImagePath"/>, напр. <c>Data/TeacherPdfs/…</c>).</summary>
    public string PdfPath { get; set; } = string.Empty;

    public string LabWpfSetupId { get; set; } = string.Empty;
    public double? ExpectedNumeric { get; set; }
    public double NumericTolerance { get; set; } = 0.01;
    public string[] Choices { get; set; } = [];
    public int CorrectChoiceIndex { get; set; }
    public float? SimulationTarget { get; set; }

    /// <summary>1–5: сложность (ИИ или вручную). 0 — не задано.</summary>
    public int Difficulty { get; set; }

    /// <summary>Краткое обоснование от ИИ (подсказка / ToolTip).</summary>
    public string AiReasoning { get; set; } = string.Empty;

    /// <summary>JSON-массив строк законов/тем, например <c>["Закон Ома","…"]</c>.</summary>
    public string KeyLawsJson { get; set; } = string.Empty;

    public int? EstimatedTimeMin { get; set; }

    [JsonIgnore]
    public bool IsTeacherTask => Id.Contains("_teacher_", StringComparison.OrdinalIgnoreCase);

    /// <summary>Краткая метка для таблицы (дата/время создания вместо полного id).</summary>
    [JsonIgnore]
    public string FriendlyId => TeacherTaskDisplayHelper.FormatFriendlyId(Id);

    [JsonIgnore]
    public string AttachedMethodichkaStatus =>
        string.IsNullOrWhiteSpace(PdfPath) ? "Отсутствует" : "Присутствует";
}
