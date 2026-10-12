namespace WPFPhisics1._1.Services;

/// <summary>
/// Контекст текущей установки для чата с нейросетью. Задаётся при переходе на страницу установки.
/// </summary>
public static class PhysicsChatContext
{
    /// <summary>Название текущей установки (например «Свободное падение») или пусто на главной.</summary>
    public static string CurrentSetupName { get; set; } = "";
}
