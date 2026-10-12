namespace ForTeacher.Services;

public interface IFileDialogService
{
    /// <returns>Полный путь к выбранному файлу или <c>null</c>.</returns>
    string? PickImageFile();

    /// <returns>Полный путь к PDF или <c>null</c>.</returns>
    string? PickPdfFile();
}
