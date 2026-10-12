using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ForTeacher.Models;

namespace ForTeacher.Services;

/// <summary>
/// Chat Completions (OpenAI-совместимый API). Ключ: тот же порядок, что у WPFPhisics1.1 Groq —
/// <see cref="TeacherAiCredentials"/>, опционально <c>FOR_TEACHER_OPENAI_API_KEY</c>.
/// URL: <c>FOR_TEACHER_OPENAI_BASE_URL</c> или авто: Groq для ключей <c>gsk_…</c>, иначе OpenAI.
/// </summary>
public sealed class OpenAiDifficultyAnalyzer : IDifficultyAnalyzer, IDifficultyAnalyzerDiagnostics, IDisposable
{
    private const string SystemPrompt =
        """
        Ты — эксперт-методист по физике. Твоя задача — проанализировать текст задачи (и при наличии — изображение с условием/чертежом) и вернуть строго JSON-объект без пояснений до и после.
        Критерии оценки сложности (поле difficulty, целое от 1 до 5):

        1: Прямое подставление в одну формулу (базовый уровень).
        2: Требуется вывод одной величины из формулы или перевод единиц СИ.
        3: Задача в 2-3 действия, использование законов из одного раздела.
        4: Комбинированная задача (2+ раздела физики), сложная математика (системы уравнений).
        5: Олимпиадный уровень, интегральное исчисление или нестандартное логическое мышление.

        Формат ответа (один JSON-объект):
        {
          "difficulty": <int 1-5>,
          "reasoning": "краткое обоснование на русском",
          "key_laws": ["закон 1", "закон 2"],
          "estimated_time_min": <int>
        }
        """;

    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly bool _ownsHttp;

    /// <inheritdoc />
    public string? LastFailureDetail { get; private set; }

    private bool IsGroqEndpoint => _baseUrl.Contains("groq.com", StringComparison.OrdinalIgnoreCase);

    public OpenAiDifficultyAnalyzer(HttpClient? httpClient = null)
    {
        _apiKey = TeacherAiCredentials.ResolveApiKey() ?? "";
        var envUrl = Environment.GetEnvironmentVariable("FOR_TEACHER_OPENAI_BASE_URL");
        if (!string.IsNullOrWhiteSpace(envUrl))
        {
            _baseUrl = envUrl.TrimEnd('/');
        }
        else if (_apiKey.StartsWith("gsk_", StringComparison.OrdinalIgnoreCase))
        {
            _baseUrl = "https://api.groq.com/openai/v1";
        }
        else
        {
            _baseUrl = "https://api.openai.com/v1";
        }

        if (httpClient is null)
        {
            _http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
            _ownsHttp = true;
        }
        else
        {
            _http = httpClient;
            _ownsHttp = false;
        }
    }

    public async Task<DifficultyAnalysisResult?> AnalyzeAsync(
        string title,
        string taskText,
        byte[]? imageBytes,
        string? imageMediaType,
        CancellationToken cancellationToken = default)
    {
        LastFailureDetail = null;

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            LastFailureDetail = "Пустой ключ API (внутренняя ошибка конфигурации).";
            return null;
        }

        var titleUse = TeacherAiImagePrep.ClampTitle(title);
        var textUse = TeacherAiImagePrep.ClampBody(taskText);

        byte[]? jpegPrepared = null;
        var hadSourceImage = imageBytes is { Length: > 0 } && !string.IsNullOrWhiteSpace(imageMediaType);
        if (hadSourceImage)
        {
            jpegPrepared = TeacherAiImagePrep.EncodeAsSmallJpeg(imageBytes!);
        }

        var result = await SendModelRequestAsync(titleUse, textUse, jpegPrepared, cancellationToken).ConfigureAwait(false);
        if (result is not null)
            return result;

        var retryWithoutImage = jpegPrepared is not null &&
                                LastFailureDetail != null &&
                                (LastFailureDetail.Contains("413", StringComparison.Ordinal)
                                 || LastFailureDetail.Contains("too large", StringComparison.OrdinalIgnoreCase)
                                 || LastFailureDetail.Contains("Too Large", StringComparison.OrdinalIgnoreCase));

