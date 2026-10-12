using System.IO;
using System.Linq;
using ForTeacher.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ForTeacher.Services;

public static class TeacherTaskPdfExporter
{
    public static void ExportToFile(TaskDefinition task, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(task);
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("Путь к файлу не задан.", nameof(outputPath));

        var dir = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(h =>
                {
                    h.Item().Text(task.TitleKey).FontSize(18).SemiBold();
                    h.Item().PaddingTop(4).Text($"Раздел: {task.Topic}").FontSize(10).FontColor(Colors.Grey.Darken2);
                    h.Item().PaddingTop(2).Text($"Внутренний id: {task.Id}").FontSize(9).FontColor(Colors.Grey.Medium);
                });

                page.Content().PaddingTop(16).Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Text("Условие и текст задачи").SemiBold().FontSize(12);
                    col.Item().Text(string.IsNullOrWhiteSpace(task.DescriptionKey) ? "—" : task.DescriptionKey);

                    if (task.ExpectedNumeric is { } n)
                        col.Item().Text($"Ожидаемый числовой ответ: {n.ToString(System.Globalization.CultureInfo.InvariantCulture)}").SemiBold();

                    col.Item().PaddingTop(8).Text("Методичка (PDF)").SemiBold().FontSize(12);
                    col.Item().Text(string.IsNullOrWhiteSpace(task.PdfPath)
                        ? "Не прикреплена."
                        : $"Прикреплена: {task.PdfPath.Replace('\\', '/')}");

                    if (task.Difficulty > 0)
                        col.Item().PaddingTop(4).Text($"Сложность (1–5): {task.Difficulty}").FontSize(10);

                    if (!string.IsNullOrWhiteSpace(task.AiReasoning))
                    {
                        col.Item().PaddingTop(8).Text("Комментарий ИИ").SemiBold().FontSize(11);
                        col.Item().Text(task.AiReasoning).FontSize(10).FontColor(Colors.Grey.Darken1);
                    }
                });
            });
        }).GeneratePdf(outputPath);
    }

    public static string SanitizeFileName(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "task";

        var invalid = Path.GetInvalidFileNameChars();
        var s = new string(title.Where(c => !invalid.Contains(c)).ToArray()).Trim();
        if (string.IsNullOrEmpty(s))
            return "task";
        return s.Length > 80 ? s[..80] : s;
    }
}
