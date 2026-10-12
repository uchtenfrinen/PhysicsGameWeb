namespace PhisicsGame.Core.Dialog;

public sealed class DialogSystem
{
    private readonly Queue<string> _lines = new();
    public bool IsActive => _lines.Count > 0;
    public string CurrentLine => _lines.Count > 0 ? _lines.Peek() : string.Empty;

    public void Start(IEnumerable<string> lines)
    {
        _lines.Clear();
        foreach (var line in lines)
        {
            _lines.Enqueue(line);
        }
    }

    public void Next()
    {
        if (_lines.Count > 0)
        {
            _lines.Dequeue();
        }
    }
}
