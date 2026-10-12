using System.Globalization;
using System.Windows.Data;

namespace WPFPhisics1._1.Converters;

/// <summary>Инвертирует bool: true → false, false → true. Удобно для привязки к read-only свойству типа !IsTwoLiquidsMode.</summary>
public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? false : true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? false : true;
    }
}
