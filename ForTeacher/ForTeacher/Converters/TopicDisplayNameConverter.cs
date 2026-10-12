using System.Globalization;
using System.Windows.Data;

namespace ForTeacher.Converters;

/// <summary>Ключ topic → короткое русское имя для таблицы.</summary>
public sealed class TopicDisplayNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string key)
            return value;

        return key.ToLowerInvariant() switch
        {
            "mechanics" => "Механика",
            "electrostatics" => "Электростатика",
            "optics" => "Оптика",
            "waves" => "Колебания и волны",
            "electricity" => "Электричество",
            "thermodynamics" => "Термодинамика",
            "atomic" => "Атомная физика",
            "hydro" => "Гидростатика",
            _ => key,
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
