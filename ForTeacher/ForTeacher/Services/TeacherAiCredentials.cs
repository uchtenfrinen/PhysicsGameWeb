using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ForTeacher.Services;

/// <summary>
/// Те же источники ключа, что и <c>WPFPhisics1.1.Services.GroqService</c>: переменная <c>GROQ_API_KEY</c>, файл <c>groq_api_key.txt</c>,
/// плюс явный <c>FOR_TEACHER_OPENAI_API_KEY</c> и <see cref="TeacherAiApiKeyConfig.Key"/>.
/// Дополнительно ищем ключ в типичных путях WPF при запуске ForTeacher из своего <c>bin</c> (ключ остаётся только у WPF).
/// </summary>
public static class TeacherAiCredentials
{
    private const int MaxAncestorWalk = 14;

    /// <summary>Пути к <c>groq_api_key.txt</c>: как в GroqService, затем предки каталога exe и корни с вложенным WPFPhisics1.1.</summary>
    private static IEnumerable<string> GetGroqKeyFilePaths()
    {
        var baseDir = AppContext.BaseDirectory;
        yield return Path.Combine(baseDir, "groq_api_key.txt");
        yield return Path.Combine(baseDir, "..", "groq_api_key.txt");
        yield return Path.Combine(Directory.GetCurrentDirectory(), "groq_api_key.txt");
        var projDir = Path.Combine(baseDir, "..", "..", "..");
        if (Directory.Exists(projDir))
        {
            yield return Path.Combine(projDir, "groq_api_key.txt");
        }

        foreach (var root in EnumerateAncestorDirectories(baseDir))
        {
            foreach (var p in GetGroqKeyFilesUnderSearchRoot(root))
            {
                yield return p;
            }
        }

        var cwd = Directory.GetCurrentDirectory();
        if (!PathsEqual(cwd, baseDir))
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

    /// <summary>Файлы ключа относительно одного каталога вверх по дереву (корень репозитория, папка решения и т.д.).</summary>
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

    /// <summary>Первая непустая строка из файла.</summary>
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

    public static string? ResolveApiKey()
    {
        var explicitOpenAi = Environment.GetEnvironmentVariable("FOR_TEACHER_OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(explicitOpenAi))
        {
            return explicitOpenAi.Trim();
        }

        var openAiEnv = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(openAiEnv))
        {
            return openAiEnv.Trim();
        }

        if (!string.IsNullOrWhiteSpace(TeacherAiApiKeyConfig.Key))
        {
            return TeacherAiApiKeyConfig.Key.Trim();
        }

        var fromEnv = Environment.GetEnvironmentVariable("GROQ_API_KEY");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv.Trim();
        }

        var triedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in GetGroqKeyFilePaths())
        {
            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(path);
            }
            catch
            {
                continue;
            }

            if (!triedFiles.Add(fullPath))
            {
                continue;
            }

            var key = ReadFirstLineFromFile(fullPath);
            if (!string.IsNullOrWhiteSpace(key))
            {
                return key;
            }
        }

        var openAiKeyPath = Path.Combine(AppContext.BaseDirectory, "openai_api_key.txt");
        var openAiKey = ReadFirstLineFromFile(openAiKeyPath);
        if (!string.IsNullOrWhiteSpace(openAiKey))
        {
            return openAiKey;
        }

        return null;
    }
}
