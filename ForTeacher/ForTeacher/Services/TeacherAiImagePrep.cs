using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace ForTeacher.Services;

/// <summary>Сжимает изображение для Vision API: Groq/OpenAI режут большие JSON с base64 (ошибка 413).</summary>
internal static class TeacherAiImagePrep
{
    /// <summary>Целевой потолок размера файла после JPEG (до base64 в JSON).</summary>
    internal const int TargetMaxBytes = 220_000;

    internal static string ClampTitle(string title, int maxChars = 400)
    {
        var t = title.Trim();
        return t.Length <= maxChars ? t : t[..maxChars] + "…";
    }

    internal static string ClampBody(string taskText, int maxChars = 14_000)
    {
        var t = taskText.Trim();
        return t.Length <= maxChars ? t : t[..maxChars] + "\n… [текст обрезан]";
    }

    /// <summary>Возвращает JPEG или <see langword="null"/> при ошибке декодирования.</summary>
    internal static byte[]? EncodeAsSmallJpeg(byte[] sourceBytes)
    {
        try
        {
            using var original = Image.Load(sourceBytes);
            var maxEdges = new[] { 1536, 1280, 1024, 768, 640, 512, 400 };
            var qualities = new[] { 85, 78, 70, 62, 52, 45, 38 };

            foreach (var maxEdge in maxEdges)
            {
                using var work = original.Clone(ctx =>
                    ctx.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(maxEdge, maxEdge),
                    }));

                foreach (var q in qualities)
                {
                    using var ms = new MemoryStream();
                    work.SaveAsJpeg(ms, new JpegEncoder { Quality = q });
                    var buf = ms.ToArray();
                    if (buf.Length <= TargetMaxBytes)
                        return buf;
                }
            }

            using (var ms = new MemoryStream())
            {
                original.SaveAsJpeg(ms, new JpegEncoder { Quality = 32 });
                return ms.ToArray();
            }
        }
        catch
        {
            return null;
        }
    }
}
