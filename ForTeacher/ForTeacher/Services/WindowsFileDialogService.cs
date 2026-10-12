using Microsoft.Win32;

namespace ForTeacher.Services;

public sealed class WindowsFileDialogService : IFileDialogService
{
    public string? PickImageFile()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Изображения|*.png;*.jpg;*.jpeg;*.bmp;*.gif|Все файлы|*.*",
            CheckFileExists = true,
            Multiselect = false,
        };

        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    public string? PickPdfFile()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Документ PDF|*.pdf|Все файлы|*.*",
            CheckFileExists = true,
            Multiselect = false,
        };

        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }
}
