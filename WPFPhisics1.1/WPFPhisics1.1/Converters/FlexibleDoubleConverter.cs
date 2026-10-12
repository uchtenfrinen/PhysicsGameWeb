using System.Globalization;
using System.Windows.Data;

namespace WPFPhisics1._1.Converters;

/// <summary>
/// Конвертер для ввода чисел: принимает и точку, и запятую; при ошибке парсинга не ломает привязку.
/// </summary>
public class FlexibleDoubleConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            string format = parameter as string ?? "N1";
            return d.ToString(format, CultureInfo.InvariantCulture);
        }
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s))
            return Binding.DoNothing;

        // Поддержка и точки, и запятой как разделителя
        s = s.Trim().Replace(',', '.');
        if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            return result;

        return Binding.DoNothing;
    }
}
