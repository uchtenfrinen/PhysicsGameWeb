using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WPFPhisics1._1.Services;

/// <summary>
/// Сервис запросов к Groq API для текстового объяснения (опционально).
/// Ключ: переменная окружения GROQ_API_KEY или файл groq_api_key.txt в папке приложения.
/// </summary>
public class GroqService
{
    private const int MaxAncestorWalk = 14;

    private static string NormalizeApiKey(string raw)
    {
        var key = (raw ?? string.Empty).Trim();
        key = key.Trim('\uFEFF', '\u200B');

        if (key.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            key = key["Bearer ".Length..].Trim();

        if ((key.StartsWith('"') && key.EndsWith('"')) || (key.StartsWith('\'') && key.EndsWith('\'')))
            key = key[1..^1].Trim();

        return key;
    }

    /// <summary>Пути, где ищем groq_api_key.txt (в порядке приоритета).</summary>
    private static IEnumerable<string> GetKeyFilePaths()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        yield return Path.Combine(baseDir, "groq_api_key.txt");
        yield return Path.Combine(baseDir, "..", "groq_api_key.txt");
        yield return Path.Combine(Directory.GetCurrentDirectory(), "groq_api_key.txt");
        var projDir = Path.Combine(baseDir, "..", "..", "..");
        if (Directory.Exists(projDir))
            yield return Path.Combine(projDir, "groq_api_key.txt");

        foreach (var root in EnumerateAncestorDirectories(baseDir))
        {
            foreach (var path in GetGroqKeyFilesUnderSearchRoot(root))
                yield return path;
        }

        var cwd = Directory.GetCurrentDirectory();
        if (!PathsEqual(cwd, baseDir))
        {
            foreach (var root in EnumerateAncestorDirectories(cwd))
            {
                foreach (var path in GetGroqKeyFilesUnderSearchRoot(root))
                    yield return path;
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
        DirectoryInfo? directory;
        try
        {
            directory = new DirectoryInfo(Path.GetFullPath(start));
        }
        catch
        {
            yield break;
        }

        for (var i = 0; i < MaxAncestorWalk && directory is not null; i++, directory = directory.Parent)
            yield return directory.FullName;
    }

    private static IEnumerable<string> GetGroqKeyFilesUnderSearchRoot(string root)
    {
        yield return Path.Combine(root, "groq_api_key.txt");
        yield return Path.Combine(root, "WPFPhisics1.1", "WPFPhisics1.1", "groq_api_key.txt");
        foreach (var configuration in new[] { "Debug", "Release" })
        {
            foreach (var targetFramework in new[] { "net9.0-windows", "net8.0-windows" })
            {
                yield return Path.Combine(
                    root,
                    "WPFPhisics1.1",
                    "WPFPhisics1.1",
                    "bin",
                    configuration,
                    targetFramework,
                    "groq_api_key.txt");
            }
        }
    }

    private static string? GetApiKey()
    {
        if (!string.IsNullOrWhiteSpace(GroqApiKeyConfig.Key))
        {
            var configured = NormalizeApiKey(GroqApiKeyConfig.Key);
            if (!string.IsNullOrWhiteSpace(configured))
                return configured;
        }

        var fromEnv = Environment.GetEnvironmentVariable("GROQ_API_KEY");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            var envKey = NormalizeApiKey(fromEnv);
            if (!string.IsNullOrWhiteSpace(envKey))
                return envKey;
        }

        foreach (var path in GetKeyFilePaths())
        {
            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath)) continue;

            try
            {
                using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                var text = reader.ReadToEnd();
                // Берём первую непустую строку (на случай нескольких строк или пробелов)
                var key = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim()).FirstOrDefault(s => !string.IsNullOrEmpty(s));
                if (!string.IsNullOrWhiteSpace(key))
                {
                    var fileKey = NormalizeApiKey(key);
                    if (!string.IsNullOrWhiteSpace(fileKey))
                        return fileKey;
                }
            }
            catch (Exception)
            {
                // Пробуем следующий путь
            }
        }

        return null;
    }

    /// <summary>Сообщение для пользователя: где искать ключ, если не найден.</summary>
    public static string GetKeySearchPathsMessage()
    {
        var paths = GetKeyFilePaths().Select(p => Path.GetFullPath(p)).ToList();
        return "Искали ключ в: " + string.Join("; ", paths);
    }

    /// <summary>Инструкция для модели: не использовать LaTeX, только обычный текст (для отображения в простом текстовом блоке).</summary>
    private const string NoLatexInstruction = "\n\nВажно: формулы пиши без LaTeX и без разметки. Используй только обычный текст: буквы греческого алфавита (α, β, ω, ν, ρ, θ), подстрочные индексы Unicode (₀, ₁, ₓ, например v₀, y₀, x₁), точку умножения ·, дроби в одну строку вида a/b или (числитель)/(знаменатель). Не используй \\[, \\], \\frac, \\cos, \\sin, \\tan, \\cdot и подобные команды — они отображаются как непонятные символы.";

    /// <summary>Инструкция: в объяснении использовать только переданные численные результаты, не пересчитывать их.</summary>
    private const string UseGivenValuesInstruction = "\n\nОбязательно: в тексте объяснения используй только те численные значения (L, H, T, силы, и т.д.), которые указаны выше в задании. Не пересчитывай их по формулам и не приводи другие числа — все цифры должны совпадать с результатами расчёта в приложении.";

    /// <summary>
    /// Универсальный запрос к Groq: отправляет prompt и возвращает ответ нейросети.
    /// Ключ берётся из GroqApiKeyConfig.Key, GROQ_API_KEY или groq_api_key.txt.
    /// </summary>
    public static async Task<string> GetExplanationAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var key = GetApiKey();
        if (string.IsNullOrWhiteSpace(key))
            return "Ключ API не найден. Задайте GroqApiKeyConfig.Key в коде (Services/GroqApiKeyConfig.cs), переменную окружения GROQ_API_KEY или файл groq_api_key.txt. " + GetKeySearchPathsMessage();

        prompt = prompt + NoLatexInstruction + UseGivenValuesInstruction;

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + key);
            var body = new
            {
                model = "llama-3.1-8b-instant",
                messages = new[] { new { role = "user", content = prompt } },
                max_tokens = 1024
            };
            var json = JsonSerializer.Serialize(body);
            var response = await client.PostAsync(
                "https://api.groq.com/openai/v1/chat/completions",
                new StringContent(json, Encoding.UTF8, "application/json"),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return $"Ошибка API: {response.StatusCode}. {await response.Content.ReadAsStringAsync(cancellationToken)}";

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);
            var choices = doc.RootElement.GetProperty("choices");
            var first = choices.EnumerateArray().FirstOrDefault();
            if (first.ValueKind == JsonValueKind.Undefined)
                return "Пустой ответ от API.";
            var content = first.GetProperty("message").GetProperty("content").GetString();
            return SanitizeLatexToPlain(content ?? "Пустой ответ.");
        }
        catch (Exception ex)
        {
            return $"Ошибка запроса: {ex.Message}";
        }
    }

    /// <summary>
    /// Чат с нейросетью: отвечает только в рамках физики. Контекст установки и вопрос пользователя.
    /// Не добавляет инструкцию про «использовать только переданные значения».
    /// </summary>
    public static async Task<string> GetChatAnswerAsync(string setupContext, string userQuestion, CancellationToken cancellationToken = default)
    {
        var prompt = "Ты — помощник по физике в учебном приложении. Отвечай только на вопросы в рамках физики: механика, гидростатика, термодинамика, электричество, оптика, колебания. " +
                     "Если вопрос не по физике — вежливо откажись и предложи переформулировать в контексте физики.\n\n" +
                     (string.IsNullOrWhiteSpace(setupContext) ? "" : $"Контекст текущей установки: {setupContext.Trim()}\n\n") +
                     "Вопрос пользователя: " + (userQuestion ?? "").Trim();
        return await GetChatAnswerRawAsync(prompt, cancellationToken);
    }

    private static async Task<string> GetChatAnswerRawAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var key = GetApiKey();
        if (string.IsNullOrWhiteSpace(key))
            return "Ключ API не найден. Задайте GROQ_API_KEY или файл groq_api_key.txt. " + GetKeySearchPathsMessage();

        prompt = prompt + NoLatexInstruction;

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + key);
            var body = new
            {
                model = "llama-3.1-8b-instant",
                messages = new[] { new { role = "user", content = prompt } },
                max_tokens = 1024
            };
            var json = JsonSerializer.Serialize(body);
            var response = await client.PostAsync(
                "https://api.groq.com/openai/v1/chat/completions",
                new StringContent(json, Encoding.UTF8, "application/json"),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return $"Ошибка API: {response.StatusCode}. {await response.Content.ReadAsStringAsync(cancellationToken)}";

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);
            var choices = doc.RootElement.GetProperty("choices");
            var first = choices.EnumerateArray().FirstOrDefault();
            if (first.ValueKind == JsonValueKind.Undefined)
                return "Пустой ответ от API.";
            var content = first.GetProperty("message").GetProperty("content").GetString();
            return SanitizeLatexToPlain(content ?? "Пустой ответ.");
        }
        catch (Exception ex)
        {
            return $"Ошибка запроса: {ex.Message}";
        }
    }

    /// <summary>Заменяет частые фрагменты LaTeX в ответе на читаемый текст для простого TextBox.</summary>
    private static string SanitizeLatexToPlain(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        text = Regex.Replace(text, @"\\\[", "");
        text = Regex.Replace(text, @"\\\]", "");
        text = Regex.Replace(text, @"\\\(", "");
        text = Regex.Replace(text, @"\\\)", "");
        text = Regex.Replace(text, @"\\cdot", "·");
        text = Regex.Replace(text, @"\\times", "×");
        text = Regex.Replace(text, @"\\frac\s*\{\s*([^{}]*)\}\s*\{\s*([^{}]*)\}", "($1)/($2)");
        text = Regex.Replace(text, @"\\sin\s*", "sin ");
        text = Regex.Replace(text, @"\\cos\s*", "cos ");
        text = Regex.Replace(text, @"\\tan\s*", "tan ");
        text = Regex.Replace(text, @"\\alpha", "α");
        text = Regex.Replace(text, @"\\beta", "β");
        text = Regex.Replace(text, @"\\omega", "ω");
        text = Regex.Replace(text, @"\\theta", "θ");
        text = Regex.Replace(text, @"\\rho", "ρ");
        text = Regex.Replace(text, @"\\nu", "ν");
        text = Regex.Replace(text, @"\\phi", "φ");
        text = Regex.Replace(text, @"\\varepsilon", "ε");
        text = Regex.Replace(text, @"_0\b", "₀");
        text = Regex.Replace(text, @"_1\b", "₁");
        text = Regex.Replace(text, @"_2\b", "₂");
        text = Regex.Replace(text, @"\^2\b", "²");
        text = Regex.Replace(text, @"\^3\b", "³");
        text = Regex.Replace(text, @"v_0", "v₀");
        text = Regex.Replace(text, @"y_0", "y₀");
        text = Regex.Replace(text, @"x_0", "x₀");
        text = Regex.Replace(text, @"v_\{0x\}", "v₀ₓ");
        text = Regex.Replace(text, @"v_\{0y\}", "v₀ᵧ");
        return text;
    }

    /// <summary>
    /// Запрос к Groq: объяснение для груза на наклонной плоскости (m, α, μ и рассчитанные силы).
    /// </summary>
    public static Task<string> GetInclinedPlaneExplanationAsync(
        double massKg,
        double angleDegrees,
        double frictionMu,
        double fWeight,
        double normalN,
        double fAlong,
        double fTr,
        double acceleration,
        bool isAtRest,
        CancellationToken cancellationToken = default)
    {
        var prompt = $"Объясни кратко и по-русски задачу о грузе на наклонной плоскости. Параметры: масса {massKg} кг, угол {angleDegrees}°, коэффициент трения μ = {frictionMu}. " +
                     $"Рассчитанные силы: F = {fWeight:F1} Н, N = {normalN:F1} Н, F вдоль плоскости = {fAlong:F1} Н, F трения = {fTr:F1} Н. " +
                     $"Ускорение: {acceleration:F2} м/с². Состояние: {(isAtRest ? "покой" : "скольжение вниз")}. " +
                     "Дай короткое структурированное объяснение с формулами и выводом.";
        return GetExplanationAsync(prompt, cancellationToken);
    }
}
