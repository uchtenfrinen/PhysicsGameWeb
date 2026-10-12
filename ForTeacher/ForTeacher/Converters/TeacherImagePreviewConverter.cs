using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using ForTeacher.Services;

namespace ForTeacher.Converters;

/// <summary>Превью по пути из json (<c>Data/TeacherImages/…</c> относительно каталога игры).</summary>
public sealed class TeacherImagePreviewConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string rel || string.IsNullOrWhiteSpace(rel))
            return null;

        var full = AppPaths.ResolveGameRelativePath(rel);
        if (!File.Exists(full))
            return null;

        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(full, UriKind.Absolute);
            bmp.DecodePixelWidth = 160;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
