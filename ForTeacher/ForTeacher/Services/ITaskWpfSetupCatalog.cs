using ForTeacher.Models;

namespace ForTeacher.Services;

/// <summary>
/// Каталог WPF-установок лабораторий, доступных преподавателю для темы.
/// </summary>
public interface ITaskWpfSetupCatalog
{
    IReadOnlyList<LabWpfSetupOption> GetTeacherLabSetupsForTopic(string topic);
}
