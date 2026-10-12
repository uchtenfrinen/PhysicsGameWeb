using System.Collections.Generic;
using PhisicsGame.Tasks;

namespace PhisicsGame.Core.Interop;

/// <summary>Сопоставление задачи игры с id установки из <c>WPFPhisics1.1.Navigation.PhysicsCatalog</c> (аргумент <c>--setup</c>).</summary>
public static class TaskWpfSetupCatalog
{
    private static readonly HashSet<string> KnownSetupIds =
    [
        "inclined_plane",
        "free_fall",
        "projectile",
        "spring",
        "buoyancy",
        "communicating_vessels",
        "ideal_gas",
        "ohm_law",
        "resistors_connection",
        "charge_in_field",
        "thin_lens",
        "refraction_at_boundary",
        "harmonic_oscillations"
    ];

    /// <summary>Установки WPF для выбора в панели преподавателя (по разделу игры).</summary>
    public static IReadOnlyList<(string Id, string RuName)> GetTeacherLabSetupsForTopic(string? topicId)
    {
        var t = topicId?.Trim().ToLowerInvariant() ?? string.Empty;
        return t switch
        {
            "mechanics" =>
            [
                ("inclined_plane", "Груз на наклонной плоскости"),
                ("free_fall", "Свободное падение"),
                ("projectile", "Движение тела, брошенного под углом"),
                ("spring", "Пружина с грузом")
            ],
            "electrostatics" => [("charge_in_field", "Заряд в однородном поле")],
            "optics" =>
            [
                ("thin_lens", "Тонкая линза"),
                ("refraction_at_boundary", "Преломление на границе")
            ],
            "waves" => [("harmonic_oscillations", "Гармонические колебания")],
            "hydro" =>
            [
                ("buoyancy", "Плавание тела (Архимед)"),
                ("communicating_vessels", "Сообщающиеся сосуды")
            ],
            "electricity" =>
            [
                ("ohm_law", "Закон Ома для замкнутой цепи"),
                ("resistors_connection", "Резисторы: последовательно/параллельно"),
                ("charge_in_field", "Заряд в однородном поле")
            ],
            "thermodynamics" => [("ideal_gas", "Уравнение состояния идеального газа")],
            _ => []
        };
    }

    public static bool IsKnownLabSetupId(string? id) =>
        !string.IsNullOrWhiteSpace(id) && KnownSetupIds.Contains(id.Trim());

    public static bool TryGetWpfSetup(string taskId, string? topicId, TaskKind kind, out string setupId)
    {
        setupId = string.Empty;
        _ = taskId;
        return TryFallbackByTopic(topicId, kind, out setupId);
    }

    public static string GetLocalizationKeyForSetup(string setupId) => "task.wpfSetup." + setupId;

    private static bool TryFallbackByTopic(string? topicId, TaskKind kind, out string setupId)
    {
        setupId = string.Empty;
        var t = topicId?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrEmpty(t))
        {
            return false;
        }

        return t switch
        {
            "mechanics" => Pick(kind, "inclined_plane", "inclined_plane", "spring", out setupId),
            "electricity" => Set("ohm_law", out setupId),
            "electrostatics" => Set("charge_in_field", out setupId),
            "optics" => Pick(kind, "thin_lens", "refraction_at_boundary", "thin_lens", out setupId),
            "waves" => Pick(kind, "harmonic_oscillations", "harmonic_oscillations", "harmonic_oscillations", out setupId),
            "thermodynamics" or "heat" => Set("ideal_gas", out setupId),
            "hydro" => Pick(kind, "buoyancy", "communicating_vessels", "buoyancy", out setupId),
            _ => Fail(out setupId)
        };
    }

    private static bool Fail(out string setupId)
    {
        setupId = string.Empty;
        return false;
    }

    private static bool Set(string id, out string setupId)
    {
        setupId = id;
        return true;
    }

    private static bool Pick(TaskKind kind, string numeric, string choice, string simulation, out string setupId)
    {
        setupId = kind switch
        {
            TaskKind.Numeric => numeric,
            TaskKind.Choice => choice,
            TaskKind.Simulation => simulation,
            _ => numeric
        };
        return true;
    }
}
