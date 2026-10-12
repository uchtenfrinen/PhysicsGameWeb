namespace WPFPhisics1._1.Navigation;

/// <summary>Раздел физики (механика, электричество и т.д.).</summary>
public class PhysicsSection
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public List<PhysicsSetup> Setups { get; init; } = new();
}

/// <summary>Установка внутри раздела (например «Груз на наклонной плоскости»).</summary>
public class PhysicsSetup
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
}

/// <summary>Каталог разделов и установок.</summary>
public static class PhysicsCatalog
{
    public static IReadOnlyList<PhysicsSection> Sections { get; } = new List<PhysicsSection>
    {
        new()
        {
            Id = "mechanics",
            Name = "Механика",
            Setups = new List<PhysicsSetup>
            {
                new() { Id = "inclined_plane", Name = "Груз на наклонной плоскости" },
                new() { Id = "free_fall", Name = "Свободное падение" },
                new() { Id = "projectile", Name = "Движение тела, брошенного под углом" },
                new() { Id = "spring", Name = "Пружина с грузом" }
            }
        },
        new()
        {
            Id = "hydro",
            Name = "Гидростатика",
            Setups = new List<PhysicsSetup>
            {
                new() { Id = "buoyancy", Name = "Плавание тела (Архимед)" },
                new() { Id = "communicating_vessels", Name = "Сообщающиеся сосуды" }
            }
        },
        new()
        {
            Id = "heat",
            Name = "Теплота и МКТ",
            Setups = new List<PhysicsSetup>
            {
                new() { Id = "ideal_gas", Name = "Уравнение состояния идеального газа" }
            }
        },
        new()
        {
            Id = "electricity",
            Name = "Электричество",
            Setups = new List<PhysicsSetup>
            {
                new() { Id = "ohm_law", Name = "Закон Ома для замкнутой цепи" },
                new() { Id = "resistors_connection", Name = "Резисторы: последовательно/параллельно" },
                new() { Id = "charge_in_field", Name = "Заряд в однородном поле" }
            }
        },
        new()
        {
            Id = "optics",
            Name = "Оптика",
            Setups = new List<PhysicsSetup>
            {
                new() { Id = "thin_lens", Name = "Тонкая линза" },
                new() { Id = "refraction_at_boundary", Name = "Преломление на границе" }
            }
        },
        new()
        {
            Id = "waves",
            Name = "Колебания и волны",
            Setups = new List<PhysicsSetup>
            {
                new() { Id = "harmonic_oscillations", Name = "Гармонические колебания" }
            }
        }
    };
}
