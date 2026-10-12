namespace ForTeacher.Models;

/// <summary>Вариант установки: <see cref="SetupId"/> пусто = авто, <c>none</c> = без лаборатории, иначе id установки.</summary>
public sealed record LabWpfSetupOption(string SetupId, string DisplayName);
