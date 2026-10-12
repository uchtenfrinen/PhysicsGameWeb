using System.Globalization;
using System.Text.Json;
using System.Windows.Data;

namespace ForTeacher.Converters;

/// <summary>Сжатый вывод массива key_laws из JSON для колонки грида.</summary>
public sealed class KeyLawsJsonToDisplayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string json || string.IsNullOrWhiteSpace(json))
        {
            return "—";
        }

        try
        {
            var arr = JsonSerializer.Deserialize<List<string>>(json);
            if (arr is null || arr.Count == 0)
            {
                return "—";
            }

            return string.Join("; ", arr.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()));
        }
        catch
        {
            return json.Length > 120 ? json[..120] + "…" : json;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
