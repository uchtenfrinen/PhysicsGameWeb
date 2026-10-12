namespace ForTeacher.Models;

public static class TeacherTaskDisplayHelper
{
    /// <summary>Человекочитаемая метка вместо длинного id вида <c>mechanics_teacher_20250419143052123</c>.</summary>
    public static string FormatFriendlyId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return "—";

        const string marker = "_teacher_";
        var i = id.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (i < 0)
            return id.Length > 42 ? id[..39] + "…" : id;

        var tail = id[(i + marker.Length)..];
        // После маркера: yyyyMMddHHmmssfff (17 цифр) при создании из модуля
        if (tail.Length >= 17 && IsDigits17(tail))
        {
            var datePart = tail[..8];
            var timePart = tail[8..17];
            if (datePart.Length == 8 && timePart.Length == 9)
            {
                var yyyy = datePart[..4];
                var mm = datePart[4..6];
                var dd = datePart[6..8];
                var hh = timePart[..2];
                var min = timePart[2..4];
                var sec = timePart[4..6];
                var ms = timePart[6..];
                var suffix = tail.Length > 17 ? " · …" : "";
                return $"{dd}.{mm}.{yyyy} {hh}:{min}:{sec}.{ms}{suffix}";
            }
        }

        return tail.Length > 40 ? tail[..37] + "…" : tail;
    }

    private static bool IsDigits17(string s)
    {
        for (var k = 0; k < 17 && k < s.Length; k++)
        {
            if (!char.IsDigit(s[k]))
                return false;
        }

        return s.Length >= 17;
    }
}
