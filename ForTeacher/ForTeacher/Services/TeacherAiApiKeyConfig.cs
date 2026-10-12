namespace ForTeacher.Services;

/// <summary>
/// Ключ для LLM (тот же приоритет, что у WPFPhisics1.1 для Groq). Не коммитьте ключ — оставьте null и используйте
/// <c>GROQ_API_KEY</c> или <c>groq_api_key.txt</c>.
/// </summary>
public static class TeacherAiApiKeyConfig
{
    /// <summary>Вставьте ключ (gsk_… / sk-…) или оставьте null.</summary>
    public static string? Key = null;
}
