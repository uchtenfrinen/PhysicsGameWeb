using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using PhysicsTasks.Models;

namespace PhysicsTasks.Terminal.ViewModels;

public sealed partial class TaskListItemViewModel : ObservableObject
{
    public TaskListItemViewModel(TaskDefinition definition, bool isCompleted)
    {
        Definition = definition;
        _isCompleted = isCompleted;
        CatalogGlyph = CreateCatalogGlyph(definition.Kind);
    }

    public TaskDefinition Definition { get; }

    public Geometry CatalogGlyph { get; }

    public string DisplayTitle => string.IsNullOrWhiteSpace(Definition.TopicId)
        ? Definition.Title
        : $"{Definition.Title}  ·  {Definition.TopicId}";

    [ObservableProperty]
    private bool _isCompleted;

    public void RefreshCompletion(bool isCompleted)
    {
        IsCompleted = isCompleted;
    }

    private static Geometry CreateCatalogGlyph(TaskKind kind) =>
        kind switch
        {
            TaskKind.Numeric => Geometry.Parse("M10,3 L10,18 M6,11 L10,3 L14,11"),
            TaskKind.Choice => Geometry.Parse("M10,2 L4,14 H9 L8,22 L18,10 H12 L16,2 Z"),
            TaskKind.Simulation => Geometry.Parse("M2,12 Q6,5 10,12 T18,12 T26,12"),
            _ => Geometry.Parse("M4,10 H16"),
        };
}