        if (!retryWithoutImage)
            return null;

        LastFailureDetail = null;
        result = await SendModelRequestAsync(titleUse, textUse, imageJpeg: null, cancellationToken).ConfigureAwait(false);
        if (result is null)
            return null;

        return new DifficultyAnalysisResult
        {
            Difficulty = result.Difficulty,
            Reasoning =
                "(Изображение не учитывалось: сервер отклонил слишком большой запрос — выполнен повтор только по тексту.) "
                + result.Reasoning,
            KeyLaws = result.KeyLaws,
            EstimatedTimeMin = result.EstimatedTimeMin
        };
    }

    private async Task<DifficultyAnalysisResult?> SendModelRequestAsync(
        string titleUse,
        string textUse,
        byte[]? imageJpeg,
        CancellationToken cancellationToken)
    {
        var hasImage = imageJpeg is { Length: > 0 };
        var model = ResolveModel(_baseUrl, hasImage);

        var userText = $"Заголовок:\n{titleUse}\n\nТекст задачи:\n{textUse}";
        var userContent = new List<Dictionary<string, object?>>
        {
            new() { ["type"] = "text", ["text"] = userText }
        };

        if (hasImage)
        {
            var b64 = Convert.ToBase64String(imageJpeg!);
            userContent.Add(new Dictionary<string, object?>
            {
                ["type"] = "image_url",
                ["image_url"] = new Dictionary<string, object?> { ["url"] = $"data:image/jpeg;base64,{b64}" }
            });
        }

        var messages = new object[]
        {
            new Dictionary<string, object?> { ["role"] = "system", ["content"] = SystemPrompt },
            new Dictionary<string, object?> { ["role"] = "user", ["content"] = userContent }
        };

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var attempts = IsGroqEndpoint ? 2 : 1;

        for (var attempt = 0; attempt < attempts; attempt++)
        {
            var useResponseFormat = !(IsGroqEndpoint && attempt == 1);
            var payload = new Dictionary<string, object?>
            {
                ["model"] = model,
                ["temperature"] = 0.2,
                ["messages"] = messages
            };

            if (useResponseFormat)
                payload["response_format"] = new Dictionary<string, object?> { ["type"] = "json_object" };

            var json = JsonSerializer.Serialize(payload, jsonOptions);
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                if (IsGroqEndpoint && attempt == 0 && useResponseFormat)
                    continue;

                SetFailureFromResponse(resp.StatusCode, body);
                return null;
            }

            var raw = TryExtractMessageContent(body);
            if (string.IsNullOrWhiteSpace(raw))
            {
                LastFailureDetail = "В ответе API нет текста сообщения (choices[0].message.content).";
                return null;
            }

            var parsed = ParseDifficultyJson(StripCodeFences(raw));
            if (parsed is null)
            {
                LastFailureDetail =
                    "Модель вернула ответ, но не удалось извлечь поля difficulty/reasoning (ожидается JSON). Фрагмент: "
                    + TruncateForHint(raw, 280);
                return null;
            }

            return parsed;
        }

        return null;
    }

    private void SetFailureFromResponse(HttpStatusCode status, string body)
    {
        var code = (int)status;
        LastFailureDetail = status switch
        {
            HttpStatusCode.Unauthorized =>
                "401 Не авторизован: API отклонил ключ. Проверьте FOR_TEACHER_OPENAI_API_KEY / OPENAI_API_KEY / GROQ_API_KEY и соответствие ключа выбранному провайдеру (base URL).",
            HttpStatusCode.Forbidden =>
                "403 Доступ запрещён: проверьте ключ API и ограничения аккаунта провайдера.",
            HttpStatusCode.RequestEntityTooLarge =>
                "413 Запрос слишком большой (часто из‑за изображения). Текст и картинка уже сжимаются автоматически; повтор без картинки должен выполниться сам — если ошибка повторяется, уменьшите текст или уберите изображение.",
            HttpStatusCode.TooManyRequests =>
                "429 Слишком много запросов к API — подождите минуту и попробуйте снова.",
            HttpStatusCode.BadRequest =>
                $"400 Ошибка запроса: {TruncateForHint(body, 320)}",
            _ =>
                $"{code} {status}: {TruncateForHint(body, 380)}"
        };
    }

    private static string? TryExtractMessageContent(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("choices", out var choices) ||
                choices.ValueKind != JsonValueKind.Array ||
                choices.GetArrayLength() == 0)
            {
                return null;
            }

            var msg = choices[0];
            if (!msg.TryGetProperty("message", out var message) ||
                !message.TryGetProperty("content", out var contentEl))
            {
                return null;
            }

            return contentEl.GetString();
        }
        catch
        {
            return null;
        }
    }

    private static string TruncateForHint(string s, int maxLen)
    {
        var t = s.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return t.Length <= maxLen ? t : t[..maxLen] + "…";
    }

    /// <summary>Как у Groq в WPF: текст — llama-3.1-8b-instant; с картинкой — vision-модель Groq.</summary>
    private static string ResolveModel(string baseUrl, bool hasImage)
    {
        var configured = Environment.GetEnvironmentVariable("FOR_TEACHER_OPENAI_MODEL");
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.Trim();

        var isGroq = baseUrl.Contains("groq.com", StringComparison.OrdinalIgnoreCase);
        if (isGroq)
        {
            if (hasImage)
            {
                var vm = Environment.GetEnvironmentVariable("FOR_TEACHER_OPENAI_VISION_MODEL");
                return string.IsNullOrWhiteSpace(vm)
                    ? "meta-llama/llama-4-scout-17b-16e-instruct"
                    : vm.Trim();
            }

            return "llama-3.1-8b-instant";
        }

        return "gpt-4o-mini";
    }

    private static string StripCodeFences(string s)
    {
        var t = s.Trim();
        if (!t.StartsWith("```", StringComparison.Ordinal))
            return t;

        var firstNl = t.IndexOf('\n');
        if (firstNl >= 0)
            t = t[(firstNl + 1)..];

        var end = t.LastIndexOf("```", StringComparison.Ordinal);
        if (end > 0)
            t = t[..end];

        return t.Trim();
    }

    private static DifficultyAnalysisResult? ParseDifficultyJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!TryGetInt(root, "difficulty", out var d))
                return null;

            d = Math.Clamp(d, 1, 5);
            var reasoning = TryGetString(root, "reasoning") ?? "";
            var time = TryGetInt(root, "estimated_time_min", out var et) ? Math.Max(1, et) : 10;

            var laws = new List<string>();
            if (TryGetPropertyInsensitive(root, "key_laws", out var kl) && kl.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in kl.EnumerateArray())
                {
                    if (el.ValueKind == JsonValueKind.String)
                    {
                        var s = el.GetString();
                        if (!string.IsNullOrWhiteSpace(s))
                            laws.Add(s.Trim());
                    }
                }
            }

            return new DifficultyAnalysisResult
            {
                Difficulty = d,
                Reasoning = reasoning,
                KeyLaws = laws,
                EstimatedTimeMin = time
            };
        }
        catch
        {
            return null;
        }
    }

    private static bool TryGetPropertyInsensitive(JsonElement root, string name, out JsonElement el)
    {
        foreach (var p in root.EnumerateObject())
        {
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                el = p.Value;
                return true;
            }
        }

        el = default;
        return false;
    }

    private static bool TryGetInt(JsonElement root, string name, out int v)
    {
        v = 0;
        if (!TryGetPropertyInsensitive(root, name, out var el))
            return false;

        return el.ValueKind switch
        {
            JsonValueKind.Number => el.TryGetInt32(out v),
            JsonValueKind.String => int.TryParse(el.GetString(), out v),
            _ => false
        };
    }

    private static string? TryGetString(JsonElement root, string name) =>
        TryGetPropertyInsensitive(root, name, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;

    public void Dispose()
    {
        if (_ownsHttp)
            _http.Dispose();
    }
}
