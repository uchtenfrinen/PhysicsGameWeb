using System.Diagnostics.CodeAnalysis;
using PhysicsTasks.Models;

namespace PhysicsTasks;

/// <summary>
/// Domain wrapper with validation. Host game should ship the same logic (or reference this project).
/// </summary>
public sealed class PhysicsTask
{
    public PhysicsTask(TaskDefinition definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public TaskDefinition Definition { get; }

    public bool Validate(TaskRuntimeState state, [NotNullWhen(false)] out string? errorMessage)
    {
        errorMessage = null;

        switch (Definition.Kind)
        {
            case TaskKind.Numeric:
                if (state.NumericAnswer is null)
                {
                    errorMessage = "Введите числовой ответ.";
                    return false;
                }

                if (Definition.ExpectedNumeric is null)
                {
                    errorMessage = "Задача не содержит эталонного значения.";
                    return false;
                }

                if (Math.Abs(state.NumericAnswer.Value - Definition.ExpectedNumeric.Value) <= Definition.NumericTolerance)
                    return true;

                errorMessage = "Неверный числовой ответ.";
                return false;

            case TaskKind.Choice:
                if (state.SelectedChoiceIndex is null)
                {
                    errorMessage = "Выберите вариант.";
                    return false;
                }

                if (Definition.CorrectChoiceIndex is null || Definition.Options is null || Definition.Options.Count == 0)
                {
                    errorMessage = "Задача не содержит вариантов ответа.";
                    return false;
                }

                var idx = state.SelectedChoiceIndex.Value;
                if (idx < 0 || idx >= Definition.Options.Count)
                {
                    errorMessage = "Некорректный выбор.";
                    return false;
                }

                if (idx == Definition.CorrectChoiceIndex.Value)
                    return true;

                errorMessage = "Неверный вариант.";
                return false;

            case TaskKind.Simulation:
                if (state.SimulationValue is null)
                {
                    errorMessage = "Установите значение на слайдере.";
                    return false;
                }

                if (Definition.CorrectSimulationValue is null)
                {
                    errorMessage = "Задача не содержит эталонного значения симуляции.";
                    return false;
                }

                if (Math.Abs(state.SimulationValue.Value - Definition.CorrectSimulationValue.Value) <= Definition.SimulationTolerance)
                    return true;

                errorMessage = "Значение симуляции не совпадает с ожидаемым.";
                return false;

            default:
                errorMessage = "Неизвестный тип задачи.";
                return false;
        }
    }
}
