using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PhisicsGame.Core.Interop;

/// <summary>
/// Те же правила поиска <c>groq_api_key.txt</c>, что в ForTeacher (<c>TeacherAiCredentials</c>), но от якорного каталога (каталог PhisicsGame.exe).
/// Нужен, чтобы перед запуском ForTeacher подставить <c>GROQ_API_KEY</c> в окружение дочернего процесса.
/// </summary>
internal static class GroqApiKeyFileLocator
{
    private const int MaxAncestorWalk = 14;

    public static string? TryReadFirstGroqKey(string anchorBaseDirectory)
    {
        var tried = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in EnumerateGroqKeyFilePaths(anchorBaseDirectory))
        {
            string full;
            try
            {
                full = Path.GetFullPath(path);
            }
            catch
            {
                continue;
            }

            if (!tried.Add(full))
            {
                continue;
            }

            var key = ReadFirstLineFromFile(full);
            if (!string.IsNullOrWhiteSpace(key))
            {
                return key.Trim();
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateGroqKeyFilePaths(string anchorBaseDirectory)
    {
        yield return Path.Combine(anchorBaseDirectory, "groq_api_key.txt");
        yield return Path.Combine(anchorBaseDirectory, "..", "groq_api_key.txt");
        yield return Path.Combine(Directory.GetCurrentDirectory(), "groq_api_key.txt");
        var projDir = Path.Combine(anchorBaseDirectory, "..", "..", "..");
        if (Directory.Exists(projDir))
        {
            yield return Path.Combine(projDir, "groq_api_key.txt");
        }

        foreach (var root in EnumerateAncestorDirectories(anchorBaseDirectory))
        {
            foreach (var p in GetGroqKeyFilesUnderSearchRoot(root))
            {
                yield return p;
            }
        }

        var cwd = Directory.GetCurrentDirectory();
        if (!PathsEqual(cwd, anchorBaseDirectory))
        {
            foreach (var root in EnumerateAncestorDirectories(cwd))
            {
                foreach (var p in GetGroqKeyFilesUnderSearchRoot(root))
                {
                    yield return p;
                }
            }
        }
    }

    private static bool PathsEqual(string a, string b)
    {
        try
        {
            return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<string> EnumerateAncestorDirectories(string start)
    {
        DirectoryInfo? d;
        try
        {
            d = new DirectoryInfo(Path.GetFullPath(start));
        }
        catch
        {
            yield break;
        }

        for (var i = 0; i < MaxAncestorWalk && d is not null; i++, d = d.Parent)
        {
            yield return d.FullName;
        }
    }

    private static IEnumerable<string> GetGroqKeyFilesUnderSearchRoot(string root)
    {
        yield return Path.Combine(root, "groq_api_key.txt");
        yield return Path.Combine(root, "WPFPhisics1.1", "WPFPhisics1.1", "groq_api_key.txt");
        foreach (var cfg in new[] { "Debug", "Release" })
        {
            foreach (var tfm in new[] { "net9.0-windows", "net8.0-windows" })
            {
                yield return Path.Combine(
                    root,
                    "WPFPhisics1.1",
                    "WPFPhisics1.1",
                    "bin",
                    cfg,
                    tfm,
                    "groq_api_key.txt");
            }
        }
    }

    private static string? ReadFirstLineFromFile(string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            return null;
        }

        try
        {
            using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var text = reader.ReadToEnd();
            return text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .FirstOrDefault(s => !string.IsNullOrEmpty(s));
        }
        catch
        {
            return null;
        }
    }
}
