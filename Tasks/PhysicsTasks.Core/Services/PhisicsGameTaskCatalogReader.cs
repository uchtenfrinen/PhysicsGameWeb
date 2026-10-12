using System.Collections.Generic;
using System.Text.Json;
using PhysicsTasks.Models;

namespace PhysicsTasks.Services;

/// <summary>
/// Читает массив задач из <c>PhisicsGame/Data/tasks.json</c> (поля Topic, Type, TitleKey, DescriptionKey, …).
/// </summary>
public static class PhisicsGameTaskCatalogReader
{
    private static readonly JsonSerializerOptions GameDtoOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static bool TryImport(string jsonText, out List<TaskDefinition> tasks)
    {
        tasks = [];
        try
        {
            using var doc = JsonDocument.Parse(jsonText);
            return TryImport(doc.RootElement, out tasks);
        }
        catch
        {
            return false;
        }
    }

    public static bool TryImport(JsonElement root, out List<TaskDefinition> tasks)
    {
        tasks = [];
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
            return false;

        var first = root[0];
        if (first.ValueKind != JsonValueKind.Object
            || !first.TryGetProperty("TitleKey", out _)
            || !first.TryGetProperty("Type", out _))
            return false;

        foreach (var el in root.EnumerateArray())
        {
            var dto = el.Deserialize<PhisicsGameTaskDto>(GameDtoOptions);
            if (dto is null || string.IsNullOrWhiteSpace(dto.Id))
                continue;

            var mapped = Map(dto);
            if (mapped is not null)
                tasks.Add(mapped);
        }

        return true;
    }

    private static TaskDefinition? Map(PhisicsGameTaskDto g)
    {
        var kind = g.Type.Trim().ToLowerInvariant() switch
        {
            "numeric" => TaskKind.Numeric,
            "choice" => TaskKind.Choice,
            "simulation" => TaskKind.Simulation,
            _ => (TaskKind?)null,
        };

        if (kind is null)
            return null;

        var lab = (g.LabWpfSetupId ?? string.Empty).Trim();
        string? labId = string.IsNullOrEmpty(lab) || string.Equals(lab, "none", StringComparison.OrdinalIgnoreCase)
            ? null
            : lab;

        var imageRel = (g.ImagePath ?? string.Empty).Trim();
        if (imageRel.Length == 0)
        {
            imageRel = null;
        }

        string? pdfRel = (g.PdfPath ?? string.Empty).Trim();
        if (pdfRel.Length == 0)
            pdfRel = null;

        var def = new TaskDefinition
        {
            Id = g.Id.Trim(),
            TopicId = string.IsNullOrWhiteSpace(g.Topic) ? null : g.Topic.Trim(),
            Title = string.IsNullOrWhiteSpace(g.TitleKey) ? g.Id : g.TitleKey.Trim(),
            ConditionMarkdown = string.IsNullOrWhiteSpace(g.DescriptionKey) ? string.Empty : g.DescriptionKey.Trim(),
            FormulaText = string.IsNullOrWhiteSpace(g.FormulaKey) ? null : g.FormulaKey.Trim(),
            Kind = kind.Value,
            ExpectedNumeric = g.ExpectedNumeric,
            NumericTolerance = g.NumericTolerance <= 0 ? 0.01 : g.NumericTolerance,
            LabWpfSetupId = labId,
            ImagePath = imageRel,
            PdfPath = pdfRel,
            Difficulty = Math.Clamp(g.Difficulty, 0, 5),
            AiReasoning = string.IsNullOrWhiteSpace(g.AiReasoning) ? null : g.AiReasoning.Trim(),
            KeyLawsJson = string.IsNullOrWhiteSpace(g.KeyLawsJson) ? null : g.KeyLawsJson.Trim(),
            EstimatedTimeMin = g.EstimatedTimeMin,
        };

        if (kind == TaskKind.Choice)
        {
            def.Options = g.Choices is { Length: > 0 } ? new List<string>(g.Choices) : [];
            def.CorrectChoiceIndex = g.CorrectChoiceIndex;
        }

        if (kind == TaskKind.Simulation)
        {
            def.SimulationMin = 0;
            def.SimulationMax = 20;
            def.CorrectSimulationValue = g.SimulationTarget.HasValue ? g.SimulationTarget.Value : null;
            def.SimulationTolerance = 0.01;
        }

        return def;
    }

    private sealed class PhisicsGameTaskDto
    {
        public string Id { get; set; } = "";
        public string Topic { get; set; } = "";
        public string Type { get; set; } = "numeric";
        public string TitleKey { get; set; } = "";
        public string DescriptionKey { get; set; } = "";
        public string FormulaKey { get; set; } = "";
        public string ImagePath { get; set; } = "";
        public string PdfPath { get; set; } = "";
        public string LabWpfSetupId { get; set; } = "";
        public double? ExpectedNumeric { get; set; }
        public double NumericTolerance { get; set; } = 0.01;
        public string[] Choices { get; set; } = [];
        public int CorrectChoiceIndex { get; set; }
        public float? SimulationTarget { get; set; }
        public int Difficulty { get; set; }
        public string AiReasoning { get; set; } = "";
        public string KeyLawsJson { get; set; } = "";
        public int? EstimatedTimeMin { get; set; }
    }
}
