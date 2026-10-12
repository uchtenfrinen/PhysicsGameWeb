namespace PhysicsTasks.Terminal;

internal static class StartupArgsParser
{
    /// <summary>Аргумент из MonoGame: <c>--tasks-json "C:\...\Data\tasks.json"</c>.</summary>
    public static string? TryGetTasksJsonPath(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], "--tasks-json", StringComparison.OrdinalIgnoreCase))
                return args[i + 1].Trim();
        }

        return null;
    }

    /// <summary>Фильтр списка задач по разделу игры: <c>--topic mechanics</c>.</summary>
    public static string? TryGetTopicFilter(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], "--topic", StringComparison.OrdinalIgnoreCase))
                return args[i + 1].Trim();
        }

        return null;
    }

    /// <summary>Фильтр списка задач по установке/подтеме: <c>--lab-setup inclined_plane</c>.</summary>
    public static string? TryGetLabSetupFilter(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], "--lab-setup", StringComparison.OrdinalIgnoreCase))
                return args[i + 1].Trim();
        }

        return null;
    }

    internal readonly record struct ParsedTask(string TaskId, string? TopicId);

    public static ParsedTask? TryParseTask(string[] args)
    {
        string? taskId = null;
        string? topicId = null;

        foreach (var raw in args)
        {
            var a = raw.Trim();
            if (a.Length == 0)
                continue;

            if (TrySplit(a, "task=", out var v) || TrySplit(a, "/task:", out v) || TrySplit(a, "-task:", out v))
            {
                taskId = v;
                continue;
            }

            if (TrySplit(a, "topic=", out v) || TrySplit(a, "/topic:", out v) || TrySplit(a, "-topic:", out v))
            {
                topicId = v;
                continue;
            }

            if (string.Equals(a, "-task", StringComparison.OrdinalIgnoreCase)
                || string.Equals(a, "/task", StringComparison.OrdinalIgnoreCase))
            {
                var idx = Array.IndexOf(args, raw);
                if (idx >= 0 && idx + 1 < args.Length)
                    taskId = args[idx + 1].Trim();
            }
        }

        return string.IsNullOrWhiteSpace(taskId) ? null : new ParsedTask(taskId, topicId);
    }

    private static bool TrySplit(string argument, string prefix, out string value)
    {
        value = "";
        if (!argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        value = argument[prefix.Length..].Trim();
        return value.Length > 0;
    }
}
