namespace ForTeacher.ViewModels;

public sealed class SectionItem
{
    public SectionItem(string topicKey, string displayName)
    {
        TopicKey = topicKey;
        DisplayName = displayName;
    }

    /// <summary>Пустая строка означает «все разделы».</summary>
    public string TopicKey { get; }
    public string DisplayName { get; }
}
