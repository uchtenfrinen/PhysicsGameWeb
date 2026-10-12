namespace PhisicsGame.Core.Tasks;

public sealed class TaskDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Topic { get; set; } = "mechanics";
    public string Type { get; set; } = "numeric";
    public string TitleKey { get; set; } = string.Empty;
    public string DescriptionKey { get; set; } = string.Empty;
    public string FormulaKey { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>Относительный путь к PDF для терминала задач (при наличии файла).</summary>
    public string PdfPath { get; set; } = string.Empty;

    /// <summary>
    /// Лаборатория WPF: пусто = авто по разделу; <c>none</c> = без установки; иначе id установки (inclined_plane и т.д.).
    /// </summary>
    public string LabWpfSetupId { get; set; } = string.Empty;

    public double? ExpectedNumeric { get; set; }
    public double NumericTolerance { get; set; } = 0.01;
    public string[] Choices { get; set; } = [];
    public int CorrectChoiceIndex { get; set; }
    public float? SimulationTarget { get; set; }

    /// <summary>1–5 или 0, если не задано.</summary>
    public int Difficulty { get; set; }

    public string AiReasoning { get; set; } = string.Empty;

    public string KeyLawsJson { get; set; } = string.Empty;

    public int? EstimatedTimeMin { get; set; }
}
