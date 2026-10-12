using ForTeacher.Models;

namespace ForTeacher.Services;

/// <summary>Совпадает с каталогом установок в игре (раздел → список id).</summary>
public sealed class TaskWpfSetupCatalog : ITaskWpfSetupCatalog
{
    public IReadOnlyList<LabWpfSetupOption> GetTeacherLabSetupsForTopic(string topic)
    {
        var t = topic.Trim().ToLowerInvariant();
        return t switch
        {
            "mechanics" =>
            [
                new LabWpfSetupOption("inclined_plane", "Груз на наклонной плоскости"),
                new LabWpfSetupOption("free_fall", "Свободное падение"),
                new LabWpfSetupOption("projectile", "Движение тела, брошенного под углом"),
                new LabWpfSetupOption("spring", "Пружина с грузом"),
            ],
            "electrostatics" => [new LabWpfSetupOption("charge_in_field", "Заряд в однородном поле")],
            "optics" =>
            [
                new LabWpfSetupOption("thin_lens", "Тонкая линза"),
                new LabWpfSetupOption("refraction_at_boundary", "Преломление на границе"),
            ],
            "waves" => [new LabWpfSetupOption("harmonic_oscillations", "Гармонические колебания")],
            "electricity" =>
            [
                new LabWpfSetupOption("ohm_law", "Закон Ома для замкнутой цепи"),
                new LabWpfSetupOption("resistors_connection", "Резисторы: последовательно/параллельно"),
                new LabWpfSetupOption("charge_in_field", "Заряд в однородном поле"),
            ],
            "thermodynamics" => [new LabWpfSetupOption("ideal_gas", "Уравнение состояния идеального газа")],
            _ => [],
        };
    }
}
